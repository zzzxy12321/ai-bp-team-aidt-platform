using System.Text.Json;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Audit;
using AIDT.Infrastructure.Persistence;

namespace AIDT.Infrastructure.Services;

public sealed class SqlMcpAuditService(
    AidtDbContext dbContext,
    IClock clock) : IMcpAuditService
{
    public async Task RecordAsync(McpCallAudit audit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(audit);
        var outcome = audit.Succeeded ? AuditOutcome.Succeeded :
            audit.Code == ErrorCodesForAudit.HumanConfirmationRequired
                ? AuditOutcome.Denied
                : AuditOutcome.Failed;
        await dbContext.AuditEvents.AddAsync(
            new AuditEventEntity
            {
                Id = Guid.NewGuid(),
                ActorId = audit.ActorId,
                Module = "MCP",
                Action = audit.Operation,
                EntityType = audit.ResourceType,
                EntityId = string.IsNullOrWhiteSpace(audit.ResourceId) ? audit.ToolOrResource : audit.ResourceId,
                TraceId = audit.TraceId,
                Outcome = outcome.ToString(),
                AfterJson = JsonSerializer.Serialize(new
                {
                    audit.ClientId,
                    audit.TokenId,
                    audit.ToolOrResource,
                    audit.Scopes,
                    audit.DepartmentIds,
                    audit.IdempotencyKey,
                    audit.AgentRunId,
                    audit.DurationMs,
                    audit.RequestHash,
                    audit.ResponseHash
                }),
                Reason = audit.Code,
                OccurredAt = clock.UtcNow
            },
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordSecurityEventAsync(
        Guid actorId,
        string action,
        string entityType,
        string entityId,
        string traceId,
        string outcome,
        string? reason,
        string? metadataJson,
        CancellationToken cancellationToken)
    {
        await dbContext.AuditEvents.AddAsync(
            new AuditEventEntity
            {
                Id = Guid.NewGuid(),
                ActorId = actorId,
                Module = "MCP",
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                TraceId = traceId,
                Outcome = outcome,
                AfterJson = metadataJson,
                Reason = reason,
                OccurredAt = clock.UtcNow
            },
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static class ErrorCodesForAudit
    {
        public const string HumanConfirmationRequired = "HUMAN_CONFIRMATION_REQUIRED";
    }
}
