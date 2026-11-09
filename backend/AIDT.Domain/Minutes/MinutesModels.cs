using AIDT.Domain.Common;

namespace AIDT.Domain.Minutes;

public enum MinutesSourceType
{
    ManualText,
    ManualUpload
}

public enum MinutesActionStatus
{
    Draft,
    Confirmed
}

public sealed record MinutesSourceRecord(
    Guid Id,
    Guid OrganizationId,
    Guid DepartmentId,
    Guid? AttachmentId,
    string Title,
    DateTimeOffset MeetingAt,
    string SourceType,
    string Transcript,
    string EnvironmentCode,
    bool IsTestData,
    Guid CreatedBy,
    DateTimeOffset CreatedAt,
    string AnalysisVersion,
    Guid? AnalysisRunId);

public sealed record MinutesActionItemRecord(
    Guid Id,
    Guid MinutesSourceId,
    string Speaker,
    string SourceTimestamp,
    string SourceExcerpt,
    string OriginalContent,
    string Content,
    Guid? SuggestedAssigneeId,
    Guid? AssigneeId,
    DateTimeOffset? SuggestedDueAt,
    DateTimeOffset? DueAt,
    string Priority,
    Guid? SuggestedObjectTypeId,
    Guid? ObjectTypeId,
    Guid? SuggestedWorkflowVersionId,
    Guid? WorkflowVersionId,
    string MissingInformation,
    decimal Confidence,
    string AnalysisVersion,
    string Status,
    Guid? ConfirmedBy,
    DateTimeOffset? ConfirmedAt,
    Guid? MinutesTaskId);

public sealed record MinutesTaskRecord(
    Guid Id,
    Guid MinutesSourceId,
    Guid ActionItemId,
    Guid? AttachmentId,
    Guid OrganizationId,
    Guid DepartmentId,
    Guid AssigneeId,
    string Title,
    string Content,
    DateTimeOffset? DueAt,
    string Priority,
    Guid? BusinessObjectId,
    Guid? WorkflowInstanceId,
    string EnvironmentCode,
    bool IsTestData,
    string Status,
    Guid CreatedBy,
    DateTimeOffset CreatedAt);

public sealed record WorkflowNodePolicyRecord(
    Guid Id,
    Guid WorkflowVersionId,
    string NodeCode,
    int VersionNumber,
    Guid? ApproverUserId,
    Guid? BackupApproverUserId,
    int? SlaMinutes,
    int? ReminderMinutes,
    int? EscalationMinutes,
    string TimeoutStrategy,
    bool AllowAgentAssist,
    bool AllowAgentProxy,
    Guid? AgentProfileId,
    string AgentMaxRisk,
    decimal AgentMaxAmount,
    string AgentMaxAutonomy,
    string AgentProxyMode,
    int? AutoProxyAfterMinutes,
    bool PermanentHumanBoundary,
    int RequiredAttachmentCount,
    string? PrecheckItems,
    string Status,
    Guid PublishedBy,
    DateTimeOffset PublishedAt);
