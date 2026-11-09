using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Application.WorkItems;
using AIDT.Domain.Agents;
using AIDT.Domain.Audit;
using AIDT.Domain.BusinessObjects;
using AIDT.Domain.Common;
using AIDT.Domain.Documents;
using AIDT.Domain.Identity;
using AIDT.Domain.Projects;
using AIDT.Domain.Workflows;
using AIDT.Infrastructure.Configuration;
using AIDT.Infrastructure.Persistence;
using AIDT.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Services;

public sealed class SqlEmployeeWorkspaceQueryService(
    AidtDbContext dbContext,
    IAuditRepository auditRepository,
    IClock clock,
    DataEnvironmentOptions environmentOptions,
    BusinessTimeOptions businessTimeOptions) : IEmployeeWorkspaceQueryService
{
    public async Task<MyWorkDto> GetMyWorkAsync(
        UserPrincipal user,
        bool includeTestData,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("dashboard", AccessAction.Read);
        user.EnsureOrganizationAccess(user.OrganizationId);
        var departments = await AccessibleDepartmentsAsync(user, cancellationToken);
        var departmentIds = departments.Select(item => Guid.Parse(item.Id)).ToArray();
        var departmentNames = departments.ToDictionary(item => Guid.Parse(item.Id), item => item.Name);
        var now = clock.UtcNow;
        var timeZone = businessTimeOptions.ResolveTimeZone();
        var aggregates = await LoadWorkItemAggregatesAsync(
            user,
            departmentIds,
            departmentNames,
            includeTestData,
            now,
            timeZone,
            cancellationToken);
        var unique = WorkItemPresentation.Deduplicate(
            aggregates,
            item => $"{item.Item.SourceType}:{item.Item.Id}");
        var pending = WorkItemPresentation.Sort(
            unique.Where(item => item.IsPending).Select(item => item.Item),
            now,
            timeZone);
        var initiated = WorkItemPresentation.Sort(
            unique.Where(item => item.IsInitiatedByMe).Select(item => item.Item),
            now,
            timeZone);
        var participating = WorkItemPresentation.Sort(
            unique.Where(item => item.IsParticipating).Select(item => item.Item),
            now,
            timeZone);
        var needsAttention = WorkItemPresentation.Sort(
            unique.Where(item =>
                    item.Item.IsPermanentHumanBoundary
                    || item.Item.PriorityGroup is "Overdue" or "DueToday" or "AwaitingConfirmation")
                .Select(item => item.Item),
            now,
            timeZone);
        var recentFiles = user.HasPermission("file", AccessAction.Read)
            ? await LoadRecentFilesAsync(user, departmentIds, includeTestData, cancellationToken)
            : [];
        var summary = new MyWorkSummaryDto(
            pending.Count,
            pending.Count(item => item.PriorityGroup == "DueToday"),
            pending.Count(item => item.RequiresConfirmation),
            pending.Count(item => item.IsPermanentHumanBoundary));

        return new MyWorkDto(
            user.DisplayName,
            pending.Count > 0 ? pending[0] : null,
            pending,
            initiated,
            participating,
            needsAttention,
            recentFiles,
            summary);
    }

    private async Task<List<WorkItemAggregate>> LoadWorkItemAggregatesAsync(
        UserPrincipal user,
        Guid[] departmentIds,
        IReadOnlyDictionary<Guid, string> departmentNames,
        bool includeTestData,
        DateTimeOffset now,
        TimeZoneInfo timeZone,
        CancellationToken cancellationToken)
    {
        var result = new List<WorkItemAggregate>();
        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);

        var taskRows = user.HasPermission("workflow-task", AccessAction.Read)
            ? await LoadTaskRowsAsync(user, departmentIds, includeTestData, cancellationToken)
            : [];
        var workflowTaskIds = taskRows
            .Where(row => row.Status == nameof(WorkflowTaskStatus.Pending)
                && (row.AssigneeId == user.UserId || row.IsCandidate))
            .Select(row => row.TaskId)
            .ToHashSet();
        foreach (var row in taskRows.Where(row =>
                     row.Status == nameof(WorkflowTaskStatus.Pending)
                     && (row.AssigneeId == user.UserId
                         || row.IsCandidate
                         || row.StartedByUserId == user.UserId)))
        {
            var requiresConfirmation = row.DecisionMode == "AwaitingAgentConfirmation";
            var item = CreateWorkItem(
                row.TaskId,
                WorkItemSourceTypes.WorkflowTask,
                row.BusinessTitle,
                row.WorkflowName,
                null,
                row.DepartmentId,
                departmentNames.GetValueOrDefault(row.DepartmentId, "当前部门"),
                NodeLabel(row.NodeCode),
                row.AssigneeId,
                row.AssigneeName,
                row.DueAt,
                row.SlaStatus,
                row.Status,
                requiresConfirmation ? "确认数字员工处理结果" : "进入流程处理",
                2,
                row.InstanceId.ToString("D"),
                $"/workflows?instance={row.InstanceId:D}&task={row.TaskId:D}",
                row.EnvironmentCode,
                row.IsTestData,
                row.PermanentHumanBoundary,
                requiresConfirmation,
                row.CreatedAt,
                now,
                timeZone);
            result.Add(new WorkItemAggregate(
                item,
                row.AssigneeId == user.UserId,
                row.StartedByUserId == user.UserId,
                row.AssigneeId == user.UserId || row.IsCandidate));
        }

        if (user.HasPermission("project-task", AccessAction.Read))
        {
            var projectTasks = await (
                from task in dbContext.ProjectTasks.AsNoTracking()
                join project in dbContext.BusinessObjects.AsNoTracking() on task.ProjectId equals project.Id
                join assignee in dbContext.Users.AsNoTracking() on task.AssigneeId equals assignee.Id
                where !project.IsDeleted
                    && project.OrganizationId == user.OrganizationId
                    && (user.IsPlatformAdmin || departmentIds.Contains(task.DepartmentId))
                    && (includeTestData && user.IsPlatformAdmin || task.EnvironmentCode == environment)
                    && (task.Status == nameof(ProjectTaskStatus.Pending)
                        || task.Status == nameof(ProjectTaskStatus.InProgress))
                    && (task.AssigneeId == user.UserId || task.CreatedBy == user.UserId)
                orderby task.DueAt, task.CreatedAt descending
                select new
                {
                    task.Id,
                    task.ProjectId,
                    task.EventInboxId,
                    task.WorkflowTaskId,
                    task.AssigneeId,
                    AssigneeName = assignee.DisplayName,
                    task.DepartmentId,
                    TaskTitle = task.Title,
                    ProjectTitle = project.Title ?? project.ObjectCode,
                    task.DueAt,
                    task.Priority,
                    task.Status,
                    task.EnvironmentCode,
                    task.IsTestData,
                    task.CreatedAt,
                    task.CreatedBy
                }).Take(300).ToListAsync(cancellationToken);
            foreach (var row in projectTasks)
            {
                if (row.WorkflowTaskId.HasValue && workflowTaskIds.Contains(row.WorkflowTaskId.Value))
                {
                    continue;
                }

                var item = CreateWorkItem(
                    row.Id,
                    WorkItemSourceTypes.ProjectTask,
                    row.TaskTitle,
                    row.ProjectTitle,
                    row.ProjectId,
                    row.DepartmentId,
                    departmentNames.GetValueOrDefault(row.DepartmentId, "当前部门"),
                    row.Status == nameof(ProjectTaskStatus.Pending) ? "项目执行" : "任务处理中",
                    row.AssigneeId,
                    row.AssigneeName,
                    row.DueAt,
                    row.Priority,
                    row.Status,
                    "完成项目任务",
                    1 + (row.EventInboxId.HasValue ? 1 : 0) + (row.WorkflowTaskId.HasValue ? 1 : 0),
                    row.Id.ToString("D"),
                    $"/projects/{row.ProjectId:D}?task={row.Id:D}",
                    row.EnvironmentCode,
                    row.IsTestData,
                    false,
                    false,
                    row.CreatedAt,
                    now,
                    timeZone);
                result.Add(new WorkItemAggregate(
                    item,
                    row.AssigneeId == user.UserId,
                    row.CreatedBy == user.UserId,
                    true));
            }
        }

        if (user.HasPermission("event-inbox", AccessAction.Read))
        {
            var canConfirmEvent = user.HasPermission("event-inbox", AccessAction.Confirm);
            var events = await dbContext.EventInboxes.AsNoTracking()
                .Where(item =>
                    item.OrganizationId == user.OrganizationId
                    && (includeTestData && user.IsPlatformAdmin || item.EnvironmentCode == environment)
                    && (user.IsPlatformAdmin
                        || item.SubmittedBy == user.UserId
                        || departmentIds.Contains(item.DepartmentId))
                    && item.Status == nameof(InboxEventStatus.AwaitingConfirmation)
                    && !dbContext.ProjectTasks.Any(task => task.EventInboxId == item.Id))
                .OrderByDescending(item => item.UpdatedAt)
                .Take(200)
                .ToListAsync(cancellationToken);
            var eventIds = events.Select(item => item.Id).ToArray();
            var eventSuggestions = await dbContext.EventSuggestions.AsNoTracking()
                .Where(item => eventIds.Contains(item.EventInboxId))
                .ToDictionaryAsync(item => item.EventInboxId, cancellationToken);
            var projectIds = eventSuggestions.Values
                .Where(item => item.SuggestedProjectId.HasValue)
                .Select(item => item.SuggestedProjectId!.Value)
                .Distinct()
                .ToArray();
            var projectTitles = await dbContext.BusinessObjects.AsNoTracking()
                .Where(item => projectIds.Contains(item.Id))
                .ToDictionaryAsync(
                    item => item.Id,
                    item => item.Title ?? item.ObjectCode,
                    cancellationToken);
            var assigneeIds = eventSuggestions.Values
                .Where(item => item.SuggestedAssigneeId.HasValue)
                .Select(item => item.SuggestedAssigneeId!.Value)
                .Distinct()
                .ToArray();
            var assigneeNames = await dbContext.Users.AsNoTracking()
                .Where(item => assigneeIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, item => item.DisplayName, cancellationToken);
            foreach (var row in events)
            {
                eventSuggestions.TryGetValue(row.Id, out var suggestion);
                var projectTitle = suggestion?.SuggestedProjectId is Guid projectId
                    ? projectTitles.GetValueOrDefault(projectId)
                    : null;
                var suggestedAssigneeId = suggestion?.SuggestedAssigneeId;
                var item = CreateWorkItem(
                    row.Id,
                    WorkItemSourceTypes.EventInbox,
                    row.Title,
                    projectTitle ?? departmentNames.GetValueOrDefault(row.DepartmentId, "当前部门"),
                    suggestion?.SuggestedProjectId,
                    row.DepartmentId,
                    departmentNames.GetValueOrDefault(row.DepartmentId, "当前部门"),
                    "事项确认",
                    suggestedAssigneeId,
                    suggestedAssigneeId.HasValue
                        ? assigneeNames.GetValueOrDefault(suggestedAssigneeId.Value, "待指定")
                        : "待指定",
                    suggestion?.SuggestedDueAt,
                    suggestion?.RiskLevel ?? "Low",
                    row.Status,
                    suggestion?.RequiresHumanBoundary == true
                        ? "转人工处理高风险事项"
                        : "确认项目、负责人和时间",
                    1 + (row.AttachmentId.HasValue ? 1 : 0),
                    row.Id.ToString("D"),
                    $"/inbox?event={row.Id:D}",
                    row.EnvironmentCode,
                    row.IsTestData,
                    suggestion?.RequiresHumanBoundary ?? false,
                    true,
                    row.UpdatedAt,
                    now,
                    timeZone);
                result.Add(new WorkItemAggregate(
                    item,
                    canConfirmEvent,
                    row.SubmittedBy == user.UserId,
                    canConfirmEvent || row.SubmittedBy == user.UserId));
            }
        }

        if (user.HasPermission("minutes", AccessAction.Read))
        {
            var canApproveMinutes = user.HasPermission("minutes", AccessAction.Approve);
            var actionRows = await (
                from action in dbContext.MinutesActionItems.AsNoTracking()
                join source in dbContext.MinutesSources.AsNoTracking()
                    on action.MinutesSourceId equals source.Id
                where source.OrganizationId == user.OrganizationId
                    && (user.IsPlatformAdmin || departmentIds.Contains(source.DepartmentId))
                    && (includeTestData && user.IsPlatformAdmin || source.EnvironmentCode == environment)
                    && action.Status == "Draft"
                    && (source.CreatedBy == user.UserId
                        || action.SuggestedAssigneeId == user.UserId
                        || action.AssigneeId == user.UserId)
                orderby action.DueAt, action.CreatedAt descending
                select new
                {
                    action.Id,
                    action.MinutesSourceId,
                    action.Content,
                    SourceTitle = source.Title,
                    source.DepartmentId,
                    source.CreatedBy,
                    ActionAssigneeId = action.AssigneeId ?? action.SuggestedAssigneeId,
                    DueAt = action.DueAt ?? action.SuggestedDueAt,
                    action.Priority,
                    action.Status,
                    action.MissingInformation,
                    source.EnvironmentCode,
                    source.IsTestData,
                    action.CreatedAt
                }).Take(200).ToListAsync(cancellationToken);
            var actionAssigneeIds = actionRows.Where(item => item.ActionAssigneeId.HasValue)
                .Select(item => item.ActionAssigneeId!.Value)
                .Distinct()
                .ToArray();
            var actionAssigneeNames = await dbContext.Users.AsNoTracking()
                .Where(item => actionAssigneeIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, item => item.DisplayName, cancellationToken);
            foreach (var row in actionRows)
            {
                var item = CreateWorkItem(
                    row.Id,
                    WorkItemSourceTypes.MinutesActionItem,
                    row.Content,
                    row.SourceTitle,
                    null,
                    row.DepartmentId,
                    departmentNames.GetValueOrDefault(row.DepartmentId, "当前部门"),
                    "会议行动项",
                    row.ActionAssigneeId,
                    row.ActionAssigneeId.HasValue
                        ? actionAssigneeNames.GetValueOrDefault(row.ActionAssigneeId.Value, "待指定")
                        : "待指定",
                    row.DueAt,
                    row.Priority,
                    row.Status,
                    "确认负责人、截止时间和下一步",
                    2,
                    row.Id.ToString("D"),
                    $"/minutes?source={row.MinutesSourceId:D}&actionItem={row.Id:D}",
                    row.EnvironmentCode,
                    row.IsTestData,
                    false,
                    true,
                    row.CreatedAt,
                    now,
                    timeZone);
                result.Add(new WorkItemAggregate(
                    item,
                    canApproveMinutes,
                    row.CreatedBy == user.UserId,
                    true));
            }
        }

        var mcpRows = await dbContext.McpHumanConfirmations.AsNoTracking()
            .Where(item =>
                item.UserId == user.UserId
                && item.OrganizationId == user.OrganizationId
                && item.Status == "Pending"
                && item.ExpiresAt > now)
            .OrderBy(item => item.ExpiresAt)
            .Take(100)
            .ToListAsync(cancellationToken);
        foreach (var row in mcpRows)
        {
            var item = CreateWorkItem(
                row.Id,
                WorkItemSourceTypes.McpHumanConfirmation,
                McpActionTitle(row.ToolName),
                "智能体接入请求",
                null,
                null,
                "智能体接入",
                "高风险确认",
                user.UserId,
                user.DisplayName,
                row.ExpiresAt,
                "High",
                row.Status,
                "核对请求并真人确认",
                1,
                row.Id.ToString("D"),
                $"/mcp-confirmations?confirmationId={row.Id:D}",
                environmentOptions.CurrentEnvironmentCode,
                false,
                true,
                true,
                row.CreatedAt,
                now,
                timeZone);
            result.Add(new WorkItemAggregate(item, true, true, true));
        }

        if (user.HasPermission("agent-run", AccessAction.Read))
        {
            var canApproveAgent = user.HasPermission("agent-run", AccessAction.Approve);
            var agentRows = await dbContext.AgentRuns.AsNoTracking()
                .Where(item =>
                    item.OrganizationId == user.OrganizationId
                    && item.RequestedBy == user.UserId
                    && (user.IsPlatformAdmin || departmentIds.Contains(item.DepartmentId))
                    && (includeTestData && user.IsPlatformAdmin || item.EnvironmentCode == environment)
                    && item.Status == nameof(AgentRunStatus.AwaitingConfirmation))
                .OrderByDescending(item => item.CreatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);
            var profileIds = agentRows.Select(item => item.AgentProfileId).Distinct().ToArray();
            var profiles = await dbContext.AgentProfiles.AsNoTracking()
                .Where(item => profileIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
            var businessObjectIds = agentRows
                .Where(item => item.BusinessObjectId.HasValue)
                .Select(item => item.BusinessObjectId!.Value)
                .Distinct()
                .ToArray();
            var businessObjectTitles = await dbContext.BusinessObjects.AsNoTracking()
                .Where(item => businessObjectIds.Contains(item.Id))
                .ToDictionaryAsync(
                    item => item.Id,
                    item => item.Title ?? item.ObjectCode,
                    cancellationToken);
            foreach (var row in agentRows)
            {
                var permanent = IsPermanentAgentAction(row.Action);
                var contextTitle = row.BusinessObjectId is Guid businessObjectId
                    ? businessObjectTitles.GetValueOrDefault(
                        businessObjectId,
                        departmentNames.GetValueOrDefault(row.DepartmentId, "当前部门"))
                    : departmentNames.GetValueOrDefault(row.DepartmentId, "当前部门");
                var item = CreateWorkItem(
                    row.Id,
                    WorkItemSourceTypes.AgentRun,
                    $"{profiles.GetValueOrDefault(row.AgentProfileId, "数字员工")}：{AgentActionLabel(row.Action)}",
                    contextTitle,
                    row.BusinessObjectId,
                    row.DepartmentId,
                    departmentNames.GetValueOrDefault(row.DepartmentId, "当前部门"),
                    "数字员工运行",
                    user.UserId,
                    user.DisplayName,
                    null,
                    row.RiskLevel,
                    row.Status,
                    permanent ? "人工核对后处理" : "确认数字员工运行",
                    1 + (row.WorkflowTaskId.HasValue ? 1 : 0)
                        + (row.WorkflowInstanceId.HasValue ? 1 : 0)
                        + (row.MinutesSourceId.HasValue ? 1 : 0),
                    row.Id.ToString("D"),
                    $"/agents?run={row.Id:D}",
                    row.EnvironmentCode,
                    row.IsTestData,
                    permanent,
                    true,
                    row.CreatedAt,
                    now,
                    timeZone);
                result.Add(new WorkItemAggregate(item, canApproveAgent, true, true));
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<DepartmentDto>> ListDepartmentsAsync(
        UserPrincipal user,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("department", AccessAction.Read);
        return await AccessibleDepartmentsAsync(user, cancellationToken);
    }

    public async Task<DepartmentWorkspaceDto> GetDepartmentWorkspaceAsync(
        UserPrincipal user,
        Guid departmentId,
        bool includeTestData,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("department", AccessAction.Read);
        try
        {
            user.EnsureDepartmentAccess(departmentId);
        }
        catch (PermissionException)
        {
            var departmentExists = await dbContext.Departments.AsNoTracking().AnyAsync(
                item => item.Id == departmentId
                    && item.OrganizationId == user.OrganizationId
                    && item.IsActive,
                cancellationToken);
            if (departmentExists)
            {
                await auditRepository.AddAsync(
                    AuditEvent.Record(
                        user.UserId,
                        "Security",
                        "ReadDenied",
                        nameof(Department),
                        departmentId.ToString("D"),
                        traceId,
                        AuditOutcome.Denied,
                        clock.UtcNow,
                        reason: "部门数据范围拒绝。"),
                    cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            throw;
        }

        var department = await dbContext.Departments.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == departmentId
                    && item.OrganizationId == user.OrganizationId
                    && item.IsActive,
                cancellationToken)
            ?? throw new NotFoundException("部门不存在。");
        var membership = await dbContext.UserDepartmentMemberships.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.UserId == user.UserId && item.DepartmentId == departmentId && item.IsActive,
                cancellationToken);

        var tasks = await LoadTaskRowsAsync(user, [departmentId], includeTestData, cancellationToken);
        var workflows = await LoadInstanceRowsAsync(user, [departmentId], includeTestData, cancellationToken);
        var objects = await LoadObjectDtosAsync(user, departmentId, includeTestData, cancellationToken);
        var files = await LoadRecentFilesAsync(user, [departmentId], includeTestData, cancellationToken);
        var pending = tasks.Where(row => row.Status == nameof(WorkflowTaskStatus.Pending)).ToList();
        return new DepartmentWorkspaceDto(
            new DepartmentDto(
                department.Id.ToString("D"),
                department.Code,
                department.Name,
                department.ManagerUserId?.ToString("D"),
                department.IsActive,
                membership?.IsPrimary ?? false),
            pending.Count,
            pending.Count(row => row.CreatedAt <= clock.UtcNow.AddDays(-3)),
            workflows.Count(row => row.Status is nameof(WorkflowInstanceStatus.Running) or nameof(WorkflowInstanceStatus.Waiting)),
            objects.Count,
            files.Count,
            pending.Select(MapTask).ToList(),
            objects,
            workflows.Select(MapInstance).ToList(),
            files);
    }

    private async Task<IReadOnlyList<DepartmentDto>> AccessibleDepartmentsAsync(
        UserPrincipal user,
        CancellationToken cancellationToken)
    {
        var query =
            from department in dbContext.Departments.AsNoTracking()
            where department.OrganizationId == user.OrganizationId && department.IsActive
            select department;
        if (!user.IsPlatformAdmin)
        {
            var ids = user.DepartmentIds;
            if (ids.Count == 0)
            {
                throw new PermissionException(ErrorCodes.DepartmentScopeDenied, "当前身份没有部门数据范围。");
            }

            query = query.Where(department => ids.Contains(department.Id));
        }

        var departments = await query.OrderBy(item => item.Name).ToListAsync(cancellationToken);
        var memberships = await dbContext.UserDepartmentMemberships.AsNoTracking()
            .Where(item => item.UserId == user.UserId && item.IsActive)
            .ToDictionaryAsync(item => item.DepartmentId, cancellationToken);
        return departments.Select(item => new DepartmentDto(
            item.Id.ToString("D"),
            item.Code,
            item.Name,
            item.ManagerUserId?.ToString("D"),
            item.IsActive,
            memberships.GetValueOrDefault(item.Id)?.IsPrimary ?? false)).ToList();
    }

    private async Task<List<TaskWorkRow>> LoadTaskRowsAsync(
        UserPrincipal user,
        Guid[] departmentIds,
        bool includeTestData,
        CancellationToken cancellationToken)
    {
        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var query =
            from task in dbContext.WorkflowTasks.AsNoTracking()
            join instance in dbContext.WorkflowInstances.AsNoTracking() on task.WorkflowInstanceId equals instance.Id
            join businessObject in dbContext.BusinessObjects.AsNoTracking() on instance.BusinessObjectId equals businessObject.Id
            join definition in dbContext.WorkflowDefinitions.AsNoTracking() on instance.WorkflowDefinitionId equals definition.Id
            join assignee in dbContext.Users.AsNoTracking() on task.AssigneeId equals assignee.Id
            join policy in dbContext.WorkflowNodePolicies.AsNoTracking()
                on task.NodePolicyId equals (Guid?)policy.Id into policies
            from policy in policies.DefaultIfEmpty()
            where !businessObject.IsDeleted
                && (includeTestData && user.IsPlatformAdmin || instance.EnvironmentCode == environment)
                && (user.IsPlatformAdmin || departmentIds.Contains(task.DepartmentId))
            select new
            {
                TaskId = task.Id,
                task.WorkflowInstanceId,
                task.NodeCode,
                task.AssigneeId,
                AssigneeName = assignee.DisplayName,
                task.Status,
                task.CreatedAt,
                task.DepartmentId,
                task.DueAt,
                task.SlaStatus,
                task.DecisionMode,
                PermanentHumanBoundary = policy != null && policy.PermanentHumanBoundary,
                instance.EnvironmentCode,
                instance.IsTestData,
                instance.StartedByUserId,
                ObjectCode = businessObject.ObjectCode,
                BusinessTitle = businessObject.Title ?? businessObject.ObjectCode,
                WorkflowName = definition.Name,
                IsCandidate = dbContext.WorkflowTaskCandidates.Any(candidate =>
                    candidate.WorkflowTaskId == task.Id && candidate.UserId == user.UserId)
            };
        return await query
            .OrderByDescending(row => row.CreatedAt)
            .Select(row => new TaskWorkRow(
                row.TaskId,
                row.WorkflowInstanceId,
                row.NodeCode,
                row.AssigneeId,
                row.AssigneeName,
                row.Status,
                row.CreatedAt,
                row.DepartmentId,
                row.DueAt,
                row.SlaStatus,
                row.DecisionMode,
                row.PermanentHumanBoundary,
                row.EnvironmentCode,
                row.IsTestData,
                row.StartedByUserId,
                row.ObjectCode,
                row.BusinessTitle,
                row.WorkflowName,
                row.IsCandidate))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<InstanceWorkRow>> LoadInstanceRowsAsync(
        UserPrincipal user,
        Guid[] departmentIds,
        bool includeTestData,
        CancellationToken cancellationToken)
    {
        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var isPlatformAdmin = user.IsPlatformAdmin;
        var userId = user.UserId;
        return await (
            from instance in dbContext.WorkflowInstances.AsNoTracking()
            join businessObject in dbContext.BusinessObjects.AsNoTracking() on instance.BusinessObjectId equals businessObject.Id
            join definition in dbContext.WorkflowDefinitions.AsNoTracking() on instance.WorkflowDefinitionId equals definition.Id
            where !businessObject.IsDeleted
                && (includeTestData && user.IsPlatformAdmin || instance.EnvironmentCode == environment)
                && (isPlatformAdmin
                    || departmentIds.Contains(instance.DepartmentId)
                    || dbContext.WorkflowTasks.Any(task =>
                        task.WorkflowInstanceId == instance.Id
                        && departmentIds.Contains(task.DepartmentId)
                        && (task.AssigneeId == userId
                            || dbContext.WorkflowTaskCandidates.Any(candidate =>
                                candidate.WorkflowTaskId == task.Id && candidate.UserId == userId))))
            orderby instance.StartedAt descending
            select new InstanceWorkRow(
                instance.Id,
                instance.Status,
                instance.CurrentNodeCode,
                businessObject.ObjectCode,
                definition.Name,
                instance.DepartmentId,
                instance.StartedByUserId,
                instance.EnvironmentCode,
                instance.IsTestData,
                instance.StartedAt,
                instance.Version))
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<BusinessObjectDto>> LoadObjectDtosAsync(
        UserPrincipal user,
        Guid departmentId,
        bool includeTestData,
        CancellationToken cancellationToken)
    {
        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var entities = await dbContext.BusinessObjects.AsNoTracking()
            .Where(item => !item.IsDeleted
                && item.OrganizationId == user.OrganizationId
                && item.DepartmentId == departmentId
                && (includeTestData && user.IsPlatformAdmin || item.EnvironmentCode == environment))
            .OrderByDescending(item => item.UpdatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);
        var typeIds = entities.Select(item => item.ObjectTypeId).Distinct().ToArray();
        var fields = await dbContext.FieldDefinitions.AsNoTracking()
            .Where(item => typeIds.Contains(item.ObjectTypeId))
            .ToListAsync(cancellationToken);
        var releasedSnapshots = await dbContext.ObjectTypeVersions.AsNoTracking()
            .Where(item => typeIds.Contains(item.ObjectTypeId)
                && (item.Status == ObjectTypeStatus.Published.ToString()
                    || item.Status == ObjectTypeStatus.Superseded.ToString()
                    || item.Status == ObjectTypeStatus.Retired.ToString()))
            .ToListAsync(cancellationToken);
        var historicalCoreKeys = releasedSnapshots
            .SelectMany(snapshot =>
                JsonStorage.DeserializeObjectFields(snapshot.FieldsJson)
                    .Where(field => field.Sensitivity == "Core")
                    .Select(field => (TypeId: snapshot.ObjectTypeId, Key: field.Key)))
            .ToHashSet();
        var fieldByType = fields.GroupBy(item => item.ObjectTypeId)
            .ToDictionary(group => group.Key, group => group.ToDictionary(field => field.Key, StringComparer.OrdinalIgnoreCase));
        return entities.Select(entity =>
        {
            var values = JsonStorage.Deserialize<Dictionary<string, string?>>(entity.FieldsJson)
                ?? new Dictionary<string, string?>();
            if (fieldByType.TryGetValue(entity.ObjectTypeId, out var definitions))
            {
                foreach (var field in definitions.Values)
                {
                    if ((field.Sensitivity == "Core"
                            || historicalCoreKeys.Contains((entity.ObjectTypeId, field.Key)))
                        && !user.IsPlatformAdmin
                        && values.ContainsKey(field.Key))
                    {
                        values[field.Key] = "***";
                    }
                }
            }

            return new BusinessObjectDto(
                entity.Id.ToString("D"),
                entity.ObjectTypeId.ToString("D"),
                entity.ObjectCode,
                entity.Status,
                entity.Version,
                values,
                entity.CreatedAt,
                entity.UpdatedAt,
                entity.DepartmentId.ToString("D"),
                entity.EnvironmentCode,
                entity.IsTestData,
                entity.SourceSystem);
        }).ToList();
    }

    private async Task<IReadOnlyList<FileAttachmentDto>> LoadRecentFilesAsync(
        UserPrincipal user,
        Guid[] departmentIds,
        bool includeTestData,
        CancellationToken cancellationToken)
    {
        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var entities = await dbContext.Attachments.AsNoTracking()
            .Where(item => (user.IsPlatformAdmin || departmentIds.Contains(item.DepartmentId))
                && item.OrganizationId == user.OrganizationId
                && (includeTestData && user.IsPlatformAdmin || item.EnvironmentCode == environment))
            .OrderByDescending(item => item.UpdatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);
        var ids = entities.Select(item => item.Id).ToArray();
        var links = await dbContext.AttachmentLinks.AsNoTracking()
            .Where(item => ids.Contains(item.AttachmentId))
            .ToListAsync(cancellationToken);
        return entities.Select(item => new FileAttachmentDto(
            item.Id.ToString("D"),
            item.DepartmentId.ToString("D"),
            item.OriginalFileName,
            item.MimeType,
            item.SizeBytes,
            item.Sha256,
            item.VirusScanStatus,
            item.Sensitivity,
            item.Status,
            item.EnvironmentCode,
            item.IsTestData,
            item.SourceSystem,
            item.CurrentVersionNumber,
            PreviewKind(item.MimeType),
            links.Where(link => link.AttachmentId == item.Id)
                .Select(link => new FileLinkDto(link.EntityType, link.EntityId.ToString("D")))
                .ToList(),
            item.CreatedAt,
            item.UpdatedAt,
            item.DeletionRequestedBy,
            item.DeletionRequestedAt,
            item.DeletionReason)).ToList();
    }

    private static WorkItemViewDto CreateWorkItem(
        Guid id,
        string sourceType,
        string businessTitle,
        string contextTitle,
        Guid? projectId,
        Guid? departmentId,
        string departmentName,
        string currentStage,
        Guid? assigneeId,
        string assigneeName,
        DateTimeOffset? dueAt,
        string? sourceRisk,
        string status,
        string nextAction,
        int sourceCount,
        string detailTargetId,
        string detailPath,
        string environmentCode,
        bool isTestData,
        bool permanentHumanBoundary,
        bool requiresConfirmation,
        DateTimeOffset occurredAt,
        DateTimeOffset now,
        TimeZoneInfo timeZone)
    {
        var risk = WorkItemPresentation.Risk(
            permanentHumanBoundary,
            dueAt,
            requiresConfirmation,
            sourceRisk,
            now,
            timeZone);
        return new WorkItemViewDto(
            id.ToString("D"),
            sourceType,
            WorkItemPresentation.SourceLabel(sourceType),
            NormalizeDisplayText(businessTitle, "待处理事项"),
            NormalizeDisplayText(contextTitle, "当前业务空间"),
            projectId?.ToString("D"),
            departmentId?.ToString("D"),
            NormalizeDisplayText(departmentName, "当前部门"),
            NormalizeDisplayText(currentStage, "待处理"),
            assigneeId?.ToString("D"),
            NormalizeDisplayText(assigneeName, "待指定"),
            dueAt,
            risk.Level,
            risk.Label,
            WorkItemPresentation.PriorityGroup(
                permanentHumanBoundary,
                dueAt,
                requiresConfirmation,
                now,
                timeZone),
            status,
            WorkItemPresentation.StatusLabel(sourceType, status),
            nextAction,
            Math.Max(1, sourceCount),
            sourceType,
            detailTargetId,
            detailPath,
            environmentCode,
            isTestData,
            permanentHumanBoundary,
            requiresConfirmation,
            occurredAt);
    }

    private static string McpActionTitle(string toolName) =>
        toolName switch
        {
            "aidt_confirm_event" => "确认高风险企业事项",
            "aidt_decide_workflow_task" => "处理高风险流程任务",
            "aidt_transfer_workflow_task" => "转交高风险流程任务",
            "aidt_change_project_status" => "变更项目最终状态",
            "aidt_download_file" => "读取敏感文件正文",
            _ => "确认智能体高风险操作"
        };

    private static string AgentActionLabel(string action) =>
        action switch
        {
            nameof(AgentActionType.Draft) => "生成业务草稿",
            nameof(AgentActionType.Submit) => "提交业务动作",
            nameof(AgentActionType.WriteBack) => "执行受控写回",
            nameof(AgentActionType.Payment) => "付款动作",
            nameof(AgentActionType.QualityRelease) => "质量放行动作",
            nameof(AgentActionType.EquipmentControl) => "设备控制动作",
            _ => "读取业务信息"
        };

    private static bool IsPermanentAgentAction(string action) =>
        action is nameof(AgentActionType.Payment)
            or nameof(AgentActionType.QualityRelease)
            or nameof(AgentActionType.EquipmentControl);

    private static string NormalizeDisplayText(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static WorkItemDto MapTask(TaskWorkRow row) =>
        new(
            row.TaskId.ToString("D"),
            "Task",
            $"{row.BusinessTitle} · {row.WorkflowName}",
            row.ObjectCode,
            row.WorkflowName,
            NodeLabel(row.NodeCode),
            row.AssigneeName,
            row.DueAt,
            row.Status,
            row.Status == nameof(WorkflowTaskStatus.Pending) ? "处理待办" : "查看进度",
            row.EnvironmentCode,
            row.IsTestData,
            row.DepartmentId.ToString("D"),
            row.InstanceId.ToString("D"),
            row.TaskId.ToString("D"));

    private static WorkItemDto MapInstance(InstanceWorkRow row) =>
        new(
            row.InstanceId.ToString("D"),
            "Workflow",
            $"{row.ObjectCode} · {row.WorkflowName}",
            row.ObjectCode,
            row.WorkflowName,
            NodeLabel(row.CurrentNodeCode),
            "流程负责人",
            null,
            row.Status,
            row.Status is nameof(WorkflowInstanceStatus.Running) or nameof(WorkflowInstanceStatus.Waiting)
                ? "查看进度"
                : "查看结果",
            row.EnvironmentCode,
            row.IsTestData,
            row.DepartmentId.ToString("D"),
            row.InstanceId.ToString("D"),
            null);

    private static string NodeLabel(string? nodeCode) =>
        nodeCode switch
        {
            "RD_INITIATE" => "研发立项",
            "QA_REVIEW" => "质量评审",
            "PRODUCTION_RELEASE" => "生产接收",
            "TRIAL_REVIEW" => "试产复核",
            "FINAL_RELEASE" => "转产发布",
            "SHIFT_CONFIRM" => "班组确认",
            "JOINT_REVIEW" => "联合评审",
            "MAJOR_REVIEW" => "重大偏差复核",
            "CAPA_DECISION" => "CAPA 决策",
            "BUDGET_REVIEW" => "预算审核",
            "HIGH_AMOUNT_REVIEW" => "大额复核",
            "PURCHASE_REVIEW" => "采购确认",
            "RECEIPT_CHECK" => "到货验收",
            "PAYMENT_APPROVAL" => "付款审批",
            "COMPLAINT_INTAKE" => "客户投诉受理",
            "BATCH_TRACE" => "批次追溯",
            "JOINT_INVESTIGATION" => "联合调查",
            "CUSTOMER_RESPONSE" => "客户处置与回访",
            null or "" => "已结束",
            _ => "待处理"
        };

    private static string PreviewKind(string mimeType) =>
        mimeType switch
        {
            "application/pdf" => "pdf",
            "image/png" or "image/jpeg" => "image",
            "text/plain" or "text/csv" or "application/csv" or "application/vnd.ms-excel" => "text",
            _ => "unsupported"
        };

    private sealed record TaskWorkRow(
        Guid TaskId,
        Guid InstanceId,
        string NodeCode,
        Guid AssigneeId,
        string AssigneeName,
        string Status,
        DateTimeOffset CreatedAt,
        Guid DepartmentId,
        DateTimeOffset? DueAt,
        string SlaStatus,
        string DecisionMode,
        bool PermanentHumanBoundary,
        string EnvironmentCode,
        bool IsTestData,
        Guid? StartedByUserId,
        string ObjectCode,
        string BusinessTitle,
        string WorkflowName,
        bool IsCandidate);

    private sealed record InstanceWorkRow(
        Guid InstanceId,
        string Status,
        string? CurrentNodeCode,
        string ObjectCode,
        string WorkflowName,
        Guid DepartmentId,
        Guid? StartedByUserId,
        string EnvironmentCode,
        bool IsTestData,
        DateTimeOffset StartedAt,
        long Version);

    private sealed record WorkItemAggregate(
        WorkItemViewDto Item,
        bool IsPending,
        bool IsInitiatedByMe,
        bool IsParticipating);
}

public sealed class SqlDepartmentCommandService(
    AidtDbContext dbContext,
    IAuditRepository auditRepository,
    IUnitOfWork unitOfWork,
    IClock clock) : IDepartmentCommandService
{
    public async Task<DepartmentDto> CreateAsync(
        UserPrincipal user,
        CreateDepartmentRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("department", AccessAction.Create);
        user.EnsureOrganizationAccess(user.OrganizationId);
        var code = DomainChecks.RequiredCode(request.Code, "部门编码", 64);
        var name = DomainChecks.Required(request.Name, "部门名称", 128);
        if (await dbContext.Departments.AnyAsync(
                item => item.OrganizationId == user.OrganizationId && item.Code == code,
                cancellationToken))
        {
            throw new ConflictException(ErrorCodes.Conflict, "部门编码已存在。");
        }

        if (request.ManagerUserId.HasValue)
        {
            var managerExists = await dbContext.Users.AnyAsync(
                item => item.Id == request.ManagerUserId.Value && item.OrganizationId == user.OrganizationId,
                cancellationToken);
            if (!managerExists)
            {
                throw new ValidationException("部门负责人不属于当前组织。");
            }
        }

        var department = Department.Create(user.OrganizationId, code, name, request.ManagerUserId);
        var now = clock.UtcNow;
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        dbContext.Departments.Add(new DepartmentEntity
        {
            Id = department.Id,
            OrganizationId = department.OrganizationId,
            Code = department.Code,
            Name = department.Name,
            ManagerUserId = department.ManagerUserId,
            IsActive = department.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        });
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Identity",
                "CreateDepartment",
                nameof(Department),
                department.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                after: $"{{\"code\":\"{department.Code}\",\"name\":\"{department.Name}\"}}"),
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new DepartmentDto(
            department.Id.ToString("D"),
            department.Code,
            department.Name,
            department.ManagerUserId?.ToString("D"),
            department.IsActive,
            false);
    }

    public async Task AddMemberAsync(
        UserPrincipal user,
        Guid departmentId,
        AddDepartmentMemberRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("department", AccessAction.Update);
        user.EnsureOrganizationAccess(user.OrganizationId);
        var department = await dbContext.Departments.SingleOrDefaultAsync(
            item => item.Id == departmentId && item.OrganizationId == user.OrganizationId,
            cancellationToken) ?? throw new NotFoundException("部门不存在。");
        var member = await dbContext.Users.SingleOrDefaultAsync(
            item => item.Id == request.UserId
                && item.OrganizationId == user.OrganizationId
                && item.IsActive,
            cancellationToken) ?? throw new NotFoundException("用户不存在。");
        var now = clock.UtcNow;
        if (request.IsPrimary)
        {
            var currentPrimary = await dbContext.UserDepartmentMemberships
                .Where(item => item.UserId == member.Id && item.IsPrimary)
                .ToListAsync(cancellationToken);
            foreach (var membership in currentPrimary)
            {
                membership.IsPrimary = false;
            }
        }

        var existing = await dbContext.UserDepartmentMemberships.SingleOrDefaultAsync(
            item => item.UserId == member.Id && item.DepartmentId == departmentId,
            cancellationToken);
        if (existing is null)
        {
            dbContext.UserDepartmentMemberships.Add(new UserDepartmentMembershipEntity
            {
                UserId = member.Id,
                DepartmentId = departmentId,
                IsPrimary = request.IsPrimary,
                IsManager = request.IsManager,
                IsActive = true,
                JoinedAt = now
            });
        }
        else
        {
            existing.IsPrimary = request.IsPrimary;
            existing.IsManager = request.IsManager;
            existing.IsActive = true;
        }

        if (!await dbContext.DataScopes.AnyAsync(
                item => item.UserId == member.Id && item.ScopeType == "department"
                    && item.ScopeReference == departmentId.ToString("D"),
                cancellationToken))
        {
            dbContext.DataScopes.Add(new DataScopeEntity
            {
                Id = Guid.NewGuid(),
                UserId = member.Id,
                ScopeType = "department",
                ScopeReference = departmentId.ToString("D")
            });
        }

        if (request.IsManager)
        {
            department.ManagerUserId = member.Id;
        }
        else if (department.ManagerUserId == member.Id)
        {
            department.ManagerUserId = null;
        }

        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Identity",
                "AddDepartmentMember",
                nameof(Department),
                departmentId.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                after: $"{{\"userId\":\"{member.Id:D}\",\"isPrimary\":{request.IsPrimary.ToString().ToLowerInvariant()},\"isManager\":{request.IsManager.ToString().ToLowerInvariant()}}}"),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
