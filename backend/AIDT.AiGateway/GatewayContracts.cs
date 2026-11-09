using System.Text.Json.Serialization;

namespace AIDT.AiGateway;

public sealed record ChatMessage(string Role, string Content);

public sealed record ChatCompletionRequest(
    string Model,
    IReadOnlyList<ChatMessage> Messages,
    double? Temperature = 0,
    int? SimulateDelayMs = null);

public sealed record ChatCompletionResponse(
    string Id,
    [property: JsonPropertyName("object")] string Kind,
    long Created,
    string Model,
    IReadOnlyList<ChatChoice> Choices,
    GatewayAudit Audit);

public sealed record ChatChoice(int Index, ChatMessage Message, string FinishReason);

public sealed record GatewayAudit(
    string Mode,
    bool ExternalModelCalled,
    string InputDigest,
    string OutputDigest,
    int TimeoutMs);

public interface IGatewayModelInvoker
{
    Task<string> InvokeAsync(
        string model,
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken);
}
