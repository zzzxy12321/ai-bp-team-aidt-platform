using AIDT.Domain.Agents;
using AIDT.Domain.Common;
using Shouldly;

namespace AIDT.UnitTests.Domain;

public sealed class AgentTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public void HighRiskAction_WithoutConfirmation_WaitsForConfirmation()
    {
        var profile = AgentProfile.Create(
            "quality-assistant",
            AgentAutonomyLevel.A3,
            10_000m,
            AgentRiskLevel.High,
            ["object.read", "quality.draft"]);

        var run = AgentRun.Request(
            profile,
            Guid.NewGuid(),
            AgentActionType.QualityRelease,
            0,
            AgentRiskLevel.High,
            null,
            Now);

        run.Status.ShouldBe(AgentRunStatus.AwaitingConfirmation);
        Should.Throw<ConflictException>(() => run.MarkRunning())
            .ErrorCode.ShouldBe(ErrorCodes.AgentConfirmationRequired);
    }

    [Fact]
    public void HighRiskAction_WithHumanConfirmation_CanRun()
    {
        var profile = AgentProfile.Create(
            "quality-assistant",
            AgentAutonomyLevel.A3,
            10_000m,
            AgentRiskLevel.High,
            ["object.read", "quality.draft"]);
        var run = AgentRun.Request(
            profile,
            Guid.NewGuid(),
            AgentActionType.QualityRelease,
            0,
            AgentRiskLevel.High,
            Guid.NewGuid(),
            Now);

        run.Status.ShouldBe(AgentRunStatus.AwaitingConfirmation);
        run.Confirm(Guid.NewGuid(), isHumanIdentity: true);
        run.Status.ShouldBe(AgentRunStatus.Running);
    }

    [Fact]
    public void AmountAboveLimit_IsRejected()
    {
        var profile = AgentProfile.Create(
            "purchase-assistant",
            AgentAutonomyLevel.A3,
            5_000m,
            AgentRiskLevel.Medium,
            ["purchase.read"]);

        Should.Throw<ConflictException>(
                () => AgentRun.Request(
                    profile,
                    Guid.NewGuid(),
                    AgentActionType.Read,
                    5_001m,
                    AgentRiskLevel.Low,
                    null,
                    Now))
            .ErrorCode.ShouldBe(ErrorCodes.AgentLimitExceeded);
    }

    [Fact]
    public void ServiceIdentity_CannotConfirmAgentRun()
    {
        var profile = AgentProfile.Create(
            "quality-assistant",
            AgentAutonomyLevel.A1,
            0,
            AgentRiskLevel.Low,
            ["draft"]);
        var run = AgentRun.Request(
            profile,
            Guid.NewGuid(),
            AgentActionType.Draft,
            0,
            AgentRiskLevel.Low,
            Guid.NewGuid(),
            Now);

        Should.Throw<PermissionException>(
                () => run.Confirm(Guid.NewGuid(), isHumanIdentity: false))
            .ErrorCode.ShouldBe(ErrorCodes.PermissionDenied);
    }
}
