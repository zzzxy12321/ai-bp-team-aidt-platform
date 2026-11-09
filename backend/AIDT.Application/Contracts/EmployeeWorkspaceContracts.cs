namespace AIDT.Application.Contracts;

public sealed record DepartmentDto(
    string Id,
    string Code,
    string Name,
    string? ManagerUserId,
    bool IsActive,
    bool IsPrimary);

public sealed record WorkItemDto(
    string Id,
    string Kind,
    string Title,
    string ObjectCode,
    string WorkflowName,
    string CurrentStep,
    string AssigneeDisplayName,
    DateTimeOffset? DueAt,
    string Status,
    string NextAction,
    string EnvironmentCode,
    bool IsTestData,
    string? DepartmentId,
    string? WorkflowInstanceId,
    string? TaskId);

public sealed record WorkItemViewDto(
    string Id,
    string SourceType,
    string SourceLabel,
    string BusinessTitle,
    string ContextTitle,
    string? ProjectId,
    string? DepartmentId,
    string DepartmentName,
    string CurrentStage,
    string? AssigneeId,
    string AssigneeDisplayName,
    DateTimeOffset? DueAt,
    string RiskLevel,
    string RiskLabel,
    string PriorityGroup,
    string Status,
    string StatusLabel,
    string NextAction,
    int SourceCount,
    string DetailType,
    string DetailTargetId,
    string DetailPath,
    string EnvironmentCode,
    bool IsTestData,
    bool IsPermanentHumanBoundary,
    bool RequiresConfirmation,
    DateTimeOffset OccurredAt);

public sealed record MyWorkSummaryDto(
    int PendingCount,
    int DueTodayCount,
    int AwaitingConfirmationCount,
    int PermanentHumanBoundaryCount);

public sealed record MyWorkDto(
    string DisplayName,
    WorkItemViewDto? TodayFocus,
    IReadOnlyList<WorkItemViewDto> PendingItems,
    IReadOnlyList<WorkItemViewDto> InitiatedByMe,
    IReadOnlyList<WorkItemViewDto> Participating,
    IReadOnlyList<WorkItemViewDto> NeedsAttention,
    IReadOnlyList<FileAttachmentDto> RecentSourceEvidence,
    MyWorkSummaryDto Summary);

public sealed record DepartmentWorkspaceDto(
    DepartmentDto Department,
    int PendingTaskCount,
    int OverdueTaskCount,
    int RunningWorkflowCount,
    int BusinessObjectCount,
    int RecentFileCount,
    IReadOnlyList<WorkItemDto> PendingTasks,
    IReadOnlyList<BusinessObjectDto> RecentObjects,
    IReadOnlyList<WorkItemDto> RecentWorkflows,
    IReadOnlyList<FileAttachmentDto> RecentFiles);

public sealed record CreateDepartmentRequest(string Code, string Name, Guid? ManagerUserId);

public sealed record AddDepartmentMemberRequest(
    Guid UserId,
    bool IsPrimary,
    bool IsManager);
