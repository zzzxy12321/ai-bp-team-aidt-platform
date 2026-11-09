namespace AIDT.Infrastructure.Persistence;

public sealed class OrganizationEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class UserEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsServiceIdentity { get; set; }
    public bool IsActive { get; set; }
    public string InvitationStatus { get; set; } = "Active";
    public DateTimeOffset? InvitedAt { get; set; }
    public DateTimeOffset? ActivatedAt { get; set; }
    public long Version { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class RoleEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RoleType { get; set; } = "Business";
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
    public long Version { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class PermissionEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}

public sealed class UserRoleEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}

public sealed class UserRoleAssignmentEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public string ScopeType { get; set; } = "organization";
    public string ScopeReference { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid AssignedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? RevokedBy { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

public sealed class RolePermissionEntity
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}

public sealed class DataScopeEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ScopeType { get; set; } = string.Empty;
    public string ScopeReference { get; set; } = string.Empty;
}

public sealed class DepartmentEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? ParentDepartmentId { get; set; }
    public string HierarchyType { get; set; } = "Department";
    public string Path { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public long Version { get; set; } = 1;
    public Guid? ManagerUserId { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class AccessChangeRequestEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public Guid? TargetUserId { get; set; }
    public Guid? TargetRoleId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public string RiskReason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public Guid RequestedBy { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewComment { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class UserDepartmentMembershipEntity
{
    public Guid UserId { get; set; }
    public Guid DepartmentId { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsManager { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
}

public sealed class ObjectTypeEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long Version { get; set; }
    public long Revision { get; set; } = 1;
    public string Status { get; set; } = "Draft";
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? RetiredAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class FieldDefinitionEntity
{
    public Guid Id { get; set; }
    public Guid ObjectTypeId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public int MaxLength { get; set; }
    public string Sensitivity { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
    public string? OptionsJson { get; set; }
    public bool ShowInList { get; set; } = true;
    public bool Filterable { get; set; }
    public int SortOrder { get; set; }
    public int SchemaVersion { get; set; }
}

public sealed class ObjectTypeVersionEntity
{
    public long Id { get; set; }
    public Guid ObjectTypeId { get; set; }
    public long Version { get; set; }
    public string Status { get; set; } = "Draft";
    public string Name { get; set; } = string.Empty;
    public string FieldsJson { get; set; } = "[]";
    public DateTimeOffset? ValidatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? RetiredAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class ObjectRelationEntity
{
    public Guid Id { get; set; }
    public Guid ObjectTypeId { get; set; }
    public Guid TargetObjectTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
}

public sealed class BusinessObjectEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid DepartmentId { get; set; }
    public string EnvironmentCode { get; set; } = "demo";
    public bool IsTestData { get; set; } = true;
    public string SourceSystem { get; set; } = "AIDT";
    public Guid ObjectTypeId { get; set; }
    public string ObjectCode { get; set; } = string.Empty;
    public string? Title { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public long Version { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public string FieldsJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public sealed class BusinessObjectVersionEntity
{
    public long Id { get; set; }
    public Guid BusinessObjectId { get; set; }
    public long Version { get; set; }
    public string SnapshotJson { get; set; } = "{}";
    public Guid ActorId { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class BusinessObjectRelationEntity
{
    public Guid Id { get; set; }
    public Guid SourceObjectId { get; set; }
    public Guid TargetObjectId { get; set; }
    public string RelationCode { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class WorkflowDefinitionEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public string ObjectTypeCode { get; set; } = "ANY";
    public string ApplicableRoleCodesJson { get; set; } = "[]";
    public bool IsDefaultForDepartment { get; set; }
    public string RiskLevel { get; set; } = "Low";
    public bool RequiresPlatformApproval { get; set; }
    public string EnvironmentCode { get; set; } = "demo";
    public bool IsTestData { get; set; } = true;
    public long Version { get; set; } = 1;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class WorkflowVersionEntity
{
    public Guid Id { get; set; }
    public Guid WorkflowDefinitionId { get; set; }
    public int VersionNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string DefinitionJson { get; set; } = "{}";
    public DateTimeOffset? PublishedAt { get; set; }
}

public sealed class WorkflowInstanceEntity
{
    public Guid Id { get; set; }
    public Guid WorkflowDefinitionId { get; set; }
    public Guid WorkflowVersionId { get; set; }
    public Guid BusinessObjectId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid? StartedByUserId { get; set; }
    public string EnvironmentCode { get; set; } = "demo";
    public bool IsTestData { get; set; } = true;
    public string SourceSystem { get; set; } = "AIDT";
    public string Status { get; set; } = string.Empty;
    public long Version { get; set; }
    public string? CurrentNodeCode { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
}

public sealed class WorkflowTaskEntity
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public string NodeCode { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public Guid AssigneeId { get; set; }
    public Guid DepartmentId { get; set; }
    public string EnvironmentCode { get; set; } = "demo";
    public bool IsTestData { get; set; } = true;
    public string Status { get; set; } = string.Empty;
    public long Version { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public string? DecisionComment { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Guid? NodePolicyId { get; set; }
    public string FormDefinitionJson { get; set; } = "[]";
    public string FormDataJson { get; set; } = "{}";
    public string FormSchemaVersion { get; set; } = "1.0";
    public string FormDefinitionHash { get; set; } = string.Empty;
    public DateTimeOffset? DueAt { get; set; }
    public string SlaStatus { get; set; } = "None";
    public DateTimeOffset? ReminderSentAt { get; set; }
    public DateTimeOffset? EscalatedAt { get; set; }
    public string DecisionMode { get; set; } = "Human";
    public Guid? AgentRunId { get; set; }
    public string AssigneeStrategy { get; set; } = "SpecificUser";
    public Guid? BusinessRoleId { get; set; }
    public string? BusinessRoleCode { get; set; }
    public int? BusinessRoleVersion { get; set; }
    public string CandidateUserIdsJson { get; set; } = "[]";
    public string AssigneeSnapshotJson { get; set; } = "{}";
    public Guid? OriginalAssigneeId { get; set; }
    public Guid? BackupAssigneeId { get; set; }
    public Guid? TransferredBy { get; set; }
    public DateTimeOffset? TransferredAt { get; set; }
    public string? TransferReason { get; set; }
}

public sealed class WorkflowNodePolicyEntity
{
    public Guid Id { get; set; }
    public Guid WorkflowVersionId { get; set; }
    public string NodeCode { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string? BusinessDescription { get; set; }
    public int VersionNumber { get; set; }
    public Guid? ApproverUserId { get; set; }
    public Guid? BackupApproverUserId { get; set; }
    public string AssigneeStrategy { get; set; } = "SpecificUser";
    public Guid? BusinessRoleId { get; set; }
    public bool AllowTransfer { get; set; }
    public string? TransferTargetStrategy { get; set; }
    public int? SlaMinutes { get; set; }
    public int? ReminderMinutes { get; set; }
    public int? EscalationMinutes { get; set; }
    public string TimeoutStrategy { get; set; } = "Escalate";
    public bool AllowAgentAssist { get; set; }
    public bool AllowAgentProxy { get; set; }
    public Guid? AgentProfileId { get; set; }
    public string AgentMaxRisk { get; set; } = "Low";
    public decimal AgentMaxAmount { get; set; }
    public string AgentMaxAutonomy { get; set; } = "A2";
    public string AgentProxyMode { get; set; } = "RemindOnly";
    public int? AutoProxyAfterMinutes { get; set; }
    public bool PermanentHumanBoundary { get; set; }
    public int RequiredAttachmentCount { get; set; }
    public string? PrecheckItems { get; set; }
    public string DeliverablesJson { get; set; } = "[]";
    public string RelatedRoleCodesJson { get; set; } = "[]";
    public string? CompletionCondition { get; set; }
    public string FormSchemaJson { get; set; } = "[]";
    public string FormSchemaVersion { get; set; } = "1.0";
    public string NodeActionsJson { get; set; } = "[]";
    public string AiReviewJson { get; set; } = "{}";
    public string NotificationPolicyJson { get; set; } = "{}";
    public string Status { get; set; } = "Draft";
    public Guid PublishedBy { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
}

public sealed class WorkflowNotificationEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid RecipientId { get; set; }
    public Guid WorkflowTaskId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class WorkflowTaskCandidateEntity
{
    public Guid WorkflowTaskId { get; set; }
    public Guid UserId { get; set; }
}

public sealed class WorkflowTaskActionEntity
{
    public long Id { get; set; }
    public Guid WorkflowTaskId { get; set; }
    public Guid ActorId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class AuditEventEntity
{
    public Guid Id { get; set; }
    public Guid ActorId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? Reason { get; set; }
    public string? IpHash { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

public sealed class EvidenceAssetEntity
{
    public Guid Id { get; set; }
    public Guid? BusinessObjectId { get; set; }
    public Guid? WorkflowInstanceId { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string Sensitivity { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ConnectorEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AdapterType { get; set; } = string.Empty;
    public string ConnectionStatus { get; set; } = string.Empty;
    public string AuthorityOwner { get; set; } = string.Empty;
}

public sealed class ConnectorCommandEntity
{
    public Guid Id { get; set; }
    public Guid ConnectorId { get; set; }
    public string CommandType { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string? ApprovalEvidenceJson { get; set; }
    public string Status { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class ConnectorReceiptEntity
{
    public Guid Id { get; set; }
    public Guid ConnectorCommandId { get; set; }
    public string? ExternalId { get; set; }
    public int StatusCode { get; set; }
    public string ResponseDigest { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; }
}

public sealed class OutboxEventEntity
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string AggregateType { get; set; } = string.Empty;
    public Guid AggregateId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string Status { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}

public sealed class DeadLetterEntity
{
    public long Id { get; set; }
    public Guid OutboxEventId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ResolutionNote { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}

public sealed class ReconciliationEntity
{
    public Guid Id { get; set; }
    public Guid ConnectorId { get; set; }
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DifferenceCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class IdempotencyRecordEntity
{
    public Guid Id { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public string ResponseType { get; set; } = string.Empty;
    public string ResponseId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

public sealed class AgentProfileEntity
{
    public Guid Id { get; set; }
    public Guid ServiceUserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public Guid BusinessOwnerUserId { get; set; }
    public Guid TechnicalOwnerUserId { get; set; }
    public string AutonomyLevel { get; set; } = string.Empty;
    public decimal AmountLimit { get; set; }
    public string RiskLimit { get; set; } = string.Empty;
    public string DataScopesJson { get; set; } = "[]";
    public string ModelScopesJson { get; set; } = "[]";
    public string AllowedToolsJson { get; set; } = "[]";
    public string RequiredConfirmationsJson { get; set; } = "[]";
    public string StopConditionsJson { get; set; } = "[]";
    public string LifecycleStatus { get; set; } = "Draft";
    public Guid? CurrentPublishedVersionId { get; set; }
    public long Version { get; set; } = 1;
    public long Revision { get; set; } = 1;
    public bool IsActive { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class AgentProfileVersionEntity
{
    public Guid Id { get; set; }
    public Guid AgentProfileId { get; set; }
    public int VersionNumber { get; set; }
    public long Revision { get; set; }
    public string Status { get; set; } = "Draft";
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ServiceUserId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid BusinessOwnerUserId { get; set; }
    public Guid TechnicalOwnerUserId { get; set; }
    public string AutonomyLevel { get; set; } = string.Empty;
    public decimal AmountLimit { get; set; }
    public string RiskLimit { get; set; } = string.Empty;
    public string DataScopesJson { get; set; } = "[]";
    public string ModelScopesJson { get; set; } = "[]";
    public string AllowedToolsJson { get; set; } = "[]";
    public string RequiredConfirmationsJson { get; set; } = "[]";
    public string StopConditionsJson { get; set; } = "[]";
    public string? ValidationJson { get; set; }
    public bool IsHighRisk { get; set; }
    public string HighRiskReasonsJson { get; set; } = "[]";
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? ValidatedBy { get; set; }
    public DateTimeOffset? ValidatedAt { get; set; }
    public Guid? PublishedBy { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? RetiredAt { get; set; }
}

public sealed class AgentRunEntity
{
    public Guid Id { get; set; }
    public Guid AgentProfileId { get; set; }
    public Guid RequestedBy { get; set; }
    public string Action { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? ConfirmationId { get; set; }
    public string? Model { get; set; }
    public string? SkillVersion { get; set; }
    public string? InputDigest { get; set; }
    public string? OutputDigest { get; set; }
    public decimal? Cost { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid DepartmentId { get; set; }
    public string EnvironmentCode { get; set; } = "demo";
    public bool IsTestData { get; set; } = true;
    public string? SubjectType { get; set; }
    public Guid? BusinessObjectId { get; set; }
    public Guid? WorkflowInstanceId { get; set; }
    public Guid? WorkflowTaskId { get; set; }
    public Guid? MinutesSourceId { get; set; }
    public Guid? GatewayCallId { get; set; }
    public string? GatewayMode { get; set; }
    public bool ExternalModelCalled { get; set; }
    public string? ProxyTriggerMode { get; set; }
    public DateTimeOffset? ProxyTriggeredAt { get; set; }
    public Guid? OriginalAssigneeId { get; set; }
    public string? CompensationStatus { get; set; }
    public string? ProxyIdempotencyKey { get; set; }
}

public sealed class AgentGatewayCallEntity
{
    public Guid Id { get; set; }
    public Guid AgentRunId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid DepartmentId { get; set; }
    public string EnvironmentCode { get; set; } = "demo";
    public string Model { get; set; } = string.Empty;
    public string Mode { get; set; } = "RuleSimulator";
    public string? GatewayCallId { get; set; }
    public bool ExternalModelCalled { get; set; }
    public string InputDigest { get; set; } = string.Empty;
    public string? OutputDigest { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int DurationMs { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class MinutesSourceEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid? AttachmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset MeetingAt { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string Transcript { get; set; } = string.Empty;
    public string EnvironmentCode { get; set; } = "demo";
    public bool IsTestData { get; set; } = true;
    public string Status { get; set; } = "Imported";
    public string AnalysisVersion { get; set; } = "none";
    public Guid? AnalysisRunId { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class MinutesActionItemEntity
{
    public Guid Id { get; set; }
    public Guid MinutesSourceId { get; set; }
    public string Speaker { get; set; } = string.Empty;
    public string SourceTimestamp { get; set; } = string.Empty;
    public string SourceExcerpt { get; set; } = string.Empty;
    public string OriginalContent { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid? SuggestedAssigneeId { get; set; }
    public Guid? AssigneeId { get; set; }
    public DateTimeOffset? SuggestedDueAt { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public string Priority { get; set; } = "P1";
    public Guid? SuggestedObjectTypeId { get; set; }
    public Guid? ObjectTypeId { get; set; }
    public Guid? SuggestedWorkflowVersionId { get; set; }
    public Guid? WorkflowVersionId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? BusinessObjectId { get; set; }
    public Guid? WorkflowInstanceId { get; set; }
    public string MissingInformation { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string AnalysisVersion { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public Guid? ConfirmedBy { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public Guid? MinutesTaskId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class MinutesTaskEntity
{
    public Guid Id { get; set; }
    public Guid MinutesSourceId { get; set; }
    public Guid ActionItemId { get; set; }
    public Guid? AttachmentId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid AssigneeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset? DueAt { get; set; }
    public string Priority { get; set; } = "P1";
    public Guid? BusinessObjectId { get; set; }
    public Guid? WorkflowInstanceId { get; set; }
    public Guid? ProjectId { get; set; }
    public string EnvironmentCode { get; set; } = "demo";
    public bool IsTestData { get; set; } = true;
    public string Status { get; set; } = "Pending";
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class AgentConfirmationEntity
{
    public Guid Id { get; set; }
    public Guid AgentRunId { get; set; }
    public Guid ConfirmedBy { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTimeOffset ConfirmedAt { get; set; }
}

public sealed class AttachmentEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid? BusinessObjectId { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public string VirusScanStatus { get; set; } = string.Empty;
    public string Sensitivity { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public string EnvironmentCode { get; set; } = "demo";
    public bool IsTestData { get; set; } = true;
    public string SourceSystem { get; set; } = "AIDT";
    public int CurrentVersionNumber { get; set; } = 1;
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? DeletionRequestedBy { get; set; }
    public DateTimeOffset? DeletionRequestedAt { get; set; }
    public string? DeletionReason { get; set; }
}

public sealed class DocumentVersionEntity
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public string VirusScanStatus { get; set; } = "NotConfigured";
    public string Sensitivity { get; set; } = "Internal";
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class AttachmentLinkEntity
{
    public Guid Id { get; set; }
    public Guid AttachmentId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class SignatureEntity
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public Guid SignedBy { get; set; }
    public string EvidenceReference { get; set; } = string.Empty;
    public DateTimeOffset SignedAt { get; set; }
}

public sealed class ConfigurationVersionEntity
{
    public Guid Id { get; set; }
    public string ConfigurationKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ConfigurationType { get; set; } = "Workflow";
    public Guid OrganizationId { get; set; }
    public Guid? DepartmentId { get; set; }
    public int VersionNumber { get; set; }
    public long Revision { get; set; }
    public string State { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public string RiskLevel { get; set; } = "Low";
    public bool RequiresPlatformApproval { get; set; }
    public string ApplicableRoleCodesJson { get; set; } = "[]";
    public string EnvironmentCode { get; set; } = "demo";
    public bool IsTestData { get; set; } = true;
    public string PayloadJson { get; set; } = "{}";
    public string? ImpactAnalysisJson { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? PublishedBy { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public Guid? PublishedWorkflowVersionId { get; set; }
}

public sealed class NavigationWorkspaceEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string WorkspaceKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string WorkspaceType { get; set; } = "DepartmentWorkspace";
    public bool IsSystem { get; set; }
    public bool IsDefault { get; set; }
    public bool IsDemoData { get; set; }
    public string EnvironmentCode { get; set; } = "demo";
    public bool IsTestData { get; set; } = true;
    public string Status { get; set; } = "Draft";
    public long Version { get; set; } = 1;
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? RetiredBy { get; set; }
    public DateTimeOffset? RetiredAt { get; set; }
}

public sealed class NavigationVersionEntity
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public int VersionNumber { get; set; }
    public string Status { get; set; } = "Draft";
    public long Revision { get; set; }
    public string? ImpactAnalysisJson { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? PublishedBy { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}

public sealed class NavigationItemEntity
{
    public Guid Id { get; set; }
    public Guid NavigationVersionId { get; set; }
    public string ItemKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string? RequiredResource { get; set; }
    public string? RequiredAction { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public bool IsSystem { get; set; }
    public string? Icon { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class NavigationUserPreferenceEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string WorkspaceKey { get; set; } = string.Empty;
    public string ItemKey { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }
    public bool IsHidden { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class WorkbenchTemplateEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AudienceTitle { get; set; } = string.Empty;
    public string PrimaryQuestion { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public bool IsDefault { get; set; }
    public string Status { get; set; } = "Draft";
    public int SortOrder { get; set; }
    public long Version { get; set; } = 1;
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? RetiredBy { get; set; }
    public DateTimeOffset? RetiredAt { get; set; }
}

public sealed class WorkbenchTemplateVersionEntity
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public int VersionNumber { get; set; }
    public string Status { get; set; } = "Draft";
    public long Revision { get; set; } = 1;
    public string? ValidationJson { get; set; }
    public string? ImpactAnalysisJson { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? PublishedBy { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}

public sealed class WorkbenchCardEntity
{
    public Guid Id { get; set; }
    public Guid TemplateVersionId { get; set; }
    public string CardType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public string Width { get; set; } = "Third";
    public string DataSourceType { get; set; } = "ExistingApi";
    public string FilterJson { get; set; } = "{}";
    public string RequiredResource { get; set; } = string.Empty;
    public string RequiredAction { get; set; } = "Read";
    public bool IsEnabled { get; set; } = true;
    public string CapabilityState { get; set; } = "Connected";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class WorkbenchAssignmentEntity
{
    public Guid Id { get; set; }
    public Guid TemplateVersionId { get; set; }
    public string AssignmentType { get; set; } = "Organization";
    public string ScopeValue { get; set; } = string.Empty;
    public int Priority { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ProjectProfileEntity
{
    public Guid BusinessObjectId { get; set; }
    public Guid OwnerUserId { get; set; }
    public DateTimeOffset? PlannedStartDate { get; set; }
    public DateTimeOffset? PlannedEndDate { get; set; }
    public string Objective { get; set; } = string.Empty;
    public string CurrentStage { get; set; } = string.Empty;
    public string ConfidentialityLevel { get; set; } = "Internal";
    public int ProgressPercent { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class ProjectMemberEntity
{
    public Guid BusinessObjectId { get; set; }
    public Guid UserId { get; set; }
    public string ProjectRole { get; set; } = "Contributor";
    public bool IsLead { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? RemovedBy { get; set; }
    public DateTimeOffset? RemovedAt { get; set; }
}

public sealed class ProjectMilestoneEntity
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid OwnerUserId { get; set; }
    public DateTimeOffset PlannedDate { get; set; }
    public string Status { get; set; } = "Planned";
    public DateTimeOffset? CompletedAt { get; set; }
    public long Version { get; set; } = 1;
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class ProjectRelationEntity
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string RelationCode { get; set; } = string.Empty;
    public string? EvidenceExcerpt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ProjectTaskEntity
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? EventInboxId { get; set; }
    public Guid AssigneeId { get; set; }
    public Guid DepartmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset? DueAt { get; set; }
    public string Priority { get; set; } = "P1";
    public string Status { get; set; } = "Pending";
    public long Version { get; set; } = 1;
    public Guid? BusinessObjectId { get; set; }
    public Guid? WorkflowInstanceId { get; set; }
    public Guid? WorkflowTaskId { get; set; }
    public string EnvironmentCode { get; set; } = "demo";
    public bool IsTestData { get; set; } = true;
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public sealed class EventInboxEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid DepartmentId { get; set; }
    public string SourceType { get; set; } = "ManualText";
    public Guid? SourceId { get; set; }
    public Guid? AttachmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string RawText { get; set; } = string.Empty;
    public string Status { get; set; } = "Received";
    public Guid SubmittedBy { get; set; }
    public Guid? AnalysisRunId { get; set; }
    public string EnvironmentCode { get; set; } = "demo";
    public bool IsTestData { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class EventSuggestionEntity
{
    public Guid Id { get; set; }
    public Guid EventInboxId { get; set; }
    public Guid AgentRunId { get; set; }
    public Guid? SuggestedProjectId { get; set; }
    public Guid? SuggestedBusinessObjectId { get; set; }
    public Guid? SuggestedAssigneeId { get; set; }
    public Guid? SuggestedWorkflowVersionId { get; set; }
    public DateTimeOffset? SuggestedDueAt { get; set; }
    public string Priority { get; set; } = "P1";
    public string RiskLevel { get; set; } = "Low";
    public bool RequiresHumanBoundary { get; set; }
    public string? BoundaryReason { get; set; }
    public string MissingInformation { get; set; } = string.Empty;
    public string EvidenceExcerpt { get; set; } = string.Empty;
    public string Status { get; set; } = "Suggested";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class McpClientEntity
{
    public Guid Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string? ClientSecretHash { get; set; }
    public string RedirectUrisJson { get; set; } = "[]";
    public string GrantTypesJson { get; set; } = "[]";
    public string ScopesJson { get; set; } = "[]";
    public string TokenEndpointAuthMethod { get; set; } = "none";
    public bool IsActive { get; set; } = true;
    public Guid? AgentProfileId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
}

public sealed class McpAuthorizationRequestEntity
{
    public Guid Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public string RedirectUri { get; set; } = string.Empty;
    public string RequestedScopesJson { get; set; } = "[]";
    public string State { get; set; } = string.Empty;
    public string CodeChallenge { get; set; } = string.Empty;
    public string CodeChallengeMethod { get; set; } = "S256";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class McpAuthorizationCodeEntity
{
    public Guid Id { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public string RedirectUri { get; set; } = string.Empty;
    public string GrantedScopesJson { get; set; } = "[]";
    public string DepartmentIdsJson { get; set; } = "[]";
    public string CodeChallenge { get; set; } = string.Empty;
    public string CodeChallengeMethod { get; set; } = "S256";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class McpAccessTokenEntity
{
    public Guid Id { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string TokenKind { get; set; } = "Access";
    public Guid TokenFamilyId { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? AgentProfileId { get; set; }
    public string GrantedScopesJson { get; set; } = "[]";
    public string DepartmentIdsJson { get; set; } = "[]";
    public Guid? ParentTokenId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid? RevokedBy { get; set; }
    public string? RevocationReason { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class McpHumanConfirmationEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? DecisionComment { get; set; }
    public Guid? ConfirmedBy { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
