using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Audit;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;
using AIDT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Services;

public sealed class SqlNavigationService(
    AidtDbContext dbContext,
    IAuditRepository auditRepository,
    IIdempotencyStore idempotencyStore,
    IUnitOfWork unitOfWork,
    IClock clock,
    DataEnvironmentOptions environmentOptions) : INavigationService
{
    private static readonly HashSet<string> WorkspaceTypes =
        new(["OrganizationWorkspace", "DepartmentWorkspace", "BusinessView"], StringComparer.Ordinal);
    private static readonly HashSet<string> SystemItemKeys =
        new(["MY_WORK", "CONFIRMATIONS", "TODO", "EVENT_INBOX"], StringComparer.Ordinal);
    private static readonly IReadOnlyDictionary<string, int> DepartmentOrder =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["FINANCE"] = 10,
            ["SUPPLY"] = 20,
            ["PRODUCTION"] = 30,
            ["PROJECT"] = 40,
            ["RND"] = 50,
            ["QA"] = 60,
            ["DELIVERY"] = 70
        };
    private static readonly string[] AutomatedTestPrefixes =
        ["E2E", "TEST", "DEMO", "PILOT", "NAV-E2E", "AGENT-UI", "META_B", "UX003", "UX004", "WFROLE-E2E"];

    public async Task<CurrentNavigationDto> GetCurrentAsync(
        UserPrincipal user,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("navigation", AccessAction.Read);
        user.EnsureAuthenticated();
        var workspaces = await dbContext.NavigationWorkspaces.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId
                && item.Status == "Published"
                && item.EnvironmentCode == environmentOptions.CurrentEnvironmentCode
                && !item.IsTestData
                && (item.IsSystem || item.IsDefault))
            .OrderBy(item => item.IsSystem ? 0 : 1)
            .ThenBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        var selectedDepartmentWorkspaces = workspaces
            .Where(item => !item.IsSystem && item.DepartmentId.HasValue)
            .GroupBy(item => item.DepartmentId!.Value)
            .Select(group => group
                .OrderBy(item => item.IsTestData)
                .ThenByDescending(item => item.IsDefault)
                .ThenByDescending(item => item.UpdatedAt)
                .First())
            .ToArray();
        workspaces = workspaces
            .Where(item => item.IsSystem)
            .Concat(selectedDepartmentWorkspaces)
            .OrderBy(item => item.IsSystem ? 0 : 1)
            .ThenBy(item => item.CreatedAt)
            .ToList();
        var workspaceIds = workspaces.Select(item => item.Id).ToArray();
        var versions = await dbContext.NavigationVersions.AsNoTracking()
            .Where(item => workspaceIds.Contains(item.WorkspaceId)
                && item.Status == "Published")
            .OrderByDescending(item => item.VersionNumber)
            .ToListAsync(cancellationToken);
        var versionByWorkspace = versions
            .GroupBy(item => item.WorkspaceId)
            .ToDictionary(group => group.Key, group => group.First());
        var versionIds = versionByWorkspace.Values.Select(item => item.Id).ToArray();
        var items = await dbContext.NavigationItems.AsNoTracking()
            .Where(item => versionIds.Contains(item.NavigationVersionId))
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
        var preferences = await dbContext.NavigationUserPreferences.AsNoTracking()
            .Where(item => item.UserId == user.UserId)
            .ToListAsync(cancellationToken);
        var preferenceMap = preferences.ToDictionary(
            item => $"{item.WorkspaceKey}|{item.ItemKey}",
            StringComparer.Ordinal);
        var departments = await dbContext.Departments.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId && item.IsActive)
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var primaryDepartmentId = await dbContext.UserDepartmentMemberships.AsNoTracking()
            .Where(item => item.UserId == user.UserId && item.IsActive)
            .OrderByDescending(item => item.IsPrimary)
            .Select(item => (Guid?)item.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken);
        var mappedItems = workspaces
            .Where(item => versionByWorkspace.ContainsKey(item.Id))
            .ToDictionary(
                item => item.Id,
                workspace => items
                    .Where(item => item.NavigationVersionId == versionByWorkspace[workspace.Id].Id
                        && item.IsVisible
                        && item.IsEnabled
                        && NavigationRouteCatalog.TryValidate(
                            item.Route,
                            item.RequiredResource,
                            item.RequiredAction,
                            out _)
                        && CanUseItem(user, item))
                    .Select(item =>
                    {
                        var preference = preferenceMap.GetValueOrDefault(
                            $"{workspace.WorkspaceKey}|{item.ItemKey}");
                        return new NavigationItemDto(
                            item.Id.ToString("D"),
                            item.ItemKey,
                            item.Name,
                            item.ItemType,
                            item.Route,
                            item.RequiredResource,
                            item.RequiredAction,
                            preference?.SortOrder ?? item.SortOrder,
                            item.IsVisible,
                            item.IsEnabled,
                            item.IsSystem,
                            item.Icon,
                            preference?.IsFavorite ?? false,
                            preference?.IsHidden ?? false);
                    })
                    .Where(item => !item.IsHidden)
                    .OrderByDescending(item => item.IsFavorite)
                    .ThenBy(item => item.SortOrder)
                    .ThenBy(item => item.Name)
                    .ToList());
        var groups = new List<NavigationGroupDto>();
        var systemWorkspace = workspaces.FirstOrDefault(item =>
            item.IsSystem && !item.DepartmentId.HasValue);
        if (systemWorkspace is not null)
        {
            var systemItems = mappedItems.GetValueOrDefault(systemWorkspace.Id) ?? [];
            var commonItems = systemItems
                .Where(item => SystemItemKeys.Contains(item.ItemKey))
                .GroupBy(item => item.ItemKey, StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderBy(item => item.SortOrder)
                .ToList();
            if (commonItems.Count > 0)
            {
                groups.Add(new NavigationGroupDto(
                    systemWorkspace.Id.ToString("D"),
                    "COMPANY",
                    "公司公共入口",
                    systemWorkspace.WorkspaceType,
                    "Company",
                    null,
                    null,
                    "公司公共入口",
                    true,
                    false,
                    false,
                    0,
                    commonItems));
            }

            var platformItems = systemItems
                .Where(item => !SystemItemKeys.Contains(item.ItemKey))
                .OrderBy(item => item.SortOrder)
                .ToList();
            if (platformItems.Count > 0)
            {
                groups.Add(new NavigationGroupDto(
                    systemWorkspace.Id.ToString("D"),
                    "PLATFORM",
                    "平台治理",
                    systemWorkspace.WorkspaceType,
                    "Platform",
                    null,
                    null,
                    "平台治理",
                    false,
                    false,
                    false,
                    900,
                    platformItems));
            }
        }

        foreach (var workspace in workspaces)
        {
            if (workspace.IsTestData && !user.IsPlatformAdmin)
            {
                continue;
            }
            if (workspace.IsSystem)
            {
                continue;
            }

            if (workspace.DepartmentId.HasValue
                && !user.IsPlatformAdmin
                && !user.DepartmentIds.Contains(workspace.DepartmentId.Value))
            {
                continue;
            }

            if (!workspace.DepartmentId.HasValue && !user.IsPlatformAdmin)
            {
                continue;
            }

            if (!versionByWorkspace.ContainsKey(workspace.Id))
            {
                continue;
            }

            var visibleItems = (mappedItems.GetValueOrDefault(workspace.Id) ?? [])
                .Where(item => !SystemItemKeys.Contains(item.ItemKey))
                .ToList();
            if (visibleItems.Count == 0)
            {
                continue;
            }

            departments.TryGetValue(workspace.DepartmentId ?? Guid.Empty, out var department);
            groups.Add(new NavigationGroupDto(
                workspace.Id.ToString("D"),
                workspace.WorkspaceKey,
                workspace.Name,
                workspace.WorkspaceType,
                "Department",
                workspace.DepartmentId?.ToString("D"),
                department?.Code,
                department?.Name,
                workspace.IsDefault,
                workspace.IsDemoData,
                workspace.IsTestData,
                department is null
                    ? 100
                    : DepartmentOrder.GetValueOrDefault(department.Code, 100),
                visibleItems));
        }

        return new CurrentNavigationDto(
            clock.UtcNow,
            primaryDepartmentId?.ToString("D"),
            groups
                .OrderBy(item => item.Layer switch
                {
                    "Company" => 0,
                    "Department" => 1,
                    _ => 2
                })
                .ThenBy(item => item.SortOrder)
                .ThenBy(item => item.WorkspaceName)
                .ToList());
    }

    public async Task<IReadOnlyList<NavigationWorkspaceDto>> ListWorkspacesAsync(
        UserPrincipal user,
        CancellationToken cancellationToken)
    {
        EnsureNavigationRead(user);
        var query = dbContext.NavigationWorkspaces.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId);
        if (!user.IsPlatformAdmin)
        {
            var departmentIds = user.DepartmentIds.ToArray();
            query = query.Where(item =>
                item.IsSystem
                || !item.IsTestData
                    && item.DepartmentId != null
                    && departmentIds.Contains(item.DepartmentId.Value));
        }

        var workspaces = await query
            .OrderBy(item => item.IsSystem ? 0 : 1)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
        var result = new List<NavigationWorkspaceDto>();
        foreach (var workspace in workspaces)
        {
            var detail = await MapWorkspaceAsync(workspace, cancellationToken);
            if (detail is not null)
            {
                result.Add(detail);
            }
        }

        return result;
    }

    public async Task<NavigationWorkspaceDto?> GetWorkspaceAsync(
        UserPrincipal user,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        EnsureNavigationRead(user);
        var workspace = await dbContext.NavigationWorkspaces.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == workspaceId && item.OrganizationId == user.OrganizationId,
            cancellationToken);
        if (workspace is null)
        {
            return null;
        }

        EnsureWorkspaceViewAccess(user, workspace);
        return await MapWorkspaceAsync(workspace, cancellationToken);
    }

    public async Task<NavigationWorkspaceDto> CreateWorkspaceAsync(
        UserPrincipal user,
        CreateNavigationWorkspaceRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("navigation", AccessAction.Manage);
        ValidateIdempotencyKey(idempotencyKey);
        ArgumentNullException.ThrowIfNull(request);
        var key = NormalizeCode(request.WorkspaceKey, "工作区编码", 80);
        var name = DomainChecks.Required(request.Name, "工作区名称", 128);
        var type = NormalizeWorkspaceType(request.WorkspaceType);
        var department = await ValidateWorkspaceDepartmentAsync(user, request.DepartmentId, cancellationToken);
        if (department is not null)
        {
            await EnsureCanManageDepartmentNavigationAsync(user, department.Id, cancellationToken);
        }
        else if (!user.IsPlatformAdmin)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "只有平台管理员可以创建组织级导航。");
        }

        var hash = Hash(new { request, key });
        var scope = $"navigation:create:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.RequestHash != hash)
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同导航请求。");
            }

            return await GetWorkspaceAsync(
                user,
                Guid.Parse(existing.ResponseId),
                cancellationToken) ?? throw new NotFoundException("幂等结果对应的工作区不存在。");
        }

        if (await dbContext.NavigationWorkspaces.AsNoTracking().AnyAsync(
                item => item.OrganizationId == user.OrganizationId
                    && item.WorkspaceKey == key,
                cancellationToken))
        {
            throw new ConflictException(ErrorCodes.Conflict, "导航工作区编码已存在。");
        }

        var now = clock.UtcNow;
        var workspaceEntity = new NavigationWorkspaceEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = user.OrganizationId,
            DepartmentId = department?.Id,
            WorkspaceKey = key,
            Name = name,
            WorkspaceType = type,
            IsSystem = false,
            IsDefault = department is not null && !IsAutomatedTestCode(key),
            IsDemoData = department is not null
                && department.Code.Contains("DEMO", StringComparison.OrdinalIgnoreCase),
            EnvironmentCode = environmentOptions.CurrentEnvironmentCode,
            IsTestData = IsAutomatedTestCode(key),
            Status = "Draft",
            Version = 1,
            CreatedBy = user.UserId,
            CreatedAt = now,
            UpdatedBy = user.UserId,
            UpdatedAt = now
        };
        var versionEntity = new NavigationVersionEntity
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceEntity.Id,
            VersionNumber = 1,
            Status = "Draft",
            Revision = 1,
            CreatedBy = user.UserId,
            CreatedAt = now,
            UpdatedBy = user.UserId,
            UpdatedAt = now
        };
        var defaultItems = BuildDefaultItems(
            workspaceEntity,
            request.Template,
            request.OptionalEntries ?? [],
            now);
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.NavigationWorkspaces.Add(workspaceEntity);
            dbContext.NavigationVersions.Add(versionEntity);
            dbContext.NavigationItems.AddRange(defaultItems.Select(item =>
            {
                item.NavigationVersionId = versionEntity.Id;
                return item;
            }));
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Navigation",
                    "navigation.workspace.created",
                    "NavigationWorkspace",
                    workspaceEntity.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: JsonSerializer.Serialize(
                        new
                        {
                            workspaceEntity.WorkspaceKey,
                            workspaceEntity.Name,
                            workspaceEntity.WorkspaceType,
                            DefaultItems = defaultItems.Select(item => item.ItemKey)
                        },
                        JsonSerializerOptions.Web)),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "NavigationWorkspace",
                    workspaceEntity.Id.ToString("D"),
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

        return await GetWorkspaceAsync(user, workspaceEntity.Id, cancellationToken)
            ?? throw new NotFoundException("导航工作区创建失败。");
    }

    public async Task<NavigationWorkspaceDto> UpdateDraftAsync(
        UserPrincipal user,
        Guid workspaceId,
        UpdateNavigationWorkspaceRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("navigation", AccessAction.Manage);
        ArgumentNullException.ThrowIfNull(request);
        var workspace = await dbContext.NavigationWorkspaces.SingleOrDefaultAsync(
            item => item.Id == workspaceId && item.OrganizationId == user.OrganizationId,
            cancellationToken) ?? throw new NotFoundException("导航工作区不存在。");
        await EnsureCanManageWorkspaceAsync(user, workspace, cancellationToken);
        if (workspace.Status == "Retired")
        {
            throw new ConflictException(ErrorCodes.Conflict, "已停用工作区需先恢复后再修改。");
        }

        var version = await dbContext.NavigationVersions
            .Where(item => item.WorkspaceId == workspaceId && item.Status == "Draft")
            .OrderByDescending(item => item.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);
        if (version is not null && version.Revision != request.ExpectedVersion
            || version is null && request.ExpectedVersion != workspace.Version)
        {
            throw new ConflictException(ErrorCodes.Conflict, "导航版本已变化，请刷新后重试。");
        }

        var now = clock.UtcNow;
        if (version is null)
        {
            var latestVersion = await dbContext.NavigationVersions.AsNoTracking()
                .Where(item => item.WorkspaceId == workspaceId)
                .MaxAsync(item => (int?)item.VersionNumber, cancellationToken) ?? 0;
            version = new NavigationVersionEntity
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                VersionNumber = latestVersion + 1,
                Status = "Draft",
                Revision = 1,
                CreatedBy = user.UserId,
                CreatedAt = now,
                UpdatedBy = user.UserId,
                UpdatedAt = now
            };
            dbContext.NavigationVersions.Add(version);
        }

        var existingItems = await dbContext.NavigationItems
            .Where(item => item.NavigationVersionId == version.Id)
            .ToListAsync(cancellationToken);
        var normalizedItems = NormalizeItems(workspace, request.Items, now);
        var missingSystem = workspace.IsSystem
            ? SystemItemKeys.Except(
                normalizedItems.Where(item => item.IsSystem).Select(item => item.ItemKey),
                StringComparer.Ordinal)
            : [];
        if (missingSystem.Any())
        {
            throw new ValidationException("系统保留入口不能被删除：" + string.Join("、", missingSystem));
        }

        if (existingItems.Count > 0)
        {
            dbContext.NavigationItems.RemoveRange(existingItems);
        }

        foreach (var item in normalizedItems)
        {
            item.NavigationVersionId = version.Id;
            dbContext.NavigationItems.Add(item);
        }

        workspace.Name = DomainChecks.Required(request.Name, "工作区名称", 128);
        workspace.Version++;
        workspace.UpdatedBy = user.UserId;
        workspace.UpdatedAt = now;
        version.Revision++;
        version.UpdatedBy = user.UserId;
        version.UpdatedAt = now;
        version.ImpactAnalysisJson = JsonSerializer.Serialize(
            new
            {
                ItemCount = normalizedItems.Count,
                SystemItemCount = normalizedItems.Count(item => item.IsSystem),
                HiddenItemCount = normalizedItems.Count(item => !item.IsVisible || !item.IsEnabled),
                ChangedAt = now
            },
            JsonSerializerOptions.Web);
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Navigation",
                "navigation.workspace.draft_updated",
                "NavigationVersion",
                version.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                before: $"revision={version.Revision - 1}",
                after: version.ImpactAnalysisJson),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetWorkspaceAsync(user, workspaceId, cancellationToken)
            ?? throw new NotFoundException("导航工作区不存在。");
    }

    public async Task<NavigationWorkspaceDto> PublishAsync(
        UserPrincipal user,
        Guid workspaceId,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("navigation", AccessAction.Approve);
        ValidateIdempotencyKey(idempotencyKey);
        var workspace = await dbContext.NavigationWorkspaces.SingleOrDefaultAsync(
            item => item.Id == workspaceId && item.OrganizationId == user.OrganizationId,
            cancellationToken) ?? throw new NotFoundException("导航工作区不存在。");
        await EnsureCanManageWorkspaceAsync(user, workspace, cancellationToken);
        var hash = Hash(new { workspaceId, action = "publish" });
        var scope = $"navigation:publish:{workspaceId:D}:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.RequestHash != hash)
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同发布请求。");
            }

            return await GetWorkspaceAsync(user, workspaceId, cancellationToken)
                ?? throw new NotFoundException("导航工作区不存在。");
        }

        var draft = await dbContext.NavigationVersions
            .Where(item => item.WorkspaceId == workspaceId && item.Status == "Draft")
            .OrderByDescending(item => item.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ConflictException(ErrorCodes.Conflict, "导航工作区没有待发布草稿。");
        var items = await dbContext.NavigationItems.AsNoTracking()
            .Where(item => item.NavigationVersionId == draft.Id)
            .ToListAsync(cancellationToken);
        if (items.Count == 0)
        {
            throw new ValidationException("导航工作区没有入口，不能发布。");
        }

        var missingSystem = workspace.IsSystem
            ? SystemItemKeys.Except(
                items.Where(item => item.IsSystem).Select(item => item.ItemKey),
                StringComparer.Ordinal)
            : [];
        if (missingSystem.Any())
        {
            throw new ValidationException("系统保留入口缺失，不能发布。");
        }

        var now = clock.UtcNow;
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var publishedVersions = await dbContext.NavigationVersions
                .Where(item => item.WorkspaceId == workspaceId && item.Status == "Published")
                .ToListAsync(cancellationToken);
            publishedVersions.ForEach(item => item.Status = "Retired");
            draft.Status = "Published";
            draft.PublishedBy = user.UserId;
            draft.PublishedAt = now;
            draft.UpdatedBy = user.UserId;
            draft.UpdatedAt = now;
            if (workspace.DepartmentId.HasValue && workspace.IsDefault)
            {
                var previousDefaults = await dbContext.NavigationWorkspaces
                    .Where(item =>
                        item.OrganizationId == user.OrganizationId
                        && item.DepartmentId == workspace.DepartmentId
                        && item.Id != workspace.Id
                        && item.IsDefault
                        && (workspace.IsTestData || !item.IsTestData))
                    .ToListAsync(cancellationToken);
                previousDefaults.ForEach(item =>
                {
                    item.IsDefault = false;
                    item.Version++;
                    item.UpdatedBy = user.UserId;
                    item.UpdatedAt = now;
                });
                if (previousDefaults.Count > 0)
                {
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }
            workspace.Status = "Published";
            workspace.UpdatedBy = user.UserId;
            workspace.UpdatedAt = now;
            workspace.Version++;
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Navigation",
                    "navigation.workspace.published",
                    "NavigationWorkspace",
                    workspace.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: JsonSerializer.Serialize(
                        new { draft.Id, draft.VersionNumber, ItemCount = items.Count },
                        JsonSerializerOptions.Web)),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "NavigationWorkspace",
                    workspace.Id.ToString("D"),
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

        return await GetWorkspaceAsync(user, workspaceId, cancellationToken)
            ?? throw new NotFoundException("导航工作区不存在。");
    }

    public async Task<NavigationWorkspaceDto> SetStatusAsync(
        UserPrincipal user,
        Guid workspaceId,
        bool retire,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("navigation", AccessAction.Manage);
        var workspace = await dbContext.NavigationWorkspaces.SingleOrDefaultAsync(
            item => item.Id == workspaceId && item.OrganizationId == user.OrganizationId,
            cancellationToken) ?? throw new NotFoundException("导航工作区不存在。");
        await EnsureCanManageWorkspaceAsync(user, workspace, cancellationToken);
        if (workspace.IsSystem && retire)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "系统工作区不能停用。");
        }

        if (workspace.DepartmentId.HasValue && !retire && workspace.IsDefault)
        {
            var otherDefaults = await dbContext.NavigationWorkspaces
                .Where(item =>
                    item.OrganizationId == user.OrganizationId
                    && item.DepartmentId == workspace.DepartmentId
                    && item.Id != workspace.Id
                    && item.IsDefault
                    && (workspace.IsTestData || !item.IsTestData))
                .ToListAsync(cancellationToken);
            otherDefaults.ForEach(item => item.IsDefault = false);
            if (otherDefaults.Count > 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        workspace.Status = retire ? "Retired" : "Published";
        workspace.RetiredBy = retire ? user.UserId : null;
        workspace.RetiredAt = retire ? clock.UtcNow : null;
        workspace.UpdatedBy = user.UserId;
        workspace.UpdatedAt = clock.UtcNow;
        workspace.Version++;
        if (retire && workspace.IsDefault && workspace.DepartmentId.HasValue)
        {
            var fallback = await dbContext.NavigationWorkspaces
                .Where(item =>
                    item.OrganizationId == user.OrganizationId
                    && item.DepartmentId == workspace.DepartmentId
                    && item.Id != workspace.Id
                    && item.Status == "Published"
                    && !item.IsTestData)
                .OrderByDescending(item => item.UpdatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (fallback is not null)
            {
                fallback.IsDefault = true;
                fallback.Version++;
                fallback.UpdatedBy = user.UserId;
                fallback.UpdatedAt = clock.UtcNow;
            }
        }
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Navigation",
                retire ? "navigation.workspace.retired" : "navigation.workspace.restored",
                "NavigationWorkspace",
                workspace.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                workspace.UpdatedAt,
                reason: retire ? "停止展示导航工作区" : "恢复导航工作区"),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetWorkspaceAsync(user, workspaceId, cancellationToken)
            ?? throw new NotFoundException("导航工作区不存在。");
    }

    public async Task<IReadOnlyList<NavigationPreferenceDto>> GetPreferencesAsync(
        UserPrincipal user,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("navigation", AccessAction.Read);
        return await dbContext.NavigationUserPreferences.AsNoTracking()
            .Where(item => item.UserId == user.UserId)
            .OrderBy(item => item.WorkspaceKey)
            .ThenBy(item => item.SortOrder)
            .Select(item => new NavigationPreferenceDto(
                item.WorkspaceKey,
                item.ItemKey,
                item.IsFavorite,
                item.IsHidden,
                item.SortOrder,
                item.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NavigationPreferenceDto>> SetPreferencesAsync(
        UserPrincipal user,
        SetNavigationPreferencesRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("navigation", AccessAction.Read);
        ArgumentNullException.ThrowIfNull(request);
        if (request.Items.Count > 500)
        {
            throw new ValidationException("导航个人偏好不能超过 500 项。");
        }

        var workspaceKeys = request.Items.Select(item => item.WorkspaceKey.Trim())
            .Distinct(StringComparer.Ordinal).ToArray();
        var validWorkspaces = await dbContext.NavigationWorkspaces.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId
                && workspaceKeys.Contains(item.WorkspaceKey)
                && item.Status == "Published")
            .Select(item => item.WorkspaceKey)
            .ToListAsync(cancellationToken);
        if (validWorkspaces.Count != workspaceKeys.Length)
        {
            throw new ValidationException("个人偏好包含不可见导航工作区。");
        }

        var existing = await dbContext.NavigationUserPreferences
            .Where(item => item.UserId == user.UserId)
            .ToListAsync(cancellationToken);
        var now = clock.UtcNow;
        foreach (var item in request.Items)
        {
            var workspaceKey = item.WorkspaceKey.Trim();
            var itemKey = item.ItemKey.Trim();
            var entity = existing.FirstOrDefault(row =>
                row.WorkspaceKey == workspaceKey && row.ItemKey == itemKey);
            if (entity is null)
            {
                entity = new NavigationUserPreferenceEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = user.UserId,
                    WorkspaceKey = workspaceKey,
                    ItemKey = itemKey
                };
                dbContext.NavigationUserPreferences.Add(entity);
                existing.Add(entity);
            }

            entity.IsFavorite = item.IsFavorite;
            entity.IsHidden = item.IsHidden;
            entity.SortOrder = item.SortOrder;
            entity.UpdatedAt = now;
        }

        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Navigation",
                "navigation.preference.updated",
                "NavigationUserPreference",
                user.UserId.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                after: JsonSerializer.Serialize(request.Items, JsonSerializerOptions.Web)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetPreferencesAsync(user, cancellationToken);
    }

    private async Task<NavigationWorkspaceDto?> MapWorkspaceAsync(
        NavigationWorkspaceEntity workspace,
        CancellationToken cancellationToken)
    {
        var version = await dbContext.NavigationVersions.AsNoTracking()
            .Where(item => item.WorkspaceId == workspace.Id && item.Status == "Draft")
            .OrderByDescending(item => item.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken)
            ?? await dbContext.NavigationVersions.AsNoTracking()
                .Where(item => item.WorkspaceId == workspace.Id && item.Status == "Published")
                .OrderByDescending(item => item.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken);
        if (version is null)
        {
            return null;
        }

        var items = await dbContext.NavigationItems.AsNoTracking()
            .Where(item => item.NavigationVersionId == version.Id)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
        var departmentName = workspace.DepartmentId.HasValue
            ? await dbContext.Departments.AsNoTracking()
                .Where(item => item.Id == workspace.DepartmentId.Value)
                .Select(item => item.Name)
                .SingleOrDefaultAsync(cancellationToken)
            : null;
        return new NavigationWorkspaceDto(
            workspace.Id.ToString("D"),
            workspace.WorkspaceKey,
            workspace.Name,
            workspace.WorkspaceType,
            workspace.DepartmentId?.ToString("D"),
            departmentName,
            workspace.IsSystem,
            workspace.IsDefault,
            workspace.IsDemoData,
            workspace.EnvironmentCode,
            workspace.IsTestData,
            workspace.Status,
            workspace.Version,
            version.Id.ToString("D"),
            version.VersionNumber,
            version.Revision,
            version.Status,
            items.Select(item => new NavigationItemDto(
                item.Id.ToString("D"),
                item.ItemKey,
                item.Name,
                item.ItemType,
                item.Route,
                item.RequiredResource,
                item.RequiredAction,
                item.SortOrder,
                item.IsVisible,
                item.IsEnabled,
                item.IsSystem,
                item.Icon,
                false,
                false)).ToList(),
            workspace.CreatedAt,
            workspace.UpdatedAt,
            version.PublishedAt);
    }

    private async Task<DepartmentEntity?> ValidateWorkspaceDepartmentAsync(
        UserPrincipal user,
        Guid? departmentId,
        CancellationToken cancellationToken)
    {
        if (!departmentId.HasValue)
        {
            return null;
        }

        user.EnsureDepartmentAccess(departmentId.Value);
        return await dbContext.Departments.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == departmentId.Value
                && item.OrganizationId == user.OrganizationId
                && item.IsActive,
            cancellationToken) ?? throw new ValidationException("导航关联部门不存在或已停用。");
    }

    private static async Task EnsureCanManageWorkspaceAsync(
        UserPrincipal user,
        NavigationWorkspaceEntity workspace,
        CancellationToken cancellationToken)
    {
        if (user.IsPlatformAdmin)
        {
            return;
        }

        if (workspace.IsSystem || !workspace.DepartmentId.HasValue)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "只有平台管理员可以维护组织级导航。");
        }

        await EnsureCanManageDepartmentNavigationAsync(
            user,
            workspace.DepartmentId.Value,
            cancellationToken);
    }

    private static Task EnsureCanManageDepartmentNavigationAsync(
        UserPrincipal user,
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        if (user.IsPlatformAdmin)
        {
            return Task.CompletedTask;
        }

        user.EnsureDepartmentAccess(departmentId);
        _ = cancellationToken;
        return Task.CompletedTask;
    }

    private static void EnsureWorkspaceViewAccess(
        UserPrincipal user,
        NavigationWorkspaceEntity workspace)
    {
        if (user.IsPlatformAdmin || workspace.IsSystem && !workspace.DepartmentId.HasValue)
        {
            return;
        }

        if (!workspace.DepartmentId.HasValue || !user.DepartmentIds.Contains(workspace.DepartmentId.Value))
        {
            throw new PermissionException(ErrorCodes.DepartmentScopeDenied, "当前身份没有该导航工作区范围。");
        }
    }

    private static List<NavigationItemEntity> NormalizeItems(
        NavigationWorkspaceEntity workspace,
        IReadOnlyList<NavigationItemWriteRequest> items,
        DateTimeOffset now)
    {
        if (items.Count > 200)
        {
            throw new ValidationException("单个导航工作区最多 200 个入口。");
        }

        var duplicateKeys = items.GroupBy(item => item.ItemKey.Trim(), StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicateKeys.Length > 0)
        {
            throw new ValidationException("导航入口编码重复：" + string.Join("、", duplicateKeys));
        }

        return items.Select(item =>
        {
            var itemKey = NormalizeCode(item.ItemKey, "导航入口编码", 80);
            var name = DomainChecks.Required(item.Name, "导航入口名称", 128);
            var route = DomainChecks.Required(item.Route, "导航路由", 500);
            var resource = NormalizeOptional(item.RequiredResource, 128);
            var action = NormalizeOptional(item.RequiredAction, 64);
            if (!NavigationRouteCatalog.TryValidate(route, resource, action, out _))
            {
                throw new ValidationException(
                    $"导航入口 {itemKey} 引用了未注册路由、权限资源或动作。");
            }

            if (workspace.DepartmentId.HasValue
                && route.Contains('?', StringComparison.Ordinal)
                && !route.Contains(
                    $"departmentId={workspace.DepartmentId.Value:D}",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException(
                    $"导航入口 {itemKey} 不能引用其他部门的数据范围。");
            }

            var isSystem = item.IsSystem || SystemItemKeys.Contains(itemKey);
            if (isSystem && (!item.IsVisible || !item.IsEnabled))
            {
                throw new ValidationException($"系统入口 {itemKey} 不能隐藏或停用。");
            }

            return new NavigationItemEntity
            {
                Id = Guid.NewGuid(),
                ItemKey = itemKey,
                Name = name,
                ItemType = DomainChecks.Required(item.ItemType, "导航入口类型", 32),
                Route = route,
                RequiredResource = resource,
                RequiredAction = action,
                SortOrder = item.SortOrder,
                IsVisible = item.IsVisible,
                IsEnabled = item.IsEnabled,
                IsSystem = isSystem,
                Icon = NormalizeOptional(item.Icon, 32),
                CreatedAt = now,
                UpdatedAt = now
            };
        }).ToList();
    }

    private static List<NavigationItemEntity> BuildDefaultItems(
        NavigationWorkspaceEntity workspace,
        string template,
        IReadOnlyList<string> optionalEntries,
        DateTimeOffset now)
    {
        var items = new List<NavigationItemEntity>();
        void Add(
            string key,
            string name,
            string type,
            string route,
            string? resource,
            string? action,
            int sort,
            string? icon)
        {
            items.Add(new NavigationItemEntity
            {
                Id = Guid.NewGuid(),
                ItemKey = key,
                Name = name,
                ItemType = type,
                Route = route,
                RequiredResource = resource,
                RequiredAction = resource is null ? null : action,
                SortOrder = sort,
                IsVisible = true,
                IsEnabled = true,
                IsSystem = SystemItemKeys.Contains(key),
                Icon = icon,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        if (string.Equals(template, "DepartmentWorkspace", StringComparison.OrdinalIgnoreCase)
            || string.Equals(workspace.WorkspaceType, "DepartmentWorkspace", StringComparison.Ordinal))
        {
            var departmentQuery = workspace.DepartmentId.HasValue
                ? $"?departmentId={workspace.DepartmentId.Value:D}"
                : string.Empty;
            Add("DEPARTMENT_HOME", "部门概览", "Workspace", $"/department{departmentQuery}", "department", "Read", 10, "部");
            Add("WORKFLOW_ITEMS", "流程事项", "Workflow", $"/workflows{departmentQuery}", "workflow-task", "Read", 20, "流");
            Add("CONFIGURATIONS", "流程配置", "Administration", $"/configurations{departmentQuery}", "configuration", "Read", 30, "配");
            Add("DEPARTMENT_FILES", "部门文件", "File", $"/documents{departmentQuery}", "file", "Read", 40, "文");
            Add("MINUTES", "AI 听记", "Minutes", $"/minutes{departmentQuery}", "minutes", "Read", 50, "记");
            Add("BUSINESS_VIEW", "业务视图", "BusinessObject", $"/objects{departmentQuery}", "business-object", "Read", 60, "业");
            Add("SCENARIOS", "岗位业务场景", "BusinessObject", $"/scenarios{departmentQuery}", "scenario", "Read", 70, "场");
            Add("AGENTS", "数字员工身份", "Administration", $"/agents{departmentQuery}", "agent", "Read", 80, "员");
            return items;
        }

        if (string.Equals(template, "BusinessView", StringComparison.OrdinalIgnoreCase))
        {
            Add("CUSTOM_BUSINESS_VIEW", workspace.Name, "BusinessObject", "/objects", "business-object", "Read", 40, "视");
        }
        else
        {
            Add("MY_WORK", "我的工作", "System", "/", "dashboard", "Read", 10, "我");
            Add("CONFIRMATIONS", "统一确认", "Inbox", "/confirmations", null, null, 15, "确");
            Add("TODO", "待办", "Task", "/workflows", "workflow-task", "Read", 20, "办");
            Add("EVENT_INBOX", "事件收件箱", "Inbox", "/inbox", "event-inbox", "Read", 30, "收");
            Add("MODEL_STUDIO", "业务建模工作室", "Administration", "/modeling", "object-model", "Read", 100, "模");
            Add("IAM_NAVIGATION", "IAM 与导航", "Administration", "/iam", "identity-admin", "Read", 110, "权");
            Add("AUDIT", "审计证据", "Administration", "/audit", "audit", "Read", 120, "审");
            Add("MCP_CENTER", "MCP 接入治理", "Administration", "/mcp-center", "mcp", "Read", 130, "接");
        }

        _ = optionalEntries;

        return items;
    }

    private static bool CanUseItem(UserPrincipal user, NavigationItemEntity item)
    {
        if (string.IsNullOrWhiteSpace(item.RequiredResource))
        {
            return true;
        }

        if (!Enum.TryParse<AccessAction>(item.RequiredAction, true, out var action))
        {
            return false;
        }

        return user.IsPlatformAdmin || user.HasPermission(item.RequiredResource, action);
    }

    private static string NormalizeCode(string? value, string fieldName, int maxLength) =>
        DomainChecks.RequiredCode(
            value?.Trim().ToUpperInvariant() ?? string.Empty,
            fieldName,
            maxLength);

    private static string NormalizeWorkspaceType(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "DepartmentWorkspace" : value.Trim();
        return WorkspaceTypes.Contains(normalized)
            ? normalized
            : throw new ValidationException("导航工作区类型无效。");
    }

    private static string? NormalizeOptional(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : DomainChecks.Required(value, "导航配置", maxLength);

    private static void EnsureNavigationRead(UserPrincipal user)
    {
        if (!user.IsPlatformAdmin
            && !user.HasPermission("navigation", AccessAction.Read)
            && !user.HasPermission("navigation", AccessAction.Manage))
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "当前身份没有导航配置读取权限。");
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

    private static bool IsAutomatedTestCode(string code) =>
        AutomatedTestPrefixes
            .Any(prefix => code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
}
