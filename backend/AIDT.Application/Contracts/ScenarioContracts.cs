using AIDT.Domain.Scenarios;

namespace AIDT.Application.Contracts;

public sealed record ScenarioStepDefinitionDto(
    int Sequence,
    string Code,
    string Name,
    string BusinessDescription,
    string ResponsibleRole,
    string Action,
    string ExpectedResult,
    string Evidence,
    bool PermanentHumanBoundary = false);

public sealed record ScenarioPreparationItemDto(
    string Code,
    string Name,
    bool IsRequired,
    string SourceType,
    string Description);

public sealed record ScenarioAgentUsageDto(
    string AgentProfileCode,
    string Capability,
    string Boundary);

public sealed record ScenarioDefinitionPayloadDto(
    string ProblemSolved,
    ScenarioStartMode StartMode,
    string TriggerDescription,
    IReadOnlyList<string> ApplicableDepartmentCodes,
    IReadOnlyList<string> ParticipantRoles,
    IReadOnlyList<string> ObjectTypeCodes,
    string? WorkflowDefinitionCode,
    int? WorkflowVersionNumber,
    IReadOnlyList<ScenarioStepDefinitionDto> Steps,
    IReadOnlyList<ScenarioPreparationItemDto> PreparationItems,
    IReadOnlyList<string> SupportedFileSources,
    IReadOnlyList<string> SupportedMinutesSources,
    IReadOnlyList<ScenarioAgentUsageDto> AllowedAgents,
    IReadOnlyList<string> PermanentHumanBoundaries,
    IReadOnlyList<string> PermissionRequirements,
    IReadOnlyList<string> CompletionEvidence);

public sealed record CreateScenarioDraftRequest(
    string Code,
    string Name,
    string Description,
    Guid OwnerDepartmentId,
    ScenarioDefinitionPayloadDto Payload);

public sealed record UpdateScenarioDraftRequest(
    long ExpectedRevision,
    string Name,
    string Description,
    Guid OwnerDepartmentId,
    ScenarioDefinitionPayloadDto Payload);

public sealed record CreateNextScenarioVersionDraftRequest(
    long ExpectedVersion,
    string ChangeSummary);

public sealed record ScenarioActionRequest(
    long ExpectedVersion,
    string? Comment);

public sealed record RollbackScenarioRequest(
    long ExpectedVersion,
    int TargetVersionNumber,
    string Reason);

public sealed record ScenarioReferenceSnapshotDto(
    IReadOnlyList<ScenarioObjectTypeReferenceDto> ObjectTypes,
    ScenarioWorkflowReferenceDto? Workflow,
    IReadOnlyList<ScenarioAgentReferenceDto> Agents,
    DateTimeOffset FrozenAt);

public sealed record ScenarioObjectTypeReferenceDto(
    string Id,
    string Code,
    string Name,
    long Version,
    string Status);

public sealed record ScenarioWorkflowReferenceDto(
    string DefinitionId,
    string DefinitionCode,
    string DefinitionName,
    string VersionId,
    int VersionNumber,
    string Status);

public sealed record ScenarioAgentReferenceDto(
    string Id,
    string Code,
    string Name,
    int VersionNumber,
    string LifecycleStatus,
    IReadOnlyList<string> AllowedTools);

public sealed record ScenarioValidationDto(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    ScenarioReferenceSnapshotDto? References,
    DateTimeOffset ValidatedAt);

public sealed record ScenarioVersionDto(
    string Id,
    string ScenarioId,
    int VersionNumber,
    ScenarioVersionStatus Status,
    long Revision,
    ScenarioDefinitionPayloadDto Payload,
    ScenarioReferenceSnapshotDto? References,
    ScenarioValidationDto? Validation,
    string? ChangeSummary,
    string CreatedBy,
    string CreatedByDisplayName,
    DateTimeOffset CreatedAt,
    string? UpdatedBy,
    string? UpdatedByDisplayName,
    DateTimeOffset UpdatedAt,
    string? PublishedBy,
    string? PublishedByDisplayName,
    DateTimeOffset? PublishedAt,
    string? RetiredBy,
    string? RetiredByDisplayName,
    DateTimeOffset? RetiredAt);

public sealed record ScenarioDto(
    string Id,
    string Code,
    string Name,
    string Description,
    string OwnerDepartmentId,
    string OwnerDepartmentName,
    ScenarioLifecycleStatus LifecycleStatus,
    long Version,
    long Revision,
    string CreatedBy,
    string CreatedByDisplayName,
    DateTimeOffset CreatedAt,
    string? UpdatedBy,
    string? UpdatedByDisplayName,
    DateTimeOffset UpdatedAt,
    string? DisabledBy,
    string? DisabledByDisplayName,
    DateTimeOffset? DisabledAt,
    string? DisabledReason,
    ScenarioVersionDto? CurrentPublishedVersion,
    ScenarioVersionDto? DraftVersion,
    bool CanManage,
    string? PendingApprovalRequestId = null,
    string? PendingApprovalStatus = null);

public sealed record ScenarioApprovalRequestDto(
    string Id,
    string ScenarioId,
    string ScenarioCode,
    string ScenarioName,
    int VersionNumber,
    string RiskReason,
    string Status,
    string RequestedBy,
    string RequestedByDisplayName,
    DateTimeOffset RequestedAt,
    string? ReviewedBy,
    string? ReviewedByDisplayName,
    DateTimeOffset? ReviewedAt,
    string? ReviewComment);

public sealed record ReviewScenarioPublishRequest(
    long ExpectedVersion,
    bool Approve,
    string Comment);

public sealed record ScenarioStartInputDto(
    Guid? DepartmentId = null,
    Guid? BusinessObjectId = null,
    string? ObjectCode = null,
    string? Title = null,
    IReadOnlyDictionary<string, string?>? Fields = null,
    Guid? ProjectId = null,
    Guid? AssigneeId = null,
    string? TaskTitle = null,
    string? TaskContent = null,
    DateTimeOffset? DueAt = null,
    string? Priority = null,
    string? Content = null,
    IReadOnlyList<Guid>? AttachmentIds = null,
    IReadOnlyList<Guid>? MinutesSourceIds = null,
    bool RequestAgentSuggestion = false,
    Guid? AgentProfileId = null,
    string? Comment = null);

public sealed record ScenarioPreparationCheckDto(
    string Code,
    string Name,
    string SourceType,
    bool IsRequired,
    bool IsProvided,
    bool IsValid,
    string Message);

public sealed record ScenarioPreflightDto(
    bool CanStart,
    string ScenarioId,
    string ScenarioCode,
    int ScenarioVersionNumber,
    ScenarioStartMode StartMode,
    string DepartmentId,
    string DepartmentName,
    IReadOnlyList<string> BlockingReasons,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<ScenarioPreparationCheckDto> PreparationChecks,
    ScenarioReferenceSnapshotDto References,
    DateTimeOffset CheckedAt);

public sealed record ScenarioBusinessLinkDto(
    string Type,
    string Id,
    string Name,
    string Role,
    string DetailPath);

public sealed record ScenarioLaunchDto(
    string Id,
    string ScenarioId,
    string ScenarioCode,
    string ScenarioName,
    string ScenarioVersionId,
    int ScenarioVersionNumber,
    string DepartmentId,
    string DepartmentName,
    string StartedBy,
    string StartedByDisplayName,
    DateTimeOffset StartedAt,
    string? BusinessObjectId,
    string? BusinessObjectCode,
    string? WorkflowInstanceId,
    string? WorkflowTaskId,
    string? ProjectId,
    string? ProjectTaskId,
    string? EventInboxId,
    string? AgentRunId,
    IReadOnlyList<string> AttachmentIds,
    IReadOnlyList<string> MinutesSourceIds,
    IReadOnlyList<ScenarioBusinessLinkDto> BusinessLinks,
    IReadOnlyList<string> PermanentHumanBoundaries);

public sealed record ScenarioLaunchSummaryDto(
    string Id,
    string ScenarioId,
    string ScenarioCode,
    string ScenarioName,
    string DepartmentId,
    string DepartmentName,
    string EnvironmentCode,
    bool IsDemo,
    DateTimeOffset StartedAt,
    string CurrentStep,
    string WaitingFor,
    DateTimeOffset? DueAt,
    int UploadedMaterialCount,
    IReadOnlyList<string> MissingMaterials,
    string WorkflowStatus,
    string TaskStatus,
    string ProjectName,
    string AgentSuggestion,
    string NextAction,
    bool IsCompleted,
    IReadOnlyList<ScenarioBusinessLinkDto> BusinessLinks,
    string EvidencePath);
