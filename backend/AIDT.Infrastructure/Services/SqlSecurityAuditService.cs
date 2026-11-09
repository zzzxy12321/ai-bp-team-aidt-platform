using System.Text.Json;
using AIDT.Application.Abstractions;
using AIDT.Domain.Audit;
using AIDT.Domain.Common;
using AIDT.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace AIDT.Infrastructure.Services;

public sealed class SqlSecurityAuditService(
    IServiceScopeFactory scopeFactory,
    IClock clock) : ISecurityAuditService
{
    public async Task RecordDeniedRequestAsync(
        Guid actorId,
        string method,
        string path,
        string traceId,
        string code,
        string reason,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var auditRepository = scope.ServiceProvider.GetRequiredService<IAuditRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AidtDbContext>();
        var now = clock.UtcNow;
        await auditRepository.AddAsync(
            AuditEvent.Record(
                actorId,
                "Security",
                "request.denied",
                "HttpRequest",
                path.Length <= 128 ? path : path[..128],
                traceId,
                AuditOutcome.Denied,
                now,
                after: JsonSerializer.Serialize(
                    new { method, path, code },
                    JsonSerializerOptions.Web),
                reason: reason),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
