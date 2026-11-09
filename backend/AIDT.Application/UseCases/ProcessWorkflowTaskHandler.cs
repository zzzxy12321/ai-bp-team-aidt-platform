using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Application.Validation;
using AIDT.Domain.Audit;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;
using AIDT.Domain.Integrations;
using AIDT.Domain.Workflows;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AIDT.Application.UseCases;

public sealed class ProcessWorkflowTaskHandler(
    ICurrentUserAccessor currentUserAccessor,
    IWorkflowRepository workflowRepository,
    IBusinessObjectRepository businessObjectRepository,
    IDepartmentRepository departmentRepository,
    IWorkflowEngine workflowEngine,
    IAuditRepository auditRepository,
    IOutboxRepository outboxRepository,
    IIdempotencyStore idempotencyStore,
    IWorkflowTaskFormValidator workflowTaskFormValidator,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<WorkflowTaskDto> HandleAsync(
        Guid taskId,
        TaskDecisionRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        RequestValidation.Validate(request);

        var user = currentUserAccessor.GetRequiredUser();
        var action = request.Decision switch
        {
            WorkflowDecision.Approve => AccessAction.Approve,
            WorkflowDecision.Return => AccessAction.Return,
            WorkflowDecision.Terminate => AccessAction.Terminate,
            _ => throw new ValidationException("不支持的流程动作。")
        };
        user.EnsureCan("workflow-task", action);

        var task = await workflowRepository.GetTaskAsync(taskId, cancellationToken)
            ?? throw new NotFoundException("流程任务不存在。");
        if (user.IsServiceIdentity)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "服务身份不能代替真人调用任务审批接口。");
        }

        user.EnsureDepartmentAccess(task.DepartmentId);
        if (task.AssigneeId != user.UserId)
        {
            throw new PermissionException(ErrorCodes.TaskAssigneeMismatch, "只有当前任务处理人可以处理该任务。");
        }

        if (task.CandidateUserIds.Count > 0
            && !task.CandidateUserIds.Contains(user.UserId))
        {
            throw new PermissionException(
                ErrorCodes.PermissionDenied,
                "当前身份不在任务冻结的业务角色处理人资格快照中。");
        }
        if (HasFormFields(task.FormDefinitionJson)
            && !string.IsNullOrWhiteSpace(task.FormDefinitionHash)
            && !string.Equals(
                task.FormDefinitionHash,
                request.FormDefinitionHash,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException(
                ErrorCodes.WorkflowFormDefinitionChanged,
                "节点字段定义已变化，请刷新页面。");
        }
        var formValidation = await workflowTaskFormValidator.ValidateAsync(
            user,
            task,
            request.FormDataJson,
            enforceRequired: true,
            cancellationToken);
        if (!formValidation.IsValid)
        {
            throw new ValidationException(string.Join("；", formValidation.Errors));
        }

        var requestHash = HashRequest(request);
        var idempotencyScope = $"workflow-task:{taskId:D}:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(
            idempotencyScope,
            request.IdempotencyKey!,
            cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同请求。");
            }

            return task.ToDto();
        }

        var instance = await workflowRepository.GetInstanceAsync(task.WorkflowInstanceId, cancellationToken)
            ?? throw new NotFoundException("流程实例不存在。");
        var businessObject = await businessObjectRepository.GetByIdAsync(
            instance.BusinessObjectId,
            cancellationToken) ?? throw new NotFoundException("流程业务对象不存在。");

        var now = clock.UtcNow;
        task.SetFormData(formValidation.NormalizedJson);
        task.Decide(user.UserId, request.Decision, request.Comment, request.ExpectedVersion, now);
        var businessObjectChanged = false;

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (request.Decision is WorkflowDecision.Return or WorkflowDecision.Terminate)
            {
                instance.Complete(request.Decision, instance.Version, now);
                businessObject.Suspend(businessObject.Version, now);
                businessObjectChanged = true;
            }
            else
            {
                var advance = await workflowEngine.AdvanceAsync(
                    instance,
                    task,
                    request.Decision,
                    cancellationToken);
                if (advance.NextNodeCode is not null
                    && advance.NextAssigneeId is not null
                    && advance.Assignment is not null)
                {
                    var assignment = advance.Assignment;
                    var nextTaskDepartmentId = assignment.Strategy == WorkflowAssignmentStrategy.Agent
                        ? instance.DepartmentId
                        : await departmentRepository.ResolveAssigneeDepartmentAsync(
                            user.OrganizationId,
                            assignment.PrimaryAssigneeId,
                            cancellationToken);
                    instance.MoveTo(advance.NextNodeCode, instance.Version);
                    var nextTask = WorkflowTask.Create(
                            instance.Id,
                            advance.NextNodeCode,
                            assignment.PrimaryAssigneeId,
                            now,
                            nextTaskDepartmentId,
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
                                ? instance.StartedByUserId ?? user.UserId
                                : null);
                    await workflowRepository.AddTaskAsync(nextTask, cancellationToken);
                    await auditRepository.AddAsync(
                        AuditEvent.Record(
                            user.UserId,
                            "Workflow",
                            "workflow.assignment_resolved",
                            nameof(WorkflowTask),
                            nextTask.Id.ToString("D"),
                            traceId,
                            AuditOutcome.Succeeded,
                            now,
                            after: assignment.SnapshotJson),
                        cancellationToken);
                }
                else if (advance.InstanceStatus != instance.Status)
                {
                    instance.Complete(WorkflowDecision.Approve, instance.Version, now);
                }
            }

            if (businessObjectChanged)
            {
                await businessObjectRepository.UpdateAsync(
                    businessObject,
                    user.UserId,
                    $"WorkflowTask:{request.Decision}",
                    cancellationToken);
            }
            await workflowRepository.SaveTaskAsync(task, cancellationToken);
            await workflowRepository.SaveInstanceAsync(instance, cancellationToken);
            await workflowRepository.AddTaskActionAsync(
                task.Id,
                user.UserId,
                request.Decision.ToString(),
                request.Comment,
                now,
                cancellationToken);
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Workflow",
                    request.Decision.ToString(),
                    nameof(WorkflowTask),
                    task.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    before: WorkflowTaskStatus.Pending.ToString(),
                    after: task.Status.ToString(),
                    reason: request.Comment),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    idempotencyScope,
                    request.IdempotencyKey!,
                    requestHash,
                    nameof(WorkflowTask),
                    task.Id.ToString("D"),
                    "completed",
                    now,
                    now.AddDays(30)),
                cancellationToken);
            await outboxRepository.AddAsync(
                OutboxMessage.Create(
                    $"WorkflowTask.{task.Status}",
                    nameof(WorkflowTask),
                    task.Id,
                    request.IdempotencyKey!,
                    $$"""{"taskId":"{{task.Id:D}}","instanceId":"{{instance.Id:D}}","decision":"{{request.Decision}}"}""",
                    now),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return task.ToDto();
    }

    private static string HashRequest(TaskDecisionRequest request)
    {
        var canonical = new
        {
            request.Decision,
            request.ExpectedVersion,
            Comment = request.Comment?.Trim(),
            IdempotencyKey = request.IdempotencyKey?.Trim(),
            FormDataJson = request.FormDataJson?.Trim(),
            request.FormDefinitionHash
        };
        var payload = JsonSerializer.Serialize(canonical, JsonSerializerOptions.Web);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    private static bool HasFormFields(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Array
                && document.RootElement.GetArrayLength() > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
