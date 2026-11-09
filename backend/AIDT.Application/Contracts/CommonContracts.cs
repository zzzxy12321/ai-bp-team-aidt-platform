using AIDT.Domain.Agents;
using AIDT.Domain.Audit;
using AIDT.Domain.BusinessObjects;
using AIDT.Domain.Workflows;

namespace AIDT.Application.Contracts;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long Total);

public sealed record SessionDto(
    string UserId,
    string DisplayName,
    string OrganizationId,
    bool IsAuthenticated,
    bool IsServiceIdentity,
    IReadOnlyList<string> Roles,
    string AuthorizationState = "Current",
    string? AuthorizationMessage = null);

public sealed record PermissionDto(string Resource, string Action);

public sealed record ObjectTypeDto(
    string Id,
    string Code,
    string Name,
    long Version,
    IReadOnlyList<ObjectFieldDto> Fields,
    string Status = "Published",
    long Revision = 1,
    DateTimeOffset? PublishedAt = null,
    DateTimeOffset? RetiredAt = null);

public sealed record ObjectFieldDto(
    string Key,
    string DisplayName,
    string DataType,
    bool IsRequired,
    int MaxLength,
    string Sensitivity,
    string? DefaultValue = null,
    IReadOnlyList<string>? Options = null,
    bool ShowInList = true,
    bool Filterable = false,
    int SortOrder = 0,
    string? Code = null);

public sealed record CreateObjectTypeRequest(
    string Code,
    string Name,
    IReadOnlyList<ObjectFieldDto> Fields);

public sealed record UpdateObjectTypeDraftRequest(
    long ExpectedRevision,
    string Name,
    IReadOnlyList<ObjectFieldDto> Fields);

public sealed record ObjectTypeActionRequest(long ExpectedRevision);

public sealed record ObjectTypeValidationDto(
    string ObjectTypeId,
    bool IsValid,
    IReadOnlyList<string> Errors,
    long Version,
    long Revision,
    DateTimeOffset ValidatedAt);

public sealed record ObjectTypeVersionDto(
    string Id,
    string ObjectTypeId,
    long Version,
    string Status,
    string Name,
    IReadOnlyList<ObjectFieldDto> Fields,
    DateTimeOffset? ValidatedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? RetiredAt,
    string? CreatedBy,
    string? UpdatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record BusinessObjectDto(
    string Id,
    string ObjectTypeId,
    string ObjectCode,
    string Status,
    long Version,
    IReadOnlyDictionary<string, string?> Fields,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string DepartmentId = "",
    string EnvironmentCode = "demo",
    bool IsTestData = true,
    string SourceSystem = "AIDT",
    string? Title = null,
    string? CreatedByUserId = null);

public sealed record CreateBusinessObjectRequest(
    Guid ObjectTypeId,
    string ObjectCode,
    IReadOnlyDictionary<string, string?> Fields,
    Guid? DepartmentId = null);

public sealed record UpdateBusinessObjectRequest(
    long ExpectedVersion,
    IReadOnlyDictionary<string, string?> Fields,
    Guid? ObjectTypeId = null,
    string? ObjectCode = null);

public sealed record BusinessObjectVersionDto(
    string Id,
    string BusinessObjectId,
    long Version,
    string Status,
    IReadOnlyDictionary<string, string?> Fields,
    string? ActorId,
    string? Reason,
    DateTimeOffset CreatedAt);

public sealed record BusinessObjectProjectLinkDto(
    string ProjectId,
    string RelationId,
    string ObjectCode,
    string Name,
    string Status,
    string RelationCode,
    string? EvidenceExcerpt,
    DateTimeOffset CreatedAt);

public sealed record BusinessObjectFileLinkDto(
    string Id,
    string OriginalFileName,
    string MimeType,
    long SizeBytes,
    string Sensitivity,
    string Status,
    int CurrentVersionNumber,
    DateTimeOffset CreatedAt);

public sealed record BusinessObjectContextDto(
    IReadOnlyList<BusinessObjectProjectLinkDto> Projects,
    IReadOnlyList<WorkflowInstanceDto> Workflows,
    IReadOnlyList<BusinessObjectFileLinkDto> Files,
    IReadOnlyList<AuditEventDto> AuditEvents);

public sealed record ObjectFieldUserOptionDto(
    string Id,
    string UserName,
    string DisplayName,
    string DepartmentId,
    string DepartmentName);

public sealed record WorkflowInstanceDto(
    string Id,
    string WorkflowDefinitionId,
    string WorkflowVersionId,
    string BusinessObjectId,
    WorkflowInstanceStatus Status,
    long Version,
    string? CurrentNodeCode,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    string DepartmentId = "",
    string? StartedByUserId = null,
    string EnvironmentCode = "demo",
    bool IsTestData = true,
    string SourceSystem = "AIDT");

public sealed record WorkflowDefinitionDto(
    string Id,
    string VersionId,
    string Code,
    string Name,
    int VersionNumber,
    string ObjectTypeCode);

public sealed record StartWorkflowInstanceRequest(
    Guid WorkflowDefinitionId,
    Guid WorkflowVersionId,
    Guid BusinessObjectId,
    string? IdempotencyKey);

public sealed record WorkflowTaskDto(
    string Id,
    string WorkflowInstanceId,
    string NodeCode,
    string AssigneeId,
    WorkflowTaskStatus Status,
    long Version,
    string? DecisionComment,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    string DepartmentId = "",
    string EnvironmentCode = "demo",
    bool IsTestData = true,
    DateTimeOffset? DueAt = null,
    string SlaStatus = "None",
    DateTimeOffset? ReminderSentAt = null,
    DateTimeOffset? EscalatedAt = null,
    string DecisionMode = "Human",
    string? AgentRunId = null,
    string AssigneeStrategy = "SpecificUser",
    string? BusinessRoleCode = null,
    int? BusinessRoleVersion = null,
    IReadOnlyList<string>? CandidateUserIds = null,
    string? OriginalAssigneeId = null,
    string? BackupAssigneeId = null,
    string? TransferredBy = null,
    DateTimeOffset? TransferredAt = null,
    string? TransferReason = null,
    string? NodeName = null,
    string FormDefinitionJson = "[]",
    string FormDataJson = "{}",
    string FormSchemaVersion = "1.0",
    string FormDefinitionHash = "");

public sealed record TaskDecisionRequest(
    WorkflowDecision Decision,
    long ExpectedVersion,
    string? Comment,
    string? IdempotencyKey,
    string? FormDataJson = null,
    string? FormDefinitionHash = null);

public sealed record TransferWorkflowTaskRequest(
    Guid TargetUserId,
    long ExpectedVersion,
    string Reason,
    string IdempotencyKey);

public sealed record EscalateWorkflowTaskRequest(
    long ExpectedVersion,
    string Reason,
    string IdempotencyKey);

public sealed record AuditEventDto(
    string Id,
    string ActorId,
    string Module,
    string Action,
    string EntityType,
    string EntityId,
    string TraceId,
    AuditOutcome Outcome,
    DateTimeOffset OccurredAt,
    string? Reason);

public sealed record AgentRunDto(
    string Id,
    string AgentProfileId,
    string RequestedBy,
    string Action,
    decimal Amount,
    AgentRiskLevel RiskLevel,
    AgentRunStatus Status,
    string? ConfirmationId,
    DateTimeOffset CreatedAt,
    string OrganizationId = "",
    string DepartmentId = "",
    string EnvironmentCode = "demo",
    bool IsTestData = true,
    string? SubjectType = null,
    string? BusinessObjectId = null,
    string? WorkflowInstanceId = null,
    string? WorkflowTaskId = null,
    string? MinutesSourceId = null,
    string? Model = null,
    string? GatewayCallId = null,
    string? GatewayMode = null,
    bool ExternalModelCalled = false,
    string? ProxyTriggerMode = null,
    DateTimeOffset? ProxyTriggeredAt = null,
    string? OriginalAssigneeId = null,
    string? CompensationStatus = null,
    string? ProxyIdempotencyKey = null);

public sealed record AgentProfileDto(
    string Id,
    string Name,
    AgentAutonomyLevel AutonomyLevel,
    decimal AmountLimit,
    AgentRiskLevel RiskLimit,
    IReadOnlyList<string> AllowedTools,
    bool IsActive,
    string ServiceUserId = "",
    string Code = "",
    string DepartmentId = "",
    string Description = "");

public sealed record ConfirmAgentRunRequest(Guid ConfirmationId);

public sealed record CreateAgentRunRequest(
    Guid AgentProfileId,
    AgentActionType Action,
    decimal Amount,
    AgentRiskLevel RiskLevel,
    string? Model = null,
    string? ToolCode = null);

public sealed record DemoLoginRequest(string UserName);

public sealed record DemoLoginResponse(
    string UserId,
    string DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<PermissionDto> Permissions);

public sealed record WorkflowTimelineItemDto(
    string Id,
    string Kind,
    string Title,
    string? Detail,
    string ActorId,
    DateTimeOffset OccurredAt);

public sealed record DashboardDto(
    int PendingTaskCount,
    int RunningWorkflowCount,
    int BusinessObjectCount,
    int TodayAuditCount,
    int PendingAgentConfirmationCount,
    string DatabaseStatus);

public sealed record ConnectorDto(
    string Id,
    string Code,
    string Name,
    string AdapterType,
    string ConnectionStatus,
    string AuthorityOwner);

public sealed record OutboxMessageDto(
    string Id,
    string EventType,
    string AggregateType,
    string AggregateId,
    string IdempotencyKey,
    string Status,
    int AttemptCount,
    DateTimeOffset CreatedAt);

public sealed record CapabilityDto(
    string FeatureId,
    string Name,
    string State,
    string Reason);

public sealed record PlatformCapabilitiesDto(
    string ProjectState,
    string DatabaseMigrationState,
    string DatabaseProvider,
    string ExternalSystemsMode,
    string CurrentEnvironmentCode,
    long MaxUploadBytes,
    IReadOnlyList<CapabilityDto> Capabilities);
