using AIDT.Application.Abstractions;

namespace AIDT.Infrastructure.Services;

internal sealed class NotReadySecurityAuditService : ISecurityAuditService
{
    public Task RecordDeniedRequestAsync(
        Guid actorId,
        string method,
        string path,
        string traceId,
        string code,
        string reason,
        CancellationToken cancellationToken) => throw NotReady.Error();
}
