using System.Net;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Common;
using AIDT.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace AIDT.McpTests;

public sealed class McpOAuthAndProtocolTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public McpOAuthAndProtocolTests(WebApplicationFactory<Program> factory)
    {
        var connection = Environment.GetEnvironmentVariable("AIDT_SQL_CONNECTION")
            ?? "Server=np:\\.\\pipe\\sql\\query;Database=AIDT;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5";
        _factory = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:Aidt", connection));
    }

    [SqlIntegrationFact]
    public async Task OAuthPkce_ToolDiscoveryAndWhoAmI_ArePersistedAuditedAndRevocable()
    {
        var token = await IssueTokenAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        var initialize = await SendMcpAsync(
            client,
            """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"integration","version":"1.0"}}}""");
        initialize.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await initialize.Content.ReadAsStringAsync()).ShouldContain("protocolVersion");

        var list = await SendMcpAsync(
            client,
            """{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}""");
        list.StatusCode.ShouldBe(HttpStatusCode.OK);
        var listBody = await list.Content.ReadAsStringAsync();
        listBody.ShouldContain("aidt_whoami");
        listBody.ShouldContain("aidt_list_projects");
        listBody.ShouldNotContain("aidt_decide_workflow_task");

        var call = await SendMcpAsync(
            client,
            """{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"aidt_whoami","arguments":{}}}""");
        call.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await call.Content.ReadAsStringAsync()).ShouldContain(token.UserId.ToString("D"));

        var crossDepartmentEventId = Guid.NewGuid();
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var inbox = scope.ServiceProvider.GetRequiredService<IEventInboxService>();
            var admin = await users.GetPrincipalByUserNameAsync("demo.admin", CancellationToken.None)
                ?? throw new InvalidOperationException("demo.admin is missing.");
            var qaDepartment = (await scope.ServiceProvider
                    .GetRequiredService<IEmployeeWorkspaceQueryService>()
                    .ListDepartmentsAsync(admin, CancellationToken.None))
                .Single(item => item.Code == "QA");
            var created = await inbox.ReceiveAsync(
                admin,
                new ReceiveInboxEventRequest(
                    Guid.Parse(qaDepartment.Id),
                    "MCP 跨部门拒绝测试",
                    "该事项属于质量部门，RND 范围的 MCP token 不得读取。",
                    "ManualText"),
                $"mcp-cross-{Guid.NewGuid():N}",
                "trace-mcp-cross",
                CancellationToken.None);
            crossDepartmentEventId = Guid.Parse(created.Id);
        }

        var crossDepartment = await SendMcpAsync(
            client,
            JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = 4,
                method = "tools/call",
                @params = new
                {
                    name = "aidt_get_event",
                    arguments = new
                    {
                        arguments = new { id = crossDepartmentEventId.ToString("D") }
                    }
                }
            }));
        var crossDepartmentBody = await crossDepartment.Content.ReadAsStringAsync();
        crossDepartmentBody.ShouldContain("\"isError\":true");
        crossDepartmentBody.ShouldContain("DEPARTMENT_SCOPE_DENIED");

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AidtDbContext>();
            var audits = await db.AuditEvents
                .Where(item => item.Module == "MCP" && item.Action == "tool.call")
                .OrderByDescending(item => item.OccurredAt)
                .Take(5)
                .ToListAsync();
            audits.ShouldContain(item => item.AfterJson!.Contains("aidt_whoami"));
        }

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var oauth = scope.ServiceProvider.GetRequiredService<IMcpOAuthService>();
            var refreshed = await oauth.RefreshAsync(
                token.ClientId,
                clientSecret: null,
                token.RefreshToken,
                CancellationToken.None)
                ?? throw new InvalidOperationException("Refresh token rotation failed.");
            var oldAccess = await oauth.ValidateAccessTokenAsync(token.AccessToken, CancellationToken.None);
            oldAccess.Identity.ShouldBeNull();
            var newAccess = await oauth.ValidateAccessTokenAsync(refreshed.AccessToken, CancellationToken.None);
            newAccess.Identity.ShouldNotBeNull();

            await oauth.RevokeAsync(
                token.ClientId,
                clientSecret: null,
                refreshed.RefreshToken,
                CancellationToken.None);
            var revoked = await oauth.ValidateAccessTokenAsync(refreshed.AccessToken, CancellationToken.None);
            revoked.Identity.ShouldBeNull();
        }
    }

    [SqlIntegrationFact]
    public async Task StdioTransport_InitializesListsToolsAndCallsWhoAmI()
    {
        var token = await IssueTokenAsync();
        var assemblyPath = typeof(AIDT.Mcp.AidtMcpOptions).Assembly.Location;
        var connection = Environment.GetEnvironmentVariable("AIDT_SQL_CONNECTION")
            ?? throw new InvalidOperationException("AIDT_SQL_CONNECTION is required.");
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = Path.GetDirectoryName(assemblyPath)!,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(assemblyPath);
        startInfo.ArgumentList.Add("--stdio");
        startInfo.Environment["ConnectionStrings__Aidt"] = connection;
        startInfo.Environment["AIDT_MCP_TOKEN"] = token.AccessToken;
        startInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Unable to start AIDT.Mcp stdio process.");
        try
        {
            await process.StandardInput.WriteLineAsync(
                """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"stdio-test","version":"1.0"}}}""");
            await process.StandardInput.WriteLineAsync(
                """{"jsonrpc":"2.0","method":"notifications/initialized","params":{}}""");
            await process.StandardInput.WriteLineAsync(
                """{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}""");
            await process.StandardInput.WriteLineAsync(
                """{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"aidt_whoami","arguments":{}}}""");
            await process.StandardInput.FlushAsync();

            var responses = new List<string>();
            var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
            while (DateTimeOffset.UtcNow < deadline
                   && !(responses.Any(line => line.Contains("\"id\":1", StringComparison.Ordinal))
                        && responses.Any(line => line.Contains("\"id\":2", StringComparison.Ordinal))
                        && responses.Any(line => line.Contains("\"id\":3", StringComparison.Ordinal))))
            {
                var remaining = deadline - DateTimeOffset.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    break;
                }

                var line = await process.StandardOutput.ReadLineAsync()
                    .WaitAsync(remaining);
                if (line is not null)
                {
                    responses.Add(line);
                }
            }

            var combined = string.Join('\n', responses);
            combined.ShouldContain("protocolVersion");
            combined.ShouldContain("aidt_whoami");
            combined.ShouldContain(token.UserId.ToString("D"));
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
    }

    [SqlIntegrationFact]
    public async Task HighRiskConfirmation_IsUserBoundSingleUseAndAudited()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var confirmations = scope.ServiceProvider.GetRequiredService<IMcpHumanConfirmationService>();
        var user = await users.GetPrincipalByUserNameAsync("demo.pm", CancellationToken.None)
            ?? throw new InvalidOperationException("demo.pm is missing.");
        var other = await users.GetPrincipalByUserNameAsync("demo.qa", CancellationToken.None)
            ?? throw new InvalidOperationException("demo.qa is missing.");
        var requestHash = Convert.ToHexString(SHA256.HashData(Guid.NewGuid().ToByteArray()));
        var resourceId = Guid.NewGuid().ToString("D");
        var confirmation = await confirmations.RequestAsync(
            new McpConfirmationRequest(
                user.UserId,
                user.OrganizationId,
                "client-human-boundary",
                "aidt_decide_workflow_task",
                "WorkflowTask",
                resourceId,
                requestHash,
                "通过流程任务"),
            CancellationToken.None);

        await Should.ThrowAsync<ConflictException>(() =>
            confirmations.ConfirmAsync(
                confirmation.Id,
                other,
                "not owner",
                "trace-other",
                CancellationToken.None));
        var confirmed = await confirmations.ConfirmAsync(
            confirmation.Id,
            user,
            "已核对",
            "trace-owner",
            CancellationToken.None);
        confirmed.Status.ShouldBe("Confirmed");

        await confirmations.EnsureConsumedAsync(
            confirmation.Id,
            user,
            "client-human-boundary",
            "aidt_decide_workflow_task",
            "WorkflowTask",
            resourceId,
            requestHash,
            CancellationToken.None);
        await Should.ThrowAsync<ConflictException>(() =>
            confirmations.EnsureConsumedAsync(
                confirmation.Id,
                user,
                "client-human-boundary",
                "aidt_decide_workflow_task",
                "WorkflowTask",
                resourceId,
                requestHash,
                CancellationToken.None));

        var db = scope.ServiceProvider.GetRequiredService<AidtDbContext>();
        (await db.AuditEvents.CountAsync(item =>
            item.Module == "MCP"
            && item.Action == "confirmation.confirmed"
            && item.EntityId == confirmation.Id.ToString("D"),
            CancellationToken.None)).ShouldBe(1);
    }

    [SqlIntegrationFact]
    public async Task ServiceIdentityToken_RequiresMcpManagerAndInheritsOnlyExplicitScope()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var oauth = scope.ServiceProvider.GetRequiredService<IMcpOAuthService>();
        var admin = await users.GetPrincipalByUserNameAsync("demo.admin", CancellationToken.None)
            ?? throw new InvalidOperationException("demo.admin is missing.");
        var serviceUser = await users.GetPrincipalByUserNameAsync("agent.project", CancellationToken.None)
            ?? throw new InvalidOperationException("agent.project is missing.");
        var projectManager = await users.GetPrincipalByUserNameAsync("demo.pm", CancellationToken.None)
            ?? throw new InvalidOperationException("demo.pm is missing.");
        var departmentId = (await scope.ServiceProvider
                .GetRequiredService<IEmployeeWorkspaceQueryService>()
                .ListDepartmentsAsync(admin, CancellationToken.None))
            .Single(item => item.Code == "RND").Id;
        var agentProfile = (await scope.ServiceProvider
                .GetRequiredService<IWorkspaceQueryService>()
                .ListAgentProfilesAsync(admin, CancellationToken.None))
            .Single(item => item.ServiceUserId == serviceUser.UserId.ToString("D"));

        await Should.ThrowAsync<PermissionException>(() =>
            oauth.IssueServiceTokenAsync(
                projectManager,
                new McpServiceTokenRequest(
                    serviceUser.UserId,
                    "Unauthorized service token",
                    [McpScopeNames.Read],
                    [Guid.Parse(departmentId)]),
                CancellationToken.None));

        var serviceToken = await oauth.IssueServiceTokenAsync(
            admin,
            new McpServiceTokenRequest(
                serviceUser.UserId,
                "Project event digital employee",
                [McpScopeNames.Read],
                [Guid.Parse(departmentId)],
                LifetimeHours: 2,
                AgentProfileId: Guid.Parse(agentProfile.Id)),
            CancellationToken.None);
        serviceToken.AccessToken.ShouldNotBeNullOrWhiteSpace();
        serviceToken.ClientId.ShouldNotBeNullOrWhiteSpace();

        var validation = await oauth.ValidateAccessTokenAsync(serviceToken.AccessToken, CancellationToken.None);
        validation.Identity.ShouldNotBeNull();
        validation.Identity!.User.IsServiceIdentity.ShouldBeTrue();
        validation.Identity.Scopes.ShouldBe([McpScopeNames.Read]);
        validation.Identity.DepartmentIds.ShouldBe([Guid.Parse(departmentId)]);
        validation.Identity.AgentProfileId.ShouldBe(Guid.Parse(agentProfile.Id));
        validation.Identity.AllowedCapabilities.ShouldNotBeNull()
            .ShouldContain("event.inbox.review");
        validation.Identity.User.DataScopes.ShouldContain(scope =>
            scope.ScopeType == "organization"
            && scope.ScopeReference == admin.OrganizationId.ToString("D"));
    }

    private async Task<TokenFixture> IssueTokenAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var oauth = scope.ServiceProvider.GetRequiredService<IMcpOAuthService>();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var user = await users.GetPrincipalByUserNameAsync("demo.admin", CancellationToken.None)
            ?? throw new InvalidOperationException("demo.admin is missing.");
        var departments = await scope.ServiceProvider
            .GetRequiredService<IEmployeeWorkspaceQueryService>()
            .ListDepartmentsAsync(user, CancellationToken.None);
        var department = departments.First(item => item.Code == "RND");
        var registration = await oauth.RegisterClientAsync(
            new McpClientRegistrationRequest(
                "MCP integration client",
                ["http://127.0.0.1:45678/callback"],
                McpScopeNames.Read,
                ["authorization_code", "refresh_token"],
                ["code"],
                "none"),
            CancellationToken.None);
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(48));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var request = await oauth.CreateAuthorizationRequestAsync(
            new McpAuthorizationRequestCommand(
                registration.ClientId,
                "http://127.0.0.1:45678/callback",
                "code",
                challenge,
                "S256",
                "state-1",
                McpScopeNames.Read),
            user,
            CancellationToken.None);
        var decision = await oauth.ApproveAuthorizationAsync(
            request.Id,
            user,
            [Guid.Parse(department.Id)],
            [McpScopeNames.Read],
            CancellationToken.None);
        var tokens = await oauth.ExchangeAuthorizationCodeAsync(
            registration.ClientId,
            clientSecret: null,
            decision.Code,
            "http://127.0.0.1:45678/callback",
            verifier,
            CancellationToken.None)
            ?? throw new InvalidOperationException("OAuth code exchange failed.");
        var identity = await oauth.ValidateAccessTokenAsync(tokens.AccessToken, CancellationToken.None);
        identity.Identity.ShouldNotBeNull();
        identity.Identity!.User.IsPlatformAdmin.ShouldBeFalse();
        identity.Identity.DepartmentIds.ShouldBe([Guid.Parse(department.Id)]);
        return new TokenFixture(
            tokens.UserId,
            registration.ClientId,
            tokens.AccessToken,
            tokens.RefreshToken);
    }

    private static async Task<HttpResponseMessage> SendMcpAsync(HttpClient client, string json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.Headers.Add("MCP-Protocol-Version", "2025-06-18");
        return await client.SendAsync(request);
    }

    private static string Base64Url(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed record TokenFixture(
        Guid UserId,
        string ClientId,
        string AccessToken,
        string RefreshToken);
}
