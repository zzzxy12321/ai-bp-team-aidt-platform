namespace AIDT.Integrations;

public enum ConnectorConnectionState
{
    Disconnected,
    Mock,
    Sandbox,
    Connected,
    Faulted
}

public sealed record ConnectorAdapterDescriptor(
    string Code,
    string Name,
    string AuthorityOwner,
    ConnectorConnectionState State,
    bool AllowsWrites);

public interface IConnectorAdapter
{
    ConnectorAdapterDescriptor Descriptor { get; }

    Task<ConnectorCommandReceipt> ExecuteAsync(
        ConnectorCommandEnvelope command,
        CancellationToken cancellationToken);
}

public sealed record ConnectorCommandEnvelope(
    Guid CommandId,
    Guid ConnectorId,
    string CommandType,
    string IdempotencyKey,
    string PayloadJson,
    string? ApprovalEvidenceJson);

public sealed record ConnectorCommandReceipt(
    Guid CommandId,
    string Status,
    string? ExternalId,
    int? StatusCode,
    string ResponseDigest,
    DateTimeOffset ReceivedAt);

public sealed class DisconnectedConnectorAdapter : IConnectorAdapter
{
    public ConnectorAdapterDescriptor Descriptor { get; } =
        new("EXTERNAL-DISCONNECTED", "外部系统未连接", "AIDT", ConnectorConnectionState.Disconnected, false);

    public Task<ConnectorCommandReceipt> ExecuteAsync(
        ConnectorCommandEnvelope command,
        CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Demo 禁止调用真实外部系统。");
}

public sealed class MockConnectorAdapter : IConnectorAdapter
{
    public MockConnectorAdapter(string code, string name, string authorityOwner)
    {
        if (string.IsNullOrWhiteSpace(code)
            || string.IsNullOrWhiteSpace(name)
            || string.IsNullOrWhiteSpace(authorityOwner))
        {
            throw new ArgumentException("Mock connector code, name and authority owner are required.");
        }

        Descriptor = new ConnectorAdapterDescriptor(
            code,
            name,
            authorityOwner,
            ConnectorConnectionState.Mock,
            false);
    }

    public ConnectorAdapterDescriptor Descriptor { get; }

    public Task<ConnectorCommandReceipt> ExecuteAsync(
        ConnectorCommandEnvelope command,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var digestSource = $"{command.CommandId:D}|{command.CommandType}|{command.IdempotencyKey}|{command.PayloadJson}";
        var digest = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(digestSource)));
        return Task.FromResult(new ConnectorCommandReceipt(
            command.CommandId,
            "Mocked",
            $"MOCK-{command.CommandId:N}",
            202,
            digest,
            DateTimeOffset.UtcNow));
    }
}
