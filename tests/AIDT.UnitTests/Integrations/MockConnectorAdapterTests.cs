using AIDT.Integrations;
using Shouldly;

namespace AIDT.UnitTests.Integrations;

public sealed class MockConnectorAdapterTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsExplicitMockReceiptWithoutNetworkSideEffect()
    {
        var adapter = new MockConnectorAdapter("U9-MOCK", "U9 Mock", "U9");
        var command = new ConnectorCommandEnvelope(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PaymentPreview",
            "mock-idempotency-001",
            """{"amount":100}""",
            null);

        var receipt = await adapter.ExecuteAsync(command, CancellationToken.None);

        adapter.Descriptor.State.ShouldBe(ConnectorConnectionState.Mock);
        adapter.Descriptor.AllowsWrites.ShouldBeFalse();
        receipt.Status.ShouldBe("Mocked");
        receipt.ExternalId.ShouldStartWith("MOCK-");
        receipt.StatusCode.ShouldBe(202);
        receipt.ResponseDigest.Length.ShouldBe(64);
    }
}
