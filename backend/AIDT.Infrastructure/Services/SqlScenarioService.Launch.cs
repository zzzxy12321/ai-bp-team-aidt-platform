using System.Text.Json;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Audit;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;
using AIDT.Domain.Integrations;
using AIDT.Domain.Scenarios;
using AIDT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Services;

public sealed partial class SqlScenarioService
{
    public async Task<ScenarioPreflightDto> PreflightAsync(
        UserPrincipal user,
        Guid scenarioId,
        ScenarioStartInputDto input,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        EnsureRead(user);
        var definition = await dbContext.ScenarioDefinitions.AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.Id == scenarioId
                && item.OrganizationId == user.OrganizationId,
                cancellationToken) ?? throw new NotFoundException("场景不存在。");
        var version = await LoadCurrentPublishedVersionAsync(definition, cancellationToken);
        var payload = DeserializePayload(version.PayloadJson);
        var departments = await LoadDepartmentsAsync(user.OrganizationId, cancellationToken);
        var department = await ResolveScenarioDepartmentAsync(
            user,
            definition,
            payload,
            input,
            departments,
            cancellationToken);
        var validation = await ValidatePayloadAsync(
            user,
            definition.OwnerDepartmentId,
            payload,
            cancellationToken);
        var frozenReferences = DeserializeReferenceSnapshot(version.ReferenceSnapshotJson);
        if (frozenReferences is not null)
        {
            var frozenErrors = await ValidateFrozenReferencesAsync(
                frozenReferences,
                cancellationToken);
            validation = validation with
            {
                IsValid = validation.IsValid && frozenErrors.Count == 0,
                Errors = validation.Errors.Concat(frozenErrors)
                    .Distinct(StringComparer.Ordinal)
                    .ToList(),
                References = frozenReferences
            };
        }
        var blockingReasons = new List<string>(validation.Errors);
        var warnings = new List<string>(validation.Warnings);
        ScenarioRules.EnsureStartable(
            ScenarioRules.ParseLifecycleStatus(definition.LifecycleStatus),
            ScenarioRules.ParseVersionStatus(version.Status),
            validation.References is not null,
            validation.Errors);
        if (!user.IsPlatformAdmin)
        {
            user.EnsureDepartmentAccess(department.Id);
        }

        var departmentCode = department.Code;
        if (!payload.ApplicableDepartmentCodes.Any(item =>
                string.Equals(item, departmentCode, StringComparison.OrdinalIgnoreCase))
            && department.Id != definition.OwnerDepartmentId)
        {
            throw new PermissionException(
                ErrorCodes.DepartmentScopeDenied,
                "当前部门无权使用该场景。");
        }

        var checks = new List<ScenarioPreparationCheckDto>();
        var existingObject = await ValidateBusinessObjectInputAsync(
            user,
            input,
            payload,
            department,
            cancellationToken);
        var project = await ValidateProjectInputAsync(
            user,
            input,
            payload,
            department,
            cancellationToken);
        var attachments = await ValidateAttachmentsAsync(
            user,
            input,
            department,
            cancellationToken);
        var minutes = await ValidateMinutesAsync(
            user,
            input,
            department,
            cancellationToken);
        var requestedAgent = await ResolveRequestedAgentAsync(
            user,
            input,
            payload,
            department,
            blockingReasons,
            cancellationToken);

        foreach (var item in payload.PreparationItems)
        {
            var isProvided = item.SourceType switch
            {
                "BusinessObject" => existingObject is not null
                    || payload.StartMode == ScenarioStartMode.CreateBusinessObjectAndWorkflow
                    && !string.IsNullOrWhiteSpace(input.ObjectCode),
                "Project" => project is not null,
                "Attachment" => attachments.Count > 0,
                "MinutesSource" => minutes.Count > 0,
                "Agent" => requestedAgent is not null,
                "EventInbox" => !string.IsNullOrWhiteSpace(
                    input.Content ?? input.TaskContent),
                _ => false
            };
            var isValid = isProvided || !item.IsRequired;
            if (item.IsRequired && !isProvided)
            {
                blockingReasons.Add($"缺少必填材料：{item.Name}。");
            }

            checks.Add(new ScenarioPreparationCheckDto(
                item.Code,
                item.Name,
                item.SourceType,
                item.IsRequired,
                isProvided,
                isValid,
                isProvided ? "已准备" : item.IsRequired ? "缺少必填材料" : "可选材料未提供"));
        }

        AddModeChecks(
            payload,
            input,
            existingObject,
            project,
            checks,
            blockingReasons);
        if (input.RequestAgentSuggestion && requestedAgent is null)
        {
            blockingReasons.Add("数字员工不可用：请选择当前场景允许且有效的数字员工。");
        }

        var checkedAt = clock.UtcNow;
        return new ScenarioPreflightDto(
            blockingReasons.Count == 0,
            definition.Id.ToString("D"),
            definition.Code,
            version.VersionNumber,
            payload.StartMode,
            department.Id.ToString("D"),
            department.Name,
            blockingReasons.Distinct(StringComparer.Ordinal).ToList(),
            warnings.Distinct(StringComparer.Ordinal).ToList(),
            checks,
            validation.References
                ?? new ScenarioReferenceSnapshotDto([], null, [], checkedAt),
            checkedAt);
    }

    public async Task<ScenarioLaunchDto> StartAsync(
        UserPrincipal user,
        Guid scenarioId,
        ScenarioStartInputDto input,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        EnsureRead(user);
        if (user.IsServiceIdentity)
        {
            throw new PermissionException(
                ErrorCodes.PermissionDenied,
                "数字员工服务身份不能启动业务场景，请由真人操作。");
        }

        ValidateIdempotencyKey(idempotencyKey);
        var definition = await dbContext.ScenarioDefinitions.AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.Id == scenarioId
                && item.OrganizationId == user.OrganizationId,
                cancellationToken) ?? throw new NotFoundException("场景不存在。");
        var version = await LoadCurrentPublishedVersionAsync(definition, cancellationToken);
        var scope = $"scenario:launch:{scenarioId:D}:{user.UserId:D}";
        var hash = Hash(new { scenarioId, versionId = version.Id, input });
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(
                    ErrorCodes.ScenarioDuplicateSubmission,
                    "重复提交已处理，但本次内容与原提交不一致，请刷新后重新发起。");
            }

            return await GetLaunchCoreAsync(
                    user,
                    Guid.Parse(existing.ResponseId),
                    cancellationToken)
                ?? throw new NotFoundException("幂等结果对应的场景启动记录不存在。");
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var preflight = await PreflightAsync(
                user,
                scenarioId,
                input,
                traceId,
                cancellationToken);
            if (!preflight.CanStart)
            {
                var code = preflight.BlockingReasons.Any(item => item.Contains("缺少", StringComparison.Ordinal))
                    ? ErrorCodes.ScenarioMaterialMissing
                    : ErrorCodes.ScenarioReferenceInvalid;
                throw new ConflictException(
                    code,
                    preflight.BlockingReasons.Count > 0
                        ? preflight.BlockingReasons[0]
                        : null
                    ?? "当前场景版本不可启动。");
            }

            var payload = DeserializePayload(version.PayloadJson);
            var department = await dbContext.Departments.SingleAsync(
                item => item.Id == Guid.Parse(preflight.DepartmentId),
                cancellationToken);
            var now = clock.UtcNow;
            Guid? businessObjectId = null;
            string? businessObjectCode = null;
            Guid? workflowInstanceId = null;
            Guid? workflowTaskId = null;
            Guid? projectId = null;
            Guid? projectTaskId = null;
            Guid? eventInboxId = null;
            string? objectTitle = null;

            switch (payload.StartMode)
            {
                case ScenarioStartMode.CreateBusinessObjectAndWorkflow:
                    {
                        var objectTypeCode = DomainChecks.Required(
                            payload.ObjectTypeCodes.SingleOrDefault(),
                            "场景业务对象类型",
                            64);
                        var objectType = await dbContext.ObjectTypes.AsNoTracking().SingleAsync(
                            item => item.Code == objectTypeCode,
                            cancellationToken);
                        var created = await createBusinessObjectHandler.StageWithinTransactionAsync(
                            user,
                            new CreateBusinessObjectRequest(
                                objectType.Id,
                                DomainChecks.Required(input.ObjectCode, "业务编号", 80),
                                input.Fields ?? new Dictionary<string, string?>(),
                                department.Id),
                            traceId,
                            now,
                            cancellationToken);
                        await dbContext.SaveChangesAsync(cancellationToken);
                        businessObjectId = Guid.Parse(created.Id);
                        businessObjectCode = created.ObjectCode;
                        objectTitle = created.Title;
                        var workflowRef = preflight.References.Workflow
                            ?? throw new ConflictException(
                                ErrorCodes.ScenarioReferenceInvalid,
                                "场景缺少有效流程版本，不能启动。");
                        var started = await startWorkflowInstanceHandler.StageWithinTransactionAsync(
                            new StartWorkflowInstanceRequest(
                                Guid.Parse(workflowRef.DefinitionId),
                                Guid.Parse(workflowRef.VersionId),
                                businessObjectId.Value,
                                IdempotencyKey: null),
                            user,
                            traceId,
                            requestHash: null,
                            idempotencyScope: null,
                            persistIdempotency: false,
                            cancellationToken);
                        workflowInstanceId = Guid.Parse(started.Instance.Id);
                        workflowTaskId = started.FirstTaskId;
                        break;
                    }
                case ScenarioStartMode.ExistingBusinessObjectAndWorkflow:
                    {
                        var target = await dbContext.BusinessObjects.SingleAsync(
                            item => item.Id == input.BusinessObjectId!.Value,
                            cancellationToken);
                        businessObjectId = target.Id;
                        businessObjectCode = target.ObjectCode;
                        objectTitle = target.Title;
                        var workflowRef = preflight.References.Workflow
                            ?? throw new ConflictException(
                                ErrorCodes.ScenarioReferenceInvalid,
                                "场景缺少有效流程版本，不能启动。");
                        var started = await startWorkflowInstanceHandler.StageWithinTransactionAsync(
                            new StartWorkflowInstanceRequest(
                                Guid.Parse(workflowRef.DefinitionId),
                                Guid.Parse(workflowRef.VersionId),
                                target.Id,
                                IdempotencyKey: null),
                            user,
                            traceId,
                            requestHash: null,
                            idempotencyScope: null,
                            persistIdempotency: false,
                            cancellationToken);
                        workflowInstanceId = Guid.Parse(started.Instance.Id);
                        workflowTaskId = started.FirstTaskId;
                        break;
                    }
                case ScenarioStartMode.EventInbox:
                    {
                        var inbox = await eventInboxService.StageReceiveWithinTransactionAsync(
                            user,
                            new ReceiveInboxEventRequest(
                                department.Id,
                                DomainChecks.Required(input.TaskTitle ?? input.Title, "事项标题", 255),
                                DomainChecks.Required(input.Content ?? input.TaskContent, "事项内容", 2_000_000),
                                "ManualText",
                                AttachmentId: input.AttachmentIds is { Count: > 0 }
                                    ? input.AttachmentIds[0]
                                    : null),
                            traceId,
                            cancellationToken);
                        eventInboxId = Guid.Parse(inbox.Id);
                        objectTitle = inbox.Title;
                        break;
                    }
                case ScenarioStartMode.ProjectTask:
                    {
                        projectId = input.ProjectId;
                        var task = await projectService.StageCreateTaskWithinTransactionAsync(
                            user,
                            input.ProjectId!.Value,
                            new CreateProjectTaskRequest(
                                input.AssigneeId!.Value,
                                DomainChecks.Required(input.TaskTitle ?? input.Title, "项目任务标题", 255),
                                DomainChecks.Required(input.TaskContent ?? input.Content, "项目任务内容", 4000),
                                input.DueAt!.Value,
                                input.Priority ?? "P1"),
                            traceId,
                            cancellationToken);
                        projectTaskId = Guid.Parse(task.Id);
                        objectTitle = task.Title;
                        break;
                    }
                default:
                    throw new ValidationException("不支持的场景启动方式。");
            }

            var launchId = Guid.NewGuid();
            var linkedEntityType = businessObjectId.HasValue
                ? "BusinessObject"
                : projectTaskId.HasValue
                    ? "ProjectTask"
                    : eventInboxId.HasValue
                        ? "EventInbox"
                        : projectId.HasValue
                            ? "Project"
                            : "ScenarioLaunch";
            var linkedEntityId = businessObjectId
                ?? projectTaskId
                ?? eventInboxId
                ?? projectId
                ?? launchId;
            var attachmentIds = input.AttachmentIds?.Distinct().ToArray() ?? [];
            foreach (var attachmentId in attachmentIds)
            {
                await fileCenterService.StageLinkToEntityWithinTransactionAsync(
                    user,
                    attachmentId,
                    linkedEntityType,
                    linkedEntityId,
                    traceId,
                    cancellationToken);
            }

            var minutesIds = input.MinutesSourceIds?.Distinct().ToArray() ?? [];
            if (projectId.HasValue)
            {
                foreach (var minutesId in minutesIds)
                {
                    await projectService.StageAddRelationWithinTransactionAsync(
                        user,
                        projectId.Value,
                        new CreateProjectRelationRequest(
                            "MinutesSource",
                            minutesId,
                            "ScenarioEvidence",
                            "由岗位业务场景启动关联"),
                        traceId,
                        cancellationToken);
                }
            }

            Guid? agentRunId = null;
            if (input.RequestAgentSuggestion && input.AgentProfileId.HasValue)
            {
                var run = await startAgentRunHandler.StageWithinTransactionAsync(
                    user,
                    new CreateAgentRunRequest(
                        input.AgentProfileId.Value,
                        Domain.Agents.AgentActionType.Draft,
                        0,
                        Domain.Agents.AgentRiskLevel.Low,
                        "aidt-rule-sim",
                        "workflow.assist"),
                    traceId,
                    "Scenario",
                    department.Id,
                    businessObjectId,
                    workflowInstanceId,
                    workflowTaskId,
                    minutesIds.FirstOrDefault() == Guid.Empty
                        ? null
                        : minutesIds.FirstOrDefault(),
                    cancellationToken);
                agentRunId = Guid.Parse(run.Id);
            }

            var links = BuildBusinessLinks(
                payload,
                businessObjectId,
                businessObjectCode,
                objectTitle,
                workflowInstanceId,
                workflowTaskId,
                projectId,
                projectTaskId,
                eventInboxId,
                agentRunId,
                preflight.References,
                attachmentIds,
                minutesIds);
            var launch = new ScenarioLaunchEntity
            {
                Id = launchId,
                OrganizationId = user.OrganizationId,
                ScenarioId = scenarioId,
                ScenarioVersionId = version.Id,
                ScenarioVersionNumber = version.VersionNumber,
                DepartmentId = department.Id,
                EnvironmentCode = DataEnvironment.Normalize(
                    environmentOptions.CurrentEnvironmentCode),
                IsTestData = DataEnvironment.IsTestEnvironment(
                    environmentOptions.CurrentEnvironmentCode),
                StartedBy = user.UserId,
                StartedAt = now,
                BusinessObjectId = businessObjectId,
                WorkflowInstanceId = workflowInstanceId,
                WorkflowTaskId = workflowTaskId,
                ProjectId = projectId,
                ProjectTaskId = projectTaskId,
                EventInboxId = eventInboxId,
                AgentRunId = agentRunId,
                AttachmentIdsJson = Serialize(attachmentIds),
                MinutesSourceIdsJson = Serialize(minutesIds),
                BusinessLinksJson = Serialize(links),
                IdempotencyKey = idempotencyKey,
                RequestHash = hash
            };
            dbContext.ScenarioLaunches.Add(launch);
            await dbContext.SaveChangesAsync(cancellationToken);
            if (attachmentIds.Length > 0)
            {
                foreach (var attachmentId in attachmentIds)
                {
                    await fileCenterService.StageLinkToEntityWithinTransactionAsync(
                        user,
                        attachmentId,
                        "ScenarioLaunch",
                        launch.Id,
                        traceId,
                        cancellationToken);
                }
            }

            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    ModuleName,
                    "scenario.launched",
                    "ScenarioLaunch",
                    launch.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: Serialize(new
                    {
                        scenarioId,
                        scenarioVersion = version.VersionNumber,
                        businessObjectId,
                        workflowInstanceId,
                        workflowTaskId,
                        projectId,
                        projectTaskId,
                        eventInboxId,
                        agentRunId,
                        attachmentIds,
                        minutesIds,
                        input.Comment,
                        payload.PermanentHumanBoundaries
                    })),
                cancellationToken);
            await outboxRepository.AddAsync(
                OutboxMessage.Create(
                    "Scenario.Launched",
                    "ScenarioLaunch",
                    launch.Id,
                    $"scenario-launched:{launch.Id:D}",
                    Serialize(new
                    {
                        launchId = launch.Id,
                        scenarioId,
                        versionId = version.Id,
                        businessObjectId,
                        workflowInstanceId,
                        workflowTaskId,
                        projectId,
                        projectTaskId,
                        eventInboxId,
                        agentRunId
                    }),
                    now),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "ScenarioLaunch",
                    launch.Id.ToString("D"),
                    "completed",
                    now,
                    now.AddDays(30)),
                cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return await GetLaunchCoreAsync(user, launch.Id, cancellationToken)
                ?? throw new InvalidOperationException("场景启动结果不可读取。");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ScenarioLaunchDto?> GetLaunchAsync(
        UserPrincipal user,
        Guid launchId,
        CancellationToken cancellationToken)
    {
        EnsureRead(user);
        return await GetLaunchCoreAsync(user, launchId, cancellationToken);
    }

    private async Task<ScenarioLaunchDto?> GetLaunchCoreAsync(
        UserPrincipal user,
        Guid launchId,
        CancellationToken cancellationToken)
    {
        var launch = await dbContext.ScenarioLaunches.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == launchId && item.OrganizationId == user.OrganizationId,
            cancellationToken);
        if (launch is null)
        {
            return null;
        }

        var canManage = await CanManageDepartmentAsync(
            user,
            launch.DepartmentId,
            cancellationToken);
        if (launch.StartedBy != user.UserId
            && !canManage
            && !user.IsPlatformAdmin)
        {
            throw new PermissionException(
                ErrorCodes.DepartmentScopeDenied,
                "当前身份无权读取该场景启动证据。");
        }

        var definition = await dbContext.ScenarioDefinitions.AsNoTracking().SingleAsync(
            item => item.Id == launch.ScenarioId,
            cancellationToken);
        var version = await dbContext.ScenarioVersions.AsNoTracking().SingleAsync(
            item => item.Id == launch.ScenarioVersionId,
            cancellationToken);
        var department = await dbContext.Departments.AsNoTracking().SingleAsync(
            item => item.Id == launch.DepartmentId,
            cancellationToken);
        var users = await LoadUserNamesAsync([launch.StartedBy], cancellationToken);
        var businessObject = launch.BusinessObjectId.HasValue
            ? await dbContext.BusinessObjects.AsNoTracking().SingleOrDefaultAsync(
                item => item.Id == launch.BusinessObjectId.Value,
                cancellationToken)
            : null;
        return new ScenarioLaunchDto(
            launch.Id.ToString("D"),
            definition.Id.ToString("D"),
            definition.Code,
            definition.Name,
            version.Id.ToString("D"),
            version.VersionNumber,
            department.Id.ToString("D"),
            department.Name,
            launch.StartedBy.ToString("D"),
            users.GetValueOrDefault(launch.StartedBy) ?? "未知用户",
            launch.StartedAt,
            launch.BusinessObjectId?.ToString("D"),
            businessObject?.ObjectCode,
            launch.WorkflowInstanceId?.ToString("D"),
            launch.WorkflowTaskId?.ToString("D"),
            launch.ProjectId?.ToString("D"),
            launch.ProjectTaskId?.ToString("D"),
            launch.EventInboxId?.ToString("D"),
            launch.AgentRunId?.ToString("D"),
            DeserializeIds(launch.AttachmentIdsJson),
            DeserializeIds(launch.MinutesSourceIdsJson),
            DeserializeBusinessLinks(launch.BusinessLinksJson),
            DeserializePayload(version.PayloadJson).PermanentHumanBoundaries);
    }

    private async Task<ScenarioVersionEntity> LoadCurrentPublishedVersionAsync(
        ScenarioDefinitionEntity definition,
        CancellationToken cancellationToken)
    {
        if (definition.CurrentPublishedVersionId is null)
        {
            throw new ConflictException(
                ErrorCodes.ScenarioNotPublished,
                "当前场景尚未发布，不能查看或启动。");
        }

        var version = await dbContext.ScenarioVersions.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == definition.CurrentPublishedVersionId.Value,
            cancellationToken) ?? throw new ConflictException(
                ErrorCodes.ScenarioReferenceInvalid,
                "当前场景发布版本不存在。");
        return version;
    }

    private async Task<DepartmentEntity> ResolveScenarioDepartmentAsync(
        UserPrincipal user,
        ScenarioDefinitionEntity definition,
        ScenarioDefinitionPayloadDto payload,
        ScenarioStartInputDto input,
        Dictionary<Guid, DepartmentEntity> departments,
        CancellationToken cancellationToken)
    {
        Guid? departmentId = input.DepartmentId;
        if (input.BusinessObjectId.HasValue)
        {
            departmentId = await dbContext.BusinessObjects.AsNoTracking()
                .Where(item => item.Id == input.BusinessObjectId.Value
                    && item.OrganizationId == user.OrganizationId
                    && !item.IsDeleted)
                .Select(item => (Guid?)item.DepartmentId)
                .SingleOrDefaultAsync(cancellationToken);
        }
        else if (input.ProjectId.HasValue)
        {
            departmentId = await dbContext.BusinessObjects.AsNoTracking()
                .Where(item => item.Id == input.ProjectId.Value
                    && item.OrganizationId == user.OrganizationId
                    && !item.IsDeleted)
                .Select(item => (Guid?)item.DepartmentId)
                .SingleOrDefaultAsync(cancellationToken);
        }

        if (!departmentId.HasValue)
        {
            departmentId = user.DepartmentIds.Count == 1
                ? user.DepartmentIds.Single()
                : definition.OwnerDepartmentId;
        }

        if (!departments.TryGetValue(departmentId.Value, out var department))
        {
            throw new NotFoundException("当前场景适用部门不存在或已停用。");
        }

        return department;
    }

    private async Task<BusinessObjectEntity?> ValidateBusinessObjectInputAsync(
        UserPrincipal user,
        ScenarioStartInputDto input,
        ScenarioDefinitionPayloadDto payload,
        DepartmentEntity department,
        CancellationToken cancellationToken)
    {
        if (input.BusinessObjectId is null)
        {
            return null;
        }

        var businessObject = await dbContext.BusinessObjects.AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.Id == input.BusinessObjectId.Value
                && item.OrganizationId == user.OrganizationId
                && !item.IsDeleted,
                cancellationToken) ?? throw new NotFoundException("业务对象不存在或已失效。");
        if (businessObject.DepartmentId != department.Id)
        {
            throw new PermissionException(
                ErrorCodes.DepartmentScopeDenied,
                "所选业务对象不属于当前场景部门。");
        }

        var objectTypeCode = await dbContext.ObjectTypes.AsNoTracking()
            .Where(item => item.Id == businessObject.ObjectTypeId)
            .Select(item => item.Code)
            .SingleAsync(cancellationToken);
        if (!payload.ObjectTypeCodes.Contains(objectTypeCode, StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationException(
                $"业务对象类型 {objectTypeCode} 不在当前场景允许范围内。");
        }

        return businessObject;
    }

    private async Task<BusinessObjectEntity?> ValidateProjectInputAsync(
        UserPrincipal user,
        ScenarioStartInputDto input,
        ScenarioDefinitionPayloadDto payload,
        DepartmentEntity department,
        CancellationToken cancellationToken)
    {
        if (input.ProjectId is null)
        {
            return null;
        }

        var project = await dbContext.BusinessObjects.AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.Id == input.ProjectId.Value
                && item.OrganizationId == user.OrganizationId
                && !item.IsDeleted,
                cancellationToken) ?? throw new NotFoundException("项目不存在或已失效。");
        if (project.DepartmentId != department.Id)
        {
            throw new PermissionException(
                ErrorCodes.DepartmentScopeDenied,
                "所选项目不属于当前场景部门。");
        }

        if (!payload.ObjectTypeCodes.Contains("PROJECT", StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationException("当前场景未声明支持项目业务链。");
        }

        return project;
    }

    private async Task<IReadOnlyList<FileAttachmentDto>> ValidateAttachmentsAsync(
        UserPrincipal user,
        ScenarioStartInputDto input,
        DepartmentEntity department,
        CancellationToken cancellationToken)
    {
        var result = new List<FileAttachmentDto>();
        foreach (var attachmentId in input.AttachmentIds?.Distinct() ?? [])
        {
            var file = await fileCenterService.GetAsync(
                user,
                attachmentId,
                includeTestData: false,
                cancellationToken);
            if (file is null)
            {
                throw new NotFoundException("场景材料文件不存在、已失效或无权使用。");
            }

            result.Add(file);
        }

        return result;
    }

    private async Task<IReadOnlyList<MinutesSourceDto>> ValidateMinutesAsync(
        UserPrincipal user,
        ScenarioStartInputDto input,
        DepartmentEntity department,
        CancellationToken cancellationToken)
    {
        var result = new List<MinutesSourceDto>();
        foreach (var minutesId in input.MinutesSourceIds?.Distinct() ?? [])
        {
            var minutes = await minutesService.GetSourceAsync(
                user,
                minutesId,
                includeTestData: false,
                cancellationToken);
            if (minutes is null)
            {
                throw new NotFoundException("听记来源不存在、已失效或无权使用。");
            }

            result.Add(minutes);
        }

        return result;
    }

    private async Task<AgentProfileEntity?> ResolveRequestedAgentAsync(
        UserPrincipal user,
        ScenarioStartInputDto input,
        ScenarioDefinitionPayloadDto payload,
        DepartmentEntity department,
        List<string> blockingReasons,
        CancellationToken cancellationToken)
    {
        if (!input.RequestAgentSuggestion)
        {
            return null;
        }

        var profile = input.AgentProfileId.HasValue
            ? await dbContext.AgentProfiles.AsNoTracking().SingleOrDefaultAsync(
                item => item.Id == input.AgentProfileId.Value,
                cancellationToken)
            : null;
        if (profile is null)
        {
            blockingReasons.Add("数字员工不可用：未选择有效身份。");
            return null;
        }

        var serviceUser = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == profile.ServiceUserId, cancellationToken);
        if (serviceUser is null
            || serviceUser.OrganizationId != user.OrganizationId
            || !profile.IsActive
            || profile.LifecycleStatus != "Published"
            || !profile.CurrentPublishedVersionId.HasValue)
        {
            blockingReasons.Add("数字员工不可用：所选身份未发布、已停用或不属于当前组织。");
            return null;
        }

        if (!payload.AllowedAgents.Any(item =>
                string.Equals(item.AgentProfileCode, profile.Code, StringComparison.OrdinalIgnoreCase)))
        {
            blockingReasons.Add("数字员工不可用：所选身份未被当前场景授权。");
            return null;
        }

        if (profile.DepartmentId != department.Id && !user.IsPlatformAdmin)
        {
            blockingReasons.Add("数字员工不可用：身份数据范围不覆盖当前部门。");
            return null;
        }

        return profile;
    }

    private static void AddModeChecks(
        ScenarioDefinitionPayloadDto payload,
        ScenarioStartInputDto input,
        BusinessObjectEntity? existingObject,
        BusinessObjectEntity? project,
        List<ScenarioPreparationCheckDto> checks,
        List<string> blockingReasons)
    {
        void Add(
            string code,
            string name,
            string sourceType,
            bool valid,
            string message)
        {
            checks.Add(new ScenarioPreparationCheckDto(
                code,
                name,
                sourceType,
                true,
                valid,
                valid,
                valid ? "已准备" : message));
            if (!valid)
            {
                blockingReasons.Add(message);
            }
        }

        switch (payload.StartMode)
        {
            case ScenarioStartMode.CreateBusinessObjectAndWorkflow:
                Add(
                    "OBJECT_CODE",
                    "业务事项编号与表单",
                    "BusinessObject",
                    !string.IsNullOrWhiteSpace(input.ObjectCode),
                    "缺少业务事项编号，无法创建真实业务对象。");
                break;
            case ScenarioStartMode.ExistingBusinessObjectAndWorkflow:
                Add(
                    "EXISTING_OBJECT",
                    "已存在的业务事项",
                    "BusinessObject",
                    existingObject is not null,
                    "缺少已发布业务对象，无法进入人工复核流程。");
                break;
            case ScenarioStartMode.EventInbox:
                Add(
                    "EVENT_CONTENT",
                    "事项标题与内容",
                    "EventInbox",
                    !string.IsNullOrWhiteSpace(input.Title ?? input.TaskTitle)
                    && !string.IsNullOrWhiteSpace(input.Content ?? input.TaskContent),
                    "缺少事项标题或内容，无法创建部门事项。");
                break;
            case ScenarioStartMode.ProjectTask:
                Add(
                    "PROJECT_TASK",
                    "项目、负责人和期限",
                    "ProjectTask",
                    project is not null
                    && input.AssigneeId.HasValue
                    && input.DueAt.HasValue
                    && !string.IsNullOrWhiteSpace(input.TaskTitle ?? input.Title)
                    && !string.IsNullOrWhiteSpace(input.TaskContent ?? input.Content),
                    "缺少真实项目、负责人、期限或任务内容，无法生成项目任务。");
                break;
        }
    }

    private static List<ScenarioBusinessLinkDto> BuildBusinessLinks(
        ScenarioDefinitionPayloadDto payload,
        Guid? businessObjectId,
        string? businessObjectCode,
        string? objectTitle,
        Guid? workflowInstanceId,
        Guid? workflowTaskId,
        Guid? projectId,
        Guid? projectTaskId,
        Guid? eventInboxId,
        Guid? agentRunId,
        ScenarioReferenceSnapshotDto references,
        IReadOnlyList<Guid> attachmentIds,
        IReadOnlyList<Guid> minutesIds)
    {
        var links = new List<ScenarioBusinessLinkDto>();
        if (businessObjectId.HasValue)
        {
            links.Add(new ScenarioBusinessLinkDto(
                "BusinessObject",
                businessObjectId.Value.ToString("D"),
                objectTitle ?? businessObjectCode ?? "业务对象",
                "业务主记录",
                $"/api/v1/business-objects/{businessObjectId.Value:D}"));
        }

        if (workflowInstanceId.HasValue)
        {
            links.Add(new ScenarioBusinessLinkDto(
                "WorkflowInstance",
                workflowInstanceId.Value.ToString("D"),
                references.Workflow?.DefinitionName ?? "业务流程",
                "流程执行",
                $"/api/v1/workflow-instances/{workflowInstanceId.Value:D}"));
        }

        if (workflowTaskId.HasValue)
        {
            links.Add(new ScenarioBusinessLinkDto(
                "WorkflowTask",
                workflowTaskId.Value.ToString("D"),
                "当前待办",
                "下一步人工处理",
                "/api/v1/workflow-tasks"));
        }

        if (projectId.HasValue)
        {
            links.Add(new ScenarioBusinessLinkDto(
                "Project",
                projectId.Value.ToString("D"),
                "项目",
                "项目载体",
                $"/api/v1/projects/{projectId.Value:D}"));
        }

        if (projectTaskId.HasValue)
        {
            links.Add(new ScenarioBusinessLinkDto(
                "ProjectTask",
                projectTaskId.Value.ToString("D"),
                objectTitle ?? "项目任务",
                "执行任务",
                "/api/v1/my-work"));
        }

        if (eventInboxId.HasValue)
        {
            links.Add(new ScenarioBusinessLinkDto(
                "EventInbox",
                eventInboxId.Value.ToString("D"),
                objectTitle ?? "部门事项",
                "待确认事项",
                $"/api/v1/inbox/{eventInboxId.Value:D}"));
        }

        if (agentRunId.HasValue)
        {
            links.Add(new ScenarioBusinessLinkDto(
                "AgentRun",
                agentRunId.Value.ToString("D"),
                "数字员工建议",
                "待人工确认的建议草稿",
                $"/api/v1/agents/runs/{agentRunId.Value:D}/context"));
        }

        foreach (var attachmentId in attachmentIds)
        {
            links.Add(new ScenarioBusinessLinkDto(
                "Attachment",
                attachmentId.ToString("D"),
                "场景材料",
                "受控附件",
                $"/api/v1/files/{attachmentId:D}"));
        }

        foreach (var minutesId in minutesIds)
        {
            links.Add(new ScenarioBusinessLinkDto(
                "MinutesSource",
                minutesId.ToString("D"),
                "AI 听记来源",
                "业务来源证据",
                $"/api/v1/minutes/sources/{minutesId:D}"));
        }

        return links;
    }
}
