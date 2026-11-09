using AIDT.Domain.Common;

namespace AIDT.Domain.Agents;

public enum AgentAutonomyLevel
{
    A0,
    A1,
    A2,
    A3,
    A4
}

public enum AgentRiskLevel
{
    Low,
    Medium,
    High
}

public enum AgentActionType
{
    Read,
    Draft,
    Submit,
    WriteBack,
    Payment,
    QualityRelease,
    EquipmentControl,
    FormulaChange,
    SafetyInterlock,
    ContractCommitment,
    CustomerCompensation
}

public enum AgentRunStatus
{
    AwaitingConfirmation,
    Running,
    Completed,
    Stopped,
    Failed
}

public sealed class AgentProfile
{
    private readonly HashSet<string> _allowedTools;

    private AgentProfile(
        Guid id,
        Guid serviceUserId,
        string name,
        AgentAutonomyLevel autonomyLevel,
        decimal amountLimit,
        AgentRiskLevel riskLimit,
        bool isActive,
        IEnumerable<string> allowedTools,
        string? code,
        Guid departmentId,
        IEnumerable<string> dataScopes,
        IEnumerable<string> modelScopes,
        IEnumerable<string> requiredConfirmations,
        IEnumerable<string> stopConditions,
        AgentProfileLifecycleStatus lifecycleStatus,
        Guid? currentPublishedVersionId)
    {
        Id = id;
        ServiceUserId = serviceUserId;
        Name = name;
        Code = code ?? id.ToString("N").ToUpperInvariant();
        AutonomyLevel = autonomyLevel;
        AmountLimit = amountLimit;
        RiskLimit = riskLimit;
        IsActive = isActive;
        DepartmentId = departmentId;
        DataScopes = dataScopes.ToArray();
        ModelScopes = modelScopes.ToArray();
        RequiredConfirmations = requiredConfirmations.ToArray();
        StopConditions = stopConditions.ToArray();
        LifecycleStatus = lifecycleStatus;
        CurrentPublishedVersionId = currentPublishedVersionId;
        _allowedTools = new HashSet<string>(allowedTools, StringComparer.OrdinalIgnoreCase);
    }

    public Guid Id { get; }

    public Guid ServiceUserId { get; }

    public string Name { get; }

    public string Code { get; }

    public AgentAutonomyLevel AutonomyLevel { get; }

    public decimal AmountLimit { get; }

    public AgentRiskLevel RiskLimit { get; }

    public bool IsActive { get; private set; }

    public Guid DepartmentId { get; }

    public IReadOnlyList<string> DataScopes { get; }

    public IReadOnlyList<string> ModelScopes { get; }

    public IReadOnlyList<string> RequiredConfirmations { get; }

    public IReadOnlyList<string> StopConditions { get; }

    public AgentProfileLifecycleStatus LifecycleStatus { get; }

    public Guid? CurrentPublishedVersionId { get; }

    public IReadOnlySet<string> AllowedTools => _allowedTools;

    public bool AllowsTool(string toolCode) =>
        _allowedTools.Contains(toolCode);

    public bool AllowsModel(string? model) =>
        string.IsNullOrWhiteSpace(model)
        || ModelScopes.Contains(model, StringComparer.OrdinalIgnoreCase);

    public bool AllowsDataScope(Guid departmentId) =>
        DataScopes.Any(scope =>
            scope.StartsWith("department:", StringComparison.OrdinalIgnoreCase)
            && Guid.TryParse(scope["department:".Length..], out var scopedDepartmentId)
            && scopedDepartmentId == departmentId);

    public static AgentProfile Create(
        Guid serviceUserId,
        string name,
        AgentAutonomyLevel autonomyLevel,
        decimal amountLimit,
        AgentRiskLevel riskLimit,
        IEnumerable<string> allowedTools)
    {
        if (amountLimit < 0)
        {
            throw new ValidationException("Agent 金额限额不能为负数。");
        }

        var tools = allowedTools.Select(tool => DomainChecks.Required(tool, "Agent 工具", 128)).ToArray();
        if (tools.Length == 0)
        {
            throw new ValidationException("Agent 至少需要一个允许工具。");
        }

        return new AgentProfile(
            Guid.NewGuid(),
            serviceUserId,
            DomainChecks.Required(name, "Agent 名称", 128),
            autonomyLevel,
            amountLimit,
            riskLimit,
            true,
            tools,
            null,
            Guid.NewGuid(),
            [$"department:{Guid.NewGuid():D}"],
            AgentCapabilityRegistry.AllowedModelScopes,
            AgentCapabilityRegistry.PermanentHumanBoundaries,
            ["manual-stop", "risk-limit", "amount-limit", "unauthorized-tool"],
            AgentProfileLifecycleStatus.Published,
            Guid.NewGuid());
    }

    public static AgentProfile Create(
        string name,
        AgentAutonomyLevel autonomyLevel,
        decimal amountLimit,
        AgentRiskLevel riskLimit,
        IEnumerable<string> allowedTools) =>
        Create(Guid.Empty, name, autonomyLevel, amountLimit, riskLimit, allowedTools);

    public static AgentProfile Restore(
        Guid id,
        Guid serviceUserId,
        string name,
        AgentAutonomyLevel autonomyLevel,
        decimal amountLimit,
        AgentRiskLevel riskLimit,
        bool isActive,
        IEnumerable<string> allowedTools,
        string? code = null,
        Guid departmentId = default,
        IEnumerable<string>? dataScopes = null,
        IEnumerable<string>? modelScopes = null,
        IEnumerable<string>? requiredConfirmations = null,
        IEnumerable<string>? stopConditions = null,
        AgentProfileLifecycleStatus lifecycleStatus = AgentProfileLifecycleStatus.Published,
        Guid? currentPublishedVersionId = null) =>
        new(
            DomainChecks.RequiredId(id, "Agent"),
            serviceUserId,
            DomainChecks.Required(name, "Agent 名称", 128),
            autonomyLevel,
            amountLimit,
            riskLimit,
            isActive,
            allowedTools,
            code,
            departmentId,
            dataScopes ?? [],
            modelScopes ?? AgentCapabilityRegistry.AllowedModelScopes,
            requiredConfirmations ?? AgentCapabilityRegistry.PermanentHumanBoundaries,
            stopConditions ?? ["manual-stop", "risk-limit", "amount-limit"],
            lifecycleStatus,
            currentPublishedVersionId);

    public static AgentProfile Restore(
        Guid id,
        string name,
        AgentAutonomyLevel autonomyLevel,
        decimal amountLimit,
        AgentRiskLevel riskLimit,
        bool isActive,
        IEnumerable<string> allowedTools) =>
        Restore(id, Guid.Empty, name, autonomyLevel, amountLimit, riskLimit, isActive, allowedTools);

    public void Stop() => IsActive = false;

    public bool RequiresConfirmation(AgentActionType action, AgentRiskLevel risk, decimal amount) =>
        action is AgentActionType.Payment
            or AgentActionType.QualityRelease
            or AgentActionType.EquipmentControl
            or AgentActionType.FormulaChange
            or AgentActionType.SafetyInterlock
            or AgentActionType.ContractCommitment
            or AgentActionType.CustomerCompensation
        || risk == AgentRiskLevel.High
        || amount > AmountLimit
        || AutonomyLevel is AgentAutonomyLevel.A0 or AgentAutonomyLevel.A1;
}

public sealed class AgentRun
{
    private AgentRun(
        Guid id,
        Guid agentProfileId,
        Guid requestedBy,
        string action,
        decimal amount,
        AgentRiskLevel riskLevel,
        AgentRunStatus status,
        Guid? confirmationId,
        DateTimeOffset createdAt,
        Guid organizationId = default,
        Guid departmentId = default,
        string environmentCode = "demo",
        bool isTestData = true,
        string? subjectType = null,
        Guid? businessObjectId = null,
        Guid? workflowInstanceId = null,
        Guid? workflowTaskId = null,
        Guid? minutesSourceId = null,
        string? model = null,
        Guid? gatewayCallId = null,
        string? gatewayMode = null,
        bool externalModelCalled = false,
        DateTimeOffset? startedAt = null,
        DateTimeOffset? endedAt = null,
        string? proxyTriggerMode = null,
        DateTimeOffset? proxyTriggeredAt = null,
        Guid? originalAssigneeId = null,
        string? compensationStatus = null,
        string? proxyIdempotencyKey = null)
    {
        Id = id;
        AgentProfileId = agentProfileId;
        RequestedBy = requestedBy;
        Action = action;
        Amount = amount;
        RiskLevel = riskLevel;
        Status = status;
        ConfirmationId = confirmationId;
        CreatedAt = createdAt;
        OrganizationId = organizationId;
        DepartmentId = departmentId;
        EnvironmentCode = environmentCode;
        IsTestData = isTestData;
        SubjectType = subjectType;
        BusinessObjectId = businessObjectId;
        WorkflowInstanceId = workflowInstanceId;
        WorkflowTaskId = workflowTaskId;
        MinutesSourceId = minutesSourceId;
        Model = model;
        GatewayCallId = gatewayCallId;
        GatewayMode = gatewayMode;
        ExternalModelCalled = externalModelCalled;
        StartedAt = startedAt;
        EndedAt = endedAt;
        ProxyTriggerMode = proxyTriggerMode;
        ProxyTriggeredAt = proxyTriggeredAt;
        OriginalAssigneeId = originalAssigneeId;
        CompensationStatus = compensationStatus;
        ProxyIdempotencyKey = proxyIdempotencyKey;
    }

    public Guid Id { get; }

    public Guid AgentProfileId { get; }

    public Guid RequestedBy { get; }

    public string Action { get; }

    public decimal Amount { get; }

    public AgentRiskLevel RiskLevel { get; }

    public AgentRunStatus Status { get; private set; }

    public Guid? ConfirmationId { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public Guid OrganizationId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public string EnvironmentCode { get; private set; }
    public bool IsTestData { get; private set; }
    public string? SubjectType { get; private set; }
    public Guid? BusinessObjectId { get; private set; }
    public Guid? WorkflowInstanceId { get; private set; }
    public Guid? WorkflowTaskId { get; private set; }
    public Guid? MinutesSourceId { get; private set; }
    public string? Model { get; private set; }
    public Guid? GatewayCallId { get; private set; }
    public string? GatewayMode { get; private set; }
    public bool ExternalModelCalled { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public string? ProxyTriggerMode { get; private set; }
    public DateTimeOffset? ProxyTriggeredAt { get; private set; }
    public Guid? OriginalAssigneeId { get; private set; }
    public string? CompensationStatus { get; private set; }
    public string? ProxyIdempotencyKey { get; private set; }

    public static AgentRun Request(
        AgentProfile profile,
        Guid requestedBy,
        AgentActionType actionType,
        decimal amount,
        AgentRiskLevel riskLevel,
        Guid? confirmationId,
        DateTimeOffset now,
        Guid organizationId = default,
        Guid departmentId = default,
        string environmentCode = "demo",
        bool isTestData = true,
        string? subjectType = null,
        Guid? businessObjectId = null,
        Guid? workflowInstanceId = null,
        Guid? workflowTaskId = null,
        Guid? minutesSourceId = null,
        string? model = null,
        bool forceConfirmation = false,
        string? proxyTriggerMode = null,
        Guid? originalAssigneeId = null,
        string? compensationStatus = null,
        string? proxyIdempotencyKey = null,
        string? toolCode = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (!profile.IsActive
            || profile.LifecycleStatus != AgentProfileLifecycleStatus.Published
            || !profile.CurrentPublishedVersionId.HasValue)
        {
            throw new ConflictException(
                ErrorCodes.AgentStopped,
                "Agent 未发布或已停用，不能发起新运行。");
        }

        if (!string.IsNullOrWhiteSpace(toolCode) && !profile.AllowsTool(toolCode))
        {
            throw new PermissionException(
                ErrorCodes.PermissionDenied,
                "请求的工具不在数字员工白名单内。");
        }

        if (!profile.AllowsModel(model))
        {
            throw new PermissionException(
                ErrorCodes.PermissionDenied,
                "请求的模型不在数字员工模型范围内。");
        }

        if (amount < 0)
        {
            throw new ValidationException("Agent 运行金额不能为负数。");
        }

        if (amount > profile.AmountLimit)
        {
            throw new ConflictException(ErrorCodes.AgentLimitExceeded, "Agent 运行金额超过授权限额。");
        }

        if (riskLevel > profile.RiskLimit)
        {
            throw new ConflictException(ErrorCodes.AgentLimitExceeded, "Agent 运行风险等级超过授权范围。");
        }

        var requiresConfirmation = forceConfirmation
            || profile.RequiresConfirmation(actionType, riskLevel, amount);
        return new AgentRun(
            Guid.NewGuid(),
            profile.Id,
            DomainChecks.RequiredId(requestedBy, "Agent 运行发起人"),
            actionType.ToString(),
            amount,
            riskLevel,
            requiresConfirmation ? AgentRunStatus.AwaitingConfirmation : AgentRunStatus.Running,
            requiresConfirmation ? null : confirmationId,
            now.ToUniversalTime(),
            organizationId,
            departmentId,
            DataEnvironment.Normalize(environmentCode),
            isTestData,
            subjectType,
            businessObjectId,
            workflowInstanceId,
            workflowTaskId,
            minutesSourceId,
            model,
            proxyTriggerMode: proxyTriggerMode,
            proxyTriggeredAt: proxyTriggerMode is null ? null : now.ToUniversalTime(),
            originalAssigneeId: originalAssigneeId,
            compensationStatus: compensationStatus,
            proxyIdempotencyKey: proxyIdempotencyKey);
    }

    public static AgentRun Restore(
        Guid id,
        Guid agentProfileId,
        Guid requestedBy,
        string action,
        decimal amount,
        AgentRiskLevel riskLevel,
        AgentRunStatus status,
        Guid? confirmationId,
        DateTimeOffset createdAt,
        Guid organizationId = default,
        Guid departmentId = default,
        string environmentCode = "demo",
        bool isTestData = true,
        string? subjectType = null,
        Guid? businessObjectId = null,
        Guid? workflowInstanceId = null,
        Guid? workflowTaskId = null,
        Guid? minutesSourceId = null,
        string? model = null,
        Guid? gatewayCallId = null,
        string? gatewayMode = null,
        bool externalModelCalled = false,
        DateTimeOffset? startedAt = null,
        DateTimeOffset? endedAt = null,
        string? proxyTriggerMode = null,
        DateTimeOffset? proxyTriggeredAt = null,
        Guid? originalAssigneeId = null,
        string? compensationStatus = null,
        string? proxyIdempotencyKey = null) =>
        new(
            DomainChecks.RequiredId(id, "Agent 运行"),
            DomainChecks.RequiredId(agentProfileId, "Agent"),
            DomainChecks.RequiredId(requestedBy, "Agent 发起人"),
            DomainChecks.Required(action, "Agent 动作", 64),
            amount,
            riskLevel,
            status,
            confirmationId,
            createdAt.ToUniversalTime(),
            organizationId,
            departmentId,
            DataEnvironment.Normalize(environmentCode),
            isTestData,
            subjectType,
            businessObjectId,
            workflowInstanceId,
            workflowTaskId,
            minutesSourceId,
            model,
            gatewayCallId,
            gatewayMode,
            externalModelCalled,
            startedAt?.ToUniversalTime(),
            endedAt?.ToUniversalTime(),
            proxyTriggerMode,
            proxyTriggeredAt?.ToUniversalTime(),
            originalAssigneeId,
            compensationStatus,
            proxyIdempotencyKey);

    public static AgentRun RequestPreAuthorizedProxy(
        AgentProfile profile,
        Guid requestedBy,
        Guid originalAssigneeId,
        Guid organizationId,
        Guid departmentId,
        string environmentCode,
        bool isTestData,
        Guid workflowInstanceId,
        Guid workflowTaskId,
        Guid businessObjectId,
        string idempotencyKey,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (!profile.IsActive
            || profile.LifecycleStatus != AgentProfileLifecycleStatus.Published
            || !profile.CurrentPublishedVersionId.HasValue
            || profile.RiskLimit != AgentRiskLevel.Low
            || profile.AutonomyLevel is AgentAutonomyLevel.A0 or AgentAutonomyLevel.A1)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "Agent 不满足预授权自动代理条件。");
        }

        return new AgentRun(
            Guid.NewGuid(),
            profile.Id,
            DomainChecks.RequiredId(requestedBy, "Agent 发起人"),
            nameof(AgentActionType.Submit),
            0,
            AgentRiskLevel.Low,
            AgentRunStatus.Running,
            null,
            now.ToUniversalTime(),
            organizationId,
            departmentId,
            DataEnvironment.Normalize(environmentCode),
            isTestData,
            "WorkflowTaskProxy",
            businessObjectId,
            workflowInstanceId,
            workflowTaskId,
            null,
            "aidt-rule-sim",
            null,
            null,
            false,
            now.ToUniversalTime(),
            null,
            "SlaTimeout",
            now.ToUniversalTime(),
            originalAssigneeId,
            "Available",
            DomainChecks.Required(idempotencyKey, "代理幂等键", 160));
    }

    public void Confirm(Guid confirmationId, bool isHumanIdentity)
    {
        if (!isHumanIdentity)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "Agent 确认必须由真人身份完成。");
        }

        if (Status != AgentRunStatus.AwaitingConfirmation)
        {
            throw new ConflictException(ErrorCodes.Conflict, "该 Agent 运行不处于待确认状态。");
        }

        ConfirmationId = DomainChecks.RequiredId(confirmationId, "人工确认");
        Status = AgentRunStatus.Running;
        StartedAt ??= DateTimeOffset.UtcNow;
    }

    public void MarkRunning()
    {
        if (Status == AgentRunStatus.AwaitingConfirmation && ConfirmationId is null)
        {
            throw new ConflictException(ErrorCodes.AgentConfirmationRequired, "缺少真人确认，不能启动 Agent 运行。");
        }

        Status = AgentRunStatus.Running;
        StartedAt ??= DateTimeOffset.UtcNow;
    }

    public void Stop()
    {
        Status = AgentRunStatus.Stopped;
        EndedAt = DateTimeOffset.UtcNow;
    }

    public void Complete()
    {
        if (Status != AgentRunStatus.Running)
        {
            throw new ConflictException(ErrorCodes.AgentStopped, "只有运行中的 Agent 任务可以完成。");
        }

        Status = AgentRunStatus.Completed;
        EndedAt = DateTimeOffset.UtcNow;
    }

    public void RecordGatewayCall(Guid gatewayCallId, string mode, bool externalModelCalled)
    {
        GatewayCallId = gatewayCallId;
        GatewayMode = mode;
        ExternalModelCalled = externalModelCalled;
    }

    public void Fail()
    {
        Status = AgentRunStatus.Failed;
        EndedAt = DateTimeOffset.UtcNow;
    }
}
