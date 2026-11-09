using AIDT.Domain.Common;

namespace AIDT.Domain.Workflows;

public enum WorkflowInstanceStatus
{
    Running,
    Waiting,
    Completed,
    Returned,
    Terminated
}

public enum WorkflowTaskStatus
{
    Pending,
    InProgress,
    Approved,
    Returned,
    Cancelled
}

public enum WorkflowDecision
{
    Approve,
    Return,
    Terminate
}

public enum WorkflowAgentProxyMode
{
    RemindOnly,
    HumanConfirmedProxy,
    PreAuthorizedAutoProxy
}

public enum WorkflowAssignmentStrategy
{
    SpecificUser,
    BusinessRole,
    DepartmentManager,
    ProjectOwner,
    InitiatorManager,
    Agent
}

public sealed class WorkflowInstance
{
    private WorkflowInstance(
        Guid id,
        Guid workflowDefinitionId,
        Guid workflowVersionId,
        Guid businessObjectId,
        Guid departmentId,
        WorkflowInstanceStatus status,
        long version,
        string? currentNodeCode,
        DateTimeOffset startedAt,
        DateTimeOffset? endedAt,
        Guid? startedByUserId,
        string environmentCode,
        bool isTestData,
        string sourceSystem)
    {
        Id = id;
        WorkflowDefinitionId = workflowDefinitionId;
        WorkflowVersionId = workflowVersionId;
        BusinessObjectId = businessObjectId;
        DepartmentId = departmentId;
        Status = status;
        Version = version;
        CurrentNodeCode = currentNodeCode;
        StartedAt = startedAt;
        EndedAt = endedAt;
        StartedByUserId = startedByUserId;
        EnvironmentCode = environmentCode;
        IsTestData = isTestData;
        SourceSystem = sourceSystem;
    }

    public Guid Id { get; }

    public Guid WorkflowDefinitionId { get; }

    public Guid WorkflowVersionId { get; }

    public Guid BusinessObjectId { get; }

    public Guid DepartmentId { get; }

    public WorkflowInstanceStatus Status { get; private set; }

    public long Version { get; private set; }

    public string? CurrentNodeCode { get; private set; }

    public DateTimeOffset StartedAt { get; }

    public DateTimeOffset? EndedAt { get; private set; }

    public Guid? StartedByUserId { get; }

    public string EnvironmentCode { get; }

    public bool IsTestData { get; }

    public string SourceSystem { get; }

    public static WorkflowInstance Start(
        Guid workflowDefinitionId,
        Guid workflowVersionId,
        Guid businessObjectId,
        DateTimeOffset now,
        string? initialNodeCode = null,
        Guid departmentId = default,
        Guid? startedByUserId = null,
        string environmentCode = "demo",
        bool isTestData = true,
        string sourceSystem = "AIDT") =>
        new(
            Guid.NewGuid(),
            DomainChecks.RequiredId(workflowDefinitionId, "流程定义"),
            DomainChecks.RequiredId(workflowVersionId, "流程版本"),
            DomainChecks.RequiredId(businessObjectId, "业务对象"),
            DomainChecks.RequiredId(departmentId, "部门"),
            WorkflowInstanceStatus.Running,
            1,
            initialNodeCode is null ? null : DomainChecks.RequiredCode(initialNodeCode, "流程节点", 80),
            now.ToUniversalTime(),
            null,
            startedByUserId,
            DataEnvironment.Normalize(environmentCode),
            isTestData,
            sourceSystem);

    public static WorkflowInstance Restore(
        Guid id,
        Guid workflowDefinitionId,
        Guid workflowVersionId,
        Guid businessObjectId,
        WorkflowInstanceStatus status,
        long version,
        string? currentNodeCode,
        DateTimeOffset startedAt,
        DateTimeOffset? endedAt,
        Guid departmentId = default,
        Guid? startedByUserId = null,
        string environmentCode = "demo",
        bool isTestData = true,
        string sourceSystem = "AIDT") =>
        new(
            DomainChecks.RequiredId(id, "流程实例"),
            DomainChecks.RequiredId(workflowDefinitionId, "流程定义"),
            DomainChecks.RequiredId(workflowVersionId, "流程版本"),
            DomainChecks.RequiredId(businessObjectId, "业务对象"),
            departmentId,
            status,
            version,
            currentNodeCode,
            startedAt.ToUniversalTime(),
            endedAt?.ToUniversalTime(),
            startedByUserId,
            DataEnvironment.Normalize(environmentCode),
            isTestData,
            sourceSystem);

    public void MoveTo(string nodeCode, long expectedVersion)
    {
        EnsureVersion(expectedVersion);
        EnsureActive();
        CurrentNodeCode = DomainChecks.RequiredCode(nodeCode, "流程节点", 80);
        Version++;
    }

    public void Wait(long expectedVersion)
    {
        EnsureVersion(expectedVersion);
        EnsureActive();
        Status = WorkflowInstanceStatus.Waiting;
        Version++;
    }

    public void Complete(WorkflowDecision decision, long expectedVersion, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        EnsureActive();

        Status = decision switch
        {
            WorkflowDecision.Approve => WorkflowInstanceStatus.Completed,
            WorkflowDecision.Return => WorkflowInstanceStatus.Returned,
            WorkflowDecision.Terminate => WorkflowInstanceStatus.Terminated,
            _ => throw new ValidationException("不支持的流程决定。")
        };
        EndedAt = now.ToUniversalTime();
        Version++;
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (expectedVersion != Version)
        {
            throw new ConflictException(ErrorCodes.WorkflowVersionConflict, "流程实例版本已变化，请刷新后重试。");
        }
    }

    private void EnsureActive()
    {
        if (Status is WorkflowInstanceStatus.Completed or WorkflowInstanceStatus.Returned or WorkflowInstanceStatus.Terminated)
        {
            throw new ConflictException(ErrorCodes.WorkflowStateInvalid, "流程实例已经结束。");
        }
    }
}

public sealed class WorkflowTask
{
    private WorkflowTask(
        Guid id,
        Guid workflowInstanceId,
        string nodeCode,
        string nodeName,
        Guid assigneeId,
        Guid departmentId,
        WorkflowTaskStatus status,
        long version,
        string? decisionComment,
        DateTimeOffset createdAt,
        DateTimeOffset? completedAt,
        string environmentCode,
        bool isTestData,
        Guid? nodePolicyId = null,
        DateTimeOffset? dueAt = null,
        string slaStatus = "None",
        DateTimeOffset? reminderSentAt = null,
        DateTimeOffset? escalatedAt = null,
        string decisionMode = "Human",
        Guid? agentRunId = null,
        WorkflowAssignmentStrategy assigneeStrategy = WorkflowAssignmentStrategy.SpecificUser,
        Guid? businessRoleId = null,
        string? businessRoleCode = null,
        int? businessRoleVersion = null,
        IReadOnlyList<Guid>? candidateUserIds = null,
        string assigneeSnapshotJson = "{}",
        Guid? originalAssigneeId = null,
        Guid? backupAssigneeId = null,
        Guid? transferredBy = null,
        DateTimeOffset? transferredAt = null,
        string? transferReason = null,
        string formDefinitionJson = "[]",
        string formDataJson = "{}",
        string formSchemaVersion = "1.0",
        string formDefinitionHash = "")
    {
        Id = id;
        WorkflowInstanceId = workflowInstanceId;
        NodeCode = nodeCode;
        NodeName = nodeName;
        AssigneeId = assigneeId;
        DepartmentId = departmentId;
        Status = status;
        Version = version;
        DecisionComment = decisionComment;
        CreatedAt = createdAt;
        CompletedAt = completedAt;
        EnvironmentCode = environmentCode;
        IsTestData = isTestData;
        NodePolicyId = nodePolicyId;
        DueAt = dueAt;
        SlaStatus = slaStatus;
        ReminderSentAt = reminderSentAt;
        EscalatedAt = escalatedAt;
        DecisionMode = decisionMode;
        AgentRunId = agentRunId;
        AssigneeStrategy = assigneeStrategy;
        BusinessRoleId = businessRoleId;
        BusinessRoleCode = businessRoleCode;
        BusinessRoleVersion = businessRoleVersion;
        CandidateUserIds = candidateUserIds ?? [assigneeId];
        AssigneeSnapshotJson = assigneeSnapshotJson;
        OriginalAssigneeId = originalAssigneeId;
        BackupAssigneeId = backupAssigneeId;
        TransferredBy = transferredBy;
        TransferredAt = transferredAt;
        TransferReason = transferReason;
        FormDefinitionJson = formDefinitionJson;
        FormDataJson = formDataJson;
        FormSchemaVersion = formSchemaVersion;
        FormDefinitionHash = formDefinitionHash;
    }

    public Guid Id { get; }

    public Guid WorkflowInstanceId { get; }

    public string NodeCode { get; private set; }

    public string NodeName { get; private set; }

    public Guid AssigneeId { get; private set; }

    public Guid DepartmentId { get; }

    public WorkflowTaskStatus Status { get; private set; }

    public long Version { get; private set; }

    public string? DecisionComment { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public string EnvironmentCode { get; }

    public bool IsTestData { get; }

    public Guid? NodePolicyId { get; }
    public DateTimeOffset? DueAt { get; }
    public string SlaStatus { get; private set; }
    public DateTimeOffset? ReminderSentAt { get; private set; }
    public DateTimeOffset? EscalatedAt { get; private set; }
    public string DecisionMode { get; private set; }
    public Guid? AgentRunId { get; private set; }
    public WorkflowAssignmentStrategy AssigneeStrategy { get; private set; }
    public Guid? BusinessRoleId { get; private set; }
    public string? BusinessRoleCode { get; private set; }
    public int? BusinessRoleVersion { get; private set; }
    public IReadOnlyList<Guid> CandidateUserIds { get; private set; }
    public string AssigneeSnapshotJson { get; private set; }
    public Guid? OriginalAssigneeId { get; private set; }
    public Guid? BackupAssigneeId { get; private set; }
    public Guid? TransferredBy { get; private set; }
    public DateTimeOffset? TransferredAt { get; private set; }
    public string? TransferReason { get; private set; }
    public string FormDefinitionJson { get; private set; }
    public string FormDataJson { get; private set; }
    public string FormSchemaVersion { get; private set; }
    public string FormDefinitionHash { get; private set; }

    public static WorkflowTask Create(
        Guid workflowInstanceId,
        string nodeCode,
        Guid assigneeId,
        DateTimeOffset now,
        Guid departmentId = default,
        string environmentCode = "demo",
        bool isTestData = true,
        Guid? nodePolicyId = null,
        DateTimeOffset? dueAt = null,
        WorkflowAssignmentStrategy assigneeStrategy = WorkflowAssignmentStrategy.SpecificUser,
        Guid? businessRoleId = null,
        string? businessRoleCode = null,
        int? businessRoleVersion = null,
        IReadOnlyList<Guid>? candidateUserIds = null,
        string assigneeSnapshotJson = "{}",
        Guid? backupAssigneeId = null,
        Guid? originalAssigneeId = null,
        string? nodeName = null,
        string formDefinitionJson = "[]",
        string formDataJson = "{}",
        string formSchemaVersion = "1.0",
        string formDefinitionHash = "") =>
        new(
            Guid.NewGuid(),
            DomainChecks.RequiredId(workflowInstanceId, "流程实例"),
            DomainChecks.RequiredCode(nodeCode, "流程节点", 80),
            string.IsNullOrWhiteSpace(nodeName) ? nodeCode : nodeName.Trim(),
            DomainChecks.RequiredId(assigneeId, "任务处理人"),
            DomainChecks.RequiredId(departmentId, "部门"),
            WorkflowTaskStatus.Pending,
            1,
            null,
            now.ToUniversalTime(),
            null,
            DataEnvironment.Normalize(environmentCode),
            isTestData,
            nodePolicyId,
            dueAt,
            assigneeStrategy: assigneeStrategy,
            businessRoleId: businessRoleId,
            businessRoleCode: businessRoleCode,
            businessRoleVersion: businessRoleVersion,
            candidateUserIds: candidateUserIds,
            assigneeSnapshotJson: assigneeSnapshotJson,
            backupAssigneeId: backupAssigneeId,
            originalAssigneeId: originalAssigneeId,
            formDefinitionJson: formDefinitionJson,
            formDataJson: formDataJson,
            formSchemaVersion: formSchemaVersion,
            formDefinitionHash: formDefinitionHash);

    public static WorkflowTask Restore(
        Guid id,
        Guid workflowInstanceId,
        string nodeCode,
        Guid assigneeId,
        WorkflowTaskStatus status,
        long version,
        string? decisionComment,
        DateTimeOffset createdAt,
        DateTimeOffset? completedAt,
        Guid departmentId = default,
        string environmentCode = "demo",
        bool isTestData = true,
        Guid? nodePolicyId = null,
        DateTimeOffset? dueAt = null,
        string slaStatus = "None",
        DateTimeOffset? reminderSentAt = null,
        DateTimeOffset? escalatedAt = null,
        string decisionMode = "Human",
        Guid? agentRunId = null,
        WorkflowAssignmentStrategy assigneeStrategy = WorkflowAssignmentStrategy.SpecificUser,
        Guid? businessRoleId = null,
        string? businessRoleCode = null,
        int? businessRoleVersion = null,
        IReadOnlyList<Guid>? candidateUserIds = null,
        string assigneeSnapshotJson = "{}",
        Guid? originalAssigneeId = null,
        Guid? backupAssigneeId = null,
        Guid? transferredBy = null,
        DateTimeOffset? transferredAt = null,
        string? transferReason = null,
        string? nodeName = null,
        string formDefinitionJson = "[]",
        string formDataJson = "{}",
        string formSchemaVersion = "1.0",
        string formDefinitionHash = "") =>
        new(
            DomainChecks.RequiredId(id, "流程任务"),
            DomainChecks.RequiredId(workflowInstanceId, "流程实例"),
            DomainChecks.RequiredCode(nodeCode, "流程节点", 80),
            string.IsNullOrWhiteSpace(nodeName) ? nodeCode : nodeName.Trim(),
            DomainChecks.RequiredId(assigneeId, "任务处理人"),
            DomainChecks.RequiredId(departmentId, "部门"),
            status,
            version,
            decisionComment,
            createdAt.ToUniversalTime(),
            completedAt?.ToUniversalTime(),
            DataEnvironment.Normalize(environmentCode),
            isTestData,
            nodePolicyId,
            dueAt?.ToUniversalTime(),
            slaStatus,
            reminderSentAt?.ToUniversalTime(),
            escalatedAt?.ToUniversalTime(),
            decisionMode,
            agentRunId,
            assigneeStrategy,
            businessRoleId,
            businessRoleCode,
            businessRoleVersion,
            candidateUserIds,
            assigneeSnapshotJson,
            originalAssigneeId,
            backupAssigneeId,
            transferredBy,
            transferredAt?.ToUniversalTime(),
            transferReason,
            formDefinitionJson,
            formDataJson,
            formSchemaVersion,
            formDefinitionHash);

    public void SetFormData(string formDataJson)
    {
        if (Status != WorkflowTaskStatus.Pending)
        {
            throw new ConflictException(ErrorCodes.TaskAlreadyProcessed, "任务已经处理，不能修改表单。");
        }

        FormDataJson = string.IsNullOrWhiteSpace(formDataJson) ? "{}" : formDataJson;
    }

    public WorkflowDecision Decide(
        Guid actorId,
        WorkflowDecision decision,
        string? comment,
        long expectedVersion,
        DateTimeOffset now)
    {
        if (Status != WorkflowTaskStatus.Pending)
        {
            throw new ConflictException(ErrorCodes.TaskAlreadyProcessed, "任务已经处理，不能重复提交。");
        }

        if (actorId != AssigneeId)
        {
            throw new PermissionException(ErrorCodes.TaskAssigneeMismatch, "只有当前任务处理人可以处理该任务。");
        }

        if (expectedVersion != Version)
        {
            throw new ConflictException(ErrorCodes.WorkflowVersionConflict, "任务版本已变化，请刷新后重试。");
        }

        if (decision == WorkflowDecision.Return && string.IsNullOrWhiteSpace(comment))
        {
            throw new ValidationException("退回操作必须填写原因。");
        }

        Status = decision switch
        {
            WorkflowDecision.Approve => WorkflowTaskStatus.Approved,
            WorkflowDecision.Return => WorkflowTaskStatus.Returned,
            WorkflowDecision.Terminate => WorkflowTaskStatus.Cancelled,
            _ => throw new ValidationException("不支持的任务决定。")
        };
        DecisionComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        CompletedAt = now.ToUniversalTime();
        Version++;
        return decision;
    }

    public void DecideAsAgent(Guid agentServiceUserId, Guid agentRunId, string comment, DateTimeOffset now)
    {
        if (Status != WorkflowTaskStatus.Pending)
        {
            throw new ConflictException(ErrorCodes.TaskAlreadyProcessed, "任务已经处理，不能重复代理。");
        }

        if (agentServiceUserId == Guid.Empty || agentRunId == Guid.Empty)
        {
            throw new ValidationException("Agent 代理必须提供独立服务身份和 Agent 运行。");
        }

        Status = WorkflowTaskStatus.Approved;
        DecisionComment = DomainChecks.Required(comment, "Agent 代理说明", 2000);
        CompletedAt = now.ToUniversalTime();
        DecisionMode = "AgentProxy";
        AgentRunId = agentRunId;
        Version++;
    }

    public void MarkReminder(DateTimeOffset now)
    {
        if (ReminderSentAt is null)
        {
            ReminderSentAt = now.ToUniversalTime();
            SlaStatus = "Reminded";
        }
    }

    public void MarkEscalated(DateTimeOffset now)
    {
        if (EscalatedAt is null)
        {
            EscalatedAt = now.ToUniversalTime();
            SlaStatus = "Escalated";
        }
    }

    public void Transfer(
        Guid actorId,
        Guid targetAssigneeId,
        Guid? backupAssigneeId,
        string reason,
        long expectedVersion,
        DateTimeOffset now)
    {
        if (Status != WorkflowTaskStatus.Pending)
        {
            throw new ConflictException(ErrorCodes.TaskAlreadyProcessed, "任务已经处理，不能转交。");
        }

        if (expectedVersion != Version)
        {
            throw new ConflictException(ErrorCodes.WorkflowVersionConflict, "任务版本已变化，请刷新后重试。");
        }

        if (targetAssigneeId == Guid.Empty || targetAssigneeId == AssigneeId)
        {
            throw new ValidationException("转交目标必须是另一位有效责任人。");
        }

        OriginalAssigneeId ??= AssigneeId;
        BackupAssigneeId = backupAssigneeId;
        AssigneeId = DomainChecks.RequiredId(targetAssigneeId, "转交处理人");
        TransferredBy = DomainChecks.RequiredId(actorId, "转交操作人");
        TransferredAt = now.ToUniversalTime();
        TransferReason = DomainChecks.Required(reason, "转交原因", 500);
        Version++;
    }

    public void Escalate(
        Guid actorId,
        Guid targetAssigneeId,
        string reason,
        long expectedVersion,
        DateTimeOffset now)
    {
        if (Status != WorkflowTaskStatus.Pending)
        {
            throw new ConflictException(ErrorCodes.TaskAlreadyProcessed, "任务已经处理，不能升级。");
        }

        if (expectedVersion != Version)
        {
            throw new ConflictException(ErrorCodes.WorkflowVersionConflict, "任务版本已变化，请刷新后重试。");
        }

        if (targetAssigneeId == Guid.Empty || targetAssigneeId == AssigneeId)
        {
            throw new ValidationException("升级目标必须是另一位有效责任人。");
        }

        OriginalAssigneeId ??= AssigneeId;
        AssigneeId = DomainChecks.RequiredId(targetAssigneeId, "升级处理人");
        TransferredBy = DomainChecks.RequiredId(actorId, "升级操作人");
        TransferredAt = now.ToUniversalTime();
        TransferReason = DomainChecks.Required(reason, "升级原因", 500);
        SlaStatus = "Escalated";
        EscalatedAt ??= now.ToUniversalTime();
        Version++;
    }
}
