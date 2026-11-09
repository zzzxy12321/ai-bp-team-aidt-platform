using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Agents;
using AIDT.Domain.Audit;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;
using AIDT.Domain.Integrations;
using AIDT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Services;

public sealed class SqlAgentGovernanceService(
    AidtDbContext dbContext,
    IAuditRepository auditRepository,
    IOutboxRepository outboxRepository,
    IIdempotencyStore idempotencyStore,
    IUnitOfWork unitOfWork,
    IClock clock) : IAgentGovernanceService
{
    private const string PublishRequestType = "AgentProfilePublish";

    public async Task<PagedResult<AgentProfileDetailDto>> ListProfilesAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        string? lifecycleStatus,
        string? departmentCode,
        string? search,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("agent", AccessAction.Read);
        var query = VisibleProfiles(user);
        if (!user.IsPlatformAdmin && !await IsAnyDepartmentManagerAsync(user, cancellationToken))
        {
            query = query.Where(item =>
                item.LifecycleStatus == AgentProfileLifecycleStatus.Published.ToString()
                && item.IsActive
                && item.CurrentPublishedVersionId != null);
        }

        if (!string.IsNullOrWhiteSpace(lifecycleStatus))
        {
            var status = DomainChecks.Required(lifecycleStatus, "生命周期状态", 16);
            if (!Enum.TryParse<AgentProfileLifecycleStatus>(status, true, out var parsed))
            {
                throw new ValidationException("数字员工生命周期状态无效。");
            }

            query = query.Where(item => item.LifecycleStatus == parsed.ToString());
        }

        if (!string.IsNullOrWhiteSpace(departmentCode))
        {
            var normalizedDepartment = DomainChecks.RequiredCode(
                departmentCode.Trim().ToUpperInvariant(),
                "部门编码",
                32);
            var departmentIds = dbContext.Departments.AsNoTracking()
                .Where(item =>
                    item.OrganizationId == user.OrganizationId
                    && item.Code == normalizedDepartment)
                .Select(item => item.Id);
            query = query.Where(item => departmentIds.Contains(item.DepartmentId));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            query = query.Where(item =>
                item.Code.Contains(normalizedSearch)
                || item.Name.Contains(normalizedSearch));
        }

        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var total = await query.LongCountAsync(cancellationToken);
        var rows = await query
            .OrderBy(item => item.DepartmentId)
            .ThenBy(item => item.Code)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<AgentProfileDetailDto>(
            await MapProfilesAsync(rows, cancellationToken),
            normalizedPage,
            normalizedPageSize,
            total);
    }

    public async Task<AgentProfileDetailDto?> GetProfileAsync(
        UserPrincipal user,
        Guid agentProfileId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("agent", AccessAction.Read);
        var entity = await VisibleProfiles(user)
            .SingleOrDefaultAsync(item => item.Id == agentProfileId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (!await CanManageProfileAsync(user, entity.DepartmentId, cancellationToken)
            && (entity.LifecycleStatus != AgentProfileLifecycleStatus.Published.ToString()
                || !entity.IsActive
                || !entity.CurrentPublishedVersionId.HasValue))
        {
            throw new PermissionException(
                ErrorCodes.PermissionDenied,
                "普通业务人员只能查看已发布且有效的数字员工身份。");
        }

        return (await MapProfilesAsync([entity], cancellationToken)).Single();
    }

    public async Task<AgentIdentityOptionsDto> GetOptionsAsync(
        UserPrincipal user,
        Guid? currentAgentProfileId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("agent", AccessAction.Read);
        var canManage = user.IsPlatformAdmin
            || user.HasPermission("agent", AccessAction.Create)
            || user.HasPermission("agent", AccessAction.Update)
            || user.HasPermission("agent", AccessAction.Manage);
        var departmentQuery = dbContext.Departments.AsNoTracking().Where(item =>
            item.OrganizationId == user.OrganizationId
            && item.IsActive
            && item.HierarchyType == "Department");
        if (!user.IsPlatformAdmin)
        {
            departmentQuery = departmentQuery.Where(item => user.DepartmentIds.Contains(item.Id));
        }

        var departmentRows = await departmentQuery
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Code)
            .ToListAsync(cancellationToken);
        var departmentIds = departmentRows.Select(item => item.Id).ToArray();
        IReadOnlySet<Guid> managedDepartmentIds = canManage
            ? await LoadManagedDepartmentIdsAsync(user, departmentIds, cancellationToken)
            : new HashSet<Guid>();
        var peopleRows = await (
            from membership in dbContext.UserDepartmentMemberships.AsNoTracking()
            join account in dbContext.Users.AsNoTracking()
                on membership.UserId equals account.Id
            where departmentIds.Contains(membership.DepartmentId)
                && membership.IsActive
                && account.IsActive
                && !account.IsServiceIdentity
                && account.OrganizationId == user.OrganizationId
            select new
            {
                account.Id,
                account.UserName,
                account.DisplayName,
                membership.DepartmentId
            })
            .Distinct()
            .OrderBy(item => item.DisplayName)
            .ToListAsync(cancellationToken);
        var people = peopleRows
            .GroupBy(item => item.Id)
            .Select(group =>
            {
                var first = group.First();
                return new AgentPersonOptionDto(
                    first.Id.ToString("D"),
                    first.UserName,
                    first.DisplayName,
                    group.Select(item => item.DepartmentId.ToString("D"))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Order(StringComparer.Ordinal)
                        .ToList());
            })
            .ToList();

        var serviceIdentities = new List<AgentServiceIdentityOptionDto>();
        if (canManage)
        {
            var serviceUsers = await dbContext.Users.AsNoTracking()
                .Where(item =>
                    item.OrganizationId == user.OrganizationId
                    && item.IsActive
                    && item.IsServiceIdentity)
                .OrderBy(item => item.DisplayName)
                .ToListAsync(cancellationToken);
            var bindings = await dbContext.AgentProfiles.AsNoTracking()
                .Where(item => serviceUsers.Select(account => account.Id).Contains(item.ServiceUserId))
                .Select(item => new
                {
                    item.Id,
                    item.Name,
                    item.ServiceUserId
                })
                .ToDictionaryAsync(item => item.ServiceUserId, cancellationToken);
            serviceIdentities = serviceUsers
                .Where(item =>
                    !bindings.TryGetValue(item.Id, out var binding)
                    || binding.Id == currentAgentProfileId)
                .Select(item =>
                {
                    var binding = bindings.GetValueOrDefault(item.Id);
                    return new AgentServiceIdentityOptionDto(
                        item.Id.ToString("D"),
                        item.UserName,
                        item.DisplayName,
                        binding?.Id.ToString("D"),
                        binding?.Name);
                })
                .ToList();
        }

        return new AgentIdentityOptionsDto(
            user.OrganizationId.ToString("D"),
            departmentRows.Select(item => new AgentDepartmentOptionDto(
                item.Id.ToString("D"),
                item.Code,
                item.Name,
                managedDepartmentIds.Contains(item.Id))).ToList(),
            people,
            serviceIdentities,
            AgentCapabilityRegistry.Capabilities.Values
                .OrderBy(item => item.Code)
                .Select(item => new AgentCapabilityOptionDto(
                    item.Code,
                    item.Name,
                    item.Description,
                    item.RiskLevel,
                    item.RequiresConfirmation,
                    item.RequiredPermissions,
                    item.McpScope))
                .ToList(),
            AgentCapabilityRegistry.PermanentHumanBoundaries,
            AgentCapabilityRegistry.AllowedConfirmationRules,
            AgentCapabilityRegistry.AllowedStopConditions,
            AgentCapabilityRegistry.AllowedModelScopes);
    }

    public async Task<AgentProfileContextDto> GetContextAsync(
        UserPrincipal user,
        Guid agentProfileId,
        CancellationToken cancellationToken)
    {
        var profile = await GetProfileAsync(user, agentProfileId, cancellationToken)
            ?? throw new NotFoundException("数字员工不存在。");
        var profileEntity = await dbContext.AgentProfiles.AsNoTracking()
            .SingleAsync(item => item.Id == agentProfileId, cancellationToken);
        var canManage = await CanManageProfileAsync(
            user,
            profileEntity.DepartmentId,
            cancellationToken);

        var runRows = await dbContext.AgentRuns.AsNoTracking()
            .Where(item =>
                item.AgentProfileId == agentProfileId
                && item.OrganizationId == user.OrganizationId
                && (user.IsPlatformAdmin || item.DepartmentId == profileEntity.DepartmentId))
            .OrderByDescending(item => item.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
        var runIds = runRows.Select(item => item.Id).ToArray();
        var businessObjectIds = runRows
            .Where(item => item.BusinessObjectId.HasValue)
            .Select(item => item.BusinessObjectId!.Value)
            .Distinct()
            .ToArray();
        var businessObjects = await dbContext.BusinessObjects.AsNoTracking()
            .Where(item => businessObjectIds.Contains(item.Id))
            .Select(item => new { item.Id, item.ObjectCode, item.Title })
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var workflowInstanceIds = runRows
            .Where(item => item.WorkflowInstanceId.HasValue)
            .Select(item => item.WorkflowInstanceId!.Value)
            .Distinct()
            .ToArray();
        var workflowInstances = await dbContext.WorkflowInstances.AsNoTracking()
            .Where(item => workflowInstanceIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var definitionIds = workflowInstances.Values
            .Select(item => item.WorkflowDefinitionId)
            .Distinct()
            .ToArray();
        var workflowDefinitions = await dbContext.WorkflowDefinitions.AsNoTracking()
            .Where(item => definitionIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => new { item.Code, item.Name }, cancellationToken);
        var workflowTaskIds = runRows
            .Where(item => item.WorkflowTaskId.HasValue)
            .Select(item => item.WorkflowTaskId!.Value)
            .Distinct()
            .ToArray();
        var workflowTasks = await dbContext.WorkflowTasks.AsNoTracking()
            .Where(item => workflowTaskIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var minutesSourceIds = runRows
            .Where(item => item.MinutesSourceId.HasValue)
            .Select(item => item.MinutesSourceId!.Value)
            .Distinct()
            .ToArray();
        var minutesSources = await dbContext.MinutesSources.AsNoTracking()
            .Where(item => minutesSourceIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Title, cancellationToken);
        var peopleIds = runRows
            .SelectMany(item => new[] { item.RequestedBy, item.OriginalAssigneeId })
            .Where(item => item.HasValue)
            .Select(item => item!.Value)
            .Distinct()
            .ToArray();
        var people = await dbContext.Users.AsNoTracking()
            .Where(item => peopleIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.DisplayName, cancellationToken);

        var recentRuns = runRows.Select(item =>
        {
            var businessObject = item.BusinessObjectId.HasValue
                ? businessObjects.GetValueOrDefault(item.BusinessObjectId.Value)
                : null;
            var instance = item.WorkflowInstanceId.HasValue
                ? workflowInstances.GetValueOrDefault(item.WorkflowInstanceId.Value)
                : null;
            var definition = instance is null
                ? null
                : workflowDefinitions.GetValueOrDefault(instance.WorkflowDefinitionId);
            var task = item.WorkflowTaskId.HasValue
                ? workflowTasks.GetValueOrDefault(item.WorkflowTaskId.Value)
                : null;
            return new AgentRunEvidenceDto(
                item.Id.ToString("D"),
                item.AgentProfileId.ToString("D"),
                profile.Name,
                item.RequestedBy.ToString("D"),
                people.GetValueOrDefault(item.RequestedBy, string.Empty),
                item.Action,
                item.Amount,
                Enum.Parse<AgentRiskLevel>(item.RiskLevel),
                Enum.Parse<AgentRunStatus>(item.Status),
                item.CreatedAt,
                item.DepartmentId.ToString("D"),
                item.EnvironmentCode,
                item.BusinessObjectId?.ToString("D"),
                businessObject?.ObjectCode,
                businessObject?.Title,
                item.WorkflowInstanceId?.ToString("D"),
                definition?.Name,
                instance?.Status,
                item.WorkflowTaskId?.ToString("D"),
                task?.NodeCode,
                task?.Status,
                item.MinutesSourceId?.ToString("D"),
                item.MinutesSourceId.HasValue
                    ? minutesSources.GetValueOrDefault(item.MinutesSourceId.Value)
                    : null,
                item.Model,
                item.GatewayCallId?.ToString("D"),
                item.GatewayMode,
                item.ExternalModelCalled,
                item.ConfirmationId?.ToString("D"),
                item.ConfirmationId.HasValue,
                item.OriginalAssigneeId?.ToString("D"),
                item.OriginalAssigneeId.HasValue
                    ? people.GetValueOrDefault(item.OriginalAssigneeId.Value)
                    : null,
                item.ProxyTriggerMode,
                item.CompensationStatus);
        }).ToList();

        var workflowNodeRows = await (
            from policy in dbContext.WorkflowNodePolicies.AsNoTracking()
            join version in dbContext.WorkflowVersions.AsNoTracking()
                on policy.WorkflowVersionId equals version.Id
            join definition in dbContext.WorkflowDefinitions.AsNoTracking()
                on version.WorkflowDefinitionId equals definition.Id
            where policy.AgentProfileId == agentProfileId
            orderby policy.PublishedAt descending, policy.NodeCode
            select new AgentWorkflowNodeEvidenceDto(
                policy.Id.ToString("D"),
                definition.Code,
                definition.Name,
                version.Id.ToString("D"),
                version.VersionNumber,
                policy.NodeCode,
                policy.Status,
                policy.AllowAgentAssist,
                policy.AllowAgentProxy,
                policy.PermanentHumanBoundary,
                policy.PublishedAt))
            .Take(100)
            .ToListAsync(cancellationToken);

        var relatedTaskIds = await dbContext.AgentRuns.AsNoTracking()
            .Where(item => item.AgentProfileId == agentProfileId && item.WorkflowTaskId.HasValue)
            .Select(item => item.WorkflowTaskId!.Value)
            .Distinct()
            .ToArrayAsync(cancellationToken);
        var relatedTasks = await dbContext.WorkflowTasks.AsNoTracking()
            .Where(item => relatedTaskIds.Contains(item.Id))
            .OrderByDescending(item => item.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
        var relatedInstanceIds = relatedTasks.Select(item => item.WorkflowInstanceId)
            .Distinct()
            .ToArray();
        var relatedInstances = await dbContext.WorkflowInstances.AsNoTracking()
            .Where(item => relatedInstanceIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var relatedDefinitionIds = relatedInstances.Values
            .Select(item => item.WorkflowDefinitionId)
            .Distinct()
            .ToArray();
        var relatedDefinitions = await dbContext.WorkflowDefinitions.AsNoTracking()
            .Where(item => relatedDefinitionIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var workflowTaskRows = relatedTasks.Select(item =>
        {
            var instance = relatedInstances.GetValueOrDefault(item.WorkflowInstanceId);
            var definition = instance is null
                ? null
                : relatedDefinitions.GetValueOrDefault(instance.WorkflowDefinitionId);
            return new AgentWorkflowTaskEvidenceDto(
                item.Id.ToString("D"),
                item.WorkflowInstanceId.ToString("D"),
                definition?.Code ?? string.Empty,
                definition?.Name ?? "历史流程",
                item.NodeCode,
                item.Status,
                item.DecisionMode,
                item.CreatedAt);
        }).ToList();

        IReadOnlyList<AgentGatewayCallEvidenceDto> gatewayCalls = runIds.Length == 0
            ? []
            : await dbContext.AgentGatewayCalls.AsNoTracking()
                .Where(item => runIds.Contains(item.AgentRunId))
                .OrderByDescending(item => item.CreatedAt)
                .Take(100)
                .Select(item => new AgentGatewayCallEvidenceDto(
                    item.Id.ToString("D"),
                    item.AgentRunId.ToString("D"),
                    item.Model,
                    item.Mode,
                    item.Status,
                    item.ExternalModelCalled,
                    item.DurationMs,
                    item.ErrorMessage,
                    item.CreatedAt))
                .ToListAsync(cancellationToken);

        var canViewMcpEvidence = canManage;
        var mcpClients = new List<McpClientSummaryDto>();
        var mcpTokens = new List<McpTokenSummaryDto>();
        if (canViewMcpEvidence)
        {
            var tokenRows = await dbContext.McpAccessTokens.AsNoTracking()
                .Where(item =>
                    item.AgentProfileId == agentProfileId
                    || item.UserId == profileEntity.ServiceUserId)
                .OrderByDescending(item => item.CreatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);
            var clientIds = tokenRows.Select(item => item.ClientId)
                .Concat(await dbContext.McpClients.AsNoTracking()
                    .Where(item => item.AgentProfileId == agentProfileId)
                    .Select(item => item.ClientId)
                    .ToListAsync(cancellationToken))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var clientRows = await dbContext.McpClients.AsNoTracking()
                .Where(item => clientIds.Contains(item.ClientId))
                .OrderByDescending(item => item.CreatedAt)
                .ToListAsync(cancellationToken);
            mcpClients = clientRows.Select(item => new McpClientSummaryDto(
                item.ClientId,
                item.ClientName,
                item.IsActive,
                tokenRows.Count(token =>
                    token.ClientId == item.ClientId
                    && token.TokenKind == "Access"
                    && token.RevokedAt is null
                    && token.ExpiresAt > clock.UtcNow),
                item.CreatedAt,
                item.LastUsedAt,
                item.AgentProfileId?.ToString("D"),
                profile.Name,
                profileEntity.LifecycleStatus == "Published"
                    && profileEntity.IsActive
                    && profileEntity.CurrentPublishedVersionId.HasValue)).ToList();
            var tokenUserIds = tokenRows.Select(item => item.UserId).Distinct().ToArray();
            var tokenUsers = await dbContext.Users.AsNoTracking()
                .Where(item => tokenUserIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, item => item.DisplayName, cancellationToken);
            mcpTokens = tokenRows.Select(item => new McpTokenSummaryDto(
                item.Id,
                item.ClientId,
                clientRows.FirstOrDefault(client => client.ClientId == item.ClientId)?.ClientName
                    ?? item.ClientId,
                tokenUsers.GetValueOrDefault(item.UserId, "未知身份"),
                item.TokenKind,
                DeserializeStringArray(item.GrantedScopesJson),
                DeserializeStringArray(item.DepartmentIdsJson)
                    .Select(value => Guid.TryParse(value, out var id) ? id : Guid.Empty)
                    .Where(id => id != Guid.Empty)
                    .ToList(),
                item.CreatedAt,
                item.ExpiresAt,
                item.RevokedAt,
                item.LastUsedAt,
                item.AgentProfileId?.ToString("D"),
                profile.Name,
                profileEntity.LifecycleStatus == "Published"
                    && profileEntity.IsActive
                    && profileEntity.CurrentPublishedVersionId.HasValue,
                item.RevokedBy?.ToString("D"),
                item.RevocationReason)).ToList();
        }

        var canViewAuditTimeline = canManage && user.HasPermission("audit", AccessAction.Read);
        IReadOnlyList<AuditEventDto> auditTimeline = !canViewAuditTimeline
            ? []
            : await dbContext.AuditEvents.AsNoTracking()
                .Where(item =>
                    item.EntityType == "AgentProfile"
                    && item.EntityId == agentProfileId.ToString("D"))
                .OrderByDescending(item => item.OccurredAt)
                .Take(100)
                .Select(item => new AuditEventDto(
                    item.Id.ToString("D"),
                    item.ActorId.ToString("D"),
                    item.Module,
                    item.Action,
                    item.EntityType,
                    item.EntityId,
                    item.TraceId,
                    Enum.Parse<AIDT.Domain.Audit.AuditOutcome>(item.Outcome),
                    item.OccurredAt,
                    item.Reason))
                .ToListAsync(cancellationToken);

        return new AgentProfileContextDto(
            profile,
            recentRuns,
            workflowNodeRows,
            workflowTaskRows,
            mcpClients,
            mcpTokens,
            gatewayCalls,
            auditTimeline,
            canViewMcpEvidence,
            canViewAuditTimeline);
    }

    public async Task<IReadOnlyList<AgentProfileDto>> ListAvailableAsync(
        UserPrincipal user,
        string? departmentCode,
        string? search,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("agent", AccessAction.Read);
        var organizationDepartmentIds = dbContext.Departments.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId)
            .Select(item => item.Id);
        var query = dbContext.AgentProfiles.AsNoTracking().Where(item =>
            item.LifecycleStatus == AgentProfileLifecycleStatus.Published.ToString()
            && item.IsActive
            && item.CurrentPublishedVersionId != null
            && organizationDepartmentIds.Contains(item.DepartmentId)
            && (user.IsPlatformAdmin || user.DepartmentIds.Contains(item.DepartmentId)));
        if (!string.IsNullOrWhiteSpace(departmentCode))
        {
            var normalizedDepartment = DomainChecks.RequiredCode(
                departmentCode.Trim().ToUpperInvariant(),
                "部门编码",
                32);
            var departmentIds = dbContext.Departments.AsNoTracking()
                .Where(item =>
                    item.OrganizationId == user.OrganizationId
                    && item.Code == normalizedDepartment)
                .Select(item => item.Id);
            query = query.Where(item => departmentIds.Contains(item.DepartmentId));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            query = query.Where(item =>
                item.Code.Contains(normalizedSearch)
                || item.Name.Contains(normalizedSearch));
        }

        var rows = await query
            .OrderBy(item => item.Name)
            .ThenBy(item => item.Id)
            .Take(500)
            .ToListAsync(cancellationToken);
        return rows.Select(MapAvailableProfile).ToList();
    }

    public async Task<AgentProfileDetailDto> CreateDraftAsync(
        UserPrincipal user,
        CreateAgentProfileDraftRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("agent", AccessAction.Create);
        EnsureHumanGovernanceActor(user);
        ValidateIdempotencyKey(idempotencyKey);
        var hash = Hash(new { request });
        var scope = $"agent-profile:create:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同数字员工草稿。");
            }

            return await GetRequiredProfileAsync(user, Guid.Parse(existing.ResponseId), cancellationToken);
        }

        if (!await CanManageDepartmentAsync(user, request.DepartmentId, cancellationToken))
        {
            throw new PermissionException(
                ErrorCodes.DepartmentScopeDenied,
                "当前身份不能管理该部门数字员工。");
        }

        var snapshot = NormalizeSnapshot(new AgentProfileSnapshot(
            request.Code,
            request.Name,
            request.Description,
            request.ServiceUserId,
            request.DepartmentId,
            request.BusinessOwnerUserId,
            request.TechnicalOwnerUserId,
            request.AutonomyLevel,
            request.RiskLimit,
            request.AmountLimit,
            request.DataScopes,
            request.ModelScopes,
            request.AllowedTools,
            request.RequiredConfirmations,
            request.StopConditions));
        await ValidateReferencesAsync(user, snapshot, cancellationToken);

        if (await dbContext.AgentProfiles.AsNoTracking()
            .AnyAsync(item => item.Code == snapshot.Code, cancellationToken))
        {
            throw new ConflictException(ErrorCodes.Conflict, "数字员工编码已存在。");
        }

        if (await dbContext.AgentProfiles.AsNoTracking()
            .AnyAsync(item => item.ServiceUserId == snapshot.ServiceUserId, cancellationToken))
        {
            throw new ConflictException(ErrorCodes.Conflict, "该服务身份已绑定其他数字员工。");
        }

        var now = clock.UtcNow;
        var profile = NewProfileEntity(snapshot, user.UserId, now);
        var version = NewVersionEntity(profile.Id, 1, snapshot, user.UserId, now);
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.AgentProfiles.Add(profile);
            dbContext.AgentProfileVersions.Add(version);
            await AddAuditAsync(
                user.UserId,
                "agent.profile.draft_created",
                profile.Id,
                traceId,
                now,
                after: new
                {
                    profile.Code,
                    VersionId = version.Id,
                    VersionNumber = version.VersionNumber,
                    version.Revision
                },
                cancellationToken: cancellationToken);
            await AddOutboxAsync(
                "AgentProfileDraftCreated",
                profile.Id,
                $"agent-profile:{profile.Id:D}:draft-created:{version.Id:D}",
                new { profile.Id, profile.Code, VersionId = version.Id, version.VersionNumber },
                now,
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "AgentProfile",
                    profile.Id.ToString("D"),
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

        return (await MapProfilesAsync([profile], cancellationToken)).Single();
    }

    public async Task<AgentProfileVersionDto> CreateNextDraftAsync(
        UserPrincipal user,
        Guid agentProfileId,
        CreateNextAgentProfileDraftRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("agent", AccessAction.Update);
        EnsureHumanGovernanceActor(user);
        ValidateIdempotencyKey(idempotencyKey);
        var profile = await LoadManageableProfileAsync(user, agentProfileId, cancellationToken);
        if (profile.Version != request.ExpectedVersion)
        {
            throw new ConflictException(ErrorCodes.Conflict, "数字员工已变化，请刷新后重试。");
        }

        if (!profile.CurrentPublishedVersionId.HasValue)
        {
            throw new ConflictException(
                ErrorCodes.AgentProfileNotPublished,
                "数字员工尚无已发布版本，不能创建后续草稿。");
        }

        var hash = Hash(new { agentProfileId, request });
        var scope = $"agent-profile:draft:{agentProfileId:D}:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同草稿请求。");
            }

            return await LoadVersionDtoAsync(user, Guid.Parse(existing.ResponseId), cancellationToken);
        }

        if (await dbContext.AgentProfileVersions.AsNoTracking().AnyAsync(
                item => item.AgentProfileId == agentProfileId
                    && (item.Status == AgentProfileVersionStatus.Draft.ToString()
                        || item.Status == AgentProfileVersionStatus.Validated.ToString()),
                cancellationToken))
        {
            throw new ConflictException(ErrorCodes.Conflict, "该数字员工已有未完成草稿。");
        }

        var current = await dbContext.AgentProfileVersions
            .SingleAsync(item => item.Id == profile.CurrentPublishedVersionId.Value, cancellationToken);
        var nextVersionNumber = (await dbContext.AgentProfileVersions
            .Where(item => item.AgentProfileId == agentProfileId)
            .MaxAsync(item => (int?)item.VersionNumber, cancellationToken) ?? 0) + 1;
        var now = clock.UtcNow;
        var draft = CloneVersion(current, nextVersionNumber, user.UserId, now);
        profile.Revision++;
        profile.UpdatedBy = user.UserId;
        profile.UpdatedAt = now;

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.AgentProfileVersions.Add(draft);
            await AddAuditAsync(
                user.UserId,
                "agent.profile.next_draft_created",
                profile.Id,
                traceId,
                now,
                after: new { VersionId = draft.Id, draft.VersionNumber },
                reason: NormalizeOptional(request.Reason, "创建草稿原因", 1000),
                cancellationToken: cancellationToken);
            await AddOutboxAsync(
                "AgentProfileDraftCreated",
                profile.Id,
                $"agent-profile:{profile.Id:D}:draft-created:{draft.Id:D}",
                new { profile.Id, VersionId = draft.Id, draft.VersionNumber },
                now,
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "AgentProfileVersion",
                    draft.Id.ToString("D"),
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

        return await LoadVersionDtoAsync(user, draft.Id, cancellationToken);
    }

    public async Task<AgentProfileVersionDto> UpdateDraftAsync(
        UserPrincipal user,
        Guid agentProfileId,
        Guid versionId,
        UpdateAgentProfileDraftRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("agent", AccessAction.Update);
        EnsureHumanGovernanceActor(user);
        var profile = await LoadManageableProfileAsync(user, agentProfileId, cancellationToken);
        var version = await dbContext.AgentProfileVersions.SingleOrDefaultAsync(
            item => item.Id == versionId && item.AgentProfileId == agentProfileId,
            cancellationToken) ?? throw new NotFoundException("数字员工版本不存在。");
        EnsureMutableDraft(version);
        if (version.Revision != request.ExpectedRevision)
        {
            throw new ConflictException(ErrorCodes.Conflict, "数字员工草稿已变化，请刷新后重试。");
        }

        var snapshot = NormalizeSnapshot(new AgentProfileSnapshot(
            version.Code,
            request.Name,
            request.Description,
            request.ServiceUserId,
            version.DepartmentId,
            version.BusinessOwnerUserId,
            version.TechnicalOwnerUserId,
            request.AutonomyLevel,
            request.RiskLimit,
            request.AmountLimit,
            request.DataScopes,
            request.ModelScopes,
            request.AllowedTools,
            request.RequiredConfirmations,
            request.StopConditions));
        await ValidateReferencesAsync(user, snapshot, cancellationToken);
        var duplicateServiceUser = await dbContext.AgentProfiles.AsNoTracking().AnyAsync(
            item => item.Id != agentProfileId && item.ServiceUserId == snapshot.ServiceUserId,
            cancellationToken);
        if (duplicateServiceUser)
        {
            throw new ConflictException(ErrorCodes.Conflict, "该服务身份已绑定其他数字员工。");
        }

        var now = clock.UtcNow;
        ApplySnapshot(version, snapshot);
        version.Status = AgentProfileVersionStatus.Draft.ToString();
        version.Revision++;
        version.UpdatedBy = user.UserId;
        version.UpdatedAt = now;
        version.ValidationJson = null;
        version.ValidatedBy = null;
        version.ValidatedAt = null;
        profile.Revision++;
        profile.UpdatedBy = user.UserId;
        profile.UpdatedAt = now;
        await AddAuditAsync(
            user.UserId,
            "agent.profile.draft_updated",
            profile.Id,
            traceId,
            now,
            before: $"revision={version.Revision - 1}",
            after: new { VersionId = version.Id, version.Revision },
            cancellationToken: cancellationToken);
        await AddOutboxAsync(
            "AgentProfileDraftUpdated",
            profile.Id,
            $"agent-profile:{profile.Id:D}:draft-updated:{version.Id:D}:{version.Revision}",
            new { profile.Id, VersionId = version.Id, version.Revision },
            now,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await LoadVersionDtoAsync(user, version.Id, cancellationToken);
    }

    public async Task<AgentProfileValidationDto> ValidateAsync(
        UserPrincipal user,
        Guid agentProfileId,
        Guid versionId,
        ValidateAgentProfileVersionRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("agent", AccessAction.Update);
        EnsureHumanGovernanceActor(user);
        ValidateIdempotencyKey(idempotencyKey);
        var hash = Hash(new { agentProfileId, versionId, request });
        var scope = $"agent-profile:validate:{versionId:D}:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同校验请求。");
            }

            var cached = await dbContext.AgentProfileVersions.AsNoTracking()
                .SingleAsync(item => item.Id == versionId, cancellationToken);
            return DeserializeValidation(cached.ValidationJson)
                ?? throw new ConflictException(ErrorCodes.Conflict, "数字员工尚未生成校验结果。");
        }

        var profile = await LoadManageableProfileAsync(user, agentProfileId, cancellationToken);
        var version = await dbContext.AgentProfileVersions.SingleOrDefaultAsync(
            item => item.Id == versionId && item.AgentProfileId == agentProfileId,
            cancellationToken) ?? throw new NotFoundException("数字员工版本不存在。");
        EnsureMutableDraft(version);
        if (version.Revision != request.ExpectedRevision)
        {
            throw new ConflictException(ErrorCodes.Conflict, "数字员工草稿已变化，请刷新后重试。");
        }

        var validation = await ValidateVersionAsync(user, profile, version, cancellationToken);
        var now = clock.UtcNow;
        version.Status = validation.IsValid
            ? AgentProfileVersionStatus.Validated.ToString()
            : AgentProfileVersionStatus.Draft.ToString();
        version.ValidationJson = JsonSerializer.Serialize(validation, JsonSerializerOptions.Web);
        version.IsHighRisk = validation.IsHighRisk;
        version.HighRiskReasonsJson = JsonSerializer.Serialize(
            validation.HighRiskReasons,
            JsonSerializerOptions.Web);
        version.ValidatedBy = user.UserId;
        version.ValidatedAt = now;
        version.Revision++;
        version.UpdatedBy = user.UserId;
        version.UpdatedAt = now;
        profile.Revision++;
        profile.UpdatedBy = user.UserId;
        profile.UpdatedAt = now;
        await AddAuditAsync(
            user.UserId,
            "agent.profile.validated",
            profile.Id,
            traceId,
            now,
            outcome: validation.IsValid ? AuditOutcome.Succeeded : AuditOutcome.Failed,
            after: validation,
            cancellationToken: cancellationToken);
        await AddOutboxAsync(
            validation.IsValid ? "AgentProfileValidated" : "AgentProfileValidationFailed",
            profile.Id,
            $"agent-profile:{profile.Id:D}:validated:{version.Id:D}:{version.Revision}",
            new { profile.Id, VersionId = version.Id, version.Revision, validation },
            now,
            cancellationToken);
        await idempotencyStore.AddAsync(
            new IdempotencyEntry(
                Guid.NewGuid(),
                scope,
                idempotencyKey,
                hash,
                "AgentProfileValidation",
                version.Id.ToString("D"),
                "completed",
                now,
                now.AddDays(30)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return validation;
    }

    public async Task<AgentProfilePublishResultDto> PublishAsync(
        UserPrincipal user,
        Guid agentProfileId,
        Guid versionId,
        PublishAgentProfileVersionRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("agent", AccessAction.Approve);
        EnsureHumanGovernanceActor(user);
        ValidateIdempotencyKey(idempotencyKey);
        var hash = Hash(new { agentProfileId, versionId, request });
        var scope = $"agent-profile:publish:{versionId:D}:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同发布请求。");
            }

            return await BuildPublishReplayAsync(user, agentProfileId, versionId, cancellationToken);
        }

        var profile = await LoadManageableProfileAsync(user, agentProfileId, cancellationToken);
        var version = await dbContext.AgentProfileVersions.SingleOrDefaultAsync(
            item => item.Id == versionId && item.AgentProfileId == agentProfileId,
            cancellationToken) ?? throw new NotFoundException("数字员工版本不存在。");
        EnsureMutableDraft(version);
        if (version.Revision != request.ExpectedRevision)
        {
            throw new ConflictException(ErrorCodes.Conflict, "数字员工草稿已变化，请刷新后重试。");
        }

        var validation = await ValidateVersionAsync(user, profile, version, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(
                "数字员工校验失败：" + string.Join("；", validation.Errors));
        }

        var now = clock.UtcNow;
        AgentProfilePublishResultDto result;
        AccessChangeRequestEntity? changeRequest = null;
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (validation.IsHighRisk)
            {
                changeRequest = await FindPendingChangeRequestAsync(
                    profile,
                    version,
                    cancellationToken)
                    ?? CreateChangeRequest(
                        user,
                        profile,
                        version,
                        validation.HighRiskReasons,
                        NormalizeOptional(request.Reason, "发布原因", 1000),
                        idempotencyKey,
                        now);
                if (changeRequest.Id != Guid.Empty
                    && !await dbContext.AccessChangeRequests.AsNoTracking()
                        .AnyAsync(item => item.Id == changeRequest.Id, cancellationToken))
                {
                    dbContext.AccessChangeRequests.Add(changeRequest);
                }

                result = new AgentProfilePublishResultDto(
                    "AwaitingApproval",
                    null,
                    changeRequest.Id.ToString("D"),
                    validation.HighRiskReasons);
                await AddAuditAsync(
                    user.UserId,
                    "agent.profile.high_risk_submitted",
                    profile.Id,
                    traceId,
                    now,
                    after: new
                    {
                        profile.Id,
                        VersionId = version.Id,
                        ChangeRequestId = changeRequest.Id,
                        validation.HighRiskReasons
                    },
                    reason: changeRequest.RiskReason,
                    cancellationToken: cancellationToken);
                await AddOutboxAsync(
                    "AgentProfileHighRiskSubmitted",
                    profile.Id,
                    $"agent-profile:{profile.Id:D}:high-risk:{changeRequest.Id:D}",
                    new { profile.Id, VersionId = version.Id, ChangeRequestId = changeRequest.Id },
                    now,
                    cancellationToken);
            }
            else
            {
                var published = await ApplyPublishAsync(
                    user,
                    profile,
                    version,
                    validation,
                    request.Reason,
                    traceId,
                    now,
                    cancellationToken);
                result = new AgentProfilePublishResultDto(
                    "Published",
                    published,
                    null,
                    []);
            }

            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "AgentProfileVersion",
                    version.Id.ToString("D"),
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

        return result;
    }

    public async Task<AgentProfileDetailDto> DisableAsync(
        UserPrincipal user,
        Guid agentProfileId,
        DisableAgentProfileRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("agent", AccessAction.Manage);
        EnsureHumanGovernanceActor(user);
        ValidateIdempotencyKey(idempotencyKey);
        var reason = DomainChecks.Required(request.Reason, "停用原因", 1000);
        var hash = Hash(new { agentProfileId, request });
        var scope = $"agent-profile:disable:{agentProfileId:D}:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同停用请求。");
            }

            return await GetRequiredProfileAsync(user, agentProfileId, cancellationToken);
        }

        var profile = await LoadManageableProfileAsync(user, agentProfileId, cancellationToken);
        if (profile.Version != request.ExpectedVersion)
        {
            throw new ConflictException(ErrorCodes.Conflict, "数字员工已变化，请刷新后重试。");
        }

        if (profile.LifecycleStatus == AgentProfileLifecycleStatus.Disabled.ToString())
        {
            return await GetRequiredProfileAsync(user, agentProfileId, cancellationToken);
        }

        if (!profile.CurrentPublishedVersionId.HasValue)
        {
            throw new ConflictException(
                ErrorCodes.AgentProfileNotPublished,
                "没有已发布版本的数字员工不能执行停用。");
        }

        var now = clock.UtcNow;
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var tokens = await dbContext.McpAccessTokens
                .Where(item =>
                    item.AgentProfileId == agentProfileId
                    || item.UserId == profile.ServiceUserId)
                .Where(item => item.RevokedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var token in tokens)
            {
                token.RevokedAt = now;
                token.RevokedBy = user.UserId;
                token.RevocationReason = $"AGENT_DISABLED:{reason}";
            }

            await dbContext.McpClients
                .Where(item => item.AgentProfileId == agentProfileId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(item => item.IsActive, false),
                    cancellationToken);
            profile.LifecycleStatus = AgentProfileLifecycleStatus.Disabled.ToString();
            profile.IsActive = false;
            profile.Version++;
            profile.Revision++;
            profile.UpdatedBy = user.UserId;
            profile.UpdatedAt = now;
            await AddAuditAsync(
                user.UserId,
                "agent.profile.disabled",
                profile.Id,
                traceId,
                now,
                before: AgentProfileLifecycleStatus.Published.ToString(),
                after: new
                {
                    profile.Id,
                    LifecycleStatus = profile.LifecycleStatus,
                    RevokedTokenCount = tokens.Count,
                    ServiceUserId = profile.ServiceUserId
                },
                reason: reason,
                cancellationToken: cancellationToken);
            await AddOutboxAsync(
                "AgentProfileDisabled",
                profile.Id,
                $"agent-profile:{profile.Id:D}:disabled:{profile.Version}",
                new
                {
                    profile.Id,
                    profile.ServiceUserId,
                    RevokedTokenCount = tokens.Count,
                    reason
                },
                now,
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "AgentProfile",
                    profile.Id.ToString("D"),
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

        return await GetRequiredProfileAsync(user, agentProfileId, cancellationToken);
    }

    public async Task<AgentProfileDetailDto> ResumeAsync(
        UserPrincipal user,
        Guid agentProfileId,
        ResumeAgentProfileRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("agent", AccessAction.Manage);
        EnsureHumanGovernanceActor(user);
        ValidateIdempotencyKey(idempotencyKey);
        var reason = DomainChecks.Required(request.Reason, "恢复原因", 1000);
        var hash = Hash(new { agentProfileId, request });
        var scope = $"agent-profile:resume:{agentProfileId:D}:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同恢复请求。");
            }

            return await GetRequiredProfileAsync(user, agentProfileId, cancellationToken);
        }

        var profile = await LoadManageableProfileAsync(user, agentProfileId, cancellationToken);
        if (profile.Version != request.ExpectedVersion)
        {
            throw new ConflictException(ErrorCodes.Conflict, "数字员工已变化，请刷新后重试。");
        }

        if (profile.LifecycleStatus == AgentProfileLifecycleStatus.Published.ToString()
            && profile.IsActive)
        {
            return await GetRequiredProfileAsync(user, agentProfileId, cancellationToken);
        }

        if (profile.LifecycleStatus != AgentProfileLifecycleStatus.Disabled.ToString()
            || !profile.CurrentPublishedVersionId.HasValue)
        {
            throw new ConflictException(ErrorCodes.Conflict, "只有已停用且存在已发布版本的身份可以恢复。");
        }

        var version = await dbContext.AgentProfileVersions.AsNoTracking()
            .SingleAsync(item => item.Id == profile.CurrentPublishedVersionId.Value, cancellationToken);
        var validation = await ValidateVersionAsync(user, profile, version, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(
                "当前发布版本已不满足恢复条件：" + string.Join("；", validation.Errors));
        }

        var now = clock.UtcNow;
        profile.LifecycleStatus = AgentProfileLifecycleStatus.Published.ToString();
        profile.IsActive = true;
        profile.Version++;
        profile.Revision++;
        profile.UpdatedBy = user.UserId;
        profile.UpdatedAt = now;
        await AddAuditAsync(
            user.UserId,
            "agent.profile.resumed",
            profile.Id,
            traceId,
            now,
            before: AgentProfileLifecycleStatus.Disabled.ToString(),
            after: new { profile.Id, LifecycleStatus = profile.LifecycleStatus },
            reason: reason,
            cancellationToken: cancellationToken);
        await AddOutboxAsync(
            "AgentProfileResumed",
            profile.Id,
            $"agent-profile:{profile.Id:D}:resumed:{profile.Version}",
            new { profile.Id, reason },
            now,
            cancellationToken);
        await idempotencyStore.AddAsync(
            new IdempotencyEntry(
                Guid.NewGuid(),
                scope,
                idempotencyKey,
                hash,
                "AgentProfile",
                profile.Id.ToString("D"),
                "completed",
                now,
                now.AddDays(30)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRequiredProfileAsync(user, agentProfileId, cancellationToken);
    }

    public async Task<IReadOnlyList<AgentProfileVersionDto>> ListVersionsAsync(
        UserPrincipal user,
        Guid agentProfileId,
        CancellationToken cancellationToken)
    {
        var profile = await LoadManageableProfileAsync(user, agentProfileId, cancellationToken);
        var versions = await dbContext.AgentProfileVersions.AsNoTracking()
            .Where(item => item.AgentProfileId == profile.Id)
            .OrderByDescending(item => item.VersionNumber)
            .ToListAsync(cancellationToken);
        return await MapVersionsAsync(versions, cancellationToken);
    }

    public async Task<AgentProfilePublishResultDto> RollbackAsync(
        UserPrincipal user,
        Guid agentProfileId,
        RollbackAgentProfileRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("agent", AccessAction.Manage);
        EnsureHumanGovernanceActor(user);
        ValidateIdempotencyKey(idempotencyKey);
        var reason = DomainChecks.Required(request.Reason, "回滚原因", 1000);
        var hash = Hash(new { agentProfileId, request });
        var scope = $"agent-profile:rollback:{agentProfileId:D}:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同回滚请求。");
            }

            return await BuildPublishReplayAsync(
                user,
                agentProfileId,
                Guid.Parse(existing.ResponseId),
                cancellationToken);
        }

        var profile = await LoadManageableProfileAsync(user, agentProfileId, cancellationToken);
        if (profile.Version != request.ExpectedVersion)
        {
            throw new ConflictException(ErrorCodes.Conflict, "数字员工已变化，请刷新后重试。");
        }

        var target = await dbContext.AgentProfileVersions.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == request.TargetVersionId
                && item.AgentProfileId == agentProfileId
                && item.PublishedAt != null,
            cancellationToken) ?? throw new NotFoundException("目标历史发布版本不存在。");
        var nextVersionNumber = (await dbContext.AgentProfileVersions
            .Where(item => item.AgentProfileId == agentProfileId)
            .MaxAsync(item => (int?)item.VersionNumber, cancellationToken) ?? 0) + 1;
        var now = clock.UtcNow;
        var draft = CloneVersion(target, nextVersionNumber, user.UserId, now);
        draft.Description = target.Description;
        profile.Revision++;
        profile.UpdatedBy = user.UserId;
        profile.UpdatedAt = now;
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.AgentProfileVersions.Add(draft);
            var validation = await ValidateVersionAsync(user, profile, draft, cancellationToken);
            if (!validation.IsValid)
            {
                throw new ValidationException(
                    "回滚目标版本校验失败：" + string.Join("；", validation.Errors));
            }

            draft.Status = AgentProfileVersionStatus.Validated.ToString();
            draft.ValidationJson = JsonSerializer.Serialize(validation, JsonSerializerOptions.Web);
            draft.IsHighRisk = validation.IsHighRisk;
            draft.HighRiskReasonsJson = JsonSerializer.Serialize(
                validation.HighRiskReasons,
                JsonSerializerOptions.Web);
            draft.ValidatedBy = user.UserId;
            draft.ValidatedAt = now;
            var result = await SubmitValidatedVersionAsync(
                user,
                profile,
                draft,
                validation,
                reason,
                traceId,
                now,
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "AgentProfileVersion",
                    draft.Id.ToString("D"),
                    "completed",
                    now,
                    now.AddDays(30)),
                cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<AgentProfileChangeRequestDto>> ListChangeRequestsAsync(
        UserPrincipal user,
        string? status,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("agent", AccessAction.Approve);
        EnsurePlatformReviewer(user);
        var query = dbContext.AccessChangeRequests.AsNoTracking()
            .Where(item =>
                item.OrganizationId == user.OrganizationId
                && item.RequestType == PublishRequestType);
        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = DomainChecks.Required(status, "审核状态", 32);
            query = query.Where(item => item.Status == normalizedStatus);
        }

        var requests = await query
            .OrderByDescending(item => item.RequestedAt)
            .Take(200)
            .ToListAsync(cancellationToken);
        var result = new List<AgentProfileChangeRequestDto>(requests.Count);
        foreach (var request in requests)
        {
            var payload = DeserializeChangePayload(request.PayloadJson);
            var profile = await dbContext.AgentProfiles.AsNoTracking()
                .SingleAsync(item => item.Id == payload.AgentProfileId, cancellationToken);
            var version = await dbContext.AgentProfileVersions.AsNoTracking()
                .SingleAsync(item => item.Id == payload.VersionId, cancellationToken);
            var requestedBy = await dbContext.Users.AsNoTracking()
                .Where(item => item.Id == request.RequestedBy)
                .Select(item => item.DisplayName)
                .SingleAsync(cancellationToken);
            var reviewedBy = request.ReviewedBy.HasValue
                ? await dbContext.Users.AsNoTracking()
                    .Where(item => item.Id == request.ReviewedBy.Value)
                    .Select(item => item.DisplayName)
                    .SingleOrDefaultAsync(cancellationToken)
                : null;
            result.Add(new AgentProfileChangeRequestDto(
                request.Id.ToString("D"),
                profile.Id.ToString("D"),
                profile.Name,
                version.Id.ToString("D"),
                version.VersionNumber,
                request.Status,
                request.RiskReason,
                request.RequestedBy.ToString("D"),
                requestedBy,
                request.RequestedAt,
                request.ReviewedBy?.ToString("D"),
                request.ReviewedAt,
                request.ReviewComment));
        }

        return result;
    }

    public async Task<AgentProfilePublishResultDto> ReviewChangeRequestAsync(
        UserPrincipal user,
        Guid requestId,
        ReviewAgentProfileChangeRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("agent", AccessAction.Approve);
        EnsurePlatformReviewer(user);
        var comment = DomainChecks.Required(request.Comment, "审核意见", 1000);
        ValidateIdempotencyKey(idempotencyKey);
        var hash = Hash(new { requestId, request });
        var scope = $"agent-profile:review:{requestId:D}:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同审核请求。");
            }

            var reviewEntity = await dbContext.AccessChangeRequests.AsNoTracking()
                .SingleAsync(item => item.Id == requestId, cancellationToken);
            var reviewPayload = DeserializeChangePayload(reviewEntity.PayloadJson);
            return await BuildPublishReplayAsync(
                user,
                reviewPayload.AgentProfileId,
                reviewPayload.VersionId,
                cancellationToken);
        }

        var changeRequest = await dbContext.AccessChangeRequests.SingleOrDefaultAsync(
            item => item.Id == requestId
                && item.OrganizationId == user.OrganizationId
                && item.RequestType == PublishRequestType,
            cancellationToken) ?? throw new NotFoundException("高风险数字员工变更申请不存在。");
        if (changeRequest.RequestedBy == user.UserId)
        {
            throw new PermissionException(
                ErrorCodes.AgentSelfReviewDenied,
                "申请人不能审核自己的高风险数字员工变更。");
        }

        if (changeRequest.Status != "Pending")
        {
            throw new ConflictException(ErrorCodes.Conflict, "该高风险变更申请已经处理。");
        }

        var payload = DeserializeChangePayload(changeRequest.PayloadJson);
        var profile = await LoadManageableProfileAsync(user, payload.AgentProfileId, cancellationToken);
        var version = await dbContext.AgentProfileVersions.SingleOrDefaultAsync(
            item => item.Id == payload.VersionId && item.AgentProfileId == profile.Id,
            cancellationToken) ?? throw new NotFoundException("待审核数字员工版本不存在。");
        if (version.Revision != payload.DraftRevision
            || version.Status != AgentProfileVersionStatus.Validated.ToString())
        {
            throw new ConflictException(ErrorCodes.Conflict, "待审核版本已变化，请重新提交。");
        }

        if (version.Revision != request.ExpectedRevision)
        {
            throw new ConflictException(ErrorCodes.Conflict, "数字员工草稿已变化，请刷新后重试。");
        }

        var now = clock.UtcNow;
        AgentProfilePublishResultDto result;
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (request.Approve)
            {
                var validation = await ValidateVersionAsync(user, profile, version, cancellationToken);
                if (!validation.IsValid)
                {
                    throw new ValidationException(
                        "待审核版本校验失败：" + string.Join("；", validation.Errors));
                }

                var published = await ApplyPublishAsync(
                    user,
                    profile,
                    version,
                    validation,
                    comment,
                    traceId,
                    now,
                    cancellationToken);
                changeRequest.Status = "Applied";
                result = new AgentProfilePublishResultDto("Published", published, null, []);
            }
            else
            {
                changeRequest.Status = "Rejected";
                result = new AgentProfilePublishResultDto(
                    "Rejected",
                    null,
                    changeRequest.Id.ToString("D"),
                    []);
            }

            changeRequest.ReviewedBy = user.UserId;
            changeRequest.ReviewedAt = now;
            changeRequest.ReviewComment = comment;
            await AddAuditAsync(
                user.UserId,
                request.Approve
                    ? "agent.profile.high_risk_approved"
                    : "agent.profile.high_risk_rejected",
                profile.Id,
                traceId,
                now,
                outcome: AuditOutcome.Succeeded,
                after: new { ChangeRequestId = changeRequest.Id, changeRequest.Status },
                reason: comment,
                cancellationToken: cancellationToken);
            await AddOutboxAsync(
                request.Approve
                    ? "AgentProfileHighRiskApproved"
                    : "AgentProfileHighRiskRejected",
                profile.Id,
                $"agent-profile:{profile.Id:D}:review:{changeRequest.Id:D}",
                new { ChangeRequestId = changeRequest.Id, changeRequest.Status, comment },
                now,
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "AgentProfileVersion",
                    version.Id.ToString("D"),
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

        return result;
    }

    private IQueryable<AgentProfileEntity> VisibleProfiles(UserPrincipal user)
    {
        var organizationDepartmentIds = dbContext.Departments.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId)
            .Select(item => item.Id);
        return dbContext.AgentProfiles.AsNoTracking().Where(item =>
            organizationDepartmentIds.Contains(item.DepartmentId)
            && (user.IsPlatformAdmin || user.DepartmentIds.Contains(item.DepartmentId)));
    }

    private async Task<AgentProfileEntity> LoadManageableProfileAsync(
        UserPrincipal user,
        Guid agentProfileId,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.AgentProfiles.SingleOrDefaultAsync(
            item => item.Id == agentProfileId,
            cancellationToken) ?? throw new NotFoundException("数字员工不存在。");
        if (!await CanManageProfileAsync(user, profile.DepartmentId, cancellationToken))
        {
            throw new PermissionException(
                ErrorCodes.DepartmentScopeDenied,
                "当前身份不能管理该部门数字员工。");
        }

        return profile;
    }

    private async Task<AgentProfileDetailDto> GetRequiredProfileAsync(
        UserPrincipal user,
        Guid agentProfileId,
        CancellationToken cancellationToken) =>
        await GetProfileAsync(user, agentProfileId, cancellationToken)
        ?? throw new NotFoundException("数字员工不存在。");

    private async Task<bool> CanManageProfileAsync(
        UserPrincipal user,
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        if (user.IsPlatformAdmin)
        {
            return true;
        }

        return await CanManageDepartmentAsync(user, departmentId, cancellationToken);
    }

    private async Task<IReadOnlySet<Guid>> LoadManagedDepartmentIdsAsync(
        UserPrincipal user,
        IReadOnlyCollection<Guid> visibleDepartmentIds,
        CancellationToken cancellationToken)
    {
        if (user.IsPlatformAdmin)
        {
            return visibleDepartmentIds.ToHashSet();
        }

        if (user.Roles.Contains("agent-department-manager", StringComparer.OrdinalIgnoreCase))
        {
            return user.DepartmentIds.Intersect(visibleDepartmentIds).ToHashSet();
        }

        var departmentManagerIds = await dbContext.Departments.AsNoTracking()
            .Where(item =>
                item.ManagerUserId == user.UserId
                && visibleDepartmentIds.Contains(item.Id))
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        var membershipManagerIds = await dbContext.UserDepartmentMemberships.AsNoTracking()
            .Where(item =>
                item.UserId == user.UserId
                && item.IsActive
                && item.IsManager
                && visibleDepartmentIds.Contains(item.DepartmentId))
            .Select(item => item.DepartmentId)
            .ToListAsync(cancellationToken);
        return departmentManagerIds.Concat(membershipManagerIds).ToHashSet();
    }

    private async Task<bool> IsAnyDepartmentManagerAsync(
        UserPrincipal user,
        CancellationToken cancellationToken) =>
        user.Roles.Contains("agent-department-manager", StringComparer.OrdinalIgnoreCase)
        || await dbContext.Departments.AsNoTracking().AnyAsync(
                item =>
                    item.OrganizationId == user.OrganizationId
                    && item.ManagerUserId == user.UserId
                    && user.DepartmentIds.Contains(item.Id),
                cancellationToken)
            || await dbContext.UserDepartmentMemberships.AsNoTracking().AnyAsync(
                item =>
                    item.UserId == user.UserId
                    && item.IsActive
                    && item.IsManager
                    && user.DepartmentIds.Contains(item.DepartmentId),
                cancellationToken);

    private async Task<bool> CanManageDepartmentAsync(
        UserPrincipal user,
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        if (user.IsPlatformAdmin)
        {
            return true;
        }

        if (!user.DepartmentIds.Contains(departmentId))
        {
            return false;
        }

        if (user.Roles.Contains("agent-department-manager", StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        return await dbContext.Departments.AsNoTracking().AnyAsync(
                item =>
                    item.Id == departmentId
                    && item.OrganizationId == user.OrganizationId
                    && item.ManagerUserId == user.UserId,
                cancellationToken)
            || await dbContext.UserDepartmentMemberships.AsNoTracking().AnyAsync(
                item =>
                    item.UserId == user.UserId
                    && item.DepartmentId == departmentId
                    && item.IsActive
                    && item.IsManager,
                cancellationToken);
    }

    private async Task ValidateReferencesAsync(
        UserPrincipal actor,
        AgentProfileSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var department = await dbContext.Departments.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == snapshot.DepartmentId,
            cancellationToken) ?? throw new ValidationException("数字员工所属部门不存在。");
        if (!department.IsActive || department.HierarchyType != "Department")
        {
            throw new ValidationException("数字员工所属部门无效或已停用。");
        }

        if (department.OrganizationId != actor.OrganizationId)
        {
            throw new PermissionException(
                ErrorCodes.DepartmentScopeDenied,
                "数字员工所属部门不在当前组织。");
        }

        var serviceUser = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == snapshot.ServiceUserId,
            cancellationToken) ?? throw new ValidationException("独立服务用户不存在。");
        if (!serviceUser.IsActive
            || !serviceUser.IsServiceIdentity
            || serviceUser.OrganizationId != actor.OrganizationId)
        {
            throw new ValidationException("独立服务用户必须为同组织有效服务身份。");
        }

        var ownerIds = new[] { snapshot.BusinessOwnerUserId, snapshot.TechnicalOwnerUserId };
        var validOwners = await dbContext.Users.AsNoTracking()
            .Where(item =>
                ownerIds.Contains(item.Id)
                && item.IsActive
                && !item.IsServiceIdentity
                && item.OrganizationId == actor.OrganizationId)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        if (validOwners.Count != ownerIds.Distinct().Count())
        {
            throw new ValidationException("业务负责人和技术负责人必须是同组织有效真人。");
        }

        if (!await CanManageProfileAsync(actor, snapshot.DepartmentId, cancellationToken))
        {
            throw new PermissionException(
                ErrorCodes.DepartmentScopeDenied,
                "当前身份不能管理该部门数字员工。");
        }

        if (!actor.IsPlatformAdmin)
        {
            if (AgentGovernanceRules.ExceedsDepartmentManagerLimit(snapshot))
            {
                throw new PermissionException(
                    ErrorCodes.PermissionDenied,
                    "部门负责人不能发布 A3/A4、High 风险或非零金额数字员工。");
            }

            var elevatedTool = snapshot.AllowedTools
                .Select(AgentCapabilityRegistry.Find)
                .Where(item => item is not null)
                .SelectMany(item => item!.RequiredPermissions)
                .Any(permission => !actor.Permissions.Any(grant =>
                    string.Equals(
                        $"{grant.Resource}:{grant.Action}",
                        permission,
                        StringComparison.OrdinalIgnoreCase)));
            if (elevatedTool)
            {
                throw new PermissionException(
                    ErrorCodes.PermissionDenied,
                    "数字员工工具能力超过当前部门负责人的有效权限上限。");
            }

            if (snapshot.DataScopes.Any(scope =>
                    scope.StartsWith("organization:", StringComparison.OrdinalIgnoreCase)
                        ? actor.DataScopes.All(item =>
                            !string.Equals(
                                item.ScopeType,
                                "organization",
                                StringComparison.OrdinalIgnoreCase))
                        : !Guid.TryParse(scope["department:".Length..], out var departmentId)
                            || !actor.DepartmentIds.Contains(departmentId)))
            {
                throw new PermissionException(
                    ErrorCodes.DepartmentScopeDenied,
                    "数字员工数据范围超过当前部门负责人授权范围。");
            }
        }
    }

    private async Task<AgentProfileValidationDto> ValidateVersionAsync(
        UserPrincipal actor,
        AgentProfileEntity profile,
        AgentProfileVersionEntity version,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        AgentProfileSnapshot normalized;
        try
        {
            normalized = NormalizeSnapshot(ToSnapshot(version));
            await ValidateReferencesAsync(actor, normalized, cancellationToken);
        }
        catch (ValidationException exception)
        {
            errors.Add(exception.Message);
            normalized = ToSnapshot(version);
        }

        var serviceUser = await dbContext.Users.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == version.ServiceUserId, cancellationToken);
        if (serviceUser is not null)
        {
            var requiredPermissions = normalized.AllowedTools
                .Select(AgentCapabilityRegistry.Find)
                .Where(item => item is not null)
                .SelectMany(item => item!.RequiredPermissions)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (requiredPermissions.Length > 0)
            {
                var grants = await (
                    from role in dbContext.UserRoles.AsNoTracking()
                    join rolePermission in dbContext.RolePermissions.AsNoTracking()
                        on role.RoleId equals rolePermission.RoleId
                    join permission in dbContext.Permissions.AsNoTracking()
                        on rolePermission.PermissionId equals permission.Id
                    where role.UserId == serviceUser.Id
                    select new { permission.Resource, permission.Action })
                    .ToListAsync(cancellationToken);
                var missing = requiredPermissions
                    .Where(required => !grants.Any(grant =>
                        string.Equals(
                            $"{grant.Resource}:{grant.Action}",
                            required,
                            StringComparison.OrdinalIgnoreCase)))
                    .ToArray();
                if (missing.Length > 0)
                {
                    errors.Add("独立服务身份缺少工具所需权限：" + string.Join("、", missing));
                }
            }
        }

        if (!normalized.ModelScopes.Contains("aidt-rule-sim", StringComparer.OrdinalIgnoreCase))
        {
            errors.Add("当前只允许 RuleSimulator 模型范围 aidt-rule-sim。");
        }

        if (normalized.DataScopes.Count > 20)
        {
            warnings.Add("数据范围条目较多，建议按部门收敛。");
        }

        AgentProfileSnapshot? current = null;
        if (profile.CurrentPublishedVersionId.HasValue)
        {
            var currentVersion = await dbContext.AgentProfileVersions.AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.Id == profile.CurrentPublishedVersionId.Value,
                    cancellationToken);
            if (currentVersion is not null)
            {
                current = ToSnapshot(currentVersion);
            }
        }

        var highRiskReasons = AgentGovernanceRules.HighRiskReasons(current, normalized);
        return new AgentProfileValidationDto(
            errors.Count == 0,
            errors,
            warnings,
            highRiskReasons.Count > 0,
            highRiskReasons,
            clock.UtcNow);
    }

    private async Task<AgentProfilePublishResultDto> SubmitValidatedVersionAsync(
        UserPrincipal user,
        AgentProfileEntity profile,
        AgentProfileVersionEntity version,
        AgentProfileValidationDto validation,
        string? reason,
        string traceId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (validation.IsHighRisk)
        {
            var changeRequest = await FindPendingChangeRequestAsync(
                profile,
                version,
                cancellationToken)
                ?? CreateChangeRequest(
                    user,
                    profile,
                    version,
                    validation.HighRiskReasons,
                    reason,
                    $"rollback-{version.Id:D}",
                    now);
            if (!await dbContext.AccessChangeRequests.AsNoTracking()
                .AnyAsync(item => item.Id == changeRequest.Id, cancellationToken))
            {
                dbContext.AccessChangeRequests.Add(changeRequest);
            }

            await AddAuditAsync(
                user.UserId,
                "agent.profile.high_risk_submitted",
                profile.Id,
                traceId,
                now,
                after: new
                {
                    profile.Id,
                    VersionId = version.Id,
                    ChangeRequestId = changeRequest.Id,
                    validation.HighRiskReasons
                },
                reason: changeRequest.RiskReason,
                cancellationToken: cancellationToken);
            await AddOutboxAsync(
                "AgentProfileHighRiskSubmitted",
                profile.Id,
                $"agent-profile:{profile.Id:D}:high-risk:{changeRequest.Id:D}",
                new { profile.Id, VersionId = version.Id, ChangeRequestId = changeRequest.Id },
                now,
                cancellationToken);
            return new AgentProfilePublishResultDto(
                "AwaitingApproval",
                null,
                changeRequest.Id.ToString("D"),
                validation.HighRiskReasons);
        }

        var published = await ApplyPublishAsync(
            user,
            profile,
            version,
            validation,
            reason,
            traceId,
            now,
            cancellationToken);
        return new AgentProfilePublishResultDto("Published", published, null, []);
    }

    private async Task<AgentProfileDetailDto> ApplyPublishAsync(
        UserPrincipal user,
        AgentProfileEntity profile,
        AgentProfileVersionEntity version,
        AgentProfileValidationDto validation,
        string? reason,
        string traceId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await dbContext.AgentProfileVersions
            .Where(item =>
                item.AgentProfileId == profile.Id
                && item.Id != version.Id
                && item.Status == AgentProfileVersionStatus.Published.ToString())
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(item => item.Status, AgentProfileVersionStatus.Retired.ToString())
                    .SetProperty(item => item.RetiredAt, now),
                cancellationToken);
        var snapshot = NormalizeSnapshot(ToSnapshot(version));
        ApplySnapshot(profile, snapshot);
        profile.LifecycleStatus = AgentProfileLifecycleStatus.Published.ToString();
        profile.IsActive = true;
        profile.CurrentPublishedVersionId = version.Id;
        profile.Version = version.VersionNumber;
        profile.Revision++;
        profile.UpdatedBy = user.UserId;
        profile.UpdatedAt = now;
        version.Status = AgentProfileVersionStatus.Published.ToString();
        version.IsHighRisk = validation.IsHighRisk;
        version.HighRiskReasonsJson = JsonSerializer.Serialize(
            validation.HighRiskReasons,
            JsonSerializerOptions.Web);
        version.ValidationJson = JsonSerializer.Serialize(validation, JsonSerializerOptions.Web);
        version.PublishedBy = user.UserId;
        version.PublishedAt = now;
        version.UpdatedBy = user.UserId;
        version.UpdatedAt = now;
        await AddAuditAsync(
            user.UserId,
            "agent.profile.published",
            profile.Id,
            traceId,
            now,
            after: new
            {
                profile.Id,
                profile.Code,
                VersionId = version.Id,
                version.VersionNumber,
                validation.IsHighRisk
            },
            reason: NormalizeOptional(reason, "发布原因", 1000),
            cancellationToken: cancellationToken);
        await AddOutboxAsync(
            "AgentProfilePublished",
            profile.Id,
            $"agent-profile:{profile.Id:D}:published:{version.Id:D}",
            new { profile.Id, profile.Code, VersionId = version.Id, version.VersionNumber },
            now,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapProfileAsync(profile, cancellationToken);
    }

    private async Task<AgentProfilePublishResultDto> BuildPublishReplayAsync(
        UserPrincipal user,
        Guid agentProfileId,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var profile = await GetRequiredProfileAsync(user, agentProfileId, cancellationToken);
        var version = await dbContext.AgentProfileVersions.AsNoTracking()
            .SingleAsync(item => item.Id == versionId, cancellationToken);
        if (version.Status == AgentProfileVersionStatus.Published.ToString()
            && string.Equals(
                profile.CurrentPublishedVersionId,
                version.Id.ToString("D"),
                StringComparison.OrdinalIgnoreCase))
        {
            return new AgentProfilePublishResultDto("Published", profile, null, []);
        }

        var pending = await dbContext.AccessChangeRequests.AsNoTracking()
            .Where(item =>
                item.OrganizationId == user.OrganizationId
                && item.RequestType == PublishRequestType
                && item.Status == "Pending")
            .ToListAsync(cancellationToken);
        var matching = pending.FirstOrDefault(item =>
            DeserializeChangePayload(item.PayloadJson).VersionId == versionId);
        if (matching is not null)
        {
            return new AgentProfilePublishResultDto(
                "AwaitingApproval",
                null,
                matching.Id.ToString("D"),
                DeserializeStringArray(version.HighRiskReasonsJson));
        }

        var rejected = await dbContext.AccessChangeRequests.AsNoTracking()
            .Where(item =>
                item.OrganizationId == user.OrganizationId
                && item.RequestType == PublishRequestType
                && item.Status == "Rejected")
            .ToListAsync(cancellationToken);
        var rejectedMatch = rejected.FirstOrDefault(item =>
            DeserializeChangePayload(item.PayloadJson).VersionId == versionId);
        return rejectedMatch is null
            ? new AgentProfilePublishResultDto(
                "Validated",
                null,
                null,
                DeserializeStringArray(version.HighRiskReasonsJson))
            : new AgentProfilePublishResultDto(
                "Rejected",
                null,
                rejectedMatch.Id.ToString("D"),
                DeserializeStringArray(version.HighRiskReasonsJson));
    }

    private async Task<AccessChangeRequestEntity?> FindPendingChangeRequestAsync(
        AgentProfileEntity profile,
        AgentProfileVersionEntity version,
        CancellationToken cancellationToken)
    {
        var organizationId = await dbContext.Departments.AsNoTracking()
            .Where(item => item.Id == profile.DepartmentId)
            .Select(item => item.OrganizationId)
            .SingleAsync(cancellationToken);
        var candidates = await dbContext.AccessChangeRequests.AsNoTracking()
            .Where(item =>
                item.OrganizationId == organizationId
                && item.RequestType == PublishRequestType
                && item.Status == "Pending")
            .ToListAsync(cancellationToken);
        return candidates.FirstOrDefault(item =>
            DeserializeChangePayload(item.PayloadJson).VersionId == version.Id);
    }

    private static AccessChangeRequestEntity CreateChangeRequest(
        UserPrincipal user,
        AgentProfileEntity profile,
        AgentProfileVersionEntity version,
        IReadOnlyList<string> reasons,
        string? reason,
        string idempotencyKey,
        DateTimeOffset now)
    {
        var riskReason = DomainChecks.Required(
            string.IsNullOrWhiteSpace(reason)
                ? string.Join("；", reasons)
                : $"{reason}；{string.Join("；", reasons)}",
            "高风险原因",
            1000);
        return new AccessChangeRequestEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = user.OrganizationId,
            RequestType = PublishRequestType,
            PayloadJson = JsonSerializer.Serialize(
                new AgentProfileChangePayload(
                    profile.Id,
                    version.Id,
                    version.VersionNumber,
                    version.Revision),
                JsonSerializerOptions.Web),
            RiskReason = riskReason,
            Status = "Pending",
            RequestedBy = user.UserId,
            RequestedAt = now,
            IdempotencyKey = DomainChecks.Required(idempotencyKey, "高风险申请幂等键", 128)
        };
    }

    private static AgentProfileEntity NewProfileEntity(
        AgentProfileSnapshot snapshot,
        Guid actorId,
        DateTimeOffset now)
    {
        var entity = new AgentProfileEntity
        {
            Id = Guid.NewGuid(),
            Code = snapshot.Code,
            LifecycleStatus = AgentProfileLifecycleStatus.Draft.ToString(),
            IsActive = false,
            Version = 0,
            Revision = 1,
            CreatedBy = actorId,
            CreatedAt = now,
            UpdatedBy = actorId,
            UpdatedAt = now
        };
        ApplySnapshot(entity, snapshot);
        return entity;
    }

    private static AgentProfileVersionEntity NewVersionEntity(
        Guid profileId,
        int versionNumber,
        AgentProfileSnapshot snapshot,
        Guid actorId,
        DateTimeOffset now)
    {
        var entity = new AgentProfileVersionEntity
        {
            Id = Guid.NewGuid(),
            AgentProfileId = profileId,
            VersionNumber = versionNumber,
            Revision = 1,
            Status = AgentProfileVersionStatus.Draft.ToString(),
            CreatedBy = actorId,
            CreatedAt = now,
            UpdatedBy = actorId,
            UpdatedAt = now
        };
        ApplySnapshot(entity, snapshot);
        return entity;
    }

    private static AgentProfileVersionEntity CloneVersion(
        AgentProfileVersionEntity source,
        int versionNumber,
        Guid actorId,
        DateTimeOffset now)
    {
        var entity = new AgentProfileVersionEntity
        {
            Id = Guid.NewGuid(),
            AgentProfileId = source.AgentProfileId,
            VersionNumber = versionNumber,
            Revision = 1,
            Status = AgentProfileVersionStatus.Draft.ToString(),
            CreatedBy = actorId,
            CreatedAt = now,
            UpdatedBy = actorId,
            UpdatedAt = now
        };
        ApplySnapshot(entity, ToSnapshot(source));
        return entity;
    }

    private static void ApplySnapshot(AgentProfileEntity entity, AgentProfileSnapshot snapshot)
    {
        entity.ServiceUserId = snapshot.ServiceUserId;
        entity.Code = snapshot.Code;
        entity.Name = snapshot.Name;
        entity.Description = snapshot.Description;
        entity.DepartmentId = snapshot.DepartmentId;
        entity.BusinessOwnerUserId = snapshot.BusinessOwnerUserId;
        entity.TechnicalOwnerUserId = snapshot.TechnicalOwnerUserId;
        entity.AutonomyLevel = snapshot.AutonomyLevel.ToString();
        entity.AmountLimit = snapshot.AmountLimit;
        entity.RiskLimit = snapshot.RiskLimit.ToString();
        entity.DataScopesJson = JsonSerializer.Serialize(snapshot.DataScopes, JsonSerializerOptions.Web);
        entity.ModelScopesJson = JsonSerializer.Serialize(snapshot.ModelScopes, JsonSerializerOptions.Web);
        entity.AllowedToolsJson = JsonSerializer.Serialize(snapshot.AllowedTools, JsonSerializerOptions.Web);
        entity.RequiredConfirmationsJson = JsonSerializer.Serialize(
            snapshot.RequiredConfirmations,
            JsonSerializerOptions.Web);
        entity.StopConditionsJson = JsonSerializer.Serialize(
            snapshot.StopConditions,
            JsonSerializerOptions.Web);
    }

    private static void ApplySnapshot(AgentProfileVersionEntity entity, AgentProfileSnapshot snapshot)
    {
        entity.ServiceUserId = snapshot.ServiceUserId;
        entity.Code = snapshot.Code;
        entity.Name = snapshot.Name;
        entity.Description = snapshot.Description;
        entity.DepartmentId = snapshot.DepartmentId;
        entity.BusinessOwnerUserId = snapshot.BusinessOwnerUserId;
        entity.TechnicalOwnerUserId = snapshot.TechnicalOwnerUserId;
        entity.AutonomyLevel = snapshot.AutonomyLevel.ToString();
        entity.AmountLimit = snapshot.AmountLimit;
        entity.RiskLimit = snapshot.RiskLimit.ToString();
        entity.DataScopesJson = JsonSerializer.Serialize(snapshot.DataScopes, JsonSerializerOptions.Web);
        entity.ModelScopesJson = JsonSerializer.Serialize(snapshot.ModelScopes, JsonSerializerOptions.Web);
        entity.AllowedToolsJson = JsonSerializer.Serialize(snapshot.AllowedTools, JsonSerializerOptions.Web);
        entity.RequiredConfirmationsJson = JsonSerializer.Serialize(
            snapshot.RequiredConfirmations,
            JsonSerializerOptions.Web);
        entity.StopConditionsJson = JsonSerializer.Serialize(
            snapshot.StopConditions,
            JsonSerializerOptions.Web);
    }

    private static AgentProfileSnapshot NormalizeSnapshot(AgentProfileSnapshot snapshot) =>
        AgentGovernanceRules.Normalize(snapshot);

    private static AgentProfileSnapshot ToSnapshot(AgentProfileEntity entity) =>
        new(
            entity.Code,
            entity.Name,
            entity.Description,
            entity.ServiceUserId,
            entity.DepartmentId,
            entity.BusinessOwnerUserId,
            entity.TechnicalOwnerUserId,
            Enum.Parse<AgentAutonomyLevel>(entity.AutonomyLevel),
            Enum.Parse<AgentRiskLevel>(entity.RiskLimit),
            entity.AmountLimit,
            DeserializeStringArray(entity.DataScopesJson),
            DeserializeStringArray(entity.ModelScopesJson),
            DeserializeStringArray(entity.AllowedToolsJson),
            DeserializeStringArray(entity.RequiredConfirmationsJson),
            DeserializeStringArray(entity.StopConditionsJson));

    private static AgentProfileSnapshot ToSnapshot(AgentProfileVersionEntity entity) =>
        new(
            entity.Code,
            entity.Name,
            entity.Description,
            entity.ServiceUserId,
            entity.DepartmentId,
            entity.BusinessOwnerUserId,
            entity.TechnicalOwnerUserId,
            Enum.Parse<AgentAutonomyLevel>(entity.AutonomyLevel),
            Enum.Parse<AgentRiskLevel>(entity.RiskLimit),
            entity.AmountLimit,
            DeserializeStringArray(entity.DataScopesJson),
            DeserializeStringArray(entity.ModelScopesJson),
            DeserializeStringArray(entity.AllowedToolsJson),
            DeserializeStringArray(entity.RequiredConfirmationsJson),
            DeserializeStringArray(entity.StopConditionsJson));

    private async Task<IReadOnlyList<AgentProfileDetailDto>> MapProfilesAsync(
        List<AgentProfileEntity> rows,
        CancellationToken cancellationToken)
    {
        var result = new List<AgentProfileDetailDto>(rows.Count);
        foreach (var row in rows)
        {
            result.Add(await MapProfileAsync(row, cancellationToken));
        }

        return result;
    }

    private async Task<AgentProfileDetailDto> MapProfileAsync(
        AgentProfileEntity entity,
        CancellationToken cancellationToken)
    {
        var serviceUser = await dbContext.Users.AsNoTracking()
            .SingleAsync(item => item.Id == entity.ServiceUserId, cancellationToken);
        var department = await dbContext.Departments.AsNoTracking()
            .SingleAsync(item => item.Id == entity.DepartmentId, cancellationToken);
        var userIds = new[]
        {
            entity.BusinessOwnerUserId,
            entity.TechnicalOwnerUserId,
            entity.CreatedBy,
            entity.UpdatedBy
        };
        var users = await dbContext.Users.AsNoTracking()
            .Where(item => userIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.DisplayName, cancellationToken);
        AgentProfileVersionEntity? publishedVersion = null;
        if (entity.CurrentPublishedVersionId.HasValue)
        {
            publishedVersion = await dbContext.AgentProfileVersions.AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.Id == entity.CurrentPublishedVersionId.Value,
                    cancellationToken);
        }
        var recentRunSummary = await dbContext.AgentRuns.AsNoTracking()
            .Where(item => item.AgentProfileId == entity.Id)
            .GroupBy(item => item.AgentProfileId)
            .Select(group => new
            {
                LastRunAt = group.Max(item => item.CreatedAt),
                Count = group.Count()
            })
            .SingleOrDefaultAsync(cancellationToken);

        return new AgentProfileDetailDto(
            entity.Id.ToString("D"),
            entity.Code,
            entity.Name,
            entity.Description,
            entity.ServiceUserId.ToString("D"),
            serviceUser.DisplayName,
            entity.DepartmentId.ToString("D"),
            department.Name,
            entity.BusinessOwnerUserId.ToString("D"),
            users.GetValueOrDefault(entity.BusinessOwnerUserId, string.Empty),
            entity.TechnicalOwnerUserId.ToString("D"),
            users.GetValueOrDefault(entity.TechnicalOwnerUserId, string.Empty),
            Enum.Parse<AgentAutonomyLevel>(entity.AutonomyLevel),
            Enum.Parse<AgentRiskLevel>(entity.RiskLimit),
            entity.AmountLimit,
            DeserializeStringArray(entity.DataScopesJson),
            DeserializeStringArray(entity.ModelScopesJson),
            DeserializeStringArray(entity.AllowedToolsJson),
            DeserializeStringArray(entity.RequiredConfirmationsJson),
            DeserializeStringArray(entity.StopConditionsJson),
            Enum.Parse<AgentProfileLifecycleStatus>(entity.LifecycleStatus),
            entity.CurrentPublishedVersionId?.ToString("D"),
            publishedVersion?.VersionNumber,
            entity.Version,
            entity.Revision,
            entity.CreatedBy.ToString("D"),
            users.GetValueOrDefault(entity.CreatedBy, string.Empty),
            entity.CreatedAt,
            entity.UpdatedBy.ToString("D"),
            users.GetValueOrDefault(entity.UpdatedBy, string.Empty),
            entity.UpdatedAt,
            recentRunSummary?.LastRunAt,
            recentRunSummary?.Count ?? 0);
    }

    private async Task<IReadOnlyList<AgentProfileVersionDto>> MapVersionsAsync(
        IReadOnlyList<AgentProfileVersionEntity> rows,
        CancellationToken cancellationToken)
    {
        var userIds = rows
            .SelectMany(item => new[]
            {
                item.CreatedBy,
                item.UpdatedBy,
                item.ValidatedBy,
                item.PublishedBy
            })
            .Where(item => item.HasValue)
            .Select(item => item!.Value)
            .Distinct()
            .ToArray();
        var users = await dbContext.Users.AsNoTracking()
            .Where(item => userIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.DisplayName, cancellationToken);
        var serviceUserIds = rows.Select(item => item.ServiceUserId).Distinct().ToArray();
        var serviceUsers = await dbContext.Users.AsNoTracking()
            .Where(item => serviceUserIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.DisplayName, cancellationToken);
        return rows.Select(item =>
        {
            var validation = DeserializeValidation(item.ValidationJson);
            return new AgentProfileVersionDto(
                item.Id.ToString("D"),
                item.AgentProfileId.ToString("D"),
                item.VersionNumber,
                item.Revision,
                Enum.Parse<AgentProfileVersionStatus>(item.Status),
                item.Code,
                item.Name,
                item.Description,
                item.ServiceUserId.ToString("D"),
                serviceUsers.GetValueOrDefault(item.ServiceUserId, string.Empty),
                item.DepartmentId.ToString("D"),
                item.BusinessOwnerUserId,
                item.TechnicalOwnerUserId,
                Enum.Parse<AgentAutonomyLevel>(item.AutonomyLevel),
                Enum.Parse<AgentRiskLevel>(item.RiskLimit),
                item.AmountLimit,
                DeserializeStringArray(item.DataScopesJson),
                DeserializeStringArray(item.ModelScopesJson),
                DeserializeStringArray(item.AllowedToolsJson),
                DeserializeStringArray(item.RequiredConfirmationsJson),
                DeserializeStringArray(item.StopConditionsJson),
                item.IsHighRisk,
                DeserializeStringArray(item.HighRiskReasonsJson),
                validation?.Errors ?? [],
                validation?.Warnings ?? [],
                item.CreatedBy.ToString("D"),
                users.GetValueOrDefault(item.CreatedBy, string.Empty),
                item.CreatedAt,
                item.UpdatedBy.ToString("D"),
                users.GetValueOrDefault(item.UpdatedBy, string.Empty),
                item.UpdatedAt,
                item.ValidatedBy?.ToString("D"),
                item.ValidatedAt,
                item.PublishedBy?.ToString("D"),
                item.PublishedAt,
                item.RetiredAt);
        }).ToList();
    }

    private async Task<AgentProfileVersionDto> LoadVersionDtoAsync(
        UserPrincipal user,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var version = await dbContext.AgentProfileVersions.AsNoTracking()
            .SingleAsync(item => item.Id == versionId, cancellationToken);
        await LoadManageableProfileAsync(user, version.AgentProfileId, cancellationToken);
        return (await MapVersionsAsync([version], cancellationToken)).Single();
    }

    private static AgentProfileDto MapAvailableProfile(AgentProfileEntity entity) =>
        new(
            entity.Id.ToString("D"),
            entity.Name,
            Enum.Parse<AgentAutonomyLevel>(entity.AutonomyLevel),
            entity.AmountLimit,
            Enum.Parse<AgentRiskLevel>(entity.RiskLimit),
            DeserializeStringArray(entity.AllowedToolsJson),
            entity.IsActive,
            entity.ServiceUserId.ToString("D"),
            entity.Code,
            entity.DepartmentId.ToString("D"),
            entity.Description);

    private static AgentProfileValidationDto? DeserializeValidation(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<AgentProfileValidationDto>(
            json,
            JsonSerializerOptions.Web);
    }

    private static string[] DeserializeStringArray(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<string[]>(json, JsonSerializerOptions.Web) ?? [];

    private static AgentProfileChangePayload DeserializeChangePayload(string json) =>
        JsonSerializer.Deserialize<AgentProfileChangePayload>(json, JsonSerializerOptions.Web)
        ?? throw new ValidationException("高风险数字员工变更申请内容无效。");

    private static void EnsureMutableDraft(AgentProfileVersionEntity version)
    {
        if (version.Status != AgentProfileVersionStatus.Draft.ToString()
            && version.Status != AgentProfileVersionStatus.Validated.ToString())
        {
            throw new ConflictException(
                ErrorCodes.AgentProfileInvalid,
                "已发布版本不可原地修改，请创建新草稿版本。");
        }
    }

    private static void EnsureHumanGovernanceActor(UserPrincipal user)
    {
        if (user.IsServiceIdentity)
        {
            throw new PermissionException(
                ErrorCodes.PermissionDenied,
                "数字员工服务身份不能登录或操作治理页面。");
        }
    }

    private static void EnsurePlatformReviewer(UserPrincipal user)
    {
        EnsureHumanGovernanceActor(user);
        if (!user.IsPlatformAdmin)
        {
            throw new PermissionException(
                ErrorCodes.PermissionDenied,
                "只有平台管理员可以审核高风险数字员工变更。");
        }
    }

    private static void ValidateIdempotencyKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length is < 8 or > 128)
        {
            throw new ValidationException("治理动作必须提供 8—128 位幂等键。");
        }
    }

    private static string Hash(object value)
    {
        var json = JsonSerializer.Serialize(value, JsonSerializerOptions.Web);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }

    private static string NormalizeOptional(string? value, string label, int maxLength)
    {
        var normalized = value?.Trim();
        if (normalized?.Length > maxLength)
        {
            throw new ValidationException($"{label}不能超过 {maxLength} 字。");
        }

        return normalized ?? string.Empty;
    }

    private Task AddAuditAsync(
        Guid actorId,
        string action,
        Guid profileId,
        string traceId,
        DateTimeOffset now,
        AuditOutcome outcome = AuditOutcome.Succeeded,
        object? before = null,
        object? after = null,
        string? reason = null,
        CancellationToken cancellationToken = default) =>
        auditRepository.AddAsync(
            AuditEvent.Record(
                actorId,
                "Agent",
                action,
                "AgentProfile",
                profileId.ToString("D"),
                traceId,
                outcome,
                now,
                before is null
                    ? null
                    : JsonSerializer.Serialize(before, JsonSerializerOptions.Web),
                after is null
                    ? null
                    : JsonSerializer.Serialize(after, JsonSerializerOptions.Web),
                string.IsNullOrWhiteSpace(reason) ? null : reason),
            cancellationToken);

    private Task AddOutboxAsync(
        string eventType,
        Guid aggregateId,
        string idempotencyKey,
        object payload,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        outboxRepository.AddAsync(
            OutboxMessage.Create(
                eventType,
                "AgentProfile",
                aggregateId,
                idempotencyKey,
                JsonSerializer.Serialize(payload, JsonSerializerOptions.Web),
                now),
            cancellationToken);

    private sealed record AgentProfileChangePayload(
        Guid AgentProfileId,
        Guid VersionId,
        int VersionNumber,
        long DraftRevision);
}
