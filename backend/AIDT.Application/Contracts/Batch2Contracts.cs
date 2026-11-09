namespace AIDT.Application.Contracts;

public sealed record RegisterMinutesRequest(
    Guid DepartmentId,
    string Title,
    DateTimeOffset? MeetingAt,
    string Transcript,
    Guid? AttachmentId = null,
    string? SourceType = null);

public sealed record UpdateMinutesActionItemRequest(
    Guid? AssigneeId,
    DateTimeOffset? DueAt,
    string Content,
    string Priority,
    Guid? ObjectTypeId,
    Guid? WorkflowVersionId,
    string? MissingInformation,
    Guid? ProjectId = null,
    Guid? BusinessObjectId = null,
    Guid? WorkflowInstanceId = null);

public sealed record ConfirmMinutesActionItemRequest(
    Guid? AssigneeId,
    DateTimeOffset? DueAt,
    string? Content,
    string Priority,
    Guid? ObjectTypeId,
    Guid? WorkflowVersionId,
    Guid? BusinessObjectId,
    Guid? WorkflowInstanceId,
    Guid? ProjectId = null);

public sealed record MinutesSourceDto(
    string Id,
    string DepartmentId,
    string? AttachmentId,
    string Title,
    DateTimeOffset MeetingAt,
    string SourceType,
    string Transcript,
    string EnvironmentCode,
    bool IsTestData,
    string Status,
    string AnalysisVersion,
    string? AnalysisRunId,
    DateTimeOffset CreatedAt);

public sealed record MinutesSourceQuery(
    int Page = 1,
    int PageSize = 20,
    bool IncludeTestData = false,
    string? Search = null,
    Guid? DepartmentId = null,
    string? Status = null,
    string? ActionItemStatus = null,
    string? SourceType = null,
    DateTimeOffset? MeetingFrom = null,
    DateTimeOffset? MeetingTo = null);

public sealed record MinutesSummaryDto(
    long TotalCount,
    long AnalyzedCount,
    long PendingActionItemCount,
    long CreatedThisWeekCount);

public sealed record MinutesActionItemDto(
    string Id,
    string MinutesSourceId,
    string Speaker,
    string SourceTimestamp,
    string SourceExcerpt,
    string OriginalContent,
    string Content,
    string? SuggestedAssigneeId,
    string? AssigneeId,
    DateTimeOffset? SuggestedDueAt,
    DateTimeOffset? DueAt,
    string Priority,
    string? SuggestedObjectTypeId,
    string? ObjectTypeId,
    string? SuggestedWorkflowVersionId,
    string? WorkflowVersionId,
    string MissingInformation,
    decimal Confidence,
    string AnalysisVersion,
    string Status,
    string? ConfirmedBy,
    DateTimeOffset? ConfirmedAt,
    string? MinutesTaskId,
    string? AssigneeDisplayName = null,
    string? ProjectId = null,
    string? ProjectName = null,
    string? BusinessObjectId = null,
    string? BusinessObjectCode = null,
    string? BusinessObjectTitle = null,
    string? WorkflowInstanceId = null,
    string? WorkflowInstanceStatus = null);

public sealed record MinutesTaskDto(
    string Id,
    string MinutesSourceId,
    string ActionItemId,
    string? AttachmentId,
    string DepartmentId,
    string AssigneeId,
    string Title,
    string Content,
    DateTimeOffset? DueAt,
    string Priority,
    string? BusinessObjectId,
    string? WorkflowInstanceId,
    string EnvironmentCode,
    bool IsTestData,
    string Status,
    DateTimeOffset CreatedAt,
    string? ProjectId = null,
    string? ProjectName = null);

public sealed record MinutesConfirmationUserDto(
    string Id,
    string UserName,
    string DisplayName,
    string DepartmentId,
    string DepartmentName);

public sealed record MinutesTaskTraceDto(
    MinutesTaskDto Task,
    MinutesSourceDto Source,
    MinutesActionItemDto ActionItem);

public sealed record AnalyzeMinutesRequest(Guid AgentProfileId);

public sealed record AgentExecutionResultDto(
    AgentRunDto Run,
    AgentGatewayCallDto Call,
    IReadOnlyList<MinutesActionItemDto> ActionItems);

public sealed record AgentGatewayCallDto(
    string Id,
    string AgentRunId,
    string Model,
    string Mode,
    string? GatewayCallId,
    bool ExternalModelCalled,
    string InputDigest,
    string? OutputDigest,
    string Status,
    string? ErrorCode,
    string? ErrorMessage,
    int DurationMs,
    DateTimeOffset CreatedAt);

public sealed record SaveWorkflowNodePolicyRequest(
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
    bool Publish = true,
    string AssigneeStrategy = "SpecificUser",
    string? BusinessRoleCode = null,
    bool AllowTransfer = false,
    string? TransferTargetStrategy = null);

public sealed record WorkflowNodePolicyDto(
    string Id,
    string WorkflowVersionId,
    string NodeCode,
    int VersionNumber,
    string? ApproverUserId,
    string? BackupApproverUserId,
    int? SlaMinutes,
    int? ReminderMinutes,
    int? EscalationMinutes,
    string TimeoutStrategy,
    bool AllowAgentAssist,
    bool AllowAgentProxy,
    string? AgentProfileId,
    string AgentMaxRisk,
    decimal AgentMaxAmount,
    string AgentMaxAutonomy,
    string AgentProxyMode,
    int? AutoProxyAfterMinutes,
    bool PermanentHumanBoundary,
    int RequiredAttachmentCount,
    string? PrecheckItems,
    string Status,
    DateTimeOffset PublishedAt,
    string AssigneeStrategy = "SpecificUser",
    string? BusinessRoleCode = null,
    bool AllowTransfer = false,
    string? TransferTargetStrategy = null);

public sealed record WorkflowPolicyUserDto(
    string Id,
    string UserName,
    string DisplayName);

public sealed record WorkflowVersionNodeDto(
    string Code,
    string Name,
    string? DefaultAssigneeUserName,
    string? NextNodeCode);

public sealed record WorkflowNotificationDto(
    string Id,
    string RecipientId,
    string WorkflowTaskId,
    string Type,
    string Title,
    string Message,
    bool IsRead,
    DateTimeOffset CreatedAt);

public sealed record SlaProcessingResultDto(
    DateTimeOffset ScannedAt,
    int Reminded,
    int Escalated,
    int AgentAssistProposed,
    int AgentProxied);

public sealed record RequestAgentProxyRequest(bool ConfirmImmediately = false);

public sealed record ConfirmAgentProxyRequest(Guid ConfirmationId);
