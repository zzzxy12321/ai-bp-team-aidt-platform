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

public sealed class SqlWorkbenchService(
    AidtDbContext dbContext,
    IEmployeeWorkspaceQueryService employeeWorkspaceQueryService,
    IProjectService projectService,
    IAuditRepository auditRepository,
    IIdempotencyStore idempotencyStore,
    IUnitOfWork unitOfWork,
    IClock clock,
    DataEnvironmentOptions environmentOptions) : IWorkbenchService
{
    private static readonly HashSet<string> CardTypes = new(StringComparer.Ordinal)
    {
        "work_items",
        "workflow_tasks",
        "project_portfolio",
        "project_tasks",
        "business_objects",
        "recent_files",
        "minutes",
        "overdue_risks",
        "team_pending",
        "ai_activity"
    };

    private static readonly HashSet<string> AssignmentTypes = new(StringComparer.Ordinal)
    {
        "User",
        "Role",
        "Department",
        "Organization"
    };

    private static readonly HashSet<string> FilterKeys = new(StringComparer.Ordinal)
    {
        "keyword",
        "objectCodes",
        "statuses",
        "riskLevels",
        "dueWithinDays",
        "top",
        "sourceTypes",
        "projectScope"
    };

    public async Task<DepartmentWorkbenchDto> ResolveAsync(
        UserPrincipal user,
        Guid departmentId,
        bool includeTestData,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("workbench-template", AccessAction.Read);
        user.EnsureOrganizationAccess(user.OrganizationId);
        user.EnsureDepartmentAccess(departmentId);
        var department = await dbContext.Departments.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == departmentId
                    && item.OrganizationId == user.OrganizationId
                    && item.IsActive,
                cancellationToken) ?? throw new NotFoundException("部门不存在或已停用。");

        var match = await MatchTemplateAsync(user, departmentId, cancellationToken);
        var roleNames = await LoadRoleNamesAsync(user, cancellationToken);
        if (match is null)
        {
            return new DepartmentWorkbenchDto(
                false,
                null,
                null,
                null,
                null,
                null,
                department.Id.ToString("D"),
                department.Name,
                "通用部门工作区",
                department.Name,
                "当前部门有哪些待处理事项和最近证据？",
                roleNames,
                "GenericDepartmentWorkspace",
                "没有匹配的已发布岗位模板。",
                clock.UtcNow,
                []);
        }

        var (template, version, assignment) = match.Value;
        var cards = await dbContext.WorkbenchCards.AsNoTracking()
            .Where(item => item.TemplateVersionId == version.Id && item.IsEnabled)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Title)
            .ToListAsync(cancellationToken);
        var resolved = new List<WorkbenchResolvedCardDto>();
        foreach (var card in cards)
        {
            if (!CanUseCard(user, card))
            {
                continue;
            }

            resolved.Add(await ResolveCardAsync(
                user,
                department,
                card,
                includeTestData,
                cancellationToken));
        }

        var updatedAt = new[]
        {
            template.UpdatedAt,
            version.PublishedAt ?? version.UpdatedAt
        }.Max();
        return new DepartmentWorkbenchDto(
            true,
            template.Id.ToString("D"),
            template.Code,
            template.Name,
            version.Id.ToString("D"),
            version.VersionNumber,
            department.Id.ToString("D"),
            department.Name,
            template.AudienceTitle,
            template.Name,
            template.PrimaryQuestion,
            roleNames,
            "RoleWorkbenchTemplate",
            assignment.AssignmentType,
            updatedAt,
            resolved);
    }

    public async Task<IReadOnlyList<WorkbenchTemplateDto>> ListTemplatesAsync(
        UserPrincipal user,
        CancellationToken cancellationToken)
    {
        EnsureRead(user);
        user.EnsureOrganizationAccess(user.OrganizationId);
        var query = dbContext.WorkbenchTemplates.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId);
        if (!user.IsPlatformAdmin)
        {
            if (!user.HasPermission("workbench-template", AccessAction.Manage))
            {
                return [];
            }

            var departments = user.DepartmentIds.ToArray();
            query = query.Where(item =>
                item.DepartmentId.HasValue
                && departments.Contains(item.DepartmentId.Value));
        }

        var ids = await query
            .OrderBy(item => item.IsSystem ? 0 : 1)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        var result = new List<WorkbenchTemplateDto>();
        foreach (var id in ids)
        {
            result.Add(await GetTemplateAsync(user, id, cancellationToken));
        }

        return result;
    }

    public async Task<WorkbenchTemplateDto> GetTemplateAsync(
        UserPrincipal user,
        Guid templateId,
        CancellationToken cancellationToken)
    {
        EnsureRead(user);
        var template = await dbContext.WorkbenchTemplates.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == templateId
                    && item.OrganizationId == user.OrganizationId,
                cancellationToken) ?? throw new NotFoundException("工作台模板不存在。");
        EnsureCanViewTemplate(user, template);
        return await MapTemplateAsync(template, cancellationToken);
    }

    public async Task<WorkbenchTemplateDto> CreateTemplateAsync(
        UserPrincipal user,
        CreateWorkbenchTemplateRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("workbench-template", AccessAction.Manage);
        user.EnsureOrganizationAccess(user.OrganizationId);
        ArgumentNullException.ThrowIfNull(request);
        ValidateIdempotencyKey(idempotencyKey);
        var code = NormalizeCode(request.Code, "模板编码", 80);
        var department = await ValidateDepartmentAsync(
            user,
            request.DepartmentId,
            true,
            cancellationToken);
        var hash = Hash(new { request, code });
        var scope = $"workbench-template:create:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.RequestHash != hash)
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同工作台模板请求。");
            }

            return await GetTemplateAsync(user, Guid.Parse(existing.ResponseId), cancellationToken);
        }

        if (await dbContext.WorkbenchTemplates.AnyAsync(
                item => item.OrganizationId == user.OrganizationId && item.Code == code,
                cancellationToken))
        {
            throw new ConflictException(ErrorCodes.Conflict, "工作台模板编码已存在。");
        }

        var now = clock.UtcNow;
        var template = new WorkbenchTemplateEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = user.OrganizationId,
            DepartmentId = department?.Id,
            Code = code,
            Name = DomainChecks.Required(request.Name, "模板名称", 128),
            Description = NormalizeOptional(request.Description, 1000) ?? string.Empty,
            AudienceTitle = DomainChecks.Required(request.AudienceTitle, "岗位名称", 80),
            PrimaryQuestion = DomainChecks.Required(request.PrimaryQuestion, "首屏问题", 300),
            IsSystem = false,
            IsDefault = request.IsDefault,
            Status = "Draft",
            SortOrder = request.SortOrder,
            Version = 1,
            CreatedBy = user.UserId,
            CreatedAt = now,
            UpdatedBy = user.UserId,
            UpdatedAt = now
        };
        var version = new WorkbenchTemplateVersionEntity
        {
            Id = Guid.NewGuid(),
            TemplateId = template.Id,
            VersionNumber = 1,
            Status = "Draft",
            Revision = 1,
            CreatedBy = user.UserId,
            CreatedAt = now,
            UpdatedBy = user.UserId,
            UpdatedAt = now
        };
        var defaultAssignment = new WorkbenchAssignmentEntity
        {
            Id = Guid.NewGuid(),
            TemplateVersionId = version.Id,
            AssignmentType = template.DepartmentId.HasValue ? "Department" : "Organization",
            ScopeValue = template.DepartmentId?.ToString("D") ?? user.OrganizationId.ToString("D"),
            Priority = 0,
            CreatedAt = now
        };
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.WorkbenchTemplates.Add(template);
            dbContext.WorkbenchTemplateVersions.Add(version);
            dbContext.WorkbenchAssignments.Add(defaultAssignment);
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Workbench",
                    "workbench.template.created",
                    nameof(WorkbenchTemplateEntity),
                    template.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: JsonSerializer.Serialize(
                        new { template.Code, template.Name, template.DepartmentId },
                        JsonSerializerOptions.Web)),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    nameof(WorkbenchTemplateEntity),
                    template.Id.ToString("D"),
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

        return await GetTemplateAsync(user, template.Id, cancellationToken);
    }

    public async Task<WorkbenchTemplateDto> CopyTemplateAsync(
        UserPrincipal user,
        Guid templateId,
        CopyWorkbenchTemplateRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("workbench-template", AccessAction.Manage);
        ValidateIdempotencyKey(idempotencyKey);
        var source = await dbContext.WorkbenchTemplates.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == templateId
                    && item.OrganizationId == user.OrganizationId,
                cancellationToken) ?? throw new NotFoundException("源工作台模板不存在。");
        EnsureCanViewTemplate(user, source);
        var department = await ValidateDepartmentAsync(user, request.DepartmentId, true, cancellationToken);
        if (department is null && !user.IsPlatformAdmin)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "只有平台管理员可以复制为组织级模板。");
        }

        var code = NormalizeCode(request.Code, "模板编码", 80);
        var hash = Hash(new { templateId, request, code });
        var scope = $"workbench-template:copy:{templateId:D}:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.RequestHash != hash)
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同复制请求。");
            }

            return await GetTemplateAsync(user, Guid.Parse(existing.ResponseId), cancellationToken);
        }

        if (await dbContext.WorkbenchTemplates.AnyAsync(
                item => item.OrganizationId == user.OrganizationId && item.Code == code,
                cancellationToken))
        {
            throw new ConflictException(ErrorCodes.Conflict, "工作台模板编码已存在。");
        }

        var sourceVersion = await dbContext.WorkbenchTemplateVersions.AsNoTracking()
            .Where(item => item.TemplateId == templateId && item.Status == "Published")
            .OrderByDescending(item => item.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken)
            ?? await dbContext.WorkbenchTemplateVersions.AsNoTracking()
                .Where(item => item.TemplateId == templateId)
                .OrderByDescending(item => item.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ConflictException(ErrorCodes.Conflict, "源模板没有可复制版本。");
        var sourceCards = await dbContext.WorkbenchCards.AsNoTracking()
            .Where(item => item.TemplateVersionId == sourceVersion.Id)
            .OrderBy(item => item.SortOrder)
            .ToListAsync(cancellationToken);
        var sourceAssignments = await dbContext.WorkbenchAssignments.AsNoTracking()
            .Where(item => item.TemplateVersionId == sourceVersion.Id)
            .ToListAsync(cancellationToken);
        var now = clock.UtcNow;
        var template = new WorkbenchTemplateEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = user.OrganizationId,
            DepartmentId = department?.Id,
            Code = code,
            Name = DomainChecks.Required(request.Name, "模板名称", 128),
            Description = source.Description,
            AudienceTitle = NormalizeOptional(request.AudienceTitle, 80) ?? source.AudienceTitle,
            PrimaryQuestion = source.PrimaryQuestion,
            IsSystem = false,
            IsDefault = false,
            Status = "Draft",
            SortOrder = source.SortOrder,
            Version = 1,
            CreatedBy = user.UserId,
            CreatedAt = now,
            UpdatedBy = user.UserId,
            UpdatedAt = now
        };
        var version = new WorkbenchTemplateVersionEntity
        {
            Id = Guid.NewGuid(),
            TemplateId = template.Id,
            VersionNumber = 1,
            Status = "Draft",
            Revision = 1,
            CreatedBy = user.UserId,
            CreatedAt = now,
            UpdatedBy = user.UserId,
            UpdatedAt = now
        };
        var cards = sourceCards.Select(item => new WorkbenchCardEntity
        {
            Id = Guid.NewGuid(),
            TemplateVersionId = version.Id,
            CardType = item.CardType,
            Title = item.Title,
            Description = item.Description,
            SortOrder = item.SortOrder,
            Width = item.Width,
            DataSourceType = item.DataSourceType,
            FilterJson = item.FilterJson,
            RequiredResource = item.RequiredResource,
            RequiredAction = item.RequiredAction,
            IsEnabled = item.IsEnabled,
            CapabilityState = item.CapabilityState,
            CreatedAt = now,
            UpdatedAt = now
        }).ToList();
        var assignments = sourceAssignments.Select(item => new WorkbenchAssignmentEntity
        {
            Id = Guid.NewGuid(),
            TemplateVersionId = version.Id,
            AssignmentType = department is not null
                && item.AssignmentType == "Department"
                ? "Department"
                : item.AssignmentType,
            ScopeValue = department is not null
                && item.AssignmentType == "Department"
                ? department.Id.ToString("D")
                : item.ScopeValue,
            Priority = item.Priority,
            CreatedAt = now
        }).ToList();
        if (assignments.Count == 0)
        {
            assignments.Add(new WorkbenchAssignmentEntity
            {
                Id = Guid.NewGuid(),
                TemplateVersionId = version.Id,
                AssignmentType = department is not null ? "Department" : "Organization",
                ScopeValue = department?.Id.ToString("D") ?? user.OrganizationId.ToString("D"),
                Priority = 0,
                CreatedAt = now
            });
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.WorkbenchTemplates.Add(template);
            dbContext.WorkbenchTemplateVersions.Add(version);
            dbContext.WorkbenchCards.AddRange(cards);
            dbContext.WorkbenchAssignments.AddRange(assignments);
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Workbench",
                    "workbench.template.copied",
                    nameof(WorkbenchTemplateEntity),
                    template.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    reason: $"复制自 {source.Code} v{sourceVersion.VersionNumber}"),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    nameof(WorkbenchTemplateEntity),
                    template.Id.ToString("D"),
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

        return await GetTemplateAsync(user, template.Id, cancellationToken);
    }

    public async Task<WorkbenchTemplateDto> UpdateDraftAsync(
        UserPrincipal user,
        Guid templateId,
        UpdateWorkbenchTemplateDraftRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("workbench-template", AccessAction.Manage);
        ArgumentNullException.ThrowIfNull(request);
        var template = await dbContext.WorkbenchTemplates
            .SingleOrDefaultAsync(
                item => item.Id == templateId
                    && item.OrganizationId == user.OrganizationId,
                cancellationToken) ?? throw new NotFoundException("工作台模板不存在。");
        await EnsureCanManageTemplateAsync(user, template, cancellationToken);
        if (template.Status == "Retired")
        {
            throw new ConflictException(ErrorCodes.Conflict, "已停用模板需先恢复后再编辑。");
        }

        var department = await ValidateDepartmentAsync(user, request.DepartmentId, false, cancellationToken);
        var draft = await dbContext.WorkbenchTemplateVersions
            .Where(item => item.TemplateId == templateId && item.Status == "Draft")
            .OrderByDescending(item => item.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);
        var now = clock.UtcNow;
        if (draft is not null)
        {
            if (draft.Revision != request.ExpectedRevision)
            {
                throw new ConflictException(ErrorCodes.Conflict, "工作台模板草稿已变化，请刷新后重试。");
            }
        }
        else
        {
            if (request.ExpectedRevision != template.Version)
            {
                throw new ConflictException(ErrorCodes.Conflict, "工作台模板已变化，请刷新后重试。");
            }

            var latestNumber = await dbContext.WorkbenchTemplateVersions.AsNoTracking()
                .Where(item => item.TemplateId == templateId)
                .MaxAsync(item => (int?)item.VersionNumber, cancellationToken) ?? 0;
            draft = new WorkbenchTemplateVersionEntity
            {
                Id = Guid.NewGuid(),
                TemplateId = templateId,
                VersionNumber = latestNumber + 1,
                Status = "Draft",
                Revision = 1,
                CreatedBy = user.UserId,
                CreatedAt = now,
                UpdatedBy = user.UserId,
                UpdatedAt = now
            };
            dbContext.WorkbenchTemplateVersions.Add(draft);
        }

        template.Name = DomainChecks.Required(request.Name, "模板名称", 128);
        template.Description = NormalizeOptional(request.Description, 1000) ?? string.Empty;
        template.AudienceTitle = DomainChecks.Required(request.AudienceTitle, "岗位名称", 80);
        template.PrimaryQuestion = DomainChecks.Required(request.PrimaryQuestion, "首屏问题", 300);
        template.DepartmentId = department?.Id;
        template.SortOrder = request.SortOrder;
        template.IsDefault = request.IsDefault;
        template.Version++;
        template.UpdatedBy = user.UserId;
        template.UpdatedAt = now;

        var existingCards = await dbContext.WorkbenchCards
            .Where(item => item.TemplateVersionId == draft.Id)
            .ToListAsync(cancellationToken);
        var existingAssignments = await dbContext.WorkbenchAssignments
            .Where(item => item.TemplateVersionId == draft.Id)
            .ToListAsync(cancellationToken);
        if (existingCards.Count > 0)
        {
            dbContext.WorkbenchCards.RemoveRange(existingCards);
        }

        if (existingAssignments.Count > 0)
        {
            dbContext.WorkbenchAssignments.RemoveRange(existingAssignments);
        }

        var normalizedCards = NormalizeCards(request.Cards, now);
        var normalizedAssignments = NormalizeAssignments(
            request.Assignments,
            user,
            department?.Id,
            now);
        if (normalizedCards.Count > 0)
        {
            foreach (var card in normalizedCards)
            {
                card.TemplateVersionId = draft.Id;
            }

            dbContext.WorkbenchCards.AddRange(normalizedCards);
        }

        if (normalizedAssignments.Count > 0)
        {
            foreach (var assignment in normalizedAssignments)
            {
                assignment.TemplateVersionId = draft.Id;
            }

            dbContext.WorkbenchAssignments.AddRange(normalizedAssignments);
        }

        draft.Revision++;
        draft.UpdatedBy = user.UserId;
        draft.UpdatedAt = now;
        draft.ImpactAnalysisJson = BuildImpactJson(normalizedCards, normalizedAssignments, now);
        draft.ValidationJson = null;
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Workbench",
                "workbench.template.draft_updated",
                nameof(WorkbenchTemplateVersionEntity),
                draft.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                after: draft.ImpactAnalysisJson),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetTemplateAsync(user, templateId, cancellationToken);
    }

    public async Task<WorkbenchTemplateValidationDto> ValidateAsync(
        UserPrincipal user,
        Guid templateId,
        CancellationToken cancellationToken)
    {
        EnsureRead(user);
        var template = await dbContext.WorkbenchTemplates.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == templateId
                    && item.OrganizationId == user.OrganizationId,
                cancellationToken) ?? throw new NotFoundException("工作台模板不存在。");
        EnsureCanViewTemplate(user, template);
        var draft = await dbContext.WorkbenchTemplateVersions.AsNoTracking()
            .Where(item => item.TemplateId == templateId && item.Status == "Draft")
            .OrderByDescending(item => item.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);
        if (draft is null)
        {
            return new WorkbenchTemplateValidationDto(
                false,
                ["没有可发布草稿。"],
                [],
                0,
                0,
                "模板没有草稿，不会影响线上。");
        }

        var cards = await dbContext.WorkbenchCards.AsNoTracking()
            .Where(item => item.TemplateVersionId == draft.Id)
            .ToListAsync(cancellationToken);
        var assignments = await dbContext.WorkbenchAssignments.AsNoTracking()
            .Where(item => item.TemplateVersionId == draft.Id)
            .ToListAsync(cancellationToken);
        var errors = new List<string>();
        var warnings = new List<string>();
        if (cards.Count == 0) errors.Add("模板至少需要一张卡片。");
        if (assignments.Count == 0) errors.Add("模板至少需要一个匹配范围。");
        if (template.IsDefault && !assignments.Any(item => item.AssignmentType == "Organization"))
        {
            warnings.Add("默认模板没有组织级分配，只会在指定范围匹配。");
        }

        if (cards.Count(item => item.CapabilityState == "NotConnected") == cards.Count && cards.Count > 0)
        {
            errors.Add("模板不能全部由尚未接入的卡片组成。");
        }

        var result = new WorkbenchTemplateValidationDto(
            errors.Count == 0,
            errors,
            warnings,
            cards.Count(item => item.IsEnabled),
            assignments.Count,
            $"发布后影响 {assignments.Count} 个匹配范围，显示 {cards.Count(item => item.IsEnabled)} 张卡片；草稿当前不影响线上。");
        return result;
    }

    public async Task<WorkbenchTemplateDto> PublishAsync(
        UserPrincipal user,
        Guid templateId,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("workbench-template", AccessAction.Approve);
        ValidateIdempotencyKey(idempotencyKey);
        var template = await dbContext.WorkbenchTemplates
            .SingleOrDefaultAsync(
                item => item.Id == templateId
                    && item.OrganizationId == user.OrganizationId,
                cancellationToken) ?? throw new NotFoundException("工作台模板不存在。");
        await EnsureCanManageTemplateAsync(user, template, cancellationToken);
        var hash = Hash(new { templateId, action = "publish" });
        var scope = $"workbench-template:publish:{templateId:D}:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.RequestHash != hash)
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同发布请求。");
            }

            return await GetTemplateAsync(user, templateId, cancellationToken);
        }

        var validation = await ValidateAsync(user, templateId, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(string.Join("；", validation.Errors));
        }

        var draft = await dbContext.WorkbenchTemplateVersions
            .Where(item => item.TemplateId == templateId && item.Status == "Draft")
            .OrderByDescending(item => item.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ConflictException(ErrorCodes.Conflict, "工作台模板没有待发布草稿。");
        var now = clock.UtcNow;
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var published = await dbContext.WorkbenchTemplateVersions
                .Where(item => item.TemplateId == templateId && item.Status == "Published")
                .ToListAsync(cancellationToken);
            published.ForEach(item => item.Status = "Retired");
            draft.Status = "Published";
            draft.PublishedBy = user.UserId;
            draft.PublishedAt = now;
            draft.UpdatedBy = user.UserId;
            draft.UpdatedAt = now;
            draft.ValidationJson = JsonSerializer.Serialize(validation, JsonSerializerOptions.Web);
            template.Status = "Published";
            template.UpdatedBy = user.UserId;
            template.UpdatedAt = now;
            template.Version++;
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Workbench",
                    "workbench.template.published",
                    nameof(WorkbenchTemplateEntity),
                    template.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: JsonSerializer.Serialize(
                        new { draft.VersionNumber, Impact = validation.ImpactSummary },
                        JsonSerializerOptions.Web)),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    nameof(WorkbenchTemplateEntity),
                    template.Id.ToString("D"),
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

        return await GetTemplateAsync(user, templateId, cancellationToken);
    }

    public async Task<WorkbenchTemplateDto> SetStatusAsync(
        UserPrincipal user,
        Guid templateId,
        bool retire,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("workbench-template", AccessAction.Manage);
        var template = await dbContext.WorkbenchTemplates
            .SingleOrDefaultAsync(
                item => item.Id == templateId
                    && item.OrganizationId == user.OrganizationId,
                cancellationToken) ?? throw new NotFoundException("工作台模板不存在。");
        await EnsureCanManageTemplateAsync(user, template, cancellationToken);
        if (template.IsSystem && retire)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "系统保留模板不能停用。");
        }

        if (!retire && !await dbContext.WorkbenchTemplateVersions.AnyAsync(
                item => item.TemplateId == templateId && item.Status == "Published",
                cancellationToken))
        {
            throw new ConflictException(ErrorCodes.Conflict, "模板没有发布版本，不能恢复为线上模板。");
        }

        var now = clock.UtcNow;
        template.Status = retire ? "Retired" : "Published";
        template.RetiredBy = retire ? user.UserId : null;
        template.RetiredAt = retire ? now : null;
        template.UpdatedBy = user.UserId;
        template.UpdatedAt = now;
        template.Version++;
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Workbench",
                retire ? "workbench.template.retired" : "workbench.template.restored",
                nameof(WorkbenchTemplateEntity),
                template.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                reason: retire ? "停用工作台模板" : "恢复工作台模板"),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetTemplateAsync(user, templateId, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkbenchTemplateVersionDto>> ListVersionsAsync(
        UserPrincipal user,
        Guid templateId,
        CancellationToken cancellationToken)
    {
        var template = await dbContext.WorkbenchTemplates.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == templateId
                    && item.OrganizationId == user.OrganizationId,
                cancellationToken) ?? throw new NotFoundException("工作台模板不存在。");
        EnsureCanViewTemplate(user, template);
        var versions = await dbContext.WorkbenchTemplateVersions.AsNoTracking()
            .Where(item => item.TemplateId == templateId)
            .OrderByDescending(item => item.VersionNumber)
            .ToListAsync(cancellationToken);
        var result = new List<WorkbenchTemplateVersionDto>();
        foreach (var version in versions)
        {
            result.Add(await MapVersionAsync(version, cancellationToken));
        }

        return result;
    }

    private async Task<(WorkbenchTemplateEntity Template, WorkbenchTemplateVersionEntity Version, WorkbenchAssignmentEntity Assignment)?> MatchTemplateAsync(
        UserPrincipal user,
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        var templates = await dbContext.WorkbenchTemplates.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId
                && item.Status == "Published"
                && (!item.DepartmentId.HasValue || item.DepartmentId == departmentId))
            .ToListAsync(cancellationToken);
        if (templates.Count == 0)
        {
            return null;
        }

        var templateIds = templates.Select(item => item.Id).ToArray();
        var versions = await dbContext.WorkbenchTemplateVersions.AsNoTracking()
            .Where(item => templateIds.Contains(item.TemplateId) && item.Status == "Published")
            .OrderByDescending(item => item.VersionNumber)
            .ToListAsync(cancellationToken);
        var versionByTemplate = versions.GroupBy(item => item.TemplateId)
            .ToDictionary(group => group.Key, group => group.First());
        var versionIds = versionByTemplate.Values.Select(item => item.Id).ToArray();
        var assignments = await dbContext.WorkbenchAssignments.AsNoTracking()
            .Where(item => versionIds.Contains(item.TemplateVersionId))
            .ToListAsync(cancellationToken);
        var candidates = new List<(WorkbenchTemplateEntity Template, WorkbenchTemplateVersionEntity Version, WorkbenchAssignmentEntity Assignment, int Rank)>();
        foreach (var template in templates)
        {
            if (!versionByTemplate.TryGetValue(template.Id, out var version))
            {
                continue;
            }

            foreach (var assignment in assignments.Where(item => item.TemplateVersionId == version.Id))
            {
                var matches = assignment.AssignmentType switch
                {
                    "User" => string.Equals(
                        assignment.ScopeValue,
                        user.UserId.ToString("D"),
                        StringComparison.OrdinalIgnoreCase),
                    "Role" => user.HasRoleInScope(assignment.ScopeValue, departmentId),
                    "Department" => string.Equals(
                        assignment.ScopeValue,
                        departmentId.ToString("D"),
                        StringComparison.OrdinalIgnoreCase),
                    "Organization" => string.Equals(
                        assignment.ScopeValue,
                        user.OrganizationId.ToString("D"),
                        StringComparison.OrdinalIgnoreCase),
                    _ => false
                };
                if (!matches)
                {
                    continue;
                }

                var typeRank = assignment.AssignmentType switch
                {
                    "User" => 400,
                    "Role" => 300,
                    "Department" => 200,
                    "Organization" => 100,
                    _ => 0
                };
                candidates.Add((template, version, assignment, typeRank + assignment.Priority));
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        var winner = candidates
            .OrderByDescending(item => item.Rank)
            .ThenByDescending(item => item.Template.IsDefault)
            .ThenBy(item => item.Template.SortOrder)
            .ThenByDescending(item => item.Version.PublishedAt)
            .ThenBy(item => item.Template.Code, StringComparer.Ordinal)
            .First();
        return (winner.Template, winner.Version, winner.Assignment);
    }

    private async Task<WorkbenchResolvedCardDto> ResolveCardAsync(
        UserPrincipal user,
        DepartmentEntity department,
        WorkbenchCardEntity card,
        bool includeTestData,
        CancellationToken cancellationToken)
    {
        var filter = ParseFilter(card.FilterJson);
        var top = ParseInt(filter.GetValueOrDefault("top"), 5, 1, 20);
        if (card.CapabilityState == "NotConnected")
        {
            return new WorkbenchResolvedCardDto(
                card.Id.ToString("D"),
                card.CardType,
                card.Title,
                card.Description,
                card.SortOrder,
                card.Width,
                "NotConnected",
                "该能力尚未接入，不会展示模拟数据。",
                0,
                []);
        }

        var items = card.CardType switch
        {
            "work_items" => await ResolveWorkItemsAsync(
                user, department.Id, includeTestData, filter, top, cancellationToken),
            "workflow_tasks" => await ResolveWorkItemsAsync(
                user, department.Id, includeTestData, filter, top, cancellationToken,
                sourceTypes: ["WorkflowTask"]),
            "project_tasks" => await ResolveWorkItemsAsync(
                user, department.Id, includeTestData, filter, top, cancellationToken,
                sourceTypes: ["ProjectTask"]),
            "overdue_risks" => await ResolveOverdueItemsAsync(
                user, department.Id, includeTestData, filter, top, cancellationToken),
            "team_pending" => await ResolveTeamPendingAsync(
                user, department.Id, includeTestData, filter, top, cancellationToken),
            "project_portfolio" => await ResolveProjectsAsync(
                user, department.Id, includeTestData, filter, top, cancellationToken),
            "business_objects" => await ResolveBusinessObjectsAsync(
                user, department.Id, includeTestData, filter, top, cancellationToken),
            "recent_files" => await ResolveFilesAsync(
                user, department.Id, includeTestData, filter, top, cancellationToken),
            "minutes" => await ResolveMinutesAsync(
                user, department.Id, includeTestData, filter, top, cancellationToken),
            "ai_activity" => await ResolveAiActivityAsync(
                user, department.Id, includeTestData, filter, top, cancellationToken),
            _ => []
        };
        return new WorkbenchResolvedCardDto(
            card.Id.ToString("D"),
            card.CardType,
            card.Title,
            card.Description,
            card.SortOrder,
            card.Width,
            items.Count > 0 ? "Ready" : "Empty",
            items.Count > 0 ? "已按当前权限返回现有业务事项。" : EmptyMessage(card.CardType),
            items.Count,
            items);
    }

    private async Task<IReadOnlyList<WorkbenchCardItemDto>> ResolveWorkItemsAsync(
        UserPrincipal user,
        Guid departmentId,
        bool includeTestData,
        IReadOnlyDictionary<string, string?> filter,
        int top,
        CancellationToken cancellationToken,
        IReadOnlyList<string>? sourceTypes = null)
    {
        var work = await employeeWorkspaceQueryService.GetMyWorkAsync(user, includeTestData, cancellationToken);
        var items = work.PendingItems
            .Concat(work.NeedsAttention)
            .Concat(work.InitiatedByMe)
            .Concat(work.Participating)
            .GroupBy(item => item.Id)
            .Select(group => group.First())
            .Where(item => string.IsNullOrWhiteSpace(item.DepartmentId)
                || string.Equals(item.DepartmentId, departmentId.ToString("D"), StringComparison.OrdinalIgnoreCase));
        if (sourceTypes is { Count: > 0 })
        {
            var allowed = sourceTypes.ToHashSet(StringComparer.Ordinal);
            items = items.Where(item => allowed.Contains(item.SourceType));
        }

        return ApplyKeyword(items, filter)
            .OrderByDescending(item => item.IsPermanentHumanBoundary)
            .ThenBy(item => item.DueAt ?? DateTimeOffset.MaxValue)
            .ThenByDescending(item => item.OccurredAt)
            .Take(top)
            .Select(item => new WorkbenchCardItemDto(
                item.Id,
                item.BusinessTitle,
                item.ContextTitle,
                item.StatusLabel,
                item.RiskLabel,
                item.DueAt,
                item.DetailPath))
            .ToList();
    }

    private async Task<IReadOnlyList<WorkbenchCardItemDto>> ResolveOverdueItemsAsync(
        UserPrincipal user,
        Guid departmentId,
        bool includeTestData,
        IReadOnlyDictionary<string, string?> filter,
        int top,
        CancellationToken cancellationToken)
    {
        var work = await employeeWorkspaceQueryService.GetMyWorkAsync(user, includeTestData, cancellationToken);
        var items = work.PendingItems.Concat(work.NeedsAttention)
            .GroupBy(item => item.Id)
            .Select(group => group.First())
            .Where(item => item.IsPermanentHumanBoundary
                || item.PriorityGroup is "PermanentHumanBoundary" or "Overdue"
                || string.Equals(item.RiskLevel, "High", StringComparison.OrdinalIgnoreCase))
            .Where(item => string.IsNullOrWhiteSpace(item.DepartmentId)
                || string.Equals(item.DepartmentId, departmentId.ToString("D"), StringComparison.OrdinalIgnoreCase));
        return ApplyKeyword(items, filter)
            .OrderByDescending(item => item.IsPermanentHumanBoundary)
            .ThenBy(item => item.DueAt ?? DateTimeOffset.MaxValue)
            .Take(top)
            .Select(item => new WorkbenchCardItemDto(
                item.Id,
                item.BusinessTitle,
                item.ContextTitle,
                item.StatusLabel,
                item.RiskLabel,
                item.DueAt,
                item.DetailPath))
            .ToList();
    }

    private async Task<IReadOnlyList<WorkbenchCardItemDto>> ResolveTeamPendingAsync(
        UserPrincipal user,
        Guid departmentId,
        bool includeTestData,
        IReadOnlyDictionary<string, string?> filter,
        int top,
        CancellationToken cancellationToken)
    {
        if (!await IsDepartmentManagerAsync(user, departmentId, cancellationToken)
            || !user.HasPermission("workflow-task", AccessAction.Read))
        {
            return [];
        }

        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var query =
            from task in dbContext.WorkflowTasks.AsNoTracking()
            join instance in dbContext.WorkflowInstances.AsNoTracking() on task.WorkflowInstanceId equals instance.Id
            join businessObject in dbContext.BusinessObjects.AsNoTracking() on instance.BusinessObjectId equals businessObject.Id
            join assignee in dbContext.Users.AsNoTracking() on task.AssigneeId equals assignee.Id
            where task.DepartmentId == departmentId
                && task.Status == "Pending"
                && (includeTestData && user.IsPlatformAdmin || instance.EnvironmentCode == environment)
            orderby task.DueAt, task.CreatedAt descending
            select new
            {
                task.Id,
                BusinessTitle = businessObject.Title ?? businessObject.ObjectCode,
                AssigneeName = assignee.DisplayName,
                task.SlaStatus,
                task.DueAt
            };
        var rows = await query.Take(Math.Max(top * 4, 20)).ToListAsync(cancellationToken);
        return FilterByKeyword(
                rows,
                filter,
                row => $"{row.BusinessTitle} {row.AssigneeName} {row.SlaStatus}")
            .Take(top)
            .Select(row => new WorkbenchCardItemDto(
                row.Id.ToString("D"),
                row.BusinessTitle,
                $"负责人：{row.AssigneeName}",
                row.SlaStatus,
                string.Equals(row.SlaStatus, "Overdue", StringComparison.OrdinalIgnoreCase) ? "High" : "Medium",
                row.DueAt,
                $"/workflows?task={row.Id:D}"))
            .ToList();
    }

    private async Task<IReadOnlyList<WorkbenchCardItemDto>> ResolveProjectsAsync(
        UserPrincipal user,
        Guid departmentId,
        bool includeTestData,
        IReadOnlyDictionary<string, string?> filter,
        int top,
        CancellationToken cancellationToken)
    {
        if (!user.HasPermission("project", AccessAction.Read))
        {
            return [];
        }

        var projects = await projectService.ListAsync(user, 1, 100, includeTestData, cancellationToken);
        var rows = projects.Items.Where(item =>
            string.Equals(filter.GetValueOrDefault("projectScope"), "owned", StringComparison.OrdinalIgnoreCase)
                ? string.Equals(item.OwnerUserId, user.UserId.ToString("D"), StringComparison.OrdinalIgnoreCase)
                : string.Equals(item.DepartmentId, departmentId.ToString("D"), StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.OwnerUserId, user.UserId.ToString("D"), StringComparison.OrdinalIgnoreCase));
        return FilterByKeyword(rows, filter, row => $"{row.Name} {row.ObjectCode} {row.CurrentStage}")
            .OrderByDescending(row => row.OverdueTaskCount > 0 || row.OverdueMilestoneCount > 0)
            .ThenByDescending(row => row.UpdatedAt)
            .Take(top)
            .Select(row => new WorkbenchCardItemDto(
                row.Id,
                row.Name,
                $"{row.ObjectCode} · {row.CurrentStage} · {row.ProgressPercent}%",
                row.StatusDisplayName,
                row.OverdueTaskCount > 0 || row.OverdueMilestoneCount > 0 ? "High" : "Low",
                row.PlannedEndDate,
                $"/projects/{row.Id}"))
            .ToList();
    }

    private async Task<IReadOnlyList<WorkbenchCardItemDto>> ResolveBusinessObjectsAsync(
        UserPrincipal user,
        Guid departmentId,
        bool includeTestData,
        IReadOnlyDictionary<string, string?> filter,
        int top,
        CancellationToken cancellationToken)
    {
        if (!user.HasPermission("business-object", AccessAction.Read))
        {
            return [];
        }

        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var query =
            from item in dbContext.BusinessObjects.AsNoTracking()
            join type in dbContext.ObjectTypes.AsNoTracking() on item.ObjectTypeId equals type.Id
            where item.OrganizationId == user.OrganizationId
                && item.DepartmentId == departmentId
                && !item.IsDeleted
                && (includeTestData && user.IsPlatformAdmin || item.EnvironmentCode == environment)
            select new
            {
                item.Id,
                item.Title,
                item.ObjectCode,
                item.Status,
                item.UpdatedAt,
                TypeCode = type.Code,
                TypeName = type.Name
            };
        var objectCodes = ParseCsv(filter.GetValueOrDefault("objectCodes"));
        var rows = await query
            .OrderByDescending(item => item.UpdatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);
        if (objectCodes.Count > 0)
        {
            rows = rows.Where(item => objectCodes.Contains(item.TypeCode, StringComparer.OrdinalIgnoreCase)).ToList();
        }

        return FilterByKeyword(rows, filter, row => $"{row.Title} {row.ObjectCode} {row.Status} {row.TypeName}")
            .Take(top)
            .Select(row => new WorkbenchCardItemDto(
                row.Id.ToString("D"),
                row.Title ?? row.ObjectCode,
                $"{row.TypeName} · {row.ObjectCode}",
                row.Status,
                "Low",
                null,
                $"/objects?object={row.Id:D}"))
            .ToList();
    }

    private async Task<IReadOnlyList<WorkbenchCardItemDto>> ResolveFilesAsync(
        UserPrincipal user,
        Guid departmentId,
        bool includeTestData,
        IReadOnlyDictionary<string, string?> filter,
        int top,
        CancellationToken cancellationToken)
    {
        if (!user.HasPermission("file", AccessAction.Read))
        {
            return [];
        }

        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var query = dbContext.Attachments.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId
                && item.DepartmentId == departmentId
                && item.Status == "Active"
                && (includeTestData && user.IsPlatformAdmin || item.EnvironmentCode == environment))
            .OrderByDescending(item => item.UpdatedAt);
        var rows = await query.Take(100).ToListAsync(cancellationToken);
        return FilterByKeyword(rows, filter, row => $"{row.OriginalFileName} {row.MimeType}")
            .Take(top)
            .Select(row => new WorkbenchCardItemDto(
                row.Id.ToString("D"),
                row.OriginalFileName,
                $"{row.MimeType} · v{row.CurrentVersionNumber}",
                row.Status,
                row.Sensitivity == "Core" ? "High" : "Low",
                null,
                $"/documents?file={row.Id:D}"))
            .ToList();
    }

    private async Task<IReadOnlyList<WorkbenchCardItemDto>> ResolveMinutesAsync(
        UserPrincipal user,
        Guid departmentId,
        bool includeTestData,
        IReadOnlyDictionary<string, string?> filter,
        int top,
        CancellationToken cancellationToken)
    {
        if (!user.HasPermission("minutes", AccessAction.Read))
        {
            return [];
        }

        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var rows = await dbContext.MinutesSources.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId
                && item.DepartmentId == departmentId
                && (includeTestData && user.IsPlatformAdmin || item.EnvironmentCode == environment))
            .OrderByDescending(item => item.MeetingAt)
            .Take(100)
            .ToListAsync(cancellationToken);
        return FilterByKeyword(rows, filter, row => $"{row.Title} {row.SourceType} {row.Status}")
            .Take(top)
            .Select(row => new WorkbenchCardItemDto(
                row.Id.ToString("D"),
                row.Title,
                $"{row.SourceType} · {row.MeetingAt:yyyy-MM-dd HH:mm}",
                row.Status,
                "Low",
                row.MeetingAt,
                $"/minutes?source={row.Id:D}"))
            .ToList();
    }

    private async Task<IReadOnlyList<WorkbenchCardItemDto>> ResolveAiActivityAsync(
        UserPrincipal user,
        Guid departmentId,
        bool includeTestData,
        IReadOnlyDictionary<string, string?> filter,
        int top,
        CancellationToken cancellationToken)
    {
        if (!user.HasPermission("agent-run", AccessAction.Read))
        {
            return [];
        }

        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var rows = await dbContext.AgentRuns.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId
                && item.DepartmentId == departmentId
                && (item.RequestedBy == user.UserId
                    || user.IsPlatformAdmin
                    || user.HasRoleInScope("project-manager", departmentId))
                && (includeTestData && user.IsPlatformAdmin || item.EnvironmentCode == environment))
            .OrderByDescending(item => item.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
        return FilterByKeyword(rows, filter, row => $"{row.Action} {row.Status} {row.RiskLevel}")
            .Take(top)
            .Select(row => new WorkbenchCardItemDto(
                row.Id.ToString("D"),
                $"Agent · {row.Action}",
                row.ExternalModelCalled ? "外部模型" : "RuleSimulator",
                row.Status,
                row.RiskLevel,
                null,
                $"/agents?run={row.Id:D}"))
            .ToList();
    }

    private async Task<bool> IsDepartmentManagerAsync(
        UserPrincipal user,
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        if (user.IsPlatformAdmin)
        {
            return true;
        }

        return await dbContext.Departments.AsNoTracking().AnyAsync(
                item => item.Id == departmentId && item.ManagerUserId == user.UserId,
                cancellationToken)
            || await dbContext.UserDepartmentMemberships.AsNoTracking().AnyAsync(
                item => item.DepartmentId == departmentId
                    && item.UserId == user.UserId
                    && item.IsManager
                    && item.IsActive,
                cancellationToken);
    }

    private async Task EnsureCanManageTemplateAsync(
        UserPrincipal user,
        WorkbenchTemplateEntity template,
        CancellationToken cancellationToken)
    {
        if (user.IsPlatformAdmin)
        {
            return;
        }

        if (template.IsSystem || !template.DepartmentId.HasValue)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "只有平台管理员可以维护系统或组织级模板。");
        }

        user.EnsureDepartmentAccess(template.DepartmentId.Value);
        if (!await IsDepartmentManagerAsync(user, template.DepartmentId.Value, cancellationToken))
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "只有授权部门负责人可以维护该工作台模板。");
        }
    }

    private static void EnsureCanViewTemplate(UserPrincipal user, WorkbenchTemplateEntity template)
    {
        if (user.IsPlatformAdmin)
        {
            return;
        }

        if (template.IsSystem && !template.DepartmentId.HasValue)
        {
            return;
        }

        if (!template.DepartmentId.HasValue || !user.DepartmentIds.Contains(template.DepartmentId.Value))
        {
            throw new PermissionException(ErrorCodes.DepartmentScopeDenied, "当前身份没有该工作台模板范围。");
        }
    }

    private async Task<DepartmentEntity?> ValidateDepartmentAsync(
        UserPrincipal user,
        Guid? departmentId,
        bool allowOrganization,
        CancellationToken cancellationToken)
    {
        if (!departmentId.HasValue)
        {
            if (!allowOrganization && !user.IsPlatformAdmin)
            {
                throw new PermissionException(ErrorCodes.PermissionDenied, "只有平台管理员可以维护组织级模板。");
            }

            return null;
        }

        user.EnsureDepartmentAccess(departmentId.Value);
        if (!user.IsPlatformAdmin && !await IsDepartmentManagerAsync(user, departmentId.Value, cancellationToken))
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "只有部门负责人可以维护该部门模板。");
        }

        return await dbContext.Departments.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == departmentId.Value
                && item.OrganizationId == user.OrganizationId
                && item.IsActive,
            cancellationToken) ?? throw new ValidationException("工作台关联部门不存在或已停用。");
    }

    private async Task<IReadOnlyList<string>> LoadRoleNamesAsync(
        UserPrincipal user,
        CancellationToken cancellationToken)
    {
        var codes = user.Roles.ToArray();
        return await dbContext.Roles.AsNoTracking()
            .Where(item => codes.Contains(item.Code) && item.IsActive)
            .OrderBy(item => item.Name)
            .Select(item => item.Name)
            .ToListAsync(cancellationToken);
    }

    private async Task<WorkbenchTemplateDto> MapTemplateAsync(
        WorkbenchTemplateEntity template,
        CancellationToken cancellationToken)
    {
        var versions = await dbContext.WorkbenchTemplateVersions.AsNoTracking()
            .Where(item => item.TemplateId == template.Id)
            .OrderByDescending(item => item.VersionNumber)
            .ToListAsync(cancellationToken);
        var draftEntity = versions.FirstOrDefault(item => item.Status == "Draft");
        var publishedEntity = versions.FirstOrDefault(item => item.Status == "Published");
        var draft = draftEntity is null ? null : await MapVersionAsync(draftEntity, cancellationToken);
        var published = publishedEntity is null ? null : await MapVersionAsync(publishedEntity, cancellationToken);
        var departmentName = template.DepartmentId.HasValue
            ? await dbContext.Departments.AsNoTracking()
                .Where(item => item.Id == template.DepartmentId.Value)
                .Select(item => item.Name)
                .SingleOrDefaultAsync(cancellationToken)
            : null;
        return new WorkbenchTemplateDto(
            template.Id.ToString("D"),
            template.Code,
            template.Name,
            template.Description,
            template.AudienceTitle,
            template.PrimaryQuestion,
            template.DepartmentId?.ToString("D"),
            departmentName,
            template.IsSystem,
            template.IsDefault,
            template.Status,
            template.SortOrder,
            template.Version,
            template.CreatedAt,
            template.UpdatedAt,
            published?.PublishedAt,
            draft,
            published);
    }

    private async Task<WorkbenchTemplateVersionDto> MapVersionAsync(
        WorkbenchTemplateVersionEntity version,
        CancellationToken cancellationToken)
    {
        var cards = await dbContext.WorkbenchCards.AsNoTracking()
            .Where(item => item.TemplateVersionId == version.Id)
            .OrderBy(item => item.SortOrder)
            .ToListAsync(cancellationToken);
        var assignments = await dbContext.WorkbenchAssignments.AsNoTracking()
            .Where(item => item.TemplateVersionId == version.Id)
            .OrderBy(item => item.Priority)
            .ToListAsync(cancellationToken);
        return new WorkbenchTemplateVersionDto(
            version.Id.ToString("D"),
            version.VersionNumber,
            version.Status,
            version.Revision,
            version.ValidationJson,
            version.ImpactAnalysisJson,
            version.CreatedAt,
            version.PublishedAt,
            cards.Select(item => new WorkbenchCardAdminDto(
                item.Id.ToString("D"),
                item.CardType,
                item.Title,
                item.Description,
                item.SortOrder,
                item.Width,
                item.DataSourceType,
                ParseFilter(item.FilterJson),
                item.RequiredResource,
                item.RequiredAction,
                item.IsEnabled,
                item.CapabilityState)).ToList(),
            assignments.Select(item => new WorkbenchAssignmentDto(
                item.Id.ToString("D"),
                item.AssignmentType,
                item.ScopeValue,
                item.Priority)).ToList());
    }

    private static List<WorkbenchCardEntity> NormalizeCards(
        IReadOnlyList<WorkbenchCardWriteRequest>? cards,
        DateTimeOffset now)
    {
        if (cards is null || cards.Count == 0)
        {
            return [];
        }

        if (cards.Count > 30)
        {
            throw new ValidationException("单个工作台模板最多 30 张卡片。");
        }

        var duplicateOrders = cards.GroupBy(item => item.SortOrder)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicateOrders.Length > 0)
        {
            throw new ValidationException("工作台卡片排序不能重复。");
        }

        return cards.Select(item =>
        {
            var cardType = DomainChecks.Required(item.CardType, "卡片类型", 64);
            if (!CardTypes.Contains(cardType))
            {
                throw new ValidationException($"卡片类型 {cardType} 不在首批注册中心。");
            }

            var width = DomainChecks.Required(item.Width, "卡片宽度", 16);
            if (width is not ("Third" or "Half" or "Full"))
            {
                throw new ValidationException("卡片宽度只能是 Third、Half 或 Full。");
            }

            var resource = DomainChecks.Required(item.RequiredResource, "卡片权限资源", 128);
            if (!Enum.TryParse<AccessAction>(item.RequiredAction, true, out var action))
            {
                throw new ValidationException("卡片权限动作无效。");
            }

            var capability = string.IsNullOrWhiteSpace(item.CapabilityState)
                ? "Connected"
                : item.CapabilityState.Trim();
            if (capability is not ("Connected" or "NotConnected"))
            {
                throw new ValidationException("卡片能力状态无效。");
            }

            var filter = item.Filter ?? new Dictionary<string, string?>();
            ValidateFilter(filter);
            return new WorkbenchCardEntity
            {
                Id = Guid.NewGuid(),
                CardType = cardType,
                Title = DomainChecks.Required(item.Title, "卡片标题", 128),
                Description = NormalizeOptional(item.Description, 500),
                SortOrder = item.SortOrder,
                Width = width,
                DataSourceType = DomainChecks.Required(item.DataSourceType, "数据来源", 64),
                FilterJson = JsonSerializer.Serialize(filter, JsonSerializerOptions.Web),
                RequiredResource = resource,
                RequiredAction = action.ToString(),
                IsEnabled = item.IsEnabled,
                CapabilityState = capability,
                CreatedAt = now,
                UpdatedAt = now
            };
        }).ToList();
    }

    private static List<WorkbenchAssignmentEntity> NormalizeAssignments(
        IReadOnlyList<WorkbenchAssignmentWriteRequest>? assignments,
        UserPrincipal user,
        Guid? departmentId,
        DateTimeOffset now)
    {
        if (assignments is null || assignments.Count == 0)
        {
            return [];
        }

        var normalized = assignments.Select(item =>
        {
            var type = DomainChecks.Required(item.AssignmentType, "匹配类型", 32);
            if (!AssignmentTypes.Contains(type))
            {
                throw new ValidationException("匹配类型只能是 User、Role、Department 或 Organization。");
            }

            var scope = DomainChecks.Required(item.ScopeValue, "匹配范围", 128);
            if (type is "User" or "Department" && !Guid.TryParse(scope, out _))
            {
                throw new ValidationException($"{type} 匹配范围必须是有效 GUID。");
            }

            if (type == "Role" && !DomainChecks.RequiredIdentifier(scope, "角色编码", 80)
                    .Equals(scope, StringComparison.Ordinal))
            {
                throw new ValidationException("角色级匹配必须使用角色编码。");
            }

            if (type == "Organization"
                && !string.Equals(scope, user.OrganizationId.ToString("D"), StringComparison.OrdinalIgnoreCase))
            {
                throw new PermissionException(ErrorCodes.PermissionDenied, "不能分配给其他组织。");
            }

            if (type == "Department" && departmentId.HasValue
                && Guid.TryParse(scope, out var assignedDepartment)
                && assignedDepartment != departmentId.Value
                && !user.IsPlatformAdmin)
            {
                throw new PermissionException(ErrorCodes.PermissionDenied, "模板只能分配给当前授权部门。");
            }

            return new WorkbenchAssignmentEntity
            {
                Id = Guid.NewGuid(),
                AssignmentType = type,
                ScopeValue = scope,
                Priority = item.Priority,
                CreatedAt = now
            };
        }).ToList();
        var duplicate = normalized
            .GroupBy(item => $"{item.AssignmentType}|{item.ScopeValue}", StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ValidationException("工作台匹配范围不能重复。");
        }

        return normalized;
    }

    private static void ValidateFilter(IReadOnlyDictionary<string, string?> filter)
    {
        if (filter.Count > 12)
        {
            throw new ValidationException("卡片过滤条件最多 12 项。");
        }

        foreach (var (key, value) in filter)
        {
            if (!FilterKeys.Contains(key))
            {
                throw new ValidationException($"不支持卡片过滤字段：{key}。");
            }

            if (value is { Length: > 500 })
            {
                throw new ValidationException($"卡片过滤字段 {key} 过长。");
            }

            if (value is not null
                && (value.Contains("--", StringComparison.Ordinal)
                    || value.Contains("script", StringComparison.OrdinalIgnoreCase)
                    || value.Contains("select ", StringComparison.OrdinalIgnoreCase)
                    || value.Contains("javascript:", StringComparison.OrdinalIgnoreCase)
                    || value.Contains('<')
                    || value.Contains('>')))
            {
                throw new ValidationException("卡片过滤条件只能包含业务标识和普通文本。");
            }
        }
    }

    private static Dictionary<string, string?> ParseFilter(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string?>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string?>>(json)
                ?? new Dictionary<string, string?>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, string?>();
        }
    }

    private static bool CanUseCard(UserPrincipal user, WorkbenchCardEntity card)
    {
        if (!Enum.TryParse<AccessAction>(card.RequiredAction, true, out var action))
        {
            return false;
        }

        return user.IsPlatformAdmin || user.HasPermission(card.RequiredResource, action);
    }

    private static IEnumerable<WorkItemViewDto> ApplyKeyword(
        IEnumerable<WorkItemViewDto> items,
        IReadOnlyDictionary<string, string?> filter)
    {
        var keyword = filter.GetValueOrDefault("keyword");
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return items;
        }

        return items.Where(item =>
            $"{item.BusinessTitle} {item.ContextTitle} {item.CurrentStage} {item.StatusLabel}"
                .Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<T> FilterByKeyword<T>(
        IEnumerable<T> rows,
        IReadOnlyDictionary<string, string?> filter,
        Func<T, string> search)
    {
        var keyword = filter.GetValueOrDefault("keyword");
        return string.IsNullOrWhiteSpace(keyword)
            ? rows
            : rows.Where(row => search(row).Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static HashSet<string> ParseCsv(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static int ParseInt(string? value, int fallback, int min, int max) =>
        int.TryParse(value, out var parsed) ? Math.Clamp(parsed, min, max) : fallback;

    private static string EmptyMessage(string cardType) => cardType switch
    {
        "team_pending" => "当前身份不是部门负责人或没有团队待办读取权限。",
        "recent_files" => "当前部门暂无可见文件。",
        "minutes" => "当前部门暂无可见听记。",
        "project_portfolio" => "当前身份暂无匹配项目。",
        "project_tasks" => "当前没有符合条件的项目任务。",
        "ai_activity" => "当前部门暂无可见 Agent 活动。",
        _ => "当前没有符合条件的真实业务事项。"
    };

    private static string BuildImpactJson(
        List<WorkbenchCardEntity> cards,
        List<WorkbenchAssignmentEntity> assignments,
        DateTimeOffset now) =>
        JsonSerializer.Serialize(
            new
            {
                CardCount = cards.Count,
                EnabledCardCount = cards.Count(item => item.IsEnabled),
                NotConnectedCardCount = cards.Count(item => item.CapabilityState == "NotConnected"),
                AssignmentCount = assignments.Count,
                AssignmentTypes = assignments.GroupBy(item => item.AssignmentType)
                    .Select(group => new { Type = group.Key, Count = group.Count() }),
                ChangedAt = now
            },
            JsonSerializerOptions.Web);

    private static void EnsureRead(UserPrincipal user)
    {
        if (!user.IsPlatformAdmin && !user.HasPermission("workbench-template", AccessAction.Read))
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "当前身份没有工作台模板读取权限。");
        }
    }

    private static string NormalizeCode(string? value, string fieldName, int maxLength) =>
        DomainChecks.RequiredCode(value?.Trim().ToUpperInvariant() ?? string.Empty, fieldName, maxLength);

    private static string? NormalizeOptional(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : DomainChecks.Required(value, "工作台配置", maxLength);

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
}
