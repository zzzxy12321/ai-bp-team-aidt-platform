using AIDT.Domain.BusinessObjects;
using AIDT.Domain.Common;
using AIDT.Domain.Projects;
using Shouldly;

namespace AIDT.UnitTests.Domain;

public sealed class ProjectRulesTests
{
    [Theory]
    [InlineData(BusinessObjectStatus.Draft, BusinessObjectStatus.Active)]
    [InlineData(BusinessObjectStatus.Active, BusinessObjectStatus.Blocked)]
    [InlineData(BusinessObjectStatus.Blocked, BusinessObjectStatus.Active)]
    [InlineData(BusinessObjectStatus.Active, BusinessObjectStatus.AwaitingAcceptance)]
    [InlineData(BusinessObjectStatus.AwaitingAcceptance, BusinessObjectStatus.Completed)]
    public void EnsureStatusTransition_AllowsDocumentedTransitions(
        BusinessObjectStatus current,
        BusinessObjectStatus next)
    {
        Should.NotThrow(() => ProjectRules.EnsureStatusTransition(current, next));
    }

    [Theory]
    [InlineData(BusinessObjectStatus.Completed, BusinessObjectStatus.Active)]
    [InlineData(BusinessObjectStatus.Terminated, BusinessObjectStatus.Active)]
    [InlineData(BusinessObjectStatus.Draft, BusinessObjectStatus.Completed)]
    public void EnsureStatusTransition_RejectsInvalidTransitions(
        BusinessObjectStatus current,
        BusinessObjectStatus next)
    {
        var exception = Should.Throw<ConflictException>(
            () => ProjectRules.EnsureStatusTransition(current, next));
        exception.ErrorCode.ShouldBe(ErrorCodes.Conflict);
    }

    [Fact]
    public void AssessRisk_HighRiskFinanceAndReleaseRequiresHumanBoundary()
    {
        var assessment = ProjectRules.AssessRisk("请尽快完成财务付款并办理质量放行。");

        assessment.RiskLevel.ShouldBe("High");
        assessment.RequiresHumanBoundary.ShouldBeTrue();
        assessment.Reason.ShouldContain("付款");
        assessment.Reason.ShouldContain("质量放行");
    }

    [Fact]
    public void AssessRisk_NormalProjectActionCanRemainLow()
    {
        var assessment = ProjectRules.AssessRisk("整理样品开发计划和会议行动项。");

        assessment.RiskLevel.ShouldBe("Low");
        assessment.RequiresHumanBoundary.ShouldBeFalse();
    }

    [Fact]
    public void IsOverdue_IgnoresCompletedAndCancelledMilestones()
    {
        var now = new DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.FromHours(8));
        var planned = now.AddDays(-2);

        ProjectRules.IsOverdue(ProjectMilestoneStatus.Planned, planned, now).ShouldBeTrue();
        ProjectRules.IsOverdue(ProjectMilestoneStatus.Completed, planned, now).ShouldBeFalse();
        ProjectRules.IsOverdue(ProjectMilestoneStatus.Cancelled, planned, now).ShouldBeFalse();
    }

    [Fact]
    public void TryExtractDeadline_ParsesExplicitDate()
    {
        var shanghai = TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");

        var deadline = ProjectRules.TryExtractDeadline(
            "截止日期：2025-08-15 前完成样品确认",
            shanghai,
            18);

        deadline.ShouldNotBeNull();
        deadline.Value.Year.ShouldBe(2025);
        deadline.Value.Month.ShouldBe(8);
        deadline.Value.Day.ShouldBe(15);
        deadline.Value.Hour.ShouldBe(18);
        deadline.Value.Minute.ShouldBe(0);
        deadline.Value.Offset.ShouldBe(TimeSpan.FromHours(8));
        deadline.Value.ToUniversalTime().ShouldBe(
            new DateTimeOffset(2025, 8, 15, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Percent_UsesWholeNumberAndHandlesEmptySet()
    {
        ProjectRules.Percent(0, 0).ShouldBe(0);
        ProjectRules.Percent(1, 3).ShouldBe(33);
        ProjectRules.Percent(2, 3).ShouldBe(67);
    }
}
