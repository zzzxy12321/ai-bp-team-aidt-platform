using System.Text.Json;
using AIDT.Domain.Integrations;
using AIDT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Worker.Jobs;

public sealed partial class WorkerHeartbeatJob(AidtDbContext dbContext, ILogger<WorkerHeartbeatJob> logger)
{
    private static readonly Guid HeartbeatId = Guid.Parse("71000000-0000-0000-0000-000000000001");
    private static readonly Guid ServiceUserId = Guid.Parse("10000000-0000-0000-0000-000000000019");

    public async Task ExecuteAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var entity = await dbContext.ConfigurationVersions.SingleOrDefaultAsync(item => item.Id == HeartbeatId);
        if (entity is null)
        {
            var organizationId = await dbContext.Users.AsNoTracking()
                .Where(item => item.Id == ServiceUserId)
                .Select(item => item.OrganizationId)
                .SingleAsync();
            entity = new ConfigurationVersionEntity
            {
                Id = HeartbeatId,
                ConfigurationKey = "SYSTEM.WORKER.HEARTBEAT",
                Name = "Worker heartbeat",
                Description = "Hangfire worker liveness heartbeat.",
                ConfigurationType = "System",
                OrganizationId = organizationId,
                VersionNumber = 1,
                State = "Active",
                CreatedBy = ServiceUserId,
                CreatedAt = now,
                UpdatedAt = now
            };
            await dbContext.ConfigurationVersions.AddAsync(entity);
        }

        entity.PayloadJson = JsonSerializer.Serialize(
            new
            {
                status = "healthy",
                utc = now,
                worker = Environment.MachineName,
                processId = Environment.ProcessId
            },
            JsonSerializerOptions.Web);
        entity.UpdatedAt = now;
        entity.PublishedAt = now;
        await dbContext.SaveChangesAsync();
        LogHeartbeat(logger, now);
    }

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "AIDT worker heartbeat persisted. Utc={Utc}")]
    private static partial void LogHeartbeat(ILogger logger, DateTimeOffset utc);
}
