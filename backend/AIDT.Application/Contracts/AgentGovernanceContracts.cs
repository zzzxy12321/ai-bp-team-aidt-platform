using AIDT.Domain.Agents;

namespace AIDT.Application.Contracts;

public sealed record AgentProfileDetailDto(
    string Id,
    string Code,
    string Name,
    string Description,
    string ServiceUserId,
    string ServiceUserName,
    string DepartmentId,
    string DepartmentName,
    string BusinessOwnerUserId,
    string BusinessOwnerDisplayName,
    string TechnicalOwnerUserId,
    string TechnicalOwnerDisplayName,
    AgentAutonomyLevel AutonomyLevel,
    AgentRiskLevel RiskLimit,
    decimal AmountLimit,
    IReadOnlyList<string> DataScopes,
    IReadOnlyList<string> ModelScopes,
    IReadOnlyList<string> AllowedTools,
    IReadOnlyList<string> RequiredConfirmations,
    IReadOnlyList<string> StopConditions,
    AgentProfileLifecycleStatus LifecycleStatus,
    string? CurrentPublishedVersionId,
    int? CurrentPublishedVersionNumber,
    long Version,
    long Revision,
    string CreatedBy,
    string CreatedByDisplayName,
    DateTimeOffset CreatedAt,
    string UpdatedBy,
    string UpdatedByDisplayName,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastRunAt = null,
    int RecentRunCount = 0);

public sealed record AgentDepartmentOptionDto(
    string Id,
    string Code,
    string Name,
    bool IsManager);

public sealed record AgentPersonOptionDto(
    string Id,
    string UserName,
    string DisplayName,
    IReadOnlyList<string> DepartmentIds);

public sealed record AgentServiceIdentityOptionDto(
    string Id,
    string UserName,
    string DisplayName,
    string? BoundAgentProfileId,
    string? BoundAgentProfileName);

public sealed record AgentCapabilityOptionDto(
    string Code,
    string Name,
    string Description,
    AgentRiskLevel RiskLevel,
    bool RequiresConfirmation,
    IReadOnlyList<string> RequiredPermissions,
    string? McpScope);

public sealed record AgentIdentityOptionsDto(
    string OrganizationId,
    IReadOnlyList<AgentDepartmentOptionDto> Departments,
    IReadOnlyList<AgentPersonOptionDto> People,
    IReadOnlyList<AgentServiceIdentityOptionDto> ServiceIdentities,
    IReadOnlyList<AgentCapabilityOptionDto> Capabilities,
    IReadOnlyList<string> PermanentHumanBoundaries,
    IReadOnlyList<string> ConfirmationRules,
    IReadOnlyList<string> StopConditions,
    IReadOnlyList<string> ModelScopes);

public sealed record AgentRunEvidenceDto(
    string Id,
    string AgentProfileId,
    string AgentProfileName,
    string RequestedBy,
    string RequestedByDisplayName,
    string Action,
    decimal Amount,
    AgentRiskLevel RiskLevel,
    AgentRunStatus Status,
    DateTimeOffset CreatedAt,
    string DepartmentId,
    string EnvironmentCode,
    string? BusinessObjectId,
    string? BusinessObjectCode,
    string? BusinessObjectTitle,
    string? WorkflowInstanceId,
    string? WorkflowName,
    string? WorkflowStatus,
    string? WorkflowTaskId,
    string? WorkflowTaskNode,
    string? WorkflowTaskStatus,
    string? MinutesSourceId,
    string? MinutesSourceTitle,
    string? Model,
    string? GatewayCallId,
    string? GatewayMode,
    bool ExternalModelCalled,
    string? ConfirmationId,
    bool HumanConfirmed,
    string? OriginalAssigneeId,
    string? OriginalAssigneeDisplayName,
    string? ProxyTriggerMode,
    string? CompensationStatus);

public sealed record AgentWorkflowNodeEvidenceDto(
    string PolicyId,
    string WorkflowDefinitionCode,
    string WorkflowDefinitionName,
    string WorkflowVersionId,
    int WorkflowVersionNumber,
    string NodeCode,
    string Status,
    bool AllowAgentAssist,
    bool AllowAgentProxy,
    bool PermanentHumanBoundary,
    DateTimeOffset PublishedAt);

public sealed record AgentWorkflowTaskEvidenceDto(
    string TaskId,
    string WorkflowInstanceId,
    string WorkflowDefinitionCode,
    string WorkflowDefinitionName,
    string NodeCode,
    string Status,
    string DecisionMode,
    DateTimeOffset CreatedAt);

public sealed record AgentGatewayCallEvidenceDto(
    string Id,
    string AgentRunId,
    string Model,
    string Mode,
    string Status,
    bool ExternalModelCalled,
    int DurationMs,
    string? ErrorMessage,
    DateTimeOffset CreatedAt);

public sealed record AgentProfileContextDto(
    AgentProfileDetailDto Profile,
    IReadOnlyList<AgentRunEvidenceDto> RecentRuns,
    IReadOnlyList<AgentWorkflowNodeEvidenceDto> WorkflowNodes,
    IReadOnlyList<AgentWorkflowTaskEvidenceDto> WorkflowTasks,
    IReadOnlyList<McpClientSummaryDto> McpClients,
    IReadOnlyList<McpTokenSummaryDto> McpTokens,
    IReadOnlyList<AgentGatewayCallEvidenceDto> GatewayCalls,
    IReadOnlyList<AuditEventDto> AuditTimeline,
    bool CanViewMcpEvidence,
    bool CanViewAuditTimeline);

public sealed record AgentProfileVersionDto(
    string Id,
    string AgentProfileId,
    int VersionNumber,
    long Revision,
    AgentProfileVersionStatus Status,
    string Code,
    string Name,
    string Description,
    string ServiceUserId,
    string ServiceUserName,
    string DepartmentId,
    Guid BusinessOwnerUserId,
    Guid TechnicalOwnerUserId,
    AgentAutonomyLevel AutonomyLevel,
    AgentRiskLevel RiskLimit,
    decimal AmountLimit,
    IReadOnlyList<string> DataScopes,
    IReadOnlyList<string> ModelScopes,
    IReadOnlyList<string> AllowedTools,
    IReadOnlyList<string> RequiredConfirmations,
    IReadOnlyList<string> StopConditions,
    bool IsHighRisk,
    IReadOnlyList<string> HighRiskReasons,
    IReadOnlyList<string> ValidationErrors,
    IReadOnlyList<string> ValidationWarnings,
    string CreatedBy,
    string CreatedByDisplayName,
    DateTimeOffset CreatedAt,
    string UpdatedBy,
    string UpdatedByDisplayName,
    DateTimeOffset UpdatedAt,
    string? ValidatedBy,
    DateTimeOffset? ValidatedAt,
    string? PublishedBy,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? RetiredAt);

public sealed record AgentProfileValidationDto(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    bool IsHighRisk,
    IReadOnlyList<string> HighRiskReasons,
    DateTimeOffset ValidatedAt);

public sealed record AgentProfilePublishResultDto(
    string Status,
    AgentProfileDetailDto? Profile,
    string? ChangeRequestId,
    IReadOnlyList<string> HighRiskReasons);

public sealed record AgentProfileChangeRequestDto(
    string Id,
    string AgentProfileId,
    string AgentProfileName,
    string AgentProfileVersionId,
    int VersionNumber,
    string Status,
    string RiskReason,
    string RequestedBy,
    string RequestedByDisplayName,
    DateTimeOffset RequestedAt,
    string? ReviewedBy,
    DateTimeOffset? ReviewedAt,
    string? ReviewComment);

public sealed record CreateAgentProfileDraftRequest(
    string Code,
    string Name,
    string Description,
    Guid ServiceUserId,
    Guid DepartmentId,
    Guid BusinessOwnerUserId,
    Guid TechnicalOwnerUserId,
    AgentAutonomyLevel AutonomyLevel,
    AgentRiskLevel RiskLimit,
    decimal AmountLimit,
    IReadOnlyList<string> DataScopes,
    IReadOnlyList<string> ModelScopes,
    IReadOnlyList<string> AllowedTools,
    IReadOnlyList<string> RequiredConfirmations,
    IReadOnlyList<string> StopConditions);

public sealed record CreateNextAgentProfileDraftRequest(
    long ExpectedVersion,
    string? Reason = null);

public sealed record UpdateAgentProfileDraftRequest(
    long ExpectedRevision,
    string Name,
    string Description,
    Guid ServiceUserId,
    AgentAutonomyLevel AutonomyLevel,
    AgentRiskLevel RiskLimit,
    decimal AmountLimit,
    IReadOnlyList<string> DataScopes,
    IReadOnlyList<string> ModelScopes,
    IReadOnlyList<string> AllowedTools,
    IReadOnlyList<string> RequiredConfirmations,
    IReadOnlyList<string> StopConditions);

public sealed record DisableAgentProfileRequest(
    long ExpectedVersion,
    string Reason);

public sealed record ValidateAgentProfileVersionRequest(
    long ExpectedRevision);

public sealed record PublishAgentProfileVersionRequest(
    long ExpectedRevision,
    string? Reason = null);

public sealed record ResumeAgentProfileRequest(
    long ExpectedVersion,
    string Reason);

public sealed record RollbackAgentProfileRequest(
    long ExpectedVersion,
    Guid TargetVersionId,
    string Reason);

public sealed record ReviewAgentProfileChangeRequest(
    bool Approve,
    string Comment,
    long ExpectedRevision);
