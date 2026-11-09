using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Audit;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;

namespace AIDT.Application.UseCases;

public sealed class ConfirmAgentRunHandler(
    ICurrentUserAccessor currentUserAccessor,
    IAgentRepository agentRepository,
    IAuditRepository auditRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<AgentRunDto> HandleAsync(
        Guid runId,
        ConfirmAgentRunRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = currentUserAccessor.GetRequiredUser();
        user.EnsureCan("agent-run", AccessAction.Approve);
        if (user.IsServiceIdentity)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "数字员工不能确认自己的运行。");
        }

        var run = await agentRepository.GetRunAsync(runId, cancellationToken)
            ?? throw new NotFoundException("Agent 运行记录不存在。");
        var now = clock.UtcNow;
        run.Confirm(request.ConfirmationId, !user.IsServiceIdentity);

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await agentRepository.SaveRunAsync(run, cancellationToken);
            await agentRepository.AddConfirmationAsync(
                run.Id,
                user.UserId,
                request.ConfirmationId,
                "Approved",
                null,
                now,
                cancellationToken);
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Agent",
                    "Confirm",
                    "AgentRun",
                    run.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    before: "AwaitingConfirmation",
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
