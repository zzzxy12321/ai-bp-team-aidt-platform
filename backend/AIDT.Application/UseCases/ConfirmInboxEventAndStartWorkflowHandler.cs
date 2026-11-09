using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;

namespace AIDT.Application.UseCases;

public sealed class ConfirmInboxEventAndStartWorkflowHandler(
    ICurrentUserAccessor currentUserAccessor,
    IAtomicEventInboxConfirmationService inboxConfirmationService,
    StartWorkflowInstanceHandler startWorkflowInstanceHandler,
    IUnitOfWork unitOfWork)
{
    public async Task<InboxConfirmationDto> HandleAsync(
        Guid eventId,
        ConfirmInboxEventRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = currentUserAccessor.GetRequiredUser();
        user.EnsureCan("event-inbox", AccessAction.Confirm);
        ValidateRequest(request);

        var existing = await inboxConfirmationService.GetIdempotentConfirmationAsync(
            user,
            eventId,
            request,
            idempotencyKey,
            traceId,
            cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            StartWorkflowInstanceHandler.StartedWorkflowResult? startedWorkflow = null;
            if (request.WorkflowVersionId.HasValue)
            {
                startedWorkflow = await startWorkflowInstanceHandler.StageWithinTransactionAsync(
                    new StartWorkflowInstanceRequest(
                        request.WorkflowDefinitionId!.Value,
                        request.WorkflowVersionId.Value,
                        request.ProjectId!.Value,
                        IdempotencyKey: null),
                    user,
                    traceId,
                    requestHash: null,
                    idempotencyScope: null,
                    persistIdempotency: false,
                    cancellationToken);
            }

            var effectiveRequest = startedWorkflow is null
                ? request
                : request with
                {
                    WorkflowInstanceId = Guid.Parse(startedWorkflow.Instance.Id)
                };
            var result = await inboxConfirmationService.ConfirmWithinTransactionAsync(
                user,
                eventId,
                effectiveRequest,
                idempotencyKey,
                traceId,
                startedWorkflow?.FirstTaskId,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result with { Workflow = startedWorkflow?.Instance ?? result.Workflow };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static void ValidateRequest(ConfirmInboxEventRequest request)
    {
        if (!request.ProjectId.HasValue || request.ProjectId.Value == Guid.Empty)
        {
            throw new ValidationException("最终确认前必须选择项目。");
        }

        if (!request.AssigneeId.HasValue || request.AssigneeId.Value == Guid.Empty)
        {
            throw new ValidationException("最终确认前必须选择负责人。");
        }

        if (!request.DueAt.HasValue)
        {
            throw new ValidationException("最终确认前必须选择截止时间。");
        }

        if (request.WorkflowInstanceId.HasValue
            && (request.WorkflowDefinitionId.HasValue || request.WorkflowVersionId.HasValue))
        {
            throw new ValidationException("关联已有流程和发起新流程不能同时选择。");
        }

        if (request.WorkflowDefinitionId.HasValue != request.WorkflowVersionId.HasValue)
        {
            throw new ValidationException("发起新流程必须同时提供流程定义和版本。");
        }
    }
}
