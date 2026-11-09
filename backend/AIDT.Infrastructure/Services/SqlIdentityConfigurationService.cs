using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Audit;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;
using AIDT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Services;

public sealed partial class SqlIdentityConfigurationService(
    AidtDbContext dbContext,
    IAuditRepository auditRepository,
    IIdempotencyStore idempotencyStore,
    IUnitOfWork unitOfWork,
    IClock clock) : IIdentityConfigurationService
{
    private static readonly HashSet<string> HierarchyTypes =
        new(["Center", "Department", "Team"], StringComparer.Ordinal);
    private static readonly HashSet<string> RoleTypes =
        new(["System", "Business", "AgentService"], StringComparer.Ordinal);
    private static readonly HashSet<string> DataScopeTypes =
        new(["organization", "department", "self", "project"], StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> HighRiskPermissionCodes =
        new(
            [
                "identity-admin-manage",
                "role-manage",
                "permission-assign",
                "platform-manage",
                "configuration-rollback"
            ],
            StringComparer.Ordinal);

    public async Task<IdentityAdministrationSnapshotDto> GetSnapshotAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken)
    {
        EnsureIdentityRead(user);
        var organizationTree = await GetOrganizationTreeAsync(user, cancellationToken);
        var users = await ListUsersAsync(user, page, pageSize, search, cancellationToken);
        var permissions = await ListPermissionsAsync(user, cancellationToken);
        var roles = await ListRolesAsync(user, null, cancellationToken);
        var requests = await ListAccessRequestsAsync(user, "Pending", cancellationToken);
        return new IdentityAdministrationSnapshotDto(
            organizationTree,
            users,
            permissions,
            roles,
            requests);
    }

    public async Task<IReadOnlyList<DepartmentTreeNodeDto>> GetOrganizationTreeAsync(
        UserPrincipal user,
        CancellationToken cancellationToken)
    {
        EnsureIdentityRead(user);
        user.EnsureOrganizationAccess(user.OrganizationId);
        var organization = await dbContext.Organizations.AsNoTracking()
            .SingleAsync(item => item.Id == user.OrganizationId, cancellationToken);
        var departments = await dbContext.Departments.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
        var visibleIds = user.IsPlatformAdmin
            ? departments.Select(item => item.Id).ToHashSet()
            : VisibleDepartmentIds(user, departments);
        var visible = departments.Where(item => visibleIds.Contains(item.Id)).ToList();
        var managers = await LoadUserNamesAsync(
            visible.Where(item => item.ManagerUserId.HasValue)
                .Select(item => item.ManagerUserId!.Value),
            cancellationToken);
        var rootDepartments = visible
            .Where(item => !item.ParentDepartmentId.HasValue)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .ToList();
        var byParent = visible
            .Where(item => item.ParentDepartmentId.HasValue)
            .GroupBy(item => item.ParentDepartmentId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(item => item.SortOrder).ThenBy(item => item.Name).ToList());

        IReadOnlyList<DepartmentTreeNodeDto> Map(Guid? parentId)
        {
            var rows = parentId.HasValue
                ? byParent.GetValueOrDefault(parentId.Value) ?? []
                : rootDepartments;
            return rows.Select(item => new DepartmentTreeNodeDto(
                item.Id.ToString("D"),
                item.ParentDepartmentId?.ToString("D"),
                item.Code,
                item.Name,
                item.HierarchyType,
                item.Path,
                item.SortOrder,
                item.ManagerUserId?.ToString("D"),
                item.ManagerUserId.HasValue
                    ? managers.GetValueOrDefault(item.ManagerUserId.Value)
                    : null,
                item.Version,
                item.IsActive,
                Map(item.Id))).ToList();
        }

        var companyChildren = Map(null);
        return
        [
            new DepartmentTreeNodeDto(
                organization.Id.ToString("D"),
                null,
                organization.Code,
                organization.Name,
                "Company",
                "/",
                0,
                null,
                null,
                1,
                organization.IsActive,
                companyChildren)
        ];
    }

    public async Task<DepartmentTreeNodeDto> CreateDepartmentAsync(
        UserPrincipal user,
        CreateIamDepartmentRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("identity-admin", AccessAction.Manage);
        user.EnsureOrganizationAccess(user.OrganizationId);
        ArgumentNullException.ThrowIfNull(request);
        var code = NormalizeCode(request.Code, "部门编码", 64);
        var name = DomainChecks.Required(request.Name, "部门名称", 128);
        var hierarchyType = NormalizeHierarchyType(request.HierarchyType);
        DepartmentEntity? parent = null;
        if (!string.IsNullOrWhiteSpace(request.ParentId))
        {
            if (!Guid.TryParse(request.ParentId, out var parentId))
            {
                throw new ValidationException("父级部门 ID 无效。");
            }

            parent = await dbContext.Departments.SingleOrDefaultAsync(
                item => item.Id == parentId
                    && item.OrganizationId == user.OrganizationId
                    && item.IsActive,
                cancellationToken) ?? throw new ValidationException("父级部门不存在或已停用。");
        }

        await ValidateManagerAsync(user.OrganizationId, request.ManagerUserId, cancellationToken);
        if (await dbContext.Departments.AsNoTracking().AnyAsync(
                item => item.OrganizationId == user.OrganizationId && item.Code == code,
                cancellationToken))
        {
            throw new ConflictException(ErrorCodes.Conflict, "部门编码已存在。");
        }

        var now = clock.UtcNow;
        var entity = new DepartmentEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = user.OrganizationId,
            ParentDepartmentId = parent?.Id,
            Code = code,
            Name = name,
            HierarchyType = hierarchyType,
            Path = $"{parent?.Path.TrimEnd('/')}/{code}",
            SortOrder = request.SortOrder,
            ManagerUserId = request.ManagerUserId,
            IsActive = true,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now
        };
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.Departments.Add(entity);
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Identity",
                    "iam.department.created",
                    "Department",
                    entity.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: JsonSerializer.Serialize(
                        new { entity.Code, entity.Name, entity.ParentDepartmentId, entity.Path },
                        JsonSerializerOptions.Web)),
                cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return (await GetOrganizationTreeAsync(user, cancellationToken))
            .SelectMany(Flatten)
            .Single(item => item.Id == entity.Id.ToString("D"));
    }

    public async Task<DepartmentTreeNodeDto> UpdateDepartmentAsync(
        UserPrincipal user,
        Guid departmentId,
        UpdateIamDepartmentRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("identity-admin", AccessAction.Manage);
        ArgumentNullException.ThrowIfNull(request);
        var entity = await LoadDepartmentAsync(user, departmentId, cancellationToken);
        if (entity.Version != request.ExpectedVersion)
        {
            throw new ConflictException(ErrorCodes.Conflict, "部门版本已变化，请刷新后重试。");
        }

        if (entity.IsActive && !request.IsActive)
        {
            var impact = await PreviewDepartmentStatusAsync(
                user,
                departmentId,
                true,
                cancellationToken);
            if (!impact.CanApply)
            {
                throw new ConflictException(ErrorCodes.Conflict, "部门仍被引用，不能通过编辑动作停用。");
            }
        }

        var name = DomainChecks.Required(request.Name, "部门名称", 128);
        var hierarchyType = NormalizeHierarchyType(request.HierarchyType);
        DepartmentEntity? parent = null;
        if (!string.IsNullOrWhiteSpace(request.ParentId))
        {
            if (!Guid.TryParse(request.ParentId, out var parentId)
                || parentId == departmentId)
            {
                throw new ValidationException("父级部门无效，不能指向自身。");
            }

            parent = await dbContext.Departments.SingleOrDefaultAsync(
                item => item.Id == parentId
                    && item.OrganizationId == user.OrganizationId
                    && item.IsActive,
                cancellationToken) ?? throw new ValidationException("父级部门不存在或已停用。");
            if (parent.Path.StartsWith(entity.Path + "/", StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException("不能把部门移动到自己的下级。");
            }
        }

        await ValidateManagerAsync(user.OrganizationId, request.ManagerUserId, cancellationToken);
        var before = JsonSerializer.Serialize(
            new
            {
                entity.Name,
                entity.ParentDepartmentId,
                entity.HierarchyType,
                entity.ManagerUserId,
                entity.SortOrder,
                entity.IsActive
            },
            JsonSerializerOptions.Web);
        var previousPath = entity.Path;
        entity.Name = name;
        entity.ParentDepartmentId = parent?.Id;
        entity.HierarchyType = hierarchyType;
        entity.Path = $"{parent?.Path.TrimEnd('/')}/{entity.Code}";
        if (!string.Equals(previousPath, entity.Path, StringComparison.OrdinalIgnoreCase))
        {
            var descendants = await dbContext.Departments
                .Where(item => item.OrganizationId == user.OrganizationId
                    && item.Id != entity.Id
                    && item.Path.StartsWith(previousPath + "/"))
                .ToListAsync(cancellationToken);
            foreach (var descendant in descendants)
            {
                descendant.Path = entity.Path + descendant.Path[previousPath.Length..];
                descendant.Version++;
                descendant.UpdatedAt = clock.UtcNow;
            }
        }
        entity.ManagerUserId = request.ManagerUserId;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
        entity.Version++;
        entity.UpdatedAt = clock.UtcNow;
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Identity",
                "iam.department.updated",
                "Department",
                entity.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                entity.UpdatedAt,
                before,
                JsonSerializer.Serialize(
                    new
                    {
                        entity.Name,
                        entity.ParentDepartmentId,
                        entity.HierarchyType,
                        entity.ManagerUserId,
                        entity.SortOrder,
                        entity.IsActive
                    },
                    JsonSerializerOptions.Web)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetOrganizationTreeAsync(user, cancellationToken))
            .SelectMany(Flatten)
            .Single(item => item.Id == entity.Id.ToString("D"));
    }

    public async Task<IamImpactPreviewDto> PreviewDepartmentStatusAsync(
        UserPrincipal user,
        Guid departmentId,
        bool deactivate,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("identity-admin", AccessAction.Manage);
        var entity = await LoadDepartmentAsync(user, departmentId, cancellationToken);
        if (!deactivate)
        {
            return new IamImpactPreviewDto(
                "Department",
                departmentId.ToString("D"),
                ["恢复部门及授权导航入口"],
                [],
                true);
        }

        var childCount = await dbContext.Departments.AsNoTracking().CountAsync(
            item => item.ParentDepartmentId == departmentId && item.IsActive,
            cancellationToken);
        var userCount = await dbContext.UserDepartmentMemberships.AsNoTracking().CountAsync(
            item => item.DepartmentId == departmentId && item.IsActive,
            cancellationToken);
        var projectCount = await dbContext.BusinessObjects.AsNoTracking().CountAsync(
            item => item.DepartmentId == departmentId && !item.IsDeleted,
            cancellationToken);
        var workflowCount = await dbContext.WorkflowInstances.AsNoTracking().CountAsync(
            item => item.DepartmentId == departmentId
                && (item.Status == "Running" || item.Status == "Waiting"),
            cancellationToken);
        var fileCount = await dbContext.Attachments.AsNoTracking().CountAsync(
            item => item.DepartmentId == departmentId && item.Status == "Active",
            cancellationToken);
        var references = new List<string>();
        if (childCount > 0) references.Add($"{childCount} 个下级组织");
        if (userCount > 0) references.Add($"{userCount} 名在职/兼职人员");
        if (projectCount > 0) references.Add($"{projectCount} 个业务对象/项目");
        if (workflowCount > 0) references.Add($"{workflowCount} 个运行中流程");
        if (fileCount > 0) references.Add($"{fileCount} 个有效文件");
        return new IamImpactPreviewDto(
            "Department",
            entity.Id.ToString("D"),
            references,
            references.Count == 0 ? [] : ["停用前必须先迁移或归档上述引用。"],
            references.Count == 0);
    }

    public async Task<DepartmentTreeNodeDto> SetDepartmentStatusAsync(
        UserPrincipal user,
        Guid departmentId,
        bool activate,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("identity-admin", AccessAction.Manage);
        var entity = await LoadDepartmentAsync(user, departmentId, cancellationToken);
        if (!activate)
        {
            var preview = await PreviewDepartmentStatusAsync(user, departmentId, true, cancellationToken);
            if (!preview.CanApply)
            {
                throw new ConflictException(
                    ErrorCodes.Conflict,
                    "部门仍被人员、项目、流程或文件引用，不能停用。");
            }
        }

        if (entity.IsActive == activate)
        {
            return (await GetOrganizationTreeAsync(user, cancellationToken))
                .SelectMany(Flatten)
                .Single(item => item.Id == entity.Id.ToString("D"));
        }

        entity.IsActive = activate;
        entity.Version++;
        entity.UpdatedAt = clock.UtcNow;
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Identity",
                activate ? "iam.department.reactivated" : "iam.department.deactivated",
                "Department",
                entity.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                entity.UpdatedAt,
                reason: activate ? "恢复部门" : "停用部门"),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetOrganizationTreeAsync(user, cancellationToken))
            .SelectMany(Flatten)
            .Single(item => item.Id == entity.Id.ToString("D"));
    }

    public async Task<PagedResult<IamUserListItemDto>> ListUsersAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken)
    {
        EnsureIdentityRead(user);
        var departmentIds = await AccessibleDepartmentIdsAsync(user, cancellationToken);
        var visibleUserIds = dbContext.UserDepartmentMemberships.AsNoTracking()
            .Where(item => departmentIds.Contains(item.DepartmentId))
            .Select(item => item.UserId);
        var query = dbContext.Users.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId
                && (user.IsPlatformAdmin || visibleUserIds.Contains(item.Id)));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var departmentUserIds = from membership in dbContext.UserDepartmentMemberships.AsNoTracking()
                                    join department in dbContext.Departments.AsNoTracking()
                                        on membership.DepartmentId equals department.Id
                                    where membership.IsActive
                                        && department.OrganizationId == user.OrganizationId
                                        && department.Name.Contains(term)
                                    select membership.UserId;
            query = query.Where(item =>
                item.UserName.Contains(term)
                || item.DisplayName.Contains(term)
                || departmentUserIds.Contains(item.Id));
        }

        query = query.OrderBy(item => item.DisplayName).ThenBy(item => item.UserName);
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var total = await query.LongCountAsync(cancellationToken);
        var users = await query.Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);
        var mapped = await MapUsersAsync(users, cancellationToken);
        return new PagedResult<IamUserListItemDto>(
            mapped,
            normalizedPage,
            normalizedPageSize,
            total);
    }

    public async Task<IamUserDetailDto?> GetUserAsync(
        UserPrincipal user,
        Guid userId,
        CancellationToken cancellationToken)
    {
        EnsureIdentityRead(user);
        var target = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == userId && item.OrganizationId == user.OrganizationId,
            cancellationToken);
        if (target is null)
        {
            return null;
        }

        if (!user.IsPlatformAdmin)
        {
            var accessible = await AccessibleDepartmentIdsAsync(user, cancellationToken);
            var sharesDepartment = await dbContext.UserDepartmentMemberships.AsNoTracking().AnyAsync(
                item => item.UserId == userId && accessible.Contains(item.DepartmentId),
                cancellationToken);
            if (!sharesDepartment)
            {
                throw new PermissionException(ErrorCodes.DepartmentScopeDenied, "不能查看其他部门人员。");
            }
        }

        var mapped = (await MapUsersAsync([target], cancellationToken)).Single();
        var memberships = await dbContext.UserDepartmentMemberships.AsNoTracking()
            .Where(item => item.UserId == userId)
            .Join(
                dbContext.Departments.AsNoTracking(),
                membership => membership.DepartmentId,
                department => department.Id,
                (membership, department) => new UserDepartmentMembershipDto(
                    department.Id.ToString("D"),
                    department.Name,
                    membership.IsPrimary,
                    membership.IsManager,
                    membership.IsActive))
            .ToListAsync(cancellationToken);
        var scopes = await LoadDataScopesAsync(userId, cancellationToken);
        var roles = await LoadUserRolesAsync(userId, cancellationToken);
        return new IamUserDetailDto(mapped, memberships, scopes, roles);
    }

    public async Task<IamUserDetailDto> InviteUserAsync(
        UserPrincipal user,
        InviteUserRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("identity-admin", AccessAction.Manage);
        ValidateIdempotencyKey(idempotencyKey);
        var normalized = NormalizeInvite(request.UserName, request.DisplayName, request.DepartmentId);
        user.EnsureDepartmentAccess(normalized.DepartmentId);
        await EnsureDepartmentAsync(user.OrganizationId, normalized.DepartmentId, cancellationToken);
        var hash = Hash(new { request, normalized });
        var scope = $"iam:user:invite:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.RequestHash != hash)
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同邀请请求。");
            }

            return await GetUserAsync(user, Guid.Parse(existing.ResponseId), cancellationToken)
                ?? throw new NotFoundException("幂等结果对应的人员不存在。");
        }

        if (await dbContext.Users.AsNoTracking().AnyAsync(
                item => item.OrganizationId == user.OrganizationId
                    && item.UserName == normalized.UserName,
                cancellationToken))
        {
            throw new ConflictException(ErrorCodes.Conflict, "登录名已存在。");
        }

        var now = clock.UtcNow;
        var entity = new UserEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = user.OrganizationId,
            UserName = normalized.UserName,
            DisplayName = normalized.DisplayName,
            IsServiceIdentity = false,
            IsActive = false,
            InvitationStatus = "Invited",
            InvitedAt = now,
            Version = 1,
            CreatedAt = now
        };
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.Users.Add(entity);
            dbContext.UserDepartmentMemberships.Add(new UserDepartmentMembershipEntity
            {
                UserId = entity.Id,
                DepartmentId = normalized.DepartmentId,
                IsPrimary = request.IsPrimary,
                IsManager = request.IsManager,
                IsActive = true,
                JoinedAt = now
            });
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Identity",
                    "iam.user.invited",
                    "User",
                    entity.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: JsonSerializer.Serialize(
                        new { entity.UserName, entity.DisplayName, normalized.DepartmentId },
                        JsonSerializerOptions.Web)),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "User",
                    entity.Id.ToString("D"),
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

        return await GetUserAsync(user, entity.Id, cancellationToken)
            ?? throw new NotFoundException("邀请人员创建失败。");
    }

    public async Task<IReadOnlyList<IamUserListItemDto>> ImportUsersAsync(
        UserPrincipal user,
        BulkImportUsersRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("identity-admin", AccessAction.Manage);
        ValidateIdempotencyKey(idempotencyKey);
        ArgumentNullException.ThrowIfNull(request);
        if (request.Users.Count is < 1 or > 200)
        {
            throw new ValidationException("批量导入必须包含 1—200 名人员。");
        }

        var normalized = request.Users
            .Select(item => NormalizeInvite(item.UserName, item.DisplayName, item.DepartmentId)
                with
            {
                IsPrimary = item.IsPrimary,
                IsManager = item.IsManager
            })
            .ToList();
        if (normalized.Select(item => item.UserName).Distinct(StringComparer.Ordinal).Count()
            != normalized.Count)
        {
            throw new ValidationException("批量导入中存在重复登录名。");
        }

        foreach (var departmentId in normalized.Select(item => item.DepartmentId).Distinct())
        {
            user.EnsureDepartmentAccess(departmentId);
            await EnsureDepartmentAsync(user.OrganizationId, departmentId, cancellationToken);
        }

        var existingNames = await dbContext.Users.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId
                && normalized.Select(seed => seed.UserName).Contains(item.UserName))
            .Select(item => item.UserName)
            .ToListAsync(cancellationToken);
        if (existingNames.Count > 0)
        {
            throw new ConflictException(
                ErrorCodes.Conflict,
                "以下登录名已存在：" + string.Join("、", existingNames));
        }

        var hash = Hash(normalized);
        var scope = $"iam:user:import:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.RequestHash != hash)
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同批量导入。");
            }

            var ids = JsonSerializer.Deserialize<string[]>(existing.ResponseId) ?? [];
            var result = new List<IamUserListItemDto>();
            foreach (var id in ids)
            {
                var detail = await GetUserAsync(user, Guid.Parse(id), cancellationToken);
                if (detail is not null) result.Add(detail.User);
            }

            return result;
        }

        var now = clock.UtcNow;
        var created = new List<UserEntity>();
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var item in normalized)
            {
                var entity = new UserEntity
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = user.OrganizationId,
                    UserName = item.UserName,
                    DisplayName = item.DisplayName,
                    IsActive = false,
                    InvitationStatus = "Invited",
                    InvitedAt = now,
                    Version = 1,
                    CreatedAt = now
                };
                created.Add(entity);
                dbContext.Users.Add(entity);
                dbContext.UserDepartmentMemberships.Add(new UserDepartmentMembershipEntity
                {
                    UserId = entity.Id,
                    DepartmentId = item.DepartmentId,
                    IsPrimary = item.IsPrimary,
                    IsManager = item.IsManager,
                    IsActive = true,
                    JoinedAt = now
                });
            }

            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Identity",
                    "iam.user.imported",
                    "User",
                    "batch",
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: JsonSerializer.Serialize(
                        created.Select(item => new { item.Id, item.UserName, item.DisplayName }),
                        JsonSerializerOptions.Web)),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "UserBatch",
                    JsonSerializer.Serialize(created.Select(item => item.Id.ToString("D")), JsonSerializerOptions.Web),
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

        return await MapUsersAsync(created, cancellationToken);
    }

    public async Task<IamUserDetailDto> SetUserMembershipsAsync(
        UserPrincipal user,
        Guid userId,
        SetUserMembershipsRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("identity-admin", AccessAction.Manage);
        if (userId == user.UserId)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "禁止修改自己的部门归属。");
        }

        var target = await dbContext.Users.SingleOrDefaultAsync(
            item => item.Id == userId && item.OrganizationId == user.OrganizationId,
            cancellationToken) ?? throw new NotFoundException("人员不存在。");
        if (target.Version != request.ExpectedVersion)
        {
            throw new ConflictException(ErrorCodes.Conflict, "人员版本已变化，请刷新后重试。");
        }

        if (request.Memberships.Count == 0
            || request.Memberships.Count(item => item.IsPrimary && item.IsActive) != 1)
        {
            throw new ValidationException("必须且只能设置一个有效主部门。");
        }

        foreach (var item in request.Memberships.Where(item => item.IsActive))
        {
            user.EnsureDepartmentAccess(item.DepartmentId);
            await EnsureDepartmentAsync(user.OrganizationId, item.DepartmentId, cancellationToken);
        }

        var existing = await dbContext.UserDepartmentMemberships
            .Where(item => item.UserId == userId)
            .ToListAsync(cancellationToken);
        var now = clock.UtcNow;
        foreach (var item in existing)
        {
            var desired = request.Memberships.FirstOrDefault(row => row.DepartmentId == item.DepartmentId);
            if (desired is null)
            {
                item.IsActive = false;
                item.IsPrimary = false;
                item.IsManager = false;
            }
            else
            {
                item.IsActive = desired.IsActive;
                item.IsPrimary = desired.IsActive && desired.IsPrimary;
                item.IsManager = desired.IsActive && desired.IsManager;
            }
        }

        foreach (var item in request.Memberships.Where(row => row.IsActive))
        {
            if (existing.Any(row => row.DepartmentId == item.DepartmentId))
            {
                continue;
            }

            dbContext.UserDepartmentMemberships.Add(new UserDepartmentMembershipEntity
            {
                UserId = userId,
                DepartmentId = item.DepartmentId,
                IsPrimary = item.IsPrimary,
                IsManager = item.IsManager,
                IsActive = true,
                JoinedAt = now
            });
        }

        target.Version++;
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Identity",
                "iam.membership.updated",
                "User",
                userId.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                after: JsonSerializer.Serialize(request.Memberships, JsonSerializerOptions.Web)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetUserAsync(user, userId, cancellationToken)
            ?? throw new NotFoundException("人员不存在。");
    }

    public async Task<IamUserDetailDto> SetUserDataScopesAsync(
        UserPrincipal user,
        Guid userId,
        SetUserDataScopesRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("data-scope", AccessAction.Update);
        user.EnsureCan("identity-admin", AccessAction.Manage);
        if (userId == user.UserId)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "禁止修改自己的数据范围。");
        }

        var target = await dbContext.Users.SingleOrDefaultAsync(
            item => item.Id == userId && item.OrganizationId == user.OrganizationId,
            cancellationToken) ?? throw new NotFoundException("人员不存在。");
        if (target.Version != request.ExpectedVersion)
        {
            throw new ConflictException(ErrorCodes.Conflict, "人员版本已变化，请刷新后重试。");
        }

        var normalized = new List<DataScopeEntity>();
        foreach (var scope in request.Scopes)
        {
            var scopeType = scope.ScopeType.Trim().ToLowerInvariant();
            if (!DataScopeTypes.Contains(scopeType))
            {
                throw new ValidationException("数据范围只支持 organization、department、self、project。");
            }

            var reference = scope.ScopeReference?.Trim() ?? string.Empty;
            switch (scopeType)
            {
                case "organization":
                    reference = user.OrganizationId.ToString("D");
                    break;
                case "department":
                    if (!Guid.TryParse(reference, out var departmentId))
                    {
                        throw new ValidationException("部门数据范围 ID 无效。");
                    }

                    await EnsureDepartmentAsync(user.OrganizationId, departmentId, cancellationToken);
                    reference = departmentId.ToString("D");
                    break;
                case "self":
                    reference = userId.ToString("D");
                    break;
                case "project":
                    reference = string.IsNullOrWhiteSpace(reference) ? "participant" : "participant";
                    break;
            }

            if (normalized.Any(item =>
                    item.ScopeType == scopeType && item.ScopeReference == reference))
            {
                continue;
            }

            normalized.Add(new DataScopeEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ScopeType = scopeType,
                ScopeReference = reference
            });
        }

        if (normalized.Count == 0)
        {
            throw new ValidationException("至少配置一个数据范围。");
        }

        var existing = await dbContext.DataScopes.Where(item => item.UserId == userId).ToListAsync(
            cancellationToken);
        dbContext.DataScopes.RemoveRange(existing);
        dbContext.DataScopes.AddRange(normalized);
        target.Version++;
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Identity",
                "iam.data_scope.updated",
                "User",
                userId.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                clock.UtcNow,
                before: JsonSerializer.Serialize(
                    existing.Select(item => new { item.ScopeType, item.ScopeReference }),
                    JsonSerializerOptions.Web),
                after: JsonSerializer.Serialize(
                    normalized.Select(item => new { item.ScopeType, item.ScopeReference }),
                    JsonSerializerOptions.Web)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetUserAsync(user, userId, cancellationToken)
            ?? throw new NotFoundException("人员不存在。");
    }

    public async Task<IamUserDetailDto> SetUserStatusAsync(
        UserPrincipal user,
        Guid userId,
        bool activate,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("identity-admin", AccessAction.Manage);
        if (userId == user.UserId && !activate)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "不能停用自己的账号。");
        }

        var target = await dbContext.Users.SingleOrDefaultAsync(
            item => item.Id == userId && item.OrganizationId == user.OrganizationId,
            cancellationToken) ?? throw new NotFoundException("人员不存在。");
        target.IsActive = activate;
        target.InvitationStatus = activate ? "Active" : "Suspended";
        if (activate)
        {
            target.ActivatedAt ??= clock.UtcNow;
        }

        target.Version++;
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Identity",
                activate ? "iam.user.reactivated" : "iam.user.deactivated",
                "User",
                userId.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                clock.UtcNow,
                reason: activate ? "重新启用/本地开通" : "停用人员"),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetUserAsync(user, userId, cancellationToken)
            ?? throw new NotFoundException("人员不存在。");
    }

    public async Task<IReadOnlyList<PermissionCatalogItemDto>> ListPermissionsAsync(
        UserPrincipal user,
        CancellationToken cancellationToken)
    {
        EnsureRoleRead(user);
        return await dbContext.Permissions.AsNoTracking()
            .OrderBy(item => item.Resource)
            .ThenBy(item => item.Action)
            .Select(item => new PermissionCatalogItemDto(
                item.Id.ToString("D"),
                item.Code,
                item.Resource,
                item.Action,
                item.Resource + " · " + item.Action,
                IsHighRisk(item.Code, item.Action) ? "High" : "Standard"))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoleDto>> ListRolesAsync(
        UserPrincipal user,
        string? roleType,
        CancellationToken cancellationToken)
    {
        EnsureRoleRead(user);
        var query = dbContext.Roles.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(roleType))
        {
            var normalizedType = NormalizeRoleType(roleType);
            query = query.Where(item => item.RoleType == normalizedType);
        }

        var roles = await query.OrderBy(item => item.IsSystem ? 0 : 1)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
        return await MapRolesAsync(roles, user.OrganizationId, cancellationToken);
    }

    public Task<RoleDto> CreateRoleAsync(
        UserPrincipal user,
        CreateRoleRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) =>
        CreateRoleCoreAsync(user, request, idempotencyKey, traceId, null, cancellationToken);

    public async Task<RoleDto> CopyRoleAsync(
        UserPrincipal user,
        Guid roleId,
        CopyRoleRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("role", AccessAction.Create);
        var source = await dbContext.Roles.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == roleId, cancellationToken)
            ?? throw new NotFoundException("源角色不存在。");
        var created = await CreateRoleCoreAsync(
            user,
            new CreateRoleRequest(
                request.Code,
                request.Name,
                source.RoleType == "System" ? "Business" : source.RoleType,
                request.Description ?? source.Description),
            idempotencyKey,
            traceId,
            source.Id,
            cancellationToken);
        return created;
    }

    public async Task<RoleDto> UpdateRoleAsync(
        UserPrincipal user,
        Guid roleId,
        UpdateRoleRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("role", AccessAction.Update);
        var role = await LoadRoleAsync(roleId, cancellationToken);
        if (role.IsSystem)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "系统保留角色不能修改。");
        }

        if (role.Version != request.ExpectedVersion)
        {
            throw new ConflictException(ErrorCodes.Conflict, "角色版本已变化，请刷新后重试。");
        }

        var memberCount = await CountEffectiveRoleMembersAsync(
            roleId,
            user.OrganizationId,
            cancellationToken);
        var roleType = NormalizeRoleType(request.RoleType);
        if (memberCount > 0 && role.RoleType != roleType)
        {
            throw new ConflictException(ErrorCodes.Conflict, "使用中的角色不能改变角色类型。");
        }

        if (!request.IsActive)
        {
            if (memberCount > 0)
            {
                throw new ConflictException(ErrorCodes.Conflict, "使用中的角色不能停用，请先迁移成员。");
            }

            var workflowTaskCount = await dbContext.WorkflowTasks.AsNoTracking().CountAsync(
                item => item.BusinessRoleId == roleId && item.Status == "Pending",
                cancellationToken);
            if (workflowTaskCount > 0)
            {
                throw new ConflictException(ErrorCodes.Conflict, "角色仍绑定待处理流程任务，不能停用。");
            }
        }

        var before = JsonSerializer.Serialize(
            new { role.Name, role.RoleType, role.Description, role.IsActive },
            JsonSerializerOptions.Web);
        role.Name = DomainChecks.Required(request.Name, "角色名称", 128);
        role.RoleType = roleType;
        role.Description = NormalizeOptional(request.Description, 1000);
        role.IsActive = request.IsActive;
        role.Version++;
        role.UpdatedAt = clock.UtcNow;
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Identity",
                "iam.role.updated",
                "Role",
                role.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                role.UpdatedAt,
                before,
                JsonSerializer.Serialize(
                    new { role.Name, role.RoleType, role.Description, role.IsActive },
                    JsonSerializerOptions.Web)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await MapRolesAsync([role], user.OrganizationId, cancellationToken)).Single();
    }

    public async Task<IReadOnlyList<RoleMemberDto>> ListRoleMembersAsync(
        UserPrincipal user,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        EnsureRoleRead(user);
        _ = await LoadRoleAsync(roleId, cancellationToken);
        return await LoadRoleMembersAsync(roleId, user, cancellationToken);
    }

    public async Task<RoleDto> SetRolePermissionsAsync(
        UserPrincipal user,
        Guid roleId,
        SetRolePermissionsRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("role", AccessAction.Manage);
        user.EnsureCan("permission", AccessAction.Assign);
        ValidateIdempotencyKey(idempotencyKey);
        var role = await LoadRoleAsync(roleId, cancellationToken);
        if (role.IsSystem)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "系统保留角色权限不可修改。");
        }

        if (role.Version != request.ExpectedRoleVersion)
        {
            throw new ConflictException(ErrorCodes.Conflict, "角色版本已变化，请刷新后重试。");
        }

        var permissions = await dbContext.Permissions.AsNoTracking()
            .Where(item => request.PermissionIds.Contains(item.Id))
            .ToListAsync(cancellationToken);
        if (permissions.Count != request.PermissionIds.Distinct().Count())
        {
            throw new ValidationException("角色权限包含不存在的权限。");
        }

        var actorPermissions = user.Permissions
            .Select(item => $"{item.Resource}:{item.Action}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var delegatedBeyondSelf = permissions.Any(permission =>
            !actorPermissions.Contains($"{permission.Resource}:{permission.Action}"));
        if (delegatedBeyondSelf)
        {
            throw new PermissionException(
                ErrorCodes.PermissionDenied,
                "不能分配或保留超出当前身份有效权限的角色权限。");
        }

        var highRisk = permissions.Any(item => IsHighRisk(item.Code, item.Action));
        if (highRisk)
        {
            await CreateAccessRequestAsync(
                user,
                "RolePermissions",
                null,
                roleId,
                new { PermissionIds = permissions.Select(item => item.Id).ToArray() },
                "角色包含管理型高风险权限，需要第二名平台管理员审核。",
                idempotencyKey,
                traceId,
                cancellationToken);
            return (await MapRolesAsync([role], user.OrganizationId, cancellationToken)).Single();
        }

        await ApplyRolePermissionsAsync(role, request.PermissionIds, user.UserId, traceId, cancellationToken);
        return (await MapRolesAsync([role], user.OrganizationId, cancellationToken)).Single();
    }

    public async Task<IReadOnlyList<RoleMemberDto>> SetRoleMembersAsync(
        UserPrincipal user,
        Guid roleId,
        SetRoleMembersRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("permission", AccessAction.Assign);
        ValidateIdempotencyKey(idempotencyKey);
        var role = await LoadRoleAsync(roleId, cancellationToken);
        if (!role.IsActive)
        {
            throw new ConflictException(ErrorCodes.Conflict, "已停用角色不能分配成员。");
        }

        if (role.Version != request.ExpectedRoleVersion)
        {
            throw new ConflictException(ErrorCodes.Conflict, "角色版本已变化，请刷新后重试。");
        }

        var normalized = await NormalizeRoleMembersAsync(user, request.Members, cancellationToken);
        if (normalized.Any(item => item.UserId == user.UserId && item.IsActive))
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "禁止给自己分配角色。");
        }

        if (role.IsSystem)
        {
            if (!user.IsPlatformAdmin)
            {
                throw new PermissionException(ErrorCodes.PermissionDenied, "只有平台管理员可以分配系统角色。");
            }

            await CreateAccessRequestAsync(
                user,
                "RoleAssignment",
                null,
                roleId,
                new { Members = normalized },
                $"分配系统角色 {role.Code} 属于高风险授权，需要第二名平台管理员审核。",
                idempotencyKey,
                traceId,
                cancellationToken);
            return await LoadRoleMembersAsync(roleId, user, cancellationToken);
        }

        await ApplyRoleMembersAsync(role, normalized, user, traceId, cancellationToken);
        return await LoadRoleMembersAsync(roleId, user, cancellationToken);
    }

    public async Task<RoleDto> SetRoleStatusAsync(
        UserPrincipal user,
        Guid roleId,
        bool activate,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("role", AccessAction.Manage);
        var role = await LoadRoleAsync(roleId, cancellationToken);
        if (role.IsSystem && !activate)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "系统保留角色不能停用。");
        }

        if (!activate)
        {
            var memberCount = await CountEffectiveRoleMembersAsync(
                roleId,
                user.OrganizationId,
                cancellationToken);
            if (memberCount > 0)
            {
                throw new ConflictException(ErrorCodes.Conflict, "使用中的角色不能停用，请先迁移成员。");
            }

            var workflowTaskCount = await dbContext.WorkflowTasks.AsNoTracking().CountAsync(
                item => item.BusinessRoleId == roleId && item.Status == "Pending",
                cancellationToken);
            if (workflowTaskCount > 0)
            {
                throw new ConflictException(ErrorCodes.Conflict, "角色仍绑定待处理流程任务，不能停用。");
            }
        }

        role.IsActive = activate;
        role.Version++;
        role.UpdatedAt = clock.UtcNow;
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Identity",
                activate ? "iam.role.reactivated" : "iam.role.deactivated",
                "Role",
                roleId.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                role.UpdatedAt,
                reason: activate ? "恢复角色" : "停用角色"),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await MapRolesAsync([role], user.OrganizationId, cancellationToken)).Single();
    }

    public async Task<IReadOnlyList<AccessChangeRequestDto>> ListAccessRequestsAsync(
        UserPrincipal user,
        string? status,
        CancellationToken cancellationToken)
    {
        EnsureIdentityRead(user);
        var query = dbContext.AccessChangeRequests.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId);
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(item => item.Status == status);
        }

        var rows = await query.OrderByDescending(item => item.RequestedAt)
            .Take(200)
            .ToListAsync(cancellationToken);
        var requesterIds = rows.Select(item => item.RequestedBy).Distinct().ToArray();
        var names = await LoadUserNamesAsync(requesterIds, cancellationToken);
        return rows.Select(item => new AccessChangeRequestDto(
            item.Id.ToString("D"),
            item.RequestType,
            item.TargetUserId?.ToString("D"),
            item.TargetRoleId?.ToString("D"),
            item.RiskReason,
            item.Status,
            item.RequestedBy.ToString("D"),
            names.GetValueOrDefault(item.RequestedBy) ?? "未知申请人",
            item.RequestedAt,
            item.ReviewedBy?.ToString("D"),
            item.ReviewedAt,
            item.ReviewComment)).ToList();
    }

    public async Task<AccessChangeRequestDto> ReviewAccessRequestAsync(
        UserPrincipal user,
        Guid requestId,
        bool approve,
        ReviewAccessChangeRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("identity-admin", AccessAction.Manage);
        ValidateIdempotencyKey(idempotencyKey);
        var entity = await dbContext.AccessChangeRequests.SingleOrDefaultAsync(
            item => item.Id == requestId && item.OrganizationId == user.OrganizationId,
            cancellationToken) ?? throw new NotFoundException("授权申请不存在。");
        if (entity.RequestedBy == user.UserId)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "不能审核自己的高危提权申请。");
        }

        if (entity.Status is "Approved" or "Rejected" or "Applied")
        {
            return (await ListAccessRequestsAsync(user, null, cancellationToken))
                .Single(item => item.Id == requestId.ToString("D"));
        }

        if (entity.Status != "Pending")
        {
            throw new ConflictException(ErrorCodes.Conflict, "当前授权申请不能审核。");
        }

        var reviewComment = DomainChecks.Required(request.Comment, "审核意见", 1000);
        var now = clock.UtcNow;
        if (!approve)
        {
            entity.Status = "Rejected";
            entity.ReviewedBy = user.UserId;
            entity.ReviewedAt = now;
            entity.ReviewComment = reviewComment;
        }
        else
        {
            await ApplyAccessRequestAsync(entity, user, traceId, now, cancellationToken);
            entity.Status = "Applied";
            entity.ReviewedBy = user.UserId;
            entity.ReviewedAt = now;
            entity.ReviewComment = reviewComment;
        }

        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Identity",
                approve ? "iam.access_request.approved" : "iam.access_request.rejected",
                "AccessChangeRequest",
                entity.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                reason: entity.ReviewComment),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await ListAccessRequestsAsync(user, null, cancellationToken))
            .Single(item => item.Id == entity.Id.ToString("D"));
    }

    private async Task<RoleDto> CreateRoleCoreAsync(
        UserPrincipal user,
        CreateRoleRequest request,
        string idempotencyKey,
        string traceId,
        Guid? copyFromRoleId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("role", AccessAction.Create);
        ValidateIdempotencyKey(idempotencyKey);
        var code = NormalizeCode(request.Code, "角色编码", 64);
        var name = DomainChecks.Required(request.Name, "角色名称", 128);
        var roleType = NormalizeRoleType(request.RoleType);
        if (roleType == "System")
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "系统角色只能由受控迁移创建。");
        }

        if (roleType == "AgentService" && !user.IsPlatformAdmin)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "只有平台管理员可以创建 Agent 服务角色。");
        }

        var hash = Hash(new { request, copyFromRoleId });
        var scope = $"iam:role:create:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.RequestHash != hash)
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同角色请求。");
            }

            var existingRole = await LoadRoleAsync(Guid.Parse(existing.ResponseId), cancellationToken);
            return (await MapRolesAsync(
                [existingRole],
                user.OrganizationId,
                cancellationToken)).Single();
        }

        if (await dbContext.Roles.AsNoTracking().AnyAsync(item => item.Code == code, cancellationToken))
        {
            throw new ConflictException(ErrorCodes.Conflict, "角色编码已存在。");
        }

        var now = clock.UtcNow;
        var role = new RoleEntity
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            RoleType = roleType,
            Description = NormalizeOptional(request.Description, 1000),
            IsSystem = false,
            IsActive = true,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now
        };
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.Roles.Add(role);
            if (copyFromRoleId.HasValue)
            {
                var sourcePermissions = await dbContext.RolePermissions.AsNoTracking()
                    .Where(item => item.RoleId == copyFromRoleId.Value)
                    .Select(item => item.PermissionId)
                    .ToListAsync(cancellationToken);
                dbContext.RolePermissions.AddRange(
                    sourcePermissions.Select(permissionId => new RolePermissionEntity
                    {
                        RoleId = role.Id,
                        PermissionId = permissionId
                    }));
            }

            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Identity",
                    copyFromRoleId.HasValue ? "iam.role.copied" : "iam.role.created",
                    "Role",
                    role.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: JsonSerializer.Serialize(
                        new { role.Code, role.Name, role.RoleType, copyFromRoleId },
                        JsonSerializerOptions.Web)),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "Role",
                    role.Id.ToString("D"),
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

        return (await MapRolesAsync([role], user.OrganizationId, cancellationToken)).Single();
    }

    private async Task ApplyRolePermissionsAsync(
        RoleEntity role,
        IReadOnlyList<Guid> desiredPermissionIds,
        Guid actorId,
        string traceId,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.RolePermissions
            .Where(item => item.RoleId == role.Id)
            .ToListAsync(cancellationToken);
        var desired = desiredPermissionIds.Distinct().ToHashSet();
        dbContext.RolePermissions.RemoveRange(
            existing.Where(item => !desired.Contains(item.PermissionId)));
        dbContext.RolePermissions.AddRange(
            desired.Where(permissionId => existing.All(item => item.PermissionId != permissionId))
                .Select(permissionId => new RolePermissionEntity
                {
                    RoleId = role.Id,
                    PermissionId = permissionId
                }));
        role.Version++;
        role.UpdatedAt = clock.UtcNow;
        await auditRepository.AddAsync(
            AuditEvent.Record(
                actorId,
                "Identity",
                "iam.role_permissions.updated",
                "Role",
                role.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                role.UpdatedAt,
                before: JsonSerializer.Serialize(
                    existing.Select(item => item.PermissionId),
                    JsonSerializerOptions.Web),
                after: JsonSerializer.Serialize(desired, JsonSerializerOptions.Web)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyRoleMembersAsync(
        RoleEntity role,
        IReadOnlyList<SetRoleMemberItem> desired,
        UserPrincipal actor,
        string traceId,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.UserRoleAssignments
            .Where(item => item.RoleId == role.Id)
            .ToListAsync(cancellationToken);
        var now = clock.UtcNow;
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
                RoleId = role.Id,
                ScopeType = item.ScopeType,
                ScopeReference = item.ScopeReference,
                IsActive = true,
                AssignedBy = actor.UserId,
                CreatedAt = now
            });
        }

        role.Version++;
        role.UpdatedAt = now;
        await auditRepository.AddAsync(
            AuditEvent.Record(
                actor.UserId,
                "Identity",
                "iam.user_roles.assigned",
                "Role",
                role.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                after: JsonSerializer.Serialize(desired, JsonSerializerOptions.Web)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task CreateAccessRequestAsync(
        UserPrincipal user,
        string requestType,
        Guid? targetUserId,
        Guid? targetRoleId,
        object payload,
        string riskReason,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.AccessChangeRequests.AsNoTracking().SingleOrDefaultAsync(
            item => item.RequestedBy == user.UserId
                && item.IdempotencyKey == idempotencyKey,
            cancellationToken);
        if (existing is not null)
        {
            return;
        }

        var now = clock.UtcNow;
        var entity = new AccessChangeRequestEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = user.OrganizationId,
            RequestType = requestType,
            TargetUserId = targetUserId,
            TargetRoleId = targetRoleId,
            PayloadJson = JsonSerializer.Serialize(payload, JsonSerializerOptions.Web),
            RiskReason = DomainChecks.Required(riskReason, "高风险原因", 1000),
            Status = "Pending",
            RequestedBy = user.UserId,
            RequestedAt = now,
            IdempotencyKey = idempotencyKey
        };
        dbContext.AccessChangeRequests.Add(entity);
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Identity",
                "iam.access_request.created",
                "AccessChangeRequest",
                entity.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                after: entity.PayloadJson,
                reason: entity.RiskReason),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyAccessRequestAsync(
        AccessChangeRequestEntity request,
        UserPrincipal reviewer,
        string traceId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!request.TargetRoleId.HasValue)
        {
            throw new ValidationException("高风险授权申请缺少目标角色。");
        }

        var role = await LoadRoleAsync(request.TargetRoleId.Value, cancellationToken);
        if (request.RequestType == "RoleAssignment")
        {
            var payload = JsonSerializer.Deserialize<RoleAssignmentPayload>(
                request.PayloadJson,
                JsonSerializerOptions.Web)
                ?? throw new ValidationException("角色分配申请内容无效。");
            await ApplyRoleMembersAsync(
                role,
                payload.Members,
                reviewer,
                traceId,
                cancellationToken);
            return;
        }

        if (request.RequestType == "RolePermissions")
        {
            var payload = JsonSerializer.Deserialize<RolePermissionPayload>(
                request.PayloadJson,
                JsonSerializerOptions.Web)
                ?? throw new ValidationException("角色权限申请内容无效。");
            await ApplyRolePermissionsAsync(
                role,
                payload.PermissionIds,
                reviewer.UserId,
                traceId,
                cancellationToken);
            return;
        }

        throw new ValidationException("不支持的高风险授权类型。");
    }

    private async Task<List<SetRoleMemberItem>> NormalizeRoleMembersAsync(
        UserPrincipal user,
        IReadOnlyList<SetRoleMemberItem> members,
        CancellationToken cancellationToken)
    {
        if (members.Count > 500)
        {
            throw new ValidationException("单次角色成员变更不能超过 500 人。");
        }

        var result = new List<SetRoleMemberItem>();
        foreach (var member in members)
        {
            var scopeType = member.ScopeType.Trim().ToLowerInvariant();
            if (scopeType is not ("organization" or "department"))
            {
                throw new ValidationException("角色范围只支持 organization 或 department。");
            }

            var scopeReference = scopeType == "organization"
                ? user.OrganizationId.ToString("D")
                : member.ScopeReference.Trim();
            if (scopeType == "organization" && !user.IsPlatformAdmin)
            {
                throw new PermissionException(
                    ErrorCodes.PermissionDenied,
                    "组织范围角色分配只能由平台管理员执行。");
            }

            if (scopeType == "department")
            {
                if (!Guid.TryParse(scopeReference, out var departmentId))
                {
                    throw new ValidationException("角色部门范围 ID 无效。");
                }

                user.EnsureDepartmentAccess(departmentId);
                await EnsureDepartmentAsync(user.OrganizationId, departmentId, cancellationToken);
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

            result.Add(member with { ScopeType = scopeType, ScopeReference = scopeReference });
        }

        return result
            .GroupBy(item => (item.UserId, item.ScopeType, item.ScopeReference))
            .Select(group => group.Last())
            .ToList();
    }

    private async Task<IReadOnlyList<IamUserListItemDto>> MapUsersAsync(
        IReadOnlyList<UserEntity> users,
        CancellationToken cancellationToken)
    {
        var userIds = users.Select(item => item.Id).ToArray();
        var memberships = await dbContext.UserDepartmentMemberships.AsNoTracking()
            .Where(item => userIds.Contains(item.UserId) && item.IsActive)
            .Join(
                dbContext.Departments.AsNoTracking(),
                membership => membership.DepartmentId,
                department => department.Id,
                (membership, department) => new { membership, department })
            .ToListAsync(cancellationToken);
        var legacyRoles = await (
            from userRole in dbContext.UserRoles.AsNoTracking()
            join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where userIds.Contains(userRole.UserId) && role.IsActive
            select new { userRole.UserId, role.Code }).ToListAsync(cancellationToken);
        var scopedRoles = await (
            from assignment in dbContext.UserRoleAssignments.AsNoTracking()
            join role in dbContext.Roles.AsNoTracking() on assignment.RoleId equals role.Id
            where userIds.Contains(assignment.UserId) && assignment.IsActive && role.IsActive
            select new { assignment.UserId, role.Code }).ToListAsync(cancellationToken);
        var scopes = await dbContext.DataScopes.AsNoTracking()
            .Where(item => userIds.Contains(item.UserId))
            .ToListAsync(cancellationToken);
        return users.Select(user =>
        {
            var primary = memberships.FirstOrDefault(item =>
                item.membership.UserId == user.Id && item.membership.IsPrimary)
                ?? memberships.FirstOrDefault(item => item.membership.UserId == user.Id);
            var roles = legacyRoles.Where(item => item.UserId == user.Id)
                .Select(item => item.Code)
                .Concat(scopedRoles.Where(item => item.UserId == user.Id).Select(item => item.Code))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(item => item)
                .ToList();
            var dataScopeTypes = scopes.Where(item => item.UserId == user.Id)
                .Select(item => item.ScopeType)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item)
                .ToList();
            return new IamUserListItemDto(
                user.Id.ToString("D"),
                user.UserName,
                user.DisplayName,
                user.IsServiceIdentity,
                user.IsActive,
                user.InvitationStatus,
                primary?.department.Id.ToString("D") ?? string.Empty,
                primary?.department.Name ?? string.Empty,
                user.Version,
                roles,
                dataScopeTypes,
                user.CreatedAt);
        }).ToList();
    }

    private async Task<List<RoleMemberDto>> LoadRoleMembersAsync(
        Guid roleId,
        UserPrincipal? viewer,
        CancellationToken cancellationToken)
    {
        var assignments = await dbContext.UserRoleAssignments.AsNoTracking()
            .Where(item => item.RoleId == roleId)
            .Join(
                dbContext.Users.AsNoTracking(),
                assignment => assignment.UserId,
                user => user.Id,
                (assignment, user) => new { assignment, user })
            .ToListAsync(cancellationToken);
        if (viewer is not null)
        {
            assignments = assignments
                .Where(item => CanManageMembershipScope(
                    viewer,
                    item.assignment.ScopeType,
                    item.assignment.ScopeReference))
                .ToList();
        }
        var departmentIds = assignments
            .Where(item => item.assignment.ScopeType == "department"
                && Guid.TryParse(item.assignment.ScopeReference, out _))
            .Select(item => Guid.Parse(item.assignment.ScopeReference))
            .Distinct()
            .ToArray();
        var departments = await dbContext.Departments.AsNoTracking()
            .Where(item => departmentIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        var scoped = assignments
            .OrderByDescending(item => item.assignment.IsActive)
            .ThenBy(item => item.user.DisplayName)
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
                item.assignment.CreatedAt))
            .ToList();
        if (viewer is not null && !viewer.IsPlatformAdmin)
        {
            return scoped;
        }

        var legacy = await (
            from userRole in dbContext.UserRoles.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on userRole.UserId equals user.Id
            where userRole.RoleId == roleId
                && (viewer == null || user.OrganizationId == viewer.OrganizationId)
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
            .Concat(legacy.Where(item =>
                scoped.All(scopedItem =>
                    scopedItem.UserId != item.UserId || !scopedItem.IsActive)))
            .ToList();
    }

    private async Task<List<RoleMemberDto>> LoadUserRolesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = new List<RoleMemberDto>();
        var legacy = await (
            from userRole in dbContext.UserRoles.AsNoTracking()
            join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            join user in dbContext.Users.AsNoTracking() on userRole.UserId equals user.Id
            where userRole.UserId == userId
            select new RoleMemberDto(
                user.Id.ToString("D"),
                user.UserName,
                user.DisplayName,
                "organization",
                user.OrganizationId.ToString("D"),
                "整个组织（旧版全局角色）",
                role.IsActive,
                DateTimeOffset.MinValue)).ToListAsync(cancellationToken);
        result.AddRange(legacy);
        var roleIds = await dbContext.UserRoleAssignments.AsNoTracking()
            .Where(item => item.UserId == userId)
            .Select(item => item.RoleId)
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var roleId in roleIds)
        {
            result.AddRange(await LoadRoleMembersAsync(roleId, null, cancellationToken));
        }

        return result.Where(item => item.UserId == userId.ToString("D")).ToList();
    }

    private async Task<List<UserDataScopeDto>> LoadDataScopesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.DataScopes.AsNoTracking()
            .Where(item => item.UserId == userId)
            .ToListAsync(cancellationToken);
        var departmentIds = rows
            .Where(item => item.ScopeType == "department" && Guid.TryParse(item.ScopeReference, out _))
            .Select(item => Guid.Parse(item.ScopeReference))
            .ToArray();
        var departments = await dbContext.Departments.AsNoTracking()
            .Where(item => departmentIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        return rows.Select(item => new UserDataScopeDto(
            item.ScopeType,
            item.ScopeReference,
            item.ScopeType switch
            {
                "organization" => "整个组织",
                "department" => departments.GetValueOrDefault(Guid.Parse(item.ScopeReference)) ?? "部门",
                "self" => "本人",
                "project" => "参与项目",
                _ => item.ScopeReference
            })).ToList();
    }

    private async Task<IReadOnlyList<RoleDto>> MapRolesAsync(
        IReadOnlyList<RoleEntity> roles,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var roleIds = roles.Select(item => item.Id).ToArray();
        var scopedMembers = await (
            from assignment in dbContext.UserRoleAssignments.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on assignment.UserId equals user.Id
            where roleIds.Contains(assignment.RoleId)
                && assignment.IsActive
                && user.IsActive
                && user.OrganizationId == organizationId
            select new { assignment.RoleId, assignment.UserId })
            .ToListAsync(cancellationToken);
        var legacyMembers = await (
            from userRole in dbContext.UserRoles.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on userRole.UserId equals user.Id
            where roleIds.Contains(userRole.RoleId)
                && user.IsActive
                && user.OrganizationId == organizationId
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
        var rolePermissions = await dbContext.RolePermissions.AsNoTracking()
            .Where(item => roleIds.Contains(item.RoleId))
            .GroupBy(item => item.RoleId)
            .Select(group => new
            {
                RoleId = group.Key,
                PermissionIds = group.Select(item => item.PermissionId).ToList()
            })
            .ToDictionaryAsync(item => item.RoleId, item => item.PermissionIds, cancellationToken);
        return roles.Select(role => new RoleDto(
            role.Id.ToString("D"),
            role.Code,
            role.Name,
            role.RoleType,
            role.Description,
            role.IsSystem,
            role.IsActive,
            role.Version,
            memberCounts.GetValueOrDefault(role.Id),
            permissionCounts.GetValueOrDefault(role.Id),
            role.CreatedAt,
            role.UpdatedAt,
            rolePermissions.GetValueOrDefault(role.Id, [])
                .Select(id => id.ToString("D"))
                .ToList())).ToList();
    }

    private async Task<HashSet<Guid>> AccessibleDepartmentIdsAsync(
        UserPrincipal user,
        CancellationToken cancellationToken)
    {
        if (user.IsPlatformAdmin)
        {
            return (await dbContext.Departments.AsNoTracking()
                .Where(item => item.OrganizationId == user.OrganizationId)
                .Select(item => item.Id)
                .ToListAsync(cancellationToken)).ToHashSet();
        }

        return (await dbContext.UserDepartmentMemberships.AsNoTracking()
            .Where(item => item.UserId == user.UserId && item.IsActive)
            .Select(item => item.DepartmentId)
            .ToListAsync(cancellationToken)).ToHashSet();
    }

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

    private async Task<int> CountEffectiveRoleMembersAsync(
        Guid roleId,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var scopedUserIds = await (
            from assignment in dbContext.UserRoleAssignments.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on assignment.UserId equals user.Id
            where assignment.RoleId == roleId
                && assignment.IsActive
                && user.IsActive
                && user.OrganizationId == organizationId
            select assignment.UserId)
            .ToListAsync(cancellationToken);
        var legacyUserIds = await (
            from userRole in dbContext.UserRoles.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on userRole.UserId equals user.Id
            where userRole.RoleId == roleId
                && user.IsActive
                && user.OrganizationId == organizationId
            select userRole.UserId)
            .ToListAsync(cancellationToken);
        return scopedUserIds.Concat(legacyUserIds).Distinct().Count();
    }

    private static HashSet<Guid> VisibleDepartmentIds(
        UserPrincipal user,
        IReadOnlyList<DepartmentEntity> departments)
    {
        var visible = departments
            .Where(item => user.DepartmentIds.Contains(item.Id) || item.ManagerUserId == user.UserId)
            .Select(item => item.Id)
            .ToHashSet();
        var byId = departments.ToDictionary(item => item.Id);
        foreach (var id in visible.ToList())
        {
            var current = byId.GetValueOrDefault(id);
            while (current?.ParentDepartmentId is Guid parentId && byId.TryGetValue(parentId, out current))
            {
                visible.Add(parentId);
            }
        }

        return visible;
    }

    private async Task<DepartmentEntity> LoadDepartmentAsync(
        UserPrincipal user,
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Departments.SingleOrDefaultAsync(
            item => item.Id == departmentId && item.OrganizationId == user.OrganizationId,
            cancellationToken) ?? throw new NotFoundException("部门不存在。");
        user.EnsureDepartmentAccess(entity.Id);
        return entity;
    }

    private async Task<RoleEntity> LoadRoleAsync(Guid roleId, CancellationToken cancellationToken) =>
        await dbContext.Roles.SingleOrDefaultAsync(item => item.Id == roleId, cancellationToken)
        ?? throw new NotFoundException("角色不存在。");

    private async Task ValidateManagerAsync(
        Guid organizationId,
        Guid? managerUserId,
        CancellationToken cancellationToken)
    {
        if (!managerUserId.HasValue)
        {
            return;
        }

        var valid = await dbContext.Users.AsNoTracking().AnyAsync(
            item => item.Id == managerUserId.Value
                && item.OrganizationId == organizationId
                && item.IsActive
                && !item.IsServiceIdentity,
            cancellationToken);
        if (!valid)
        {
            throw new ValidationException("部门负责人不存在或已停用。");
        }
    }

    private async Task EnsureDepartmentAsync(
        Guid organizationId,
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        var valid = await dbContext.Departments.AsNoTracking().AnyAsync(
            item => item.Id == departmentId
                && item.OrganizationId == organizationId
                && item.IsActive,
            cancellationToken);
        if (!valid)
        {
            throw new ValidationException("部门不存在或已停用。");
        }
    }

    private async Task<Dictionary<Guid, string>> LoadUserNamesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToArray();
        return await dbContext.Users.AsNoTracking()
            .Where(item => ids.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.DisplayName, cancellationToken);
    }

    private static IEnumerable<DepartmentTreeNodeDto> Flatten(DepartmentTreeNodeDto node)
    {
        yield return node;
        foreach (var child in node.Children.SelectMany(Flatten))
        {
            yield return child;
        }
    }

    private static InviteUserNormalized NormalizeInvite(
        string userName,
        string displayName,
        Guid departmentId)
    {
        var normalizedUserName = DomainChecks.Required(userName, "登录名", 128);
        if (!UserNameRegex().IsMatch(normalizedUserName))
        {
            throw new ValidationException("登录名只能包含字母、数字、点、下划线或短横线。");
        }

        if (departmentId == Guid.Empty)
        {
            throw new ValidationException("人员所属部门不能为空。");
        }

        return new InviteUserNormalized(
            normalizedUserName,
            DomainChecks.Required(displayName, "姓名", 128),
            departmentId,
            true,
            false);
    }

    private static string NormalizeHierarchyType(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "Department" : value.Trim();
        return HierarchyTypes.Contains(normalized)
            ? normalized
            : throw new ValidationException("组织层级类型必须是 Center、Department 或 Team。");
    }

    private static string NormalizeRoleType(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "Business" : value.Trim();
        return RoleTypes.Contains(normalized)
            ? normalized
            : throw new ValidationException("角色类型必须是 System、Business 或 AgentService。");
    }

    private static string NormalizeCode(string? value, string fieldName, int maxLength) =>
        DomainChecks.RequiredCode(
            value?.Trim().ToUpperInvariant() ?? string.Empty,
            fieldName,
            maxLength);

    private static string? NormalizeOptional(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : DomainChecks.Required(value, "说明", maxLength);

    private static bool IsHighRisk(string code, string action) =>
        HighRiskPermissionCodes.Contains(code)
        || string.Equals(action, "Manage", StringComparison.OrdinalIgnoreCase)
        || string.Equals(action, "Assign", StringComparison.OrdinalIgnoreCase);

    private static void EnsureIdentityRead(UserPrincipal user)
    {
        if (!user.IsPlatformAdmin
            && !user.HasPermission("identity-admin", AccessAction.Read)
            && !user.HasPermission("role", AccessAction.Read))
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "当前身份没有 IAM 管理读取权限。");
        }
    }

    private static void EnsureRoleRead(UserPrincipal user)
    {
        if (!user.IsPlatformAdmin
            && !user.HasPermission("role", AccessAction.Read)
            && !user.HasPermission("identity-admin", AccessAction.Read))
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "当前身份没有角色读取权限。");
        }
    }

    private static void ValidateIdempotencyKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length is < 8 or > 128)
        {
            throw new ValidationException("幂等键必须为 8—128 位。");
        }
    }

    private static string Hash<T>(T value) =>
        Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    JsonSerializer.Serialize(value, JsonSerializerOptions.Web))));

    [GeneratedRegex("^[A-Za-z0-9._-]{2,128}$", RegexOptions.CultureInvariant)]
    private static partial Regex UserNameRegex();

    private sealed record InviteUserNormalized(
        string UserName,
        string DisplayName,
        Guid DepartmentId,
        bool IsPrimary,
        bool IsManager);

    private sealed record RolePermissionPayload(Guid[] PermissionIds);

    private sealed record RoleAssignmentPayload(SetRoleMemberItem[] Members);
}
