using AIDT.Application.Abstractions;
using AIDT.Domain.Audit;
using AIDT.Domain.Integrations;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Persistence.Repositories;

public sealed class SqlAuditRepository(AidtDbContext dbContext) : IAuditRepository
{
    public Task AddAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        return dbContext.AuditEvents.AddAsync(
            new AuditEventEntity
            {
                Id = auditEvent.Id,
                ActorId = auditEvent.ActorId,
                Module = auditEvent.Module,
                Action = auditEvent.Action,
                EntityType = auditEvent.EntityType,
                EntityId = auditEvent.EntityId,
                TraceId = auditEvent.TraceId,
                Outcome = auditEvent.Outcome.ToString(),
                BeforeJson = auditEvent.Before,
                AfterJson = auditEvent.After,
                Reason = auditEvent.Reason,
                OccurredAt = auditEvent.OccurredAt
            },
            cancellationToken).AsTask();
    }
}

public sealed class SqlOutboxRepository(AidtDbContext dbContext) : IOutboxRepository
{
    public Task AddAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        return dbContext.OutboxEvents.AddAsync(
            new OutboxEventEntity
            {
                Id = message.Id,
                EventType = message.EventType,
                AggregateType = message.AggregateType,
                AggregateId = message.AggregateId,
                IdempotencyKey = message.IdempotencyKey,
                PayloadJson = message.Payload,
                Status = message.Status.ToString(),
                AttemptCount = message.AttemptCount,
                NextAttemptAt = message.CreatedAt,
                CreatedAt = message.CreatedAt
            },
            cancellationToken).AsTask();
    }
}

public sealed class SqlIdempotencyStore(AidtDbContext dbContext) : IIdempotencyStore
{
    public async Task<IdempotencyEntry?> FindAsync(
        string scope,
        string key,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.IdempotencyRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Scope == scope && item.IdempotencyKey == key,
                cancellationToken);
        return entity is null
            ? null
            : new IdempotencyEntry(
                entity.Id,
                entity.Scope,
                entity.IdempotencyKey,
                entity.RequestHash,
                entity.ResponseType,
                entity.ResponseId,
                entity.Status,
                entity.CreatedAt,
                entity.ExpiresAt);
    }

    public Task AddAsync(IdempotencyEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return dbContext.IdempotencyRecords.AddAsync(
            new IdempotencyRecordEntity
            {
                Id = entry.Id,
                Scope = entry.Scope,
                IdempotencyKey = entry.IdempotencyKey,
                RequestHash = entry.RequestHash,
                ResponseType = entry.ResponseType,
                ResponseId = entry.ResponseId,
                Status = entry.Status,
                CreatedAt = entry.CreatedAt,
                ExpiresAt = entry.ExpiresAt
            },
            cancellationToken).AsTask();
    }
}
