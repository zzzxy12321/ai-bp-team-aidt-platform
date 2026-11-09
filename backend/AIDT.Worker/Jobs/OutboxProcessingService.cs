using AIDT.Infrastructure.Persistence;
using AIDT.Workflow;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace AIDT.Worker.Jobs;

public sealed partial class OutboxProcessingService(
    IServiceScopeFactory scopeFactory,
    IConnectionMultiplexer redis,
    IConfiguration configuration,
    ILogger<OutboxProcessingService> logger) : BackgroundService
{
    private const int BatchSize = 20;
    private const int MaxAttempts = 5;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessBatchAsync(stoppingToken);
                if (processed == 0)
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogPollingFailed(logger, exception);
                await Task.Delay(PollInterval, stoppingToken);
            }
        }
    }

    private async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AidtDbContext>();
        var now = DateTimeOffset.UtcNow;
        var events = await dbContext.OutboxEvents
            .Where(item => item.Status == "Pending" && item.NextAttemptAt <= now)
            .OrderBy(item => item.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);
        if (events.Count == 0)
        {
            return 0;
        }

        var prefix = configuration.GetValue<string>("AIDT:Cache:KeyPrefix") ?? "aidt";
        var processed = 0;
        foreach (var outboxEvent in events)
        {
            var lockKey = $"{prefix}:outbox-lock:{outboxEvent.Id:D}";
            var lockValue = Guid.NewGuid().ToString("N");
            var acquired = false;
            try
            {
                acquired = await redis.GetDatabase().StringSetAsync(
                    lockKey,
                    lockValue,
                    TimeSpan.FromSeconds(30),
                    When.NotExists);
            }
            catch (RedisConnectionException exception)
            {
                LogGarnetUnavailable(logger, exception, outboxEvent.Id);
            }

            if (!acquired)
            {
                continue;
            }

            try
            {
                var recorder = scope.ServiceProvider.GetRequiredService<ElsaTransitionRecorder>();
                var transitionId = await recorder.RecordAsync(
                    outboxEvent.AggregateId,
                    outboxEvent.EventType,
                    "OutboxDelivered",
                    cancellationToken);
                outboxEvent.Status = "Delivered";
                outboxEvent.ProcessedAt = DateTimeOffset.UtcNow;
                processed++;
                LogDelivered(logger, outboxEvent.Id, transitionId);
            }
            catch (Exception exception)
            {
                outboxEvent.AttemptCount++;
                outboxEvent.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(Math.Pow(2, outboxEvent.AttemptCount));
                if (outboxEvent.AttemptCount >= MaxAttempts)
                {
                    outboxEvent.Status = "DeadLettered";
                    await dbContext.DeadLetters.AddAsync(
                        new DeadLetterEntity
                        {
                            OutboxEventId = outboxEvent.Id,
                            Reason = exception.GetBaseException().Message,
                            CreatedAt = DateTimeOffset.UtcNow
                        },
                        cancellationToken);
                }

                LogEventFailed(logger, exception, outboxEvent.Id);
            }
            finally
            {
                try
                {
                    var database = redis.GetDatabase();
                    var current = await database.StringGetAsync(lockKey);
                    if (current == lockValue)
                    {
                        await database.KeyDeleteAsync(lockKey);
                    }
                }
                catch (RedisException)
                {
                    // Lock expires automatically; this does not change the persistent outbox outcome.
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return processed;
    }

    [LoggerMessage(EventId = 2101, Level = LogLevel.Error, Message = "AIDT outbox polling failed.")]
    private static partial void LogPollingFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2102, Level = LogLevel.Warning, Message = "Garnet unavailable; outbox item will be retried. Id={OutboxId}")]
    private static partial void LogGarnetUnavailable(ILogger logger, RedisConnectionException exception, Guid outboxId);

    [LoggerMessage(EventId = 2103, Level = LogLevel.Information, Message = "Outbox event delivered to Elsa. OutboxId={OutboxId}, ElsaTransitionId={TransitionId}")]
    private static partial void LogDelivered(ILogger logger, Guid outboxId, string transitionId);

    [LoggerMessage(EventId = 2104, Level = LogLevel.Error, Message = "Outbox event handling failed. OutboxId={OutboxId}")]
    private static partial void LogEventFailed(ILogger logger, Exception exception, Guid outboxId);
}
