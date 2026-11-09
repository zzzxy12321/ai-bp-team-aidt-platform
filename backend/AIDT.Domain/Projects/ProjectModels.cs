using System.Text.RegularExpressions;
using AIDT.Domain.BusinessObjects;
using AIDT.Domain.Common;

namespace AIDT.Domain.Projects;

public enum ProjectMemberRole
{
    Lead,
    Manager,
    Contributor,
    Viewer
}

public enum ProjectMilestoneStatus
{
    Planned,
    InProgress,
    Completed,
    AtRisk,
    Cancelled
}

public enum ProjectTaskStatus
{
    Pending,
    InProgress,
    Completed,
    Cancelled
}

public enum InboxEventStatus
{
    Received,
    AwaitingConfirmation,
    Confirmed,
    Rejected
}

public enum InboxSuggestionStatus
{
    Suggested,
    HighRisk,
    Confirmed,
    Rejected
}

public static partial class ProjectRules
{
    public static readonly IReadOnlySet<BusinessObjectStatus> ProjectStatuses =
        new HashSet<BusinessObjectStatus>
        {
            BusinessObjectStatus.Draft,
            BusinessObjectStatus.Active,
            BusinessObjectStatus.Blocked,
            BusinessObjectStatus.AwaitingAcceptance,
            BusinessObjectStatus.Completed,
            BusinessObjectStatus.Terminated
        };

    public static void EnsureProjectStatus(BusinessObjectStatus status)
    {
        if (!ProjectStatuses.Contains(status))
        {
            throw new ValidationException($"状态 {status} 不是有效的项目状态。");
        }
    }

    public static void EnsureStatusTransition(BusinessObjectStatus current, BusinessObjectStatus next)
    {
        EnsureProjectStatus(current);
        EnsureProjectStatus(next);
        if (current == next)
        {
            return;
        }

        var allowed = current switch
        {
            BusinessObjectStatus.Draft => next is BusinessObjectStatus.Active or BusinessObjectStatus.Terminated,
            BusinessObjectStatus.Active => next is BusinessObjectStatus.Blocked
                or BusinessObjectStatus.AwaitingAcceptance
                or BusinessObjectStatus.Completed
                or BusinessObjectStatus.Terminated,
            BusinessObjectStatus.Blocked => next is BusinessObjectStatus.Active or BusinessObjectStatus.Terminated,
            BusinessObjectStatus.AwaitingAcceptance => next is BusinessObjectStatus.Active
                or BusinessObjectStatus.Completed
                or BusinessObjectStatus.Terminated,
            _ => false
        };
        if (!allowed)
        {
            throw new ConflictException(
                ErrorCodes.Conflict,
                $"项目状态不能从 {DisplayStatus(current)} 变更为 {DisplayStatus(next)}。");
        }
    }

    public static string DisplayStatus(BusinessObjectStatus status) =>
        status switch
        {
            BusinessObjectStatus.Draft => "草拟",
            BusinessObjectStatus.Active => "进行中",
            BusinessObjectStatus.Blocked => "阻塞",
            BusinessObjectStatus.AwaitingAcceptance => "待验收",
            BusinessObjectStatus.Completed => "已完成",
            BusinessObjectStatus.Terminated => "已终止",
            _ => status.ToString()
        };

    public static bool IsOverdue(
        ProjectMilestoneStatus status,
        DateTimeOffset plannedDate,
        DateTimeOffset now) =>
        status is not (ProjectMilestoneStatus.Completed or ProjectMilestoneStatus.Cancelled)
        && plannedDate.UtcDateTime.Date < now.UtcDateTime.Date;

    public static int Percent(int completed, int total) =>
        total <= 0 ? 0 : (int)Math.Round(completed * 100m / total, MidpointRounding.AwayFromZero);

    public static RiskAssessment AssessRisk(string text)
    {
        var normalized = text ?? string.Empty;
        var matched = HighRiskPattern().Matches(normalized)
            .Select(match => match.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (matched.Length > 0)
        {
            return new RiskAssessment(
                "High",
                true,
                "命中永久人工边界：" + string.Join("、", matched));
        }

        if (MediumRiskPattern().IsMatch(normalized))
        {
            return new RiskAssessment("Medium", false, "存在外部承诺、期限或影响范围不明确。");
        }

        return new RiskAssessment("Low", false, string.Empty);
    }

    public static DateTimeOffset? TryExtractDeadline(
        string text,
        TimeZoneInfo businessTimeZone,
        int deadlineHour = 18)
    {
        ArgumentNullException.ThrowIfNull(businessTimeZone);
        if (deadlineHour is < 0 or > 23)
        {
            throw new ValidationException("业务截止小时必须在 0—23 之间。");
        }

        var match = DeadlinePattern().Match(text ?? string.Empty);
        if (!match.Success)
        {
            return null;
        }

        if (!DateTime.TryParse(match.Groups["date"].Value, out var parsed))
        {
            return null;
        }

        var localDateTime = DateTime.SpecifyKind(
            parsed.Date.AddHours(deadlineHour),
            DateTimeKind.Unspecified);
        if (businessTimeZone.IsInvalidTime(localDateTime))
        {
            return null;
        }

        return new DateTimeOffset(
            localDateTime,
            businessTimeZone.GetUtcOffset(localDateTime));
    }

    public static string DetectPriority(string text, string riskLevel)
    {
        if (string.Equals(riskLevel, "High", StringComparison.OrdinalIgnoreCase))
        {
            return "P0";
        }

        return UrgentPattern().IsMatch(text ?? string.Empty) ? "P0" : "P1";
    }

    public static string BuildMissingInformation(
        Guid? projectId,
        Guid? assigneeId,
        Guid? workflowVersionId,
        DateTimeOffset? deadline)
    {
        var missing = new List<string>();
        if (!projectId.HasValue)
        {
            missing.Add("建议项目未确定");
        }

        if (!assigneeId.HasValue)
        {
            missing.Add("负责人未确定");
        }

        if (!workflowVersionId.HasValue)
        {
            missing.Add("是否发起流程待人工判断");
        }

        if (!deadline.HasValue)
        {
            missing.Add("截止时间未识别");
        }

        return string.Join("；", missing);
    }

    [GeneratedRegex(
        "付款|支付|质量放行|放行|配方|DCS|PLC|安全联锁|合同承诺|重大赔付",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HighRiskPattern();

    [GeneratedRegex(
        "客户|对外|承诺|紧急|投诉|停线|超期|风险",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MediumRiskPattern();

    [GeneratedRegex(
        "截止(?:日期|时间)?[：:\\s]*(?<date>\\d{4}[-/]\\d{1,2}[-/]\\d{1,2})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DeadlinePattern();

    [GeneratedRegex("立即|今天|今天内|加急|ASAP|紧急", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UrgentPattern();
}

public sealed record RiskAssessment(string RiskLevel, bool RequiresHumanBoundary, string Reason);
