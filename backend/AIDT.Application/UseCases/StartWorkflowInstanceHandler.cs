using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Application.Validation;
using AIDT.Domain.Audit;
using AIDT.Domain.BusinessObjects;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;
using AIDT.Domain.Integrations;
using AIDT.Domain.Workflows;

namespace AIDT.Application.UseCases;

public sealed class StartWorkflowInstanceHandler(
    ICurrentUserAccessor currentUserAccessor,
    IBusinessObjectRepository businessObjectRepository,
    IDepartmentRepository departmentRepository,
    IWorkflowCatalogRepository workflowCatalogRepository,
    IWorkflowRepository workflowRepository,
    IWorkflowEngine workflowEngine,
    IAuditRepository auditRepository,
    IOutboxRepository outboxRepository,
    IIdempotencyStore idempotencyStore,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public sealed record StartedWorkflowResult(WorkflowInstanceDto Instance, Guid FirstTaskId);

    public async Task<WorkflowInstanceDto> HandleAsync(
        StartWorkflowInstanceRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        RequestValidation.Validate(request);
        var user = currentUserAccessor.GetRequiredUser();
        user.EnsureCan("workflow-instance", AccessAction.Submit);
        var requestHash = HashRequest(request);
        var scope = $"workflow-instance:start:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, request.IdempotencyKey!, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同流程发起请求。");
            }

            return (await workflowRepository.GetInstanceAsync(
                Guid.Parse(existing.ResponseId),
                cancellationToken) ?? throw new NotFoundException("幂等结果对应的流程实例不存在。")).ToDto();
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await StageWithinTransactionAsync(
                request,
                user,
                traceId,
                requestHash,
                scope,
                persistIdempotency: true,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result.Instance;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<StartedWorkflowResult> StageWithinTransactionAsync(
        StartWorkflowInstanceRequest request,
        UserPrincipal user,
        string traceId,
        string? requestHash,
        string? idempotencyScope,
        bool persistIdempotency,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("workflow-instance", AccessAction.Submit);
        var businessObject = await businessObjectRepository.GetByIdAsync(request.BusinessObjectId, cancellationToken)
            ?? throw new NotFoundException("业务对象不存在。");
        if (businessObject.OrganizationId != user.OrganizationId)
        {
            throw new PermissionException(ErrorCodes.DataScopeDenied, "当前身份不能为该业务对象发起流程。");
        }
        user.EnsureDepartmentAccess(businessObject.DepartmentId);

        var definition = await workflowCatalogRepository.GetVersionAsync(
            request.WorkflowVersionId,
            cancellationToken) ?? throw new NotFoundException("流程版本不存在。");
        if (definition.WorkflowDefinitionId != request.WorkflowDefinitionId)
        {
            throw new ValidationException("流程定义与流程版本不匹配。");
        }

        var plan = await workflowEngine.StartAsync(
            definition,
            businessObject,
            cancellationToken,
            user.UserId);
        var assignment = plan.Assignment
            ?? throw new ValidationException("流程节点处理人解析结果缺失。");
        var firstTaskDepartmentId = assignment.Strategy == WorkflowAssignmentStrategy.Agent
            ? businessObject.DepartmentId
            : await departmentRepository.ResolveAssigneeDepartmentAsync(
                user.OrganizationId,
                plan.FirstAssigneeId,
                cancellationToken);
        var now = clock.UtcNow;
        var businessObjectChanged = false;
        if (businessObject.Status == BusinessObjectStatus.Draft)
        {
            businessObject.Activate(businessObject.Version, now);
            businessObjectChanged = true;
        }
        else if (businessObject.Status != BusinessObjectStatus.Active)
        {
            throw new ConflictException(ErrorCodes.WorkflowStateInvalid, "当前业务对象状态不能发起流程。");
        }
        var instance = WorkflowInstance.Start(
            request.WorkflowDefinitionId,
            request.WorkflowVersionId,
            request.BusinessObjectId,
            now,
            plan.FirstNodeCode,
            businessObject.DepartmentId,
            user.UserId,
            businessObject.EnvironmentCode,
            businessObject.IsTestData,
            businessObject.SourceSystem);
        var task = WorkflowTask.Create(
            instance.Id,
            plan.FirstNodeCode,
            plan.FirstAssigneeId,
            now,
            firstTaskDepartmentId,
            instance.EnvironmentCode,
            instance.IsTestData,
            assigneeStrategy: assignment.Strategy,
            businessRoleId: assignment.BusinessRoleId,
            businessRoleCode: assignment.BusinessRoleCode,
            businessRoleVersion: assignment.BusinessRoleVersion,
            candidateUserIds: assignment.CandidateUserIds,
            assigneeSnapshotJson: assignment.SnapshotJson,
            backupAssigneeId: assignment.BackupAssigneeId,
            originalAssigneeId: assignment.Strategy == WorkflowAssignmentStrategy.Agent
                ? user.UserId
                : null);

        await workflowRepository.AddInstanceAsync(instance, cancellationToken);
        await workflowRepository.AddTaskAsync(task, cancellationToken);
        if (businessObjectChanged)
        {
            await businessObjectRepository.UpdateAsync(
                businessObject,
                user.UserId,
                "WorkflowStarted",
                cancellationToken);
        }

        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Workflow",
                "Start",
                nameof(WorkflowInstance),
                instance.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                after: $"{{\"status\":\"Running\",\"node\":\"{plan.FirstNodeCode}\"}}"),
            cancellationToken);
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Workflow",
                "workflow.assignment_resolved",
                nameof(WorkflowTask),
                task.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                after: assignment.SnapshotJson),
            cancellationToken);
        await outboxRepository.AddAsync(
            OutboxMessage.Create(
                "WorkflowInstance.Started",
                nameof(WorkflowInstance),
                instance.Id,
                $"workflow-instance-started:{instance.Id:D}",
                JsonSerializer.Serialize(
                    new { workflowInstanceId = instance.Id, taskId = task.Id, businessObjectId = businessObject.Id },
                    JsonSerializerOptions.Web),
                now),
            cancellationToken);
        if (persistIdempotency)
        {
            if (string.IsNullOrWhiteSpace(requestHash) || string.IsNullOrWhiteSpace(idempotencyScope)
                || string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                throw new InvalidOperationException("流程启动幂等参数缺失。");
            }

            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    idempotencyScope,
                    request.IdempotencyKey!,
                    requestHash,
                    nameof(WorkflowInstance),
                    instance.Id.ToString("D"),
                    "completed",
                    now,
                    now.AddDays(30)),
                cancellationToken);
        }

        return new StartedWorkflowResult(instance.ToDto(), task.Id);
    }

    private static string HashRequest(StartWorkflowInstanceRequest request)
    {
        var canonical = new
        {
            request.WorkflowDefinitionId,
            request.WorkflowVersionId,
            request.BusinessObjectId,
            IdempotencyKey = request.IdempotencyKey?.Trim()
        };
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(canonical, JsonSerializerOptions.Web))));
    }
}
