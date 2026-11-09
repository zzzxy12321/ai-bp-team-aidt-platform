using AIDT.Application.Contracts;

namespace AIDT.Application.WorkItems;

public static class WorkItemSourceTypes
{
    public const string WorkflowTask = "WorkflowTask";
    public const string ProjectTask = "ProjectTask";
    public const string EventInbox = "EventInbox";
    public const string MinutesActionItem = "MinutesActionItem";
    public const string McpHumanConfirmation = "McpHumanConfirmation";
    public const string AgentRun = "AgentRun";
}

public static class WorkItemPresentation
{
    public static string SourceLabel(string sourceType) =>
        sourceType switch
        {
            WorkItemSourceTypes.WorkflowTask => "流程待办",
            WorkItemSourceTypes.ProjectTask => "项目任务",
            WorkItemSourceTypes.EventInbox => "事项确认",
            WorkItemSourceTypes.MinutesActionItem => "会议行动项",
            WorkItemSourceTypes.McpHumanConfirmation => "智能体确认",
            WorkItemSourceTypes.AgentRun => "数字员工确认",
            _ => "工作事项"
        };

    public static string StatusLabel(string sourceType, string status) =>
        (sourceType, status) switch
        {
            (WorkItemSourceTypes.WorkflowTask, "Pending") => "待处理",
            (WorkItemSourceTypes.WorkflowTask, "Approved") => "已通过",
            (WorkItemSourceTypes.WorkflowTask, "Returned") => "需补充",
            (WorkItemSourceTypes.WorkflowTask, "Cancelled") => "已终止",
            (WorkItemSourceTypes.ProjectTask, "Pending") => "待处理",
            (WorkItemSourceTypes.ProjectTask, "InProgress") => "进行中",
            (WorkItemSourceTypes.ProjectTask, "Completed") => "已完成",
            (WorkItemSourceTypes.ProjectTask, "Cancelled") => "已取消",
            (WorkItemSourceTypes.EventInbox, "Received") => "待整理",
            (WorkItemSourceTypes.EventInbox, "AwaitingConfirmation") => "待我确认",
            (WorkItemSourceTypes.EventInbox, "Confirmed") => "已确认",
            (WorkItemSourceTypes.EventInbox, "Rejected") => "已拒绝",
            (WorkItemSourceTypes.MinutesActionItem, "Draft") => "待我确认",
            (WorkItemSourceTypes.MinutesActionItem, "Confirmed") => "已确认",
            (WorkItemSourceTypes.McpHumanConfirmation, "Pending") => "待真人确认",
            (WorkItemSourceTypes.McpHumanConfirmation, "Confirmed") => "已确认",
            (WorkItemSourceTypes.McpHumanConfirmation, "Rejected") => "已拒绝",
            (WorkItemSourceTypes.McpHumanConfirmation, "Consumed") => "已使用",
            (WorkItemSourceTypes.AgentRun, "AwaitingConfirmation") => "待人工确认",
            (WorkItemSourceTypes.AgentRun, "Running") => "运行中",
            (WorkItemSourceTypes.AgentRun, "Completed") => "已完成",
            (WorkItemSourceTypes.AgentRun, "Stopped") => "已停止",
            (WorkItemSourceTypes.AgentRun, "Failed") => "已失败",
            _ => "待处理"
        };

    public static (string Level, string Label) Risk(
        bool permanentHumanBoundary,
        DateTimeOffset? dueAt,
        bool requiresConfirmation,
        string? sourceRisk,
        DateTimeOffset now,
        TimeZoneInfo timeZone)
    {
        if (permanentHumanBoundary)
        {
            return ("High", "永久人工边界");
        }

        if (dueAt.HasValue && dueAt.Value < now)
        {
            return ("High", "已逾期");
        }

        if (IsLocalDate(dueAt, now, timeZone))
        {
            return ("Medium", "今天到期");
        }

        if (requiresConfirmation)
        {
            return ("Medium", "等待确认");
        }

        return sourceRisk?.Trim().ToLowerInvariant() switch
        {
            "high" or "p0" => ("High", "高风险"),
            "medium" or "p1" => ("Medium", "中风险"),
            "low" or "p2" => ("Low", "低风险"),
            _ => ("Low", "常规")
        };
    }

    public static string PriorityGroup(
        bool permanentHumanBoundary,
        DateTimeOffset? dueAt,
        bool requiresConfirmation,
        DateTimeOffset now,
        TimeZoneInfo timeZone)
    {
        if (permanentHumanBoundary)
        {
            return "PermanentHumanBoundary";
        }

        if (dueAt.HasValue && dueAt.Value < now)
        {
            return "Overdue";
        }

        if (IsLocalDate(dueAt, now, timeZone))
        {
            return "DueToday";
        }

        if (requiresConfirmation)
        {
            return "AwaitingConfirmation";
        }

        if (dueAt.HasValue && IsThisWeek(dueAt.Value, now, timeZone))
        {
            return "ThisWeek";
        }

        return "Other";
    }

    public static int PriorityRank(string priorityGroup) =>
        priorityGroup switch
        {
            "PermanentHumanBoundary" => 0,
            "Overdue" => 1,
            "DueToday" => 2,
            "AwaitingConfirmation" => 3,
            "ThisWeek" => 4,
            _ => 5
        };

    public static IReadOnlyList<WorkItemViewDto> Sort(
        IEnumerable<WorkItemViewDto> items,
        DateTimeOffset now,
        TimeZoneInfo timeZone) =>
        items
            .OrderBy(item => PriorityRank(item.PriorityGroup))
            .ThenBy(item => item.DueAt ?? DateTimeOffset.MaxValue)
            .ThenByDescending(item => item.OccurredAt)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToList();

    public static IReadOnlyList<T> Deduplicate<T>(
        IEnumerable<T> items,
        Func<T, string> identity)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        return items.Where(item => seen.Add(identity(item))).ToList();
    }

    public static bool IsLocalDate(
        DateTimeOffset? value,
        DateTimeOffset now,
        TimeZoneInfo timeZone) =>
        value.HasValue
        && TimeZoneInfo.ConvertTime(value.Value, timeZone).Date
            == TimeZoneInfo.ConvertTime(now, timeZone).Date;

    public static bool IsThisWeek(
        DateTimeOffset value,
        DateTimeOffset now,
        TimeZoneInfo timeZone)
    {
        var localNow = TimeZoneInfo.ConvertTime(now, timeZone);
        var daysUntilNextMonday = ((int)DayOfWeek.Monday - (int)localNow.DayOfWeek + 7) % 7;
        if (daysUntilNextMonday == 0)
        {
            daysUntilNextMonday = 7;
        }

        var weekEnd = new DateTimeOffset(
            localNow.Date.AddDays(daysUntilNextMonday),
            localNow.Offset);
        return value < weekEnd;
    }
}
