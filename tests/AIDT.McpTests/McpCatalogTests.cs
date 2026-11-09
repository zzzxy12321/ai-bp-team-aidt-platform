using AIDT.Application.Contracts;
using AIDT.Application.Mcp;
using Shouldly;
using System.Text.Json;

namespace AIDT.McpTests;

public sealed class McpCatalogTests
{
    [Fact]
    public void Catalog_ContainsControlledBusinessToolsAndNoUnsafePrimitive()
    {
        var catalog = new McpToolCatalog();
        var tools = catalog.List();

        tools.ShouldContain(item => item.Name == "aidt_list_projects");
        tools.ShouldContain(item => item.Name == "aidt_create_event");
        tools.ShouldContain(item => item.Name == "aidt_confirm_event" && item.HighRisk);
        tools.ShouldContain(item => item.Name == "aidt_decide_workflow_task" && item.HighRisk);
        tools.ShouldContain(item => item.Name == "aidt_upload_file");
        tools.ShouldNotContain(item =>
            item.Name.Contains("sql", StringComparison.OrdinalIgnoreCase)
            || item.Name.Contains("shell", StringComparison.OrdinalIgnoreCase)
            || item.Name.Contains("http", StringComparison.OrdinalIgnoreCase)
            || item.Name.Contains("filesystem", StringComparison.OrdinalIgnoreCase));
        tools.Select(item => item.Name).Distinct(StringComparer.Ordinal).Count().ShouldBe(tools.Count);
        tools.ShouldAllBe(item => McpScopeNames.All.Contains(item.Scope));
    }

    [Fact]
    public void ScopeParser_DropsUnknownScopesAndKeepsStableOrder()
    {
        McpScopeNames.Parse("aidt:write aidt:read unknown aidt:files")
            .ShouldBe([McpScopeNames.Files, McpScopeNames.Read, McpScopeNames.Write]);
    }

    [Fact]
    public void ClientRegistrationRequest_BindsStandardOAuthSnakeCaseFields()
    {
        var request = JsonSerializer.Deserialize<McpClientRegistrationRequest>(
            """
            {
              "client_name": "Codex",
              "redirect_uris": ["http://127.0.0.1:45678/callback"],
              "scope": "aidt:read aidt:approve",
              "grant_types": ["authorization_code", "refresh_token"],
              "response_types": ["code"],
              "token_endpoint_auth_method": "none"
            }
            """,
            JsonSerializerOptions.Web);

        request.ShouldNotBeNull();
        request!.ClientName.ShouldBe("Codex");
        request.RedirectUris.ShouldBe(["http://127.0.0.1:45678/callback"]);
        request.Scope.ShouldBe("aidt:read aidt:approve");
        request.GrantTypes.ShouldBe(["authorization_code", "refresh_token"]);
        request.ResponseTypes.ShouldBe(["code"]);
    }
}
