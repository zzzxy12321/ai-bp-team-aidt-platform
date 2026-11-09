using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Audit;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;
using AIDT.Domain.Workflows;
using AIDT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Services;

public sealed class SqlWorkflowTaskAssignmentService(
    AidtDbContext dbContext,
    IAuditRepository auditRepository,
    IIdempotencyStore idempotencyStore,
    IUnitOfWork unitOfWork,
    IClock clock) : IWorkflowTaskAssignmentService
{
    public Task<WorkflowTaskDto> TransferAsync(
        UserPrincipal user,
        Guid taskId,
        TransferWorkflowTaskRequest request,
        string traceId,
        CancellationToken cancellationToken) =>
        ChangeAssigneeAsync(
            user,
            taskId,
            request.TargetUserId,
            request.ExpectedVersion,
            request.Reason,
            request.IdempotencyKey,
            false,
            traceId,
            cancellationToken);

    public async Task<WorkflowTaskDto> EscalateAsync(
        UserPrincipal user,
        Guid taskId,
        EscalateWorkflowTaskRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("workflow-task", AccessAction.Update);
        ValidateIdempotencyKey(request.IdempotencyKey);
        var task = await dbContext.WorkflowTasks.SingleOrDefaultAsync(
            item => item.Id == taskId,
            cancellationToken) ?? throw new NotFoundException("流程任务不存在。");
        user.EnsureDepartmentAccess(task.DepartmentId);
        var scope = $"workflow-task:escalate:{taskId:D}:{user.UserId:D}";
        var hash = Hash(new { taskId, request.ExpectedVersion, request.Reason });
        var existing = await idempotencyStore.FindAsync(scope, request.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.RequestHash != hash)
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同升级请求。");
            }

            return Map(task);
        }

        var policy = task.NodePolicyId.HasValue
            ? await dbContext.WorkflowNodePolicies.AsNoTracking().SingleOrDefaultAsync(
                item => item.Id == task.NodePolicyId.Value,
                cancellationToken)
            : null;
        var targetId = task.BackupAssigneeId
            ?? policy?.BackupApproverUserId
            ?? await dbContext.Departments.AsNoTracking()
                .Where(item => item.Id == task.DepartmentId)
                .Select(item => item.ManagerUserId)
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ConflictException(ErrorCodes.Conflict, "任务没有可用的备份处理人或部门负责人。");
        if (targetId == task.AssigneeId)
        {
            throw new ConflictException(ErrorCodes.Conflict, "升级目标与当前处理人相同。");
        }
        await EnsureTargetCanHandleAsync(user, task, targetId, cancellationToken);

        return await ApplyChangeAsync(
            user,
            task,
            targetId,
            request.ExpectedVersion,
            request.Reason,
            request.IdempotencyKey,
            scope,
            hash,
            true,
            traceId,
            cancellationToken);
    }

    private async Task<WorkflowTaskDto> ChangeAssigneeAsync(
        UserPrincipal user,
        Guid taskId,
        Guid targetUserId,
        long expectedVersion,
        string reason,
        string idempotencyKey,
        bool escalate,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("workflow-task", AccessAction.Update);
        ValidateIdempotencyKey(idempotencyKey);
        var task = await dbContext.WorkflowTasks.SingleOrDefaultAsync(
            item => item.Id == taskId,
            cancellationToken) ?? throw new NotFoundException("流程任务不存在。");
        user.EnsureDepartmentAccess(task.DepartmentId);
        if (!user.IsPlatformAdmin && task.AssigneeId != user.UserId)
        {
            throw new PermissionException(ErrorCodes.TaskAssigneeMismatch, "只有当前处理人或平台管理员可以转交任务。");
        }

        var policy = task.NodePolicyId.HasValue
            ? await dbContext.WorkflowNodePolicies.AsNoTracking().SingleOrDefaultAsync(
                item => item.Id == task.NodePolicyId.Value,
                cancellationToken)
            : null;
        if (policy is not null && !policy.AllowTransfer && !user.IsPlatformAdmin)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "该节点未授权人工转交。");
        }

        var target = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == targetUserId
                && item.OrganizationId == user.OrganizationId
                && item.IsActive
                && !item.IsServiceIdentity,
            cancellationToken) ?? throw new ValidationException("转交目标不存在、已停用或属于服务身份。");
        await EnsureTargetCanHandleAsync(user, task, target.Id, cancellationToken);
        var scope = $"workflow-task:transfer:{taskId:D}:{user.UserId:D}";
        var hash = Hash(new { taskId, targetUserId, expectedVersion, reason });
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.RequestHash != hash)
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同转交请求。");
            }

            return Map(task);
        }

        return await ApplyChangeAsync(
            user,
            task,
            targetUserId,
            expectedVersion,
            reason,
            idempotencyKey,
            scope,
            hash,
            escalate,
            traceId,
            cancellationToken);
    }

    private async Task<WorkflowTaskDto> ApplyChangeAsync(
        UserPrincipal user,
        WorkflowTaskEntity task,
        Guid targetUserId,
        long expectedVersion,
        string reason,
        string idempotencyKey,
        string scope,
        string hash,
        bool escalate,
        string traceId,
        CancellationToken cancellationToken)
    {
        if (task.Version != expectedVersion)
        {
            throw new ConflictException(ErrorCodes.WorkflowVersionConflict, "任务版本已变化，请刷新后重试。");
        }

        var now = clock.UtcNow;
        var originalAssigneeId = task.OriginalAssigneeId ?? task.AssigneeId;
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            task.OriginalAssigneeId = originalAssigneeId;
            task.AssigneeId = targetUserId;
            var candidateIds = (JsonSerializer.Deserialize<Guid[]>(task.CandidateUserIdsJson) ?? [])
                .Append(targetUserId)
                .Distinct()
                .ToArray();
            task.CandidateUserIdsJson = JsonSerializer.Serialize(candidateIds);
            task.TransferredBy = user.UserId;
            task.TransferredAt = now;
            task.TransferReason = DomainChecks.Required(reason, "转交/升级原因", 500);
            if (escalate)
            {
                task.SlaStatus = "Escalated";
                task.EscalatedAt ??= now;
            }

            task.Version++;
            dbContext.WorkflowTaskActions.Add(new WorkflowTaskActionEntity
            {
                WorkflowTaskId = task.Id,
                ActorId = user.UserId,
                Action = escalate ? "Escalate" : "Transfer",
                Comment = task.TransferReason,
                CreatedAt = now
            });
            var candidateExists = await dbContext.WorkflowTaskCandidates.AnyAsync(
                item => item.WorkflowTaskId == task.Id && item.UserId == targetUserId,
                cancellationToken);
            if (!candidateExists)
            {
                dbContext.WorkflowTaskCandidates.Add(new WorkflowTaskCandidateEntity
                {
                    WorkflowTaskId = task.Id,
                    UserId = targetUserId
                });
            }
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Workflow",
                    escalate ? "task.escalated" : "task.transferred",
                    nameof(WorkflowTaskEntity),
                    task.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    before: $"assignee={originalAssigneeId:D}",
                    after: $"assignee={targetUserId:D}",
                    reason: task.TransferReason),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    nameof(WorkflowTaskEntity),
                    task.Id.ToString("D"),
                    "completed",
                    now,
                    now.AddDays(30)),
                cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return Map(task);
    }

    private static WorkflowTaskDto Map(WorkflowTaskEntity item) =>
        new(
            item.Id.ToString("D"),
            item.WorkflowInstanceId.ToString("D"),
            item.NodeCode,
            item.AssigneeId.ToString("D"),
            Enum.Parse<WorkflowTaskStatus>(item.Status),
            item.Version,
            item.DecisionComment,
            item.CreatedAt,
            item.CompletedAt,
            item.DepartmentId.ToString("D"),
            item.EnvironmentCode,
            item.IsTestData,
            item.DueAt,
            item.SlaStatus,
            item.ReminderSentAt,
            item.EscalatedAt,
            item.DecisionMode,
            item.AgentRunId?.ToString("D"),
            item.AssigneeStrategy,
            item.BusinessRoleCode,
            item.BusinessRoleVersion,
            JsonSerializer.Deserialize<string[]>(item.CandidateUserIdsJson) ?? [],
            item.OriginalAssigneeId?.ToString("D"),
            item.BackupAssigneeId?.ToString("D"),
            item.TransferredBy?.ToString("D"),
            item.TransferredAt,
            item.TransferReason,
            item.NodeName,
            item.FormDefinitionJson,
            item.FormDataJson,
            item.FormSchemaVersion,
            item.FormDefinitionHash);

    private async Task EnsureTargetCanHandleAsync(
        UserPrincipal actor,
        WorkflowTaskEntity task,
        Guid targetUserId,
        CancellationToken cancellationToken)
    {
        if (string.Equals(task.AssigneeStrategy, nameof(WorkflowAssignmentStrategy.BusinessRole), StringComparison.Ordinal))
        {
            var frozenCandidateIds = JsonSerializer.Deserialize<Guid[]>(task.CandidateUserIdsJson) ?? [];
            if (!frozenCandidateIds.Contains(targetUserId))
            {
                throw new PermissionException(
                    ErrorCodes.PermissionDenied,
                    "转交目标不在任务冻结的业务角色候选资格中。");
            }
        }

        var hasDepartmentEligibility = await dbContext.UserDepartmentMemberships.AsNoTracking().AnyAsync(
                item => item.UserId == targetUserId
                    && item.DepartmentId == task.DepartmentId
                    && item.IsActive,
                cancellationToken)
            || await dbContext.Departments.AsNoTracking().AnyAsync(
                item => item.Id == task.DepartmentId && item.ManagerUserId == targetUserId,
                cancellationToken)
            || await dbContext.DataScopes.AsNoTracking().AnyAsync(
                item => item.UserId == targetUserId
                    && (item.ScopeType == "organization"
                            && item.ScopeReference == actor.OrganizationId.ToString("D")
                        || item.ScopeType == "department"
                            && item.ScopeReference == task.DepartmentId.ToString("D")),
                cancellationToken);
        if (!hasDepartmentEligibility)
        {
            throw new PermissionException(
                ErrorCodes.DepartmentScopeDenied,
                "转交目标没有任务所属部门的有效处理范围。");
        }
    }

    private static string Hash<T>(T value) =>
        Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    JsonSerializer.Serialize(value, JsonSerializerOptions.Web))));

    private static void ValidateIdempotencyKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length is < 8 or > 128)
        {
            throw new ValidationException("幂等键必须为 8—128 位。");
        }
    }
}
