using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace AIDT.McpTests;

public sealed class McpHttpContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public McpHttpContractTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthAndOAuthMetadata_AreAnonymous()
    {
        using var client = _factory.CreateClient();
        var health = await client.GetAsync("/health/live");
        var metadata = await client.GetAsync("/.well-known/oauth-authorization-server");

        health.StatusCode.ShouldBe(HttpStatusCode.OK);
        metadata.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await metadata.Content.ReadAsStringAsync();
        json.ShouldContain("code_challenge_methods_supported");
        json.ShouldContain("aidt:read");
        json.ShouldContain("aidt:approve");
    }

    [Fact]
    public async Task McpEndpoint_WithoutToken_ChallengesWithProtectedResourceMetadata()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(
                """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}""",
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ToString().ShouldContain("resource_metadata");
    }
}
