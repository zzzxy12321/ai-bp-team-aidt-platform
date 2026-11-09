namespace AIDT.Infrastructure.Persistence;

public sealed class ScenarioDefinitionEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid OwnerDepartmentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LifecycleStatus { get; set; } = "Draft";
    public Guid? CurrentPublishedVersionId { get; set; }
    public long Version { get; set; } = 1;
    public long Revision { get; set; } = 1;
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? DisabledBy { get; set; }
    public DateTimeOffset? DisabledAt { get; set; }
    public string? DisabledReason { get; set; }
}

public sealed class ScenarioVersionEntity
{
    public Guid Id { get; set; }
    public Guid ScenarioId { get; set; }
    public int VersionNumber { get; set; }
    public string Status { get; set; } = "Draft";
    public long Revision { get; set; } = 1;
    public string PayloadJson { get; set; } = "{}";
    public string? ReferenceSnapshotJson { get; set; }
    public string? ValidationJson { get; set; }
    public string? ImpactAnalysisJson { get; set; }
    public string? ChangeSummary { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? ValidatedBy { get; set; }
    public DateTimeOffset? ValidatedAt { get; set; }
    public Guid? PublishedBy { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public Guid? RetiredBy { get; set; }
    public DateTimeOffset? RetiredAt { get; set; }
}

public sealed class ScenarioLaunchEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ScenarioId { get; set; }
    public Guid ScenarioVersionId { get; set; }
    public int ScenarioVersionNumber { get; set; }
    public Guid DepartmentId { get; set; }
    public string EnvironmentCode { get; set; } = "demo";
    public bool IsTestData { get; set; } = true;
    public Guid StartedBy { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public Guid? BusinessObjectId { get; set; }
    public Guid? WorkflowInstanceId { get; set; }
    public Guid? WorkflowTaskId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? ProjectTaskId { get; set; }
    public Guid? EventInboxId { get; set; }
    public Guid? AgentRunId { get; set; }
    public string AttachmentIdsJson { get; set; } = "[]";
    public string MinutesSourceIdsJson { get; set; } = "[]";
    public string BusinessLinksJson { get; set; } = "[]";
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
}
