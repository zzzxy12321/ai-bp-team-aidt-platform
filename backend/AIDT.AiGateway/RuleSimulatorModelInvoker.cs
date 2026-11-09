namespace AIDT.AiGateway;

public sealed class RuleSimulatorModelInvoker : IGatewayModelInvoker
{
    public async Task<string> InvokeAsync(
        string model,
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
        return $"AIDT 规则化模拟结果：模型 {model} 已接收 {messages.Count} 条消息。"
            + " 本次未调用外部模型，不产生真实 AI 结论。";
    }
}
