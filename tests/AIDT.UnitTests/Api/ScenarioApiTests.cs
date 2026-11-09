using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIDT.Api.Contracts;
using AIDT.Application.Contracts;
using AIDT.Domain.Scenarios;
using AIDT.Domain.Workflows;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Shouldly;

namespace AIDT.UnitTests.Api;

public sealed class ScenarioApiTests :
    IClassFixture<WebApplicationFactory<Program>>,
    IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly WebApplicationFactory<Program> _factory;

    public ScenarioApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync() => await CleanupTestScenariosAsync();

    public async Task DisposeAsync() => await CleanupTestScenariosAsync();

    [SqlIntegrationFact]
    public async Task CatalogLifecycle_UsesImmutableViewsDepartmentBoundaryAndRollback()
    {
        using var manager = await LoginAsync("demo.agentmanager");
        using var researcher = await LoginAsync("demo.researcher");
        var rnd = (await GetAsync<IReadOnlyList<DepartmentDto>>(manager, "/api/v1/departments"))
            .Single(item => item.Code == "RND");
        var code = $"SCN-TEST-{Guid.NewGuid():N}"[..26].ToUpperInvariant();
        var createRequest = new CreateScenarioDraftRequest(
            code,
            "经理事项分派自动化",
            "验证场景草稿、发布、停用、恢复和回滚。",
            Guid.Parse(rnd.Id),
            BuildEventPayload());

        var createKey = "scenario-create-" + Guid.NewGuid().ToString("N");
        var created = await SendAsync<ScenarioDto>(
            manager,
            HttpMethod.Post,
            "/api/v1/scenarios/drafts",
            createRequest,
            createKey,
            HttpStatusCode.Created);
        var replay = await SendAsync<ScenarioDto>(
            manager,
            HttpMethod.Post,
            "/api/v1/scenarios/drafts",
            createRequest,
            createKey,
            HttpStatusCode.Created);
        replay.Id.ShouldBe(created.Id);
        created.DraftVersion.ShouldNotBeNull();
        created.CurrentPublishedVersion.ShouldBeNull();

        var visibleBeforePublish = await GetAsync<PagedResult<ScenarioDto>>(
            researcher,
            "/api/v1/scenarios?pageSize=100");
        visibleBeforePublish.Items.ShouldNotContain(item => item.Code == code);

        var validation = await SendAsync<ScenarioValidationDto>(
            manager,
            HttpMethod.Post,
            $"/api/v1/scenarios/{created.Id}/versions/{created.DraftVersion!.Id}/actions/validate",
            new ScenarioActionRequest(created.Version, "发布前校验"),
            "scenario-validate-" + Guid.NewGuid().ToString("N"));
        validation.IsValid.ShouldBeTrue();

        var afterValidation = await GetManagedAsync(manager, created.Id);
        var published = await SendAsync<ScenarioDto>(
            manager,
            HttpMethod.Post,
            $"/api/v1/scenarios/{created.Id}/versions/{afterValidation.DraftVersion!.Id}/actions/publish",
            new ScenarioActionRequest(afterValidation.Version, "发布 V1"),
            "scenario-publish-" + Guid.NewGuid().ToString("N"));
        published.CurrentPublishedVersion.ShouldNotBeNull();
        published.CurrentPublishedVersion!.VersionNumber.ShouldBe(1);
        published.CurrentPublishedVersion.References.ShouldNotBeNull();
        published.CurrentPublishedVersion.Payload.Steps.Count.ShouldBeGreaterThan(0);

        var visibleAfterPublish = await GetAsync<PagedResult<ScenarioDto>>(
            researcher,
            "/api/v1/scenarios?pageSize=100");
        visibleAfterPublish.Items.ShouldContain(item => item.Code == code);

        var disabled = await SendAsync<ScenarioDto>(
            manager,
            HttpMethod.Post,
            $"/api/v1/scenarios/{created.Id}/actions/disable",
            new ScenarioActionRequest(published.Version, "部门临时停用"),
            "scenario-disable-" + Guid.NewGuid().ToString("N"));
        disabled.LifecycleStatus.ShouldBe(ScenarioLifecycleStatus.Disabled);
        var hiddenWhileDisabled = await GetAsync<PagedResult<ScenarioDto>>(
            researcher,
            "/api/v1/scenarios?pageSize=100");
        hiddenWhileDisabled.Items.ShouldNotContain(item => item.Code == code);

        var restored = await SendAsync<ScenarioDto>(
            manager,
            HttpMethod.Post,
            $"/api/v1/scenarios/{created.Id}/actions/restore",
            new ScenarioActionRequest(disabled.Version, "恢复使用"),
            "scenario-restore-" + Guid.NewGuid().ToString("N"));
        restored.LifecycleStatus.ShouldBe(ScenarioLifecycleStatus.Published);

        var nextDraft = await SendAsync<ScenarioDto>(
            manager,
            HttpMethod.Post,
            $"/api/v1/scenarios/{created.Id}/drafts",
            new CreateNextScenarioVersionDraftRequest(restored.Version, "创建 V2 草稿"),
            "scenario-next-draft-" + Guid.NewGuid().ToString("N"),
            HttpStatusCode.Created);
        nextDraft.DraftVersion.ShouldNotBeNull();
        nextDraft.DraftVersion!.VersionNumber.ShouldBe(2);
        var immutable = await SendForStatusAsync(
            manager,
            HttpMethod.Put,
            $"/api/v1/scenarios/{created.Id}/drafts/{restored.CurrentPublishedVersion!.Id}",
            new UpdateScenarioDraftRequest(
                1,
                "非法覆盖",
                "已发布版本不能修改",
                Guid.Parse(rnd.Id),
                BuildEventPayload()),
            "scenario-immutable-" + Guid.NewGuid().ToString("N"));
        immutable.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var current = await GetManagedAsync(manager, created.Id);
        var validationV2 = await SendAsync<ScenarioValidationDto>(
            manager,
            HttpMethod.Post,
            $"/api/v1/scenarios/{created.Id}/versions/{current.DraftVersion!.Id}/actions/validate",
            new ScenarioActionRequest(current.Version, "V2 校验"),
            "scenario-v2-validate-" + Guid.NewGuid().ToString("N"));
        validationV2.IsValid.ShouldBeTrue();
        current = await GetManagedAsync(manager, created.Id);
        var publishedV2 = await SendAsync<ScenarioDto>(
            manager,
            HttpMethod.Post,
            $"/api/v1/scenarios/{created.Id}/versions/{current.DraftVersion!.Id}/actions/publish",
            new ScenarioActionRequest(current.Version, "发布 V2"),
            "scenario-v2-publish-" + Guid.NewGuid().ToString("N"));
        publishedV2.CurrentPublishedVersion!.VersionNumber.ShouldBe(2);

        var rollback = await SendAsync<ScenarioDto>(
            manager,
            HttpMethod.Post,
            $"/api/v1/scenarios/{created.Id}/actions/rollback",
            new RollbackScenarioRequest(publishedV2.Version, 1, "回滚到已验证 V1"),
            "scenario-rollback-" + Guid.NewGuid().ToString("N"));
        rollback.CurrentPublishedVersion!.VersionNumber.ShouldBe(3);
        JsonSerializer.Serialize(rollback.CurrentPublishedVersion.Payload, JsonOptions)
            .ShouldBe(JsonSerializer.Serialize(
                published.CurrentPublishedVersion.Payload,
                JsonOptions));

        var versions = await GetAsync<IReadOnlyList<ScenarioVersionDto>>(
            manager,
            $"/api/v1/scenarios/{created.Id}/versions");
        versions.Select(item => item.VersionNumber).ShouldBe([3, 2, 1]);
        versions.Single(item => item.VersionNumber == 1).Status
            .ShouldBe(ScenarioVersionStatus.Retired);
        versions.Single(item => item.VersionNumber == 2).Status
            .ShouldBe(ScenarioVersionStatus.Retired);
        versions.Single(item => item.VersionNumber == 3).Status
            .ShouldBe(ScenarioVersionStatus.Published);
    }

    [SqlIntegrationFact]
    public async Task SixBaseScenarios_StartRealBusinessChainsAndRemainIdempotent()
    {
        using var admin = await LoginAsync("demo.admin");
        using var researcher = await LoginAsync("demo.researcher");
        using var qa = await LoginAsync("demo.qa");
        using var supply = await LoginAsync("demo.supply");
        using var finance = await LoginAsync("demo.finance");
        using var pm = await LoginAsync("demo.pm");
        var all = await GetAsync<PagedResult<ScenarioDto>>(
            admin,
            "/api/v1/scenarios?pageSize=100");
        var departments = await GetAsync<IReadOnlyList<DepartmentDto>>(
            admin,
            "/api/v1/departments");
        var rnd = departments.Single(item => item.Code == "RND");
        var quality = departments.Single(item => item.Code == "QA");
        var supplyDepartment = departments.Single(item => item.Code == "SUPPLY");
        var pmSession = await GetAsync<SessionDto>(pm, "/api/v1/auth/session");

        var sampleScene = all.Items.Single(item => item.Code == "SCN-RND-SAMPLE-TO-PRODUCTION");
        var sampleInput = new ScenarioStartInputDto(
            DepartmentId: Guid.Parse(rnd.Id),
            ObjectCode: "SCN-SAMPLE-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(),
            Title: "场景样品到转产验收",
            Fields: new Dictionary<string, string?>
            {
                ["Customer"] = "内部演示客户",
                ["Product"] = "苯丙乳液样品",
                ["Requirement"] = "验证样品到转产场景的业务链。",
                ["TrialRequired"] = "Yes"
            });
        var sample = await StartAsync(
            researcher,
            sampleScene.Id,
            sampleInput,
            "scenario-start-sample-" + Guid.NewGuid().ToString("N"));
        sample.BusinessObjectId.ShouldNotBeNull();
        sample.WorkflowInstanceId.ShouldNotBeNull();
        sample.WorkflowTaskId.ShouldNotBeNull();
        sample.BusinessLinks.ShouldContain(item => item.Type == "BusinessObject");
        sample.BusinessLinks.ShouldContain(item => item.Type == "WorkflowInstance");

        var purchaseAttachment = await UploadTextAsync(
            supply,
            Guid.Parse(supplyDepartment.Id),
            "采购报价与验收材料");
        var purchaseScene = all.Items.Single(item => item.Code == "SCN-SCM-PURCHASE-TO-RECEIPT");
        var purchase = await StartAsync(
            supply,
            purchaseScene.Id,
            new ScenarioStartInputDto(
                DepartmentId: Guid.Parse(supplyDepartment.Id),
                ObjectCode: "SCN-PUR-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(),
                Title: "场景采购请购验收",
                Fields: new Dictionary<string, string?>
                {
                    ["Amount"] = "3200.00",
                    ["Category"] = "实验耗材",
                    ["Requester"] = "采购专员",
                    ["Supplier"] = "内部演示供应商"
                },
                AttachmentIds: [Guid.Parse(purchaseAttachment.Id)]),
            "scenario-start-purchase-" + Guid.NewGuid().ToString("N"));
        purchase.BusinessObjectId.ShouldNotBeNull();
        purchase.AttachmentIds.ShouldContain(purchaseAttachment.Id);

        var financeScene = all.Items.Single(item => item.Code == "SCN-FIN-PAYMENT-REVIEW");
        var financeLaunch = await StartAsync(
            supply,
            financeScene.Id,
            new ScenarioStartInputDto(
                BusinessObjectId: Guid.Parse(purchase.BusinessObjectId!),
                AttachmentIds: [Guid.Parse(purchaseAttachment.Id)],
                Comment: "仅进行财务人工复核"),
            "scenario-start-finance-" + Guid.NewGuid().ToString("N"));
        financeLaunch.WorkflowTaskId.ShouldNotBeNull();
        financeLaunch.PermanentHumanBoundaries.ShouldContain("财务付款");
        (await GetAsync<PagedResult<WorkflowTaskDto>>(
            finance,
            "/api/v1/workflow-tasks?status=Pending&pageSize=100"))
            .Items.ShouldContain(item => item.Id == financeLaunch.WorkflowTaskId);

        var managerScene = all.Items.Single(item => item.Code == "SCN-MGR-ASSIGNMENT");
        var managerInput = new ScenarioStartInputDto(
            DepartmentId: Guid.Parse(rnd.Id),
            Title: "经理事项分派",
            Content: "从会议行动项转入部门事项，由经理确认负责人和期限。");
        var managerLaunch = await StartAsync(
            pm,
            managerScene.Id,
            managerInput,
            "scenario-start-manager-" + Guid.NewGuid().ToString("N"));
        managerLaunch.EventInboxId.ShouldNotBeNull();
        var managerReplay = await StartAsync(
            pm,
            managerScene.Id,
            managerInput,
            "scenario-start-manager-" + Guid.NewGuid().ToString("N"),
            expectCreated: false);
        managerReplay.EventInboxId.ShouldNotBe(managerLaunch.EventInboxId);

        var project = await SendAsync<ProjectDto>(
            pm,
            HttpMethod.Post,
            "/api/v1/projects",
            new CreateProjectRequest(
                "SCN-PRJ-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(),
                "场景项目里程碑与风险",
                Guid.Parse(pmSession.UserId),
                Guid.Parse(rnd.Id),
                DateTimeOffset.UtcNow.AddDays(-2),
                DateTimeOffset.UtcNow.AddDays(30),
                "验证场景创建真实项目任务。",
                "执行",
                "Internal"),
            "scenario-project-" + Guid.NewGuid().ToString("N"),
            HttpStatusCode.Created);
        var projectScene = all.Items.Single(item => item.Code == "SCN-PM-MILESTONE-RISK");
        var projectLaunch = await StartAsync(
            pm,
            projectScene.Id,
            new ScenarioStartInputDto(
                ProjectId: Guid.Parse(project.Id),
                AssigneeId: Guid.Parse(pmSession.UserId),
                TaskTitle: "处理场景识别的里程碑风险",
                TaskContent: "核对供应商来料风险并补充证据。",
                DueAt: DateTimeOffset.UtcNow.AddDays(3),
                Priority: "P1"),
            "scenario-start-project-" + Guid.NewGuid().ToString("N"));
        projectLaunch.ProjectTaskId.ShouldNotBeNull();

        var qualityAttachment = await UploadTextAsync(
            qa,
            Guid.Parse(quality.Id),
            "批次检测与异常现场材料");
        var qualityScene = all.Items.Single(item => item.Code == "SCN-QA-DEVIATION-TO-CAPA");
        var qualityLaunch = await StartAsync(
            qa,
            qualityScene.Id,
            new ScenarioStartInputDto(
                DepartmentId: Guid.Parse(quality.Id),
                ObjectCode: "SCN-QA-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(),
                Title: "场景批次异常到 CAPA",
                Fields: new Dictionary<string, string?>
                {
                    ["Alarm"] = "批次检测结果超出内部控制限",
                    ["BatchNo"] = "B-SCN-" + Guid.NewGuid().ToString("N")[..6],
                    ["Severity"] = "Major"
                },
                AttachmentIds: [Guid.Parse(qualityAttachment.Id)]),
            "scenario-start-quality-" + Guid.NewGuid().ToString("N"));
        qualityLaunch.BusinessObjectId.ShouldNotBeNull();
        qualityLaunch.WorkflowInstanceId.ShouldNotBeNull();

        var preflight = await SendAsync<ScenarioPreflightDto>(
            researcher,
            HttpMethod.Post,
            $"/api/v1/scenarios/{sampleScene.Id}/actions/preflight",
            sampleInput,
            null);
        preflight.CanStart.ShouldBeTrue();
        preflight.References.ObjectTypes.ShouldContain(item => item.Code == "CUSTOMER_SAMPLE");

        var myLaunches = await GetAsync<PagedResult<ScenarioLaunchSummaryDto>>(
            researcher,
            "/api/v1/scenarios/launches?pageSize=100");
        var sampleSummary = myLaunches.Items.Single(item => item.Id == sample.Id);
        sampleSummary.ScenarioName.ShouldBe("研发样品到转产");
        sampleSummary.CurrentStep.ShouldNotBeNullOrWhiteSpace();
        sampleSummary.WaitingFor.ShouldNotBeNullOrWhiteSpace();
        sampleSummary.WorkflowStatus.ShouldBe("进行中");
        sampleSummary.IsDemo.ShouldBeTrue();
        sampleSummary.BusinessLinks.ShouldContain(item => item.Type == "BusinessObject");
        sampleSummary.EvidencePath.ShouldContain(sample.Id);

        using var researcherAfterRelogin = await LoginAsync("demo.researcher");
        var reloadedLaunches = await GetAsync<PagedResult<ScenarioLaunchSummaryDto>>(
            researcherAfterRelogin,
            "/api/v1/scenarios/launches?pageSize=100");
        reloadedLaunches.Items.ShouldContain(item =>
            item.Id == sample.Id && item.CurrentStep == sampleSummary.CurrentStep);

        var firstTask = (await GetAsync<PagedResult<WorkflowTaskDto>>(
                researcherAfterRelogin,
                "/api/v1/workflow-tasks?status=Pending&pageSize=100"))
            .Items.Single(item =>
                item.WorkflowInstanceId == sample.WorkflowInstanceId
                && item.Id == sample.WorkflowTaskId);
        await SendAsync<WorkflowTaskDto>(
            researcherAfterRelogin,
            HttpMethod.Post,
            $"/api/v1/workflow-tasks/{firstTask.Id}/actions/decide",
            new TaskDecisionRequest(
                WorkflowDecision.Approve,
                firstTask.Version,
                "场景进度真源回归",
                "scenario-progress-" + Guid.NewGuid().ToString("N")),
            null);
        var progressed = await GetAsync<PagedResult<ScenarioLaunchSummaryDto>>(
            researcherAfterRelogin,
            "/api/v1/scenarios/launches?pageSize=100");
        var progressedSample = progressed.Items.Single(item => item.Id == sample.Id);
        progressedSample.CurrentStep.ShouldNotBe(sampleSummary.CurrentStep);
        progressedSample.WorkflowStatus.ShouldBe("进行中");
    }

    [SqlIntegrationFact]
    public async Task IdempotencyConflictAndDuplicateObjectFailureLeaveNoScenarioHalfProduct()
    {
        using var researcher = await LoginAsync("demo.researcher");
        var all = await GetAsync<PagedResult<ScenarioDto>>(
            researcher,
            "/api/v1/scenarios?pageSize=100");
        var rnd = (await GetAsync<IReadOnlyList<DepartmentDto>>(
                researcher,
                "/api/v1/departments"))
            .Single(item => item.Code == "RND");
        var scene = all.Items.Single(item => item.Code == "SCN-RND-SAMPLE-TO-PRODUCTION");
        var objectCode = "SCN-IDEMP-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        var input = new ScenarioStartInputDto(
            DepartmentId: Guid.Parse(rnd.Id),
            ObjectCode: objectCode,
            Title: "幂等与失败回滚验证",
            Fields: new Dictionary<string, string?>
            {
                ["Customer"] = "内部演示客户",
                ["Product"] = "样品",
                ["Requirement"] = "验证启动幂等和失败回滚。"
            });
        var key = "scenario-launch-idem-" + Guid.NewGuid().ToString("N");
        var first = await StartAsync(researcher, scene.Id, input, key);
        var replay = await StartAsync(researcher, scene.Id, input, key, expectCreated: false);
        replay.Id.ShouldBe(first.Id);
        replay.BusinessObjectId.ShouldBe(first.BusinessObjectId);

        var conflict = await SendForStatusAsync(
            researcher,
            HttpMethod.Post,
            $"/api/v1/scenarios/{scene.Id}/actions/start",
            input with { Title = "同键不同内容" },
            key);
        conflict.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var beforeFailures = await CountScenarioBusinessChainsAsync(objectCode);
        var failedKey = "scenario-launch-fail-" + Guid.NewGuid().ToString("N");
        var failed = await SendForStatusAsync(
            researcher,
            HttpMethod.Post,
            $"/api/v1/scenarios/{scene.Id}/actions/start",
            input,
            failedKey);
        failed.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await CountScenarioBusinessChainsAsync(objectCode)).ShouldBe(beforeFailures);
    }

    [SqlIntegrationFact]
    public async Task HighRiskPublish_RequiresSecondPlatformAdministrator()
    {
        using var admin = await LoginAsync("demo.admin");
        using var reviewer = await LoginAsync("demo.security");
        var rnd = (await GetAsync<IReadOnlyList<DepartmentDto>>(
                admin,
                "/api/v1/departments"))
            .Single(item => item.Code == "RND");
        var code = $"SCN-HIGH-{Guid.NewGuid():N}"[..26].ToUpperInvariant();
        var created = await SendAsync<ScenarioDto>(
            admin,
            HttpMethod.Post,
            "/api/v1/scenarios/drafts",
            new CreateScenarioDraftRequest(
                code,
                "高风险质量异常场景",
                "验证高风险场景发布双人审核。",
                Guid.Parse(rnd.Id),
                BuildHighRiskPayload()),
            "scenario-high-create-" + Guid.NewGuid().ToString("N"),
            HttpStatusCode.Created);
        var validation = await SendAsync<ScenarioValidationDto>(
            admin,
            HttpMethod.Post,
            $"/api/v1/scenarios/{created.Id}/versions/{created.DraftVersion!.Id}/actions/validate",
            new ScenarioActionRequest(created.Version, "高风险场景校验"),
            "scenario-high-validate-" + Guid.NewGuid().ToString("N"));
        validation.IsValid.ShouldBeTrue();

        created = await GetManagedAsync(admin, created.Id);
        var pending = await SendAsync<ScenarioDto>(
            admin,
            HttpMethod.Post,
            $"/api/v1/scenarios/{created.Id}/versions/{created.DraftVersion!.Id}/actions/publish",
            new ScenarioActionRequest(created.Version, "申请发布质量场景"),
            "scenario-high-publish-" + Guid.NewGuid().ToString("N"));
        pending.CurrentPublishedVersion.ShouldBeNull();
        pending.PendingApprovalRequestId.ShouldNotBeNullOrWhiteSpace();
        pending.PendingApprovalStatus.ShouldBe("Pending");

        var requests = await GetAsync<IReadOnlyList<ScenarioApprovalRequestDto>>(
            reviewer,
            "/api/v1/scenarios/approval-requests?status=Pending");
        var approval = requests.Single(item => item.ScenarioId == created.Id);
        var selfReview = await SendForStatusAsync(
            admin,
            HttpMethod.Post,
            $"/api/v1/scenarios/approval-requests/{approval.Id}/actions/review",
            new ReviewScenarioPublishRequest(created.Version, true, "申请人不能自审"),
            "scenario-high-self-review-" + Guid.NewGuid().ToString("N"));
        selfReview.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        created = await GetManagedAsync(admin, created.Id);
        var published = await SendAsync<ScenarioDto>(
            reviewer,
            HttpMethod.Post,
            $"/api/v1/scenarios/approval-requests/{approval.Id}/actions/review",
            new ReviewScenarioPublishRequest(created.Version, true, "复核通过"),
            "scenario-high-review-" + Guid.NewGuid().ToString("N"));
        published.CurrentPublishedVersion.ShouldNotBeNull();
        published.CurrentPublishedVersion!.Payload.ObjectTypeCodes.ShouldContain("QUALITY_DEVIATION");
        published.PendingApprovalRequestId.ShouldBeNull();
    }

    private static async Task<ScenarioLaunchDto> StartAsync(
        AuthenticatedApiClient client,
        string scenarioId,
        ScenarioStartInputDto input,
        string key,
        bool expectCreated = true) =>
        await SendAsync<ScenarioLaunchDto>(
            client,
            HttpMethod.Post,
            $"/api/v1/scenarios/{scenarioId}/actions/start",
            input,
            key,
            expectCreated ? HttpStatusCode.Created : HttpStatusCode.Created);

    private static ScenarioDefinitionPayloadDto BuildEventPayload() =>
        new(
            "验证经理从事项来源发起真实任务。",
            ScenarioStartMode.EventInbox,
            "经理收到事项后发起。",
            ["RND"],
            ["部门负责人"],
            [],
            null,
            null,
            [
                new ScenarioStepDefinitionDto(
                    1,
                    "INTAKE",
                    "事项进入收件箱",
                    "保留来源和原文。",
                    "部门负责人",
                    "创建事项",
                    "形成可追溯事项",
                    "事项、审计和附件")
            ],
            [
                new ScenarioPreparationItemDto(
                    "EVENT_CONTENT",
                    "事项标题与内容",
                    true,
                    "EventInbox",
                    "必须保留业务原文")
            ],
            ["Attachment"],
            ["MinutesSource"],
            [],
            ScenarioRules.PermanentHumanBoundaries.Order().ToList(),
            ["event-inbox:Create"],
            ["EventInbox", "Audit"]);

    private static ScenarioDefinitionPayloadDto BuildHighRiskPayload() =>
        new(
            "质量异常进入受控 CAPA 流程，质量放行仍由人工负责。",
            ScenarioStartMode.CreateBusinessObjectAndWorkflow,
            "质量异常登记后发起。",
            ["RND", "QA"],
            ["质量工程师", "质量负责人"],
            ["QUALITY_DEVIATION"],
            "BATCH_DEVIATION_CAPA",
            1,
            [
                new ScenarioStepDefinitionDto(
                    1,
                    "CAPA_DECISION",
                    "CAPA 决策",
                    "完成质量调查和 CAPA 人工审批。",
                    "质量负责人",
                    "人工审批 CAPA",
                    "形成可追溯结论",
                    "流程任务、附件和审计",
                    true)
            ],
            [
                new ScenarioPreparationItemDto(
                    "DEVIATION",
                    "质量偏差对象",
                    true,
                    "BusinessObject",
                    "批次和异常信息")
            ],
            ["Attachment"],
            ["MinutesSource"],
            [],
            ScenarioRules.PermanentHumanBoundaries.Order().ToList(),
            ["business-object:Create", "workflow-instance:Submit"],
            ["质量偏差", "CAPA 流程", "人工质量边界审计"]);

    private static async Task<ScenarioDto> GetManagedAsync(
        AuthenticatedApiClient client,
        string scenarioId)
    {
        var managed = await GetAsync<PagedResult<ScenarioDto>>(
            client,
            "/api/v1/scenarios/manage?pageSize=100");
        return managed.Items.Single(item => item.Id == scenarioId);
    }

    private static async Task<FileAttachmentDto> UploadTextAsync(
        AuthenticatedApiClient client,
        Guid departmentId,
        string content)
    {
        using var multipart = new MultipartFormDataContent();
        var bytes = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        bytes.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        multipart.Add(bytes, "File", $"scenario-{Guid.NewGuid():N}.txt");
        multipart.Add(new StringContent(departmentId.ToString("D")), "DepartmentId");
        multipart.Add(new StringContent("Internal"), "Sensitivity");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/files")
        {
            Content = multipart
        };
        request.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        using var response = await client.Client.SendAsync(request);
        response.StatusCode.ShouldBe(
            HttpStatusCode.Created,
            await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<FileAttachmentDto>>(JsonOptions);
        return payload!.Data!;
    }

    private async Task<AuthenticatedApiClient> LoginAsync(string userName)
    {
        var client = _factory.CreateClient();
        var token = await GetCsrfTokenAsync(client);
        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/demo-login")
        {
            Content = JsonContent.Create(new DemoLoginRequest(userName))
        };
        login.Headers.Add("X-CSRF-TOKEN", token);
        var response = await client.SendAsync(login);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return new AuthenticatedApiClient(client, await GetCsrfTokenAsync(client));
    }

    private static async Task<T> GetAsync<T>(
        AuthenticatedApiClient client,
        string path)
    {
        var response = await client.Client.GetAsync(path);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
        return payload!.Data!;
    }

    private static async Task<T> SendAsync<T>(
        AuthenticatedApiClient client,
        HttpMethod method,
        string path,
        object body,
        string? idempotencyKey,
        HttpStatusCode expected = HttpStatusCode.OK)
    {
        using var response = await SendForStatusAsync(
            client,
            method,
            path,
            body,
            idempotencyKey);
        response.StatusCode.ShouldBe(expected, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
        return payload!.Data!;
    }

    private static async Task<HttpResponseMessage> SendForStatusAsync(
        AuthenticatedApiClient client,
        HttpMethod method,
        string path,
        object body,
        string? idempotencyKey)
    {
        using var request = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        request.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        return await client.Client.SendAsync(request);
    }

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        using var document = JsonDocument.Parse(await client.GetStringAsync("/api/v1/auth/csrf"));
        return document.RootElement
            .GetProperty("data")
            .GetProperty("requestToken")
            .GetString() ?? throw new InvalidOperationException("CSRF token is missing.");
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static async Task CleanupTestScenariosAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("AIDT_SQL_CONNECTION")
            ?? throw new InvalidOperationException("AIDT_SQL_CONNECTION is required.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            DECLARE @Ids TABLE (Id uniqueidentifier PRIMARY KEY);
            INSERT INTO @Ids (Id)
            SELECT Id FROM [Scenario].[ScenarioDefinition]
            WHERE Code LIKE N'SCN-TEST-%'
               OR Code LIKE N'SCN-HIGH-%';

            DELETE launchRow
            FROM [Scenario].[ScenarioLaunch] launchRow
            JOIN @Ids ids ON ids.Id = launchRow.ScenarioId;
            DELETE requestRow
            FROM [Identity].[AccessChangeRequest] requestRow
            WHERE requestRow.RequestType = N'ScenarioPublish'
              AND JSON_VALUE(requestRow.PayloadJson, N'$.scenarioId') IN (
                  SELECT CONVERT(nvarchar(36), Id) FROM @Ids);
            DELETE versionRow
            FROM [Scenario].[ScenarioVersion] versionRow
            JOIN @Ids ids ON ids.Id = versionRow.ScenarioId;
            DELETE definitionRow
            FROM [Scenario].[ScenarioDefinition] definitionRow
            JOIN @Ids ids ON ids.Id = definitionRow.Id;
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<int> CountScenarioBusinessChainsAsync(string objectCode)
    {
        var connectionString = Environment.GetEnvironmentVariable("AIDT_SQL_CONNECTION")
            ?? throw new InvalidOperationException("AIDT_SQL_CONNECTION is required.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                (SELECT COUNT(*) FROM [BusinessObject].[BusinessObject] WHERE ObjectCode=@Code)
              + (SELECT COUNT(*) FROM [Workflow].[WorkflowInstance] wi
                 JOIN [BusinessObject].[BusinessObject] bo ON bo.Id=wi.BusinessObjectId
                 WHERE bo.ObjectCode=@Code);
            """;
        command.Parameters.AddWithValue("@Code", objectCode);
        return Convert.ToInt32(
            await command.ExecuteScalarAsync(),
            CultureInfo.InvariantCulture);
    }

    private sealed class AuthenticatedApiClient(HttpClient client, string csrfToken) : IDisposable
    {
        public HttpClient Client { get; } = client;
        public string CsrfToken { get; } = csrfToken;

        public void Dispose() => Client.Dispose();
    }
}
