using AIDT.Application.Contracts;
using AIDT.Application.WorkItems;
using Shouldly;

namespace AIDT.UnitTests.Application;

public sealed class WorkItemPresentationTests
{
    private static readonly TimeZoneInfo Shanghai =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");

    [Fact]
    public void PriorityGroup_FollowsRequiredBusinessOrder()
    {
        var now = new DateTimeOffset(2026, 11, 4, 10, 0, 0, TimeSpan.FromHours(8));
        var dueToday = new DateTimeOffset(2026, 11, 4, 18, 0, 0, TimeSpan.FromHours(8));
        var thisWeek = new DateTimeOffset(2026, 11, 6, 18, 0, 0, TimeSpan.FromHours(8));
        var later = new DateTimeOffset(2026, 11, 10, 18, 0, 0, TimeSpan.FromHours(8));

        WorkItemPresentation.PriorityGroup(true, later, true, now, Shanghai).ShouldBe("PermanentHumanBoundary");
        WorkItemPresentation.PriorityGroup(false, now.AddHours(-1), false, now, Shanghai).ShouldBe("Overdue");
        WorkItemPresentation.PriorityGroup(false, dueToday, false, now, Shanghai).ShouldBe("DueToday");
        WorkItemPresentation.PriorityGroup(false, null, true, now, Shanghai).ShouldBe("AwaitingConfirmation");
        WorkItemPresentation.PriorityGroup(false, thisWeek, false, now, Shanghai).ShouldBe("ThisWeek");
        WorkItemPresentation.PriorityGroup(false, later, false, now, Shanghai).ShouldBe("Other");
    }

    [Fact]
    public void Sort_OrdersByRiskThenDueThenOccurrence()
    {
        var now = new DateTimeOffset(2026, 11, 4, 10, 0, 0, TimeSpan.FromHours(8));
        var items = new[]
        {
            Item("other", "Other", now.AddDays(7)),
            Item("today", "DueToday", now.AddHours(3)),
            Item("confirmed", "AwaitingConfirmation", null),
            Item("overdue", "Overdue", now.AddHours(-2)),
            Item("human", "PermanentHumanBoundary", now.AddDays(2)),
        };

        var sorted = WorkItemPresentation.Sort(items, now, Shanghai);

        sorted.Select(item => item.Id).ShouldBe(
            ["human", "overdue", "today", "confirmed", "other"]);
    }

    [Fact]
    public void Deduplicate_RemovesSameDisplayIdentityAndPreservesOrder()
    {
        var items = new[]
        {
            Item("same", "Other", null),
            Item("other", "Other", null),
            Item("same", "Other", null),
        };

        var result = WorkItemPresentation.Deduplicate(items, item => item.Id);

        result.Select(item => item.Id).ShouldBe(["same", "other"]);
        result.ShouldNotContain(item => item.BusinessTitle.Contains("UUID", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("WorkflowTask", "Pending", "待处理")]
    [InlineData("ProjectTask", "InProgress", "进行中")]
    [InlineData("EventInbox", "AwaitingConfirmation", "待我确认")]
    [InlineData("MinutesActionItem", "Draft", "待我确认")]
    [InlineData("McpHumanConfirmation", "Pending", "待真人确认")]
    [InlineData("AgentRun", "AwaitingConfirmation", "待人工确认")]
    public void StatusLabel_UsesBusinessChinese(
        string sourceType,
        string status,
        string expected)
    {
        WorkItemPresentation.StatusLabel(sourceType, status).ShouldBe(expected);
    }

    private static WorkItemViewDto Item(
        string id,
        string priorityGroup,
        DateTimeOffset? dueAt) =>
        new(
            id,
            WorkItemSourceTypes.ProjectTask,
            "项目任务",
            $"事项 {id}",
            "项目 A",
            Guid.NewGuid().ToString("D"),
            Guid.NewGuid().ToString("D"),
            "研发部",
            "待处理",
            Guid.NewGuid().ToString("D"),
            "张三",
            dueAt,
            "Medium",
            "中风险",
            priorityGroup,
            "Pending",
            "待处理",
            "完成项目任务",
            2,
            WorkItemSourceTypes.ProjectTask,
            Guid.NewGuid().ToString("D"),
            "/projects/example",
            "demo",
            true,
            priorityGroup == "PermanentHumanBoundary",
            priorityGroup == "AwaitingConfirmation",
            DateTimeOffset.UtcNow);
}
