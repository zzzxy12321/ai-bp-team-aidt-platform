using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using System.Globalization;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;
using AIDT.Infrastructure.Persistence;
using AIDT.Infrastructure.Persistence.Repositories;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

using AIDT.Domain.Audit;
using AIDT.Domain.BusinessObjects;
using AIDT.Domain.Workflows;

namespace AIDT.Infrastructure.Queries;

public sealed class SqlWorkspaceQueryService(
    AidtDbContext dbContext,
    DataEnvironmentOptions environmentOptions,
    IAuditRepository auditRepository,
    IClock clock) : IWorkspaceQueryService
{
    public async Task<PagedResult<ObjectTypeDto>> ListObjectTypesAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        EnsureRead(user, "object-type");
        if (!user.HasPermission("object-model", AccessAction.Read))
        {
            var publishedItems = await LoadPublishedObjectTypesAsync(cancellationToken);
            return new PagedResult<ObjectTypeDto>(
                publishedItems.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                page,
                pageSize,
                publishedItems.Count);
        }

        var query = dbContext.ObjectTypes.AsNoTracking().OrderBy(item => item.Code);
        var total = await query.LongCountAsync(cancellationToken);
        var entities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var ids = entities.Select(item => item.Id).ToArray();
        var fields = await dbContext.FieldDefinitions.AsNoTracking()
            .Where(item => ids.Contains(item.ObjectTypeId))
            .OrderBy(item => item.Key)
            .ToListAsync(cancellationToken);
        var fieldsByType = fields.GroupBy(item => item.ObjectTypeId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var items = entities.Select(item => new ObjectTypeDto(
            item.Id.ToString("D"),
            item.Code,
            item.Name,
            item.Version,
            (fieldsByType.GetValueOrDefault(item.Id) ?? [])
                .OrderBy(field => field.SortOrder)
                .ThenBy(field => field.Key)
                .Select(field => new ObjectFieldDto(
                    field.Key,
                    field.DisplayName,
                    ObjectFieldTypes.Normalize(field.DataType),
                    field.IsRequired,
                    field.MaxLength,
                    field.Sensitivity,
                    field.DefaultValue,
                    field.OptionsJson is null
                        ? []
                        : JsonStorage.Deserialize<List<string>>(field.OptionsJson) ?? [],
                    field.ShowInList,
                    field.Filterable,
                    field.SortOrder,
                    field.Key))
                .ToList(),
            item.Status,
            item.Revision,
            item.PublishedAt,
            item.RetiredAt))
            .ToList();
        return new PagedResult<ObjectTypeDto>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<ObjectTypeDto>> ListPublishedObjectTypesAsync(
        UserPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!user.HasPermission("object-type", AccessAction.Read)
            && !user.HasPermission("object-model", AccessAction.Read)
            && !user.HasPermission("business-object", AccessAction.Read))
        {
            throw new PermissionException(
                ErrorCodes.PermissionDenied,
                "当前身份没有执行该动作的权限。");
        }
        return await LoadPublishedObjectTypesAsync(cancellationToken);
    }

    public async Task<PagedResult<BusinessObjectDto>> ListBusinessObjectsAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        bool includeTestData,
        CancellationToken cancellationToken)
    {
        EnsureRead(user, "business-object");
        EnsureOrganizationScope(user);
        var departmentIds = EnsureDepartmentScopes(user);
        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var includeAllEnvironments = user.IsPlatformAdmin && includeTestData;
        var isPlatformAdmin = user.IsPlatformAdmin;
        var userId = user.UserId;
        var query = dbContext.BusinessObjects.AsNoTracking()
            .Where(item => !item.IsDeleted
                && item.OrganizationId == user.OrganizationId
                && (user.IsPlatformAdmin || departmentIds.Contains(item.DepartmentId))
                && (includeAllEnvironments || item.EnvironmentCode == environment))
            .OrderByDescending(item => item.UpdatedAt)
            .ThenByDescending(item => item.Id);
        var total = await query.LongCountAsync(cancellationToken);
        var entities = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var items = await MapBusinessObjectsAsync(entities, user, cancellationToken);
        return new PagedResult<BusinessObjectDto>(items, page, pageSize, total);
    }

    public async Task<BusinessObjectDto?> GetBusinessObjectAsync(
        UserPrincipal user,
        Guid id,
        bool includeTestData,
        string traceId,
        CancellationToken cancellationToken)
    {
        EnsureRead(user, "business-object");
        EnsureOrganizationScope(user);
        var departmentIds = EnsureDepartmentScopes(user);
        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var includeAllEnvironments = user.IsPlatformAdmin && includeTestData;
        var entity = await dbContext.BusinessObjects.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == id
                && !item.IsDeleted
                && item.OrganizationId == user.OrganizationId
                && (user.IsPlatformAdmin || departmentIds.Contains(item.DepartmentId))
                && (includeAllEnvironments || item.EnvironmentCode == environment),
            cancellationToken);
        if (entity is not null)
        {
            return (await MapBusinessObjectsAsync([entity], user, cancellationToken)).Single();
        }

        var exists = await dbContext.BusinessObjects.AsNoTracking().AnyAsync(
            item => item.Id == id
                && item.OrganizationId == user.OrganizationId
                && !item.IsDeleted,
            cancellationToken);
        if (exists)
        {
            await RecordDeniedAsync(user, nameof(BusinessObject), id, traceId, cancellationToken);
        }

        return null;
    }

    public async Task<IReadOnlyList<BusinessObjectVersionDto>?> GetBusinessObjectVersionsAsync(
        UserPrincipal user,
        Guid id,
        bool includeTestData,
        string traceId,
        CancellationToken cancellationToken)
    {
        var businessObject = await GetBusinessObjectAsync(
            user,
            id,
            includeTestData,
            traceId,
            cancellationToken);
        if (businessObject is null)
        {
            return null;
        }

        var coreKeys = await LoadCoreFieldKeysAsync(
            Guid.Parse(businessObject.ObjectTypeId),
            cancellationToken);
        var versions = await dbContext.BusinessObjectVersions.AsNoTracking()
            .Where(item => item.BusinessObjectId == id)
            .OrderByDescending(item => item.Version)
            .ToListAsync(cancellationToken);
        return versions.Select(version =>
        {
            var fields = ParseBusinessObjectVersionFields(version.SnapshotJson);
            if (!user.IsPlatformAdmin)
            {
                foreach (var key in coreKeys.Where(fields.ContainsKey))
                {
                    fields[key] = "***";
                }
            }

            return new BusinessObjectVersionDto(
                version.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                version.BusinessObjectId.ToString("D"),
                version.Version,
                GetSnapshotString(version.SnapshotJson, "status") ?? "Unknown",
                fields,
                version.ActorId == Guid.Empty ? null : version.ActorId.ToString("D"),
                version.Reason,
                version.CreatedAt);
        }).ToList();
    }

    public async Task<BusinessObjectContextDto?> GetBusinessObjectContextAsync(
        UserPrincipal user,
        Guid id,
        bool includeTestData,
        string traceId,
        CancellationToken cancellationToken)
    {
        var businessObject = await GetBusinessObjectAsync(
            user,
            id,
            includeTestData,
            traceId,
            cancellationToken);
        if (businessObject is null)
        {
            return null;
        }

        var departmentId = Guid.Parse(businessObject.DepartmentId);
        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var includeAllEnvironments = user.IsPlatformAdmin && includeTestData;
        var projects =
            from relation in dbContext.ProjectRelations.AsNoTracking()
            join project in dbContext.BusinessObjects.AsNoTracking()
                on relation.ProjectId equals project.Id
            where relation.EntityType == "BusinessObject"
                && relation.EntityId == id
                && project.OrganizationId == user.OrganizationId
                && project.DepartmentId == departmentId
                && !project.IsDeleted
                && (includeAllEnvironments || project.EnvironmentCode == environment)
            orderby relation.CreatedAt descending
            select new BusinessObjectProjectLinkDto(
                project.Id.ToString("D"),
                relation.Id.ToString("D"),
                project.ObjectCode,
                project.Title ?? project.ObjectCode,
                project.Status,
                relation.RelationCode,
                relation.EvidenceExcerpt,
                relation.CreatedAt);

        var workflowRows = await dbContext.WorkflowInstances.AsNoTracking()
            .Where(item => item.BusinessObjectId == id
                && item.DepartmentId == departmentId
                && (includeAllEnvironments || item.EnvironmentCode == environment))
            .OrderByDescending(item => item.StartedAt)
            .ToListAsync(cancellationToken);
        var workflows = workflowRows.Select(item => new WorkflowInstanceDto(
            item.Id.ToString("D"),
            item.WorkflowDefinitionId.ToString("D"),
            item.WorkflowVersionId.ToString("D"),
            item.BusinessObjectId.ToString("D"),
            Enum.Parse<WorkflowInstanceStatus>(item.Status, true),
            item.Version,
            item.CurrentNodeCode,
            item.StartedAt,
            item.EndedAt,
            item.DepartmentId.ToString("D"),
            item.StartedByUserId?.ToString("D"),
            item.EnvironmentCode,
            item.IsTestData,
            item.SourceSystem)).ToList();

        var linkedFileIds = dbContext.AttachmentLinks.AsNoTracking()
            .Where(item => item.EntityType == "BusinessObject" && item.EntityId == id)
            .Select(item => item.AttachmentId);
        var files = await dbContext.Attachments.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId
                && item.DepartmentId == departmentId
                && (item.BusinessObjectId == id || linkedFileIds.Contains(item.Id))
                && (includeAllEnvironments || item.EnvironmentCode == environment))
            .OrderByDescending(item => item.UpdatedAt)
            .Select(item => new BusinessObjectFileLinkDto(
                item.Id.ToString("D"),
                item.OriginalFileName,
                item.MimeType,
                item.SizeBytes,
                item.Sensitivity,
                item.Status,
                item.CurrentVersionNumber,
                item.CreatedAt))
            .ToListAsync(cancellationToken);

        var auditRows = await dbContext.AuditEvents.AsNoTracking()
            .Where(item => item.EntityType == "BusinessObject" && item.EntityId == id.ToString("D"))
            .OrderByDescending(item => item.OccurredAt)
            .Take(30)
            .ToListAsync(cancellationToken);
        var auditEvents = auditRows.Select(item => new AuditEventDto(
            item.Id.ToString("D"),
            item.ActorId.ToString("D"),
            item.Module,
            item.Action,
            item.EntityType,
            item.EntityId,
            item.TraceId,
            Enum.Parse<AuditOutcome>(item.Outcome),
            item.OccurredAt,
            item.Reason)).ToList();

        return new BusinessObjectContextDto(
            await projects.ToListAsync(cancellationToken),
            workflows,
            files,
            auditEvents);
    }

    public async Task<IReadOnlyList<ObjectFieldUserOptionDto>> ListObjectFieldUsersAsync(
        UserPrincipal user,
        string? search,
        Guid? departmentId,
        int limit,
        CancellationToken cancellationToken)
    {
        EnsureRead(user, "business-object");
        EnsureOrganizationScope(user);
        var visibleDepartments = EnsureDepartmentScopes(user);
        if (departmentId.HasValue)
        {
            user.EnsureDepartmentAccess(departmentId.Value);
        }

        var query =
            from account in dbContext.Users.AsNoTracking()
            join membership in dbContext.UserDepartmentMemberships.AsNoTracking()
                on account.Id equals membership.UserId
            join department in dbContext.Departments.AsNoTracking()
                on membership.DepartmentId equals department.Id
            where account.OrganizationId == user.OrganizationId
                && account.IsActive
                && !account.IsServiceIdentity
                && membership.IsActive
                && department.IsActive
                && (departmentId.HasValue
                    ? membership.DepartmentId == departmentId.Value
                    : user.IsPlatformAdmin || visibleDepartments.Contains(membership.DepartmentId))
            select new
            {
                account.Id,
                account.UserName,
                account.DisplayName,
                DepartmentId = department.Id,
                DepartmentName = department.Name
            };
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(item =>
                item.UserName.Contains(keyword) || item.DisplayName.Contains(keyword));
        }

        return await query
            .OrderBy(item => item.DisplayName)
            .Take(Math.Clamp(limit, 1, 100))
            .Select(item => new ObjectFieldUserOptionDto(
                item.Id.ToString("D"),
                item.UserName,
                item.DisplayName,
                item.DepartmentId.ToString("D"),
                item.DepartmentName))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<WorkflowInstanceDto>> ListWorkflowInstancesAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        string? status,
        bool includeTestData,
        CancellationToken cancellationToken)
    {
        EnsureRead(user, "workflow-instance");
        EnsureOrganizationScope(user);
        var departmentIds = EnsureDepartmentScopes(user);
        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var includeAllEnvironments = user.IsPlatformAdmin && includeTestData;
        var isPlatformAdmin = user.IsPlatformAdmin;
        var userId = user.UserId;
        var query =
            from instance in dbContext.WorkflowInstances.AsNoTracking()
            join businessObject in dbContext.BusinessObjects.AsNoTracking()
                on instance.BusinessObjectId equals businessObject.Id
            where businessObject.OrganizationId == user.OrganizationId
                && !businessObject.IsDeleted
                && (isPlatformAdmin
                    || departmentIds.Contains(instance.DepartmentId)
                    || dbContext.WorkflowTasks.Any(task =>
                        task.WorkflowInstanceId == instance.Id
                        && departmentIds.Contains(task.DepartmentId)
                        && (task.AssigneeId == userId
                            || dbContext.WorkflowTaskCandidates.Any(candidate =>
                                candidate.WorkflowTaskId == task.Id && candidate.UserId == userId))))
                && (includeAllEnvironments || instance.EnvironmentCode == environment)
            select instance;
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(item => item.Status == status);
        }

        query = query.OrderByDescending(item => item.StartedAt).ThenByDescending(item => item.Id);
        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new WorkflowInstanceDto(
                item.Id.ToString("D"),
                item.WorkflowDefinitionId.ToString("D"),
                item.WorkflowVersionId.ToString("D"),
                item.BusinessObjectId.ToString("D"),
                Enum.Parse<Domain.Workflows.WorkflowInstanceStatus>(item.Status),
                item.Version,
                item.CurrentNodeCode,
                item.StartedAt,
                item.EndedAt,
                item.DepartmentId.ToString("D"),
                item.StartedByUserId == null ? null : item.StartedByUserId.Value.ToString("D"),
                item.EnvironmentCode,
                item.IsTestData,
                item.SourceSystem))
            .ToListAsync(cancellationToken);
        return new PagedResult<WorkflowInstanceDto>(items, page, pageSize, total);
    }

    public async Task<WorkflowInstanceDto?> GetWorkflowInstanceAsync(
        UserPrincipal user,
        Guid id,
        bool includeTestData,
        string traceId,
        CancellationToken cancellationToken)
    {
        EnsureRead(user, "workflow-instance");
        EnsureOrganizationScope(user);
        var departmentIds = EnsureDepartmentScopes(user);
        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var includeAllEnvironments = user.IsPlatformAdmin && includeTestData;
        var isPlatformAdmin = user.IsPlatformAdmin;
        var userId = user.UserId;
        var result = await (
            from instance in dbContext.WorkflowInstances.AsNoTracking()
            join businessObject in dbContext.BusinessObjects.AsNoTracking()
                on instance.BusinessObjectId equals businessObject.Id
            where instance.Id == id
                && businessObject.OrganizationId == user.OrganizationId
                && !businessObject.IsDeleted
                && (isPlatformAdmin
                    || departmentIds.Contains(instance.DepartmentId)
                    || dbContext.WorkflowTasks.Any(task =>
                        task.WorkflowInstanceId == instance.Id
                        && departmentIds.Contains(task.DepartmentId)
                        && (task.AssigneeId == userId
                            || dbContext.WorkflowTaskCandidates.Any(candidate =>
                                candidate.WorkflowTaskId == task.Id && candidate.UserId == userId))))
                && (includeAllEnvironments || instance.EnvironmentCode == environment)
            select new WorkflowInstanceDto(
                instance.Id.ToString("D"),
                instance.WorkflowDefinitionId.ToString("D"),
                instance.WorkflowVersionId.ToString("D"),
                instance.BusinessObjectId.ToString("D"),
                Enum.Parse<Domain.Workflows.WorkflowInstanceStatus>(instance.Status),
                instance.Version,
                instance.CurrentNodeCode,
                instance.StartedAt,
                instance.EndedAt,
                instance.DepartmentId.ToString("D"),
                instance.StartedByUserId == null ? null : instance.StartedByUserId.Value.ToString("D"),
                instance.EnvironmentCode,
                instance.IsTestData,
                instance.SourceSystem))
            .SingleOrDefaultAsync(cancellationToken);
        if (result is not null)
        {
            return result;
        }

        var exists = await (
            from instance in dbContext.WorkflowInstances.AsNoTracking()
            join businessObject in dbContext.BusinessObjects.AsNoTracking()
                on instance.BusinessObjectId equals businessObject.Id
            where instance.Id == id && businessObject.OrganizationId == user.OrganizationId
            select instance.Id)
            .AnyAsync(cancellationToken);
        if (exists)
        {
            await RecordDeniedAsync(user, nameof(WorkflowInstance), id, traceId, cancellationToken);
        }

        return null;
    }

    public async Task<IReadOnlyList<WorkflowDefinitionDto>> ListWorkflowDefinitionsAsync(
        UserPrincipal user,
        CancellationToken cancellationToken)
    {
        EnsureRead(user, "workflow-instance");
        var definitions = await (
            from version in dbContext.WorkflowVersions.AsNoTracking()
            join definition in dbContext.WorkflowDefinitions.AsNoTracking()
                on version.WorkflowDefinitionId equals definition.Id
            where version.Status == "Published" && definition.Status == "Published"
            orderby definition.Code, version.VersionNumber descending
            select new { definition, version }).ToListAsync(cancellationToken);
        return definitions.Select(item =>
        {
            var objectTypeCode = "ANY";
            try
            {
                using var document = JsonDocument.Parse(item.version.DefinitionJson);
                if (document.RootElement.TryGetProperty("objectTypeCode", out var code))
                {
                    objectTypeCode = code.GetString() ?? "ANY";
                }
            }
            catch (JsonException)
            {
                objectTypeCode = "INVALID";
            }

            return new WorkflowDefinitionDto(
                item.definition.Id.ToString("D"),
                item.version.Id.ToString("D"),
                item.definition.Code,
                item.definition.Name,
                item.version.VersionNumber,
                objectTypeCode);
        }).ToList();
    }

    public async Task<PagedResult<WorkflowTaskDto>> ListTasksAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        string? status,
        bool includeTestData,
        CancellationToken cancellationToken)
    {
        EnsureRead(user, "workflow-task");
        var departmentIds = EnsureDepartmentScopes(user);
        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var isReviewer = user.IsPlatformAdmin;
        var userId = user.UserId;
        var includeAllEnvironments = user.IsPlatformAdmin && includeTestData;
        var query = dbContext.WorkflowTasks.AsNoTracking()
            .Where(item => (isReviewer
                    || item.AssigneeId == userId
                    || dbContext.WorkflowTaskCandidates.Any(candidate =>
                        candidate.WorkflowTaskId == item.Id && candidate.UserId == userId))
                && (isReviewer || departmentIds.Contains(item.DepartmentId))
                && (includeAllEnvironments || item.EnvironmentCode == environment));
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(item => item.Status == status);
        }

        query = query.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id);
        var total = await query.LongCountAsync(cancellationToken);
        var taskEntities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var items = taskEntities.Select(item => new WorkflowTaskDto(
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
            JsonStorage.Deserialize<string[]>(item.CandidateUserIdsJson) ?? [],
            item.OriginalAssigneeId?.ToString("D"),
            item.BackupAssigneeId?.ToString("D"),
            item.TransferredBy?.ToString("D"),
            item.TransferredAt,
            item.TransferReason,
            item.NodeName,
            item.FormDefinitionJson,
            item.FormDataJson,
            item.FormSchemaVersion,
            item.FormDefinitionHash)).ToList();
        return new PagedResult<WorkflowTaskDto>(items, page, pageSize, total);
    }

    public async Task<Guid?> GetTaskDepartmentAsync(
        UserPrincipal user,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        EnsureRead(user, "workflow-task");
        var departmentIds = EnsureDepartmentScopes(user);
        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var isReviewer = user.IsPlatformAdmin;
        var userId = user.UserId;
        return await dbContext.WorkflowTasks
            .AsNoTracking()
            .Where(item => item.Id == taskId
                && (isReviewer
                    || item.AssigneeId == userId
                    || dbContext.WorkflowTaskCandidates.Any(candidate =>
                        candidate.WorkflowTaskId == item.Id && candidate.UserId == userId))
                && (isReviewer || departmentIds.Contains(item.DepartmentId))
                && item.EnvironmentCode == environment)
            .Select(item => (Guid?)item.DepartmentId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowTimelineItemDto>> GetWorkflowTimelineAsync(
        UserPrincipal user,
        Guid instanceId,
        bool includeTestData,
        string traceId,
        CancellationToken cancellationToken)
    {
        EnsureRead(user, "workflow-instance");
        _ = await GetWorkflowInstanceAsync(user, instanceId, includeTestData, traceId, cancellationToken)
            ?? throw new NotFoundException("流程实例不存在。");
        var taskIds = await dbContext.WorkflowTasks.AsNoTracking()
            .Where(task => task.WorkflowInstanceId == instanceId)
            .Select(task => task.Id)
            .ToListAsync(cancellationToken);
        var actions = await dbContext.WorkflowTaskActions.AsNoTracking()
            .Where(action => taskIds.Contains(action.WorkflowTaskId))
            .OrderBy(action => action.CreatedAt)
            .Select(action => new WorkflowTimelineItemDto(
                action.Id.ToString(CultureInfo.InvariantCulture),
                "TaskAction",
                action.Action,
                action.Comment,
                action.ActorId.ToString("D"),
                action.CreatedAt))
            .ToListAsync(cancellationToken);
        var taskEntityIds = taskIds.Select(id => id.ToString("D")).ToArray();
        var audits = await dbContext.AuditEvents.AsNoTracking()
            .Where(audit => audit.EntityType == "WorkflowTask"
                && taskEntityIds.Contains(audit.EntityId))
            .OrderBy(audit => audit.OccurredAt)
            .Select(audit => new WorkflowTimelineItemDto(
                audit.Id.ToString("D"),
                "Audit",
                audit.Action,
                audit.Reason,
                audit.ActorId.ToString("D"),
                audit.OccurredAt))
            .ToListAsync(cancellationToken);
        return actions.Concat(audits).OrderBy(item => item.OccurredAt).ToList();
    }

    public async Task<PagedResult<AuditEventDto>> ListAuditEventsAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        string? entityType,
        string? entityId,
        CancellationToken cancellationToken)
    {
        EnsureRead(user, "audit");
        var organizationUserIds = await dbContext.Users.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        var query = dbContext.AuditEvents.AsNoTracking()
            .Where(item => organizationUserIds.Contains(item.ActorId));
        if (!user.IsPlatformAdmin)
        {
            var departmentIds = EnsureDepartmentScopes(user);
            var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
            var departmentEntityIds = departmentIds
                .Select(id => id.ToString("D"))
                .ToArray();
            var objectIds = await dbContext.BusinessObjects.AsNoTracking()
                .Where(item => departmentIds.Contains(item.DepartmentId)
                    && item.EnvironmentCode == environment
                    && !item.IsDeleted)
                .Select(item => item.Id.ToString("D"))
                .ToListAsync(cancellationToken);
            var instanceIds = await dbContext.WorkflowInstances.AsNoTracking()
                .Where(item => departmentIds.Contains(item.DepartmentId)
                    && item.EnvironmentCode == environment)
                .Select(item => item.Id.ToString("D"))
                .ToListAsync(cancellationToken);
            var taskIds = await dbContext.WorkflowTasks.AsNoTracking()
                .Where(item => departmentIds.Contains(item.DepartmentId)
                    && item.EnvironmentCode == environment)
                .Select(item => item.Id.ToString("D"))
                .ToListAsync(cancellationToken);
            var fileIds = await dbContext.Attachments.AsNoTracking()
                .Where(item => departmentIds.Contains(item.DepartmentId)
                    && item.EnvironmentCode == environment)
                .Select(item => item.Id.ToString("D"))
                .ToListAsync(cancellationToken);
            query = query.Where(item =>
                item.ActorId == user.UserId
                || item.EntityType == "Department" && departmentEntityIds.Contains(item.EntityId)
                || item.EntityType == "BusinessObject" && objectIds.Contains(item.EntityId)
                || item.EntityType == "WorkflowInstance" && instanceIds.Contains(item.EntityId)
                || item.EntityType == "WorkflowTask" && taskIds.Contains(item.EntityId)
                || item.EntityType == "FileAttachment" && fileIds.Contains(item.EntityId));
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(item => item.EntityType == entityType);
        }

        if (!string.IsNullOrWhiteSpace(entityId))
        {
            query = query.Where(item => item.EntityId == entityId);
        }

        query = query.OrderByDescending(item => item.OccurredAt).ThenByDescending(item => item.Id);
        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new AuditEventDto(
                item.Id.ToString("D"),
                item.ActorId.ToString("D"),
                item.Module,
                item.Action,
                item.EntityType,
                item.EntityId,
                item.TraceId,
                Enum.Parse<Domain.Audit.AuditOutcome>(item.Outcome),
                item.OccurredAt,
                item.Reason))
            .ToListAsync(cancellationToken);
        return new PagedResult<AuditEventDto>(items, page, pageSize, total);
    }

    public async Task<PagedResult<AgentRunDto>> ListAgentRunsAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        string? status,
        CancellationToken cancellationToken)
    {
        EnsureRead(user, "agent");
        user.EnsureOrganizationAccess(user.OrganizationId);
        var departmentIds = user.DepartmentIds;
        var query = dbContext.AgentRuns.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId
                && (user.IsPlatformAdmin || departmentIds.Contains(item.DepartmentId)));
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(item => item.Status == status);
        }

        query = query.OrderByDescending(item => item.CreatedAt);
        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new AgentRunDto(
                item.Id.ToString("D"),
                item.AgentProfileId.ToString("D"),
                item.RequestedBy.ToString("D"),
                item.Action,
                item.Amount,
                Enum.Parse<Domain.Agents.AgentRiskLevel>(item.RiskLevel),
                Enum.Parse<Domain.Agents.AgentRunStatus>(item.Status),
                item.ConfirmationId == null ? null : item.ConfirmationId.Value.ToString("D"),
                item.CreatedAt,
                item.OrganizationId.ToString("D"),
                item.DepartmentId.ToString("D"),
                item.EnvironmentCode,
                item.IsTestData,
                item.SubjectType,
                item.BusinessObjectId == null ? null : item.BusinessObjectId.Value.ToString("D"),
                item.WorkflowInstanceId == null ? null : item.WorkflowInstanceId.Value.ToString("D"),
                item.WorkflowTaskId == null ? null : item.WorkflowTaskId.Value.ToString("D"),
                item.MinutesSourceId == null ? null : item.MinutesSourceId.Value.ToString("D"),
                item.Model,
                item.GatewayCallId == null ? null : item.GatewayCallId.Value.ToString("D"),
                item.GatewayMode,
                item.ExternalModelCalled))
            .ToListAsync(cancellationToken);
        return new PagedResult<AgentRunDto>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<AgentProfileDto>> ListAgentProfilesAsync(
        UserPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!user.IsPlatformAdmin
            && !user.HasPermission("agent", AccessAction.Read)
            && !user.HasPermission("configuration", AccessAction.Read)
            && !user.HasPermission("configuration", AccessAction.Update))
        {
            throw new PermissionException(
                ErrorCodes.PermissionDenied,
                "当前身份没有查看数字员工候选的权限。");
        }

        var organizationDepartmentIds = dbContext.Departments.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId)
            .Select(item => item.Id);
        var profiles = await dbContext.AgentProfiles.AsNoTracking()
            .Where(item =>
                item.LifecycleStatus == "Published"
                && item.IsActive
                && item.CurrentPublishedVersionId != null
                && organizationDepartmentIds.Contains(item.DepartmentId)
                && (user.IsPlatformAdmin || user.DepartmentIds.Contains(item.DepartmentId)))
            .OrderBy(item => item.Id)
            .ToListAsync(cancellationToken);
        return profiles.Select(item => new AgentProfileDto(
            item.Id.ToString("D"),
            item.Name,
            Enum.Parse<Domain.Agents.AgentAutonomyLevel>(item.AutonomyLevel),
            item.AmountLimit,
            Enum.Parse<Domain.Agents.AgentRiskLevel>(item.RiskLimit),
            JsonStorage.Deserialize<string[]>(item.AllowedToolsJson) ?? [],
            item.IsActive,
            item.ServiceUserId.ToString("D"),
            item.Code,
            item.DepartmentId.ToString("D"),
            item.Description)).ToList();
    }

    public async Task<IReadOnlyList<ConnectorDto>> ListConnectorsAsync(
        UserPrincipal user,
        CancellationToken cancellationToken)
    {
        EnsureRead(user, "connector");
        return await dbContext.Connectors.AsNoTracking()
            .OrderBy(item => item.Code)
            .Select(item => new ConnectorDto(
                item.Id.ToString("D"),
                item.Code,
                item.Name,
                item.AdapterType,
                item.ConnectionStatus,
                item.AuthorityOwner))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<OutboxMessageDto>> ListOutboxMessagesAsync(
        UserPrincipal user,
        Guid connectorId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        EnsureRead(user, "connector");
        var connectorExists = await dbContext.Connectors.AsNoTracking()
            .AnyAsync(item => item.Id == connectorId, cancellationToken);
        if (!connectorExists)
        {
            throw new NotFoundException("连接器不存在。");
        }

        var query = dbContext.OutboxEvents.AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id);
        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new OutboxMessageDto(
                item.Id.ToString("D"),
                item.EventType,
                item.AggregateType,
                item.AggregateId.ToString("D"),
                item.IdempotencyKey,
                item.Status,
                item.AttemptCount,
                item.CreatedAt))
            .ToListAsync(cancellationToken);
        return new PagedResult<OutboxMessageDto>(items, page, pageSize, total);
    }

    public async Task<DashboardDto> GetDashboardAsync(
        UserPrincipal user,
        bool includeTestData,
        CancellationToken cancellationToken)
    {
        EnsureRead(user, "dashboard");
        EnsureOrganizationScope(user);
        var departmentIds = EnsureDepartmentScopes(user);
        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var includeAllEnvironments = user.IsPlatformAdmin && includeTestData;
        var objectIds = dbContext.BusinessObjects.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId
                && !item.IsDeleted
                && (user.IsPlatformAdmin || departmentIds.Contains(item.DepartmentId))
                && (includeAllEnvironments || item.EnvironmentCode == environment))
            .Select(item => item.Id);
        var todayUtc = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var pending = await dbContext.WorkflowTasks.AsNoTracking().LongCountAsync(
            item => item.AssigneeId == user.UserId
                && item.Status == "Pending"
                && (user.IsPlatformAdmin || departmentIds.Contains(item.DepartmentId))
                && (includeAllEnvironments || item.EnvironmentCode == environment),
            cancellationToken);
        var running = await dbContext.WorkflowInstances.AsNoTracking().LongCountAsync(
            item => objectIds.Contains(item.BusinessObjectId)
                && (item.Status == "Running" || item.Status == "Waiting")
                && (includeAllEnvironments || item.EnvironmentCode == environment),
            cancellationToken);
        var objects = await objectIds.LongCountAsync(cancellationToken);
        var audits = await dbContext.AuditEvents.AsNoTracking().LongCountAsync(
            item => item.OccurredAt >= todayUtc
                && dbContext.Users.Any(userRow =>
                    userRow.Id == item.ActorId && userRow.OrganizationId == user.OrganizationId),
            cancellationToken);
        var confirmations = await dbContext.AgentRuns.AsNoTracking().LongCountAsync(
            item => item.RequestedBy == user.UserId && item.Status == "AwaitingConfirmation",
            cancellationToken);
        return new DashboardDto(
            checked((int)pending),
            checked((int)running),
            checked((int)objects),
            checked((int)audits),
            checked((int)confirmations),
            "available");
    }

    private async Task<List<BusinessObjectDto>> MapBusinessObjectsAsync(
        IReadOnlyCollection<BusinessObjectEntity> entities,
        UserPrincipal user,
        CancellationToken cancellationToken)
    {
        var typeIds = entities.Select(item => item.ObjectTypeId).Distinct().ToArray();
        var currentTypes = await dbContext.ObjectTypes.AsNoTracking()
            .Where(item => typeIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var currentFieldEntities = await dbContext.FieldDefinitions.AsNoTracking()
            .Where(item => typeIds.Contains(item.ObjectTypeId))
            .ToListAsync(cancellationToken);
        var nonPublishedIds = currentTypes
            .Where(item => item.Value.Status != ObjectTypeStatus.Published.ToString())
            .Select(item => item.Key)
            .ToArray();
        var snapshots = await dbContext.ObjectTypeVersions.AsNoTracking()
            .Where(item => nonPublishedIds.Contains(item.ObjectTypeId)
                && (item.Status == ObjectTypeStatus.Published.ToString()
                    || item.Status == ObjectTypeStatus.Superseded.ToString()
                    || item.Status == ObjectTypeStatus.Retired.ToString()))
            .ToListAsync(cancellationToken);
        var snapshotByType = snapshots.GroupBy(item => item.ObjectTypeId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(item => item.Version).First());
        var fieldsByType = currentFieldEntities
            .Where(field => !nonPublishedIds.Contains(field.ObjectTypeId))
            .GroupBy(item => item.ObjectTypeId)
            .ToDictionary(
                group => group.Key,
                group => group.ToDictionary(
                    field => field.Key,
                    field => new ObjectFieldDefinition(
                        field.Key,
                        field.DisplayName,
                        ObjectFieldTypes.Normalize(field.DataType),
                        field.IsRequired,
                        field.MaxLength,
                        Enum.Parse<FieldSensitivity>(field.Sensitivity, true)),
                    StringComparer.OrdinalIgnoreCase));
        foreach (var snapshot in snapshotByType)
        {
            var fields = JsonStorage.DeserializeObjectFields(snapshot.Value.FieldsJson);
            fieldsByType[snapshot.Key] = fields
                .Select(SqlObjectTypeRepository.ToDefinition)
                .ToDictionary(field => field.Code, StringComparer.OrdinalIgnoreCase);
        }

        return entities.Select(entity =>
        {
            var values = JsonStorage.Deserialize<Dictionary<string, string?>>(entity.FieldsJson)
                ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var definitions = fieldsByType.GetValueOrDefault(entity.ObjectTypeId);
            if (definitions is not null)
            {
                foreach (var field in definitions.Values)
                {
                    if (field.Sensitivity == FieldSensitivity.Core
                        && !user.Roles.Contains("platform-admin", StringComparer.Ordinal)
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

    private static void EnsureRead(UserPrincipal user, string resource) =>
        user.EnsureCan(resource, AccessAction.Read);

    private async Task<List<ObjectTypeDto>> LoadPublishedObjectTypesAsync(
        CancellationToken cancellationToken)
    {
        var publishedVersions = await dbContext.ObjectTypeVersions.AsNoTracking()
            .Where(item => item.Status == ObjectTypeStatus.Published.ToString())
            .OrderByDescending(item => item.Version)
            .ToListAsync(cancellationToken);
        var latestByType = publishedVersions
            .GroupBy(item => item.ObjectTypeId)
            .ToDictionary(group => group.Key, group => group.First());
        var visibleTypeIds = latestByType.Keys.ToArray();
        var types = await dbContext.ObjectTypes.AsNoTracking()
            .Where(item => visibleTypeIds.Contains(item.Id))
            .OrderBy(item => item.Code)
            .ToListAsync(cancellationToken);
        return types.Select(item =>
        {
            var version = latestByType[item.Id];
            var fields = JsonStorage.DeserializeObjectFields(version.FieldsJson);
            return new ObjectTypeDto(
                item.Id.ToString("D"),
                item.Code,
                version.Name,
                version.Version,
                fields.OrderBy(field => field.SortOrder).ThenBy(field => field.Key).ToList(),
                ObjectTypeStatus.Published.ToString(),
                item.Revision,
                version.PublishedAt,
                null);
        }).ToList();
    }

    private async Task<HashSet<string>> LoadCoreFieldKeysAsync(
        Guid objectTypeId,
        CancellationToken cancellationToken)
    {
        var keys = await dbContext.FieldDefinitions.AsNoTracking()
            .Where(item => item.ObjectTypeId == objectTypeId && item.Sensitivity == "Core")
            .Select(item => item.Key)
            .ToListAsync(cancellationToken);
        var snapshots = await dbContext.ObjectTypeVersions.AsNoTracking()
            .Where(item => item.ObjectTypeId == objectTypeId
                && (item.Status == ObjectTypeStatus.Published.ToString()
                    || item.Status == ObjectTypeStatus.Superseded.ToString()
                    || item.Status == ObjectTypeStatus.Retired.ToString()))
            .ToListAsync(cancellationToken);
        foreach (var field in snapshots.SelectMany(snapshot =>
                     JsonStorage.DeserializeObjectFields(snapshot.FieldsJson))
                 .Where(field => field.Sensitivity == FieldSensitivity.Core.ToString()))
        {
            keys.Add(field.Key);
        }

        return new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string?> ParseBusinessObjectVersionFields(
        string snapshotJson)
    {
        using var document = JsonDocument.Parse(snapshotJson);
        var root = document.RootElement;
        if (!root.TryGetProperty("fields", out var fieldsElement)
            && !root.TryGetProperty("Fields", out fieldsElement))
        {
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }

        return JsonSerializer.Deserialize<Dictionary<string, string?>>(
                fieldsElement.GetRawText(),
                JsonStorage.Options)
            ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    }

    private static string? GetSnapshotString(string snapshotJson, string propertyName)
    {
        using var document = JsonDocument.Parse(snapshotJson);
        return document.RootElement.TryGetProperty(propertyName, out var value)
            || document.RootElement.TryGetProperty(
                char.ToUpperInvariant(propertyName[0]) + propertyName[1..],
                out value)
            ? value.GetString()
            : null;
    }

    private async Task RecordDeniedAsync(
        UserPrincipal user,
        string entityType,
        Guid entityId,
        string traceId,
        CancellationToken cancellationToken)
    {
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Security",
                "ReadDenied",
                entityType,
                entityId.ToString("D"),
                traceId,
                AuditOutcome.Denied,
                clock.UtcNow,
                reason: "部门或环境数据范围拒绝。"),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureOrganizationScope(UserPrincipal user)
    {
        if (user.DataScopes.All(scope =>
                scope.ScopeType != "organization"
                || !string.Equals(scope.ScopeReference, user.OrganizationId.ToString("D"), StringComparison.OrdinalIgnoreCase)))
        {
            throw new PermissionException(ErrorCodes.DataScopeDenied, "当前身份没有该组织的数据范围。");
        }
    }

    private static IReadOnlySet<Guid> EnsureDepartmentScopes(UserPrincipal user)
    {
        if (user.IsPlatformAdmin)
        {
            return new HashSet<Guid>();
        }

        var ids = user.DepartmentIds;
        if (ids.Count == 0)
        {
            throw new PermissionException(ErrorCodes.DepartmentScopeDenied, "当前身份没有部门数据范围。");
        }

        return ids;
    }

}
