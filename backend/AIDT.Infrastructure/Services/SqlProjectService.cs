using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Audit;
using AIDT.Domain.BusinessObjects;
using AIDT.Domain.Common;
using AIDT.Domain.Documents;
using AIDT.Domain.Identity;
using AIDT.Domain.Projects;
using AIDT.Domain.Workflows;
using AIDT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Services;

public sealed class SqlProjectService(
    AidtDbContext dbContext,
    IAuditRepository auditRepository,
    IIdempotencyStore idempotencyStore,
    IUnitOfWork unitOfWork,
    IClock clock,
    DataEnvironmentOptions environmentOptions) : IProjectService
{
    private const string ProjectObjectTypeCode = "PROJECT";

    public async Task<PagedResult<ProjectDto>> ListAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        bool includeTestData,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("project", AccessAction.Read);
        user.EnsureOrganizationAccess(user.OrganizationId);
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var query = ApplyProjectScope(user, includeTestData, await LoadAccessibleProjectIdsAsync(user, cancellationToken));
        query = query.OrderByDescending(item => item.UpdatedAt).ThenByDescending(item => item.Id);
        var total = await query.LongCountAsync(cancellationToken);
        var entities = await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);
        var items = await MapProjectSummariesAsync(user, entities, cancellationToken);
        return new PagedResult<ProjectDto>(items, normalizedPage, normalizedPageSize, total);
    }

    public async Task<ProjectDetailDto?> GetAsync(
        UserPrincipal user,
        Guid projectId,
        bool includeTestData,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("project", AccessAction.Read);
        user.EnsureOrganizationAccess(user.OrganizationId);
        var entity = await LoadVisibleProjectAsync(
            user,
            projectId,
            includeTestData,
            traceId,
            cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var project = (await MapProjectSummariesAsync(user, [entity], cancellationToken)).Single();
        var members = await LoadMembersAsync(projectId, cancellationToken);
        var milestones = await LoadMilestonesAsync(projectId, cancellationToken);
        var tasks = await LoadProjectTasksAsync(projectId, cancellationToken);
        var relations = await dbContext.ProjectRelations.AsNoTracking()
            .Where(item => item.ProjectId == projectId)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new ProjectRelationDto(
                item.Id.ToString("D"),
                item.ProjectId.ToString("D"),
                item.EntityType,
                item.EntityId.ToString("D"),
                item.RelationCode,
                item.EvidenceExcerpt,
                item.CreatedAt))
            .ToListAsync(cancellationToken);

        var relatedBusinessObjectIds = relations
            .Where(item => item.EntityType == "BusinessObject")
            .Select(item => Guid.Parse(item.EntityId))
            .Concat(tasks.Where(item => item.BusinessObjectId is not null).Select(item => Guid.Parse(item.BusinessObjectId!)))
            .Distinct()
            .ToArray();
        var relatedBusinessObjects = await LoadBusinessObjectDtosAsync(
            user,
            relatedBusinessObjectIds,
            cancellationToken);

        var workflowIds = relations
            .Where(item => item.EntityType == "WorkflowInstance")
            .Select(item => Guid.Parse(item.EntityId))
            .Concat(tasks.Where(item => item.WorkflowInstanceId is not null).Select(item => Guid.Parse(item.WorkflowInstanceId!)))
            .Distinct()
            .ToArray();
        var workflows = await LoadProjectWorkflowsAsync(workflowIds, cancellationToken);

        var fileIds = relations
            .Where(item => item.EntityType == "Attachment")
            .Select(item => Guid.Parse(item.EntityId))
            .Concat(await dbContext.AttachmentLinks.AsNoTracking()
                .Where(item =>
                    (item.EntityType == "Project" && item.EntityId == projectId)
                    || (item.EntityType == "ProjectTask"
                        && dbContext.ProjectTasks.Any(task => task.Id == item.EntityId && task.ProjectId == projectId)))
                .Select(item => item.AttachmentId)
                .ToListAsync(cancellationToken))
            .Distinct()
            .ToArray();
        var files = await LoadFilesAsync(fileIds, cancellationToken);

        var minutesIds = relations
            .Where(item => item.EntityType == "MinutesSource")
            .Select(item => Guid.Parse(item.EntityId))
            .Distinct()
            .ToArray();
        var minutes = await LoadMinutesAsync(minutesIds, cancellationToken);
        var auditTimeline = user.IsPlatformAdmin || user.HasPermission("project-audit", AccessAction.Read)
            ? await LoadProjectAuditAsync(projectId, cancellationToken)
            : [];
        return new ProjectDetailDto(
            project,
            members,
            milestones,
            tasks,
            relations,
            relatedBusinessObjects,
            workflows,
            files,
            minutes,
            auditTimeline);
    }

    public async Task<ProjectDto> CreateAsync(
        UserPrincipal user,
        CreateProjectRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("project", AccessAction.Create);
        user.EnsureOrganizationAccess(user.OrganizationId);
        user.EnsureDepartmentAccess(request.DepartmentId);
        ValidateIdempotencyKey(idempotencyKey);
        var hash = Hash(request);
        var scope = $"project:create:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同项目请求。");
            }

            var existingProject = await LoadProjectEntityAsync(
                Guid.Parse(existing.ResponseId),
                false,
                cancellationToken);
            if (existingProject is null)
            {
                throw new NotFoundException("幂等结果对应的项目不存在。");
            }

            return (await MapProjectSummariesAsync(user, [existingProject], cancellationToken)).Single();
        }

        var ownerUserId = await ValidateActiveUserAsync(
            user.OrganizationId,
            request.OwnerUserId,
            cancellationToken);
        await ValidateDepartmentAsync(user.OrganizationId, request.DepartmentId, cancellationToken);
        ValidateDateRange(request.PlannedStartDate, request.PlannedEndDate);
        var status = ParseProjectStatus(request.InitialStatus ?? nameof(BusinessObjectStatus.Active));
        var objectType = await dbContext.ObjectTypes.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Code == ProjectObjectTypeCode,
                cancellationToken)
            ?? throw new ValidationException("PROJECT 对象类型尚未注册，请先执行迁移 007。");
        var objectCode = DomainChecks.RequiredCode(request.ObjectCode.ToUpperInvariant(), "项目编号", 80);
        if (await dbContext.BusinessObjects.AnyAsync(
                item => item.ObjectTypeId == objectType.Id
                    && item.ObjectCode == objectCode
                    && !item.IsDeleted,
                cancellationToken))
        {
            throw new ConflictException(ErrorCodes.DuplicateObjectCode, "项目编号不能重复。");
        }

        var now = clock.UtcNow;
        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var businessObject = BusinessObject.Create(
            user.OrganizationId,
            objectType.Id,
            objectCode,
            new Dictionary<string, string?>
            {
                ["OwnerUserId"] = ownerUserId.ToString("D"),
                ["CurrentStage"] = request.CurrentStage
            },
            now,
            request.DepartmentId,
            environment,
            DataEnvironment.IsTestEnvironment(environment),
            "AIDT",
            DomainChecks.Required(request.Name, "项目名称", 255),
            user.UserId);
        businessObject.ChangeStatus(status, businessObject.Version, now);
        var projectEntity = new BusinessObjectEntity
        {
            Id = businessObject.Id,
            OrganizationId = businessObject.OrganizationId,
            DepartmentId = businessObject.DepartmentId,
            EnvironmentCode = businessObject.EnvironmentCode,
            IsTestData = businessObject.IsTestData,
            SourceSystem = businessObject.SourceSystem,
            ObjectTypeId = businessObject.ObjectTypeId,
            ObjectCode = businessObject.ObjectCode,
            Title = businessObject.Title,
            CreatedByUserId = businessObject.CreatedByUserId,
            Status = businessObject.Status.ToString(),
            Version = businessObject.Version,
            FieldsJson = JsonSerializer.Serialize(businessObject.Fields, JsonSerializerOptions.Web),
            CreatedAt = businessObject.CreatedAt,
            UpdatedAt = businessObject.UpdatedAt
        };
        var profile = new ProjectProfileEntity
        {
            BusinessObjectId = businessObject.Id,
            OwnerUserId = ownerUserId,
            PlannedStartDate = request.PlannedStartDate?.ToUniversalTime(),
            PlannedEndDate = request.PlannedEndDate?.ToUniversalTime(),
            Objective = DomainChecks.Required(request.Objective, "项目目标", 2000),
            CurrentStage = DomainChecks.Required(request.CurrentStage, "当前阶段", 80),
            ConfidentialityLevel = NormalizeConfidentiality(request.ConfidentialityLevel),
            ProgressPercent = 0,
            CreatedBy = user.UserId,
            CreatedAt = now,
            UpdatedAt = now
        };
        var ownerMember = new ProjectMemberEntity
        {
            BusinessObjectId = businessObject.Id,
            UserId = ownerUserId,
            ProjectRole = nameof(ProjectMemberRole.Lead),
            IsLead = true,
            IsActive = true,
            CreatedBy = user.UserId,
            CreatedAt = now
        };

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.BusinessObjects.Add(projectEntity);
            dbContext.BusinessObjectVersions.Add(CreateProjectVersion(projectEntity, user.UserId, "项目创建"));
            dbContext.ProjectProfiles.Add(profile);
            dbContext.ProjectMembers.Add(ownerMember);
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "PMO",
                    "project.created",
                    "Project",
                    businessObject.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: JsonSerializer.Serialize(new
                    {
                        businessObject.ObjectCode,
                        businessObject.Title,
                        Status = status.ToString(),
                        DepartmentId = businessObject.DepartmentId,
                        EnvironmentCode = businessObject.EnvironmentCode,
                        OwnerUserId = ownerUserId
                    }, JsonSerializerOptions.Web)),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "Project",
                    businessObject.Id.ToString("D"),
                    "completed",
                    now,
                    now.AddDays(30)),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return (await MapProjectSummariesAsync(user, [projectEntity], cancellationToken)).Single();
    }

    public async Task<ProjectDto> UpdateAsync(
        UserPrincipal user,
        Guid projectId,
        UpdateProjectRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("project", AccessAction.Update);
        var project = await LoadTrackedManagedProjectAsync(
            user,
            projectId,
            traceId,
            cancellationToken);
        var profile = await dbContext.ProjectProfiles.SingleAsync(
            item => item.BusinessObjectId == projectId,
            cancellationToken);
        if (request.ExpectedVersion != project.Version)
        {
            throw new ConflictException(ErrorCodes.Conflict, "项目版本已变化，请刷新后重试。");
        }

        var ownerId = await ValidateActiveUserAsync(user.OrganizationId, request.OwnerUserId, cancellationToken);
        ValidateDateRange(request.PlannedStartDate, request.PlannedEndDate);
        var before = JsonSerializer.Serialize(new
        {
            project.Title,
            profile.OwnerUserId,
            profile.CurrentStage,
            profile.Objective
        }, JsonSerializerOptions.Web);
        project.Title = DomainChecks.Required(request.Name, "项目名称", 255);
        project.Version++;
        project.UpdatedAt = clock.UtcNow;
        profile.OwnerUserId = ownerId;
        profile.PlannedStartDate = request.PlannedStartDate?.ToUniversalTime();
        profile.PlannedEndDate = request.PlannedEndDate?.ToUniversalTime();
        profile.Objective = DomainChecks.Required(request.Objective, "项目目标", 2000);
        profile.CurrentStage = DomainChecks.Required(request.CurrentStage, "当前阶段", 80);
        profile.ConfidentialityLevel = NormalizeConfidentiality(request.ConfidentialityLevel);
        profile.UpdatedAt = project.UpdatedAt;
        dbContext.BusinessObjectVersions.Add(
            CreateProjectVersion(project, user.UserId, "project.updated"));
        await UpsertLeadMemberAsync(projectId, ownerId, user.UserId, project.UpdatedAt, cancellationToken);
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "PMO",
                "project.updated",
                "Project",
                projectId.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                project.UpdatedAt,
                before,
                JsonSerializer.Serialize(new
                {
                    project.Title,
                    OwnerUserId = ownerId,
                    profile.CurrentStage,
                    profile.Objective
                }, JsonSerializerOptions.Web)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await MapProjectSummariesAsync(user, [project], cancellationToken)).Single();
    }

    public async Task<ProjectDto> ChangeStatusAsync(
        UserPrincipal user,
        Guid projectId,
        ChangeProjectStatusRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("project", AccessAction.Update);
        var project = await LoadTrackedManagedProjectAsync(
            user,
            projectId,
            traceId,
            cancellationToken);
        if (request.ExpectedVersion != project.Version)
        {
            throw new ConflictException(ErrorCodes.Conflict, "项目版本已变化，请刷新后重试。");
        }

        var current = Enum.Parse<BusinessObjectStatus>(project.Status);
        var next = ParseProjectStatus(request.Status);
        ProjectRules.EnsureStatusTransition(current, next);
        if (next == BusinessObjectStatus.Completed
            && await dbContext.ProjectTasks.AnyAsync(
                item => item.ProjectId == projectId
                    && (item.Status == nameof(ProjectTaskStatus.Pending)
                        || item.Status == nameof(ProjectTaskStatus.InProgress)),
                cancellationToken))
        {
            throw new ConflictException(ErrorCodes.Conflict, "项目仍有未完成任务，不能标记完成。");
        }

        var now = clock.UtcNow;
        project.Status = next.ToString();
        project.Version++;
        project.UpdatedAt = now;
        dbContext.BusinessObjectVersions.Add(
            CreateProjectVersion(project, user.UserId, "project.status_changed"));
        var profile = await dbContext.ProjectProfiles.SingleAsync(
            item => item.BusinessObjectId == projectId,
            cancellationToken);
        if (next == BusinessObjectStatus.Completed)
        {
            profile.ProgressPercent = 100;
        }

        profile.UpdatedAt = now;
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "PMO",
                "project.status_changed",
                "Project",
                projectId.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                before: current.ToString(),
                after: next.ToString(),
                reason: request.Comment),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await MapProjectSummariesAsync(user, [project], cancellationToken)).Single();
    }

    public async Task<IReadOnlyList<ProjectCandidateUserDto>> ListCandidatesAsync(
        UserPrincipal user,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("project-member", AccessAction.Read);
        var project = await LoadVisibleProjectAsync(
            user,
            projectId,
            false,
            "project-member-candidates",
            cancellationToken) ?? throw new NotFoundException("项目不存在。");
        var rows = await (
            from candidate in dbContext.Users.AsNoTracking()
            join membership in dbContext.UserDepartmentMemberships.AsNoTracking()
                on candidate.Id equals membership.UserId
            join department in dbContext.Departments.AsNoTracking()
                on membership.DepartmentId equals department.Id
            where candidate.OrganizationId == user.OrganizationId
                && candidate.IsActive
                && !candidate.IsServiceIdentity
                && membership.IsActive
                && membership.IsPrimary
            orderby department.Id == project.DepartmentId descending, candidate.DisplayName
            select new ProjectCandidateUserDto(
                candidate.Id.ToString("D"),
                candidate.UserName,
                candidate.DisplayName,
                department.Id.ToString("D"),
                department.Name))
            .Take(100)
            .ToListAsync(cancellationToken);
        return rows;
    }

    public async Task<ProjectMemberDto> AddMemberAsync(
        UserPrincipal user,
        Guid projectId,
        AddProjectMemberRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("project-member", AccessAction.Manage);
        await LoadManagedProjectAsync(user, projectId, traceId, cancellationToken);
        await ValidateActiveUserAsync(user.OrganizationId, request.UserId, cancellationToken);
        var role = ParseMemberRole(request.ProjectRole);
        if (request.IsLead || role == ProjectMemberRole.Lead)
        {
            throw new ValidationException("请通过编辑项目更换负责人，成员管理不能绕过唯一 Lead 规则。");
        }

        var now = clock.UtcNow;
        var member = await dbContext.ProjectMembers.SingleOrDefaultAsync(
            item => item.BusinessObjectId == projectId && item.UserId == request.UserId,
            cancellationToken);
        if (member is null)
        {
            member = new ProjectMemberEntity
            {
                BusinessObjectId = projectId,
                UserId = request.UserId,
                CreatedBy = user.UserId,
                CreatedAt = now
            };
            dbContext.ProjectMembers.Add(member);
        }
        else if (member.IsLead)
        {
            throw new ConflictException(ErrorCodes.Conflict, "项目主负责人不能重复设置，请先变更负责人。");
        }

        member.ProjectRole = role.ToString();
        member.IsLead = false;
        member.IsActive = true;
        member.RemovedAt = null;
        member.RemovedBy = null;
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "PMO",
                "project.member_added",
                "Project",
                projectId.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                after: JsonSerializer.Serialize(new
                {
                    UserId = request.UserId,
                    Role = role.ToString(),
                    IsLead = false
                }, JsonSerializerOptions.Web)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await LoadMembersAsync(projectId, cancellationToken))
            .Single(item => item.UserId == request.UserId.ToString("D"));
    }

    public async Task<ProjectMemberDto> RemoveMemberAsync(
        UserPrincipal user,
        Guid projectId,
        Guid userId,
        RemoveProjectMemberRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("project-member", AccessAction.Manage);
        var project = await LoadManagedProjectAsync(user, projectId, traceId, cancellationToken);
        var profile = await dbContext.ProjectProfiles.AsNoTracking()
            .SingleAsync(item => item.BusinessObjectId == projectId, cancellationToken);
        if (profile.OwnerUserId == userId)
        {
            throw new ConflictException(ErrorCodes.Conflict, "必须先将项目负责人更换给其他成员，才能移除该成员。");
        }

        var member = await dbContext.ProjectMembers.SingleOrDefaultAsync(
            item => item.BusinessObjectId == projectId && item.UserId == userId && item.IsActive,
            cancellationToken) ?? throw new NotFoundException("项目成员不存在。");
        if (member.IsLead)
        {
            throw new ConflictException(ErrorCodes.Conflict, "项目主负责人不能直接移除。");
        }

        var now = clock.UtcNow;
        member.IsActive = false;
        member.RemovedBy = user.UserId;
        member.RemovedAt = now;
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "PMO",
                "project.member_removed",
                "Project",
                projectId.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                before: userId.ToString("D"),
                reason: request.Reason),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await LoadMembersAsync(project.Id, cancellationToken))
            .Single(item => item.UserId == userId.ToString("D"));
    }

    public async Task<ProjectMilestoneDto> AddMilestoneAsync(
        UserPrincipal user,
        Guid projectId,
        CreateProjectMilestoneRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("milestone", AccessAction.Manage);
        await LoadManagedProjectAsync(user, projectId, traceId, cancellationToken);
        await EnsureProjectMemberAsync(projectId, request.OwnerUserId, cancellationToken);
        var status = ParseMilestoneStatus(request.Status);
        var now = clock.UtcNow;
        var milestone = new ProjectMilestoneEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Title = DomainChecks.Required(request.Title, "里程碑标题", 255),
            OwnerUserId = request.OwnerUserId,
            PlannedDate = request.PlannedDate.ToUniversalTime(),
            Status = status.ToString(),
            CompletedAt = status == ProjectMilestoneStatus.Completed ? now : null,
            Version = 1,
            CreatedBy = user.UserId,
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.ProjectMilestones.Add(milestone);
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "PMO",
                "project.milestone_created",
                "Project",
                projectId.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                after: JsonSerializer.Serialize(new
                {
                    milestone.Id,
                    milestone.Title,
                    milestone.OwnerUserId,
                    milestone.PlannedDate,
                    milestone.Status
                }, JsonSerializerOptions.Web)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await RecalculateProjectProgressAsync(projectId, now, cancellationToken);
        return (await LoadMilestonesAsync(projectId, cancellationToken))
            .Single(item => item.Id == milestone.Id.ToString("D"));
    }

    public async Task<ProjectMilestoneDto> UpdateMilestoneAsync(
        UserPrincipal user,
        Guid projectId,
        Guid milestoneId,
        UpdateProjectMilestoneRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("milestone", AccessAction.Manage);
        await LoadManagedProjectAsync(user, projectId, traceId, cancellationToken);
        await EnsureProjectMemberAsync(projectId, request.OwnerUserId, cancellationToken);
        var milestone = await dbContext.ProjectMilestones.SingleOrDefaultAsync(
            item => item.Id == milestoneId && item.ProjectId == projectId,
            cancellationToken) ?? throw new NotFoundException("项目里程碑不存在。");
        if (request.ExpectedVersion != milestone.Version)
        {
            throw new ConflictException(ErrorCodes.Conflict, "里程碑版本已变化，请刷新后重试。");
        }

        var before = JsonSerializer.Serialize(new
        {
            milestone.Title,
            milestone.OwnerUserId,
            milestone.PlannedDate,
            milestone.Status
        }, JsonSerializerOptions.Web);
        var status = ParseMilestoneStatus(request.Status);
        var now = clock.UtcNow;
        milestone.Title = DomainChecks.Required(request.Title, "里程碑标题", 255);
        milestone.OwnerUserId = request.OwnerUserId;
        milestone.PlannedDate = request.PlannedDate.ToUniversalTime();
        milestone.Status = status.ToString();
        milestone.CompletedAt = status == ProjectMilestoneStatus.Completed
            ? milestone.CompletedAt ?? now
            : null;
        milestone.Version++;
        milestone.UpdatedAt = now;
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "PMO",
                "project.milestone_changed",
                "Project",
                projectId.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                before,
                JsonSerializer.Serialize(new
                {
                    milestone.Title,
                    milestone.OwnerUserId,
                    milestone.PlannedDate,
                    milestone.Status
                }, JsonSerializerOptions.Web)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await RecalculateProjectProgressAsync(projectId, now, cancellationToken);
        return (await LoadMilestonesAsync(projectId, cancellationToken))
            .Single(item => item.Id == milestone.Id.ToString("D"));
    }

    public async Task<ProjectRelationDto> AddRelationAsync(
        UserPrincipal user,
        Guid projectId,
        CreateProjectRelationRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("project", AccessAction.Update);
        var project = await LoadManagedProjectAsync(user, projectId, traceId, cancellationToken);
        var entityType = NormalizeRelationEntityType(request.EntityType);
        await EnsureRelationTargetAsync(user, project, entityType, request.EntityId, cancellationToken);
        var relationCode = DomainChecks.RequiredIdentifier(request.RelationCode, "关系编码", 64);
        var existing = await dbContext.ProjectRelations.AsNoTracking().SingleOrDefaultAsync(
            item => item.ProjectId == projectId
                && item.EntityType == entityType
                && item.EntityId == request.EntityId
                && item.RelationCode == relationCode,
            cancellationToken);
        if (existing is not null)
        {
            return MapRelation(existing);
        }

        var now = clock.UtcNow;
        var relation = new ProjectRelationEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            EntityType = entityType,
            EntityId = request.EntityId,
            RelationCode = relationCode,
            EvidenceExcerpt = string.IsNullOrWhiteSpace(request.EvidenceExcerpt)
                ? null
                : DomainChecks.Required(request.EvidenceExcerpt, "证据摘要", 4000),
            CreatedBy = user.UserId,
            CreatedAt = now
        };
        dbContext.ProjectRelations.Add(relation);
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "PMO",
                "project.relation_added",
                "Project",
                projectId.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                after: JsonSerializer.Serialize(new
                {
                    relation.EntityType,
                    relation.EntityId,
                    relation.RelationCode
                }, JsonSerializerOptions.Web)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapRelation(relation);
    }

    public async Task<ProjectTaskDto> CreateTaskAsync(
        UserPrincipal user,
        Guid projectId,
        CreateProjectTaskRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("project-task", AccessAction.Create);
        ValidateIdempotencyKey(idempotencyKey);
        var project = await LoadManagedProjectAsync(user, projectId, traceId, cancellationToken);
        await EnsureProjectMemberAsync(projectId, request.AssigneeId, cancellationToken);
        var priority = ParseTaskPriority(request.Priority);
        var now = clock.UtcNow;
        var dueAt = request.DueAt.ToUniversalTime();
        if (dueAt <= now)
        {
            throw new ValidationException("项目任务截止时间必须晚于当前时间。");
        }

        var scope = $"project-task:create:{user.UserId:D}:{projectId:D}";
        var hash = Hash(request);
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同项目任务请求。");
            }

            var existingTask = await LoadProjectTaskEntityAsync(
                Guid.Parse(existing.ResponseId),
                cancellationToken);
            return (await LoadProjectTasksAsync(existingTask.ProjectId, cancellationToken))
                .Single(item => item.Id == existingTask.Id.ToString("D"));
        }

        var task = new ProjectTaskEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            AssigneeId = request.AssigneeId,
            DepartmentId = project.DepartmentId,
            Title = DomainChecks.Required(request.Title, "项目任务标题", 255),
            Content = DomainChecks.Required(request.Content, "项目任务内容", 4000),
            DueAt = dueAt,
            Priority = priority,
            Status = nameof(ProjectTaskStatus.Pending),
            Version = 1,
            EnvironmentCode = project.EnvironmentCode,
            IsTestData = project.IsTestData,
            CreatedBy = user.UserId,
            CreatedAt = now
        };

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.ProjectTasks.Add(task);
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "PMO",
                    "project.task_created",
                    "Project",
                    projectId.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: JsonSerializer.Serialize(new
                    {
                        task.Id,
                        task.Title,
                        task.AssigneeId,
                        task.DueAt,
                        task.Priority,
                        task.Status
                    }, JsonSerializerOptions.Web)),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "ProjectTask",
                    task.Id.ToString("D"),
                    "completed",
                    now,
                    now.AddDays(30)),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        await RecalculateProjectProgressAsync(projectId, now, cancellationToken);
        return (await LoadProjectTasksAsync(projectId, cancellationToken))
            .Single(item => item.Id == task.Id.ToString("D"));
    }

    public async Task<ProjectTaskDto> StageCreateTaskWithinTransactionAsync(
        UserPrincipal user,
        Guid projectId,
        CreateProjectTaskRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("project-task", AccessAction.Create);
        var project = await LoadManagedProjectAsync(user, projectId, traceId, cancellationToken);
        await EnsureProjectMemberAsync(projectId, request.AssigneeId, cancellationToken);
        var priority = ParseTaskPriority(request.Priority);
        var now = clock.UtcNow;
        var dueAt = request.DueAt.ToUniversalTime();
        if (dueAt <= now)
        {
            throw new ValidationException("项目任务截止时间必须晚于当前时间。");
        }

        var task = new ProjectTaskEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            AssigneeId = request.AssigneeId,
            DepartmentId = project.DepartmentId,
            Title = DomainChecks.Required(request.Title, "项目任务标题", 255),
            Content = DomainChecks.Required(request.Content, "项目任务内容", 4000),
            DueAt = dueAt,
            Priority = priority,
            Status = nameof(ProjectTaskStatus.Pending),
            Version = 1,
            EnvironmentCode = project.EnvironmentCode,
            IsTestData = project.IsTestData,
            CreatedBy = user.UserId,
            CreatedAt = now
        };
        dbContext.ProjectTasks.Add(task);
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "PMO",
                "project.task_created",
                "Project",
                projectId.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                after: JsonSerializer.Serialize(new
                {
                    task.Id,
                    task.Title,
                    task.AssigneeId,
                    task.DueAt,
                    task.Priority,
                    task.Status,
                    Source = "Scenario"
                }, JsonSerializerOptions.Web)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await RecalculateProjectProgressAsync(projectId, now, cancellationToken);
        return (await LoadProjectTasksAsync(projectId, cancellationToken))
            .Single(item => item.Id == task.Id.ToString("D"));
    }

    public async Task<ProjectRelationDto> StageAddRelationWithinTransactionAsync(
        UserPrincipal user,
        Guid projectId,
        CreateProjectRelationRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("project", AccessAction.Update);
        var project = await LoadManagedProjectAsync(user, projectId, traceId, cancellationToken);
        var entityType = NormalizeRelationEntityType(request.EntityType);
        await EnsureRelationTargetAsync(user, project, entityType, request.EntityId, cancellationToken);
        var relationCode = DomainChecks.RequiredIdentifier(request.RelationCode, "关系编码", 64);
        var existing = await dbContext.ProjectRelations.AsNoTracking().SingleOrDefaultAsync(
            item => item.ProjectId == projectId
                && item.EntityType == entityType
                && item.EntityId == request.EntityId
                && item.RelationCode == relationCode,
            cancellationToken);
        if (existing is not null)
        {
            return MapRelation(existing);
        }

        var now = clock.UtcNow;
        var relation = new ProjectRelationEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            EntityType = entityType,
            EntityId = request.EntityId,
            RelationCode = relationCode,
            EvidenceExcerpt = string.IsNullOrWhiteSpace(request.EvidenceExcerpt)
                ? null
                : DomainChecks.Required(request.EvidenceExcerpt, "证据摘要", 4000),
            CreatedBy = user.UserId,
            CreatedAt = now
        };
        dbContext.ProjectRelations.Add(relation);
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "PMO",
                "project.relation_added",
                "Project",
                projectId.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                after: JsonSerializer.Serialize(new
                {
                    relation.EntityType,
                    relation.EntityId,
                    relation.RelationCode,
                    Source = "Scenario"
                }, JsonSerializerOptions.Web)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapRelation(relation);
    }

    public async Task<ProjectTaskDto> CompleteTaskAsync(
        UserPrincipal user,
        Guid taskId,
        CompleteProjectTaskRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("project-task", AccessAction.Update);
        var task = await dbContext.ProjectTasks.SingleOrDefaultAsync(
            item => item.Id == taskId,
            cancellationToken) ?? throw new NotFoundException("项目任务不存在。");
        var project = await LoadVisibleProjectAsync(
            user,
            task.ProjectId,
            false,
            traceId,
            cancellationToken) ?? throw new PermissionException(ErrorCodes.ProjectScopeDenied, "当前身份不能访问该项目任务。");
        var canManage = await CanManageProjectAsync(user, project, cancellationToken);
        if (task.AssigneeId != user.UserId && !canManage)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "只有任务负责人或项目经理可以完成任务。");
        }

        if (request.ExpectedVersion != task.Version)
        {
            throw new ConflictException(ErrorCodes.Conflict, "项目任务版本已变化，请刷新后重试。");
        }

        if (task.Status is nameof(ProjectTaskStatus.Completed) or nameof(ProjectTaskStatus.Cancelled))
        {
            return (await LoadProjectTasksAsync(task.ProjectId, cancellationToken))
                .Single(item => item.Id == taskId.ToString("D"));
        }

        var now = clock.UtcNow;
        task.Status = nameof(ProjectTaskStatus.Completed);
        task.CompletedAt = now;
        task.Version++;
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "PMO",
                "project.task_completed",
                "Project",
                task.ProjectId.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                before: task.Status,
                after: nameof(ProjectTaskStatus.Completed),
                reason: request.Comment),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await RecalculateProjectProgressAsync(task.ProjectId, now, cancellationToken);
        return (await LoadProjectTasksAsync(task.ProjectId, cancellationToken))
            .Single(item => item.Id == taskId.ToString("D"));
    }

    internal async Task<BusinessObjectEntity> LoadManagedProjectAsync(
        UserPrincipal user,
        Guid projectId,
        string traceId,
        CancellationToken cancellationToken)
    {
        var project = await LoadVisibleProjectAsync(
            user,
            projectId,
            false,
            traceId,
            cancellationToken) ?? throw new PermissionException(ErrorCodes.ProjectScopeDenied, "当前身份不能访问该项目。");
        if (!await CanManageProjectAsync(user, project, cancellationToken))
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "当前身份只能管理自己负责或授权部门内的项目。");
        }

        return project;
    }

    private async Task<BusinessObjectEntity> LoadTrackedManagedProjectAsync(
        UserPrincipal user,
        Guid projectId,
        string traceId,
        CancellationToken cancellationToken)
    {
        _ = await LoadManagedProjectAsync(
            user,
            projectId,
            traceId,
            cancellationToken);
        return await dbContext.BusinessObjects
            .SingleAsync(item => item.Id == projectId, cancellationToken);
    }

    internal async Task<BusinessObjectEntity?> LoadVisibleProjectAsync(
        UserPrincipal user,
        Guid projectId,
        bool includeTestData,
        string traceId,
        CancellationToken cancellationToken)
    {
        var project = await LoadProjectEntityAsync(
            projectId,
            includeTestData && user.IsPlatformAdmin,
            cancellationToken);
        if (project is null || project.OrganizationId != user.OrganizationId)
        {
            return null;
        }

        if (user.IsPlatformAdmin
            || await IsProjectMemberAsync(projectId, user.UserId, cancellationToken)
            || await IsDepartmentManagerAsync(user.UserId, project.DepartmentId, cancellationToken))
        {
            return project;
        }

        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "PMO",
                "project.access_denied",
                "Project",
                projectId.ToString("D"),
                traceId,
                AuditOutcome.Denied,
                clock.UtcNow,
                before: project.ObjectCode),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        throw new PermissionException(ErrorCodes.ProjectScopeDenied, "非项目成员不能通过直接 ID 读取项目。");
    }

    private IQueryable<BusinessObjectEntity> ApplyProjectScope(
        UserPrincipal user,
        bool includeTestData,
        IReadOnlySet<Guid> accessibleProjectIds)
    {
        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var includeAllEnvironments = user.IsPlatformAdmin && includeTestData;
        return from item in dbContext.BusinessObjects.AsNoTracking()
               join objectType in dbContext.ObjectTypes.AsNoTracking()
                   on item.ObjectTypeId equals objectType.Id
               where objectType.Code == ProjectObjectTypeCode
                   && !item.IsDeleted
                   && item.OrganizationId == user.OrganizationId
                   && (user.IsPlatformAdmin || accessibleProjectIds.Contains(item.Id))
                   && (includeAllEnvironments || item.EnvironmentCode == environment)
               select item;
    }

    private async Task<IReadOnlySet<Guid>> LoadAccessibleProjectIdsAsync(
        UserPrincipal user,
        CancellationToken cancellationToken)
    {
        if (user.IsPlatformAdmin)
        {
            return await dbContext.BusinessObjects.AsNoTracking()
                .Where(item => item.OrganizationId == user.OrganizationId && !item.IsDeleted)
                .Select(item => item.Id)
                .ToHashSetAsync(cancellationToken);
        }

        var memberIds = await dbContext.ProjectMembers.AsNoTracking()
            .Where(item => item.UserId == user.UserId && item.IsActive)
            .Select(item => item.BusinessObjectId)
            .ToListAsync(cancellationToken);
        var managedDepartmentIds = await dbContext.UserDepartmentMemberships.AsNoTracking()
            .Where(item => item.UserId == user.UserId && item.IsActive && item.IsManager)
            .Select(item => item.DepartmentId)
            .ToListAsync(cancellationToken);
        var departmentProjectIds = await dbContext.BusinessObjects.AsNoTracking()
            .Where(item => managedDepartmentIds.Contains(item.DepartmentId) && !item.IsDeleted)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        var ownerIds = await dbContext.ProjectProfiles.AsNoTracking()
            .Where(item => item.OwnerUserId == user.UserId)
            .Select(item => item.BusinessObjectId)
            .ToListAsync(cancellationToken);
        return memberIds.Concat(departmentProjectIds).Concat(ownerIds).ToHashSet();
    }

    private async Task<List<ProjectDto>> MapProjectSummariesAsync(
        UserPrincipal user,
        List<BusinessObjectEntity> projects,
        CancellationToken cancellationToken)
    {
        if (projects.Count == 0)
        {
            return [];
        }

        var projectIds = projects.Select(item => item.Id).ToArray();
        var profiles = await dbContext.ProjectProfiles.AsNoTracking()
            .Where(item => projectIds.Contains(item.BusinessObjectId))
            .ToDictionaryAsync(item => item.BusinessObjectId, cancellationToken);
        var ownerIds = profiles.Values.Select(item => item.OwnerUserId)
            .Concat(projects.Where(item => item.CreatedByUserId.HasValue)
                .Select(item => item.CreatedByUserId!.Value))
            .Distinct()
            .ToArray();
        var users = await dbContext.Users.AsNoTracking()
            .Where(item => ownerIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.DisplayName, cancellationToken);
        var departments = await dbContext.Departments.AsNoTracking()
            .Where(item => projects.Select(project => project.DepartmentId).Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        var members = await dbContext.ProjectMembers.AsNoTracking()
            .Where(item => projectIds.Contains(item.BusinessObjectId) && item.IsActive)
            .ToListAsync(cancellationToken);
        var milestones = await dbContext.ProjectMilestones.AsNoTracking()
            .Where(item => projectIds.Contains(item.ProjectId))
            .ToListAsync(cancellationToken);
        var tasks = await dbContext.ProjectTasks.AsNoTracking()
            .Where(item => projectIds.Contains(item.ProjectId))
            .ToListAsync(cancellationToken);
        var now = clock.UtcNow;
        return projects.Select(project =>
        {
            var profile = profiles.GetValueOrDefault(project.Id);
            var ownerUserId = profile?.OwnerUserId ?? project.CreatedByUserId ?? Guid.Empty;
            var projectMilestones = milestones.Where(item => item.ProjectId == project.Id).ToArray();
            var projectTasks = tasks.Where(item => item.ProjectId == project.Id).ToArray();
            var overdueTasks = projectTasks.Count(item =>
                item.DueAt.HasValue
                && item.DueAt.Value < now
                && item.Status is nameof(ProjectTaskStatus.Pending) or nameof(ProjectTaskStatus.InProgress));
            return new ProjectDto(
                project.Id.ToString("D"),
                project.ObjectCode,
                project.Title ?? project.ObjectCode,
                project.Status,
                ProjectRules.DisplayStatus(Enum.Parse<BusinessObjectStatus>(project.Status)),
                project.Version,
                ownerUserId.ToString("D"),
                users.GetValueOrDefault(ownerUserId, "未知用户"),
                project.DepartmentId.ToString("D"),
                departments.GetValueOrDefault(project.DepartmentId, "未知部门"),
                project.EnvironmentCode,
                project.IsTestData,
                profile?.PlannedStartDate,
                profile?.PlannedEndDate,
                profile?.Objective ?? string.Empty,
                profile?.CurrentStage ?? string.Empty,
                profile?.ConfidentialityLevel ?? "Internal",
                profile?.ProgressPercent ?? 0,
                members.Count(item => item.BusinessObjectId == project.Id),
                projectMilestones.Length,
                projectMilestones.Count(item => item.Status == nameof(ProjectMilestoneStatus.Completed)),
                projectMilestones.Count(item => ProjectRules.IsOverdue(
                    Enum.Parse<ProjectMilestoneStatus>(item.Status),
                    item.PlannedDate,
                    now)),
                projectTasks.Length,
                projectTasks.Count(item => item.Status == nameof(ProjectTaskStatus.Completed)),
                overdueTasks,
                project.CreatedAt,
                project.UpdatedAt);
        }).ToList();
    }

    private async Task<List<ProjectMemberDto>> LoadMembersAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from member in dbContext.ProjectMembers.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on member.UserId equals user.Id
            join membership in dbContext.UserDepartmentMemberships.AsNoTracking()
                on member.UserId equals membership.UserId
            join department in dbContext.Departments.AsNoTracking()
                on membership.DepartmentId equals department.Id
            where member.BusinessObjectId == projectId
                && membership.IsActive
                && membership.IsPrimary
            orderby member.IsActive descending, member.IsLead descending, user.DisplayName
            select new
            {
                Member = member,
                User = user,
                Department = department
            }).ToListAsync(cancellationToken);
        return rows.Select(item => new ProjectMemberDto(
            item.Member.BusinessObjectId.ToString("D"),
            item.Member.UserId.ToString("D"),
            item.User.DisplayName,
            item.User.UserName,
            item.Department.Id.ToString("D"),
            item.Department.Name,
            item.Member.ProjectRole,
            item.Member.IsLead,
            item.Member.IsActive,
            item.Member.CreatedAt,
            item.Member.RemovedAt)).ToList();
    }

    private async Task<List<ProjectMilestoneDto>> LoadMilestonesAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from milestone in dbContext.ProjectMilestones.AsNoTracking()
            join owner in dbContext.Users.AsNoTracking() on milestone.OwnerUserId equals owner.Id
            where milestone.ProjectId == projectId
            orderby milestone.PlannedDate, milestone.Id
            select new { Milestone = milestone, Owner = owner }).ToListAsync(cancellationToken);
        var now = clock.UtcNow;
        return rows.Select(item => new ProjectMilestoneDto(
            item.Milestone.Id.ToString("D"),
            item.Milestone.ProjectId.ToString("D"),
            item.Milestone.Title,
            item.Milestone.OwnerUserId.ToString("D"),
            item.Owner.DisplayName,
            item.Milestone.PlannedDate,
            item.Milestone.Status,
            ProjectRules.IsOverdue(
                Enum.Parse<ProjectMilestoneStatus>(item.Milestone.Status),
                item.Milestone.PlannedDate,
                now),
            item.Milestone.CompletedAt,
            item.Milestone.Version,
            item.Milestone.CreatedAt,
            item.Milestone.UpdatedAt)).ToList();
    }

    internal async Task<List<ProjectTaskDto>> LoadProjectTasksAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from task in dbContext.ProjectTasks.AsNoTracking()
            join assignee in dbContext.Users.AsNoTracking() on task.AssigneeId equals assignee.Id
            where task.ProjectId == projectId
            orderby task.Status, task.DueAt, task.Id
            select new { Task = task, Assignee = assignee }).ToListAsync(cancellationToken);
        return rows.Select(item => MapProjectTask(item.Task, item.Assignee.DisplayName)).ToList();
    }

    internal async Task<ProjectTaskEntity> LoadProjectTaskEntityAsync(
        Guid taskId,
        CancellationToken cancellationToken) =>
        await dbContext.ProjectTasks.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == taskId, cancellationToken)
        ?? throw new NotFoundException("项目任务不存在。");

    internal static ProjectTaskDto MapTaskWithAssignee(ProjectTaskEntity task, string assigneeName) =>
        MapProjectTask(task, assigneeName);

    private async Task<List<BusinessObjectDto>> LoadBusinessObjectDtosAsync(
        UserPrincipal user,
        Guid[] ids,
        CancellationToken cancellationToken)
    {
        if (ids.Length == 0)
        {
            return [];
        }

        var departmentIds = user.DepartmentIds;
        var entities = await dbContext.BusinessObjects.AsNoTracking()
            .Where(item => ids.Contains(item.Id)
                && item.OrganizationId == user.OrganizationId
                && !item.IsDeleted
                && (user.IsPlatformAdmin || departmentIds.Contains(item.DepartmentId)))
            .ToListAsync(cancellationToken);
        var objectTypeIds = entities.Select(item => item.ObjectTypeId).Distinct().ToArray();
        var coreFields = await dbContext.FieldDefinitions.AsNoTracking()
            .Where(item => objectTypeIds.Contains(item.ObjectTypeId) && item.Sensitivity == "Core")
            .Select(item => new { item.ObjectTypeId, item.Key })
            .ToListAsync(cancellationToken);
        var coreKeysByType = coreFields
            .GroupBy(item => item.ObjectTypeId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.Key).ToHashSet(StringComparer.OrdinalIgnoreCase));
        return entities.Select(item =>
        {
            var fields = JsonSerializer.Deserialize<Dictionary<string, string?>>(item.FieldsJson)
                ?? new Dictionary<string, string?>();
            if (!user.IsPlatformAdmin)
            {
                foreach (var key in coreKeysByType.GetValueOrDefault(item.ObjectTypeId) ?? [])
                {
                    if (fields.ContainsKey(key))
                    {
                        fields[key] = "***";
                    }
                }
            }

            return new BusinessObjectDto(
                item.Id.ToString("D"),
                item.ObjectTypeId.ToString("D"),
                item.ObjectCode,
                item.Status,
                item.Version,
                fields,
                item.CreatedAt,
                item.UpdatedAt,
                item.DepartmentId.ToString("D"),
                item.EnvironmentCode,
                item.IsTestData,
                item.SourceSystem,
                item.Title,
                item.CreatedByUserId?.ToString("D"));
        }).ToList();
    }

    private async Task<List<ProjectWorkflowDto>> LoadProjectWorkflowsAsync(
        Guid[] workflowIds,
        CancellationToken cancellationToken)
    {
        if (workflowIds.Length == 0)
        {
            return [];
        }

        var instances = await dbContext.WorkflowInstances.AsNoTracking()
            .Where(item => workflowIds.Contains(item.Id))
            .ToListAsync(cancellationToken);
        var definitions = await dbContext.WorkflowDefinitions.AsNoTracking()
            .ToDictionaryAsync(item => item.Id, item => item, cancellationToken);
        var tasks = await dbContext.WorkflowTasks.AsNoTracking()
            .Where(item => workflowIds.Contains(item.WorkflowInstanceId))
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        var now = clock.UtcNow;
        return instances.Select(instance =>
        {
            var definition = definitions.GetValueOrDefault(instance.WorkflowDefinitionId);
            var instanceTasks = tasks.Where(item => item.WorkflowInstanceId == instance.Id).ToList();
            var pending = instanceTasks.Where(item => item.Status == nameof(WorkflowTaskStatus.Pending)).ToList();
            var slaRisk = pending.Any(item => item.DueAt.HasValue && item.DueAt.Value < now)
                ? "Overdue"
                : pending.Any(item => item.DueAt.HasValue && item.DueAt.Value <= now.AddHours(24))
                    ? "AtRisk"
                    : "Normal";
            return new ProjectWorkflowDto(
                MapWorkflowInstance(instance),
                definition?.Code ?? string.Empty,
                definition?.Name ?? string.Empty,
                instanceTasks.Select(MapWorkflowTask).ToList(),
                slaRisk);
        }).ToList();
    }

    private async Task<List<FileAttachmentDto>> LoadFilesAsync(
        Guid[] ids,
        CancellationToken cancellationToken)
    {
        if (ids.Length == 0)
        {
            return [];
        }

        var rows = await dbContext.Attachments.AsNoTracking()
            .Where(item => ids.Contains(item.Id))
            .OrderByDescending(item => item.UpdatedAt)
            .ToListAsync(cancellationToken);
        return rows.Select(item => new FileAttachmentDto(
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
            FilePreviewKind(item.MimeType),
            [],
            item.CreatedAt,
            item.UpdatedAt,
            item.DeletionRequestedBy,
            item.DeletionRequestedAt,
            item.DeletionReason)).ToList();
    }

    private async Task<List<MinutesSourceDto>> LoadMinutesAsync(
        Guid[] ids,
        CancellationToken cancellationToken)
    {
        if (ids.Length == 0)
        {
            return [];
        }

        var rows = await dbContext.MinutesSources.AsNoTracking()
            .Where(item => ids.Contains(item.Id))
            .OrderByDescending(item => item.MeetingAt)
            .ToListAsync(cancellationToken);
        return rows.Select(item => new MinutesSourceDto(
            item.Id.ToString("D"),
            item.DepartmentId.ToString("D"),
            item.AttachmentId?.ToString("D"),
            item.Title,
            item.MeetingAt,
            item.SourceType,
            item.Transcript,
            item.EnvironmentCode,
            item.IsTestData,
            item.Status,
            item.AnalysisVersion,
            item.AnalysisRunId?.ToString("D"),
            item.CreatedAt)).ToList();
    }

    private async Task<List<AuditEventDto>> LoadProjectAuditAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var projectIdText = projectId.ToString("D");
        var relatedTaskIds = await dbContext.ProjectTasks.AsNoTracking()
            .Where(item => item.ProjectId == projectId)
            .Select(item => item.Id.ToString())
            .ToListAsync(cancellationToken);
        var relatedMilestoneIds = await dbContext.ProjectMilestones.AsNoTracking()
            .Where(item => item.ProjectId == projectId)
            .Select(item => item.Id.ToString())
            .ToListAsync(cancellationToken);
        var relatedEventIds = await dbContext.EventInboxes.AsNoTracking()
            .Where(item => dbContext.ProjectTasks.Any(task =>
                task.ProjectId == projectId && task.EventInboxId == item.Id))
            .Select(item => item.Id.ToString())
            .ToListAsync(cancellationToken);
        var ids = relatedTaskIds.Concat(relatedMilestoneIds).Concat(relatedEventIds)
            .Append(projectIdText)
            .ToArray();
        var rows = await dbContext.AuditEvents.AsNoTracking()
            .Where(item => ids.Contains(item.EntityId)
                || (item.EntityType == "Project" && item.EntityId == projectIdText))
            .OrderByDescending(item => item.OccurredAt)
            .Take(200)
            .ToListAsync(cancellationToken);
        return rows.Select(item => new AuditEventDto(
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
    }

    private async Task<BusinessObjectEntity?> LoadProjectEntityAsync(
        Guid projectId,
        bool includeAllEnvironments,
        CancellationToken cancellationToken)
    {
        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        return await (
            from item in dbContext.BusinessObjects.AsNoTracking()
            join objectType in dbContext.ObjectTypes.AsNoTracking()
                on item.ObjectTypeId equals objectType.Id
            where item.Id == projectId
                && item.ObjectCode != string.Empty
                && objectType.Code == ProjectObjectTypeCode
                && !item.IsDeleted
                && (includeAllEnvironments || item.EnvironmentCode == environment)
            select item).SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<bool> CanManageProjectAsync(
        UserPrincipal user,
        BusinessObjectEntity project,
        CancellationToken cancellationToken)
    {
        if (user.IsPlatformAdmin)
        {
            return true;
        }

        if (!user.HasPermission("project", AccessAction.Update)
            && !user.HasPermission("project-member", AccessAction.Manage)
            && !user.HasPermission("milestone", AccessAction.Manage))
        {
            return false;
        }

        var profile = await dbContext.ProjectProfiles.AsNoTracking()
            .SingleOrDefaultAsync(item => item.BusinessObjectId == project.Id, cancellationToken);
        if (profile?.OwnerUserId == user.UserId)
        {
            return true;
        }

        return user.DepartmentIds.Contains(project.DepartmentId)
            && await IsDepartmentManagerAsync(user.UserId, project.DepartmentId, cancellationToken);
    }

    private Task<bool> IsProjectMemberAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.ProjectMembers.AnyAsync(
            item => item.BusinessObjectId == projectId && item.UserId == userId && item.IsActive,
            cancellationToken);

    private Task<bool> IsDepartmentManagerAsync(
        Guid userId,
        Guid departmentId,
        CancellationToken cancellationToken) =>
        dbContext.UserDepartmentMemberships.AnyAsync(
            item => item.UserId == userId
                && item.DepartmentId == departmentId
                && item.IsActive
                && item.IsManager,
            cancellationToken);

    private async Task EnsureProjectMemberAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (!await IsProjectMemberAsync(projectId, userId, cancellationToken))
        {
            throw new ValidationException("里程碑负责人必须先是项目成员。");
        }
    }

    private async Task UpsertLeadMemberAsync(
        Guid projectId,
        Guid ownerUserId,
        Guid actorId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await foreach (var member in dbContext.ProjectMembers
                           .Where(item => item.BusinessObjectId == projectId && item.IsLead && item.UserId != ownerUserId)
                           .AsAsyncEnumerable()
                           .WithCancellation(cancellationToken))
        {
            member.IsLead = false;
            member.ProjectRole = nameof(ProjectMemberRole.Manager);
        }

        var owner = await dbContext.ProjectMembers.SingleOrDefaultAsync(
            item => item.BusinessObjectId == projectId && item.UserId == ownerUserId,
            cancellationToken);
        if (owner is null)
        {
            owner = new ProjectMemberEntity
            {
                BusinessObjectId = projectId,
                UserId = ownerUserId,
                CreatedBy = actorId,
                CreatedAt = now
            };
            dbContext.ProjectMembers.Add(owner);
        }

        owner.IsActive = true;
        owner.IsLead = true;
        owner.ProjectRole = nameof(ProjectMemberRole.Lead);
        owner.RemovedAt = null;
        owner.RemovedBy = null;
    }

    internal async Task RecalculateProjectProgressAsync(
        Guid projectId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.ProjectProfiles.SingleOrDefaultAsync(
            item => item.BusinessObjectId == projectId,
            cancellationToken);
        if (profile is null)
        {
            return;
        }

        var milestones = await dbContext.ProjectMilestones.AsNoTracking()
            .Where(item => item.ProjectId == projectId && item.Status != nameof(ProjectMilestoneStatus.Cancelled))
            .Select(item => item.Status)
            .ToListAsync(cancellationToken);
        var tasks = await dbContext.ProjectTasks.AsNoTracking()
            .Where(item => item.ProjectId == projectId && item.Status != nameof(ProjectTaskStatus.Cancelled))
            .Select(item => item.Status)
            .ToListAsync(cancellationToken);
        var milestoneProgress = ProjectRules.Percent(
            milestones.Count(status => status == nameof(ProjectMilestoneStatus.Completed)),
            milestones.Count);
        var taskProgress = ProjectRules.Percent(
            tasks.Count(status => status == nameof(ProjectTaskStatus.Completed)),
            tasks.Count);
        profile.ProgressPercent = milestones.Count > 0 && tasks.Count > 0
            ? (int)Math.Round((milestoneProgress + taskProgress) / 2m, MidpointRounding.AwayFromZero)
            : milestones.Count > 0
                ? milestoneProgress
                : taskProgress;
        profile.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    internal async Task<bool> EnsureAttachmentEvidenceRelationAsync(
        Guid projectId,
        Guid attachmentId,
        Guid actorId,
        string traceId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var isProject = await (
            from project in dbContext.BusinessObjects.AsNoTracking()
            join objectType in dbContext.ObjectTypes.AsNoTracking()
                on project.ObjectTypeId equals objectType.Id
            where project.Id == projectId
                && objectType.Code == ProjectObjectTypeCode
                && !project.IsDeleted
            select project.Id).AnyAsync(cancellationToken);
        if (!isProject)
        {
            return false;
        }

        var relationExists = dbContext.ProjectRelations.Local.Any(item =>
                item.ProjectId == projectId
                && item.EntityType == "Attachment"
                && item.EntityId == attachmentId
                && item.RelationCode == "SupportingEvidence")
            || await dbContext.ProjectRelations.AsNoTracking().AnyAsync(
                item => item.ProjectId == projectId
                    && item.EntityType == "Attachment"
                    && item.EntityId == attachmentId
                    && item.RelationCode == "SupportingEvidence",
                cancellationToken);
        if (relationExists)
        {
            return false;
        }

        dbContext.ProjectRelations.Add(new ProjectRelationEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            EntityType = "Attachment",
            EntityId = attachmentId,
            RelationCode = "SupportingEvidence",
            CreatedBy = actorId,
            CreatedAt = now
        });
        await auditRepository.AddAsync(
            AuditEvent.Record(
                actorId,
                "PMO",
                "project.file_evidence_linked",
                "Project",
                projectId.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                after: JsonSerializer.Serialize(
                    new { AttachmentId = attachmentId, RelationCode = "SupportingEvidence" },
                    JsonSerializerOptions.Web)),
            cancellationToken);
        return true;
    }

    internal static string MapProjectRoleToPermissionAction(ProjectMemberRole role) =>
        role is ProjectMemberRole.Lead or ProjectMemberRole.Manager
            ? nameof(AccessAction.Update)
            : nameof(AccessAction.Read);

    internal static ProjectMemberRole ParseMemberRole(string role) =>
        Enum.TryParse<ProjectMemberRole>(role, true, out var parsed)
            ? parsed
            : throw new ValidationException("项目成员角色必须是 Lead、Manager、Contributor 或 Viewer。");

    internal static ProjectMilestoneStatus ParseMilestoneStatus(string status) =>
        Enum.TryParse<ProjectMilestoneStatus>(status, true, out var parsed)
            ? parsed
            : throw new ValidationException("里程碑状态无效。");

    internal static string ParseTaskPriority(string priority)
    {
        var normalized = DomainChecks.Required(priority, "项目任务优先级", 8).ToUpperInvariant();
        return normalized is "P0" or "P1" or "P2"
            ? normalized
            : throw new ValidationException("项目任务优先级必须是 P0、P1 或 P2。");
    }

    internal static BusinessObjectStatus ParseProjectStatus(string status) =>
        Enum.TryParse<BusinessObjectStatus>(status, true, out var parsed)
            ? parsed
            : throw new ValidationException("项目状态无效。");

    internal async Task<Guid> ValidateActiveUserAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Users.AsNoTracking().AnyAsync(
            item => item.Id == userId
                && item.OrganizationId == organizationId
                && item.IsActive
                && !item.IsServiceIdentity,
            cancellationToken);
        return exists ? userId : throw new ValidationException("项目成员必须是当前组织的有效真人用户。");
    }

    internal Task<bool> ValidateDepartmentAsync(
        Guid organizationId,
        Guid departmentId,
        CancellationToken cancellationToken) =>
        dbContext.Departments.AsNoTracking().AnyAsync(
            item => item.Id == departmentId && item.OrganizationId == organizationId && item.IsActive,
            cancellationToken);

    internal static ProjectTaskDto MapProjectTask(ProjectTaskEntity task, string assigneeName) =>
        new(
            task.Id.ToString("D"),
            task.ProjectId.ToString("D"),
            task.EventInboxId?.ToString("D"),
            task.AssigneeId.ToString("D"),
            assigneeName,
            task.DepartmentId.ToString("D"),
            task.Title,
            task.Content,
            task.DueAt,
            task.Priority,
            task.Status,
            task.Version,
            task.BusinessObjectId?.ToString("D"),
            task.WorkflowInstanceId?.ToString("D"),
            task.WorkflowTaskId?.ToString("D"),
            task.EnvironmentCode,
            task.IsTestData,
            task.CreatedAt,
            task.CompletedAt);

    internal static ProjectRelationDto MapRelation(ProjectRelationEntity relation) =>
        new(
            relation.Id.ToString("D"),
            relation.ProjectId.ToString("D"),
            relation.EntityType,
            relation.EntityId.ToString("D"),
            relation.RelationCode,
            relation.EvidenceExcerpt,
            relation.CreatedAt);

    internal static WorkflowInstanceDto MapWorkflowInstance(WorkflowInstanceEntity instance) =>
        new(
            instance.Id.ToString("D"),
            instance.WorkflowDefinitionId.ToString("D"),
            instance.WorkflowVersionId.ToString("D"),
            instance.BusinessObjectId.ToString("D"),
            Enum.Parse<WorkflowInstanceStatus>(instance.Status),
            instance.Version,
            instance.CurrentNodeCode,
            instance.StartedAt,
            instance.EndedAt,
            instance.DepartmentId.ToString("D"),
            instance.StartedByUserId?.ToString("D"),
            instance.EnvironmentCode,
            instance.IsTestData,
            instance.SourceSystem);

    private static WorkflowTaskDto MapWorkflowTask(WorkflowTaskEntity task) =>
        new(
            task.Id.ToString("D"),
            task.WorkflowInstanceId.ToString("D"),
            task.NodeCode,
            task.AssigneeId.ToString("D"),
            Enum.Parse<WorkflowTaskStatus>(task.Status),
            task.Version,
            task.DecisionComment,
            task.CreatedAt,
            task.CompletedAt,
            task.DepartmentId.ToString("D"),
            task.EnvironmentCode,
            task.IsTestData,
            task.DueAt,
            task.SlaStatus,
            task.ReminderSentAt,
            task.EscalatedAt,
            task.DecisionMode,
            task.AgentRunId?.ToString("D"));

    private async Task EnsureRelationTargetAsync(
        UserPrincipal user,
        BusinessObjectEntity project,
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        var exists = entityType switch
        {
            "BusinessObject" => await dbContext.BusinessObjects.AnyAsync(
                item => item.Id == entityId
                    && item.OrganizationId == user.OrganizationId
                    && !item.IsDeleted
                    && (user.IsPlatformAdmin || user.DepartmentIds.Contains(item.DepartmentId)),
                cancellationToken),
            "WorkflowInstance" => await dbContext.WorkflowInstances.AnyAsync(
                item => item.Id == entityId
                    && item.DepartmentId == project.DepartmentId,
                cancellationToken),
            "WorkflowTask" => await dbContext.WorkflowTasks.AnyAsync(
                item => item.Id == entityId
                    && item.DepartmentId == project.DepartmentId,
                cancellationToken),
            "WorkflowNotification" => await dbContext.WorkflowNotifications.AnyAsync(
                item => item.Id == entityId
                    && item.OrganizationId == user.OrganizationId,
                cancellationToken),
            "MinutesSource" => await dbContext.MinutesSources.AnyAsync(
                item => item.Id == entityId
                    && item.OrganizationId == user.OrganizationId
                    && (user.IsPlatformAdmin || user.DepartmentIds.Contains(item.DepartmentId)),
                cancellationToken),
            "MinutesActionItem" => await dbContext.MinutesActionItems.AnyAsync(
                item => item.Id == entityId,
                cancellationToken),
            "Attachment" => await dbContext.Attachments.AnyAsync(
                item => item.Id == entityId
                    && item.OrganizationId == user.OrganizationId
                    && (user.IsPlatformAdmin || user.DepartmentIds.Contains(item.DepartmentId)),
                cancellationToken),
            "ProjectMilestone" => await dbContext.ProjectMilestones.AnyAsync(
                item => item.Id == entityId && item.ProjectId == project.Id,
                cancellationToken),
            "AuditEvent" => await dbContext.AuditEvents.AnyAsync(
                item => item.Id == entityId,
                cancellationToken),
            "InboxEvent" => await dbContext.EventInboxes.AnyAsync(
                item => item.Id == entityId
                    && item.OrganizationId == user.OrganizationId
                    && (user.IsPlatformAdmin || user.DepartmentIds.Contains(item.DepartmentId)),
                cancellationToken),
            _ => false
        };
        if (!exists)
        {
            throw new NotFoundException($"关联对象 {entityType}/{entityId:D} 不存在或不在授权范围。");
        }
    }

    private static string NormalizeRelationEntityType(string entityType)
    {
        var allowed = new HashSet<string>(
            [
                "BusinessObject",
                "WorkflowInstance",
                "WorkflowTask",
                "WorkflowNotification",
                "MinutesSource",
                "MinutesActionItem",
                "Attachment",
                "ProjectMilestone",
                "AuditEvent",
                "InboxEvent"
            ],
            StringComparer.Ordinal);
        var normalized = DomainChecks.RequiredIdentifier(entityType, "关联实体类型", 64);
        return allowed.Contains(normalized)
            ? normalized
            : throw new ValidationException("不支持该项目关联实体类型。");
    }

    private static string NormalizeConfidentiality(string value)
    {
        var normalized = DomainChecks.Required(value, "保密等级", 16);
        var canonical = normalized switch
        {
            "Internal" or "内部" => "Internal",
            "Sensitive" or "敏感" => "Sensitive",
            "Core" or "核心" => "Core",
            _ => throw new ValidationException("保密等级必须是 Internal、Sensitive 或 Core。")
        };
        return canonical;
    }

    private static void ValidateDateRange(DateTimeOffset? start, DateTimeOffset? end)
    {
        if (start.HasValue && end.HasValue && end.Value < start.Value)
        {
            throw new ValidationException("计划结束日期不能早于开始日期。");
        }
    }

    private static void ValidateIdempotencyKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length is < 8 or > 128)
        {
            throw new ValidationException("写操作必须提供 8—128 位幂等键。");
        }
    }

    private static string Hash<T>(T request) =>
        Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request, JsonSerializerOptions.Web))));

    private static BusinessObjectVersionEntity CreateProjectVersion(
        BusinessObjectEntity entity,
        Guid actorId,
        string reason) =>
        new()
        {
            BusinessObjectId = entity.Id,
            Version = entity.Version,
            SnapshotJson = JsonSerializer.Serialize(new
            {
                entity.ObjectCode,
                entity.Title,
                entity.ObjectTypeId,
                entity.OrganizationId,
                entity.DepartmentId,
                entity.Status,
                entity.Version,
                entity.EnvironmentCode,
                entity.IsTestData,
                entity.SourceSystem
            }, JsonSerializerOptions.Web),
            ActorId = actorId,
            Reason = reason,
            CreatedAt = entity.UpdatedAt
        };

    private static string FilePreviewKind(string mimeType)
    {
        if (mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return "image";
        }

        if (mimeType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return "pdf";
        }

        return mimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
            || mimeType.Contains("json", StringComparison.OrdinalIgnoreCase)
            ? "text"
            : "unsupported";
    }
}
