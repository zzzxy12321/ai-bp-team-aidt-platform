using AIDT.Application.Abstractions;
using AIDT.Domain.Workflows;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Persistence.Repositories;

public sealed class SqlWorkflowRepository(AidtDbContext dbContext) : IWorkflowRepository
{
    private readonly Dictionary<Guid, WorkflowInstanceEntity> _instances = [];
    private readonly Dictionary<Guid, WorkflowTaskEntity> _tasks = [];

    public async Task<WorkflowInstance?> GetInstanceAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.WorkflowInstances
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        _instances[id] = entity;
        return Map(entity);
    }

    public async Task<WorkflowTask?> GetTaskAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.WorkflowTasks
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        _tasks[id] = entity;
        return Map(entity);
    }

    public async Task AddInstanceAsync(WorkflowInstance instance, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instance);
        var entity = new WorkflowInstanceEntity
        {
            Id = instance.Id,
            WorkflowDefinitionId = instance.WorkflowDefinitionId,
            WorkflowVersionId = instance.WorkflowVersionId,
            BusinessObjectId = instance.BusinessObjectId,
            DepartmentId = instance.DepartmentId,
            StartedByUserId = instance.StartedByUserId,
            EnvironmentCode = instance.EnvironmentCode,
            IsTestData = instance.IsTestData,
            SourceSystem = instance.SourceSystem,
            Status = instance.Status.ToString(),
            Version = instance.Version,
            CurrentNodeCode = instance.CurrentNodeCode,
            StartedAt = instance.StartedAt,
            EndedAt = instance.EndedAt
        };
        await dbContext.WorkflowInstances.AddAsync(entity, cancellationToken);
        _instances[instance.Id] = entity;
    }

    public async Task AddTaskAsync(WorkflowTask task, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);
        var instance = dbContext.WorkflowInstances.Local.SingleOrDefault(
                item => item.Id == task.WorkflowInstanceId)
            ?? await dbContext.WorkflowInstances.AsNoTracking()
                .SingleAsync(item => item.Id == task.WorkflowInstanceId, cancellationToken);
        var policy = await dbContext.WorkflowNodePolicies.AsNoTracking()
            .Where(item => item.WorkflowVersionId == instance.WorkflowVersionId
                && item.NodeCode == task.NodeCode
                && item.Status == "Published")
            .OrderByDescending(item => item.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);
        var dueAt = task.DueAt
            ?? (policy?.SlaMinutes is int minutes ? task.CreatedAt.AddMinutes(minutes) : null);
        var formDefinitionJson = policy?.FormSchemaJson ?? task.FormDefinitionJson;
        var entity = new WorkflowTaskEntity
        {
            Id = task.Id,
            WorkflowInstanceId = task.WorkflowInstanceId,
            NodeCode = task.NodeCode,
            NodeName = policy?.NodeName ?? task.NodeName,
            AssigneeId = task.AssigneeId,
            DepartmentId = task.DepartmentId,
            EnvironmentCode = task.EnvironmentCode,
            IsTestData = task.IsTestData,
            Status = task.Status.ToString(),
            Version = task.Version,
            DecisionComment = task.DecisionComment,
            CreatedAt = task.CreatedAt,
            CompletedAt = task.CompletedAt,
            NodePolicyId = policy?.Id ?? task.NodePolicyId,
            FormDefinitionJson = formDefinitionJson,
            FormDataJson = task.FormDataJson,
            FormSchemaVersion = policy?.FormSchemaVersion ?? task.FormSchemaVersion,
            FormDefinitionHash = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(formDefinitionJson))),
            DueAt = dueAt,
            SlaStatus = dueAt is null ? "None" : "OnTrack",
            DecisionMode = task.DecisionMode,
            AgentRunId = task.AgentRunId,
            AssigneeStrategy = task.AssigneeStrategy.ToString(),
            BusinessRoleId = task.BusinessRoleId,
            BusinessRoleCode = task.BusinessRoleCode,
            BusinessRoleVersion = task.BusinessRoleVersion,
            CandidateUserIdsJson = JsonStorage.Serialize(task.CandidateUserIds.Distinct().ToArray()),
            AssigneeSnapshotJson = task.AssigneeSnapshotJson,
            OriginalAssigneeId = task.OriginalAssigneeId,
            BackupAssigneeId = task.BackupAssigneeId,
            TransferredBy = task.TransferredBy,
            TransferredAt = task.TransferredAt,
            TransferReason = task.TransferReason
        };
        await dbContext.WorkflowTasks.AddAsync(entity, cancellationToken);
        foreach (var candidateId in task.CandidateUserIds.Append(task.AssigneeId).Distinct())
        {
            await dbContext.WorkflowTaskCandidates.AddAsync(
                new WorkflowTaskCandidateEntity
                {
                    WorkflowTaskId = task.Id,
                    UserId = candidateId
                },
                cancellationToken);
        }
        _tasks[task.Id] = entity;
    }

    public async Task SaveInstanceAsync(WorkflowInstance instance, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instance);
        var entity = await GetTrackedInstanceAsync(instance.Id, cancellationToken);
        entity.Status = instance.Status.ToString();
        entity.Version = instance.Version;
        entity.CurrentNodeCode = instance.CurrentNodeCode;
        entity.EndedAt = instance.EndedAt;
    }

    public async Task SaveTaskAsync(WorkflowTask task, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);
        var entity = await GetTrackedTaskAsync(task.Id, cancellationToken);
        entity.Status = task.Status.ToString();
        entity.Version = task.Version;
        entity.DecisionComment = task.DecisionComment;
        entity.CompletedAt = task.CompletedAt;
        entity.SlaStatus = task.SlaStatus;
        entity.ReminderSentAt = task.ReminderSentAt;
        entity.EscalatedAt = task.EscalatedAt;
        entity.DecisionMode = task.DecisionMode;
        entity.AgentRunId = task.AgentRunId;
        entity.AssigneeId = task.AssigneeId;
        entity.OriginalAssigneeId = task.OriginalAssigneeId;
        entity.BackupAssigneeId = task.BackupAssigneeId;
        entity.TransferredBy = task.TransferredBy;
        entity.TransferredAt = task.TransferredAt;
        entity.TransferReason = task.TransferReason;
        entity.FormDataJson = task.FormDataJson;
    }

    public Task AddTaskActionAsync(
        Guid taskId,
        Guid actorId,
        string action,
        string? comment,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken) =>
        dbContext.WorkflowTaskActions.AddAsync(
            new WorkflowTaskActionEntity
            {
                WorkflowTaskId = taskId,
                ActorId = actorId,
                Action = action,
                Comment = comment,
                CreatedAt = occurredAt
            },
            cancellationToken).AsTask();

    private async Task<WorkflowInstanceEntity> GetTrackedInstanceAsync(Guid id, CancellationToken cancellationToken)
    {
        if (_instances.TryGetValue(id, out var entity))
        {
            return entity;
        }

        entity = await dbContext.WorkflowInstances.SingleAsync(item => item.Id == id, cancellationToken);
        _instances[id] = entity;
        return entity;
    }

    private async Task<WorkflowTaskEntity> GetTrackedTaskAsync(Guid id, CancellationToken cancellationToken)
    {
        if (_tasks.TryGetValue(id, out var entity))
        {
            return entity;
        }

        entity = await dbContext.WorkflowTasks.SingleAsync(item => item.Id == id, cancellationToken);
        _tasks[id] = entity;
        return entity;
    }

    private static WorkflowInstance Map(WorkflowInstanceEntity entity) =>
        WorkflowInstance.Restore(
            entity.Id,
            entity.WorkflowDefinitionId,
            entity.WorkflowVersionId,
            entity.BusinessObjectId,
            Enum.Parse<WorkflowInstanceStatus>(entity.Status),
            entity.Version,
            entity.CurrentNodeCode,
            entity.StartedAt,
            entity.EndedAt,
            entity.DepartmentId,
            entity.StartedByUserId,
            entity.EnvironmentCode,
            entity.IsTestData,
            entity.SourceSystem);

    private static WorkflowTask Map(WorkflowTaskEntity entity) =>
        WorkflowTask.Restore(
            entity.Id,
            entity.WorkflowInstanceId,
            entity.NodeCode,
            entity.AssigneeId,
            Enum.Parse<WorkflowTaskStatus>(entity.Status),
            entity.Version,
            entity.DecisionComment,
            entity.CreatedAt,
            entity.CompletedAt,
            entity.DepartmentId,
            entity.EnvironmentCode,
            entity.IsTestData,
            entity.NodePolicyId,
            entity.DueAt,
            entity.SlaStatus,
            entity.ReminderSentAt,
            entity.EscalatedAt,
            entity.DecisionMode,
            entity.AgentRunId,
            Enum.TryParse<WorkflowAssignmentStrategy>(entity.AssigneeStrategy, true, out var strategy)
                ? strategy
                : WorkflowAssignmentStrategy.SpecificUser,
            entity.BusinessRoleId,
            entity.BusinessRoleCode,
            entity.BusinessRoleVersion,
            JsonStorage.Deserialize<Guid[]>(entity.CandidateUserIdsJson) ?? [entity.AssigneeId],
            entity.AssigneeSnapshotJson,
            entity.OriginalAssigneeId,
            entity.BackupAssigneeId,
            entity.TransferredBy,
            entity.TransferredAt,
            entity.TransferReason,
            entity.NodeName,
            entity.FormDefinitionJson,
            entity.FormDataJson,
            entity.FormSchemaVersion,
            entity.FormDefinitionHash);
}
