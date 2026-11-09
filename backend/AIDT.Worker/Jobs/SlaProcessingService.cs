using AIDT.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AIDT.Worker.Jobs;

public sealed partial class SlaProcessingService(
    IServiceScopeFactory scopeFactory,
    ILogger<SlaProcessingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<ISlaProcessor>();
                var result = await processor.ProcessAsync(
                    $"worker-sla-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
                    stoppingToken);
                if (result.Reminded + result.Escalated + result.AgentAssistProposed + result.AgentProxied > 0)
                {
                    LogCompleted(
                        logger,
                        result.Reminded,
                        result.Escalated,
                        result.AgentAssistProposed,
                        result.AgentProxied);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogFailed(logger, exception);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    [LoggerMessage(
        EventId = 2201,
        Level = LogLevel.Information,
        Message = "SLA scan completed. Reminded={Reminded}, Escalated={Escalated}, AgentAssist={AgentAssist}, AgentProxied={AgentProxied}")]
    private static partial void LogCompleted(
        ILogger logger,
        int reminded,
        int escalated,
        int agentAssist,
        int agentProxied);

    [LoggerMessage(EventId = 2202, Level = LogLevel.Error, Message = "SLA scan failed; retrying after delay.")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
