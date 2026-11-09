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

public sealed class SqlWorkflowAssignmentResolver(
    AidtDbContext dbContext,
    IClock clock) : IWorkflowAssignmentResolver
{
    public async Task<ResolvedWorkflowAssignment> ResolveAsync(
        Guid organizationId,
        Guid workflowDepartmentId,
        Guid? initiatorUserId,
        Guid businessObjectId,
        WorkflowAssignmentRule rule,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rule);
        var resolvedAt = clock.UtcNow;
        var candidates = new List<CandidateUser>();
        Guid? businessRoleId = null;
        string? businessRoleCode = null;
        int? businessRoleVersion = null;

        switch (rule.Strategy)
        {
            case WorkflowAssignmentStrategy.SpecificUser:
                var user = await dbContext.Users.AsNoTracking()
                    .Where(item => item.OrganizationId == organizationId
                        && item.UserName == rule.SpecificUserName
                        && item.IsActive
                        && !item.IsServiceIdentity)
                    .Select(item => new CandidateUser(item.Id, item.DisplayName, null))
                    .SingleOrDefaultAsync(cancellationToken)
                    ?? throw new ValidationException("指定处理人不存在、已停用或属于服务身份。");
                candidates.Add(user);
                break;

            case WorkflowAssignmentStrategy.BusinessRole:
                var role = await dbContext.Roles.AsNoTracking()
                    .SingleOrDefaultAsync(
                        item => item.Code == rule.BusinessRoleCode
                            && item.RoleType == "Business"
                            && item.IsActive,
                        cancellationToken)
                    ?? throw new ValidationException($"业务角色 {rule.BusinessRoleCode} 不存在或已停用。");
                businessRoleId = role.Id;
                businessRoleCode = role.Code;
                businessRoleVersion = checked((int)role.Version);
                var organizationScope = organizationId.ToString("D");
                var workflowDepartmentScope = workflowDepartmentId.ToString("D");
                candidates = await (
                    from assignment in dbContext.UserRoleAssignments.AsNoTracking()
                    join userRow in dbContext.Users.AsNoTracking() on assignment.UserId equals userRow.Id
                    where assignment.RoleId == role.Id
                        && assignment.IsActive
                        && userRow.IsActive
                        && !userRow.IsServiceIdentity
                        && userRow.OrganizationId == organizationId
                        && (assignment.ScopeType == "organization"
                                && assignment.ScopeReference == organizationScope
                            || assignment.ScopeType == "department"
                                && assignment.ScopeReference == workflowDepartmentScope)
                    orderby userRow.Id
                    select new CandidateUser(userRow.Id, userRow.DisplayName, assignment.ScopeType))
                    .ToListAsync(cancellationToken);
                if (candidates.Count == 0)
                {
                    throw new ValidationException(
                        $"业务角色 {role.Code} 在部门 {workflowDepartmentId:D} 没有可解析成员。");
                }

                break;

            case WorkflowAssignmentStrategy.DepartmentManager:
                candidates.Add(await ResolveDepartmentManagerAsync(
                    organizationId,
                    workflowDepartmentId,
                    cancellationToken));
                break;

            case WorkflowAssignmentStrategy.ProjectOwner:
                var owner = await (
                    from profile in dbContext.ProjectProfiles.AsNoTracking()
                    join userRow in dbContext.Users.AsNoTracking() on profile.OwnerUserId equals userRow.Id
                    where profile.BusinessObjectId == businessObjectId
                        && userRow.IsActive
                        && !userRow.IsServiceIdentity
                    select new CandidateUser(userRow.Id, userRow.DisplayName, "project-owner"))
                    .SingleOrDefaultAsync(cancellationToken)
                    ?? throw new ValidationException("业务对象没有可解析的项目负责人。");
                candidates.Add(owner);
                break;

            case WorkflowAssignmentStrategy.InitiatorManager:
                if (!initiatorUserId.HasValue)
                {
                    throw new ValidationException("发起人主管策略缺少发起人。");
                }

                var managerId = await ResolveInitiatorManagerAsync(
                    organizationId,
                    initiatorUserId.Value,
                    workflowDepartmentId,
                    cancellationToken);
                candidates.Add(await LoadUserAsync(organizationId, managerId, cancellationToken));
                break;

            case WorkflowAssignmentStrategy.Agent:
                if (!rule.AgentProfileId.HasValue)
                {
                    throw new ValidationException("Agent 处理人策略必须指定数字员工。");
                }

                var agent = await (
                    from profile in dbContext.AgentProfiles.AsNoTracking()
                    join userRow in dbContext.Users.AsNoTracking() on profile.ServiceUserId equals userRow.Id
                    where profile.Id == rule.AgentProfileId.Value
                        && profile.IsActive
                        && profile.LifecycleStatus == "Published"
                        && profile.CurrentPublishedVersionId != null
                        && userRow.IsActive
                        && userRow.IsServiceIdentity
                    select new CandidateUser(userRow.Id, userRow.DisplayName, "agent"))
                    .SingleOrDefaultAsync(cancellationToken)
                    ?? throw new ValidationException("指定数字员工不存在或已停用。");
                candidates.Add(agent);
                break;

            default:
                throw new ValidationException("不支持的流程处理人策略。");
        }

        candidates = candidates
            .DistinctBy(item => item.Id)
            .OrderBy(item => item.Id)
            .ToList();
        var backupAssigneeId = candidates.Count > 1
            ? candidates[1].Id
            : await TryResolveBackupManagerAsync(
                organizationId,
                workflowDepartmentId,
                candidates[0].Id,
                cancellationToken);
        var snapshot = JsonSerializer.Serialize(
            new
            {
                rule.Strategy,
                organizationId,
                workflowDepartmentId,
                initiatorUserId,
                businessObjectId,
                agentProfileId = rule.AgentProfileId,
                resolvedAt,
                candidates = candidates.Select(item => new
                {
                    userId = item.Id,
                    item.DisplayName,
                    item.Scope
                }),
                businessRoleId,
                businessRoleCode,
                businessRoleVersion
            },
            JsonSerializerOptions.Web);
        return new ResolvedWorkflowAssignment(
            candidates[0].Id,
            candidates.Select(item => item.Id).ToList(),
            rule.Strategy,
            businessRoleId,
            businessRoleCode,
            businessRoleVersion,
            backupAssigneeId,
            snapshot);
    }

    public async Task<WorkflowAssignmentValidation> ValidateAsync(
        Guid organizationId,
        Guid workflowDepartmentId,
        string objectTypeCode,
        WorkflowAssignmentRule rule,
        CancellationToken cancellationToken)
    {
        try
        {
            switch (rule.Strategy)
            {
                case WorkflowAssignmentStrategy.ProjectOwner:
                    if (!string.Equals(objectTypeCode, "PROJECT", StringComparison.Ordinal))
                    {
                        return new WorkflowAssignmentValidation(
                            false,
                            "项目负责人策略只能用于 PROJECT 对象类型。",
                            [],
                            null,
                            null);
                    }

                    return new WorkflowAssignmentValidation(
                        true,
                        "项目负责人策略可在任务创建时解析。",
                        [],
                        null,
                        null);
                case WorkflowAssignmentStrategy.InitiatorManager:
                    var hasManager = await dbContext.Departments.AsNoTracking()
                            .AnyAsync(
                                item => item.Id == workflowDepartmentId
                                    && item.OrganizationId == organizationId
                                    && item.IsActive
                                    && item.ManagerUserId != null,
                                cancellationToken)
                        || await dbContext.UserDepartmentMemberships.AsNoTracking()
                            .AnyAsync(
                                item => item.DepartmentId == workflowDepartmentId
                                    && item.IsManager
                                    && item.IsActive,
                                cancellationToken);
                    return hasManager
                        ? new WorkflowAssignmentValidation(true, "可在任务创建时解析发起人主管。", [], null, null)
                        : new WorkflowAssignmentValidation(
                            false,
                            "当前组织没有可用的部门负责人，无法解析发起人主管。",
                            [],
                            null,
                            null);
                default:
                    var resolved = await ResolveAsync(
                        organizationId,
                        workflowDepartmentId,
                        null,
                        Guid.Empty,
                        rule,
                        cancellationToken);
                    return new WorkflowAssignmentValidation(
                        true,
                        "处理人策略可解析。",
                        resolved.CandidateUserIds,
                        resolved.BusinessRoleCode,
                        resolved.BusinessRoleVersion);
            }
        }
        catch (ValidationException exception)
        {
            return new WorkflowAssignmentValidation(false, exception.Message, [], null, null);
        }
    }

    private async Task<CandidateUser> ResolveDepartmentManagerAsync(
        Guid organizationId,
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        var membershipManager = await (
            from membership in dbContext.UserDepartmentMemberships.AsNoTracking()
            join userRow in dbContext.Users.AsNoTracking() on membership.UserId equals userRow.Id
            join department in dbContext.Departments.AsNoTracking() on membership.DepartmentId equals department.Id
            where membership.DepartmentId == departmentId
                && membership.IsActive
                && membership.IsManager
                && department.OrganizationId == organizationId
                && department.IsActive
                && userRow.IsActive
                && !userRow.IsServiceIdentity
            orderby userRow.Id
            select new CandidateUser(userRow.Id, userRow.DisplayName, "department-manager"))
            .FirstOrDefaultAsync(cancellationToken);
        if (membershipManager is not null)
        {
            return membershipManager;
        }

        return await (
            from department in dbContext.Departments.AsNoTracking()
            join userRow in dbContext.Users.AsNoTracking() on department.ManagerUserId equals userRow.Id
            where department.Id == departmentId
                && department.OrganizationId == organizationId
                && department.IsActive
                && userRow.IsActive
                && !userRow.IsServiceIdentity
            select new CandidateUser(userRow.Id, userRow.DisplayName, "department-manager"))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ValidationException("部门负责人不存在或已停用。");
    }

    private async Task<Guid> ResolveInitiatorManagerAsync(
        Guid organizationId,
        Guid initiatorUserId,
        Guid fallbackDepartmentId,
        CancellationToken cancellationToken)
    {
        var primaryDepartmentId = await dbContext.UserDepartmentMemberships.AsNoTracking()
            .Where(item => item.UserId == initiatorUserId && item.IsPrimary && item.IsActive)
            .Select(item => (Guid?)item.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken);
        var membershipManagerId = await (
            from membership in dbContext.UserDepartmentMemberships.AsNoTracking()
            join userRow in dbContext.Users.AsNoTracking() on membership.UserId equals userRow.Id
            where membership.DepartmentId == (primaryDepartmentId ?? fallbackDepartmentId)
                && membership.IsActive
                && membership.IsManager
                && userRow.IsActive
                && !userRow.IsServiceIdentity
            orderby userRow.Id
            select (Guid?)userRow.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (membershipManagerId.HasValue)
        {
            return membershipManagerId.Value;
        }

        return await dbContext.Departments.AsNoTracking()
            .Where(item => item.Id == (primaryDepartmentId ?? fallbackDepartmentId)
                && item.OrganizationId == organizationId
                && item.IsActive
                && item.ManagerUserId != null)
            .Select(item => item.ManagerUserId!.Value)
            .SingleOrDefaultAsync(cancellationToken)
            is var managerId && managerId != Guid.Empty
                ? managerId
                : throw new ValidationException("发起人主管不存在或已停用。");
    }

    private async Task<CandidateUser> LoadUserAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken) =>
        await dbContext.Users.AsNoTracking()
            .Where(item => item.Id == userId
                && item.OrganizationId == organizationId
                && item.IsActive
                && !item.IsServiceIdentity)
            .Select(item => new CandidateUser(item.Id, item.DisplayName, "initiator-manager"))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new ValidationException("解析出的处理人不存在或已停用。");

    private async Task<Guid?> TryResolveBackupManagerAsync(
        Guid organizationId,
        Guid departmentId,
        Guid primaryUserId,
        CancellationToken cancellationToken)
    {
        var managerId = await dbContext.Departments.AsNoTracking()
            .Where(item => item.Id == departmentId
                && item.OrganizationId == organizationId
                && item.IsActive)
            .Select(item => item.ManagerUserId)
            .FirstOrDefaultAsync(cancellationToken);
        return managerId.HasValue && managerId.Value != primaryUserId && managerId.Value != Guid.Empty
            ? managerId
            : null;
    }

    private sealed record CandidateUser(Guid Id, string DisplayName, string? Scope);
}

public sealed class SqlWorkflowRoleService(
    AidtDbContext dbContext,
    IAuditRepository auditRepository,
    IIdempotencyStore idempotencyStore,
    IUnitOfWork unitOfWork,
    IClock clock) : IWorkflowRoleService
{
    public async Task<IReadOnlyList<RoleDto>> ListAsync(
        UserPrincipal user,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("workflow-role", AccessAction.Read);
        return await LoadRolesAsync(user.OrganizationId, "Business", cancellationToken);
    }

    public async Task<IReadOnlyList<RoleMemberDto>> ListMembersAsync(
        UserPrincipal user,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("workflow-role", AccessAction.Read);
        await EnsureWorkflowRoleAsync(roleId, user.OrganizationId, cancellationToken);
        return await LoadMembersAsync(roleId, user, cancellationToken);
    }

    public async Task<IReadOnlyList<RoleMemberDto>> SetMembersAsync(
        UserPrincipal user,
        Guid roleId,
        SetRoleMembersRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("workflow-role", AccessAction.Manage);
        ValidateIdempotencyKey(idempotencyKey);
        var role = await EnsureWorkflowRoleAsync(roleId, user.OrganizationId, cancellationToken);
        if (role.Version != request.ExpectedRoleVersion)
        {
            throw new ConflictException(ErrorCodes.Conflict, "业务角色版本已变化，请刷新后重试。");
        }

        var normalized = await NormalizeMembersAsync(user, request.Members, cancellationToken);
        if (normalized.Any(item => item.UserId == user.UserId && item.IsActive))
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "禁止给自己授予业务流程角色。");
        }

        var hash = Hash(new { roleId, request });
        var scope = $"workflow-role:members:{roleId:D}:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.RequestHash != hash)
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同业务角色成员请求。");
            }

            return await LoadMembersAsync(roleId, user, cancellationToken);
        }

        var now = clock.UtcNow;
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await ApplyMembersAsync(roleId, normalized, user, now, cancellationToken);
            role.Version++;
            role.UpdatedAt = now;
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Identity",
                    "workflow.role.members_updated",
                    "Role",
                    roleId.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: JsonSerializer.Serialize(normalized, JsonSerializerOptions.Web)),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "Role",
                    roleId.ToString("D"),
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

        return await LoadMembersAsync(roleId, user, cancellationToken);
    }

    private async Task<RoleEntity> EnsureWorkflowRoleAsync(
        Guid roleId,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles.SingleOrDefaultAsync(
            item => item.Id == roleId && item.RoleType == "Business" && item.IsActive,
            cancellationToken) ?? throw new NotFoundException("业务流程角色不存在或已停用。");
        var validScopeExists = await (
            from userRole in dbContext.UserRoleAssignments.AsNoTracking()
            join userRow in dbContext.Users.AsNoTracking() on userRole.UserId equals userRow.Id
            where userRole.RoleId == roleId
            select userRow.OrganizationId)
            .AnyAsync(id => id == organizationId, cancellationToken);
        var anyAssignment = await dbContext.UserRoleAssignments.AsNoTracking()
            .AnyAsync(item => item.RoleId == roleId, cancellationToken);
        if (anyAssignment && !validScopeExists)
        {
            throw new PermissionException(ErrorCodes.DataScopeDenied, "业务角色不属于当前组织。");
        }

        return role;
    }

    private async Task<List<SetRoleMemberItem>> NormalizeMembersAsync(
        UserPrincipal user,
        IReadOnlyList<SetRoleMemberItem> members,
        CancellationToken cancellationToken)
    {
        if (members.Count > 500)
        {
            throw new ValidationException("单次业务角色成员变更不能超过 500 人。");
        }

        var normalized = new List<SetRoleMemberItem>();
        foreach (var member in members)
        {
            var scopeType = member.ScopeType.Trim().ToLowerInvariant();
            if (scopeType is not ("organization" or "department"))
            {
                throw new ValidationException("角色范围只支持 organization 或 department。");
            }

            var scopeReference = member.ScopeReference.Trim();
            if (scopeType == "organization")
            {
                if (!user.IsPlatformAdmin)
                {
                    throw new PermissionException(
                        ErrorCodes.PermissionDenied,
                        "组织范围业务流程角色分配只能由平台管理员执行。");
                }

                scopeReference = user.OrganizationId.ToString("D");
            }
            else if (!Guid.TryParse(scopeReference, out var departmentId)
                || !await dbContext.Departments.AsNoTracking().AnyAsync(
                    item => item.Id == departmentId
                        && item.OrganizationId == user.OrganizationId
                        && item.IsActive,
                    cancellationToken))
            {
                throw new ValidationException("角色部门范围不存在或已停用。");
            }
            else
            {
                user.EnsureDepartmentAccess(departmentId);
            }

            var target = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(
                item => item.Id == member.UserId
                    && item.OrganizationId == user.OrganizationId
                    && item.IsActive
                    && !item.IsServiceIdentity,
                cancellationToken);
            if (target is null)
            {
                throw new ValidationException("角色成员不存在或已停用。");
            }

            if (scopeType == "department")
            {
                var departmentId = Guid.Parse(scopeReference);
                var qualifies = await dbContext.UserDepartmentMemberships.AsNoTracking().AnyAsync(
                        item => item.UserId == target.Id
                            && item.DepartmentId == departmentId
                            && item.IsActive,
                        cancellationToken)
                    || await dbContext.Departments.AsNoTracking().AnyAsync(
                        item => item.Id == departmentId && item.ManagerUserId == target.Id,
                        cancellationToken);
                if (!qualifies)
                {
                    throw new ValidationException("角色成员必须属于目标部门或担任该部门负责人。");
                }
            }

            normalized.Add(member with { ScopeType = scopeType, ScopeReference = scopeReference });
        }

        return normalized
            .GroupBy(item => (item.UserId, item.ScopeType, item.ScopeReference))
            .Select(group => group.Last())
            .ToList();
    }

    private async Task ApplyMembersAsync(
        Guid roleId,
        IReadOnlyList<SetRoleMemberItem> desired,
        UserPrincipal actor,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.UserRoleAssignments
            .Where(item => item.RoleId == roleId)
            .ToListAsync(cancellationToken);
        foreach (var assignment in existing)
        {
            if (!CanManageMembershipScope(actor, assignment.ScopeType, assignment.ScopeReference))
            {
                continue;
            }

            var match = desired.FirstOrDefault(item =>
                item.UserId == assignment.UserId
                && string.Equals(item.ScopeType, assignment.ScopeType, StringComparison.OrdinalIgnoreCase)
                && string.Equals(
                    item.ScopeReference,
                    assignment.ScopeReference,
                    StringComparison.OrdinalIgnoreCase));
            if (match is null || !match.IsActive)
            {
                if (assignment.IsActive)
                {
                    assignment.IsActive = false;
                    assignment.RevokedBy = actor.UserId;
                    assignment.RevokedAt = now;
                }

                continue;
            }

            assignment.IsActive = true;
            assignment.RevokedBy = null;
            assignment.RevokedAt = null;
        }

        foreach (var item in desired.Where(item => item.IsActive))
        {
            if (existing.Any(assignment =>
                    assignment.UserId == item.UserId
                    && string.Equals(
                        assignment.ScopeType,
                        item.ScopeType,
                        StringComparison.OrdinalIgnoreCase)
                    && string.Equals(
                        assignment.ScopeReference,
                        item.ScopeReference,
                        StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            dbContext.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                Id = Guid.NewGuid(),
                UserId = item.UserId,
                RoleId = roleId,
                ScopeType = item.ScopeType,
                ScopeReference = item.ScopeReference,
                IsActive = true,
                AssignedBy = actor.UserId,
                CreatedAt = now
            });
        }
    }

    private async Task<IReadOnlyList<RoleDto>> LoadRolesAsync(
        Guid organizationId,
        string type,
        CancellationToken cancellationToken)
    {
        var roles = await dbContext.Roles.AsNoTracking()
            .Where(item => item.RoleType == type)
            .OrderBy(item => item.IsSystem ? 0 : 1)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
        var roleIds = roles.Select(item => item.Id).ToArray();
        var scopedMembers = await dbContext.UserRoleAssignments.AsNoTracking()
            .Where(item => roleIds.Contains(item.RoleId) && item.IsActive)
            .Join(
                dbContext.Users.Where(item => item.OrganizationId == organizationId && item.IsActive),
                assignment => assignment.UserId,
                user => user.Id,
                (assignment, _) => new { assignment.RoleId, assignment.UserId })
            .ToListAsync(cancellationToken);
        var legacyMembers = await (
            from userRole in dbContext.UserRoles.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on userRole.UserId equals user.Id
            where roleIds.Contains(userRole.RoleId)
                && user.OrganizationId == organizationId
                && user.IsActive
            select new { userRole.RoleId, userRole.UserId })
            .ToListAsync(cancellationToken);
        var memberCounts = scopedMembers
            .Concat(legacyMembers)
            .Distinct()
            .GroupBy(item => item.RoleId)
            .ToDictionary(group => group.Key, group => group.Count());
        var permissionCounts = await dbContext.RolePermissions.AsNoTracking()
            .Where(item => roleIds.Contains(item.RoleId))
            .GroupBy(item => item.RoleId)
            .Select(group => new { RoleId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.RoleId, item => item.Count, cancellationToken);
        return roles.Select(role => MapRole(
            role,
            memberCounts.GetValueOrDefault(role.Id),
            permissionCounts.GetValueOrDefault(role.Id))).ToList();
    }

    private async Task<IReadOnlyList<RoleMemberDto>> LoadMembersAsync(
        Guid roleId,
        UserPrincipal viewer,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from assignment in dbContext.UserRoleAssignments.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on assignment.UserId equals user.Id
            where assignment.RoleId == roleId
            orderby assignment.IsActive descending, user.DisplayName
            select new { assignment, user }).ToListAsync(cancellationToken);
        var departmentIds = rows
            .Where(item => item.assignment.ScopeType == "department"
                && Guid.TryParse(item.assignment.ScopeReference, out _))
            .Select(item => Guid.Parse(item.assignment.ScopeReference))
            .Distinct()
            .ToArray();
        var departments = await dbContext.Departments.AsNoTracking()
            .Where(item => departmentIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        var scoped = rows
            .Where(item => CanManageMembershipScope(
                viewer,
                item.assignment.ScopeType,
                item.assignment.ScopeReference))
            .Select(item => new RoleMemberDto(
            item.user.Id.ToString("D"),
            item.user.UserName,
            item.user.DisplayName,
            item.assignment.ScopeType,
            item.assignment.ScopeReference,
            item.assignment.ScopeType == "organization"
                ? "整个组织"
                : departments.GetValueOrDefault(Guid.Parse(item.assignment.ScopeReference)) ?? "未知部门",
            item.assignment.IsActive,
            item.assignment.CreatedAt)).ToList();
        if (!viewer.IsPlatformAdmin)
        {
            return scoped;
        }

        var legacy = await (
            from userRole in dbContext.UserRoles.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on userRole.UserId equals user.Id
            where userRole.RoleId == roleId
                && user.OrganizationId == viewer.OrganizationId
            select new RoleMemberDto(
                user.Id.ToString("D"),
                user.UserName,
                user.DisplayName,
                "organization",
                user.OrganizationId.ToString("D"),
                "整个组织（旧版全局角色）",
                user.IsActive,
                DateTimeOffset.MinValue)).ToListAsync(cancellationToken);
        return scoped
            .Concat(legacy.Where(item => scoped.All(existing =>
                existing.UserId != item.UserId || !existing.IsActive)))
            .ToList();
    }

    private static RoleDto MapRole(RoleEntity role, int memberCount, int permissionCount) =>
        new(
            role.Id.ToString("D"),
            role.Code,
            role.Name,
            role.RoleType,
            role.Description,
            role.IsSystem,
            role.IsActive,
            role.Version,
            memberCount,
            permissionCount,
            role.CreatedAt,
            role.UpdatedAt);

    private static bool CanManageMembershipScope(
        UserPrincipal actor,
        string scopeType,
        string scopeReference)
    {
        if (actor.IsPlatformAdmin)
        {
            return true;
        }

        return string.Equals(scopeType, "department", StringComparison.OrdinalIgnoreCase)
            && Guid.TryParse(scopeReference, out var departmentId)
            && actor.DepartmentIds.Contains(departmentId);
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
