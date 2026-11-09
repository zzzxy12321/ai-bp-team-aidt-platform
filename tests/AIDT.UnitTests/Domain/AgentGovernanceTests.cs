using AIDT.Domain.Agents;
using AIDT.Domain.Common;
using Shouldly;

namespace AIDT.UnitTests.Domain;

public sealed class AgentGovernanceTests
{
    private static readonly Guid OrganizationId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid DepartmentId = Guid.Parse("50000000-0000-0000-0000-000000000001");
    private static readonly Guid ServiceUserId = Guid.Parse("10000000-0000-0000-0000-000000000028");
    private static readonly Guid OwnerId = Guid.Parse("10000000-0000-0000-0000-000000000010");

    [Fact]
    public void Normalize_AlwaysRestoresPermanentHumanBoundaries()
    {
        var snapshot = AgentGovernanceRules.Normalize(CreateSnapshot() with
        {
            RequiredConfirmations = ["low-confidence"]
        });

        snapshot.RequiredConfirmations.ShouldContain("payment");
        snapshot.RequiredConfirmations.ShouldContain("quality-release");
        snapshot.RequiredConfirmations.ShouldContain("formula-change");
        snapshot.RequiredConfirmations.ShouldContain("equipment-control");
        snapshot.RequiredConfirmations.ShouldContain("safety-interlock");
        snapshot.RequiredConfirmations.ShouldContain("contract-commitment");
        snapshot.RequiredConfirmations.ShouldContain("customer-compensation");
        snapshot.RequiredConfirmations.ShouldContain("low-confidence");
    }

    [Fact]
    public void Normalize_RejectsUnregisteredOrArbitraryTools()
    {
        Should.Throw<ValidationException>(() =>
                AgentGovernanceRules.Normalize(CreateSnapshot() with
                {
                    AllowedTools = ["http.any-url"]
                }))
            .Message.ShouldContain("未注册工具能力");
    }

    [Fact]
    public void HighRiskRules_DetectElevationAndExpandedCapability()
    {
        var current = AgentGovernanceRules.Normalize(CreateSnapshot());
        var next = AgentGovernanceRules.Normalize(CreateSnapshot() with
        {
            AutonomyLevel = AgentAutonomyLevel.A3,
            RiskLimit = AgentRiskLevel.High,
            AmountLimit = 10_000m,
            AllowedTools = ["workflow.read", "workflow.proxy-low-risk"]
        });

        var reasons = AgentGovernanceRules.HighRiskReasons(current, next);

        reasons.ShouldContain("提升自主等级");
        reasons.ShouldContain("提升风险上限");
        reasons.ShouldContain("提升金额上限");
        reasons.ShouldContain("扩大工具白名单");
    }

    [Fact]
    public void DraftOrDisabledProfile_CannotStartNewRun()
    {
        var profile = AgentProfile.Restore(
            Guid.NewGuid(),
            ServiceUserId,
            "草稿数字员工",
            AgentAutonomyLevel.A2,
            0,
            AgentRiskLevel.Low,
            false,
            ["workflow.read"],
            "AGENT-DRAFT",
            DepartmentId,
            [$"department:{DepartmentId:D}"],
            ["aidt-rule-sim"],
            AgentCapabilityRegistry.PermanentHumanBoundaries,
            ["manual-stop"],
            AgentProfileLifecycleStatus.Draft,
            null);

        Should.Throw<ConflictException>(() =>
                AgentRun.Request(
                    profile,
                    OwnerId,
                    AgentActionType.Read,
                    0,
                    AgentRiskLevel.Low,
                    null,
                    DateTimeOffset.UtcNow,
                    OrganizationId,
                    DepartmentId))
            .ErrorCode.ShouldBe(ErrorCodes.AgentStopped);
    }

    [Fact]
    public void RunRequest_RejectsToolOutsideWhitelist()
    {
        var profile = AgentProfile.Create(
            ServiceUserId,
            "受限数字员工",
            AgentAutonomyLevel.A2,
            0,
            AgentRiskLevel.Low,
            ["workflow.read"]);

        Should.Throw<PermissionException>(() =>
                AgentRun.Request(
                    profile,
                    OwnerId,
                    AgentActionType.Read,
                    0,
                    AgentRiskLevel.Low,
                    null,
                    DateTimeOffset.UtcNow,
                    toolCode: "workflow.proxy-low-risk"))
            .ErrorCode.ShouldBe(ErrorCodes.PermissionDenied);
    }

    [Fact]
    public void PermanentBoundaryActions_AlwaysRequireHumanConfirmation()
    {
        var profile = AgentProfile.Create(
            ServiceUserId,
            "高风险边界数字员工",
            AgentAutonomyLevel.A4,
            10m,
            AgentRiskLevel.Low,
            ["workflow.assist"]);

        var run = AgentRun.Request(
            profile,
            OwnerId,
            AgentActionType.FormulaChange,
            0,
            AgentRiskLevel.Low,
            null,
            DateTimeOffset.UtcNow);

        run.Status.ShouldBe(AgentRunStatus.AwaitingConfirmation);
    }

    private static AgentProfileSnapshot CreateSnapshot() =>
        new(
            "agent-lowrisk",
            "低风险数字员工",
            "只生成建议",
            ServiceUserId,
            DepartmentId,
            OwnerId,
            OwnerId,
            AgentAutonomyLevel.A2,
            AgentRiskLevel.Low,
            0,
            [$"department:{DepartmentId:D}"],
            ["aidt-rule-sim"],
            ["workflow.read"],
            [],
            ["manual-stop", "risk-limit"]);
}
