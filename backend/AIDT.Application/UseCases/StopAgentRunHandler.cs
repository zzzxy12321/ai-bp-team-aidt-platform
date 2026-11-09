using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Audit;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;

namespace AIDT.Application.UseCases;

public sealed class StopAgentRunHandler(
    ICurrentUserAccessor currentUserAccessor,
    IAgentRepository agentRepository,
    IAuditRepository auditRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<AgentRunDto> HandleAsync(
        Guid runId,
        string traceId,
        CancellationToken cancellationToken)
    {
        var user = currentUserAccessor.GetRequiredUser();
        user.EnsureCan("agent-run", AccessAction.Stop);
        var run = await agentRepository.GetRunAsync(runId, cancellationToken)
            ?? throw new NotFoundException("Agent 运行记录不存在。");
        if (run.RequestedBy != user.UserId && !user.Roles.Contains("platform-admin", StringComparer.Ordinal))
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "只能停止自己发起或由管理员管理的 Agent 运行。");
        }

        var now = clock.UtcNow;
        run.Stop();
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await agentRepository.SaveRunAsync(run, cancellationToken);
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Agent",
                    "StopRun",
                    "AgentRun",
                    run.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    before: "RunningOrAwaiting",
                    after: run.Status.ToString()),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return run.ToDto();
    }
}
