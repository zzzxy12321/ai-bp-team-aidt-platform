using AIDT.Domain.Common;

namespace AIDT.Domain.Audit;

public enum AuditOutcome
{
    Succeeded,
    Denied,
    Failed
}

public sealed class AuditEvent
{
    private AuditEvent(
        Guid id,
        Guid actorId,
        string module,
        string action,
        string entityType,
        string entityId,
        string traceId,
        AuditOutcome outcome,
        string? before,
        string? after,
        string? reason,
        DateTimeOffset occurredAt)
    {
        Id = id;
        ActorId = actorId;
        Module = module;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        TraceId = traceId;
        Outcome = outcome;
        Before = before;
        After = after;
        Reason = reason;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; }

    public Guid ActorId { get; }

    public string Module { get; }

    public string Action { get; }

    public string EntityType { get; }

    public string EntityId { get; }

    public string TraceId { get; }

    public AuditOutcome Outcome { get; }

    public string? Before { get; }

    public string? After { get; }

    public string? Reason { get; }

    public DateTimeOffset OccurredAt { get; }

    public static AuditEvent Record(
        Guid actorId,
        string module,
        string action,
        string entityType,
        string entityId,
        string traceId,
        AuditOutcome outcome,
        DateTimeOffset now,
        string? before = null,
        string? after = null,
        string? reason = null) =>
        new(
            Guid.NewGuid(),
            DomainChecks.RequiredId(actorId, "审计操作人"),
            DomainChecks.Required(module, "审计模块", 64),
            DomainChecks.Required(action, "审计动作", 64),
            DomainChecks.Required(entityType, "审计实体类型", 128),
            DomainChecks.Required(entityId, "审计实体标识", 128),
            DomainChecks.Required(traceId, "TraceId", 128),
            outcome,
            before,
            after,
            reason,
            now.ToUniversalTime());
}
