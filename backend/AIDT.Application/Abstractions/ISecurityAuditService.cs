namespace AIDT.Application.Abstractions;

public interface ISecurityAuditService
{
    Task RecordDeniedRequestAsync(
        Guid actorId,
        string method,
        string path,
        string traceId,
        string code,
        string reason,
        CancellationToken cancellationToken);
}
