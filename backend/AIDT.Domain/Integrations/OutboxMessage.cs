using System.Text.Json;
using AIDT.Domain.Common;

namespace AIDT.Domain.Integrations;

public enum OutboxMessageStatus
{
    Pending,
    Delivered,
    DeadLettered
}

public sealed class OutboxMessage
{
    private OutboxMessage(
        Guid id,
        string eventType,
        string aggregateType,
        Guid aggregateId,
        string idempotencyKey,
        string payload,
        OutboxMessageStatus status,
        DateTimeOffset createdAt)
    {
        Id = id;
        EventType = eventType;
        AggregateType = aggregateType;
        AggregateId = aggregateId;
        IdempotencyKey = idempotencyKey;
        Payload = payload;
        Status = status;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public string EventType { get; }

    public string AggregateType { get; }

    public Guid AggregateId { get; }

    public string IdempotencyKey { get; }

    public string Payload { get; }

    public OutboxMessageStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public static OutboxMessage Create(
        string eventType,
        string aggregateType,
        Guid aggregateId,
        string idempotencyKey,
        string payload,
        DateTimeOffset now)
    {
        if (!IsValidJson(payload))
        {
            throw new ValidationException("Outbox Payload 必须是有效 JSON。");
        }

        return new OutboxMessage(
            Guid.NewGuid(),
            DomainChecks.Required(eventType, "Outbox 事件类型", 128),
            DomainChecks.Required(aggregateType, "Outbox 聚合类型", 128),
            DomainChecks.RequiredId(aggregateId, "Outbox 聚合"),
            DomainChecks.Required(idempotencyKey, "幂等键", 128),
            payload,
            OutboxMessageStatus.Pending,
            now.ToUniversalTime());
    }

    public void MarkDelivered() => Status = OutboxMessageStatus.Delivered;

    public void RecordFailure(bool deadLetter)
    {
        AttemptCount++;
        if (deadLetter)
        {
            Status = OutboxMessageStatus.DeadLettered;
        }
    }

    private static bool IsValidJson(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
