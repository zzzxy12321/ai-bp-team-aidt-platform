using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AIDT.Api.Contracts;
using AIDT.Application.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Shouldly;

namespace AIDT.UnitTests.Api;

public sealed class WorkbenchApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions ClientJsonOptions = CreateJsonOptions();
    private readonly WebApplicationFactory<Program> _factory;

    public WorkbenchApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [SqlIntegrationFact]
    public async Task DefaultTemplates_ResolveByRoleAndDepartmentWithRealDifferences()
    {
        await CleanupNonSystemTemplatesAsync();
        using var researcher = await CreateAuthenticatedClientAsync("demo.researcher");
        using var qa = await CreateAuthenticatedClientAsync("demo.qa");
        using var supply = await CreateAuthenticatedClientAsync("demo.supply");
        using var finance = await CreateAuthenticatedClientAsync("demo.finance");
        using var pm = await CreateAuthenticatedClientAsync("demo.pm");
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        using var production = await CreateAuthenticatedClientAsync("demo.production");

        var rnd = await GetDepartmentIdAsync(researcher.Client, "RND");
        var quality = await GetDepartmentIdAsync(qa.Client, "QA");
        var supplyDepartment = await GetDepartmentIdAsync(supply.Client, "SUPPLY");
        var financeDepartment = await GetDepartmentIdAsync(finance.Client, "FINANCE");
        var productionDepartment = await GetDepartmentIdAsync(production.Client, "PRODUCTION");

        var rndWorkbench = await GetAsync<DepartmentWorkbenchDto>(
            researcher.Client,
            $"/api/v1/departments/{rnd}/workbench");
        var qaWorkbench = await GetAsync<DepartmentWorkbenchDto>(
            qa.Client,
            $"/api/v1/departments/{quality}/workbench");
        var supplyWorkbench = await GetAsync<DepartmentWorkbenchDto>(
            supply.Client,
            $"/api/v1/departments/{supplyDepartment}/workbench");
        var financeWorkbench = await GetAsync<DepartmentWorkbenchDto>(
            finance.Client,
            $"/api/v1/departments/{financeDepartment}/workbench");
        var pmWorkbench = await GetAsync<DepartmentWorkbenchDto>(
            pm.Client,
            $"/api/v1/departments/{rnd}/workbench");
        var managerWorkbench = await GetAsync<DepartmentWorkbenchDto>(
            admin.Client,
            $"/api/v1/departments/{rnd}/workbench");
        var fallback = await GetAsync<DepartmentWorkbenchDto>(
            production.Client,
            $"/api/v1/departments/{productionDepartment}/workbench");

        rndWorkbench.TemplateCode.ShouldBe("WS-RND");
        qaWorkbench.TemplateCode.ShouldBe("WS-QA");
        supplyWorkbench.TemplateCode.ShouldBe("WS-SCM");
        financeWorkbench.TemplateCode.ShouldBe("WS-FIN");
        pmWorkbench.TemplateCode.ShouldBe("WS-PM");
        managerWorkbench.TemplateCode.ShouldBe("WS-MGR");
        fallback.Matched.ShouldBeFalse();
        fallback.FallbackMode.ShouldBe("GenericDepartmentWorkspace");

        rndWorkbench.PrimaryQuestion.ShouldNotBe(qaWorkbench.PrimaryQuestion);
        supplyWorkbench.PrimaryQuestion.ShouldNotBe(financeWorkbench.PrimaryQuestion);
        rndWorkbench.Cards.Select(card => card.Title)
            .ShouldNotBe(qaWorkbench.Cards.Select(card => card.Title));
        pmWorkbench.Cards.ShouldContain(card => card.CardType == "project_portfolio");
        managerWorkbench.Cards.ShouldContain(card => card.CardType == "team_pending");

        var crossDepartment = await researcher.Client.GetAsync(
            $"/api/v1/departments/{quality}/workbench");
        crossDepartment.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [SqlIntegrationFact]
    public async Task UserPriority_DraftPublishRetireRestoreAndCardPermissions_AreEnforced()
    {
        await CleanupNonSystemTemplatesAsync();
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var rnd = await GetDepartmentIdAsync(admin.Client, "RND");
        var researcherId = await GetUserIdAsync(admin, "demo.researcher");
        var code = $"WS-TEST-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
        var created = await PostJsonAsync<WorkbenchTemplateDto>(
            admin,
            "/api/v1/workbench-templates",
            new CreateWorkbenchTemplateRequest(
                code,
                "研发用户优先模板",
                "API 集成测试模板",
                "研发验收岗位",
                "用户级模板是否优先？",
                Guid.Parse(rnd),
                5,
                false),
            $"workbench-create-{Guid.NewGuid():N}");

        var draft = created.DraftVersion.ShouldNotBeNull();
        var updated = await PutJsonAsync<WorkbenchTemplateDto>(
            admin,
            $"/api/v1/workbench-templates/{created.Id}/draft",
            new UpdateWorkbenchTemplateDraftRequest(
                draft.Revision,
                created.Name,
                created.Description,
                created.AudienceTitle,
                created.PrimaryQuestion,
                Guid.Parse(rnd),
                5,
                false,
                [
                    new WorkbenchCardWriteRequest(
                        "work_items",
                        "用户级真实事项",
                        "只读取现有事项",
                        10,
                        "Half",
                        "ExistingApi",
                        new Dictionary<string, string?> { ["top"] = "5" },
                        "dashboard",
                        "Read"),
                    new WorkbenchCardWriteRequest(
                        "business_objects",
                        "不可见卡",
                        "无权限时必须不返回",
                        20,
                        "Half",
                        "ExistingApi",
                        new Dictionary<string, string?> { ["top"] = "5" },
                        "unavailable-resource",
                        "Read")
                ],
                [new WorkbenchAssignmentWriteRequest("User", researcherId, 500)]),
            null);
        updated.DraftVersion.ShouldNotBeNull();

        using var researcher = await CreateAuthenticatedClientAsync("demo.researcher");
        var beforePublish = await GetAsync<DepartmentWorkbenchDto>(
            researcher.Client,
            $"/api/v1/departments/{rnd}/workbench");
        beforePublish.TemplateCode.ShouldBe("WS-RND");

        var validation = await PostJsonAsync<WorkbenchTemplateValidationDto>(
            admin,
            $"/api/v1/workbench-templates/{created.Id}/actions/validate",
            new { },
            null);
        validation.IsValid.ShouldBeTrue();
        validation.VisibleCardCount.ShouldBe(2);

        var published = await PostJsonAsync<WorkbenchTemplateDto>(
            admin,
            $"/api/v1/workbench-templates/{created.Id}/actions/publish",
            new { },
            $"workbench-publish-{Guid.NewGuid():N}");
        published.PublishedVersion.ShouldNotBeNull();

        var afterPublish = await GetAsync<DepartmentWorkbenchDto>(
            researcher.Client,
            $"/api/v1/departments/{rnd}/workbench");
        afterPublish.TemplateId.ShouldBe(created.Id);
        afterPublish.Cards.Count.ShouldBe(1);
        afterPublish.Cards.Single().Title.ShouldBe("用户级真实事项");

        var retired = await PostJsonAsync<WorkbenchTemplateDto>(
            admin,
            $"/api/v1/workbench-templates/{created.Id}/actions/retire",
            new { },
            null);
        retired.Status.ShouldBe("Retired");
        var afterRetire = await GetAsync<DepartmentWorkbenchDto>(
            researcher.Client,
            $"/api/v1/departments/{rnd}/workbench");
        afterRetire.TemplateCode.ShouldBe("WS-RND");

        await PostJsonAsync<WorkbenchTemplateDto>(
            admin,
            $"/api/v1/workbench-templates/{created.Id}/actions/restore",
            new { },
            null);
        var afterRestore = await GetAsync<DepartmentWorkbenchDto>(
            researcher.Client,
            $"/api/v1/departments/{rnd}/workbench");
        afterRestore.TemplateId.ShouldBe(created.Id);

        var versions = await GetAsync<IReadOnlyList<WorkbenchTemplateVersionDto>>(
            admin.Client,
            $"/api/v1/workbench-templates/{created.Id}/versions");
        versions.Count.ShouldBe(1);
        versions.Single().Status.ShouldBe("Published");
    }

    [SqlIntegrationFact]
    public async Task DepartmentManager_CanCopyAuthorizedDepartmentSystemTemplateOnly()
    {
        await CleanupNonSystemTemplatesAsync();
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        using var qa = await CreateAuthenticatedClientAsync("demo.qa");
        using var researcher = await CreateAuthenticatedClientAsync("demo.researcher");
        var templates = await GetAsync<IReadOnlyList<WorkbenchTemplateDto>>(
            admin.Client,
            "/api/v1/workbench-templates");
        var qaSystem = templates.Single(item => item.Code == "WS-QA");
        var qaDepartmentId = qaSystem.DepartmentId.ShouldNotBeNull();

        var qaVisible = await GetAsync<IReadOnlyList<WorkbenchTemplateDto>>(
            qa.Client,
            "/api/v1/workbench-templates");
        qaVisible.ShouldContain(item => item.Code == "WS-QA");
        var copied = await PostJsonAsync<WorkbenchTemplateDto>(
            qa,
            $"/api/v1/workbench-templates/{qaSystem.Id}/actions/copy",
            new CopyWorkbenchTemplateRequest(
                $"WS-QA-COPY-{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
                "质量部门业务模板",
                Guid.Parse(qaDepartmentId),
                "质量岗位"),
            $"workbench-copy-{Guid.NewGuid():N}");
        copied.IsSystem.ShouldBeFalse();
        copied.DepartmentId.ShouldBe(qaDepartmentId);

        var forbidden = await PostJsonRawAsync(
            researcher,
            $"/api/v1/workbench-templates/{qaSystem.Id}/actions/copy",
            new CopyWorkbenchTemplateRequest(
                $"WS-QA-DENY-{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
                "越权复制",
                Guid.Parse(qaDepartmentId),
                "质量岗位"),
            $"workbench-copy-deny-{Guid.NewGuid():N}");
        forbidden.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private async Task<string> GetDepartmentIdAsync(HttpClient client, string code)
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var tree = await GetAsync<IReadOnlyList<DepartmentTreeNodeDto>>(
            admin.Client,
            "/api/v1/iam/organization-tree");
        _ = client;
        return Flatten(tree).Single(item => item.Code == code).Id;
    }

    private static async Task<string> GetUserIdAsync(AuthenticatedClient admin, string userName)
    {
        var users = await GetAsync<PagedResult<IamUserListItemDto>>(
            admin.Client,
            $"/api/v1/iam/users?search={Uri.EscapeDataString(userName)}");
        return users.Items.Single(item => item.UserName == userName).Id;
    }

    private async Task<AuthenticatedClient> CreateAuthenticatedClientAsync(string userName)
    {
        var client = _factory.CreateClient();
        var csrf = await GetCsrfTokenAsync(client);
        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/demo-login")
        {
            Content = JsonContent.Create(new DemoLoginRequest(userName))
        };
        login.Headers.Add("X-CSRF-TOKEN", csrf);
        var response = await client.SendAsync(login);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return new AuthenticatedClient(client, await GetCsrfTokenAsync(client));
    }

    private static async Task<T> GetAsync<T>(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(ClientJsonOptions);
        return payload!.Data!;
    }

    private static async Task<T> PostJsonAsync<T>(
        AuthenticatedClient authenticated,
        string path,
        object body,
        string? idempotencyKey)
    {
        using var response = await PostJsonRawAsync(authenticated, path, body, idempotencyKey);
        response.StatusCode.ShouldBeOneOf(
            [HttpStatusCode.OK, HttpStatusCode.Created],
            await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(ClientJsonOptions);
        return payload!.Data!;
    }

    private static async Task<HttpResponseMessage> PostJsonRawAsync(
        AuthenticatedClient authenticated,
        string path,
        object body,
        string? idempotencyKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body, options: ClientJsonOptions)
        };
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }
        request.Headers.Add("X-CSRF-TOKEN", authenticated.CsrfToken);
        return await authenticated.Client.SendAsync(request);
    }

    private static async Task<T> PutJsonAsync<T>(
        AuthenticatedClient authenticated,
        string path,
        object body,
        string? idempotencyKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, path)
        {
            Content = JsonContent.Create(body, options: ClientJsonOptions)
        };
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }
        request.Headers.Add("X-CSRF-TOKEN", authenticated.CsrfToken);
        using var response = await authenticated.Client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(ClientJsonOptions);
        return payload!.Data!;
    }

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        using var document = JsonDocument.Parse(await client.GetStringAsync("/api/v1/auth/csrf"));
        return document.RootElement
            .GetProperty("data")
            .GetProperty("requestToken")
            .GetString() ?? throw new InvalidOperationException("CSRF token is missing.");
    }

    private static List<DepartmentTreeNodeDto> Flatten(
        IReadOnlyList<DepartmentTreeNodeDto> nodes) =>
        nodes.SelectMany(node => new[] { node }.Concat(Flatten(node.Children))).ToList();

    private static async Task CleanupNonSystemTemplatesAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("AIDT_SQL_CONNECTION")
            ?? throw new InvalidOperationException("AIDT_SQL_CONNECTION is required.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            DELETE assignmentRow
            FROM [Workspace].[WorkbenchAssignment] assignmentRow
            JOIN [Workspace].[WorkbenchTemplateVersion] versionRow
                ON versionRow.Id = assignmentRow.TemplateVersionId
            JOIN [Workspace].[WorkbenchTemplate] templateRow
                ON templateRow.Id = versionRow.TemplateId
            WHERE templateRow.IsSystem = 0;

            DELETE cardRow
            FROM [Workspace].[WorkbenchCard] cardRow
            JOIN [Workspace].[WorkbenchTemplateVersion] versionRow
                ON versionRow.Id = cardRow.TemplateVersionId
            JOIN [Workspace].[WorkbenchTemplate] templateRow
                ON templateRow.Id = versionRow.TemplateId
            WHERE templateRow.IsSystem = 0;

            DELETE versionRow
            FROM [Workspace].[WorkbenchTemplateVersion] versionRow
            JOIN [Workspace].[WorkbenchTemplate] templateRow
                ON templateRow.Id = versionRow.TemplateId
            WHERE templateRow.IsSystem = 0;

            DELETE FROM [Workspace].[WorkbenchTemplate] WHERE IsSystem = 0;
            DELETE FROM [Integration].[IdempotencyRecord]
            WHERE ResponseType = N'WorkbenchTemplateEntity';
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        return options;
    }

    private sealed record AuthenticatedClient(HttpClient Client, string CsrfToken) : IDisposable
    {
        public void Dispose() => Client.Dispose();
    }
}
