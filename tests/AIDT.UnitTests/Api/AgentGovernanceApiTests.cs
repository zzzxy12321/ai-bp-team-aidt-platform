using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIDT.Api.Contracts;
using AIDT.Application.Contracts;
using AIDT.Domain.Agents;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Shouldly;

namespace AIDT.UnitTests.Api;

public sealed class AgentGovernanceApiTests :
    IClassFixture<WebApplicationFactory<Program>>,
    IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private static readonly Guid LowRiskServiceUserId =
        Guid.Parse("10000000-0000-0000-0000-000000000028");
    private static readonly Guid HighRiskServiceUserId =
        Guid.Parse("10000000-0000-0000-0000-000000000029");
    private static readonly Guid AdminUserId =
        Guid.Parse("10000000-0000-0000-0000-000000000010");

    private readonly WebApplicationFactory<Program> _factory;

    public AgentGovernanceApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync() => await CleanTestProfilesAsync();

    public async Task DisposeAsync() => await CleanTestProfilesAsync();

    [SqlIntegrationFact]
    public async Task Lifecycle_Permissions_Idempotency_AndTokenRevocation_AreEnforced()
    {
        using var manager = await LoginAsync("demo.agentmanager");
        using var researcher = await LoginAsync("demo.researcher");
        using var admin = await LoginAsync("demo.admin");
        var departments = await GetAsync<IReadOnlyList<DepartmentDto>>(
            manager,
            "/api/v1/departments");
        var rnd = departments.Single(item => item.Code == "RND");
        var qa = (await GetAsync<IReadOnlyList<DepartmentDto>>(
                admin,
                "/api/v1/departments"))
            .Single(item => item.Code == "QA");
        var code = $"AGT-6A-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
        var createRequest = CreateDraftRequest(
            code,
            LowRiskServiceUserId,
            Guid.Parse(rnd.Id),
            AgentAutonomyLevel.A2,
            AgentRiskLevel.Low,
            0,
            ["workflow.read"]);

        var createKey = "agent-create-" + Guid.NewGuid().ToString("N");
        var created = await SendAsync<AgentProfileDetailDto>(
            manager,
            HttpMethod.Post,
            "/api/v1/agents/drafts",
            createRequest,
            createKey,
            HttpStatusCode.Created);
        var repeated = await SendAsync<AgentProfileDetailDto>(
            manager,
            HttpMethod.Post,
            "/api/v1/agents/drafts",
            createRequest,
            createKey,
            HttpStatusCode.Created);
        repeated.Id.ShouldBe(created.Id);
        created.LifecycleStatus.ShouldBe(AgentProfileLifecycleStatus.Draft);

        var identityOptions = await GetAsync<AgentIdentityOptionsDto>(
            manager,
            $"/api/v1/agents/options?profileId={created.Id}");
        identityOptions.Departments.ShouldContain(item => item.Code == "RND" && item.IsManager);
        identityOptions.Departments.ShouldNotContain(item => item.Code == "QA");
        identityOptions.People.ShouldNotBeEmpty();
        identityOptions.ServiceIdentities.ShouldContain(item =>
            item.Id == LowRiskServiceUserId.ToString("D")
            && item.BoundAgentProfileId == created.Id);
        identityOptions.PermanentHumanBoundaries.ShouldContain("payment");
        identityOptions.Capabilities.ShouldContain(item =>
            item.Code == "workflow.read" && item.RequiredPermissions.Count > 0);

        var ordinaryVisibleDrafts = await GetAsync<PagedResult<AgentProfileDetailDto>>(
            researcher,
            "/api/v1/agents/manage?pageSize=100");
        ordinaryVisibleDrafts.Items.ShouldNotContain(item => item.Code == code);

        var crossDepartment = await SendForStatusAsync(
            manager,
            HttpMethod.Post,
            "/api/v1/agents/drafts",
            createRequest with { Code = code + "X", DepartmentId = Guid.Parse(qa.Id) },
            "agent-cross-" + Guid.NewGuid().ToString("N"));
        crossDepartment.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var versions = await GetAsync<IReadOnlyList<AgentProfileVersionDto>>(
            manager,
            $"/api/v1/agents/{created.Id}/versions");
        var draft = versions.Single();
        var staleUpdate = await SendForStatusAsync(
            manager,
            HttpMethod.Put,
            $"/api/v1/agents/{created.Id}/drafts/{draft.Id}",
            UpdateRequest(draft, 999, ["workflow.read"]),
            null);
        staleUpdate.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        await SendAsync<AgentProfileValidationDto>(
            manager,
            HttpMethod.Post,
            $"/api/v1/agents/{created.Id}/versions/{draft.Id}/actions/validate",
            new ValidateAgentProfileVersionRequest(draft.Revision),
            "agent-validate-" + Guid.NewGuid().ToString("N"));
        versions = await GetAsync<IReadOnlyList<AgentProfileVersionDto>>(
            manager,
            $"/api/v1/agents/{created.Id}/versions");
        draft = versions.Single();
        var publishKey = "agent-publish-" + Guid.NewGuid().ToString("N");
        var published = await SendAsync<AgentProfilePublishResultDto>(
            manager,
            HttpMethod.Post,
            $"/api/v1/agents/{created.Id}/versions/{draft.Id}/actions/publish",
            new PublishAgentProfileVersionRequest(draft.Revision, "低风险发布"),
            publishKey);
        published.Status.ShouldBe("Published");
        published.Profile.ShouldNotBeNull();
        var publishedProfile = published.Profile!;

        var readOnlyContext = await GetAsync<AgentProfileContextDto>(
            researcher,
            $"/api/v1/agents/{created.Id}/context");
        readOnlyContext.Profile.Id.ShouldBe(created.Id);
        readOnlyContext.CanViewMcpEvidence.ShouldBeFalse();
        readOnlyContext.CanViewAuditTimeline.ShouldBeFalse();

        var replay = await SendAsync<AgentProfilePublishResultDto>(
            manager,
            HttpMethod.Post,
            $"/api/v1/agents/{created.Id}/versions/{draft.Id}/actions/publish",
            new PublishAgentProfileVersionRequest(draft.Revision, "低风险发布"),
            publishKey);
        replay.Status.ShouldBe("Published");
        replay.Profile!.Id.ShouldBe(publishedProfile.Id);

        var nextDraft = await SendAsync<AgentProfileVersionDto>(
            manager,
            HttpMethod.Post,
            $"/api/v1/agents/{created.Id}/drafts",
            new CreateNextAgentProfileDraftRequest(publishedProfile.Version, "创建第二版"),
            "agent-next-draft-" + Guid.NewGuid().ToString("N"),
            HttpStatusCode.Created);
        var editedDraft = await SendAsync<AgentProfileVersionDto>(
            manager,
            HttpMethod.Put,
            $"/api/v1/agents/{created.Id}/drafts/{nextDraft.Id}",
            new UpdateAgentProfileDraftRequest(
                nextDraft.Revision,
                "6A 自动化数字员工 V2",
                nextDraft.Description,
                Guid.Parse(nextDraft.ServiceUserId),
                nextDraft.AutonomyLevel,
                nextDraft.RiskLimit,
                nextDraft.AmountLimit,
                nextDraft.DataScopes,
                nextDraft.ModelScopes,
                nextDraft.AllowedTools,
                nextDraft.RequiredConfirmations,
                nextDraft.StopConditions),
            null);
        editedDraft.Name.ShouldBe("6A 自动化数字员工 V2");
        await SendAsync<AgentProfileValidationDto>(
            manager,
            HttpMethod.Post,
            $"/api/v1/agents/{created.Id}/versions/{editedDraft.Id}/actions/validate",
            new ValidateAgentProfileVersionRequest(editedDraft.Revision),
            "agent-v2-validate-" + Guid.NewGuid().ToString("N"));
        var versionsAfterValidation = await GetAsync<IReadOnlyList<AgentProfileVersionDto>>(
            manager,
            $"/api/v1/agents/{created.Id}/versions");
        var validatedV2 = versionsAfterValidation.Single(item => item.Id == editedDraft.Id);
        var publishedV2 = await SendAsync<AgentProfilePublishResultDto>(
            manager,
            HttpMethod.Post,
            $"/api/v1/agents/{created.Id}/versions/{validatedV2.Id}/actions/publish",
            new PublishAgentProfileVersionRequest(validatedV2.Revision, "发布第二版"),
            "agent-v2-publish-" + Guid.NewGuid().ToString("N"));
        publishedV2.Status.ShouldBe("Published");
        publishedV2.Profile!.CurrentPublishedVersionNumber.ShouldBe(2);
        var immutablePublished = await SendForStatusAsync(
            manager,
            HttpMethod.Put,
            $"/api/v1/agents/{created.Id}/drafts/{draft.Id}",
            UpdateRequest(draft, draft.Revision, ["workflow.read"]),
            null);
        immutablePublished.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var rollback = await SendAsync<AgentProfilePublishResultDto>(
            manager,
            HttpMethod.Post,
            $"/api/v1/agents/{created.Id}/actions/rollback",
            new RollbackAgentProfileRequest(
                publishedV2.Profile.Version,
                Guid.Parse(draft.Id),
                "回滚到第一版"),
            "agent-rollback-" + Guid.NewGuid().ToString("N"));
        rollback.Status.ShouldBe("Published");
        rollback.Profile!.CurrentPublishedVersionNumber.ShouldBe(3);
        publishedProfile = rollback.Profile;

        var history = await GetAsync<IReadOnlyList<AgentProfileVersionDto>>(
            manager,
            $"/api/v1/agents/{created.Id}/versions");
        history.Count.ShouldBe(3);
        history.ShouldContain(item => item.VersionNumber == 1 && item.Status == AgentProfileVersionStatus.Retired);
        history.ShouldContain(item => item.VersionNumber == 2 && item.Status == AgentProfileVersionStatus.Retired);
        history.ShouldContain(item => item.VersionNumber == 3 && item.Status == AgentProfileVersionStatus.Published);

        var run = await SendAsync<AgentRunDto>(
            admin,
            HttpMethod.Post,
            "/api/v1/agents/runs",
            new CreateAgentRunRequest(
                Guid.Parse(publishedProfile.Id),
                AgentActionType.Read,
                0,
                AgentRiskLevel.Low,
                "aidt-rule-sim",
                "workflow.read"),
            "agent-run-" + Guid.NewGuid().ToString("N"),
            HttpStatusCode.Created);
        run.AgentProfileId.ShouldBe(publishedProfile.Id);

        var token = await SendAsync<McpServiceTokenDto>(
            admin,
            HttpMethod.Post,
            "/api/v1/mcp/service-tokens",
            new McpServiceTokenRequest(
                Guid.Parse(publishedProfile.ServiceUserId),
                "6A 生命周期验收令牌",
                [McpScopeNames.Read],
                [Guid.Parse(publishedProfile.DepartmentId)],
                1,
                Guid.Parse(publishedProfile.Id)),
            null,
            HttpStatusCode.Created);
        token.AccessToken.ShouldNotBeNullOrWhiteSpace();
        var populatedContext = await GetAsync<AgentProfileContextDto>(
            manager,
            $"/api/v1/agents/{created.Id}/context");
        populatedContext.CanViewMcpEvidence.ShouldBeTrue();
        populatedContext.CanViewAuditTimeline.ShouldBeTrue();
        populatedContext.RecentRuns.ShouldContain(item =>
            item.Id == run.Id && item.AgentProfileName == publishedProfile.Name);
        populatedContext.McpTokens.ShouldContain(item => item.Id == token.TokenId);
        populatedContext.AuditTimeline.ShouldContain(item =>
            item.Action == "agent.profile.published");

        await SendAsync<AgentProfileDetailDto>(
            manager,
            HttpMethod.Post,
            $"/api/v1/agents/{created.Id}/actions/disable",
            new DisableAgentProfileRequest(publishedProfile.Version, "6A 停用验收"),
            "agent-disable-" + Guid.NewGuid().ToString("N"));

        var disabledRun = await SendForStatusAsync(
            admin,
            HttpMethod.Post,
            "/api/v1/agents/runs",
            new CreateAgentRunRequest(
                Guid.Parse(publishedProfile.Id),
                AgentActionType.Read,
                0,
                AgentRiskLevel.Low,
                "aidt-rule-sim",
                "workflow.read"),
            "agent-run-disabled-" + Guid.NewGuid().ToString("N"));
        disabledRun.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await disabledRun.Content.ReadFromJsonAsync<ApiResponse<object?>>(JsonOptions))!
            .Code.ShouldBe("AGENT_STOPPED");

        using var reviewer = await LoginAsync("demo.security");
        var disabledContext = await GetAsync<AgentProfileContextDto>(
            reviewer,
            $"/api/v1/agents/{created.Id}/context");
        disabledContext.Profile.LifecycleStatus.ShouldBe(AgentProfileLifecycleStatus.Disabled);
        disabledContext.RecentRuns.ShouldContain(item => item.Id == run.Id);
        disabledContext.McpTokens.ShouldContain(item =>
            item.Id == token.TokenId && item.RevokedAt.HasValue);

        (await ReadRevokedTokenStateAsync(token.TokenId)).ShouldBeTrue();
        var deniedToken = await SendForStatusAsync(
            admin,
            HttpMethod.Post,
            "/api/v1/mcp/service-tokens",
            new McpServiceTokenRequest(
                Guid.Parse(publishedProfile.ServiceUserId),
                "停用后不应签发",
                [McpScopeNames.Read],
                [Guid.Parse(publishedProfile.DepartmentId)],
                1,
                Guid.Parse(publishedProfile.Id)),
            null);
        deniedToken.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var runs = await GetAsync<PagedResult<AgentRunDto>>(
            admin,
            "/api/v1/agents/runs?pageSize=100");
        runs.Items.ShouldContain(item => item.Id == run.Id);

        using var serviceLoginClient = _factory.CreateClient();
        var csrf = await GetCsrfTokenAsync(serviceLoginClient);
        using var serviceLogin = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/demo-login")
        {
            Content = JsonContent.Create(new DemoLoginRequest("agent.lowrisk"))
        };
        serviceLogin.Headers.Add("X-CSRF-TOKEN", csrf);
        (await serviceLoginClient.SendAsync(serviceLogin)).StatusCode.ShouldBe(
            HttpStatusCode.Forbidden);
    }

    [SqlIntegrationFact]
    public async Task HighRiskPublish_RequiresSecondPlatformAdminAndRejectsSelfReview()
    {
        using var admin = await LoginAsync("demo.admin");
        using var security = await LoginAsync("demo.security");
        var departments = await GetAsync<IReadOnlyList<DepartmentDto>>(
            admin,
            "/api/v1/departments");
        var rnd = departments.Single(item => item.Code == "RND");
        var code = $"AGT-HIGH-{Guid.NewGuid():N}"[..25].ToUpperInvariant();
        var created = await SendAsync<AgentProfileDetailDto>(
            admin,
            HttpMethod.Post,
            "/api/v1/agents/drafts",
            CreateDraftRequest(
                code,
                HighRiskServiceUserId,
                Guid.Parse(rnd.Id),
                AgentAutonomyLevel.A3,
                AgentRiskLevel.High,
                10_000m,
                ["workflow.proxy-low-risk"]),
            "agent-high-create-" + Guid.NewGuid().ToString("N"),
            HttpStatusCode.Created);
        var versions = await GetAsync<IReadOnlyList<AgentProfileVersionDto>>(
            admin,
            $"/api/v1/agents/{created.Id}/versions");
        var draft = versions.Single();
        await SendAsync<AgentProfileValidationDto>(
            admin,
            HttpMethod.Post,
            $"/api/v1/agents/{created.Id}/versions/{draft.Id}/actions/validate",
            new ValidateAgentProfileVersionRequest(draft.Revision),
            "agent-high-validate-" + Guid.NewGuid().ToString("N"));
        versions = await GetAsync<IReadOnlyList<AgentProfileVersionDto>>(
            admin,
            $"/api/v1/agents/{created.Id}/versions");
        draft = versions.Single();
        var submitted = await SendAsync<AgentProfilePublishResultDto>(
            admin,
            HttpMethod.Post,
            $"/api/v1/agents/{created.Id}/versions/{draft.Id}/actions/publish",
            new PublishAgentProfileVersionRequest(draft.Revision, "高风险能力变更"),
            "agent-high-publish-" + Guid.NewGuid().ToString("N"),
            HttpStatusCode.Accepted);
        submitted.Status.ShouldBe("AwaitingApproval");
        submitted.ChangeRequestId.ShouldNotBeNullOrWhiteSpace();

        var requests = await GetAsync<IReadOnlyList<AgentProfileChangeRequestDto>>(
            security,
            "/api/v1/agents/change-requests?status=Pending");
        var request = requests.Single(item => item.Id == submitted.ChangeRequestId);
        request.RequestedBy.ShouldBe(AdminUserId.ToString("D"));

        var selfReview = await SendForStatusAsync(
            admin,
            HttpMethod.Post,
            $"/api/v1/agents/change-requests/{request.Id}/actions/review",
            new ReviewAgentProfileChangeRequest(true, "申请人自审应失败", draft.Revision),
            "agent-self-review-" + Guid.NewGuid().ToString("N"));
        selfReview.StatusCode.ShouldBe(
            HttpStatusCode.Forbidden,
            await selfReview.Content.ReadAsStringAsync());
        var selfReviewPayload =
            (await selfReview.Content.ReadFromJsonAsync<ApiResponse<object?>>(JsonOptions))!;
        selfReviewPayload.Code.ShouldBe("AGENT_SELF_REVIEW_DENIED");
        (await CountDeniedAuditAsync(selfReviewPayload.TraceId)).ShouldBe(1);

        var approved = await SendAsync<AgentProfilePublishResultDto>(
            security,
            HttpMethod.Post,
            $"/api/v1/agents/change-requests/{request.Id}/actions/review",
            new ReviewAgentProfileChangeRequest(true, "第二位平台管理员审核通过", draft.Revision),
            "agent-high-review-" + Guid.NewGuid().ToString("N"));
        approved.Status.ShouldBe("Published");
        approved.Profile!.LifecycleStatus.ShouldBe(AgentProfileLifecycleStatus.Published);
    }

    [SqlIntegrationFact]
    public async Task OrdinaryBusinessRoles_CanReadEffectiveIdentitiesButCannotMutate()
    {
        foreach (var userName in new[]
                 {
                     "demo.supply",
                     "demo.finance",
                     "demo.production"
                 })
        {
            using var reader = await LoginAsync(userName);
            var profiles = await GetAsync<PagedResult<AgentProfileDetailDto>>(
                reader,
                "/api/v1/agents/manage?pageSize=100");
            profiles.Items.ShouldAllBe(item =>
                item.LifecycleStatus == AgentProfileLifecycleStatus.Published);

            var options = await GetAsync<AgentIdentityOptionsDto>(
                reader,
                "/api/v1/agents/options");
            options.Capabilities.ShouldNotBeEmpty();
            options.ServiceIdentities.ShouldBeEmpty();
            var departmentId = options.Departments.Count > 0
                ? options.Departments[0].Id
                : Guid.NewGuid().ToString("D");

            var denied = await SendForStatusAsync(
                reader,
                HttpMethod.Post,
                "/api/v1/agents/drafts",
                CreateDraftRequest(
                    $"AGT-READONLY-{Guid.NewGuid():N}"[..26].ToUpperInvariant(),
                    LowRiskServiceUserId,
                    Guid.Parse(departmentId),
                    AgentAutonomyLevel.A2,
                    AgentRiskLevel.Low,
                    0,
                    ["workflow.read"]),
                "agent-readonly-" + Guid.NewGuid().ToString("N"));
            denied.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }
    }

    private async Task<AuthenticatedApiClient> LoginAsync(string userName)
    {
        var client = _factory.CreateClient();
        var anonymousToken = await GetCsrfTokenAsync(client);
        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/demo-login")
        {
            Content = JsonContent.Create(new DemoLoginRequest(userName))
        };
        login.Headers.Add("X-CSRF-TOKEN", anonymousToken);
        (await client.SendAsync(login)).StatusCode.ShouldBe(HttpStatusCode.OK);
        return new AuthenticatedApiClient(client, await GetCsrfTokenAsync(client));
    }

    private static async Task<T> GetAsync<T>(AuthenticatedApiClient client, string path)
    {
        var response = await client.Client.GetAsync(path);
        response.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
        payload.ShouldNotBeNull();
        return payload!.Data!;
    }

    private static Task<T> SendAsync<T>(
        AuthenticatedApiClient client,
        HttpMethod method,
        string path,
        object body,
        string? idempotencyKey,
        HttpStatusCode expected = HttpStatusCode.OK) =>
        SendInternalAsync<T>(client, method, path, body, idempotencyKey, expected);

    private static async Task<T> SendInternalAsync<T>(
        AuthenticatedApiClient client,
        HttpMethod method,
        string path,
        object body,
        string? idempotencyKey,
        HttpStatusCode expected)
    {
        using var request = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        var response = await client.Client.SendAsync(request);
        response.StatusCode.ShouldBe(expected, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
        payload.ShouldNotBeNull();
        return payload!.Data!;
    }

    private static async Task<HttpResponseMessage> SendForStatusAsync(
        AuthenticatedApiClient client,
        HttpMethod method,
        string path,
        object body,
        string? idempotencyKey)
    {
        var request = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        return await client.Client.SendAsync(request);
    }

    private static CreateAgentProfileDraftRequest CreateDraftRequest(
        string code,
        Guid serviceUserId,
        Guid departmentId,
        AgentAutonomyLevel autonomy,
        AgentRiskLevel risk,
        decimal amount,
        IReadOnlyList<string> tools) =>
        new(
            code,
            "6A 自动化数字员工",
            "验证身份生命周期、权限边界和令牌撤销",
            serviceUserId,
            departmentId,
            AdminUserId,
            AdminUserId,
            autonomy,
            risk,
            amount,
            [$"department:{departmentId:D}"],
            ["aidt-rule-sim"],
            tools,
            [],
            ["manual-stop", "risk-limit", "amount-limit"]);

    private static UpdateAgentProfileDraftRequest UpdateRequest(
        AgentProfileVersionDto version,
        long expectedRevision,
        IReadOnlyList<string> tools) =>
        new(
            expectedRevision,
            version.Name,
            version.Description,
            Guid.Parse(version.ServiceUserId),
            version.AutonomyLevel,
            version.RiskLimit,
            version.AmountLimit,
            version.DataScopes,
            version.ModelScopes,
            tools,
            version.RequiredConfirmations,
            version.StopConditions);

    private static async Task<bool> ReadRevokedTokenStateAsync(Guid tokenId)
    {
        var connectionString = Environment.GetEnvironmentVariable("AIDT_SQL_CONNECTION")
            ?? throw new InvalidOperationException("AIDT_SQL_CONNECTION is required.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT CASE
                WHEN RevokedAt IS NOT NULL
                 AND RevokedBy IS NOT NULL
                 AND RevocationReason LIKE N'AGENT_DISABLED:%'
                THEN 1 ELSE 0 END
            FROM [Mcp].[AccessToken]
            WHERE Id=@TokenId;
            """;
        command.Parameters.AddWithValue("@TokenId", tokenId);
        return (int)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Token state is missing.")) == 1;
    }

    private static async Task<int> CountDeniedAuditAsync(string traceId)
    {
        var connectionString = Environment.GetEnvironmentVariable("AIDT_SQL_CONNECTION")
            ?? throw new InvalidOperationException("AIDT_SQL_CONNECTION is required.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(*)
            FROM [Audit].[AuditEvent]
            WHERE [Module]=N'Security'
              AND [Action]=N'request.denied'
              AND [TraceId]=@TraceId;
            """;
        command.Parameters.AddWithValue("@TraceId", traceId);
        return (int)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Audit count is missing."));
    }

    private static async Task CleanTestProfilesAsync()
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
            SELECT Id FROM [Agent].[AgentProfile]
            WHERE ServiceUserId IN (
                '10000000-0000-0000-0000-000000000028',
                '10000000-0000-0000-0000-000000000029')
              OR Code LIKE N'AGT-6A-%'
              OR Code LIKE N'AGT-HIGH-%';

            DELETE FROM [Mcp].[AccessToken] WHERE AgentProfileId IN (SELECT Id FROM @Ids);
            DELETE FROM [Mcp].[Client] WHERE AgentProfileId IN (SELECT Id FROM @Ids);
            UPDATE [Workflow].[WorkflowNodePolicy]
            SET AgentProfileId = NULL
            WHERE AgentProfileId IN (SELECT Id FROM @Ids);
            DELETE FROM [Agent].[AgentRun] WHERE AgentProfileId IN (SELECT Id FROM @Ids);
            DELETE FROM [Identity].[AccessChangeRequest]
            WHERE RequestType = N'AgentProfilePublish'
              AND JSON_VALUE(PayloadJson, '$.agentProfileId') IN (
                  SELECT CONVERT(nvarchar(36), Id) FROM @Ids);
            UPDATE [Agent].[AgentProfile]
            SET CurrentPublishedVersionId = NULL
            WHERE Id IN (SELECT Id FROM @Ids);
            DELETE FROM [Agent].[AgentProfileVersion] WHERE AgentProfileId IN (SELECT Id FROM @Ids);
            DELETE FROM [Agent].[AgentProfile] WHERE Id IN (SELECT Id FROM @Ids);
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        using var document = JsonDocument.Parse(await client.GetStringAsync("/api/v1/auth/csrf"));
        return document.RootElement
            .GetProperty("data")
            .GetProperty("requestToken")
            .GetString() ?? throw new InvalidOperationException("CSRF token missing.");
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private sealed record AuthenticatedApiClient(HttpClient Client, string CsrfToken) : IDisposable
    {
        public void Dispose() => Client.Dispose();
    }
}
