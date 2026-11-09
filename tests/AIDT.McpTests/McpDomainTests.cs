using AIDT.Domain.Common;
using AIDT.Domain.Mcp;
using Shouldly;

namespace AIDT.McpTests;

public sealed class McpDomainTests
{
    [Fact]
    public void HumanConfirmation_IsBoundToCurrentUserAndConsumedOnce()
    {
        var userId = Guid.NewGuid();
        var confirmation = McpHumanConfirmation.Create(
            Guid.NewGuid(),
            userId,
            "aidt_decide_workflow_task",
            new string('A', 64),
            DateTimeOffset.UtcNow.AddMinutes(10));

        Should.Throw<PermissionException>(() =>
            confirmation.Confirm(Guid.NewGuid(), DateTimeOffset.UtcNow));
        confirmation.Confirm(userId, DateTimeOffset.UtcNow);
        confirmation.Consume(DateTimeOffset.UtcNow);

        confirmation.Status.ShouldBe(McpHumanConfirmationStatus.Consumed);
        Should.Throw<ConflictException>(() => confirmation.Consume(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void AccessToken_ExpiresAndRevocationIsStable()
    {
        var now = DateTimeOffset.UtcNow;
        var token = McpAccessTokenGrant.Issue(
            Guid.NewGuid(),
            McpTokenKind.Access,
            now.AddMinutes(5));

        token.IsActive(now).ShouldBeTrue();
        token.Revoke(now);
        token.IsActive(now).ShouldBeFalse();
        Should.Throw<ConflictException>(() => token.Revoke(now));
        token.IsActive(now.AddMinutes(10)).ShouldBeFalse();
    }
}
