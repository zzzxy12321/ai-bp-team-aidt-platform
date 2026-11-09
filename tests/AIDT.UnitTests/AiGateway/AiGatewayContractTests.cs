using System.Net;
using System.Net.Http.Json;
using AIDT.AiGateway;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace AIDT.UnitTests.AiGateway;

public sealed class AiGatewayContractTests : IClassFixture<WebApplicationFactory<AIDT.AiGateway.Program>>
{
    private readonly WebApplicationFactory<AIDT.AiGateway.Program> _factory;

    public AiGatewayContractTests(WebApplicationFactory<AIDT.AiGateway.Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Liveness_ReportsRuleSimulator()
    {
        using var client = _factory.CreateClient();
        var payload = await client.GetStringAsync("/health/live");

        payload.ShouldContain("AIDT.AiGateway");
        payload.ShouldContain("RuleSimulator");
    }

    [Fact]
    public async Task AllowedModel_ReturnsExplicitMockResponse()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/v1/chat/completions",
            new ChatCompletionRequest(
                "aidt-rule-sim",
                [new ChatMessage("user", "请总结当前待办")],
                0));
        var payload = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("X-AIDT-External-Model").Single().ShouldBe("false");
        payload.ShouldNotBeNull();
        payload!.Model.ShouldBe("aidt-rule-sim");
        payload.Audit.ExternalModelCalled.ShouldBeFalse();
        payload.Choices.Single().Message.Content.ShouldContain("未调用外部模型");
    }

    [Fact]
    public async Task ModelOutsideWhitelist_IsRejected()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/v1/chat/completions",
            new ChatCompletionRequest(
                "unapproved-model",
                [new ChatMessage("user", "test")],
                0));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("MODEL_NOT_ALLOWED");
    }

    [Fact]
    public async Task RuleSimulatorDelay_ReturnsGatewayTimeout()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AiGateway:TimeoutSeconds"] = "1"
                });
            });
        });
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/v1/chat/completions",
            new ChatCompletionRequest(
                "aidt-rule-sim",
                [new ChatMessage("user", "延迟超时测试")],
                0,
                3_000));

        response.StatusCode.ShouldBe(HttpStatusCode.GatewayTimeout);
        (await response.Content.ReadAsStringAsync()).ShouldContain("GATEWAY_TIMEOUT");
    }
}
