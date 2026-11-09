using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AIDT.Api.Contracts;
using AIDT.Application.Contracts;
using AIDT.Application.WorkItems;
using AIDT.Domain.Agents;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;

using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using AIDT.Application.Abstractions;
using AIDT.Infrastructure.Persistence;

namespace AIDT.UnitTests.Api;

public sealed class ApiContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions ClientJsonOptions = CreateClientJsonOptions();

    private readonly WebApplicationFactory<Program> _factory;

    public ApiContractTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new TestGatewayClientFactory());
            }));
    }

    [Fact]
    public async Task Liveness_DoesNotDependOnSqlServer()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/live");

        response.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            await response.Content.ReadAsStringAsync());
        response.Headers.Contains("X-Trace-Id").ShouldBeTrue();
    }

    [Fact]
    public async Task Readiness_ReflectsConfiguredSqlState()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/ready");
        var payload = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.ShouldContain("ready");
        payload.ShouldContain("applied_verified");
    }

    [Fact]
    public async Task Capabilities_ReflectConfiguredMigrationState()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/platform/capabilities");
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<PlatformPayload>>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.ShouldNotBeNull();
        var data = payload!.Data ?? throw new InvalidOperationException("capabilities data is null");
        data.ProjectState.ShouldBe("MCP_READY");
        data.DatabaseMigrationState.ShouldBe("applied_verified");
        data.DatabaseProvider.ShouldBe("Microsoft.EntityFrameworkCore.SqlServer");
        data.ExternalSystemsMode.ShouldBe("Mock");
        data.CurrentEnvironmentCode.ShouldBe("demo");
        data.MaxUploadBytes.ShouldBe(50 * 1024 * 1024);
        data.Capabilities.Single(item => item.FeatureId == "UX-001").State.ShouldBe("available");
        data.Capabilities.Single(item => item.FeatureId == "UX-003").State.ShouldBe("available");
        data.Capabilities.Single(item => item.FeatureId == "UX-004").State.ShouldBe("available");
        data.Capabilities.Single(item => item.FeatureId == "DATA-001").State.ShouldBe("available");
        data.Capabilities.Single(item => item.FeatureId == "PMO-001").State.ShouldBe("available");
        data.Capabilities.Single(item => item.FeatureId == "PMO-003").State.ShouldBe("available");
        data.Capabilities.Single(item => item.FeatureId == "EVT-001").State.ShouldBe("available");
        data.Capabilities.Single(item => item.FeatureId == "CFG-001").State.ShouldBe("available");
        data.Capabilities.Single(item => item.FeatureId == "NAV-001").State.ShouldBe("available");
        data.Capabilities.Single(item => item.FeatureId == "IAM-001").State.ShouldBe("available");
        data.Capabilities.Single(item => item.FeatureId == "WFROLE-001").State.ShouldBe("available");
        data.Capabilities.Single(item => item.FeatureId == "META-001").State.ShouldBe("available");
    }

    [SqlIntegrationFact]
    public async Task McpManagement_IsAdminOnlyAndReturnsControlledToolCatalog()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var snapshot = await admin.Client.GetAsync("/api/v1/mcp/snapshot?pageSize=100");
        snapshot.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await snapshot.Content.ReadAsStringAsync();
        body.ShouldContain("aidt_whoami");
        body.ShouldContain("aidt_confirm_event");
        body.ShouldContain("aidt_decide_workflow_task");

        using var researcher = await CreateAuthenticatedClientAsync("demo.researcher");
        var denied = await researcher.Client.GetAsync("/api/v1/mcp/snapshot");
        denied.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var deniedPayload = await denied.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        deniedPayload!.Code.ShouldBe("PERMISSION_DENIED");
    }

    [Fact]
    public async Task ObjectQuery_WithoutAuthentication_IsRejected()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/object-types");
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>();

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        payload!.Code.ShouldBe("AUTHENTICATION_REQUIRED");
    }

    [Fact]
    public async Task EmployeeWorkspaceEndpoints_WithoutAuthentication_AreRejected()
    {
        using var client = _factory.CreateClient();
        foreach (var path in new[]
                 {
                     "/api/v1/my-work",
                     "/api/v1/departments",
                     "/api/v1/files",
                     $"/api/v1/files/{Guid.NewGuid():D}"
                 })
        {
            var response = await client.GetAsync(path);
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task FileUpload_WithoutAuthentication_IsRejectedBeforeFormBinding()
    {
        using var client = _factory.CreateClient();
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent([1, 2, 3]), "file", "evidence.pdf");
        content.Add(new StringContent(Guid.NewGuid().ToString("D")), "departmentId");
        content.Add(new StringContent("Internal"), "sensitivity");

        var response = await client.PostAsync("/api/v1/files", content);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [SqlIntegrationFact]
    public async Task AuthenticatedAdmin_CanReadSeededObjectTypesAndConnectors()
    {
        using var authenticated = await CreateAuthenticatedClientAsync("demo.admin");
        var objectTypes = await authenticated.Client.GetFromJsonAsync<ApiResponse<PagedResult<ObjectTypeDto>>>(
            "/api/v1/object-types");
        var connectors = await authenticated.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<ConnectorDto>>>(
            "/api/v1/connectors");

        objectTypes.ShouldNotBeNull();
        objectTypes!.Data.ShouldNotBeNull();
        objectTypes.Data!.Total.ShouldBeGreaterThanOrEqualTo(4);
        connectors.ShouldNotBeNull();
        connectors!.Data.ShouldNotBeNull();
        connectors.Data!.ShouldContain(item => item.ConnectionStatus == "Mock");
        connectors.Data.ShouldContain(item => item.ConnectionStatus == "Disconnected");

        var connector = connectors.Data.First(item => item.ConnectionStatus == "Mock");
        var outbox = await authenticated.Client.GetFromJsonAsync<ApiResponse<PagedResult<OutboxMessageDto>>>(
            $"/api/v1/connectors/{connector.Id}/outbox?pageSize=100");
        outbox.ShouldNotBeNull();
        outbox!.Data.ShouldNotBeNull();
        outbox.Data!.Total.ShouldBeGreaterThan(0);
        outbox.Data.Items.ShouldContain(item =>
            item.Status == "Delivered" || item.Status == "Pending");
    }

    [SqlIntegrationFact]
    public async Task RoleWithoutConnectorPermission_IsForbiddenByApi()
    {
        using var authenticated = await CreateAuthenticatedClientAsync("demo.researcher");
        var response = await authenticated.Client.GetAsync("/api/v1/connectors");
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>();

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        payload!.Code.ShouldBe("PERMISSION_DENIED");
    }

    [SqlIntegrationFact]
    public async Task UserWithoutOrganizationScope_IsDataScopeDenied()
    {
        using var authenticated = await CreateAuthenticatedClientAsync("demo.noscope");
        var response = await authenticated.Client.GetAsync("/api/v1/business-objects");
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>();

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        payload!.Code.ShouldBe("DATA_SCOPE_DENIED");
    }

    [SqlIntegrationFact]
    public async Task CoreField_IsMaskedForNonAdminIdentity()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var types = await admin.Client.GetFromJsonAsync<ApiResponse<PagedResult<ObjectTypeDto>>>(
            "/api/v1/object-types?pageSize=100");
        var sampleType = types!.Data!.Items.Single(item => item.Code == "CUSTOMER_SAMPLE");
        var objectCode = $"MASK-{Guid.NewGuid():N}"[..18].ToUpperInvariant();
        var createRequest = new CreateBusinessObjectRequest(
            Guid.Parse(sampleType.Id),
            objectCode,
            new Dictionary<string, string?>
            {
                ["Customer"] = "脱敏测试客户",
                ["Product"] = "脱敏测试产品",
                ["Requirement"] = "验证 Core 字段不可见",
                ["InternalFormulaReference"] = "FORMULA-SECRET-001"
            });
        using var createMessage = new HttpRequestMessage(HttpMethod.Post, "/api/v1/business-objects")
        {
            Content = JsonContent.Create(createRequest)
        };
        createMessage.Headers.Add("Idempotency-Key", $"mask-test-{Guid.NewGuid():N}");
        createMessage.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var createResponse = await admin.Client.SendAsync(createMessage);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<BusinessObjectDto>>();

        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        created!.Data!.Fields["InternalFormulaReference"].ShouldBe("FORMULA-SECRET-001");

        using var researcher = await CreateAuthenticatedClientAsync("demo.researcher");
        var businessObjects =
            await researcher.Client.GetFromJsonAsync<ApiResponse<PagedResult<BusinessObjectDto>>>(
                "/api/v1/business-objects?pageSize=100");
        var masked = businessObjects!.Data!.Items.Single(item => item.ObjectCode == objectCode);

        masked.Fields["InternalFormulaReference"].ShouldBe("***");
    }

    [SqlIntegrationFact]
    public async Task CreateBusinessObject_ReplaysSameIdempotencyKeyAndRejectsChangedPayload()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var types = await admin.Client.GetFromJsonAsync<ApiResponse<PagedResult<ObjectTypeDto>>>(
            "/api/v1/object-types?pageSize=100");
        var sampleType = types!.Data!.Items.Single(item => item.Code == "CUSTOMER_SAMPLE");
        var objectCode = $"IDEM-{Guid.NewGuid():N}"[..18].ToUpperInvariant();
        var key = $"idem-{Guid.NewGuid():N}";
        var request = new CreateBusinessObjectRequest(
            Guid.Parse(sampleType.Id),
            objectCode,
            new Dictionary<string, string?>
            {
                ["Customer"] = "幂等测试客户",
                ["Product"] = "幂等测试产品",
                ["Requirement"] = "验证幂等重放"
            });

        var first = await CreateObjectAsync(admin, key, request);
        var replayed = await CreateObjectAsync(admin, key, request);
        var changed = await CreateObjectAsync(
            admin,
            key,
            request with { ObjectCode = $"{objectCode}X" });

        first.StatusCode.ShouldBe(HttpStatusCode.Created);
        replayed.StatusCode.ShouldBe(HttpStatusCode.Created);
        var firstPayload = await first.Content.ReadFromJsonAsync<ApiResponse<BusinessObjectDto>>();
        var replayedPayload = await replayed.Content.ReadFromJsonAsync<ApiResponse<BusinessObjectDto>>();
        firstPayload!.Data!.Id.ShouldBe(replayedPayload!.Data!.Id);

        changed.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var changedPayload = await changed.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        changedPayload!.Code.ShouldBe("CONFLICT");
    }

    [SqlIntegrationFact]
    public async Task DepartmentScope_HidesOtherDepartmentObjectAndDirectId()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var departments = await admin.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<DepartmentDto>>>(
            "/api/v1/departments");
        var qaDepartment = departments!.Data!.Single(item => item.Code == "QA");
        var types = await admin.Client.GetFromJsonAsync<ApiResponse<PagedResult<ObjectTypeDto>>>(
            "/api/v1/object-types?pageSize=100");
        var qualityType = types!.Data!.Items.Single(item => item.Code == "QUALITY_DEVIATION");
        var objectCode = $"QA-SCOPE-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
        var createResponse = await CreateObjectAsync(
            admin,
            $"cross-dept-{Guid.NewGuid():N}",
            new CreateBusinessObjectRequest(
                Guid.Parse(qualityType.Id),
                objectCode,
                new Dictionary<string, string?>
                {
                    ["BatchNo"] = objectCode,
                    ["Alarm"] = "部门隔离测试",
                    ["Severity"] = "Minor"
                },
                Guid.Parse(qaDepartment.Id)));
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<BusinessObjectDto>>();
        var definitions = await admin.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<WorkflowDefinitionDto>>>(
            "/api/v1/workflow-definitions");
        var definition = definitions!.Data!.Single(item => item.ObjectTypeCode == "QUALITY_DEVIATION");
        using var startRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/workflow-instances")
        {
            Content = JsonContent.Create(
                new StartWorkflowInstanceRequest(
                    Guid.Parse(definition.Id),
                    Guid.Parse(definition.VersionId),
                    Guid.Parse(created!.Data!.Id),
                    $"cross-workflow-{Guid.NewGuid():N}"))
        };
        startRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var startResponse = await admin.Client.SendAsync(startRequest);
        startResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var started = await startResponse.Content.ReadFromJsonAsync<ApiResponse<WorkflowInstanceDto>>(
            ClientJsonOptions);

        using var researcher = await CreateAuthenticatedClientAsync("demo.researcher");
        var researcherObjects =
            await researcher.Client.GetFromJsonAsync<ApiResponse<PagedResult<BusinessObjectDto>>>(
                "/api/v1/business-objects?pageSize=100");
        researcherObjects!.Data!.Items.ShouldNotContain(item => item.ObjectCode == objectCode);

        var direct = await researcher.Client.GetAsync(
            $"/api/v1/business-objects/{created!.Data!.Id}");
        direct.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var hiddenWorkflow = await researcher.Client.GetAsync(
            $"/api/v1/workflow-instances/{started!.Data!.Id}");
        hiddenWorkflow.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var crossDepartmentWorkspace = await researcher.Client.GetAsync(
            $"/api/v1/departments/{qaDepartment.Id}/workspace");
        crossDepartmentWorkspace.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var objectAudits = await admin.Client.GetFromJsonAsync<ApiResponse<PagedResult<AuditEventDto>>>(
            $"/api/v1/audit-events?pageSize=100&entityType=BusinessObject&entityId={created.Data.Id}",
            ClientJsonOptions);
        objectAudits!.Data!.Items.ShouldContain(item =>
            item.Outcome == AIDT.Domain.Audit.AuditOutcome.Denied
            && item.Action == "ReadDenied");
        var workflowAudits = await admin.Client.GetFromJsonAsync<ApiResponse<PagedResult<AuditEventDto>>>(
            $"/api/v1/audit-events?pageSize=100&entityType=WorkflowInstance&entityId={started.Data.Id}",
            ClientJsonOptions);
        workflowAudits!.Data!.Items.ShouldContain(item =>
            item.Outcome == AIDT.Domain.Audit.AuditOutcome.Denied
            && item.Action == "ReadDenied");
        var departmentAudits = await admin.Client.GetFromJsonAsync<ApiResponse<PagedResult<AuditEventDto>>>(
            $"/api/v1/audit-events?pageSize=100&entityType=Department&entityId={qaDepartment.Id}",
            ClientJsonOptions);
        departmentAudits!.Data!.Items.ShouldContain(item =>
            item.Outcome == AIDT.Domain.Audit.AuditOutcome.Denied
            && item.Action == "ReadDenied");
    }

    [SqlIntegrationFact]
    public async Task DepartmentAndMembership_ArePersistedAndExposeDepartmentScope()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var code = $"E2E-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/departments")
        {
            Content = JsonContent.Create(new CreateDepartmentRequest(code, "E2E 部门", null))
        };
        createRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var createdResponse = await admin.Client.SendAsync(createRequest);
        createdResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var department =
            (await createdResponse.Content.ReadFromJsonAsync<ApiResponse<DepartmentDto>>())!.Data!;
        var qaUserId = await GetUserIdAsync("demo.qa");
        try
        {
            using var memberRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/v1/departments/{department.Id}/members")
            {
                Content = JsonContent.Create(
                    new AddDepartmentMemberRequest(qaUserId, IsPrimary: false, IsManager: true))
            };
            memberRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
            var memberResponse = await admin.Client.SendAsync(memberRequest);
            memberResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

            using var qa = await CreateAuthenticatedClientAsync("demo.qa");
            var departments = await qa.Client.GetFromJsonAsync<
                ApiResponse<IReadOnlyList<DepartmentDto>>>("/api/v1/departments");
            departments!.Data!.ShouldContain(item =>
                item.Id == department.Id
                && item.Name == "E2E 部门");
        }
        finally
        {
            await DeleteDepartmentAsync(Guid.Parse(department.Id));
        }
    }

    [SqlIntegrationFact]
    public async Task FileCenter_UploadPreviewVersionAndDeletionRequest_WorkEndToEnd()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var departmentId = await GetDepartmentIdAsync(admin, "RND");
        var types = await admin.Client.GetFromJsonAsync<ApiResponse<PagedResult<ObjectTypeDto>>>(
            "/api/v1/object-types?pageSize=100");
        var sampleType = types!.Data!.Items.Single(item => item.Code == "CUSTOMER_SAMPLE");
        var objectCode = $"FILE-OBJ-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
        var objectResponse = await CreateObjectAsync(
            admin,
            $"file-object-{Guid.NewGuid():N}",
            new CreateBusinessObjectRequest(
                Guid.Parse(sampleType.Id),
                objectCode,
                new Dictionary<string, string?>
                {
                    ["Customer"] = "文件关联测试",
                    ["Product"] = "AIDT 测试",
                    ["Requirement"] = "验证文件通用关联"
                },
                departmentId));
        objectResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var businessObject =
            (await objectResponse.Content.ReadFromJsonAsync<ApiResponse<BusinessObjectDto>>())!.Data!;
        var original = Encoding.ASCII.GetBytes("%PDF-1.7\nAIDT file v1");
        var uploaded = await UploadFileAsync(
            admin,
            departmentId,
            original,
            "batch1-evidence.pdf",
            "application/pdf",
            "Internal",
            Guid.Parse(businessObject.Id));
        uploaded.StatusCode.ShouldBe(HttpStatusCode.Created);
        var uploadedText = await uploaded.Content.ReadAsStringAsync();
        uploadedText.ShouldNotContain("storagePath");
        uploadedText.ShouldNotContain("C:\\\\");
        var file = JsonSerializer.Deserialize<ApiResponse<FileAttachmentDto>>(
            uploadedText,
            ClientJsonOptions)!.Data!;
        file.Sha256.ShouldBe(Convert.ToHexString(SHA256.HashData(original)));
        file.MimeType.ShouldBe("application/pdf");
        file.SizeBytes.ShouldBe(original.Length);
        file.PreviewKind.ShouldBe("pdf");
        file.CurrentVersionNumber.ShouldBe(1);
        file.Links.ShouldContain(link => link.EntityType == "Department");
        file.Links.ShouldContain(link =>
            link.EntityType == "BusinessObject" && link.EntityId == businessObject.Id);

        var detail = await admin.Client.GetFromJsonAsync<ApiResponse<FileAttachmentDto>>(
            $"/api/v1/files/{file.Id}?includeTestData=false");
        detail!.Data!.Id.ShouldBe(file.Id);
        var fileList = await admin.Client.GetFromJsonAsync<ApiResponse<PagedResult<FileAttachmentDto>>>(
            $"/api/v1/files?pageSize=100&includeTestData=false");
        fileList!.Data!.Items.ShouldContain(item => item.Id == file.Id);

        var download = await admin.Client.GetAsync($"/api/v1/files/{file.Id}/download?includeTestData=false");
        download.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await download.Content.ReadAsByteArrayAsync()).ShouldBe(original);

        var preview = await admin.Client.GetAsync($"/api/v1/files/{file.Id}/preview?includeTestData=false");
        preview.StatusCode.ShouldBe(HttpStatusCode.OK);
        preview.Content.Headers.ContentType!.MediaType.ShouldBe("application/pdf");

        var versionContent = Encoding.ASCII.GetBytes("%PDF-1.7\nAIDT file v2");
        var versionResponse = await UploadFileAsync(
            admin,
            departmentId,
            versionContent,
            "batch1-evidence-v2.pdf",
            "application/pdf",
            "Internal",
            attachmentId: file.Id);
        versionResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var versioned = (await versionResponse.Content.ReadFromJsonAsync<ApiResponse<FileAttachmentDto>>())!.Data!;
        versioned.CurrentVersionNumber.ShouldBe(2);

        var versions = await admin.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<FileVersionDto>>>(
            $"/api/v1/files/{file.Id}/versions?includeTestData=false");
        versions!.Data!.Count.ShouldBe(2);
        versions.Data.ShouldContain(item => item.VersionNumber == 1);
        versions.Data.ShouldContain(item => item.VersionNumber == 2);

        using var deleteRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/files/{file.Id}/actions/delete-request")
        {
            Content = JsonContent.Create(new RequestFileDeletionCommand("测试结束，申请保留证据并标记删除"))
        };
        deleteRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var deleted = await admin.Client.SendAsync(deleteRequest);
        deleted.StatusCode.ShouldBe(HttpStatusCode.OK);
        var deletedFile = (await deleted.Content.ReadFromJsonAsync<ApiResponse<FileAttachmentDto>>())!.Data!;
        deletedFile.Status.ShouldBe("DeletionRequested");
        deletedFile.DeletionReason.ShouldNotBeNullOrWhiteSpace();
    }

    [SqlIntegrationFact]
    public async Task SensitiveAndCrossDepartmentDownloads_AreDeniedAndAudited()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var rndDepartmentId = await GetDepartmentIdAsync(admin, "RND");
        var sensitiveResponse = await UploadFileAsync(
            admin,
            rndDepartmentId,
            Encoding.ASCII.GetBytes("%PDF-1.7\nsensitive"),
            "sensitive-evidence.pdf",
            "application/pdf",
            "Sensitive");
        var sensitive =
            (await sensitiveResponse.Content.ReadFromJsonAsync<ApiResponse<FileAttachmentDto>>())!.Data!;

        using var researcher = await CreateAuthenticatedClientAsync("demo.researcher");
        var sensitiveDownload = await researcher.Client.GetAsync(
            $"/api/v1/files/{sensitive.Id}/download?includeTestData=false");
        sensitiveDownload.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var sensitiveError = await sensitiveDownload.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        sensitiveError!.Code.ShouldBe("FILE_SCAN_REQUIRED");

        var internalResponse = await UploadFileAsync(
            admin,
            rndDepartmentId,
            Encoding.ASCII.GetBytes("%PDF-1.7\ninternal"),
            "rnd-internal.pdf",
            "application/pdf",
            "Internal");
        var internalFile =
            (await internalResponse.Content.ReadFromJsonAsync<ApiResponse<FileAttachmentDto>>())!.Data!;
        using var qa = await CreateAuthenticatedClientAsync("demo.qa");
        var crossDepartment = await qa.Client.GetAsync(
            $"/api/v1/files/{internalFile.Id}/download?includeTestData=false");
        crossDepartment.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var crossDepartmentError = await crossDepartment.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        crossDepartmentError!.Code.ShouldBe("DEPARTMENT_SCOPE_DENIED");

        var audits = await admin.Client.GetFromJsonAsync<ApiResponse<PagedResult<AuditEventDto>>>(
            $"/api/v1/audit-events?pageSize=100&entityType=FileAttachment&entityId={internalFile.Id}",
            ClientJsonOptions);
        audits!.Data!.Items.ShouldContain(item =>
            item.Outcome == AIDT.Domain.Audit.AuditOutcome.Denied
            && item.Action == "Download");
    }

    [SqlIntegrationFact]
    public async Task FileCenter_CanLinkWorkflowInstanceAndTask()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var instances = await admin.Client.GetFromJsonAsync<ApiResponse<PagedResult<WorkflowInstanceDto>>>(
            "/api/v1/workflow-instances?pageSize=100&includeTestData=false",
            ClientJsonOptions);
        var tasks = await admin.Client.GetFromJsonAsync<ApiResponse<PagedResult<WorkflowTaskDto>>>(
            "/api/v1/workflow-tasks?pageSize=100&includeTestData=false",
            ClientJsonOptions);
        var instance = instances!.Data!.Items[0];
        var task = tasks!.Data!.Items.First(item =>
            item.WorkflowInstanceId == instance.Id);
        var uploaded = await UploadFileAsync(
            admin,
            Guid.Parse(task.DepartmentId),
            Encoding.ASCII.GetBytes("%PDF-1.7\nworkflow-linked"),
            "workflow-linked.pdf",
            "application/pdf",
            "Internal",
            Guid.Parse(instance.BusinessObjectId),
            Guid.Parse(instance.Id),
            Guid.Parse(task.Id));
        uploaded.StatusCode.ShouldBe(HttpStatusCode.Created);
        var file = (await uploaded.Content.ReadFromJsonAsync<ApiResponse<FileAttachmentDto>>())!.Data!;
        file.Links.ShouldContain(link =>
            link.EntityType == "WorkflowInstance" && link.EntityId == instance.Id);
        file.Links.ShouldContain(link =>
            link.EntityType == "WorkflowTask" && link.EntityId == task.Id);
    }

    [SqlIntegrationFact]
    public async Task TestEnvironmentData_IsHiddenByDefaultAndVisibleToAdminOnRequest()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var departmentId = await GetDepartmentIdAsync(admin, "RND");
        var marker = $"ENV-{Guid.NewGuid():N}.txt";
        var testRows = await InsertTestEnvironmentDataAsync(departmentId, marker);
        try
        {
            var adminDefault = await admin.Client.GetFromJsonAsync<ApiResponse<PagedResult<FileAttachmentDto>>>(
                "/api/v1/files?pageSize=100&includeTestData=false");
            adminDefault!.Data!.Items.ShouldNotContain(item => item.OriginalFileName == marker);
            var adminDefaultObjects =
                await admin.Client.GetFromJsonAsync<ApiResponse<PagedResult<BusinessObjectDto>>>(
                    "/api/v1/business-objects?pageSize=100&includeTestData=false");
            adminDefaultObjects!.Data!.Items.ShouldNotContain(item =>
                item.ObjectCode == testRows.ObjectCode);
            var adminDefaultWorkflows =
                await admin.Client.GetFromJsonAsync<ApiResponse<PagedResult<WorkflowInstanceDto>>>(
                    "/api/v1/workflow-instances?pageSize=100&includeTestData=false",
                    ClientJsonOptions);
            adminDefaultWorkflows!.Data!.Items.ShouldNotContain(item =>
                item.Id == testRows.InstanceId.ToString("D"));

            var adminCrossEnvironment =
                await admin.Client.GetFromJsonAsync<ApiResponse<PagedResult<FileAttachmentDto>>>(
                    "/api/v1/files?pageSize=100&environmentCode=test&includeTestData=true");
            adminCrossEnvironment!.Data!.Items.ShouldContain(item =>
                item.Id == testRows.AttachmentId.ToString("D")
                && item.EnvironmentCode == "test");
            var adminCrossEnvironmentObjects =
                await admin.Client.GetFromJsonAsync<ApiResponse<PagedResult<BusinessObjectDto>>>(
                    "/api/v1/business-objects?pageSize=100&includeTestData=true");
            adminCrossEnvironmentObjects!.Data!.Items.ShouldContain(item =>
                item.Id == testRows.ObjectId.ToString("D")
                && item.EnvironmentCode == "test");
            var adminCrossEnvironmentWorkflows =
                await admin.Client.GetFromJsonAsync<ApiResponse<PagedResult<WorkflowInstanceDto>>>(
                    "/api/v1/workflow-instances?pageSize=100&includeTestData=true",
                    ClientJsonOptions);
            adminCrossEnvironmentWorkflows!.Data!.Items.ShouldContain(item =>
                item.Id == testRows.InstanceId.ToString("D")
                && item.EnvironmentCode == "test");

            using var researcher = await CreateAuthenticatedClientAsync("demo.researcher");
            var employeeDefault =
                await researcher.Client.GetFromJsonAsync<ApiResponse<PagedResult<FileAttachmentDto>>>(
                    "/api/v1/files?pageSize=100&includeTestData=false");
            employeeDefault!.Data!.Items.ShouldNotContain(item => item.OriginalFileName == marker);

            var forbiddenEnvironment = await researcher.Client.GetAsync(
                "/api/v1/files?environmentCode=test&includeTestData=true");
            forbiddenEnvironment.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

            var hiddenObject = await researcher.Client.GetAsync(
                $"/api/v1/business-objects/{testRows.ObjectId}");
            hiddenObject.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            var hiddenWorkflow = await researcher.Client.GetAsync(
                $"/api/v1/workflow-instances/{testRows.InstanceId}");
            hiddenWorkflow.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
        finally
        {
            await DeleteTestEnvironmentDataAsync(testRows);
        }
    }

    [SqlIntegrationFact]
    public async Task Minutes_RequireHumanConfirmationAndCreateTraceableIdempotentTask()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var departmentId = await GetDepartmentIdAsync(admin, "RND");
        var profiles = await admin.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<AgentProfileDto>>>(
            "/api/v1/agents",
            ClientJsonOptions);
        var profile = profiles!.Data!.Single(item => item.Name.Contains("业务协同"));
        var transcript =
            "会议：样品周会\n" +
            "[09:31:05] 研发工程师：请质量工程师在 2026-11-02 完成样品测试确认。\n" +
            "[09:33:12] 质量工程师：需要研发整理测试原始数据并提交。";
        var registerKey = $"minutes-register-{Guid.NewGuid():N}";
        using var registerRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/minutes")
        {
            Content = JsonContent.Create(new RegisterMinutesRequest(
                departmentId,
                "批次2听记测试",
                DateTimeOffset.UtcNow,
                transcript))
        };
        registerRequest.Headers.Add("Idempotency-Key", registerKey);
        registerRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var registerResponse = await admin.Client.SendAsync(registerRequest);
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var source = (await registerResponse.Content.ReadFromJsonAsync<ApiResponse<MinutesSourceDto>>())!.Data!;
        source.Transcript.ShouldContain("样品测试确认");

        using var analyzeRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/minutes/{source.Id}/actions/analyze")
        {
            Content = JsonContent.Create(new AnalyzeMinutesRequest(Guid.Parse(profile.Id)))
        };
        analyzeRequest.Headers.Add("Idempotency-Key", $"minutes-analyze-{Guid.NewGuid():N}");
        analyzeRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var analyzeResponse = await admin.Client.SendAsync(analyzeRequest);
        analyzeResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var run = (await analyzeResponse.Content.ReadFromJsonAsync<ApiResponse<AgentRunDto>>(
            ClientJsonOptions))!.Data!;
        run.Status.ShouldBe(AgentRunStatus.AwaitingConfirmation);

        var beforeConfirmation = await admin.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<MinutesActionItemDto>>>(
            $"/api/v1/minutes/{source.Id}/action-items");
        beforeConfirmation!.Data!.ShouldBeEmpty();

        using var confirmRunRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/agents/runs/{run.Id}/actions/confirm")
        {
            Content = JsonContent.Create(new ConfirmAgentRunRequest(Guid.NewGuid()))
        };
        confirmRunRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var confirmedRun = await admin.Client.SendAsync(confirmRunRequest);
        confirmedRun.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var executeRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/agents/runs/{run.Id}/actions/execute");
        executeRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var executeResponse = await admin.Client.SendAsync(executeRequest);
        executeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var execution = (await executeResponse.Content.ReadFromJsonAsync<
            ApiResponse<AgentExecutionResultDto>>(ClientJsonOptions))!.Data!;
        execution.Call.Status.ShouldBe("Succeeded");
        execution.Call.Mode.ShouldBe("RuleSimulator");
        execution.Call.ExternalModelCalled.ShouldBeFalse();
        execution.ActionItems.Count.ShouldBeGreaterThanOrEqualTo(2);

        var first = execution.ActionItems[0] with { Content = "人工校对后的行动项" };
        var adminUserId = await GetUserIdAsync("demo.admin");
        var confirmedDueAt = new DateTimeOffset(2026, 11, 8, 18, 0, 0, TimeSpan.FromHours(8));
        using var updateRequest = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/v1/minutes/{source.Id}/action-items/{first.Id}")
        {
            Content = JsonContent.Create(new UpdateMinutesActionItemRequest(
                adminUserId,
                confirmedDueAt,
                first.Content,
                "P0",
                null,
                null,
                "无"))
        };
        updateRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        (await admin.Client.SendAsync(updateRequest)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var confirmKey = $"minutes-confirm-{Guid.NewGuid():N}";
        var confirmBody = new ConfirmMinutesActionItemRequest(
            adminUserId,
            confirmedDueAt,
            first.Content,
            "P0",
            null,
            null,
            null,
            null);
        var task1 = await ConfirmMinutesActionAsync(first.Id, confirmKey, confirmBody);
        var task2 = await ConfirmMinutesActionAsync(first.Id, confirmKey, confirmBody);
        task1.StatusCode.ShouldBe(HttpStatusCode.Created);
        task2.StatusCode.ShouldBe(HttpStatusCode.Created);
        var taskPayload1 = (await task1.Content.ReadFromJsonAsync<ApiResponse<MinutesTaskDto>>())!.Data!;
        var taskPayload2 = (await task2.Content.ReadFromJsonAsync<ApiResponse<MinutesTaskDto>>())!.Data!;
        taskPayload1.Id.ShouldBe(taskPayload2.Id);
        taskPayload1.Content.ShouldBe("人工校对后的行动项");

        var trace = await admin.Client.GetFromJsonAsync<ApiResponse<MinutesTaskTraceDto>>(
            $"/api/v1/minutes/by-task/{taskPayload1.Id}");
        trace!.Data!.Source.Id.ShouldBe(source.Id);
        trace.Data.ActionItem.Speaker.ShouldNotBeNullOrWhiteSpace();
        trace.Data.ActionItem.SourceTimestamp.ShouldNotBeNullOrWhiteSpace();
    }

    [SqlIntegrationFact]
    public async Task Minutes_ReadOnlySummaryAndServerFiltersUseScopedTruth()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var departmentId = await GetDepartmentIdAsync(admin, "RND");
        var adminUserId = await GetUserIdAsync("demo.admin");
        var adminSession = await admin.Client.GetFromJsonAsync<ApiResponse<SessionDto>>(
            "/api/v1/auth/session",
            ClientJsonOptions);
        var adminOrganizationId = Guid.Parse(adminSession!.Data!.OrganizationId);
        var crossOrganizationSourceId = Guid.NewGuid();
        var testEnvironmentSourceId = Guid.NewGuid();
        var crossOrganizationTitle = $"UX005-CROSS-ORG-{Guid.NewGuid():N}";
        var testEnvironmentTitle = $"UX005-TEST-ENV-{Guid.NewGuid():N}";
        var noPermissionUserId = Guid.NewGuid();
        var noPermissionUserName = $"ux005.noperm.{Guid.NewGuid():N}"[..40];
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AidtDbContext>();
            var fixtureNow = DateTimeOffset.UtcNow;
            var otherOrganizationId = Guid.NewGuid();
            var otherDepartmentId = Guid.NewGuid();
            db.Organizations.Add(new OrganizationEntity
            {
                Id = otherOrganizationId,
                Code = $"UX005-{Guid.NewGuid():N}"[..32].ToUpperInvariant(),
                Name = "UX-005 跨组织测试",
                IsActive = true,
                CreatedAt = fixtureNow
            });
            db.Departments.Add(new DepartmentEntity
            {
                Id = otherDepartmentId,
                OrganizationId = otherOrganizationId,
                Code = $"UX005-{Guid.NewGuid():N}"[..32].ToUpperInvariant(),
                Name = "UX-005 跨组织测试部门",
                HierarchyType = "Department",
                Path = "/UX005-CROSS-ORG",
                SortOrder = 999,
                IsActive = true,
                CreatedAt = fixtureNow,
                UpdatedAt = fixtureNow
            });
            db.MinutesSources.AddRange(
                new MinutesSourceEntity
                {
                    Id = crossOrganizationSourceId,
                    OrganizationId = otherOrganizationId,
                    DepartmentId = otherDepartmentId,
                    Title = crossOrganizationTitle,
                    MeetingAt = fixtureNow,
                    SourceType = "ManualText",
                    Transcript = "跨组织测试听记",
                    EnvironmentCode = "demo",
                    IsTestData = true,
                    Status = "Imported",
                    AnalysisVersion = "none",
                    CreatedBy = adminUserId,
                    CreatedAt = fixtureNow,
                    UpdatedAt = fixtureNow
                },
                new MinutesSourceEntity
                {
                    Id = testEnvironmentSourceId,
                    OrganizationId = adminOrganizationId,
                    DepartmentId = departmentId,
                    Title = testEnvironmentTitle,
                    MeetingAt = fixtureNow,
                    SourceType = "ManualText",
                    Transcript = "测试环境范围听记",
                    EnvironmentCode = "test",
                    IsTestData = true,
                    Status = "Imported",
                    AnalysisVersion = "none",
                    CreatedBy = adminUserId,
                    CreatedAt = fixtureNow,
                    UpdatedAt = fixtureNow
                });
            db.Users.Add(new UserEntity
            {
                Id = noPermissionUserId,
                OrganizationId = adminOrganizationId,
                UserName = noPermissionUserName,
                DisplayName = "UX-005 无听记权限用户",
                IsServiceIdentity = false,
                IsActive = true,
                InvitationStatus = "Active",
                Version = 1,
                CreatedAt = fixtureNow
            });
            db.UserDepartmentMemberships.Add(new UserDepartmentMembershipEntity
            {
                UserId = noPermissionUserId,
                DepartmentId = departmentId,
                IsPrimary = true,
                IsManager = false,
                IsActive = true,
                JoinedAt = fixtureNow
            });
            db.DataScopes.AddRange(
                new DataScopeEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = noPermissionUserId,
                    ScopeType = "department",
                    ScopeReference = departmentId.ToString("D")
                },
                new DataScopeEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = noPermissionUserId,
                    ScopeType = "organization",
                    ScopeReference = adminOrganizationId.ToString("D")
                });
            await db.SaveChangesAsync();
        }

        var profiles = await admin.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<AgentProfileDto>>>(
            "/api/v1/agents",
            ClientJsonOptions);
        var profile = profiles!.Data!.Single(item => item.Name.Contains("业务协同"));
        var before = await admin.Client.GetFromJsonAsync<ApiResponse<MinutesSummaryDto>>(
            "/api/v1/minutes/summary?includeTestData=true",
            ClientJsonOptions);
        var marker = $"UX005-MINUTES-{Guid.NewGuid():N}";
        var source = await CreateAndExecuteMinutesAsync(
            admin,
            departmentId,
            marker,
            Guid.Parse(profile.Id));

        var summary = await admin.Client.GetFromJsonAsync<ApiResponse<MinutesSummaryDto>>(
            $"/api/v1/minutes/summary?includeTestData=true",
            ClientJsonOptions);
        summary!.Data!.TotalCount.ShouldBeGreaterThan(before!.Data!.TotalCount);
        summary.Data.AnalyzedCount.ShouldBeGreaterThan(before.Data.AnalyzedCount);
        summary.Data.PendingActionItemCount.ShouldBeGreaterThan(
            before.Data.PendingActionItemCount);
        summary.Data.CreatedThisWeekCount.ShouldBeGreaterThan(before.Data.CreatedThisWeekCount);

        var now = DateTimeOffset.UtcNow;
        var query = new Dictionary<string, string>
        {
            ["page"] = "1",
            ["pageSize"] = "20",
            ["includeTestData"] = "true",
            ["search"] = "项目经理",
            ["departmentId"] = departmentId.ToString("D"),
            ["status"] = "Analyzed",
            ["actionItemStatus"] = "Draft",
            ["sourceType"] = "ManualText",
            ["meetingFrom"] = now.AddDays(-1).ToString("O"),
            ["meetingTo"] = now.AddDays(1).ToString("O")
        };
        var queryString = string.Join(
            "&",
            query.Select(item => $"{item.Key}={Uri.EscapeDataString(item.Value)}"));
        var filtered = await admin.Client.GetFromJsonAsync<
            ApiResponse<PagedResult<MinutesSourceDto>>>(
            $"/api/v1/minutes?{queryString}",
            ClientJsonOptions);

        filtered!.Data!.Total.ShouldBeGreaterThanOrEqualTo(1);
        filtered.Data.Items.ShouldContain(item => item.Id == source.Id);
        filtered.Data.Items.ShouldAllBe(item =>
            item.DepartmentId == departmentId.ToString("D")
            && item.SourceType == "ManualText"
            && item.Status == "Analyzed");

        var oneItemPage = await admin.Client.GetFromJsonAsync<ApiResponse<PagedResult<MinutesSourceDto>>>(
            "/api/v1/minutes?page=1&pageSize=1&includeTestData=true",
            ClientJsonOptions);
        oneItemPage!.Data!.Items.Count.ShouldBe(1);
        oneItemPage.Data.Total.ShouldBe(summary.Data.TotalCount);
        summary.Data.TotalCount.ShouldBeGreaterThan(oneItemPage.Data.Items.Count);

        var crossOrganization = await admin.Client.GetFromJsonAsync<
            ApiResponse<PagedResult<MinutesSourceDto>>>(
            $"/api/v1/minutes?search={Uri.EscapeDataString(crossOrganizationTitle)}&includeTestData=true",
            ClientJsonOptions);
        crossOrganization!.Data!.Total.ShouldBe(0);

        var testEnvironmentHidden = await admin.Client.GetFromJsonAsync<
            ApiResponse<PagedResult<MinutesSourceDto>>>(
            $"/api/v1/minutes?search={Uri.EscapeDataString(testEnvironmentTitle)}&includeTestData=false",
            ClientJsonOptions);
        testEnvironmentHidden!.Data!.Total.ShouldBe(0);
        var testEnvironmentVisible = await admin.Client.GetFromJsonAsync<
            ApiResponse<PagedResult<MinutesSourceDto>>>(
            $"/api/v1/minutes?search={Uri.EscapeDataString(testEnvironmentTitle)}&includeTestData=true",
            ClientJsonOptions);
        testEnvironmentVisible!.Data!.Items.ShouldContain(item => item.Id == testEnvironmentSourceId.ToString("D"));

        using var quality = await CreateAuthenticatedClientAsync("demo.qa");
        var crossDepartment = await quality.Client.GetFromJsonAsync<
            ApiResponse<PagedResult<MinutesSourceDto>>>(
            $"/api/v1/minutes?departmentId={departmentId:D}&includeTestData=true",
            ClientJsonOptions);
        crossDepartment!.Data!.Total.ShouldBe(0);

        using var noPermission = await CreateAuthenticatedClientAsync(noPermissionUserName);
        var permissionDenied = await noPermission.Client.GetAsync(
            "/api/v1/minutes/summary?includeTestData=true");
        permissionDenied.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await permissionDenied.Content.ReadFromJsonAsync<ApiResponse<object?>>())!
            .Code.ShouldBe("PERMISSION_DENIED");

        using var noScope = await CreateAuthenticatedClientAsync("demo.noscope");
        var scopeDenied = await noScope.Client.GetAsync("/api/v1/minutes/summary");
        scopeDenied.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await scopeDenied.Content.ReadFromJsonAsync<ApiResponse<object?>>())!
            .Code.ShouldBe("DATA_SCOPE_DENIED");

        using var anonymous = _factory.CreateClient();
        (await anonymous.GetAsync("/api/v1/minutes/summary")).StatusCode
            .ShouldBe(HttpStatusCode.Unauthorized);
    }

    [SqlIntegrationFact]
    public async Task Minutes_ConfirmedContextAndIncompleteDraftRemainEditableAndTraceable()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var departmentId = await GetDepartmentIdAsync(admin, "RND");
        var marker = $"UX004B-{Guid.NewGuid():N}";
        var project = await CreateProjectAsync(admin, departmentId, $"{marker} 听记确认项目");
        var objectTypeId = await GetObjectTypeIdAsync(admin, "CUSTOMER_SAMPLE");
        var objectResponse = await CreateObjectAsync(
            admin,
            $"ux004b-object-{Guid.NewGuid():N}",
            new CreateBusinessObjectRequest(
                Guid.Parse(objectTypeId),
                $"UX004B-{Guid.NewGuid():N}"[..32].ToUpperInvariant(),
                new Dictionary<string, string?>
                {
                    ["Customer"] = "听记统一确认客户",
                    ["Product"] = "听记统一确认样品",
                    ["Requirement"] = "验证听记行动项业务上下文",
                    ["TrialRequired"] = "No"
                },
                departmentId));
        var objectResponseBody = await objectResponse.Content.ReadAsStringAsync();
        objectResponse.StatusCode.ShouldBe(HttpStatusCode.Created, objectResponseBody);
        var businessObject = (await objectResponse.Content.ReadFromJsonAsync<
            ApiResponse<BusinessObjectDto>>(ClientJsonOptions))!.Data!;
        var projectWorkflow = await PublishProjectWorkflowAsync(admin, departmentId, marker);
        var existingWorkflow = await StartProjectWorkflowAsync(
            admin,
            projectWorkflow,
            project.Id);
        var agentProfiles = await admin.Client.GetFromJsonAsync<
            ApiResponse<IReadOnlyList<AgentProfileDto>>>("/api/v1/agents", ClientJsonOptions);
        var businessAgent = agentProfiles!.Data!.Single(item => item.Name.Contains("业务协同"));
        var source = await CreateAndExecuteMinutesAsync(admin, departmentId, marker, Guid.Parse(businessAgent.Id));
        var actions = await admin.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<MinutesActionItemDto>>>(
            $"/api/v1/minutes/{source.Id}/action-items",
            ClientJsonOptions);
        actions!.Data!.ShouldNotBeEmpty();
        var candidateResponse = await admin.Client.GetFromJsonAsync<
            ApiResponse<IReadOnlyList<MinutesConfirmationUserDto>>>(
            $"/api/v1/minutes/confirmation-users?departmentId={departmentId}",
            ClientJsonOptions);
        candidateResponse!.Data!.ShouldContain(item => item.UserName == "demo.admin");

        var adminUserId = await GetUserIdAsync("demo.admin");
        var dueAt = new DateTimeOffset(2026, 12, 18, 18, 0, 0, TimeSpan.FromHours(8));
        var selected = actions.Data[0];
        selected.ShouldNotBeNull();
        existingWorkflow.ShouldNotBeNull();
        businessObject.ShouldNotBeNull();
        project.ShouldNotBeNull();
        Console.WriteLine(
            $"UX004B update source={source.Id} action={selected.Id} project={project.Id} object={businessObject.Id} workflow={existingWorkflow.Id} version={projectWorkflow.WorkflowVersionId}");
        using var updateRequest = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/v1/minutes/{source.Id}/action-items/{selected.Id}")
        {
            Content = JsonContent.Create(new UpdateMinutesActionItemRequest(
                adminUserId,
                dueAt,
                "统一确认后的听记任务",
                "P0",
                Guid.Parse(objectTypeId),
                projectWorkflow.WorkflowVersionId,
                "无",
                Guid.Parse(project.Id),
                Guid.Parse(businessObject.Id),
                Guid.Parse(existingWorkflow.Id)))
        };
        updateRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        Console.WriteLine("UX004B update request ready");
        var updateResponse = await admin.Client.SendAsync(updateRequest);
        Console.WriteLine($"UX004B update status={updateResponse.StatusCode}");
        var updateBody = await updateResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"UX004B update body={updateBody}");
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK, updateBody);
        var updated = (await updateResponse.Content.ReadFromJsonAsync<
            ApiResponse<MinutesActionItemDto>>(ClientJsonOptions))!.Data!;
        Console.WriteLine($"UX004B updated project={updated.ProjectId}");
        updated.ProjectName.ShouldBe(project.Name);
        updated.BusinessObjectCode.ShouldBe(businessObject.ObjectCode);
        updated.WorkflowInstanceId.ShouldBe(existingWorkflow.Id);
        updated.AssigneeDisplayName.ShouldBe("平台管理员");

        var confirmRequest = new ConfirmMinutesActionItemRequest(
            adminUserId,
            dueAt,
            "统一确认后的听记任务",
            "P0",
            Guid.Parse(objectTypeId),
            projectWorkflow.WorkflowVersionId,
            Guid.Parse(businessObject.Id),
            Guid.Parse(existingWorkflow.Id),
            Guid.Parse(project.Id));
        var confirmation = await ConfirmMinutesActionAsync(
            selected.Id,
            $"ux004b-confirm-{Guid.NewGuid():N}",
            confirmRequest);
        confirmation.StatusCode.ShouldBe(HttpStatusCode.Created);
        var task = (await confirmation.Content.ReadFromJsonAsync<ApiResponse<MinutesTaskDto>>(
            ClientJsonOptions))!.Data!;
        task.ProjectId.ShouldBe(project.Id);
        task.ProjectName.ShouldBe(project.Name);
        task.BusinessObjectId.ShouldBe(businessObject.Id);
        task.WorkflowInstanceId.ShouldBe(existingWorkflow.Id);
        task.DueAt!.Value.ToUniversalTime().ShouldBe(dueAt.ToUniversalTime());
        var duplicateConfirmation = await ConfirmMinutesActionAsync(
            selected.Id,
            $"ux004b-duplicate-{Guid.NewGuid():N}",
            confirmRequest);
        duplicateConfirmation.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        using var freshReader = await CreateAuthenticatedClientAsync("demo.pm");
        var freshActions = await freshReader.Client.GetFromJsonAsync<
            ApiResponse<IReadOnlyList<MinutesActionItemDto>>>(
            $"/api/v1/minutes/{source.Id}/action-items",
            ClientJsonOptions);
        var persisted = freshActions!.Data!.Single(item => item.Id == selected.Id);
        persisted.Status.ShouldBe("Confirmed");
        persisted.ProjectId.ShouldBe(project.Id);
        persisted.BusinessObjectId.ShouldBe(businessObject.Id);
        persisted.WorkflowInstanceId.ShouldBe(existingWorkflow.Id);
        persisted.AssigneeId.ShouldBe(adminUserId.ToString("D"));
        persisted.DueAt!.Value.ToUniversalTime().ShouldBe(dueAt.ToUniversalTime());

        var detail = await freshReader.Client.GetFromJsonAsync<ApiResponse<ProjectDetailDto>>(
            $"/api/v1/projects/{project.Id}",
            ClientJsonOptions);
        detail!.Data!.Relations.ShouldContain(item =>
            item.EntityType == "MinutesActionItem" && item.EntityId == selected.Id);
        detail.Data.Relations.ShouldContain(item =>
            item.EntityType == "MinutesTask" && item.EntityId == task.Id);
        detail.Data.RelatedWorkflows.ShouldContain(item => item.Instance.Id == existingWorkflow.Id);

        var incompleteSource = await CreateAndExecuteMinutesAsync(
            admin,
            departmentId,
            $"{marker}-INCOMPLETE",
            Guid.Parse(businessAgent.Id));
        var incompleteActions = await admin.Client.GetFromJsonAsync<
            ApiResponse<IReadOnlyList<MinutesActionItemDto>>>(
            $"/api/v1/minutes/{incompleteSource.Id}/action-items",
            ClientJsonOptions);
        var incomplete = incompleteActions!.Data![0];
        using var incompleteRequest = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/v1/minutes/{incompleteSource.Id}/action-items/{incomplete.Id}")
        {
            Content = JsonContent.Create(new UpdateMinutesActionItemRequest(
                null,
                null,
                incomplete.Content,
                "P1",
                null,
                null,
                "负责人和截止时间待补充"))
        };
        incompleteRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var incompleteSaved = await admin.Client.SendAsync(incompleteRequest);
        incompleteSaved.StatusCode.ShouldBe(HttpStatusCode.OK);
        var incompleteBody = (await incompleteSaved.Content.ReadFromJsonAsync<
            ApiResponse<MinutesActionItemDto>>(ClientJsonOptions))!.Data!;
        incompleteBody.AssigneeId.ShouldBeNull();
        incompleteBody.DueAt.ShouldBeNull();
        var blocked = await ConfirmMinutesActionAsync(
            incomplete.Id,
            $"ux004b-incomplete-{Guid.NewGuid():N}",
            new ConfirmMinutesActionItemRequest(
                null,
                null,
                incomplete.Content,
                "P1",
                null,
                null,
                null,
                null));
        blocked.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [SqlIntegrationFact]
    public async Task NodePolicySlaAndAgentProxy_AreVersionFrozenAndHumanAuthorized()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var departments = await admin.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<DepartmentDto>>>(
            "/api/v1/departments");
        var departmentId = departments!.Data!.Single(item => item.Code == "RND").Id;
        var definitions = await admin.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<WorkflowDefinitionDto>>>(
            "/api/v1/workflow-definitions");
        var definition = definitions!.Data!.Single(item => item.Code == "CUSTOMER_SAMPLE_TO_PRODUCTION");
        var profiles = await admin.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<AgentProfileDto>>>(
            "/api/v1/agents",
            ClientJsonOptions);
        var proxyProfile = profiles!.Data!.Single(item => item.Name.Contains("流程代理"));
        var policyRequest = new SaveWorkflowNodePolicyRequest(
            null,
            null,
            120,
            30,
            60,
            "Escalate",
            true,
            true,
            Guid.Parse(proxyProfile.Id),
            "Low",
            0,
            "A2",
            "HumanConfirmedProxy",
            null,
            false,
            0,
            "确认材料完整",
            true);
        var policy = await SaveNodePolicyAsync(definition.VersionId, "RD_INITIATE", policyRequest);
        policy.StatusCode.ShouldBe(HttpStatusCode.OK);

        var typeId = await GetObjectTypeIdAsync(admin, "CUSTOMER_SAMPLE");
        var objectCode = $"SLA-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
        var objectResponse = await CreateObjectAsync(
            admin,
            $"sla-object-{Guid.NewGuid():N}",
            new CreateBusinessObjectRequest(
                Guid.Parse(typeId),
                objectCode,
                new Dictionary<string, string?>
                {
                    ["Customer"] = "SLA 测试客户",
                    ["Product"] = "SLA 测试产品",
                    ["Requirement"] = "验证节点策略"
                },
                Guid.Parse(departmentId)));
        var businessObject =
            (await objectResponse.Content.ReadFromJsonAsync<ApiResponse<BusinessObjectDto>>())!.Data!;
        using var startRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/workflow-instances")
        {
            Content = JsonContent.Create(new StartWorkflowInstanceRequest(
                Guid.Parse(definition.Id),
                Guid.Parse(definition.VersionId),
                Guid.Parse(businessObject.Id),
                $"sla-workflow-{Guid.NewGuid():N}"))
        };
        startRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var startResponse = await admin.Client.SendAsync(startRequest);
        startResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var instance = (await startResponse.Content.ReadFromJsonAsync<ApiResponse<WorkflowInstanceDto>>(
            ClientJsonOptions))!.Data!;
        var tasks = await admin.Client.GetFromJsonAsync<ApiResponse<PagedResult<WorkflowTaskDto>>>(
            "/api/v1/workflow-tasks?pageSize=100",
            ClientJsonOptions);
        var task = tasks!.Data!.Items.First(item => item.WorkflowInstanceId == instance.Id);
        task.DueAt.ShouldNotBeNull();
        task.SlaStatus.ShouldBe("OnTrack");

        await SetTaskCreatedAtAsync(Guid.Parse(task.Id), DateTimeOffset.UtcNow.AddMinutes(-70));
        using var scan1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/workflow-sla/actions/run");
        scan1.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var scanResponse1 = await admin.Client.SendAsync(scan1);
        scanResponse1.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var scan2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/workflow-sla/actions/run");
        scan2.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var scanResponse2 = await admin.Client.SendAsync(scan2);
        scanResponse2.StatusCode.ShouldBe(HttpStatusCode.OK);
        var refreshedTasks = await admin.Client.GetFromJsonAsync<ApiResponse<PagedResult<WorkflowTaskDto>>>(
            "/api/v1/workflow-tasks?pageSize=100",
            ClientJsonOptions);
        var refreshedTask = refreshedTasks!.Data!.Items.Single(item => item.Id == task.Id);
        refreshedTask.SlaStatus.ShouldBe("Escalated");
        refreshedTask.ReminderSentAt.ShouldNotBeNull();
        refreshedTask.EscalatedAt.ShouldNotBeNull();
        using var assignee = await CreateAuthenticatedClientAsync("demo.researcher");
        var notifications = await assignee.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<WorkflowNotificationDto>>>(
            "/api/v1/workflow-notifications?pageSize=100");
        notifications!.Data!.Count(item =>
            item.WorkflowTaskId == task.Id && item.Type == "SlaReminder").ShouldBe(1);
        var adminNotifications = await admin.Client.GetFromJsonAsync<
            ApiResponse<IReadOnlyList<WorkflowNotificationDto>>>(
            "/api/v1/workflow-notifications?pageSize=100");
        adminNotifications!.Data!.Count(item =>
            item.WorkflowTaskId == task.Id && item.Type == "SlaEscalated").ShouldBe(1);

        using var proxyRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/workflow-tasks/{task.Id}/actions/agent-proxy");
        proxyRequest.Headers.Add("Idempotency-Key", $"agent-proxy-{Guid.NewGuid():N}");
        proxyRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var proxyResponse = await admin.Client.SendAsync(proxyRequest);
        proxyResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var proxyRun = (await proxyResponse.Content.ReadFromJsonAsync<ApiResponse<AgentRunDto>>(
            ClientJsonOptions))!.Data!;
        proxyRun.Status.ShouldBe(AgentRunStatus.AwaitingConfirmation);

        using var confirmProxyRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/agents/runs/{proxyRun.Id}/actions/confirm-proxy")
        {
            Content = JsonContent.Create(new ConfirmAgentProxyRequest(Guid.NewGuid()))
        };
        confirmProxyRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var confirmProxyResponse = await admin.Client.SendAsync(confirmProxyRequest);
        confirmProxyResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var completedProxy = (await confirmProxyResponse.Content.ReadFromJsonAsync<ApiResponse<AgentRunDto>>(
            ClientJsonOptions))!.Data!;
        completedProxy.Status.ShouldBe(AgentRunStatus.Completed);

        var rejectedPolicy = await SaveNodePolicyAsync(
            definition.VersionId,
            "FINAL_RELEASE",
            policyRequest with { PermanentHumanBoundary = true });
        rejectedPolicy.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var employeePolicy = await SaveNodePolicyAsync(
            definition.VersionId,
            "RD_INITIATE",
            policyRequest with { Publish = false },
            "demo.researcher");
        employeePolicy.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var excessiveAmount = await SaveNodePolicyAsync(
            definition.VersionId,
            "RD_INITIATE",
            policyRequest with { AgentMaxAmount = 1 });
        excessiveAmount.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var excessiveAutonomy = await SaveNodePolicyAsync(
            definition.VersionId,
            "RD_INITIATE",
            policyRequest with { AgentMaxAutonomy = "A3" });
        excessiveAutonomy.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var excessiveRisk = await SaveNodePolicyAsync(
            definition.VersionId,
            "RD_INITIATE",
            policyRequest with { AgentMaxRisk = "Medium" });
        excessiveRisk.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var unavailableAgent = await SaveNodePolicyAsync(
            definition.VersionId,
            "RD_INITIATE",
            policyRequest with { AgentProfileId = Guid.NewGuid() });
        unavailableAgent.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [SqlIntegrationFact]
    public async Task PreAuthorizedSlaProxy_UsesServiceIdentityAndRunsOnlyOnce()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var departments = await admin.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<DepartmentDto>>>(
            "/api/v1/departments");
        var departmentId = departments!.Data!.Single(item => item.Code == "RND").Id;
        var definitions = await admin.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<WorkflowDefinitionDto>>>(
            "/api/v1/workflow-definitions");
        var definition = definitions!.Data!.Single(item => item.Code == "RND_MATERIALS_CONFIRM");
        var typeId = await GetObjectTypeIdAsync(admin, "CUSTOMER_SAMPLE");
        var objectResponse = await CreateObjectAsync(
            admin,
            $"auto-proxy-object-{Guid.NewGuid():N}",
            new CreateBusinessObjectRequest(
                Guid.Parse(typeId),
                $"APX-{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
                new Dictionary<string, string?>
                {
                    ["Customer"] = "自动代理测试客户",
                    ["Product"] = "自动代理测试产品",
                    ["Requirement"] = "研发资料完整性确认"
                },
                Guid.Parse(departmentId)));
        var businessObject =
            (await objectResponse.Content.ReadFromJsonAsync<ApiResponse<BusinessObjectDto>>())!.Data!;
        using var startRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/workflow-instances")
        {
            Content = JsonContent.Create(new StartWorkflowInstanceRequest(
                Guid.Parse(definition.Id),
                Guid.Parse(definition.VersionId),
                Guid.Parse(businessObject.Id),
                $"auto-proxy-workflow-{Guid.NewGuid():N}"))
        };
        startRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var startResponse = await admin.Client.SendAsync(startRequest);
        startResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var instance = (await startResponse.Content.ReadFromJsonAsync<ApiResponse<WorkflowInstanceDto>>(
            ClientJsonOptions))!.Data!;
        var tasks = await admin.Client.GetFromJsonAsync<ApiResponse<PagedResult<WorkflowTaskDto>>>(
            "/api/v1/workflow-tasks?pageSize=100",
            ClientJsonOptions);
        var task = tasks!.Data!.Items.Single(item => item.WorkflowInstanceId == instance.Id);
        task.NodeCode.ShouldBe("MATERIALS_CONFIRM");
        task.DueAt.ShouldNotBeNull();

        using var simulateRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/workflow-sla/tasks/{task.Id}/actions/simulate-timeout");
        simulateRequest.Headers.Add("Idempotency-Key", $"simulate-{Guid.NewGuid():N}");
        simulateRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        (await admin.Client.SendAsync(simulateRequest)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var firstScan = await RunSlaScanAsync(admin.Client, admin.CsrfToken);
        firstScan.Reminded.ShouldBeGreaterThanOrEqualTo(1);
        firstScan.Escalated.ShouldBeGreaterThanOrEqualTo(1);
        firstScan.AgentProxied.ShouldBe(1);
        var secondScan = await RunSlaScanAsync(admin.Client, admin.CsrfToken);
        secondScan.AgentProxied.ShouldBe(0);

        var refreshed = await admin.Client.GetFromJsonAsync<ApiResponse<PagedResult<WorkflowTaskDto>>>(
            "/api/v1/workflow-tasks?pageSize=100",
            ClientJsonOptions);
        var completed = refreshed!.Data!.Items.Single(item => item.Id == task.Id);
        completed.Status.ShouldBe(AIDT.Domain.Workflows.WorkflowTaskStatus.Approved);
        completed.DecisionMode.ShouldBe("AgentProxy");
        completed.AgentRunId.ShouldNotBeNull();

        var evidence = await GetAutoProxyEvidenceAsync(Guid.Parse(task.Id));
        evidence.ProxyCount.ShouldBe(1);
        evidence.Status.ShouldBe("Completed");
        evidence.TriggerMode.ShouldBe("SlaTimeout");
        evidence.IdempotencyKey.ShouldBe($"sla-auto-proxy:{task.Id}");
        evidence.RequestedBy.ShouldBe(evidence.ServiceUserId);
        evidence.AuditReason.ShouldContain("DecisionMode=AgentProxy");
        evidence.AuditReason.ShouldContain("Trigger=SlaTimeout");
        evidence.AuditReason.ShouldContain($"OriginalAssignee={completed.AssigneeId}");

        using var researcher = await CreateAuthenticatedClientAsync("demo.researcher");
        var notifications = await researcher.Client.GetFromJsonAsync<
            ApiResponse<IReadOnlyList<WorkflowNotificationDto>>>(
            "/api/v1/workflow-notifications?pageSize=100");
        notifications!.Data!.Count(item =>
            item.WorkflowTaskId == task.Id && item.Type == "AgentProxied").ShouldBe(1);
    }

    [SqlIntegrationFact]
    public async Task PermanentHumanBoundary_NeverAutoApprovesOnTimeout()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var departments = await admin.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<DepartmentDto>>>(
            "/api/v1/departments");
        var departmentId = departments!.Data!.Single(item => item.Code == "RND").Id;
        var definitions = await admin.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<WorkflowDefinitionDto>>>(
            "/api/v1/workflow-definitions");
        var definition = definitions!.Data!.Single(item => item.Code == "CUSTOMER_SAMPLE_TO_PRODUCTION");
        var typeId = await GetObjectTypeIdAsync(admin, "CUSTOMER_SAMPLE");
        var objectResponse = await CreateObjectAsync(
            admin,
            $"high-risk-object-{Guid.NewGuid():N}",
            new CreateBusinessObjectRequest(
                Guid.Parse(typeId),
                $"HRX-{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
                new Dictionary<string, string?>
                {
                    ["Customer"] = "高风险边界测试客户",
                    ["Product"] = "高风险边界测试产品",
                    ["Requirement"] = "验证永久人工边界"
                },
                Guid.Parse(departmentId)));
        var businessObject =
            (await objectResponse.Content.ReadFromJsonAsync<ApiResponse<BusinessObjectDto>>())!.Data!;
        using var startRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/workflow-instances")
        {
            Content = JsonContent.Create(new StartWorkflowInstanceRequest(
                Guid.Parse(definition.Id),
                Guid.Parse(definition.VersionId),
                Guid.Parse(businessObject.Id),
                $"high-risk-workflow-{Guid.NewGuid():N}"))
        };
        startRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var startResponse = await admin.Client.SendAsync(startRequest);
        startResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var instance = (await startResponse.Content.ReadFromJsonAsync<ApiResponse<WorkflowInstanceDto>>(
            ClientJsonOptions))!.Data!;
        var tasks = await admin.Client.GetFromJsonAsync<ApiResponse<PagedResult<WorkflowTaskDto>>>(
            "/api/v1/workflow-tasks?pageSize=100",
            ClientJsonOptions);
        var task = tasks!.Data!.Items.Single(item => item.WorkflowInstanceId == instance.Id);
        await InstallDefenseInDepthHighRiskPolicyAsync(Guid.Parse(task.Id), Guid.Parse(instance.Id));

        using var simulateRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/workflow-sla/tasks/{task.Id}/actions/simulate-timeout");
        simulateRequest.Headers.Add("Idempotency-Key", $"simulate-high-risk-{Guid.NewGuid():N}");
        simulateRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        (await admin.Client.SendAsync(simulateRequest)).StatusCode.ShouldBe(HttpStatusCode.OK);
        var scan = await RunSlaScanAsync(admin.Client, admin.CsrfToken);
        scan.AgentProxied.ShouldBe(0);

        var refreshed = await admin.Client.GetFromJsonAsync<ApiResponse<PagedResult<WorkflowTaskDto>>>(
            "/api/v1/workflow-tasks?pageSize=100",
            ClientJsonOptions);
        var pending = refreshed!.Data!.Items.Single(item => item.Id == task.Id);
        pending.Status.ShouldBe(AIDT.Domain.Workflows.WorkflowTaskStatus.Pending);
        pending.DecisionMode.ShouldBe("Human");
        pending.AgentRunId.ShouldBeNull();
        (await CountProxyRunsAsync(Guid.Parse(task.Id))).ShouldBe(0);
    }

    [SqlIntegrationFact]
    public async Task AgentRun_UnapprovedModelIsRejectedBeforeGatewayCall()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var profiles = await admin.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<AgentProfileDto>>>(
            "/api/v1/agents",
            ClientJsonOptions);
        var profile = profiles!.Data!.Single(item => item.Name.Contains("业务协同"));
        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/agents/runs")
        {
            Content = JsonContent.Create(new CreateAgentRunRequest(
                Guid.Parse(profile.Id),
                AgentActionType.Draft,
                0,
                AgentRiskLevel.Low,
                "unapproved-model"))
        };
        createRequest.Headers.Add("Idempotency-Key", $"bad-model-{Guid.NewGuid():N}");
        createRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var createResponse = await admin.Client.SendAsync(createRequest);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var denied = await createResponse.Content.ReadFromJsonAsync<ApiResponse<object?>>(
            ClientJsonOptions);
        denied!.Code.ShouldBe("PERMISSION_DENIED");
    }

    [SqlIntegrationFact]
    public async Task ProjectManager_CreatesProjectMembersMilestonesAndEnforcesScope()
    {
        using var pm = await CreateAuthenticatedClientAsync("demo.pm");
        var departmentId = await GetDepartmentIdAsync(pm, "RND");
        var researcherId = await GetUserIdAsync("demo.researcher");
        var project = await CreateProjectAsync(pm, departmentId, "耐候型苯丙乳液客户开发");

        project.OwnerDisplayName.ShouldBe("项目经理");
        project.Status.ShouldBe("Active");
        project.EnvironmentCode.ShouldBe("demo");

        await AddProjectMemberAsync(pm, project.Id, researcherId, "Contributor");
        var milestone = await AddMilestoneAsync(pm, project.Id, researcherId, DateTimeOffset.UtcNow.AddDays(3));
        milestone.OwnerUserId.ShouldBe(researcherId.ToString("D"));

        using var researcher = await CreateAuthenticatedClientAsync("demo.researcher");
        var researcherDetail = await researcher.Client.GetFromJsonAsync<ApiResponse<ProjectDetailDto>>(
            $"/api/v1/projects/{project.Id}",
            ClientJsonOptions);
        researcherDetail!.Data!.Members.ShouldContain(item => item.UserId == researcherId.ToString("D"));
        researcherDetail.Data.Milestones.ShouldContain(item => item.Id == milestone.Id);

        using var qa = await CreateAuthenticatedClientAsync("demo.qa");
        var crossDepartment = await qa.Client.GetAsync($"/api/v1/projects/{project.Id}");
        crossDepartment.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await crossDepartment.Content.ReadFromJsonAsync<ApiResponse<object?>>())!
            .Code.ShouldBe("PROJECT_SCOPE_DENIED");

        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        (await admin.Client.GetAsync($"/api/v1/projects/{project.Id}")).StatusCode.ShouldBe(HttpStatusCode.OK);

        using var memberMilestoneRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/projects/{project.Id}/milestones")
        {
            Content = JsonContent.Create(new CreateProjectMilestoneRequest(
                "普通成员越权里程碑",
                researcherId,
                DateTimeOffset.UtcNow.AddDays(5),
                "Planned"))
        };
        memberMilestoneRequest.Headers.Add("X-CSRF-TOKEN", researcher.CsrfToken);
        var deniedMilestone = await researcher.Client.SendAsync(memberMilestoneRequest);
        deniedMilestone.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [SqlIntegrationFact]
    public async Task Inbox_AgentAnalysisHasNoSideEffectsAndHumanConfirmationIsIdempotent()
    {
        using var pm = await CreateAuthenticatedClientAsync("demo.pm");
        var departmentId = await GetDepartmentIdAsync(pm, "RND");
        var project = await CreateProjectAsync(pm, departmentId, "耐候样品技术与客户确认");
        var agents = await pm.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<AgentProfileDto>>>(
            "/api/v1/agents",
            ClientJsonOptions);
        var agent = agents!.Data!.First(item => item.AutonomyLevel != AgentAutonomyLevel.A1);
        var title = $"会议事项-{Guid.NewGuid():N}";
        var inbox = await ReceiveInboxEventAsync(
            pm,
            departmentId,
            title,
            $"项目 {project.Name} 需在截止日期：{DateTime.Today.AddDays(7):yyyy-MM-dd} 前完成客户样品资料确认。");

        var analyzed = await AnalyzeInboxEventAsync(pm, inbox.Id, agent.Id);
        analyzed.Status.ShouldBe("AwaitingConfirmation");
        analyzed.Suggestion.ShouldNotBeNull();
        analyzed.Suggestion!.SuggestedProjectId.ShouldBe(project.Id);
        analyzed.Suggestion.RequiresHumanBoundary.ShouldBeFalse();
        (await CountProjectTasksByEventAsync(Guid.Parse(inbox.Id))).ShouldBe(0);
        (await CountWorkflowInstancesByProjectAsync(Guid.Parse(project.Id))).ShouldBe(0);

        var key = $"confirm-{Guid.NewGuid():N}";
        var confirmation = await ConfirmInboxEventAsync(pm, inbox.Id, project.Id, key);
        confirmation.Task.ShouldNotBeNull();
        var taskId = confirmation.Task!.Id;
        var replay = await ConfirmInboxEventAsync(pm, inbox.Id, project.Id, key);
        replay.Task!.Id.ShouldBe(taskId);
        (await CountProjectTasksByEventAsync(Guid.Parse(inbox.Id))).ShouldBe(1);

        var task = await CompleteProjectTaskAsync(pm, taskId, confirmation.Task.Version);
        task.Status.ShouldBe("Completed");
        var trace = await pm.Client.GetFromJsonAsync<ApiResponse<InboxTaskTraceDto>>(
            $"/api/v1/project-tasks/{taskId}/trace",
            ClientJsonOptions);
        trace!.Data!.Event.Id.ShouldBe(inbox.Id);
        trace.Data.Event.Suggestion!.EvidenceExcerpt.ShouldContain("客户样品资料确认");
        trace.Data.Project.ProgressPercent.ShouldBe(100);
    }

    [SqlIntegrationFact]
    public async Task Inbox_StatusPersistsAcrossFreshReadsAndSharedDeadline()
    {
        using var pm = await CreateAuthenticatedClientAsync("demo.pm");
        var departmentId = await GetDepartmentIdAsync(pm, "RND");
        var project = await CreateProjectAsync(pm, departmentId, "事件状态持久化项目");
        var agents = await pm.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<AgentProfileDto>>>(
            "/api/v1/agents",
            ClientJsonOptions);
        var agent = agents!.Data!.First(item => item.AutonomyLevel != AgentAutonomyLevel.A1);
        var inbox = await ReceiveInboxEventAsync(
            pm,
            departmentId,
            "事件状态重新读取",
            $"项目 {project.Name} 需在截止日期：2025-08-15 前完成客户资料确认。");

        await AnalyzeInboxEventAsync(pm, inbox.Id, agent.Id);
        using var freshReader = await CreateAuthenticatedClientAsync("demo.pm");
        var afterAnalyze = await freshReader.Client.GetFromJsonAsync<ApiResponse<InboxEventDto>>(
            $"/api/v1/inbox/{inbox.Id}",
            ClientJsonOptions);
        afterAnalyze!.Data!.Status.ShouldBe("AwaitingConfirmation");
        afterAnalyze.Data.AnalysisRunId.ShouldNotBeNullOrWhiteSpace();
        afterAnalyze.Data.Suggestion!.SuggestedDueAt.ShouldNotBeNull();
        var suggestionDeadline = afterAnalyze.Data.Suggestion.SuggestedDueAt.Value;
        suggestionDeadline.ToUniversalTime().ShouldBe(
            new DateTimeOffset(2025, 8, 15, 10, 0, 0, TimeSpan.Zero));
        var shanghaiDeadline = suggestionDeadline.ToOffset(TimeSpan.FromHours(8));
        shanghaiDeadline.Year.ShouldBe(2025);
        shanghaiDeadline.Month.ShouldBe(8);
        shanghaiDeadline.Day.ShouldBe(15);
        shanghaiDeadline.Hour.ShouldBe(18);
        shanghaiDeadline.Minute.ShouldBe(0);
        shanghaiDeadline.Offset.ShouldBe(TimeSpan.FromHours(8));

        var confirmation = await ConfirmInboxEventAsync(
            pm,
            inbox.Id,
            project.Id,
            $"persist-confirm-{Guid.NewGuid():N}");
        confirmation.Task.ShouldNotBeNull();
        confirmation.Task!.DueAt!.Value.ToUniversalTime().ShouldBe(suggestionDeadline.ToUniversalTime());

        using var finalReader = await CreateAuthenticatedClientAsync("demo.pm");
        var afterConfirm = await finalReader.Client.GetFromJsonAsync<ApiResponse<InboxEventDto>>(
            $"/api/v1/inbox/{inbox.Id}",
            ClientJsonOptions);
        afterConfirm!.Data!.Status.ShouldBe("Confirmed");
        afterConfirm.Data.Suggestion!.Status.ShouldBe("Confirmed");
        var persisted = await GetInboxPersistenceAsync(Guid.Parse(inbox.Id));
        persisted.EventStatus.ShouldBe("Confirmed");
        persisted.SuggestionStatus.ShouldBe("Confirmed");
        persisted.TaskCount.ShouldBe(1);
    }

    [SqlIntegrationFact]
    public async Task Inbox_EditableAssigneeDeadlineAndContentPersistAcrossRefresh()
    {
        using var pm = await CreateAuthenticatedClientAsync("demo.pm");
        var departmentId = await GetDepartmentIdAsync(pm, "RND");
        var project = await CreateProjectAsync(pm, departmentId, "事件确认编辑项目");
        var researcherId = await GetUserIdAsync("demo.researcher");
        await AddProjectMemberAsync(pm, project.Id, researcherId, "Contributor");
        var agents = await pm.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<AgentProfileDto>>>(
            "/api/v1/agents",
            ClientJsonOptions);
        var agent = agents!.Data!.First(item => item.AutonomyLevel != AgentAutonomyLevel.A1);
        var inbox = await ReceiveInboxEventAsync(
            pm,
            departmentId,
            "可编辑人工确认",
            $"项目 {project.Name} 需要质量工程师跟进。");
        await AnalyzeInboxEventAsync(pm, inbox.Id, agent.Id);

        var dueAt = new DateTimeOffset(2026, 5, 1, 18, 0, 0, TimeSpan.FromHours(8));
        var confirmation = await ConfirmInboxEventAndStartFlowAsync(
            pm,
            inbox.Id,
            new ConfirmInboxEventRequest(
                Guid.Parse(project.Id),
                researcherId,
                dueAt,
                "P2",
                "真人编辑后的研发确认内容",
                null,
                true));

        confirmation.Task.ShouldNotBeNull();
        confirmation.Task!.AssigneeId.ShouldBe(researcherId.ToString("D"));
        confirmation.Task.Content.ShouldBe("真人编辑后的研发确认内容");
        confirmation.Task.Priority.ShouldBe("P2");
        confirmation.Task.DueAt!.Value.ToUniversalTime().ShouldBe(dueAt.ToUniversalTime());
        confirmation.Task.WorkflowInstanceId.ShouldBeNull();

        using var freshReader = await CreateAuthenticatedClientAsync("demo.pm");
        var freshEvent = await freshReader.Client.GetFromJsonAsync<ApiResponse<InboxEventDto>>(
            $"/api/v1/inbox/{inbox.Id}",
            ClientJsonOptions);
        freshEvent!.Data!.Status.ShouldBe("Confirmed");
        var projectDetail = await freshReader.Client.GetFromJsonAsync<ApiResponse<ProjectDetailDto>>(
            $"/api/v1/projects/{project.Id}",
            ClientJsonOptions);
        var persistedTask = projectDetail!.Data!.Tasks.Single(item => item.Id == confirmation.Task.Id);
        persistedTask.AssigneeId.ShouldBe(researcherId.ToString("D"));
        persistedTask.Content.ShouldBe("真人编辑后的研发确认内容");
        persistedTask.Priority.ShouldBe("P2");
        persistedTask.DueAt!.Value.ToUniversalTime().ShouldBe(dueAt.ToUniversalTime());
    }

    [SqlIntegrationFact]
    public async Task Inbox_ConfirmationAndNewWorkflowCommitAtomicallyAndRemainTraceable()
    {
        using var pm = await CreateAuthenticatedClientAsync("demo.pm");
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var departmentId = await GetDepartmentIdAsync(pm, "RND");
        var project = await CreateProjectAsync(pm, departmentId, "事件并发起流程项目");
        var researcherId = await GetUserIdAsync("demo.researcher");
        await AddProjectMemberAsync(pm, project.Id, researcherId, "Contributor");
        var definition = await PublishProjectWorkflowAsync(admin, departmentId, "UX004-A");
        var agents = await pm.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<AgentProfileDto>>>(
            "/api/v1/agents",
            ClientJsonOptions);
        var agent = agents!.Data!.First(item => item.AutonomyLevel != AgentAutonomyLevel.A1);
        var inbox = await ReceiveInboxEventAsync(
            pm,
            departmentId,
            "确认并发起流程",
            $"项目 {project.Name} 需完成事件确认并发起项目流程。");
        await AnalyzeInboxEventAsync(pm, inbox.Id, agent.Id);

        var dueAt = new DateTimeOffset(2026, 6, 8, 18, 0, 0, TimeSpan.FromHours(8));
        var confirmation = await ConfirmInboxEventAndStartFlowAsync(
            admin,
            inbox.Id,
            new ConfirmInboxEventRequest(
                Guid.Parse(project.Id),
                researcherId,
                dueAt,
                "P1",
                "确认后创建任务并启动流程",
                null,
                true,
                definition.WorkflowDefinitionId,
                definition.WorkflowVersionId));
        confirmation.Task.ShouldNotBeNull();
        confirmation.Workflow.ShouldNotBeNull();
        confirmation.Task!.WorkflowInstanceId.ShouldBe(confirmation.Workflow!.Id);
        confirmation.Task.WorkflowTaskId.ShouldNotBeNullOrWhiteSpace();

        using var freshReader = await CreateAuthenticatedClientAsync("demo.admin");
        var detail = await freshReader.Client.GetFromJsonAsync<ApiResponse<ProjectDetailDto>>(
            $"/api/v1/projects/{project.Id}",
            ClientJsonOptions);
        detail!.Data!.Tasks.ShouldContain(item => item.Id == confirmation.Task.Id);
        detail.Data.RelatedWorkflows.ShouldContain(item =>
            item.Instance.Id == confirmation.Workflow.Id);
        detail.Data.Relations.ShouldContain(item =>
            item.EntityType == "InboxEvent"
            && item.EntityId == inbox.Id
            && item.RelationCode == "SourceEvent");
        detail.Data.Relations.ShouldContain(item =>
            item.EntityType == "ProjectTask"
            && item.EntityId == confirmation.Task.Id
            && item.RelationCode == "ActionTask");
        detail.Data.Relations.ShouldContain(item =>
            item.EntityType == "WorkflowInstance"
            && item.EntityId == confirmation.Workflow.Id
            && item.RelationCode == "ProjectFlow");
        detail.Data.AuditTimeline.ShouldContain(item => item.Action == "event.confirmed");
        detail.Data.AuditTimeline.ShouldContain(item => item.Action == "project.task_created");

        var trace = await freshReader.Client.GetFromJsonAsync<ApiResponse<InboxTaskTraceDto>>(
            $"/api/v1/project-tasks/{confirmation.Task.Id}/trace",
            ClientJsonOptions);
        trace!.Data!.Event.Id.ShouldBe(inbox.Id);
        trace.Data.Task.WorkflowInstanceId.ShouldBe(confirmation.Workflow.Id);

        var duplicate = await SendConfirmInboxEventAndStartFlowAsync(
            admin,
            inbox.Id,
            new ConfirmInboxEventRequest(
                Guid.Parse(project.Id),
                researcherId,
                dueAt,
                "P1",
                "同一事件不能再次派发",
                null,
                true,
                definition.WorkflowDefinitionId,
                definition.WorkflowVersionId));
        duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await CountProjectTasksByEventAsync(Guid.Parse(inbox.Id))).ShouldBe(1);
        (await CountWorkflowInstancesByProjectAsync(Guid.Parse(project.Id))).ShouldBe(1);
    }

    [SqlIntegrationFact]
    public async Task Inbox_NewWorkflowFailureRollsBackEventTaskAndFlow()
    {
        using var pm = await CreateAuthenticatedClientAsync("demo.pm");
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var departmentId = await GetDepartmentIdAsync(pm, "RND");
        var project = await CreateProjectAsync(pm, departmentId, "流程失败回滚项目");
        var definition = await PublishProjectWorkflowAsync(admin, departmentId, "UX004-ROLLBACK");
        var qaUserId = await GetUserIdAsync("demo.qa");
        var agents = await pm.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<AgentProfileDto>>>(
            "/api/v1/agents",
            ClientJsonOptions);
        var agent = agents!.Data!.First(item => item.AutonomyLevel != AgentAutonomyLevel.A1);
        var inbox = await ReceiveInboxEventAsync(
            pm,
            departmentId,
            "流程启动后确认失败",
            $"项目 {project.Name} 选择非项目成员触发事务回滚。");
        await AnalyzeInboxEventAsync(pm, inbox.Id, agent.Id);

        var failed = await SendConfirmInboxEventAndStartFlowAsync(
            admin,
            inbox.Id,
            new ConfirmInboxEventRequest(
                Guid.Parse(project.Id),
                qaUserId,
                DateTimeOffset.UtcNow.AddDays(3),
                "P1",
                "此请求应整体回滚",
                null,
                true,
                definition.WorkflowDefinitionId,
                definition.WorkflowVersionId));
        failed.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await CountProjectTasksByEventAsync(Guid.Parse(inbox.Id))).ShouldBe(0);
        (await CountWorkflowInstancesByProjectAsync(Guid.Parse(project.Id))).ShouldBe(0);

        using var freshReader = await CreateAuthenticatedClientAsync("demo.pm");
        var freshEvent = await freshReader.Client.GetFromJsonAsync<ApiResponse<InboxEventDto>>(
            $"/api/v1/inbox/{inbox.Id}",
            ClientJsonOptions);
        freshEvent!.Data!.Status.ShouldBe("AwaitingConfirmation");
        var detail = await freshReader.Client.GetFromJsonAsync<ApiResponse<ProjectDetailDto>>(
            $"/api/v1/projects/{project.Id}",
            ClientJsonOptions);
        detail!.Data!.Tasks.ShouldBeEmpty();
        detail.Data.RelatedWorkflows.ShouldBeEmpty();
        detail.Data.Relations.ShouldNotContain(item =>
            item.EntityType == "InboxEvent" && item.EntityId == inbox.Id);
    }

    [SqlIntegrationFact]
    public async Task Inbox_ConfirmationLinksAnExistingFlowWithoutStartingAnother()
    {
        using var pm = await CreateAuthenticatedClientAsync("demo.pm");
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var departmentId = await GetDepartmentIdAsync(pm, "RND");
        var project = await CreateProjectAsync(pm, departmentId, "关联已有流程项目");
        var researcherId = await GetUserIdAsync("demo.researcher");
        await AddProjectMemberAsync(pm, project.Id, researcherId, "Contributor");
        var definition = await PublishProjectWorkflowAsync(admin, departmentId, "UX004-EXISTING");
        var existingWorkflow = await StartProjectWorkflowAsync(admin, definition, project.Id);
        var agents = await pm.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<AgentProfileDto>>>(
            "/api/v1/agents",
            ClientJsonOptions);
        var agent = agents!.Data!.First(item => item.AutonomyLevel != AgentAutonomyLevel.A1);
        var inbox = await ReceiveInboxEventAsync(
            pm,
            departmentId,
            "关联已有流程",
            $"项目 {project.Name} 确认后关联已有流程。");
        await AnalyzeInboxEventAsync(pm, inbox.Id, agent.Id);

        var confirmation = await ConfirmInboxEventAndStartFlowAsync(
            admin,
            inbox.Id,
            new ConfirmInboxEventRequest(
                Guid.Parse(project.Id),
                researcherId,
                DateTimeOffset.UtcNow.AddDays(3),
                "P1",
                "关联已有流程并创建任务",
                Guid.Parse(existingWorkflow.Id),
                true));
        confirmation.Workflow.ShouldNotBeNull();
        confirmation.Workflow!.Id.ShouldBe(existingWorkflow.Id);
        confirmation.Task!.WorkflowInstanceId.ShouldBe(existingWorkflow.Id);
        (await CountWorkflowInstancesByProjectAsync(Guid.Parse(project.Id))).ShouldBe(1);
    }

    [SqlIntegrationFact]
    public async Task Inbox_ConfirmationRejectsMissingPermissionAndCrossProjectAssignee()
    {
        using var pm = await CreateAuthenticatedClientAsync("demo.pm");
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        using var researcher = await CreateAuthenticatedClientAsync("demo.researcher");
        var departmentId = await GetDepartmentIdAsync(pm, "RND");
        var project = await CreateProjectAsync(pm, departmentId, "确认权限边界项目");
        var qaUserId = await GetUserIdAsync("demo.qa");
        var agents = await pm.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<AgentProfileDto>>>(
            "/api/v1/agents",
            ClientJsonOptions);
        var agent = agents!.Data!.First(item => item.AutonomyLevel != AgentAutonomyLevel.A1);
        var inbox = await ReceiveInboxEventAsync(
            pm,
            departmentId,
            "人工确认权限边界",
            $"项目 {project.Name} 的确认权限。");
        await AnalyzeInboxEventAsync(pm, inbox.Id, agent.Id);

        var request = new ConfirmInboxEventRequest(
            Guid.Parse(project.Id),
            qaUserId,
            DateTimeOffset.UtcNow.AddDays(2),
            "P1",
            "跨项目负责人应被拒绝",
            null,
            true);
        var missingDeadline = await SendConfirmInboxEventAndStartFlowAsync(
            pm,
            inbox.Id,
            request with { DueAt = null });
        missingDeadline.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var missingAssignee = await SendConfirmInboxEventAndStartFlowAsync(
            pm,
            inbox.Id,
            request with { AssigneeId = null });
        missingAssignee.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var deniedAssignee = await SendConfirmInboxEventAndStartFlowAsync(pm, inbox.Id, request);
        deniedAssignee.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var deniedPermission = await SendConfirmInboxEventAndStartFlowAsync(
            researcher,
            inbox.Id,
            request);
        deniedPermission.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await CountProjectTasksByEventAsync(Guid.Parse(inbox.Id))).ShouldBe(0);

        var qaDepartmentId = await GetDepartmentIdAsync(admin, "QA");
        var qaProject = await CreateProjectAsync(admin, qaDepartmentId, "跨部门确认边界项目");
        var crossDepartmentEvent = await ReceiveInboxEventAsync(
            pm,
            departmentId,
            "跨部门人工确认边界",
            "研发收件箱事项不能直接派发到质量部门项目。");
        await AnalyzeInboxEventAsync(pm, crossDepartmentEvent.Id, agent.Id);
        var crossDepartment = await SendConfirmInboxEventAndStartFlowAsync(
            pm,
            crossDepartmentEvent.Id,
            new ConfirmInboxEventRequest(
                Guid.Parse(qaProject.Id),
                await GetUserIdAsync("demo.pm"),
                DateTimeOffset.UtcNow.AddDays(2),
                "P1",
                "跨部门请求应被拒绝",
                null,
                true));
        crossDepartment.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await CountProjectTasksByEventAsync(Guid.Parse(crossDepartmentEvent.Id))).ShouldBe(0);
    }

    [SqlIntegrationFact]
    public async Task McpConfirmationView_IsBusinessReadableAndRequiresDecisionComment()
    {
        using var pm = await CreateAuthenticatedClientAsync("demo.pm");
        var departmentId = await GetDepartmentIdAsync(pm, "RND");
        var project = await CreateProjectAsync(pm, departmentId, "MCP业务确认视图项目");
        var agents = await pm.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<AgentProfileDto>>>(
            "/api/v1/agents",
            ClientJsonOptions);
        var agent = agents!.Data!.First(item => item.AutonomyLevel != AgentAutonomyLevel.A1);
        var inbox = await ReceiveInboxEventAsync(
            pm,
            departmentId,
            "MCP业务确认资源",
            $"项目 {project.Name} 创建任务后由智能体请求完成。");
        await AnalyzeInboxEventAsync(pm, inbox.Id, agent.Id);
        var confirmation = await ConfirmInboxEventAndStartFlowAsync(
            pm,
            inbox.Id,
            new ConfirmInboxEventRequest(
                Guid.Parse(project.Id),
                await GetUserIdAsync("demo.pm"),
                DateTimeOffset.UtcNow.AddDays(2),
                "P1",
                "MCP 业务确认任务",
                null,
                true));

        await using var scope = _factory.Services.CreateAsyncScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var mcpConfirmations = scope.ServiceProvider.GetRequiredService<IMcpHumanConfirmationService>();
        var principal = await userRepository.GetPrincipalByUserNameAsync("demo.pm", CancellationToken.None)
            ?? throw new InvalidOperationException("demo.pm is missing.");
        var mcpRequest = await mcpConfirmations.RequestAsync(
            new McpConfirmationRequest(
                principal.UserId,
                principal.OrganizationId,
                "ux004b-business-client",
                "aidt_complete_project_task",
                "ProjectTask",
                confirmation.Task!.Id,
                Convert.ToHexString(SHA256.HashData(Guid.NewGuid().ToByteArray())),
                "完成项目任务"),
            CancellationToken.None);

        var views = await pm.Client.GetFromJsonAsync<
            ApiResponse<IReadOnlyList<McpHumanConfirmationViewDto>>>(
            "/api/v1/mcp/confirmation-views/me",
            ClientJsonOptions);
        var view = views!.Data!.Single(item => item.Id == mcpRequest.Id);
        view.ActionTitle.ShouldBe("完成项目任务");
        view.ResourceDisplayName.ShouldBe("MCP 业务确认任务");
        view.ProjectName.ShouldBe(project.Name);
        view.Technical.ClientId.ShouldBe("ux004b-business-client");
        view.Technical.RequestHash.ShouldNotBeNullOrWhiteSpace();

        var blankConfirm = await SendMcpDecisionAsync(pm, view.Id, approve: true, comment: null);
        blankConfirm.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var blankReject = await SendMcpDecisionAsync(pm, view.Id, approve: false, comment: "  ");
        blankReject.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var approved = await SendMcpDecisionAsync(pm, view.Id, approve: true, comment: "核对任务和项目后通过");
        approved.StatusCode.ShouldBe(HttpStatusCode.OK);
        var duplicateDecision = await SendMcpDecisionAsync(
            pm,
            view.Id,
            approve: true,
            comment: "重复确认应被拒绝");
        duplicateDecision.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var rejectRequest = await mcpConfirmations.RequestAsync(
            new McpConfirmationRequest(
                principal.UserId,
                principal.OrganizationId,
                "ux004b-business-client",
                "aidt_complete_project_task",
                "ProjectTask",
                confirmation.Task.Id,
                Convert.ToHexString(SHA256.HashData(Guid.NewGuid().ToByteArray())),
                "再次处理项目任务"),
            CancellationToken.None);
        var rejected = await SendMcpDecisionAsync(
            pm,
            rejectRequest.Id,
            approve: false,
            comment: "业务依据不足，拒绝");
        rejected.StatusCode.ShouldBe(HttpStatusCode.OK);
        var rejectedPayload = (await rejected.Content.ReadFromJsonAsync<
            ApiResponse<McpHumanConfirmationDto>>(ClientJsonOptions))!.Data!;
        rejectedPayload.Status.ShouldBe("Rejected");

        var expiredRequest = await mcpConfirmations.RequestAsync(
            new McpConfirmationRequest(
                principal.UserId,
                principal.OrganizationId,
                "ux004b-business-client",
                "aidt_complete_project_task",
                "ProjectTask",
                confirmation.Task.Id,
                Convert.ToHexString(SHA256.HashData(Guid.NewGuid().ToByteArray())),
                "过期确认"),
            CancellationToken.None);
        var db = scope.ServiceProvider.GetRequiredService<AidtDbContext>();
        await db.McpHumanConfirmations
            .Where(item => item.Id == expiredRequest.Id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    item => item.ExpiresAt,
                    DateTimeOffset.UtcNow.AddMinutes(-1)));
        var viewsAfterExpiry = await pm.Client.GetFromJsonAsync<
            ApiResponse<IReadOnlyList<McpHumanConfirmationViewDto>>>(
            "/api/v1/mcp/confirmation-views/me",
            ClientJsonOptions);
        viewsAfterExpiry!.Data!.Single(item => item.Id == expiredRequest.Id).Status.ShouldBe("Expired");
    }

    [SqlIntegrationFact]
    public async Task ProjectStatusTransitions_PersistVersionProgressAndCompletionGuards()
    {
        using var pm = await CreateAuthenticatedClientAsync("demo.pm");
        var departmentId = await GetDepartmentIdAsync(pm, "RND");
        var project = await CreateProjectAsync(pm, departmentId, "项目状态持久化项目");
        var activeVersion = project.Version;

        var blocked = await ChangeProjectStatusAsync(pm, project.Id, activeVersion, "Blocked");
        blocked.Status.ShouldBe("Blocked");
        var blockedReRead = await GetProjectAsync(pm, project.Id);
        blockedReRead.Status.ShouldBe("Blocked");
        blockedReRead.Version.ShouldBeGreaterThan(activeVersion);

        var active = await ChangeProjectStatusAsync(pm, project.Id, blockedReRead.Version, "Active");
        active.Status.ShouldBe("Active");
        var activeReRead = await GetProjectAsync(pm, project.Id);
        activeReRead.Status.ShouldBe("Active");
        activeReRead.Version.ShouldBeGreaterThan(blockedReRead.Version);

        var awaiting = await ChangeProjectStatusAsync(
            pm,
            project.Id,
            activeReRead.Version,
            "AwaitingAcceptance");
        awaiting.Status.ShouldBe("AwaitingAcceptance");
        var awaitingReRead = await GetProjectAsync(pm, project.Id);
        awaitingReRead.Status.ShouldBe("AwaitingAcceptance");

        var agents = await pm.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<AgentProfileDto>>>(
            "/api/v1/agents",
            ClientJsonOptions);
        var agent = agents!.Data!.First(item => item.AutonomyLevel != AgentAutonomyLevel.A1);
        var inbox = await ReceiveInboxEventAsync(
            pm,
            departmentId,
            "完成保护项目任务",
            $"项目 {project.Name} 需完成未结任务。");
        await AnalyzeInboxEventAsync(pm, inbox.Id, agent.Id);
        var confirmation = await ConfirmInboxEventAsync(
            pm,
            inbox.Id,
            project.Id,
            $"project-status-task-{Guid.NewGuid():N}");
        confirmation.Task.ShouldNotBeNull();

        var blockedCompletion = await ChangeProjectStatusRawAsync(
            pm,
            project.Id,
            awaitingReRead.Version,
            "Completed");
        blockedCompletion.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        await CompleteProjectTaskAsync(pm, confirmation.Task!.Id, confirmation.Task.Version);
        var latest = await GetProjectAsync(pm, project.Id);
        var completed = await ChangeProjectStatusAsync(pm, project.Id, latest.Version, "Completed");
        completed.Status.ShouldBe("Completed");
        completed.ProgressPercent.ShouldBe(100);
        var completedReRead = await GetProjectAsync(pm, project.Id);
        completedReRead.Status.ShouldBe("Completed");
        completedReRead.ProgressPercent.ShouldBe(100);
        completedReRead.Version.ShouldBeGreaterThan(latest.Version);
    }

    [SqlIntegrationFact]
    public async Task ProjectOperations_EditMembersCreateTasksLinkEvidenceAndPersist()
    {
        using var pm = await CreateAuthenticatedClientAsync("demo.pm");
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var departmentId = await GetDepartmentIdAsync(pm, "RND");
        var researcherId = await GetUserIdAsync("demo.researcher");
        var qualityUserId = await GetUserIdAsync("demo.qa");
        var project = await CreateProjectAsync(pm, departmentId, "项目操作闭环集成测试");
        await AddProjectMemberAsync(pm, project.Id, researcherId, "Contributor");

        var dueAt = new DateTimeOffset(2026, 12, 18, 18, 0, 0, TimeSpan.FromHours(8));
        var taskRequest = new CreateProjectTaskRequest(
            researcherId,
            "完成项目操作闭环验收",
            "核对编辑、成员、任务、证据、进度和状态刷新后保持一致。",
            dueAt,
            "P1");
        var idempotencyKey = $"project-task-create-{Guid.NewGuid():N}";
        var created = await CreateProjectTaskAsync(pm, project.Id, taskRequest, idempotencyKey);
        created.Status.ShouldBe("Pending");
        created.AssigneeId.ShouldBe(researcherId.ToString("D"));
        created.DueAt!.Value.ToUniversalTime().ShouldBe(dueAt.ToUniversalTime());

        var replay = await CreateProjectTaskAsync(pm, project.Id, taskRequest, idempotencyKey);
        replay.Id.ShouldBe(created.Id);
        var changedRequest = taskRequest with { Title = "同一幂等键的错误任务" };
        using var changedResponse = await SendCreateProjectTaskAsync(
            pm,
            project.Id,
            changedRequest,
            idempotencyKey);
        changedResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var nonMemberRequest = taskRequest with { AssigneeId = qualityUserId };
        using var nonMemberResponse = await SendCreateProjectTaskAsync(
            pm,
            project.Id,
            nonMemberRequest,
            $"project-task-nonmember-{Guid.NewGuid():N}");
        nonMemberResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var qualityMember = await AddProjectMemberAsync(pm, project.Id, qualityUserId, "Viewer");
        qualityMember.ProjectRole.ShouldBe("Viewer");
        var qualityContributor = await AddProjectMemberAsync(pm, project.Id, qualityUserId, "Contributor");
        qualityContributor.ProjectRole.ShouldBe("Contributor");
        using var leadBypass = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/projects/{project.Id}/members")
        {
            Content = JsonContent.Create(new AddProjectMemberRequest(qualityUserId, "Lead", true))
        };
        leadBypass.Headers.Add("X-CSRF-TOKEN", pm.CsrfToken);
        (await pm.Client.SendAsync(leadBypass)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var removed = await RemoveProjectMemberAsync(pm, project.Id, qualityUserId);
        removed.IsActive.ShouldBeFalse();

        var fileResponse = await UploadFileAsync(
            pm,
            departmentId,
            Encoding.ASCII.GetBytes("%PDF-1.7\nPMO-003 evidence"),
            $"pmo003-{Guid.NewGuid():N}.pdf",
            "application/pdf",
            "Internal",
            Guid.Parse(project.Id));
        fileResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var attachment = (await fileResponse.Content.ReadFromJsonAsync<ApiResponse<FileAttachmentDto>>(
            ClientJsonOptions))!.Data!;

        using var minutesRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/minutes")
        {
            Content = JsonContent.Create(new RegisterMinutesRequest(
                departmentId,
                "PMO-003 项目操作评审",
                DateTimeOffset.UtcNow,
                "[10:00:00] 项目经理：确认项目任务和交付证据。"))
        };
        minutesRequest.Headers.Add("Idempotency-Key", $"pmo003-minutes-{Guid.NewGuid():N}");
        minutesRequest.Headers.Add("X-CSRF-TOKEN", pm.CsrfToken);
        var minutesResponse = await pm.Client.SendAsync(minutesRequest);
        minutesResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var minutes = (await minutesResponse.Content.ReadFromJsonAsync<ApiResponse<MinutesSourceDto>>(
            ClientJsonOptions))!.Data!;

        var sampleTypeId = await GetObjectTypeIdAsync(admin, "CUSTOMER_SAMPLE");
        var objectResponse = await CreateObjectAsync(
            admin,
            $"pmo003-object-{Guid.NewGuid():N}",
            new CreateBusinessObjectRequest(
                Guid.Parse(sampleTypeId),
                $"PMO003-{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
                new Dictionary<string, string?>
                {
                    ["Customer"] = "项目操作闭环客户",
                    ["Product"] = "项目证据样品",
                    ["Requirement"] = "关联项目证据"
                },
                departmentId));
        objectResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var businessObject = (await objectResponse.Content.ReadFromJsonAsync<ApiResponse<BusinessObjectDto>>(
            ClientJsonOptions))!.Data!;
        var definitions = await admin.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<WorkflowDefinitionDto>>>(
            "/api/v1/workflow-definitions",
            ClientJsonOptions);
        var definition = definitions!.Data!.First(item => item.Code == "CUSTOMER_SAMPLE_TO_PRODUCTION");
        using var startRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/workflow-instances")
        {
            Content = JsonContent.Create(new StartWorkflowInstanceRequest(
                Guid.Parse(definition.Id),
                Guid.Parse(definition.VersionId),
                Guid.Parse(businessObject.Id),
                $"pmo003-workflow-{Guid.NewGuid():N}"))
        };
        startRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var startResponse = await admin.Client.SendAsync(startRequest);
        startResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var instance = (await startResponse.Content.ReadFromJsonAsync<ApiResponse<WorkflowInstanceDto>>(
            ClientJsonOptions))!.Data!;

        await AddProjectRelationAsync(pm, project.Id, "Attachment", attachment.Id, "SupportingEvidence");
        await AddProjectRelationAsync(pm, project.Id, "MinutesSource", minutes.Id, "MeetingEvidence");
        await AddProjectRelationAsync(pm, project.Id, "WorkflowInstance", instance.Id, "RelatedWorkflow");
        await AddProjectRelationAsync(pm, project.Id, "BusinessObject", businessObject.Id, "RelatedEvidence");

        var current = await GetProjectAsync(admin, project.Id);
        var updated = await UpdateProjectAsync(
            admin,
            project.Id,
            current.Version,
            new UpdateProjectRequest(
                current.Version,
                "项目操作闭环已编辑",
                researcherId,
                new DateTimeOffset(2026, 11, 2, 0, 0, 0, TimeSpan.FromHours(8)),
                new DateTimeOffset(2026, 12, 31, 23, 59, 59, TimeSpan.FromHours(8)),
                "编辑后的项目目标用于验证服务端持久化。",
                "任务执行",
                "Sensitive"));
        updated.Name.ShouldBe("项目操作闭环已编辑");
        updated.OwnerUserId.ShouldBe(researcherId.ToString("D"));
        updated.Objective.ShouldBe("编辑后的项目目标用于验证服务端持久化。");

        using var researcher = await CreateAuthenticatedClientAsync("demo.researcher");
        var afterEdit = await researcher.Client.GetFromJsonAsync<ApiResponse<ProjectDetailDto>>(
            $"/api/v1/projects/{project.Id}",
            ClientJsonOptions);
        afterEdit!.Data!.Project.OwnerUserId.ShouldBe(researcherId.ToString("D"));
        afterEdit.Data.Project.ConfidentialityLevel.ShouldBe("Sensitive");
        afterEdit.Data.Members.Count(item => item.IsLead && item.IsActive).ShouldBe(1);
        afterEdit.Data.Members.Single(item => item.UserId == researcherId.ToString("D")).IsLead.ShouldBeTrue();
        afterEdit.Data.Tasks.ShouldContain(item => item.Id == created.Id);
        afterEdit.Data.Relations.ShouldContain(item =>
            item.EntityType == "Attachment" && item.EntityId == attachment.Id);
        afterEdit.Data.Relations.ShouldContain(item =>
            item.EntityType == "MinutesSource" && item.EntityId == minutes.Id);
        afterEdit.Data.Relations.ShouldContain(item =>
            item.EntityType == "WorkflowInstance" && item.EntityId == instance.Id);
        afterEdit.Data.Relations.ShouldContain(item =>
            item.EntityType == "BusinessObject" && item.EntityId == businessObject.Id);

        var completedTask = await CompleteProjectTaskAsync(researcher, created.Id, created.Version);
        completedTask.Status.ShouldBe("Completed");
        var afterTask = await GetProjectAsync(admin, project.Id);
        afterTask.ProgressPercent.ShouldBe(100);
        var awaiting = await ChangeProjectStatusAsync(
            admin,
            project.Id,
            afterTask.Version,
            "AwaitingAcceptance");
        var completed = await ChangeProjectStatusAsync(
            admin,
            project.Id,
            awaiting.Version,
            "Completed");

        using var freshReader = await CreateAuthenticatedClientAsync("demo.pm");
        var persisted = await freshReader.Client.GetFromJsonAsync<ApiResponse<ProjectDetailDto>>(
            $"/api/v1/projects/{project.Id}",
            ClientJsonOptions);
        persisted!.Data!.Project.Name.ShouldBe("项目操作闭环已编辑");
        persisted.Data.Project.Status.ShouldBe("Completed");
        persisted.Data.Project.ProgressPercent.ShouldBe(100);
        persisted.Data.Project.Version.ShouldBe(completed.Version);
        persisted.Data.Tasks.Single(item => item.Id == created.Id).Status.ShouldBe("Completed");
        persisted.Data.Relations.Count.ShouldBeGreaterThanOrEqualTo(4);
    }

    [SqlIntegrationFact]
    public async Task ProjectFileUpload_AutoLinksProjectEvidenceAndPreservesOtherObjects()
    {
        using var pm = await CreateAuthenticatedClientAsync("demo.pm");
        var departmentId = await GetDepartmentIdAsync(pm, "RND");
        var project = await CreateProjectAsync(pm, departmentId, "项目文件自动证据项目");
        var projectId = Guid.Parse(project.Id);

        var first = await UploadFileAsync(
            pm,
            departmentId,
            Encoding.ASCII.GetBytes("%PDF-1.7\nproject evidence 1"),
            "project-auto-evidence-1.pdf",
            "application/pdf",
            "Internal",
            projectId);
        first.StatusCode.ShouldBe(HttpStatusCode.Created);
        var firstAttachment = (await first.Content.ReadFromJsonAsync<ApiResponse<FileAttachmentDto>>(
            ClientJsonOptions))!.Data!;

        var second = await UploadFileAsync(
            pm,
            departmentId,
            Encoding.ASCII.GetBytes("%PDF-1.7\nproject evidence retry"),
            "project-auto-evidence-2.pdf",
            "application/pdf",
            "Internal",
            projectId);
        second.StatusCode.ShouldBe(HttpStatusCode.Created);
        var secondAttachment = (await second.Content.ReadFromJsonAsync<ApiResponse<FileAttachmentDto>>(
            ClientJsonOptions))!.Data!;

        using var freshReader = await CreateAuthenticatedClientAsync("demo.pm");
        var detail = await freshReader.Client.GetFromJsonAsync<ApiResponse<ProjectDetailDto>>(
            $"/api/v1/projects/{project.Id}",
            ClientJsonOptions);
        detail!.Data!.Files.ShouldContain(item => item.Id == firstAttachment.Id);
        detail.Data.Files.ShouldContain(item => item.Id == secondAttachment.Id);
        detail.Data.Relations.ShouldContain(item =>
            item.EntityType == "Attachment"
            && item.EntityId == firstAttachment.Id
            && item.RelationCode == "SupportingEvidence");

        (await CountProjectAttachmentRelationsAsync(projectId, Guid.Parse(firstAttachment.Id))).ShouldBe(1);
        (await CountProjectAttachmentRelationsAsync(projectId, Guid.Parse(secondAttachment.Id))).ShouldBe(1);
        (await CountDuplicateProjectAttachmentRelationsAsync(projectId)).ShouldBe(0);

        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var objectTypes = await admin.Client.GetFromJsonAsync<ApiResponse<PagedResult<ObjectTypeDto>>>(
            "/api/v1/object-types?pageSize=100");
        var sampleType = objectTypes!.Data!.Items.Single(item => item.Code == "CUSTOMER_SAMPLE");
        var objectCode = $"NONPROJ-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
        var nonProject = await CreateObjectAsync(
            admin,
            $"non-project-file-{Guid.NewGuid():N}",
            new CreateBusinessObjectRequest(
                Guid.Parse(sampleType.Id),
                objectCode,
                new Dictionary<string, string?>
                {
                    ["Customer"] = "非项目对象",
                    ["Product"] = "普通附件",
                    ["Requirement"] = "保持原有 AttachmentLink 行为"
                },
                departmentId));
        nonProject.StatusCode.ShouldBe(HttpStatusCode.Created);
        var nonProjectObject = (await nonProject.Content.ReadFromJsonAsync<ApiResponse<BusinessObjectDto>>(
            ClientJsonOptions))!.Data!;
        var ordinary = await UploadFileAsync(
            admin,
            departmentId,
            Encoding.ASCII.GetBytes("%PDF-1.7\nordinary attachment"),
            "ordinary-object.pdf",
            "application/pdf",
            "Internal",
            Guid.Parse(nonProjectObject.Id));
        ordinary.StatusCode.ShouldBe(HttpStatusCode.Created);
        var ordinaryAttachment = (await ordinary.Content.ReadFromJsonAsync<ApiResponse<FileAttachmentDto>>(
            ClientJsonOptions))!.Data!;
        ordinaryAttachment.Links.ShouldContain(link =>
            link.EntityType == "BusinessObject" && link.EntityId == nonProjectObject.Id);
        (await CountProjectAttachmentRelationsAsync(Guid.Parse(nonProjectObject.Id), Guid.Parse(ordinaryAttachment.Id)))
            .ShouldBe(0);
    }

    [SqlIntegrationFact]
    public async Task Inbox_FromMinutesActionItem_TracesSpeakerTimestampAndTranscript()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var departmentId = await GetDepartmentIdAsync(admin, "RND");
        var profiles = await admin.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<AgentProfileDto>>>(
            "/api/v1/agents",
            ClientJsonOptions);
        var profileRows = profiles!.Data!;
        var businessAgent = profileRows.Single(item => item.Name.Contains("业务协同"));
        var eventAgent = profileRows.First(item => item.AutonomyLevel != AgentAutonomyLevel.A1);
        var transcript =
            "会议：样品评审\n" +
            "[10:21:08] 研发工程师：请质量工程师在 2026-11-20 前完成耐候样品测试确认。\n" +
            "[10:23:16] 质量工程师：研发需补充测试原始数据。";
        using var registerRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/minutes")
        {
            Content = JsonContent.Create(new RegisterMinutesRequest(
                departmentId,
                "项目事件听记来源",
                DateTimeOffset.UtcNow,
                transcript))
        };
        registerRequest.Headers.Add("Idempotency-Key", $"inbox-minutes-{Guid.NewGuid():N}");
        registerRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var registerResponse = await admin.Client.SendAsync(registerRequest);
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var source = (await registerResponse.Content.ReadFromJsonAsync<ApiResponse<MinutesSourceDto>>(
            ClientJsonOptions))!.Data!;

        using var analyzeRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/minutes/{source.Id}/actions/analyze")
        {
            Content = JsonContent.Create(new AnalyzeMinutesRequest(Guid.Parse(businessAgent.Id)))
        };
        analyzeRequest.Headers.Add("Idempotency-Key", $"inbox-minutes-analyze-{Guid.NewGuid():N}");
        analyzeRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var analyzeResponse = await admin.Client.SendAsync(analyzeRequest);
        analyzeResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var run = (await analyzeResponse.Content.ReadFromJsonAsync<ApiResponse<AgentRunDto>>(
            ClientJsonOptions))!.Data!;

        using var confirmRunRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/agents/runs/{run.Id}/actions/confirm")
        {
            Content = JsonContent.Create(new ConfirmAgentRunRequest(Guid.NewGuid()))
        };
        confirmRunRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        (await admin.Client.SendAsync(confirmRunRequest)).StatusCode.ShouldBe(HttpStatusCode.OK);
        using var executeRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/agents/runs/{run.Id}/actions/execute");
        executeRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var executeResponse = await admin.Client.SendAsync(executeRequest);
        executeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var execution = (await executeResponse.Content.ReadFromJsonAsync<
            ApiResponse<AgentExecutionResultDto>>(ClientJsonOptions))!.Data!;
        var actionItem = execution.ActionItems[0];

        using var pm = await CreateAuthenticatedClientAsync("demo.pm");
        var project = await CreateProjectAsync(pm, departmentId, "听记行动项转项目任务");
        var inbox = await ReceiveInboxEventAsync(
            pm,
            departmentId,
            "听记行动项入收件箱",
            string.Empty,
            "MinutesActionItem",
            actionItem.Id);
        var analyzed = await AnalyzeInboxEventAsync(pm, inbox.Id, eventAgent.Id);
        analyzed.Suggestion.ShouldNotBeNull();
        var confirmation = await ConfirmInboxEventAsync(
            pm,
            inbox.Id,
            project.Id,
            $"inbox-minutes-confirm-{Guid.NewGuid():N}");
        confirmation.Task.ShouldNotBeNull();

        var trace = await pm.Client.GetFromJsonAsync<ApiResponse<InboxTaskTraceDto>>(
            $"/api/v1/project-tasks/{confirmation.Task!.Id}/trace",
            ClientJsonOptions);
        trace!.Data!.Event.SourceType.ShouldBe("MinutesActionItem");
        trace.Data.Event.RawText.ShouldContain("发言人");
        trace.Data.Event.RawText.ShouldContain("时间戳");
        trace.Data.Event.RawText.ShouldContain("10:21:08");
        trace.Data.Event.RawText.ShouldContain("耐候样品测试确认");
    }

    [SqlIntegrationFact]
    public async Task HighRiskInboxEvent_CreatesSuggestionOnlyUntilHumanConfirmation()
    {
        using var pm = await CreateAuthenticatedClientAsync("demo.pm");
        var departmentId = await GetDepartmentIdAsync(pm, "RND");
        var project = await CreateProjectAsync(pm, departmentId, "财务与质量边界演示");
        var agents = await pm.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<AgentProfileDto>>>(
            "/api/v1/agents",
            ClientJsonOptions);
        var agent = agents!.Data!.First(item => item.AutonomyLevel != AgentAutonomyLevel.A1);
        var inbox = await ReceiveInboxEventAsync(
            pm,
            departmentId,
            "高风险事项边界",
            "请直接完成财务付款并对本批次办理质量放行。");

        var analyzed = await AnalyzeInboxEventAsync(pm, inbox.Id, agent.Id);
        analyzed.Suggestion!.RiskLevel.ShouldBe("High");
        analyzed.Suggestion.RequiresHumanBoundary.ShouldBeTrue();
        analyzed.Suggestion.Status.ShouldBe("HighRisk");
        analyzed.Suggestion.BoundaryReason!.ShouldContain("付款");
        (await CountProjectTasksByEventAsync(Guid.Parse(inbox.Id))).ShouldBe(0);
        (await CountWorkflowInstancesByProjectAsync(Guid.Parse(project.Id))).ShouldBe(0);

        var confirmation = await ConfirmInboxEventAsync(
            pm,
            inbox.Id,
            project.Id,
            $"high-risk-{Guid.NewGuid():N}");
        confirmation.Task.ShouldNotBeNull();
        (await CountWorkflowInstancesByProjectAsync(Guid.Parse(project.Id))).ShouldBe(0);
    }

    [SqlIntegrationFact]
    public async Task ProjectDetail_TracesBusinessObjectWorkflowTasksAndAudit()
    {
        using var pm = await CreateAuthenticatedClientAsync("demo.pm");
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var departmentId = await GetDepartmentIdAsync(pm, "RND");
        var project = await CreateProjectAsync(pm, departmentId, "证据链完整项目");
        var sampleTypeId = await GetObjectTypeIdAsync(admin, "CUSTOMER_SAMPLE");
        var objectCode = $"PMO-EVID-{Guid.NewGuid():N}"[..22].ToUpperInvariant();
        var createObjectRequest = new CreateBusinessObjectRequest(
            Guid.Parse(sampleTypeId),
            objectCode,
            new Dictionary<string, string?>
            {
                ["Customer"] = "证据链测试客户",
                ["Product"] = "耐候苯丙样品",
                ["Requirement"] = "项目关联流程证据",
                ["TrialRequired"] = "Yes"
            },
            departmentId);
        var createObjectResponse = await CreateObjectAsync(
            admin,
            $"pmo-object-{Guid.NewGuid():N}",
            createObjectRequest);
        createObjectResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var businessObject = (await createObjectResponse.Content.ReadFromJsonAsync<
            ApiResponse<BusinessObjectDto>>(ClientJsonOptions))!.Data!;

        var definitions = await admin.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<WorkflowDefinitionDto>>>(
            "/api/v1/workflow-definitions",
            ClientJsonOptions);
        var definition = definitions!.Data!.First(item => item.Code == "CUSTOMER_SAMPLE_TO_PRODUCTION");
        using var startRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/workflow-instances")
        {
            Content = JsonContent.Create(new StartWorkflowInstanceRequest(
                Guid.Parse(definition.Id),
                Guid.Parse(definition.VersionId),
                Guid.Parse(businessObject.Id),
                $"pmo-workflow-{Guid.NewGuid():N}"))
        };
        startRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var startResponse = await admin.Client.SendAsync(startRequest);
        startResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var instance = (await startResponse.Content.ReadFromJsonAsync<ApiResponse<WorkflowInstanceDto>>(
            ClientJsonOptions))!.Data!;

        await AddProjectRelationAsync(pm, project.Id, "BusinessObject", businessObject.Id, "CustomerSample");
        await AddProjectRelationAsync(pm, project.Id, "WorkflowInstance", instance.Id, "PrimaryFlow");
        var detail = await pm.Client.GetFromJsonAsync<ApiResponse<ProjectDetailDto>>(
            $"/api/v1/projects/{project.Id}",
            ClientJsonOptions);
        detail!.Data!.RelatedBusinessObjects.ShouldContain(item => item.Id == businessObject.Id);
        detail.Data.RelatedWorkflows.ShouldContain(item => item.Instance.Id == instance.Id);
        detail.Data.AuditTimeline.ShouldContain(item => item.Action == "project.created");
        detail.Data.AuditTimeline.ShouldContain(item => item.Action == "project.relation_added");
    }

    [SqlIntegrationFact]
    public async Task ConfigurationCenter_PublishesExecutableWorkflowAndSupportsRollback()
    {
        using var pm = await CreateAuthenticatedClientAsync("demo.pm");
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var departmentId = await GetDepartmentIdAsync(pm, "RND");
        var configurationKey = $"CFG-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
        var draftRequest = new CreateWorkflowConfigurationRequest(
            configurationKey,
            "配置中心可执行流程验证",
            "验证草稿、校验、发布、回滚与 WorkflowVersion 联动",
            departmentId,
            "CUSTOMER_SAMPLE",
            "RD_CONFIRM",
            [
                new WorkflowConfigurationNodeRequest(
                    "RD_CONFIRM",
                    "研发确认",
                    "demo.researcher",
                    null,
                    CompleteOnApprove: true)
            ]);

        var draft = await CreateConfigurationAsync(
            admin,
            $"configuration-create-{Guid.NewGuid():N}",
            draftRequest);
        draft.State.ShouldBe("Draft");
        draft.VersionNumber.ShouldBe(1);

        var validation = await ValidateConfigurationAsync(
            admin,
            draft.Id,
            $"configuration-validate-{Guid.NewGuid():N}");
        validation.IsValid.ShouldBeTrue();
        validation.NodeCount.ShouldBe(1);

        var forbiddenPublish = await SendPublishConfigurationAsync(
            pm,
            draft.Id,
            $"configuration-publish-pm-{Guid.NewGuid():N}");
        forbiddenPublish.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var published = await PublishConfigurationAsync(
            admin,
            draft.Id,
            $"configuration-publish-{Guid.NewGuid():N}");
        published.State.ShouldBe("Published");
        published.PublishedWorkflowDefinitionId.ShouldNotBeNullOrWhiteSpace();
        published.PublishedWorkflowVersionId.ShouldNotBeNullOrWhiteSpace();

        var secondDraft = await CreateConfigurationAsync(
            admin,
            $"configuration-create-v2-{Guid.NewGuid():N}",
            draftRequest with
            {
                Name = "配置中心可执行流程验证 v2",
                Nodes =
                [
                    new WorkflowConfigurationNodeRequest(
                        "RD_CONFIRM",
                        "研发确认 v2",
                        "demo.researcher",
                        null,
                        CompleteOnApprove: true)
                ]
            });
        secondDraft.VersionNumber.ShouldBe(2);
        (await ValidateConfigurationAsync(
            admin,
            secondDraft.Id,
            $"configuration-validate-v2-{Guid.NewGuid():N}")).IsValid.ShouldBeTrue();
        var secondPublished = await PublishConfigurationAsync(
            admin,
            secondDraft.Id,
            $"configuration-publish-v2-{Guid.NewGuid():N}");
        secondPublished.State.ShouldBe("Published");

        var rolledBack = await RollbackConfigurationAsync(
            admin,
            draft.Id,
            $"configuration-rollback-{Guid.NewGuid():N}");
        rolledBack.State.ShouldBe("Published");
        rolledBack.PublishedWorkflowVersionId.ShouldBe(published.PublishedWorkflowVersionId);
        var definitionsAfterRollback = await admin.Client.GetFromJsonAsync<
            ApiResponse<IReadOnlyList<WorkflowDefinitionDto>>>(
            "/api/v1/workflow-definitions",
            ClientJsonOptions);
        definitionsAfterRollback!.Data!.ShouldContain(item =>
            item.Code == configurationKey && item.Name == draftRequest.Name);

        var typeId = await GetObjectTypeIdAsync(admin, "CUSTOMER_SAMPLE");
        var objectCode = $"CFG-OBJ-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
        var createObjectResponse = await CreateObjectAsync(
            admin,
            $"configuration-object-{Guid.NewGuid():N}",
            new CreateBusinessObjectRequest(
                Guid.Parse(typeId),
                objectCode,
                new Dictionary<string, string?>
                {
                    ["Customer"] = "配置中心验证客户",
                    ["Product"] = "配置中心验证样品",
                    ["Requirement"] = "验证配置发布后可执行",
                    ["TrialRequired"] = "No"
                },
                departmentId));
        createObjectResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var businessObject = (await createObjectResponse.Content.ReadFromJsonAsync<
            ApiResponse<BusinessObjectDto>>(ClientJsonOptions))!.Data!;

        using var startRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/workflow-instances")
        {
            Content = JsonContent.Create(new StartWorkflowInstanceRequest(
                Guid.Parse(rolledBack.PublishedWorkflowDefinitionId!),
                Guid.Parse(rolledBack.PublishedWorkflowVersionId!),
                Guid.Parse(businessObject.Id),
                $"configuration-workflow-{Guid.NewGuid():N}"))
        };
        startRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var startResponse = await admin.Client.SendAsync(startRequest);
        startResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var configurations = await admin.Client.GetFromJsonAsync<
            ApiResponse<PagedResult<WorkflowConfigurationDto>>>(
            $"/api/v1/configurations?configurationKey={configurationKey}",
            ClientJsonOptions);
        configurations!.Data!.Items.Count(item => item.State == "Published").ShouldBe(1);
        configurations.Data.Items.ShouldContain(item => item.Id == draft.Id && item.State == "Published");
        configurations.Data.Items.ShouldContain(item =>
            item.Id == secondDraft.Id && item.State == "Retired");
    }

    [SqlIntegrationFact]
    public async Task DepartmentWorkflowTemplate_FreezesDynamicFormAndRejectsInvalidSubmission()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var departmentId = await GetDepartmentIdAsync(admin, "RND");
        var adminUserId = await GetUserIdAsync("demo.admin");
        var objectTypeId = await GetObjectTypeIdAsync(admin, "CUSTOMER_SAMPLE");
        var objectCode = $"DEPT-WF-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
        var objectResponse = await CreateObjectAsync(
            admin,
            $"dept-wf-object-{Guid.NewGuid():N}",
            new CreateBusinessObjectRequest(
                Guid.Parse(objectTypeId),
                objectCode,
                new Dictionary<string, string?>
                {
                    ["Customer"] = "部门流程表单客户",
                    ["Product"] = "动态表单样品",
                    ["Requirement"] = "验证冻结节点表单",
                    ["TrialRequired"] = "No"
                },
                departmentId));
        objectResponse.StatusCode.ShouldBe(
            HttpStatusCode.Created,
            await objectResponse.Content.ReadAsStringAsync());
        var businessObject = (await objectResponse.Content.ReadFromJsonAsync<
            ApiResponse<BusinessObjectDto>>(ClientJsonOptions))!.Data!;

        var configurationKey = $"DEPT-WF-{Guid.NewGuid():N}"[..28].ToUpperInvariant();
        var agentsJson = await admin.Client.GetStringAsync(
            "/api/v1/agents/available?departmentCode=RND");
        using var agentsDocument = JsonDocument.Parse(agentsJson);
        var agentProfileId = agentsDocument.RootElement
            .GetProperty("data")
            .EnumerateArray()
            .First(item =>
                item.GetProperty("autonomyLevel").GetString() == "A2")
            .GetProperty("id")
            .GetString()!;
        var fields = new WorkflowFormFieldRequest[]
        {
            new("RECORD_NOTE", "现场记录", "Text", Required: true),
            new("REVIEWER", "复核人", "User", Required: true),
            new("DUE_DATE", "完成日期", "Date", Required: true),
            new("EVIDENCE", "证据附件", "Attachment", Required: true)
        };
        var node = new WorkflowConfigurationNodeRequest(
            "DEPARTMENT_REVIEW",
            "部门复核",
            "demo.admin",
            null,
            CompleteOnApprove: true,
            BusinessDescription: "确认记录、人员和期限，并提交可追溯证据。",
            SlaMinutes: 240,
            Deliverables: ["现场记录", "证据附件"],
            RelatedRoleCodes: ["rd-engineer"],
            RequiredAttachmentCount: 0,
            CompletionCondition: "记录与日期填写完整",
            Fields: fields,
            Actions:
            [
                new WorkflowNodeActionRequest(
                    "APPROVE",
                    "通过",
                    "Approve",
                    "确认进入完成状态")
            ],
            AiReview: new WorkflowAiReviewRequest(
                true,
                "CompletenessCheck",
                Guid.Parse(agentProfileId),
                ["RECORD_NOTE"],
                ["Attachment"],
                "SuggestionOnly"),
            Notification: new WorkflowNotificationRequest(true, 60, 120, "rd-engineer"));
        var createRequest = new CreateWorkflowConfigurationRequest(
            configurationKey,
            "研发部门节点表单验证",
            "验证节点字段、角色范围、附件、AI 规则模拟和冻结版本。",
            departmentId,
            "CUSTOMER_SAMPLE",
            "DEPARTMENT_REVIEW",
            [node],
            IsDefault: true,
            ApplicableRoleCodes: ["rd-engineer"],
            RiskLevel: "Low");
        var first = await CreateConfigurationAsync(
            admin,
            $"dept-wf-create-{Guid.NewGuid():N}",
            createRequest);
        await ValidateConfigurationAsync(
            admin,
            first.Id,
            $"dept-wf-validate-{Guid.NewGuid():N}");
        var publishedFirst = await PublishConfigurationAsync(
            admin,
            first.Id,
            $"dept-wf-publish-{Guid.NewGuid():N}");

        using var startRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/workflow-instances")
        {
            Content = JsonContent.Create(new StartWorkflowInstanceRequest(
                Guid.Parse(publishedFirst.PublishedWorkflowDefinitionId!),
                Guid.Parse(publishedFirst.PublishedWorkflowVersionId!),
                Guid.Parse(businessObject.Id),
                $"dept-wf-start-{Guid.NewGuid():N}"))
        };
        startRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var startResponse = await admin.Client.SendAsync(startRequest);
        startResponse.StatusCode.ShouldBe(
            HttpStatusCode.Created,
            await startResponse.Content.ReadAsStringAsync());
        var instance = (await startResponse.Content.ReadFromJsonAsync<
            ApiResponse<WorkflowInstanceDto>>(ClientJsonOptions))!.Data!;

        var tasksJson = await admin.Client.GetStringAsync(
            $"/api/v1/workflow-tasks?status=Pending&pageSize=100&includeTestData={businessObject.IsTestData}");
        using var tasksDocument = JsonDocument.Parse(tasksJson);
        var taskElement = tasksDocument.RootElement
            .GetProperty("data")
            .GetProperty("items")
            .EnumerateArray()
            .Single(item => item.GetProperty("workflowInstanceId").GetString() == instance.Id);
        var taskId = taskElement.GetProperty("id").GetString()!;
        var taskVersion = taskElement.GetProperty("version").GetInt64();
        taskElement.GetProperty("status").GetString().ShouldBe("Pending");
        var form = await admin.Client.GetFromJsonAsync<ApiResponse<WorkflowTaskFormDto>>(
            $"/api/v1/workflow-tasks/{taskId}/form");
        form!.Data!.Fields.Count.ShouldBe(4);
        form.Data.NodeName.ShouldBe("部门复核");
        form.Data.PermanentHumanBoundary.ShouldBeFalse();
        form.Data.AiReview.RegisteredAction.ShouldBe("CompletenessCheck");
        form.Data.AiReview.AgentProfileId.ShouldBe(Guid.Parse(agentProfileId));
        form.Data.FormDefinitionHash.ShouldNotBeNullOrWhiteSpace();

        using var aiReviewRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/workflow-tasks/{taskId}/actions/ai-review");
        aiReviewRequest.Headers.Add(
            "Idempotency-Key",
            $"dept-wf-ai-review-{Guid.NewGuid():N}");
        aiReviewRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var aiReviewResponse = await admin.Client.SendAsync(aiReviewRequest);
        aiReviewResponse.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            await aiReviewResponse.Content.ReadAsStringAsync());
        var aiResult = (await aiReviewResponse.Content.ReadFromJsonAsync<
            ApiResponse<WorkflowTaskAiReviewResultDto>>(ClientJsonOptions))!.Data!;
        aiResult.RegisteredAction.ShouldBe("CompletenessCheck");
        aiResult.Mode.ShouldBe("RuleSimulator");
        aiResult.ExternalModelCalled.ShouldBeFalse();
        aiResult.RequiresConfirmation.ShouldBeFalse();
        aiResult.GatewayCallId.ShouldNotBeNullOrWhiteSpace();
        aiResult.Suggestion.ShouldContain("规则模拟");

        using var draftRequest = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/v1/workflow-tasks/{taskId}/form-draft")
        {
            Content = JsonContent.Create(new SaveWorkflowTaskFormDraftRequest(
                taskVersion,
                """{"RECORD_NOTE":"第一版现场记录"}""",
                form.Data.FormDefinitionHash,
                $"dept-wf-form-draft-{Guid.NewGuid():N}"))
        };
        draftRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var draftResponse = await admin.Client.SendAsync(draftRequest);
        draftResponse.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            await draftResponse.Content.ReadAsStringAsync());
        var taskAfterDraft = (await draftResponse.Content.ReadFromJsonAsync<
            ApiResponse<WorkflowTaskDto>>(ClientJsonOptions))!.Data!;
        using (var draftDocument = JsonDocument.Parse(taskAfterDraft.FormDataJson))
        {
            draftDocument.RootElement.GetProperty("RECORD_NOTE").GetString()
                .ShouldBe("第一版现场记录");
        }

        var missingRequired = new TaskDecisionRequest(
            AIDT.Domain.Workflows.WorkflowDecision.Approve,
            taskAfterDraft.Version,
            "缺少必填字段",
            $"dept-wf-missing-{Guid.NewGuid():N}",
            """{"RECORD_NOTE":"只有一项"}""",
            form.Data.FormDefinitionHash);
        using var invalidRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/workflow-tasks/{taskId}/actions/decide")
        {
            Content = JsonContent.Create(missingRequired)
        };
        invalidRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var invalidResponse = await admin.Client.SendAsync(invalidRequest);
        invalidResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await invalidResponse.Content.ReadAsStringAsync()).ShouldContain("缺少必填字段");

        var uploadResponse = await UploadFileAsync(
            admin,
            departmentId,
            Encoding.UTF8.GetBytes("部门流程节点附件验证"),
            $"dept-wf-{Guid.NewGuid():N}.txt",
            "text/plain",
            "Internal",
            Guid.Parse(businessObject.Id),
            Guid.Parse(instance.Id),
            Guid.Parse(taskId));
        uploadResponse.StatusCode.ShouldBe(
            HttpStatusCode.Created,
            await uploadResponse.Content.ReadAsStringAsync());
        var uploadedFile = (await uploadResponse.Content.ReadFromJsonAsync<
            ApiResponse<FileAttachmentDto>>(ClientJsonOptions))!.Data!;
        var validData = JsonSerializer.Serialize(new
        {
            RECORD_NOTE = "现场核对完成",
            REVIEWER = adminUserId.ToString("D"),
            DUE_DATE = "2026-11-10",
            EVIDENCE = new[] { uploadedFile.Id }
        });
        var decision = new TaskDecisionRequest(
            AIDT.Domain.Workflows.WorkflowDecision.Approve,
            taskAfterDraft.Version,
            "节点表单完整，确认通过",
            $"dept-wf-approve-{Guid.NewGuid():N}",
            validData,
            form.Data.FormDefinitionHash);
        using var decisionRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/workflow-tasks/{taskId}/actions/decide")
        {
            Content = JsonContent.Create(decision)
        };
        decisionRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var decisionResponse = await admin.Client.SendAsync(decisionRequest);
        decisionResponse.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            await decisionResponse.Content.ReadAsStringAsync());

        var secondRequest = createRequest with
        {
            Name = "研发部门节点表单验证 v2",
            Nodes =
            [
                node with
                {
                    Name = "部门复核 v2",
                    Fields =
                    [
                        fields[0] with { DisplayName = "现场记录 v2" },
                        fields[1],
                        fields[2],
                        fields[3]
                    ]
                }
            ]
        };
        var second = await CreateConfigurationAsync(
            admin,
            $"dept-wf-create-v2-{Guid.NewGuid():N}",
            secondRequest);
        await ValidateConfigurationAsync(
            admin,
            second.Id,
            $"dept-wf-validate-v2-{Guid.NewGuid():N}");
        var publishedSecond = await PublishConfigurationAsync(
            admin,
            second.Id,
            $"dept-wf-publish-v2-{Guid.NewGuid():N}");

        var historicalForm = await admin.Client.GetFromJsonAsync<
            ApiResponse<WorkflowTaskFormDto>>(
            $"/api/v1/workflow-tasks/{taskId}/form");
        historicalForm!.Data!.Fields.Single(field => field.Key == "RECORD_NOTE")
            .DisplayName.ShouldBe("现场记录");
        historicalForm.Data.FormDefinitionHash.ShouldBe(form.Data.FormDefinitionHash);
        using (var historicalDocument = JsonDocument.Parse(historicalForm.Data.FormDataJson))
        {
            historicalDocument.RootElement.GetProperty("RECORD_NOTE").GetString()
                .ShouldBe("现场核对完成");
        }

        using var disableRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/configurations/{second.Id}/actions/disable");
        disableRequest.Headers.Add("Idempotency-Key", $"dept-wf-disable-{Guid.NewGuid():N}");
        disableRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var disableResponse = await admin.Client.SendAsync(disableRequest);
        disableResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var disabled = (await disableResponse.Content.ReadFromJsonAsync<
            ApiResponse<WorkflowConfigurationDto>>(ClientJsonOptions))!.Data!;
        disabled.State.ShouldBe("Retired");
        disabled.Id.ShouldBe(publishedSecond.Id);
    }

    [SqlIntegrationFact]
    public async Task Session_ReportsPermissionChangeInsteadOfReturningEmptyFeatures()
    {
        using var researcher = await CreateAuthenticatedClientAsync("demo.noscope");
        var before = await researcher.Client.GetFromJsonAsync<ApiResponse<SessionDto>>(
            "/api/v1/auth/session");
        before!.Data!.AuthorizationState.ShouldBe("Current");

        var connectionString = Environment.GetEnvironmentVariable("AIDT_SQL_CONNECTION")
            ?? throw new InvalidOperationException("AIDT_SQL_CONNECTION is required.");
        Guid userId;
        Guid departmentId;
        var inserted = false;
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using (var lookup = connection.CreateCommand())
        {
            lookup.CommandText =
                """
                SELECT userId = (SELECT Id FROM [Identity].[User] WHERE UserName=N'demo.noscope'),
                       departmentId = (
                           SELECT TOP (1) Id
                           FROM [Identity].[Department]
                           WHERE Code=N'RND' AND IsActive=1);
                """;
            await using var reader = await lookup.ExecuteReaderAsync();
            (await reader.ReadAsync()).ShouldBeTrue();
            userId = reader.GetGuid(0);
            departmentId = reader.GetGuid(1);
        }

        try
        {
            await using (var insert = connection.CreateCommand())
            {
                insert.CommandText =
                    """
                    IF NOT EXISTS (
                        SELECT 1 FROM [Identity].[UserDepartmentMembership]
                        WHERE UserId=@UserId AND DepartmentId=@DepartmentId)
                    BEGIN
                        INSERT INTO [Identity].[UserDepartmentMembership]
                            (UserId, DepartmentId, IsPrimary, IsManager, IsActive, JoinedAt)
                        VALUES (@UserId, @DepartmentId, 0, 0, 1, SYSUTCDATETIME());
                        SELECT 1;
                    END
                    ELSE
                    BEGIN
                        SELECT 0;
                    END;
                    """;
                insert.Parameters.AddWithValue("@UserId", userId);
                insert.Parameters.AddWithValue("@DepartmentId", departmentId);
                inserted = Convert.ToInt32(
                    await insert.ExecuteScalarAsync(),
                    System.Globalization.CultureInfo.InvariantCulture) == 1;
            }
            if (!inserted)
            {
                return;
            }

            var after = await researcher.Client.GetFromJsonAsync<ApiResponse<SessionDto>>(
                "/api/v1/auth/session");
            after!.Data!.AuthorizationState.ShouldBe("Stale");
            after.Data.AuthorizationMessage.ShouldBe("权限已更新，请重新登录。");
        }
        finally
        {
            if (inserted)
            {
                await using var cleanup = connection.CreateCommand();
                cleanup.CommandText =
                    """
                    DELETE FROM [Identity].[UserDepartmentMembership]
                    WHERE UserId=@UserId AND DepartmentId=@DepartmentId;
                    """;
                cleanup.Parameters.AddWithValue("@UserId", userId);
                cleanup.Parameters.AddWithValue("@DepartmentId", departmentId);
                await cleanup.ExecuteNonQueryAsync();
            }
        }
    }

    [SqlIntegrationFact]
    public async Task DepartmentFlowOwner_CannotPublishPaymentBoundaryTemplate()
    {
        using var finance = await CreateAuthenticatedClientAsync("demo.finance");
        var departmentId = await GetDepartmentIdAsync(finance, "FINANCE");
        var configuration = await CreateConfigurationAsync(
            finance,
            $"dept-finance-b-{Guid.NewGuid():N}",
            new CreateWorkflowConfigurationRequest(
                $"FIN-HIGH-{Guid.NewGuid():N}"[..28].ToUpperInvariant(),
                "财务付款人工复核模板",
                "验证部门流程负责人不能绕过平台审核发布付款边界。",
                departmentId,
                "ANY",
                "PAYMENT_REVIEW",
                [
                    new WorkflowConfigurationNodeRequest(
                        "PAYMENT_REVIEW",
                        "付款人工复核",
                        "demo.finance",
                        null,
                        CompleteOnApprove: true,
                        BusinessDescription: "仅做付款前材料复核，不执行真实付款。",
                        PermanentHumanBoundary: true)
                ]));
        configuration.IsTestData.ShouldBeFalse();
        configuration.RiskLevel.ShouldBe("High");
        configuration.RequiresPlatformApproval.ShouldBeTrue();
        await ValidateConfigurationAsync(
            finance,
            configuration.Id,
            $"dept-finance-validate-{Guid.NewGuid():N}");
        var publishResponse = await SendPublishConfigurationAsync(
            finance,
            configuration.Id,
            $"dept-finance-publish-{Guid.NewGuid():N}");
        publishResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await publishResponse.Content.ReadAsStringAsync())
            .ShouldContain("平台管理员审核发布");
    }

    [SqlIntegrationFact]
    public async Task FourDepartmentPilots_CreatePublishAndExposeOnlyOwnedNavigation()
    {
        var pilots = new[]
        {
            new DepartmentPilot("demo.researcher", "RND", "研发样品资料复核"),
            new DepartmentPilot("demo.supply", "SUPPLY", "供应链到货资料复核"),
            new DepartmentPilot("demo.finance", "FINANCE", "财务材料人工复核"),
            new DepartmentPilot("demo.delivery", "DELIVERY", "交付资料验收复核")
        };
        foreach (var pilot in pilots)
        {
            using var owner = await CreateAuthenticatedClientAsync(pilot.UserName);
            var departmentId = await GetDepartmentIdAsync(owner, pilot.DepartmentCode);
            var nodeCode = "PILOT_REVIEW";
            var request = new CreateWorkflowConfigurationRequest(
                ($"PILOT-{pilot.DepartmentCode}-{Guid.NewGuid():N}").ToUpperInvariant(),
                pilot.TemplateName,
                $"四部门试点：{pilot.DepartmentCode} 普通低风险流程。",
                departmentId,
                "ANY",
                nodeCode,
                [
                    new WorkflowConfigurationNodeRequest(
                        nodeCode,
                        "资料复核",
                        pilot.UserName,
                        null,
                        CompleteOnApprove: true,
                        BusinessDescription: "核对业务资料、人员和日期后完成。",
                        SlaMinutes: 480,
                        Fields:
                        [
                            new WorkflowFormFieldRequest(
                                "NOTE",
                                "复核记录",
                                "Text",
                                Required: true),
                            new WorkflowFormFieldRequest(
                                "DUE_DATE",
                                "完成日期",
                                "Date",
                                Required: true)
                        ],
                        Actions:
                        [
                            new WorkflowNodeActionRequest("APPROVE", "通过", "Approve", null)
                        ])
                ]);
            var configuration = await CreateConfigurationAsync(
                owner,
                $"pilot-create-{pilot.DepartmentCode}-{Guid.NewGuid():N}",
                request);
            configuration.IsTestData.ShouldBeTrue();
            configuration.RiskLevel.ShouldBe("Low");
            await ValidateConfigurationAsync(
                owner,
                configuration.Id,
                $"pilot-validate-{pilot.DepartmentCode}-{Guid.NewGuid():N}");
            var published = await PublishConfigurationAsync(
                owner,
                configuration.Id,
                $"pilot-publish-{pilot.DepartmentCode}-{Guid.NewGuid():N}");
            published.State.ShouldBe("Published");

            var navigation = await owner.Client.GetFromJsonAsync<ApiResponse<CurrentNavigationDto>>(
                "/api/v1/navigation/current");
            navigation!.Data!.Groups.Count(group => group.Layer == "Company").ShouldBe(1);
            var departmentGroups = navigation.Data.Groups.Where(group =>
                group.Layer == "Department"
                && group.DepartmentCode == pilot.DepartmentCode).ToList();
            departmentGroups.Count.ShouldBe(
                1,
                $"部门导航未返回 {pilot.UserName} / {pilot.DepartmentCode}");
            var departmentGroup = departmentGroups[0];
            departmentGroup.Items.ShouldContain(item => item.ItemKey == "CONFIGURATIONS");
            departmentGroup.Items.ShouldContain(item => item.ItemKey == "WORKFLOW_ITEMS");

            var crossDepartmentId = await GetDepartmentIdByCodeAsync(
                pilot.DepartmentCode == "RND" ? "FINANCE" : "RND");
            using var crossRequest = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/v1/configurations")
            {
                Content = JsonContent.Create(request with
                {
                    ConfigurationKey = $"CROSS-{Guid.NewGuid():N}"[..32].ToUpperInvariant(),
                    DepartmentId = crossDepartmentId
                })
            };
            crossRequest.Headers.Add(
                "Idempotency-Key",
                $"pilot-cross-{Guid.NewGuid():N}");
            crossRequest.Headers.Add("X-CSRF-TOKEN", owner.CsrfToken);
            (await owner.Client.SendAsync(crossRequest)).StatusCode.ShouldBe(
                HttpStatusCode.Forbidden);
        }
    }

    [Fact]
    public async Task OpenApiDocument_IsServedFromRepositoryContract()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/openapi/v1.json");
        var text = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        text.ShouldContain("\"openapi\": \"3.1.0\"");
        text.ShouldContain("/api/v1/workflow-tasks");
        text.ShouldContain("\"WorkItemView\"");
        text.ShouldContain("/api/v1/workflow-policy/users");
        text.ShouldContain("/api/v1/workflow-sla/tasks/{id}/actions/simulate-timeout");
    }

    [SqlIntegrationFact]
    public async Task MyWork_AggregatesSixPendingSourcesDeduplicatesAndSurvivesRelogin()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var adminUserId = await GetUserIdAsync("demo.admin");
        var organizationId = await GetOrganizationIdAsync("demo.admin");
        var departmentId = await GetDepartmentIdAsync(admin, "RND");
        var marker = $"UX003-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        var objectTypeId = await GetObjectTypeIdAsync(admin, "CUSTOMER_SAMPLE");
        var agentProfiles = await admin.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<AgentProfileDto>>>(
            "/api/v1/agents",
            ClientJsonOptions);
        var businessAgent = agentProfiles!.Data!.Single(item => item.Name.Contains("业务协同"));
        var eventAgent = agentProfiles.Data!.Single(item => item.Name.Contains("项目事件"));

        var objectResponse = await CreateObjectAsync(
            admin,
            $"ux003-object-{Guid.NewGuid():N}",
            new CreateBusinessObjectRequest(
                Guid.Parse(objectTypeId),
                $"{marker}-OBJ",
                new Dictionary<string, string?>
                {
                    ["Customer"] = "聚合测试",
                    ["Product"] = "测试样品",
                    ["Requirement"] = marker,
                    ["TrialRequired"] = "No",
                    ["InternalFormulaReference"] = "TEST-ONLY"
                },
                departmentId));
        objectResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var businessObject = (await objectResponse.Content.ReadFromJsonAsync<
            ApiResponse<BusinessObjectDto>>(ClientJsonOptions))!.Data!;

        var configuration = await CreateConfigurationAsync(
            admin,
            $"ux003-config-{Guid.NewGuid():N}",
            new CreateWorkflowConfigurationRequest(
                $"UX003_{DateTime.UtcNow:HHmmssfff}",
                $"{marker} 统一事项流程",
                "统一工作事项测试",
                departmentId,
                "CUSTOMER_SAMPLE",
                "START",
                [
                    new WorkflowConfigurationNodeRequest(
                        "START",
                        "统一工作事项确认",
                        "demo.admin",
                        null,
                        true,
                        null,
                        "SpecificUser",
                        null,
                        null)
                ]));
        await ValidateConfigurationAsync(admin, configuration.Id, $"ux003-validate-{Guid.NewGuid():N}");
        var published = await PublishConfigurationAsync(
            admin,
            configuration.Id,
            $"ux003-publish-{Guid.NewGuid():N}");
        using var startRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/workflow-instances")
        {
            Content = JsonContent.Create(new StartWorkflowInstanceRequest(
                Guid.Parse(published.PublishedWorkflowDefinitionId!),
                Guid.Parse(published.PublishedWorkflowVersionId!),
                Guid.Parse(businessObject.Id),
                $"ux003-workflow-{Guid.NewGuid():N}"))
        };
        startRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var startResponse = await admin.Client.SendAsync(startRequest);
        startResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var workflow = (await startResponse.Content.ReadFromJsonAsync<ApiResponse<WorkflowInstanceDto>>(
            ClientJsonOptions))!.Data!;
        var workflowTasks = await admin.Client.GetFromJsonAsync<ApiResponse<PagedResult<WorkflowTaskDto>>>(
            "/api/v1/workflow-tasks?pageSize=100&includeTestData=true",
            ClientJsonOptions);
        var workflowTask = workflowTasks!.Data!.Items.Single(item =>
            item.WorkflowInstanceId == workflow.Id && item.Status.ToString() == "Pending");

        var project = await CreateProjectForOwnerAsync(
            admin,
            adminUserId,
            departmentId,
            marker);
        var pendingEvent = await ReceiveInboxEventAsync(
            admin,
            departmentId,
            $"{marker} 待确认事项",
            "请确认负责人和截止时间。");
        await AnalyzeInboxEventAsync(admin, pendingEvent.Id, eventAgent.Id);

        var taskEvent = await ReceiveInboxEventAsync(
            admin,
            departmentId,
            $"{marker} 去重事项",
            "确认后创建项目任务，事件与任务只能显示一个主事项。");
        await AnalyzeInboxEventAsync(admin, taskEvent.Id, eventAgent.Id);
        var confirmation = await ConfirmInboxEventForUserAsync(
            admin,
            taskEvent.Id,
            project.Id,
            adminUserId,
            DateTimeOffset.UtcNow.AddDays(2));

        var minutesSource = await CreateAndExecuteMinutesAsync(
            admin,
            departmentId,
            marker,
            Guid.Parse(businessAgent.Id));
        var mcpConfirmation = await CreateMcpConfirmationAsync(
            adminUserId,
            organizationId,
            marker);
        var agentRun = await StartAgentRunAsync(
            admin,
            Guid.Parse(businessAgent.Id),
            $"ux003-agent-{Guid.NewGuid():N}");

        var first = await admin.Client.GetFromJsonAsync<ApiResponse<MyWorkDto>>(
            "/api/v1/my-work?includeTestData=true",
            ClientJsonOptions);
        var work = first!.Data!;
        var sourceTypes = work.PendingItems.Select(item => item.SourceType).ToHashSet(StringComparer.Ordinal);
        sourceTypes.ShouldContain("WorkflowTask");
        sourceTypes.ShouldContain("ProjectTask");
        sourceTypes.ShouldContain("EventInbox");
        sourceTypes.ShouldContain("MinutesActionItem");
        sourceTypes.ShouldContain("McpHumanConfirmation");
        sourceTypes.ShouldContain("AgentRun");
        work.PendingItems.ShouldContain(item => item.Id == workflowTask.Id);
        work.PendingItems.ShouldContain(item => item.Id == confirmation.Task!.Id);
        work.PendingItems.ShouldContain(item => item.Id == pendingEvent.Id);
        work.PendingItems.ShouldContain(item =>
            item.SourceType == "MinutesActionItem"
            && item.ContextTitle == minutesSource.Title);
        work.PendingItems.ShouldContain(item => item.Id == mcpConfirmation.Id.ToString("D"));
        work.PendingItems.ShouldContain(item => item.Id == agentRun.Id);
        work.PendingItems.ShouldNotContain(item =>
            item.SourceType == "EventInbox" && item.Id == taskEvent.Id);
        work.PendingItems.ShouldAllBe(item =>
            !item.BusinessTitle.Contains("traceId", StringComparison.OrdinalIgnoreCase)
            && !item.BusinessTitle.Contains(item.Id, StringComparison.OrdinalIgnoreCase));
        work.PendingItems.ShouldAllBe(item =>
            item.StatusLabel != "Pending"
            && item.StatusLabel != "AwaitingConfirmation"
            && item.StatusLabel != "Draft");
        var ranks = work.PendingItems.Select(item =>
            WorkItemPresentation.PriorityRank(item.PriorityGroup)).ToList();
        ranks.ShouldBe(ranks.OrderBy(item => item));
        work.PendingItems[0].PriorityGroup.ShouldBe("PermanentHumanBoundary");
        work.Summary.PendingCount.ShouldBe(work.PendingItems.Count);

        using var reloadedSession = await CreateAuthenticatedClientAsync("demo.admin");
        var second = await reloadedSession.Client.GetFromJsonAsync<ApiResponse<MyWorkDto>>(
            "/api/v1/my-work?includeTestData=true",
            ClientJsonOptions);
        second!.Data!.PendingItems.Select(item => $"{item.SourceType}:{item.Id}")
            .ShouldBe(work.PendingItems.Select(item => $"{item.SourceType}:{item.Id}"));
        second.Data.Summary.ShouldBe(work.Summary);

        await CompleteProjectTaskAsync(admin, confirmation.Task!.Id, confirmation.Task.Version);
        using var afterCompletion = await CreateAuthenticatedClientAsync("demo.admin");
        var refreshed = await afterCompletion.Client.GetFromJsonAsync<ApiResponse<MyWorkDto>>(
            "/api/v1/my-work?includeTestData=true",
            ClientJsonOptions);
        refreshed!.Data!.PendingItems.ShouldNotContain(item => item.Id == confirmation.Task.Id);
        refreshed.Data.PendingItems.ShouldNotContain(item => item.Id == taskEvent.Id);
    }

    [SqlIntegrationFact]
    public async Task MyWork_DoesNotExposeAnotherDepartmentEvent()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        using var pm = await CreateAuthenticatedClientAsync("demo.pm");
        var qaDepartmentId = await GetDepartmentIdAsync(admin, "QA");
        var agentProfiles = await admin.Client.GetFromJsonAsync<ApiResponse<IReadOnlyList<AgentProfileDto>>>(
            "/api/v1/agents",
            ClientJsonOptions);
        var eventAgent = agentProfiles!.Data!.Single(item => item.Name.Contains("项目事件"));
        var marker = $"UX003-DENY-{Guid.NewGuid():N}";
        var hidden = await ReceiveInboxEventAsync(
            admin,
            qaDepartmentId,
            $"{marker} 质量部私有事项",
            "仅质量部授权人员可见。");
        await AnalyzeInboxEventAsync(admin, hidden.Id, eventAgent.Id);

        var result = await pm.Client.GetFromJsonAsync<ApiResponse<MyWorkDto>>(
            "/api/v1/my-work",
            ClientJsonOptions);

        result!.Data!.PendingItems.ShouldNotContain(item => item.Id == hidden.Id);
        result.Data.PendingItems.ShouldNotContain(item =>
            item.BusinessTitle.Contains(marker, StringComparison.Ordinal));

        using var noScope = await CreateAuthenticatedClientAsync("demo.noscope");
        var denied = await noScope.Client.GetAsync("/api/v1/my-work");
        denied.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static async Task<Guid> GetOrganizationIdAsync(string userName)
    {
        var connectionString = Environment.GetEnvironmentVariable("AIDT_SQL_CONNECTION")
            ?? throw new InvalidOperationException("AIDT_SQL_CONNECTION is required.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT OrganizationId FROM [Identity].[User] WHERE UserName=@UserName;";
        command.Parameters.AddWithValue("@UserName", userName);
        return (Guid)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException($"User not found: {userName}"));
    }

    private static async Task<ProjectDto> CreateProjectForOwnerAsync(
        AuthenticatedClient client,
        Guid ownerUserId,
        Guid departmentId,
        string marker)
    {
        var objectCode = $"UX003-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/projects")
        {
            Content = JsonContent.Create(new CreateProjectRequest(
                objectCode,
                $"{marker} 聚合项目",
                ownerUserId,
                departmentId,
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(30),
                "验证统一工作事项聚合",
                "执行中",
                "Internal",
                "Active"))
        };
        request.Headers.Add("Idempotency-Key", $"ux003-project-{Guid.NewGuid():N}");
        request.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        var response = await client.Client.SendAsync(request);
        response.StatusCode.ShouldBe(
            HttpStatusCode.Created,
            await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<ApiResponse<ProjectDto>>(
            ClientJsonOptions))!.Data!;
    }

    private static async Task<InboxConfirmationDto> ConfirmInboxEventForUserAsync(
        AuthenticatedClient client,
        string eventId,
        string projectId,
        Guid assigneeId,
        DateTimeOffset dueAt)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/inbox/{eventId}/actions/confirm")
        {
            Content = JsonContent.Create(new ConfirmInboxEventRequest(
                Guid.Parse(projectId),
                assigneeId,
                dueAt,
                "P1",
                $"UX-003 去重任务 {Guid.NewGuid():N}",
                null,
                true))
        };
        request.Headers.Add("Idempotency-Key", $"ux003-confirm-{Guid.NewGuid():N}");
        request.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        var response = await client.Client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, body);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<InboxConfirmationDto>>(
            ClientJsonOptions))!.Data!;
    }

    private static async Task<MinutesSourceDto> CreateAndExecuteMinutesAsync(
        AuthenticatedClient client,
        Guid departmentId,
        string marker,
        Guid agentProfileId)
    {
        using var register = new HttpRequestMessage(HttpMethod.Post, "/api/v1/minutes")
        {
            Content = JsonContent.Create(new RegisterMinutesRequest(
                departmentId,
                $"{marker} 统一工作事项会议",
                DateTimeOffset.UtcNow,
                $"[09:30:00] 项目经理：请在 2026-11-10 完成 {marker} 证据核对。"))
        };
        register.Headers.Add("Idempotency-Key", $"ux003-minutes-{Guid.NewGuid():N}");
        register.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        var registerResponse = await client.Client.SendAsync(register);
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var source = (await registerResponse.Content.ReadFromJsonAsync<ApiResponse<MinutesSourceDto>>(
            ClientJsonOptions))!.Data!;

        using var analyze = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/minutes/{source.Id}/actions/analyze")
        {
            Content = JsonContent.Create(new AnalyzeMinutesRequest(agentProfileId))
        };
        analyze.Headers.Add("Idempotency-Key", $"ux003-minutes-analyze-{Guid.NewGuid():N}");
        analyze.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        var analyzeResponse = await client.Client.SendAsync(analyze);
        analyzeResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var run = (await analyzeResponse.Content.ReadFromJsonAsync<ApiResponse<AgentRunDto>>(
            ClientJsonOptions))!.Data!;

        using var confirm = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/agents/runs/{run.Id}/actions/confirm")
        {
            Content = JsonContent.Create(new ConfirmAgentRunRequest(Guid.NewGuid()))
        };
        confirm.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        (await client.Client.SendAsync(confirm)).StatusCode.ShouldBe(HttpStatusCode.OK);

        using var execute = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/agents/runs/{run.Id}/actions/execute");
        execute.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        var executeResponse = await client.Client.SendAsync(execute);
        executeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = (await executeResponse.Content.ReadFromJsonAsync<
            ApiResponse<AgentExecutionResultDto>>(ClientJsonOptions))!.Data!;
        result.ActionItems.ShouldNotBeEmpty();
        return source;
    }

    private async Task<McpHumanConfirmationDto> CreateMcpConfirmationAsync(
        Guid userId,
        Guid organizationId,
        string marker)
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IMcpHumanConfirmationService>();
        var requestHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(marker))).ToLowerInvariant();
        return await service.RequestAsync(
            new McpConfirmationRequest(
                userId,
                organizationId,
                "ux003-client",
                "aidt_confirm_event",
                "EventInbox",
                marker,
                requestHash,
                $"{marker} 需要真人确认高风险事项"),
            CancellationToken.None);
    }

    private static async Task<AgentRunDto> StartAgentRunAsync(
        AuthenticatedClient client,
        Guid agentProfileId,
        string idempotencyKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/agents/runs")
        {
            Content = JsonContent.Create(new CreateAgentRunRequest(
                agentProfileId,
                AgentActionType.Draft,
                0,
                AgentRiskLevel.Low,
                "aidt-rule-sim"))
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        var response = await client.Client.SendAsync(request);
        response.StatusCode.ShouldBe(
            HttpStatusCode.Created,
            await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<ApiResponse<AgentRunDto>>(
            ClientJsonOptions))!.Data!;
    }

    private static async Task<ProjectDto> CreateProjectAsync(
        AuthenticatedClient pm,
        Guid departmentId,
        string name)
    {
        var ownerId = await GetUserIdAsync("demo.pm");
        var objectCode = $"PRJ-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/projects")
        {
            Content = JsonContent.Create(new CreateProjectRequest(
                objectCode,
                name,
                ownerId,
                departmentId,
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(30),
                $"{name}的演示项目目标",
                "需求确认",
                "Internal",
                "Active"))
        };
        request.Headers.Add("Idempotency-Key", $"project-{Guid.NewGuid():N}");
        request.Headers.Add("X-CSRF-TOKEN", pm.CsrfToken);
        var response = await pm.Client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<ProjectDto>>(ClientJsonOptions))!.Data!;
    }

    private static async Task<ProjectDto> UpdateProjectAsync(
        AuthenticatedClient client,
        string projectId,
        long expectedVersion,
        UpdateProjectRequest request)
    {
        using var message = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/v1/projects/{projectId}")
        {
            Content = JsonContent.Create(request with { ExpectedVersion = expectedVersion })
        };
        message.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        var response = await client.Client.SendAsync(message);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.OK, body);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<ProjectDto>>(
            ClientJsonOptions))!.Data!;
    }

    private static async Task<ProjectTaskDto> CreateProjectTaskAsync(
        AuthenticatedClient client,
        string projectId,
        CreateProjectTaskRequest request,
        string idempotencyKey)
    {
        using var response = await SendCreateProjectTaskAsync(
            client,
            projectId,
            request,
            idempotencyKey);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, body);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<ProjectTaskDto>>(
            ClientJsonOptions))!.Data!;
    }

    private static async Task<HttpResponseMessage> SendCreateProjectTaskAsync(
        AuthenticatedClient client,
        string projectId,
        CreateProjectTaskRequest request,
        string idempotencyKey)
    {
        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/projects/{projectId}/tasks")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add("Idempotency-Key", idempotencyKey);
        message.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        return await client.Client.SendAsync(message);
    }

    private static async Task<ProjectDto> GetProjectAsync(
        AuthenticatedClient client,
        string projectId)
    {
        var response = await client.Client.GetFromJsonAsync<ApiResponse<ProjectDetailDto>>(
            $"/api/v1/projects/{projectId}",
            ClientJsonOptions);
        return response!.Data!.Project;
    }

    private static async Task<ProjectDto> ChangeProjectStatusAsync(
        AuthenticatedClient client,
        string projectId,
        long expectedVersion,
        string status)
    {
        using var response = await ChangeProjectStatusRawAsync(
            client,
            projectId,
            expectedVersion,
            status);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.OK, body);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<ProjectDto>>(
            ClientJsonOptions))!.Data!;
    }

    private static async Task<HttpResponseMessage> ChangeProjectStatusRawAsync(
        AuthenticatedClient client,
        string projectId,
        long expectedVersion,
        string status)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/projects/{projectId}/actions/change-status")
        {
            Content = JsonContent.Create(new ChangeProjectStatusRequest(
                expectedVersion,
                status,
                $"状态持久化测试：{status}"))
        };
        request.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        return await client.Client.SendAsync(request);
    }

    private static async Task<InboxPersistence> GetInboxPersistenceAsync(Guid eventId)
    {
        var connectionString = Environment.GetEnvironmentVariable("AIDT_SQL_CONNECTION")
            ?? throw new InvalidOperationException("AIDT_SQL_CONNECTION is required.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                inbox.Status,
                suggestion.Status,
                (SELECT COUNT(*) FROM [PMO].[ProjectTask] task WHERE task.EventInboxId=inbox.Id)
            FROM [PMO].[EventInbox] inbox
            JOIN [PMO].[EventSuggestion] suggestion ON suggestion.EventInboxId=inbox.Id
            WHERE inbox.Id=@EventId;
            """;
        command.Parameters.AddWithValue("@EventId", eventId);
        await using var reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).ShouldBeTrue();
        return new InboxPersistence(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetInt32(2));
    }

    private static async Task<int> CountProjectAttachmentRelationsAsync(
        Guid projectId,
        Guid attachmentId)
    {
        var connectionString = Environment.GetEnvironmentVariable("AIDT_SQL_CONNECTION")
            ?? throw new InvalidOperationException("AIDT_SQL_CONNECTION is required.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(*)
            FROM [PMO].[ProjectRelation]
            WHERE ProjectId=@ProjectId
              AND EntityType=N'Attachment'
              AND EntityId=@AttachmentId
              AND RelationCode=N'SupportingEvidence';
            """;
        command.Parameters.AddWithValue("@ProjectId", projectId);
        command.Parameters.AddWithValue("@AttachmentId", attachmentId);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<int> CountDuplicateProjectAttachmentRelationsAsync(Guid projectId)
    {
        var connectionString = Environment.GetEnvironmentVariable("AIDT_SQL_CONNECTION")
            ?? throw new InvalidOperationException("AIDT_SQL_CONNECTION is required.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(*)
            FROM (
                SELECT EntityId
                FROM [PMO].[ProjectRelation]
                WHERE ProjectId=@ProjectId
                  AND EntityType=N'Attachment'
                  AND RelationCode=N'SupportingEvidence'
                GROUP BY EntityId
                HAVING COUNT(*) > 1
            ) duplicateRelations;
            """;
        command.Parameters.AddWithValue("@ProjectId", projectId);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<WorkflowConfigurationDto> CreateConfigurationAsync(
        AuthenticatedClient client,
        string idempotencyKey,
        CreateWorkflowConfigurationRequest request)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/configurations")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add("Idempotency-Key", idempotencyKey);
        message.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        var response = await client.Client.SendAsync(message);
        response.StatusCode.ShouldBe(
            HttpStatusCode.Created,
            await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<ApiResponse<WorkflowConfigurationDto>>(
            ClientJsonOptions))!.Data!;
    }

    private static async Task<(Guid WorkflowDefinitionId, Guid WorkflowVersionId)>
        PublishProjectWorkflowAsync(
            AuthenticatedClient client,
            Guid departmentId,
            string marker)
    {
        var configurationCode = $"UX004_{Guid.NewGuid():N}"[..24].ToUpperInvariant();
        var draft = await CreateConfigurationAsync(
            client,
            $"ux004-config-{Guid.NewGuid():N}",
            new CreateWorkflowConfigurationRequest(
                configurationCode,
                $"{marker} 项目事件确认流程",
                "UX-004-A 事件确认与流程发起原子编排测试",
                departmentId,
                "PROJECT",
                "START",
                [
                    new WorkflowConfigurationNodeRequest(
                        "START",
                        "项目事件确认",
                        "demo.pm",
                        null,
                        CompleteOnApprove: true)
                ]));
        (await ValidateConfigurationAsync(
            client,
            draft.Id,
            $"ux004-validate-{Guid.NewGuid():N}")).IsValid.ShouldBeTrue();
        var published = await PublishConfigurationAsync(
            client,
            draft.Id,
            $"ux004-publish-{Guid.NewGuid():N}");
        return (
            Guid.Parse(published.PublishedWorkflowDefinitionId!),
            Guid.Parse(published.PublishedWorkflowVersionId!));
    }

    private static async Task<WorkflowInstanceDto> StartProjectWorkflowAsync(
        AuthenticatedClient client,
        (Guid WorkflowDefinitionId, Guid WorkflowVersionId) definition,
        string projectId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/workflow-instances")
        {
            Content = JsonContent.Create(new StartWorkflowInstanceRequest(
                definition.WorkflowDefinitionId,
                definition.WorkflowVersionId,
                Guid.Parse(projectId),
                $"ux004-existing-flow-{Guid.NewGuid():N}"))
        };
        request.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        var response = await client.Client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, responseBody);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<WorkflowInstanceDto>>(
            ClientJsonOptions))!.Data!;
    }

    private static async Task<WorkflowConfigurationValidationDto> ValidateConfigurationAsync(
        AuthenticatedClient client,
        string configurationId,
        string idempotencyKey)
    {
        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/configurations/{configurationId}/actions/validate");
        message.Headers.Add("Idempotency-Key", idempotencyKey);
        message.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        var response = await client.Client.SendAsync(message);
        response.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<
            ApiResponse<WorkflowConfigurationValidationDto>>(ClientJsonOptions))!.Data!;
    }

    private static async Task<HttpResponseMessage> SendPublishConfigurationAsync(
        AuthenticatedClient client,
        string configurationId,
        string idempotencyKey)
    {
        var message = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/configurations/{configurationId}/actions/publish");
        message.Headers.Add("Idempotency-Key", idempotencyKey);
        message.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        return await client.Client.SendAsync(message);
    }

    private static async Task<WorkflowConfigurationDto> PublishConfigurationAsync(
        AuthenticatedClient client,
        string configurationId,
        string idempotencyKey)
    {
        using var response = await SendPublishConfigurationAsync(client, configurationId, idempotencyKey);
        response.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<ApiResponse<WorkflowConfigurationDto>>(
            ClientJsonOptions))!.Data!;
    }

    private static async Task<WorkflowConfigurationDto> RollbackConfigurationAsync(
        AuthenticatedClient client,
        string configurationId,
        string idempotencyKey)
    {
        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/configurations/{configurationId}/actions/rollback")
        {
            Content = JsonContent.Create(new RollbackWorkflowConfigurationRequest("回归历史稳定版本"))
        };
        message.Headers.Add("Idempotency-Key", idempotencyKey);
        message.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        var response = await client.Client.SendAsync(message);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<WorkflowConfigurationDto>>(
            ClientJsonOptions))!.Data!;
    }

    private static async Task<ProjectMemberDto> AddProjectMemberAsync(
        AuthenticatedClient client,
        string projectId,
        Guid userId,
        string role)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/projects/{projectId}/members")
        {
            Content = JsonContent.Create(new AddProjectMemberRequest(userId, role))
        };
        request.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        var response = await client.Client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<ProjectMemberDto>>(
            ClientJsonOptions))!.Data!;
    }

    private static async Task<ProjectMemberDto> RemoveProjectMemberAsync(
        AuthenticatedClient client,
        string projectId,
        Guid userId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/v1/projects/{projectId}/members/{userId:D}")
        {
            Content = JsonContent.Create(new RemoveProjectMemberRequest("PMO-003 成员管理测试"))
        };
        request.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        var response = await client.Client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.OK, body);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<ProjectMemberDto>>(
            ClientJsonOptions))!.Data!;
    }

    private static async Task<ProjectMilestoneDto> AddMilestoneAsync(
        AuthenticatedClient client,
        string projectId,
        Guid ownerUserId,
        DateTimeOffset plannedDate)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/projects/{projectId}/milestones")
        {
            Content = JsonContent.Create(new CreateProjectMilestoneRequest(
                "客户资料确认",
                ownerUserId,
                plannedDate,
                "Planned"))
        };
        request.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        var response = await client.Client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<ProjectMilestoneDto>>(
            ClientJsonOptions))!.Data!;
    }

    private static async Task<InboxEventDto> ReceiveInboxEventAsync(
        AuthenticatedClient client,
        Guid departmentId,
        string title,
        string content,
        string sourceType = "ManualText",
        string? sourceId = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/inbox")
        {
            Content = JsonContent.Create(new ReceiveInboxEventRequest(
                departmentId,
                title,
                content,
                sourceType,
                sourceId is null ? null : Guid.Parse(sourceId)))
        };
        request.Headers.Add("Idempotency-Key", $"inbox-{Guid.NewGuid():N}");
        request.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        var response = await client.Client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<InboxEventDto>>(
            ClientJsonOptions))!.Data!;
    }

    private static async Task<InboxEventDto> AnalyzeInboxEventAsync(
        AuthenticatedClient client,
        string eventId,
        string agentProfileId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/inbox/{eventId}/actions/analyze")
        {
            Content = JsonContent.Create(new AnalyzeInboxEventRequest(Guid.Parse(agentProfileId)))
        };
        request.Headers.Add("Idempotency-Key", $"inbox-analyze-{Guid.NewGuid():N}");
        request.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        var response = await client.Client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<InboxEventDto>>(
            ClientJsonOptions))!.Data!;
    }

    private static async Task<InboxConfirmationDto> ConfirmInboxEventAsync(
        AuthenticatedClient client,
        string eventId,
        string projectId,
        string idempotencyKey,
        DateTimeOffset? dueAt = null)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/inbox/{eventId}/actions/confirm")
        {
            Content = JsonContent.Create(new ConfirmInboxEventRequest(
                Guid.Parse(projectId),
                null,
                dueAt,
                "P1",
                "从事件确认生成的项目任务",
                null,
                true))
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        var response = await client.Client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, responseBody);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<InboxConfirmationDto>>(
            ClientJsonOptions))!.Data!;
    }

    private static async Task<HttpResponseMessage> SendConfirmInboxEventAndStartFlowAsync(
        AuthenticatedClient client,
        string eventId,
        ConfirmInboxEventRequest request)
    {
        var message = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/inbox/{eventId}/actions/confirm-and-start-flow")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add("Idempotency-Key", $"ux004-confirm-{Guid.NewGuid():N}");
        message.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        return await client.Client.SendAsync(message);
    }

    private static async Task<InboxConfirmationDto> ConfirmInboxEventAndStartFlowAsync(
        AuthenticatedClient client,
        string eventId,
        ConfirmInboxEventRequest request)
    {
        using var response = await SendConfirmInboxEventAndStartFlowAsync(client, eventId, request);
        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, responseBody);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<InboxConfirmationDto>>(
            ClientJsonOptions))!.Data!;
    }

    private static async Task<HttpResponseMessage> SendMcpDecisionAsync(
        AuthenticatedClient client,
        Guid confirmationId,
        bool approve,
        string? comment)
    {
        var action = approve ? "confirm" : "reject";
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/mcp/confirmations/{confirmationId:D}/actions/{action}")
        {
            Content = JsonContent.Create(new { comment })
        };
        request.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        return await client.Client.SendAsync(request);
    }

    private static async Task<ProjectTaskDto> CompleteProjectTaskAsync(
        AuthenticatedClient client,
        string taskId,
        long expectedVersion)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/project-tasks/{taskId}/actions/complete")
        {
            Content = JsonContent.Create(new CompleteProjectTaskRequest(
                expectedVersion,
                "事件转任务测试完成"))
        };
        request.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        var response = await client.Client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<ProjectTaskDto>>(
            ClientJsonOptions))!.Data!;
    }

    private static async Task AddProjectRelationAsync(
        AuthenticatedClient client,
        string projectId,
        string entityType,
        string entityId,
        string relationCode)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/projects/{projectId}/relations")
        {
            Content = JsonContent.Create(new CreateProjectRelationRequest(
                entityType,
                Guid.Parse(entityId),
                relationCode,
                "API 集成测试证据"))
        };
        request.Headers.Add("X-CSRF-TOKEN", client.CsrfToken);
        var response = await client.Client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    private static async Task<int> CountProjectTasksByEventAsync(Guid eventId)
    {
        var connectionString = Environment.GetEnvironmentVariable("AIDT_SQL_CONNECTION")
            ?? throw new InvalidOperationException("AIDT_SQL_CONNECTION is required.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM [PMO].[ProjectTask] WHERE [EventInboxId]=@EventInboxId;";
        command.Parameters.AddWithValue("@EventInboxId", eventId);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<int> CountWorkflowInstancesByProjectAsync(Guid projectId)
    {
        var connectionString = Environment.GetEnvironmentVariable("AIDT_SQL_CONNECTION")
            ?? throw new InvalidOperationException("AIDT_SQL_CONNECTION is required.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM [Workflow].[WorkflowInstance] WHERE [BusinessObjectId]=@ProjectId;";
        command.Parameters.AddWithValue("@ProjectId", projectId);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    [SqlIntegrationFact]
    public async Task MetaObjectModeling_EnforcesLifecyclePermissionsConcurrencyAndMasking()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var code = $"META_{Guid.NewGuid():N}"[..24].ToUpperInvariant();
        var fields = new[]
        {
            new ObjectFieldDto(
                "Name", "名称", "Text", true, 100, "Internal",
                ShowInList: true, Filterable: true, SortOrder: 10, Code: "Name"),
            new ObjectFieldDto(
                "Secret", "核心字段", "LongText", false, 2000, "Core",
                ShowInList: false, Filterable: false, SortOrder: 20, Code: "Secret"),
            new ObjectFieldDto(
                "Status", "状态", "Select", true, 20, "Internal",
                Options: ["Open", "Closed"], ShowInList: true, Filterable: true,
                SortOrder: 30, Code: "Status")
        };
        using var createTypeRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/object-types")
        {
            Content = JsonContent.Create(new CreateObjectTypeRequest(code, "META 测试模型", fields))
        };
        createTypeRequest.Headers.Add("Idempotency-Key", $"meta-create-{Guid.NewGuid():N}");
        createTypeRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var createTypeResponse = await admin.Client.SendAsync(createTypeRequest);
        var createTypeBody = await createTypeResponse.Content.ReadAsStringAsync();
        createTypeResponse.StatusCode.ShouldBe(HttpStatusCode.Created, createTypeBody);
        var createdType =
            (await createTypeResponse.Content.ReadFromJsonAsync<ApiResponse<ObjectTypeDto>>(
                ClientJsonOptions))!.Data!;
        createdType.Status.ShouldBe("Draft");
        createdType.Version.ShouldBe(1);
        using var researcher = await CreateAuthenticatedClientAsync("demo.researcher");
        var hiddenDraftTypes = await researcher.Client.GetFromJsonAsync<
            ApiResponse<IReadOnlyList<ObjectTypeDto>>>("/api/v1/object-types/published");
        hiddenDraftTypes!.Data!.ShouldNotContain(item => item.Code == code);

        using var updateTypeRequest = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/v1/object-types/{createdType.Id}/draft")
        {
            Content = JsonContent.Create(
                new UpdateObjectTypeDraftRequest(
                    createdType.Revision,
                    "META 测试模型 V2",
                    fields.Append(new ObjectFieldDto(
                        "Amount", "金额", "Number", false, 30, "Sensitive",
                        ShowInList: true, Filterable: true, SortOrder: 40, Code: "Amount"))
                    .ToArray()))
        };
        updateTypeRequest.Headers.Add("Idempotency-Key", $"meta-update-{Guid.NewGuid():N}");
        updateTypeRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var updateTypeResponse = await admin.Client.SendAsync(updateTypeRequest);
        updateTypeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updatedType =
            (await updateTypeResponse.Content.ReadFromJsonAsync<ApiResponse<ObjectTypeDto>>(
                ClientJsonOptions))!.Data!;
        updatedType.Version.ShouldBe(2);
        updatedType.Revision.ShouldBe(2);

        using var publishRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/object-types/{createdType.Id}/actions/publish")
        {
            Content = JsonContent.Create(new ObjectTypeActionRequest(updatedType.Revision))
        };
        publishRequest.Headers.Add("Idempotency-Key", $"meta-publish-{Guid.NewGuid():N}");
        publishRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var publishResponse = await admin.Client.SendAsync(publishRequest);
        publishResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var published =
            (await publishResponse.Content.ReadFromJsonAsync<ApiResponse<ObjectTypeDto>>(
                ClientJsonOptions))!.Data!;
        published.Status.ShouldBe("Published");
        var visiblePublishedTypes = await researcher.Client.GetFromJsonAsync<
            ApiResponse<IReadOnlyList<ObjectTypeDto>>>("/api/v1/object-types/published");
        visiblePublishedTypes!.Data!.Single(item => item.Code == code)
            .Fields.Select(field => field.Key)
            .ShouldBe(["Name", "Secret", "Status", "Amount"], ignoreOrder: true);

        var departmentId = await GetDepartmentIdAsync(admin, "RND");
        var objectCode = $"META-OBJ-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
        var createObjectResponse = await CreateObjectAsync(
            admin,
            $"meta-object-{Guid.NewGuid():N}",
            new CreateBusinessObjectRequest(
                Guid.Parse(published.Id),
                objectCode,
                new Dictionary<string, string?>
                {
                    ["Name"] = "动态对象",
                    ["Secret"] = "CORE-SECRET",
                    ["Status"] = "Open",
                    ["Amount"] = "12.5"
                },
                departmentId));
        createObjectResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var createdObject =
            (await createObjectResponse.Content.ReadFromJsonAsync<ApiResponse<BusinessObjectDto>>(
                ClientJsonOptions))!.Data!;

        var invalidOptionResponse = await CreateObjectAsync(
            admin,
            $"meta-invalid-option-{Guid.NewGuid():N}",
            new CreateBusinessObjectRequest(
                Guid.Parse(published.Id),
                $"{objectCode}-BAD",
                new Dictionary<string, string?>
                {
                    ["Name"] = "非法选项",
                    ["Status"] = "Invalid"
                },
                departmentId));
        invalidOptionResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var missingRequiredResponse = await CreateObjectAsync(
            admin,
            $"meta-missing-required-{Guid.NewGuid():N}",
            new CreateBusinessObjectRequest(
                Guid.Parse(published.Id),
                $"{objectCode}-REQ",
                new Dictionary<string, string?> { ["Status"] = "Open" },
                departmentId));
        missingRequiredResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        using var updateObjectRequest = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/v1/business-objects/{createdObject.Id}")
        {
            Content = JsonContent.Create(
                new UpdateBusinessObjectRequest(
                    createdObject.Version,
                    new Dictionary<string, string?>
                    {
                        ["Name"] = "已编辑对象",
                        ["Status"] = "Closed"
                    },
                    Guid.Parse(published.Id),
                    objectCode))
        };
        updateObjectRequest.Headers.Add("Idempotency-Key", $"meta-object-update-{Guid.NewGuid():N}");
        updateObjectRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var updateObjectResponse = await admin.Client.SendAsync(updateObjectRequest);
        updateObjectResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updatedObject =
            (await updateObjectResponse.Content.ReadFromJsonAsync<ApiResponse<BusinessObjectDto>>(
                ClientJsonOptions))!.Data!;
        updatedObject.Version.ShouldBe(2);
        updatedObject.Fields["Name"].ShouldBe("已编辑对象");
        updatedObject.Fields["Secret"].ShouldBe("CORE-SECRET");

        using var staleRequest = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/v1/business-objects/{createdObject.Id}")
        {
            Content = JsonContent.Create(
                new UpdateBusinessObjectRequest(
                    createdObject.Version,
                    new Dictionary<string, string?> { ["Name"] = "过期写入" }))
        };
        staleRequest.Headers.Add("Idempotency-Key", $"meta-stale-{Guid.NewGuid():N}");
        staleRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var staleResponse = await admin.Client.SendAsync(staleRequest);
        staleResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        using var deniedRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/object-types")
        {
            Content = JsonContent.Create(new CreateObjectTypeRequest(
                $"DENIED_{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
                "无权限模型",
                fields))
        };
        deniedRequest.Headers.Add("Idempotency-Key", $"meta-denied-{Guid.NewGuid():N}");
        deniedRequest.Headers.Add("X-CSRF-TOKEN", researcher.CsrfToken);
        (await researcher.Client.SendAsync(deniedRequest)).StatusCode.ShouldBe(
            HttpStatusCode.Forbidden);

        var researcherObjects =
            await researcher.Client.GetFromJsonAsync<ApiResponse<PagedResult<BusinessObjectDto>>>(
                "/api/v1/business-objects?pageSize=100");
        researcherObjects!.Data!.Items.Single(item => item.Id == createdObject.Id)
            .Fields["Secret"].ShouldBe("***");

        var objectVersions = await researcher.Client.GetFromJsonAsync<
            ApiResponse<IReadOnlyList<BusinessObjectVersionDto>>>(
            $"/api/v1/business-objects/{createdObject.Id}/versions?includeTestData=false");
        objectVersions!.Data!.Count.ShouldBeGreaterThanOrEqualTo(2);
        objectVersions.Data.ShouldContain(item => item.Version == updatedObject.Version);
        objectVersions.Data.SelectMany(item => item.Fields)
            .Where(item => item.Key == "Secret")
            .ShouldNotContain(item => item.Value != "***");

        var objectContext = await researcher.Client.GetFromJsonAsync<
            ApiResponse<BusinessObjectContextDto>>(
            $"/api/v1/business-objects/{createdObject.Id}/context?includeTestData=false",
            ClientJsonOptions);
        objectContext!.Data.ShouldNotBeNull();
        objectContext.Data.AuditEvents.ShouldContain(item => item.Action == "Create");

        var fieldUsers = await researcher.Client.GetFromJsonAsync<
            ApiResponse<IReadOnlyList<ObjectFieldUserOptionDto>>>(
            "/api/v1/business-objects/field-options/users?search=demo.researcher");
        fieldUsers!.Data!.ShouldContain(item => item.UserName == "demo.researcher");

        using var retireRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/object-types/{createdType.Id}/actions/retire")
        {
            Content = JsonContent.Create(new ObjectTypeActionRequest(published.Revision))
        };
        retireRequest.Headers.Add("Idempotency-Key", $"meta-retire-{Guid.NewGuid():N}");
        retireRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var retireResponse = await admin.Client.SendAsync(retireRequest);
        retireResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var retired =
            (await retireResponse.Content.ReadFromJsonAsync<ApiResponse<ObjectTypeDto>>(
                ClientJsonOptions))!.Data!;
        retired.Status.ShouldBe("Retired");

        using var restoreRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/object-types/{createdType.Id}/actions/restore")
        {
            Content = JsonContent.Create(new ObjectTypeActionRequest(retired.Revision))
        };
        restoreRequest.Headers.Add("Idempotency-Key", $"meta-restore-{Guid.NewGuid():N}");
        restoreRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        var restoreResponse = await admin.Client.SendAsync(restoreRequest);
        restoreResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var restored =
            (await restoreResponse.Content.ReadFromJsonAsync<ApiResponse<ObjectTypeDto>>(
                ClientJsonOptions))!.Data!;
        restored.Status.ShouldBe("Published");

        using var incompatibleRequest = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/v1/object-types/{createdType.Id}/draft")
        {
            Content = JsonContent.Create(
                new UpdateObjectTypeDraftRequest(
                    restored.Revision,
                    "不兼容字段变更",
                    fields.Select(field => field.Key == "Name"
                            ? field with { DataType = "Number" }
                            : field)
                        .ToArray()))
        };
        incompatibleRequest.Headers.Add(
            "Idempotency-Key",
            $"meta-incompatible-{Guid.NewGuid():N}");
        incompatibleRequest.Headers.Add("X-CSRF-TOKEN", admin.CsrfToken);
        (await admin.Client.SendAsync(incompatibleRequest)).StatusCode.ShouldBe(
            HttpStatusCode.BadRequest);

        var versions = await admin.Client.GetFromJsonAsync<
            ApiResponse<IReadOnlyList<ObjectTypeVersionDto>>>(
            $"/api/v1/object-types/{createdType.Id}/versions");
        versions!.Data!.Count.ShouldBeGreaterThanOrEqualTo(2);
        versions.Data.ShouldContain(item => item.Status == "Published");
    }

    private async Task<AuthenticatedClient> CreateAuthenticatedClientAsync(string userName)
    {
        var client = _factory.CreateClient();
        var anonymousToken = await GetCsrfTokenAsync(client);
        var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/demo-login")
        {
            Content = JsonContent.Create(new DemoLoginRequest(userName))
        };
        loginRequest.Headers.Add("X-CSRF-TOKEN", anonymousToken);
        var loginResponse = await client.SendAsync(loginRequest);
        loginResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Antiforgery binds to the authenticated identity.
        var authenticatedToken = await GetCsrfTokenAsync(client);
        return new AuthenticatedClient(client, authenticatedToken);
    }

    private static async Task<HttpResponseMessage> CreateObjectAsync(
        AuthenticatedClient authenticated,
        string idempotencyKey,
        CreateBusinessObjectRequest request)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/business-objects")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add("Idempotency-Key", idempotencyKey);
        message.Headers.Add("X-CSRF-TOKEN", authenticated.CsrfToken);
        return await authenticated.Client.SendAsync(message);
    }

    private static async Task<Guid> GetDepartmentIdAsync(
        AuthenticatedClient authenticated,
        string code)
    {
        var departments = await authenticated.Client.GetFromJsonAsync<
            ApiResponse<IReadOnlyList<DepartmentDto>>>("/api/v1/departments");
        return Guid.Parse(departments!.Data!.Single(item => item.Code == code).Id);
    }

    private static async Task<Guid> GetDepartmentIdByCodeAsync(string code)
    {
        var connectionString = Environment.GetEnvironmentVariable("AIDT_SQL_CONNECTION")
            ?? throw new InvalidOperationException("AIDT_SQL_CONNECTION is required.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT Id FROM [Identity].[Department] WHERE Code=@Code AND IsActive=1;";
        command.Parameters.AddWithValue("@Code", code);
        return (Guid)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException($"Department {code} is missing."));
    }

    private static async Task<Guid> GetUserIdAsync(string userName)
    {
        var connectionString = Environment.GetEnvironmentVariable("AIDT_SQL_CONNECTION")
            ?? throw new InvalidOperationException("AIDT_SQL_CONNECTION is required.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id FROM [Identity].[User] WHERE UserName=@UserName;";
        command.Parameters.AddWithValue("@UserName", userName);
        return (Guid)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException($"User not found: {userName}"));
    }

    private static async Task<string> GetObjectTypeIdAsync(
        AuthenticatedClient authenticated,
        string code)
    {
        var types = await authenticated.Client.GetFromJsonAsync<ApiResponse<PagedResult<ObjectTypeDto>>>(
            "/api/v1/object-types?pageSize=100");
        return types!.Data!.Items.Single(item => item.Code == code).Id;
    }

    private async Task<HttpResponseMessage> ConfirmMinutesActionAsync(
        string actionItemId,
        string idempotencyKey,
        ConfirmMinutesActionItemRequest body)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/minutes/action-items/{actionItemId}/actions/confirm")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        using var client = _factory.CreateClient();
        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/demo-login")
        {
            Content = JsonContent.Create(new DemoLoginRequest("demo.admin"))
        };
        login.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(client));
        await client.SendAsync(login);
        request.Headers.Remove("X-CSRF-TOKEN");
        request.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(client));
        return await client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SaveNodePolicyAsync(
        string workflowVersionId,
        string nodeCode,
        SaveWorkflowNodePolicyRequest request,
        string userName = "demo.admin")
    {
        using var client = _factory.CreateClient();
        var anonymousToken = await GetCsrfTokenAsync(client);
        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/demo-login")
        {
            Content = JsonContent.Create(new DemoLoginRequest(userName))
        };
        login.Headers.Add("X-CSRF-TOKEN", anonymousToken);
        (await client.SendAsync(login)).StatusCode.ShouldBe(HttpStatusCode.OK);
        using var policyRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/workflow-versions/{workflowVersionId}/nodes/{nodeCode}/policy")
        {
            Content = JsonContent.Create(request)
        };
        policyRequest.Headers.Add("Idempotency-Key", $"policy-{Guid.NewGuid():N}");
        policyRequest.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(client));
        return await client.SendAsync(policyRequest);
    }

    private static async Task SetTaskCreatedAtAsync(Guid taskId, DateTimeOffset createdAt)
    {
        var connectionString = Environment.GetEnvironmentVariable("AIDT_SQL_CONNECTION")
            ?? throw new InvalidOperationException("AIDT_SQL_CONNECTION is required.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE [Workflow].[WorkflowTask] SET [CreatedAt]=@CreatedAt WHERE [Id]=@TaskId;";
        command.Parameters.AddWithValue("@CreatedAt", createdAt.UtcDateTime);
        command.Parameters.AddWithValue("@TaskId", taskId);
        (await command.ExecuteNonQueryAsync()).ShouldBe(1);
    }

    private static async Task<SlaProcessingResultDto> RunSlaScanAsync(
        HttpClient client,
        string csrfToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/workflow-sla/actions/run");
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);
        var response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<SlaProcessingResultDto>>(
            ClientJsonOptions))!.Data!;
    }

    private static async Task<AutoProxyEvidence> GetAutoProxyEvidenceAsync(Guid taskId)
    {
        var connectionString = Environment.GetEnvironmentVariable("AIDT_SQL_CONNECTION")
            ?? throw new InvalidOperationException("AIDT_SQL_CONNECTION is required.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT TOP (1)
                run.RequestedBy,
                run.Status,
                run.ProxyTriggerMode,
                run.ProxyIdempotencyKey,
                profile.ServiceUserId,
                (SELECT COUNT(*) FROM [Agent].[AgentRun] allRuns
                 WHERE allRuns.WorkflowTaskId=@TaskId AND allRuns.SubjectType=N'WorkflowTaskProxy'),
                (SELECT TOP (1) [Reason] FROM [Audit].[AuditEvent]
                 WHERE [EntityId]=CONVERT(nvarchar(36),@TaskId) AND [Action]=N'task.agent_proxied'
                 ORDER BY [OccurredAt] DESC)
            FROM [Agent].[AgentRun] run
            JOIN [Agent].[AgentProfile] profile ON profile.Id=run.AgentProfileId
            WHERE run.WorkflowTaskId=@TaskId AND run.SubjectType=N'WorkflowTaskProxy'
            ORDER BY run.CreatedAt DESC;
            """;
        command.Parameters.AddWithValue("@TaskId", taskId);
        await using var reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).ShouldBeTrue();
        return new AutoProxyEvidence(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetGuid(4),
            reader.GetInt32(5),
            reader.GetString(6));
    }

    private static async Task InstallDefenseInDepthHighRiskPolicyAsync(Guid taskId, Guid instanceId)
    {
        var connectionString = Environment.GetEnvironmentVariable("AIDT_SQL_CONNECTION")
            ?? throw new InvalidOperationException("AIDT_SQL_CONNECTION is required.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            DECLARE @VersionId uniqueidentifier = (SELECT WorkflowVersionId FROM [Workflow].[WorkflowInstance] WHERE Id=@InstanceId);
            DECLARE @PolicyId uniqueidentifier = NEWID();
            DECLARE @ProfileId uniqueidentifier = (SELECT Id FROM [Agent].[AgentProfile] WHERE Name=N'流程代理数字员工' AND IsActive=1);
            DECLARE @PublisherId uniqueidentifier = (SELECT Id FROM [Identity].[User] WHERE UserName=N'demo.admin');
            DECLARE @NextVersion int = ISNULL((SELECT MAX(VersionNumber) FROM [Workflow].[WorkflowNodePolicy]
                WHERE WorkflowVersionId=@VersionId AND NodeCode=N'FINAL_RELEASE'),0)+1;
            INSERT INTO [Workflow].[WorkflowNodePolicy]
                (Id,WorkflowVersionId,NodeCode,VersionNumber,SlaMinutes,ReminderMinutes,EscalationMinutes,
                 TimeoutStrategy,AllowAgentAssist,AllowAgentProxy,AgentProfileId,AgentMaxRisk,AgentMaxAmount,
                 AgentMaxAutonomy,AgentProxyMode,AutoProxyAfterMinutes,PermanentHumanBoundary,
                 RequiredAttachmentCount,Status,PublishedBy,PublishedAt)
            VALUES
                (@PolicyId,@VersionId,N'FINAL_RELEASE',@NextVersion,120,30,60,N'Escalate',1,1,@ProfileId,
                 N'Low',0,N'A2',N'PreAuthorizedAutoProxy',60,1,0,N'Published',@PublisherId,SYSUTCDATETIME());
            UPDATE [Workflow].[WorkflowTask]
            SET NodeCode=N'FINAL_RELEASE',NodePolicyId=@PolicyId,Status=N'Pending',
                DueAt=DATEADD(minute,120,SYSUTCDATETIME()),DecisionMode=N'Human',AgentRunId=NULL
            WHERE Id=@TaskId;
            """;
        command.Parameters.AddWithValue("@TaskId", taskId);
        command.Parameters.AddWithValue("@InstanceId", instanceId);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<int> CountProxyRunsAsync(Guid taskId)
    {
        var connectionString = Environment.GetEnvironmentVariable("AIDT_SQL_CONNECTION")
            ?? throw new InvalidOperationException("AIDT_SQL_CONNECTION is required.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM [Agent].[AgentRun] WHERE WorkflowTaskId=@TaskId AND SubjectType=N'WorkflowTaskProxy';";
        command.Parameters.AddWithValue("@TaskId", taskId);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    private sealed record AutoProxyEvidence(
        Guid RequestedBy,
        string Status,
        string TriggerMode,
        string IdempotencyKey,
        Guid ServiceUserId,
        int ProxyCount,
        string AuditReason);

    private static async Task<HttpResponseMessage> UploadFileAsync(
        AuthenticatedClient authenticated,
        Guid departmentId,
        byte[] content,
        string fileName,
        string contentType,
        string sensitivity,
        Guid? businessObjectId = null,
        Guid? workflowInstanceId = null,
        Guid? workflowTaskId = null,
        string? attachmentId = null)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(content);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        form.Add(fileContent, "file", fileName);
        form.Add(new StringContent(departmentId.ToString("D")), "departmentId");
        form.Add(new StringContent(sensitivity), "sensitivity");
        if (businessObjectId.HasValue)
        {
            form.Add(new StringContent(businessObjectId.Value.ToString("D")), "businessObjectId");
        }
        if (workflowInstanceId.HasValue)
        {
            form.Add(new StringContent(workflowInstanceId.Value.ToString("D")), "workflowInstanceId");
        }
        if (workflowTaskId.HasValue)
        {
            form.Add(new StringContent(workflowTaskId.Value.ToString("D")), "workflowTaskId");
        }
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            attachmentId is null
                ? "/api/v1/files"
                : $"/api/v1/files/{attachmentId}/versions")
        {
            Content = form
        };
        request.Headers.Add("X-CSRF-TOKEN", authenticated.CsrfToken);
        return await authenticated.Client.SendAsync(request);
    }

    private static async Task<TestEnvironmentRows> InsertTestEnvironmentDataAsync(
        Guid departmentId,
        string fileName)
    {
        var connectionString = Environment.GetEnvironmentVariable("AIDT_SQL_CONNECTION")
            ?? throw new InvalidOperationException("AIDT_SQL_CONNECTION is required.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        var objectId = Guid.NewGuid();
        var instanceId = Guid.NewGuid();
        var attachmentId = Guid.NewGuid();
        var objectCode = $"ENV-OBJ-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            DECLARE @OrganizationId uniqueidentifier = (SELECT OrganizationId FROM [Identity].[Department] WHERE Id=@DepartmentId);
            DECLARE @CreatedBy uniqueidentifier = (SELECT Id FROM [Identity].[User] WHERE UserName=N'demo.admin');
            DECLARE @ObjectTypeId uniqueidentifier = (SELECT Id FROM [Metadata].[ObjectType] WHERE Code=N'CUSTOMER_SAMPLE');
            DECLARE @WorkflowDefinitionId uniqueidentifier;
            DECLARE @WorkflowVersionId uniqueidentifier;
            SELECT TOP (1)
                @WorkflowDefinitionId = WorkflowDefinitionId,
                @WorkflowVersionId = Id
            FROM [Workflow].[WorkflowVersion]
            WHERE Status=N'Published'
            ORDER BY VersionNumber DESC;

            INSERT INTO [BusinessObject].[BusinessObject]
                (Id, OrganizationId, DepartmentId, EnvironmentCode, IsTestData, SourceSystem,
                 ObjectTypeId, ObjectCode, Status, Version, FieldsJson, CreatedAt, UpdatedAt, IsDeleted)
            VALUES
                (@ObjectId, @OrganizationId, @DepartmentId, N'test', 1, N'AIDT-INTEGRATION',
                 @ObjectTypeId, @ObjectCode, N'Active', 1, N'{}', SYSUTCDATETIME(), SYSUTCDATETIME(), 0);

            INSERT INTO [Workflow].[WorkflowInstance]
                (Id, WorkflowDefinitionId, WorkflowVersionId, BusinessObjectId, DepartmentId,
                 StartedByUserId, EnvironmentCode, IsTestData, SourceSystem, Status, Version,
                 CurrentNodeCode, StartedAt, EndedAt)
            VALUES
                (@InstanceId, @WorkflowDefinitionId, @WorkflowVersionId, @ObjectId, @DepartmentId,
                 @CreatedBy, N'test', 1, N'AIDT-INTEGRATION', N'Running', 1,
                 N'QA_REVIEW', SYSUTCDATETIME(), NULL);

            INSERT INTO [Document].[Attachment]
                (Id, OrganizationId, DepartmentId, BusinessObjectId, StoragePath, OriginalFileName,
                 MimeType, SizeBytes, Sha256, VirusScanStatus, Sensitivity, Status, EnvironmentCode,
                 IsTestData, SourceSystem, CurrentVersionNumber, CreatedBy, CreatedAt, UpdatedAt,
                 DeletionRequestedBy, DeletionRequestedAt, DeletionReason)
            VALUES
                (@AttachmentId, @OrganizationId, @DepartmentId, @ObjectId, N'test/environment-marker.txt',
                 @FileName, N'text/plain', 10, REPLICATE('A', 64), N'NotConfigured', N'Internal',
                 N'Active', N'test', 1, N'AIDT-INTEGRATION', 1, @CreatedBy,
                 SYSUTCDATETIME(), SYSUTCDATETIME(), NULL, NULL, NULL);
            """;
        command.Parameters.AddWithValue("@ObjectId", objectId);
        command.Parameters.AddWithValue("@InstanceId", instanceId);
        command.Parameters.AddWithValue("@AttachmentId", attachmentId);
        command.Parameters.AddWithValue("@DepartmentId", departmentId);
        command.Parameters.AddWithValue("@FileName", fileName);
        command.Parameters.AddWithValue("@ObjectCode", objectCode);
        await command.ExecuteNonQueryAsync();
        return new TestEnvironmentRows(objectId, instanceId, attachmentId, objectCode);
    }

    private static async Task DeleteTestEnvironmentDataAsync(TestEnvironmentRows rows)
    {
        var connectionString = Environment.GetEnvironmentVariable("AIDT_SQL_CONNECTION")
            ?? throw new InvalidOperationException("AIDT_SQL_CONNECTION is required.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            DELETE FROM [Document].[AttachmentLink] WHERE AttachmentId=@AttachmentId;
            DELETE FROM [Document].[DocumentVersion] WHERE DocumentId=@AttachmentId;
            DELETE FROM [Document].[Attachment] WHERE Id=@AttachmentId;
            DELETE FROM [Workflow].[WorkflowTask] WHERE WorkflowInstanceId=@InstanceId;
            DELETE FROM [Workflow].[WorkflowInstance] WHERE Id=@InstanceId;
            DELETE FROM [BusinessObject].[BusinessObjectVersion] WHERE BusinessObjectId=@ObjectId;
            DELETE FROM [BusinessObject].[BusinessObject] WHERE Id=@ObjectId;
            """;
        command.Parameters.AddWithValue("@AttachmentId", rows.AttachmentId);
        command.Parameters.AddWithValue("@InstanceId", rows.InstanceId);
        command.Parameters.AddWithValue("@ObjectId", rows.ObjectId);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DeleteDepartmentAsync(Guid departmentId)
    {
        var connectionString = Environment.GetEnvironmentVariable("AIDT_SQL_CONNECTION")
            ?? throw new InvalidOperationException("AIDT_SQL_CONNECTION is required.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            DELETE FROM [Identity].[DataScope]
            WHERE ScopeType=N'department'
              AND ScopeReference=CONVERT(nvarchar(36), @DepartmentId);
            DELETE FROM [Identity].[UserDepartmentMembership] WHERE DepartmentId=@DepartmentId;
            DELETE FROM [Identity].[Department] WHERE Id=@DepartmentId;
            """;
        command.Parameters.AddWithValue("@DepartmentId", departmentId);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        using var document = JsonDocument.Parse(
            await client.GetStringAsync("/api/v1/auth/csrf"));
        return document.RootElement
            .GetProperty("data")
            .GetProperty("requestToken")
            .GetString() ?? throw new InvalidOperationException("CSRF token is missing.");
    }

    private static JsonSerializerOptions CreateClientJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        return options;
    }

    private sealed record PlatformPayload(
        string ProjectState,
        string DatabaseMigrationState,
        string DatabaseProvider,
        string ExternalSystemsMode,
        string CurrentEnvironmentCode,
        long MaxUploadBytes,
        IReadOnlyList<CapabilityDto> Capabilities);

    private sealed record AuthenticatedClient(HttpClient Client, string CsrfToken) : IDisposable
    {
        public void Dispose() => Client.Dispose();
    }

    private sealed record TestEnvironmentRows(
        Guid ObjectId,
        Guid InstanceId,
        Guid AttachmentId,
        string ObjectCode);

    private sealed record DepartmentPilot(
        string UserName,
        string DepartmentCode,
        string TemplateName);

    private sealed record InboxPersistence(
        string EventStatus,
        string SuggestionStatus,
        int TaskCount);

    private sealed class TestGatewayClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(new TestGatewayHandler(), disposeHandler: true)
            {
                BaseAddress = new Uri("http://gateway.test")
            };
    }

    private sealed class TestGatewayHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            using var document = JsonDocument.Parse(
                await request.Content!.ReadAsStringAsync(cancellationToken));
            var model = document.RootElement.GetProperty("model").GetString() ?? string.Empty;
            if (model != "aidt-rule-sim")
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = JsonContent.Create(new
                    {
                        error = new { code = "MODEL_NOT_ALLOWED", message = "模型未授权" }
                    })
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    id = $"chatcmpl-mock-{Guid.NewGuid():N}",
                    @object = "chat.completion",
                    created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    model,
                    choices = new[]
                    {
                        new
                        {
                            index = 0,
                            message = new { role = "assistant", content = "AIDT 规则化模拟结果" },
                            finishReason = "stop"
                        }
                    },
                    audit = new
                    {
                        mode = "RuleSimulator",
                        externalModelCalled = false,
                        inputDigest = new string('A', 64),
                        outputDigest = new string('B', 64),
                        timeoutMs = 10_000
                    }
                })
            };
        }
    }
}
