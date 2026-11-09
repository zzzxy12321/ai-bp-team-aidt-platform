using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Audit;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;
using AIDT.Domain.Workflows;
using AIDT.Infrastructure.Persistence;
using AIDT.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Services;

public sealed partial class SqlWorkflowConfigurationService(
    AidtDbContext dbContext,
    IAuditRepository auditRepository,
    IIdempotencyStore idempotencyStore,
    IUnitOfWork unitOfWork,
    IClock clock,
    IWorkflowAssignmentResolver assignmentResolver,
    DataEnvironmentOptions environmentOptions) : IWorkflowConfigurationService
{
    private const string WorkflowConfigurationType = "Workflow";
    private static readonly HashSet<string> SupportedOperators =
        new(["equals", "notEquals", "greaterThan", "greaterThanOrEqual"], StringComparer.Ordinal);
    private static readonly HashSet<string> SupportedFieldTypes =
        new(
            [
                "Text", "LongText", "Number", "Money", "Percent", "Select",
                "MultiSelect", "User", "MultiUser", "Department", "Date",
                "DateTime", "DateRange", "Attachment", "Boolean",
                "ReadOnlySystem", "BusinessObjectField"
            ],
            StringComparer.Ordinal);
    private static readonly HashSet<string> SupportedFieldSources =
        new(["TaskInput", "BusinessObjectReference", "EvidenceReference"], StringComparer.Ordinal);
    private static readonly HashSet<string> SupportedSensitivities =
        new(["Public", "Internal", "Sensitive", "Core"], StringComparer.Ordinal);
    private static readonly HashSet<string> SupportedAiActions =
        new(
            ["CompletenessCheck", "RiskIdentify", "GenerateReviewOpinion", "GenerateDraft"],
            StringComparer.Ordinal);
    private static readonly HashSet<string> SupportedNodeActionTypes =
        new(
            ["Submit", "Approve", "Return", "Terminate", "UploadEvidence", "RequestReview"],
            StringComparer.OrdinalIgnoreCase);
    private static readonly string[] AutomatedTestPrefixes =
        ["E2E", "TEST", "DEMO", "PILOT", "NAV-E2E", "AGENT-UI", "META_B", "UX003", "UX004", "WFROLE-E2E"];

    public async Task<IReadOnlyList<WorkflowPolicyUserDto>> ListUsersAsync(
        UserPrincipal user,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("configuration", AccessAction.Read);
        user.EnsureAuthenticated();
        return await dbContext.Users.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId
                && item.IsActive
                && !item.IsServiceIdentity)
            .OrderBy(item => item.DisplayName)
            .Select(item => new WorkflowPolicyUserDto(
                item.Id.ToString("D"),
                item.UserName,
                item.DisplayName))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<WorkflowConfigurationDto>> ListAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        string? state,
        string? configurationKey,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("configuration", AccessAction.Read);
        user.EnsureAuthenticated();
        var query = ApplyScope(user)
            .Where(item => item.ConfigurationType == WorkflowConfigurationType);
        if (!string.IsNullOrWhiteSpace(state))
        {
            var normalizedState = DomainChecks.Required(state, "配置状态", 32);
            query = query.Where(item => item.State == normalizedState);
        }

        if (!string.IsNullOrWhiteSpace(configurationKey))
        {
            var normalizedKey = DomainChecks.RequiredCode(
                configurationKey.Trim().ToUpperInvariant(),
                "配置编号",
                128);
            query = query.Where(item => item.ConfigurationKey == normalizedKey);
        }

        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var total = await query.LongCountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(item => item.UpdatedAt)
            .ThenByDescending(item => item.Id)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<WorkflowConfigurationDto>(
            await MapAsync(rows, cancellationToken),
            normalizedPage,
            normalizedPageSize,
            total);
    }

    public async Task<WorkflowConfigurationDto?> GetAsync(
        UserPrincipal user,
        Guid configurationId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("configuration", AccessAction.Read);
        user.EnsureAuthenticated();
        var entity = await ApplyScope(user)
            .SingleOrDefaultAsync(
                item => item.Id == configurationId
                    && item.ConfigurationType == WorkflowConfigurationType,
                cancellationToken);
        return entity is null
            ? null
            : (await MapAsync([entity], cancellationToken)).Single();
    }

    public async Task<WorkflowConfigurationDto> CreateAsync(
        UserPrincipal user,
        CreateWorkflowConfigurationRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("configuration", AccessAction.Create);
        user.EnsureAuthenticated();
        ValidateIdempotencyKey(idempotencyKey);
        var hash = Hash(request);
        var scope = $"configuration:create:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同配置草稿。");
            }

            return await GetRequiredAsync(
                user,
                Guid.Parse(existing.ResponseId),
                cancellationToken);
        }

        var key = DomainChecks.RequiredCode(
            request.ConfigurationKey.Trim().ToUpperInvariant(),
            "配置编号",
            128);
        var name = DomainChecks.Required(request.Name, "配置名称", 200);
        var description = NormalizeOptional(request.Description, "配置说明", 2000);
        await ValidateDepartmentAsync(user, request.DepartmentId, cancellationToken);
        await EnsureDepartmentFlowOwnerAsync(user, request.DepartmentId, cancellationToken);
        if (request.IsDefault && request.DepartmentId == Guid.Empty)
        {
            throw new ValidationException("默认模板必须属于一个业务部门。");
        }
        var applicableRoles = await NormalizeApplicableRolesAsync(
            request.ApplicableRoleCodes,
            cancellationToken);
        var risk = await ClassifyRiskAsync(
            request.DepartmentId,
            request.ObjectTypeCode,
            request.Nodes,
            cancellationToken);
        var payload = CreatePayload(request.ObjectTypeCode, request.StartNode, request.Nodes);
        var canonicalPayload = JsonSerializer.Serialize(payload, JsonSerializerOptions.Web);
        var now = clock.UtcNow;
        var versionNumber = (await dbContext.ConfigurationVersions
            .Where(item => item.ConfigurationKey == key)
            .MaxAsync(item => (int?)item.VersionNumber, cancellationToken) ?? 0) + 1;
        var entity = new ConfigurationVersionEntity
        {
            Id = Guid.NewGuid(),
            ConfigurationKey = key,
            Name = name,
            Description = description,
            ConfigurationType = WorkflowConfigurationType,
            OrganizationId = user.OrganizationId,
            DepartmentId = request.DepartmentId,
            VersionNumber = versionNumber,
            Revision = 0,
            State = "Draft",
            IsDefault = request.IsDefault,
            RiskLevel = risk.RiskLevel,
            RequiresPlatformApproval = risk.RequiresPlatformApproval,
            ApplicableRoleCodesJson = JsonStorage.Serialize(applicableRoles),
            EnvironmentCode = environmentOptions.CurrentEnvironmentCode,
            IsTestData = IsAutomatedTestCode(key),
            PayloadJson = canonicalPayload,
            CreatedBy = user.UserId,
            CreatedAt = now,
            UpdatedBy = user.UserId,
            UpdatedAt = now
        };

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await dbContext.ConfigurationVersions.AddAsync(entity, cancellationToken);
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Configuration",
                    "configuration.draft_created",
                    "ConfigurationVersion",
                    entity.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: JsonSerializer.Serialize(
                        new { entity.ConfigurationKey, entity.VersionNumber, entity.State },
                        JsonSerializerOptions.Web)),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "ConfigurationVersion",
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

        return (await MapAsync([entity], cancellationToken)).Single();
    }

    public async Task<WorkflowConfigurationDto> UpdateAsync(
        UserPrincipal user,
        Guid configurationId,
        UpdateWorkflowConfigurationRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("configuration", AccessAction.Update);
        user.EnsureAuthenticated();
        var entity = await LoadEditableAsync(user, configurationId, cancellationToken);
        EnsureMutable(entity);
        if (request.ExpectedRevision != entity.Revision)
        {
            throw new ConflictException(ErrorCodes.Conflict, "配置版本已变化，请刷新后重试。");
        }

        await ValidateDepartmentAsync(user, request.DepartmentId, cancellationToken);
        var applicableRoles = await NormalizeApplicableRolesAsync(
            request.ApplicableRoleCodes,
            cancellationToken);
        var risk = await ClassifyRiskAsync(
            request.DepartmentId,
            request.ObjectTypeCode,
            request.Nodes,
            cancellationToken);
        var now = clock.UtcNow;
        entity.Name = DomainChecks.Required(request.Name, "配置名称", 200);
        entity.Description = NormalizeOptional(request.Description, "配置说明", 2000);
        entity.DepartmentId = request.DepartmentId;
        entity.IsDefault = request.IsDefault;
        entity.RiskLevel = risk.RiskLevel;
        entity.RequiresPlatformApproval = risk.RequiresPlatformApproval;
        entity.ApplicableRoleCodesJson = JsonStorage.Serialize(applicableRoles);
        entity.PayloadJson = JsonSerializer.Serialize(
            CreatePayload(request.ObjectTypeCode, request.StartNode, request.Nodes),
            JsonSerializerOptions.Web);
        entity.State = "Draft";
        entity.Revision++;
        entity.UpdatedBy = user.UserId;
        entity.UpdatedAt = now;
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Configuration",
                "configuration.draft_updated",
                "ConfigurationVersion",
                entity.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                before: $"revision={entity.Revision - 1}",
                after: $"revision={entity.Revision}"),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await MapAsync([entity], cancellationToken)).Single();
    }

    public async Task<WorkflowConfigurationValidationDto> ValidateAsync(
        UserPrincipal user,
        Guid configurationId,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("configuration", AccessAction.Update);
        user.EnsureAuthenticated();
        ValidateIdempotencyKey(idempotencyKey);
        var scope = $"configuration:validate:{configurationId:D}:{user.UserId:D}";
        var hash = Hash(new { configurationId });
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同校验请求。");
            }

            return (await GetRequiredAsync(user, configurationId, cancellationToken)).Validation
                ?? throw new ConflictException(ErrorCodes.Conflict, "配置尚未生成校验结果。");
        }

        var entity = await LoadEditableAsync(user, configurationId, cancellationToken);
        EnsureMutable(entity);
        var validation = await ValidateEntityAsync(entity, cancellationToken);
        var now = clock.UtcNow;
        entity.State = validation.IsValid ? "Validated" : "Draft";
        entity.ImpactAnalysisJson = JsonSerializer.Serialize(validation, JsonSerializerOptions.Web);
        entity.Revision++;
        entity.UpdatedBy = user.UserId;
        entity.UpdatedAt = now;
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Configuration",
                "configuration.validated",
                "ConfigurationVersion",
                entity.Id.ToString("D"),
                traceId,
                validation.IsValid ? AuditOutcome.Succeeded : AuditOutcome.Failed,
                now,
                after: entity.ImpactAnalysisJson),
            cancellationToken);
        await idempotencyStore.AddAsync(
            new IdempotencyEntry(
                Guid.NewGuid(),
                scope,
                idempotencyKey,
                hash,
                "ConfigurationVersion",
                entity.Id.ToString("D"),
                "completed",
                now,
                now.AddDays(30)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return validation;
    }

    public async Task<WorkflowConfigurationDto> PublishAsync(
        UserPrincipal user,
        Guid configurationId,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("configuration", AccessAction.Approve);
        user.EnsureAuthenticated();
        ValidateIdempotencyKey(idempotencyKey);
        var scope = $"configuration:publish:{configurationId:D}:{user.UserId:D}";
        var hash = Hash(new { configurationId });
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同发布请求。");
            }

            return await GetRequiredAsync(user, configurationId, cancellationToken);
        }

        var entity = await LoadEditableAsync(user, configurationId, cancellationToken, allowPublished: true);
        await EnsurePublishScopeAsync(user, entity, cancellationToken);
        if (entity.State == "Published")
        {
            throw new ConflictException(ErrorCodes.Conflict, "该配置版本已经发布。");
        }

        EnsureMutable(entity);
        var validation = await ValidateEntityAsync(entity, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException("配置校验失败：" + string.Join("；", validation.Errors));
        }

        var payload = DeserializePayload(entity.PayloadJson);
        var now = clock.UtcNow;
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var definition = await dbContext.WorkflowDefinitions
                .SingleOrDefaultAsync(item => item.Code == entity.ConfigurationKey, cancellationToken);
            if (definition is null)
            {
                definition = new WorkflowDefinitionEntity
                {
                    Id = Guid.NewGuid(),
                    Code = entity.ConfigurationKey,
                    Name = entity.Name,
                    Description = entity.Description,
                    Status = "Published",
                    DepartmentId = entity.DepartmentId,
                    ObjectTypeCode = payload.ObjectTypeCode,
                    ApplicableRoleCodesJson = entity.ApplicableRoleCodesJson,
                    IsDefaultForDepartment = entity.IsDefault && entity.DepartmentId.HasValue,
                    RiskLevel = entity.RiskLevel,
                    RequiresPlatformApproval = entity.RequiresPlatformApproval,
                    EnvironmentCode = entity.EnvironmentCode,
                    IsTestData = entity.IsTestData,
                    Version = 1,
                    CreatedBy = user.UserId,
                    CreatedAt = now,
                    UpdatedBy = user.UserId,
                    UpdatedAt = now
                };
                await dbContext.WorkflowDefinitions.AddAsync(definition, cancellationToken);
            }
            else
            {
                definition.Name = entity.Name;
                definition.Description = entity.Description;
                definition.Status = "Published";
                definition.DepartmentId = entity.DepartmentId;
                definition.ObjectTypeCode = payload.ObjectTypeCode;
                definition.ApplicableRoleCodesJson = entity.ApplicableRoleCodesJson;
                definition.IsDefaultForDepartment = entity.IsDefault && entity.DepartmentId.HasValue;
                definition.RiskLevel = entity.RiskLevel;
                definition.RequiresPlatformApproval = entity.RequiresPlatformApproval;
                definition.EnvironmentCode = entity.EnvironmentCode;
                definition.IsTestData = entity.IsTestData;
                definition.Version++;
                definition.UpdatedBy = user.UserId;
                definition.UpdatedAt = now;
            }

            if (definition.IsDefaultForDepartment && definition.DepartmentId.HasValue)
            {
                var previousDefaults = await dbContext.WorkflowDefinitions
                    .Where(item =>
                        item.Id != definition.Id
                        && item.DepartmentId == definition.DepartmentId
                        && item.ObjectTypeCode == definition.ObjectTypeCode
                        && item.IsDefaultForDepartment
                        && item.Status == "Published"
                        && !item.IsTestData)
                    .ToListAsync(cancellationToken);
                previousDefaults.ForEach(item => item.IsDefaultForDepartment = false);
            }

            var previousVersions = await dbContext.WorkflowVersions
                .Where(item => item.WorkflowDefinitionId == definition.Id && item.Status == "Published")
                .ToListAsync(cancellationToken);
            previousVersions.ForEach(item => item.Status = "Retired");
            var nextVersionNumber = (await dbContext.WorkflowVersions
                .Where(item => item.WorkflowDefinitionId == definition.Id)
                .MaxAsync(item => (int?)item.VersionNumber, cancellationToken) ?? 0) + 1;
            var workflowVersion = new WorkflowVersionEntity
            {
                Id = Guid.NewGuid(),
                WorkflowDefinitionId = definition.Id,
                VersionNumber = nextVersionNumber,
                Status = "Published",
                DefinitionJson = JsonSerializer.Serialize(payload, JsonSerializerOptions.Web),
                PublishedAt = now
            };
            await dbContext.WorkflowVersions.AddAsync(workflowVersion, cancellationToken);
            await CreateNodePoliciesAsync(
                user,
                workflowVersion.Id,
                payload.Nodes,
                now,
                cancellationToken);

            var otherPublished = await dbContext.ConfigurationVersions
                .Where(item => item.ConfigurationKey == entity.ConfigurationKey
                    && item.Id != entity.Id
                    && item.State == "Published")
                .ToListAsync(cancellationToken);
            otherPublished.ForEach(item => item.State = "Retired");

            entity.State = "Published";
            entity.PublishedBy = user.UserId;
            entity.PublishedAt = now;
            entity.PublishedWorkflowVersionId = workflowVersion.Id;
            entity.Revision++;
            entity.UpdatedBy = user.UserId;
            entity.UpdatedAt = now;
            entity.ImpactAnalysisJson = JsonSerializer.Serialize(
                new
                {
                    validation.IsValid,
                    validation.Errors,
                    validation.Warnings,
                    validation.ObjectTypeCode,
                    validation.NodeCount,
                    validation.AssigneeCount,
                    validation.TerminalNodeCount,
                    validation.ValidatedAt,
                    WorkflowDefinitionId = definition.Id,
                    WorkflowVersionId = workflowVersion.Id,
                    WorkflowVersionNumber = workflowVersion.VersionNumber,
                    entity.RiskLevel,
                    entity.RequiresPlatformApproval,
                    entity.IsDefault
                },
                JsonSerializerOptions.Web);
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Configuration",
                    "configuration.published",
                    "ConfigurationVersion",
                    entity.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: entity.ImpactAnalysisJson),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "ConfigurationVersion",
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

        return (await MapAsync([entity], cancellationToken)).Single();
    }

    public async Task<WorkflowConfigurationDto> RollbackAsync(
        UserPrincipal user,
        Guid configurationId,
        string? comment,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("configuration", AccessAction.Manage);
        user.EnsureAuthenticated();
        ValidateIdempotencyKey(idempotencyKey);
        var scope = $"configuration:rollback:{configurationId:D}:{user.UserId:D}";
        var hash = Hash(new { configurationId, comment });
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同回滚请求。");
            }

            return await GetRequiredAsync(user, configurationId, cancellationToken);
        }

        var entity = await LoadVisibleTrackedAsync(user, configurationId, cancellationToken);
        await EnsurePublishScopeAsync(user, entity, cancellationToken);
        if (entity.State != "Retired")
        {
            throw new ConflictException(ErrorCodes.Conflict, "只有已退役的历史配置可以回滚。");
        }

        if (!entity.PublishedWorkflowVersionId.HasValue)
        {
            throw new ConflictException(ErrorCodes.Conflict, "该配置没有可回滚的已发布流程版本。");
        }

        var workflowVersion = await dbContext.WorkflowVersions
            .SingleOrDefaultAsync(
                item => item.Id == entity.PublishedWorkflowVersionId.Value,
                cancellationToken) ?? throw new NotFoundException("目标流程版本不存在。");
        var definition = await dbContext.WorkflowDefinitions
            .SingleAsync(item => item.Id == workflowVersion.WorkflowDefinitionId, cancellationToken);
        var now = clock.UtcNow;
        var reason = NormalizeOptional(comment, "回滚原因", 1000) ?? "回滚到历史配置版本";

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var allVersions = await dbContext.WorkflowVersions
                .Where(item => item.WorkflowDefinitionId == definition.Id)
                .ToListAsync(cancellationToken);
            allVersions.ForEach(item => item.Status = "Retired");
            workflowVersion.Status = "Published";
            workflowVersion.PublishedAt = now;
            definition.Name = entity.Name;
            definition.Description = entity.Description;
            definition.Status = "Published";
            definition.DepartmentId = entity.DepartmentId;
            definition.ObjectTypeCode = DeserializePayload(entity.PayloadJson).ObjectTypeCode;
            definition.ApplicableRoleCodesJson = entity.ApplicableRoleCodesJson;
            definition.IsDefaultForDepartment = entity.IsDefault && entity.DepartmentId.HasValue;
            definition.RiskLevel = entity.RiskLevel;
            definition.RequiresPlatformApproval = entity.RequiresPlatformApproval;
            definition.EnvironmentCode = entity.EnvironmentCode;
            definition.IsTestData = entity.IsTestData;
            definition.Version++;
            definition.UpdatedBy = user.UserId;
            definition.UpdatedAt = now;
            if (definition.IsDefaultForDepartment && definition.DepartmentId.HasValue)
            {
                var otherDefaults = await dbContext.WorkflowDefinitions
                    .Where(item =>
                        item.Id != definition.Id
                        && item.DepartmentId == definition.DepartmentId
                        && item.ObjectTypeCode == definition.ObjectTypeCode
                        && item.IsDefaultForDepartment
                        && item.Status == "Published"
                        && !item.IsTestData)
                    .ToListAsync(cancellationToken);
                otherDefaults.ForEach(item => item.IsDefaultForDepartment = false);
            }

            var currentConfigurations = await dbContext.ConfigurationVersions
                .Where(item => item.ConfigurationKey == entity.ConfigurationKey
                    && item.State == "Published")
                .ToListAsync(cancellationToken);
            currentConfigurations.ForEach(item => item.State = "Retired");
            entity.State = "Published";
            entity.PublishedBy = user.UserId;
            entity.PublishedAt = now;
            entity.Revision++;
            entity.UpdatedBy = user.UserId;
            entity.UpdatedAt = now;
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Configuration",
                    "configuration.rollback",
                    "ConfigurationVersion",
                    entity.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: JsonSerializer.Serialize(
                        new
                        {
                            entity.ConfigurationKey,
                            WorkflowDefinitionId = definition.Id,
                            WorkflowVersionId = workflowVersion.Id,
                            WorkflowVersionNumber = workflowVersion.VersionNumber
                        },
                        JsonSerializerOptions.Web),
                    reason: reason),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "ConfigurationVersion",
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

        return (await MapAsync([entity], cancellationToken)).Single();
    }

    public async Task<IReadOnlyList<WorkflowConfigurationDto>> ListVersionsAsync(
        UserPrincipal user,
        Guid configurationId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("configuration", AccessAction.Read);
        var current = await ApplyScope(user).SingleOrDefaultAsync(
            item => item.Id == configurationId
                && item.ConfigurationType == WorkflowConfigurationType,
            cancellationToken) ?? throw new NotFoundException("配置版本不存在。");
        var rows = await ApplyScope(user)
            .Where(item => item.ConfigurationKey == current.ConfigurationKey)
            .OrderByDescending(item => item.VersionNumber)
            .ToListAsync(cancellationToken);
        return await MapAsync(rows, cancellationToken);
    }

    public async Task<WorkflowConfigurationDto> SetStatusAsync(
        UserPrincipal user,
        Guid configurationId,
        bool disable,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan(
            "configuration",
            disable ? AccessAction.Manage : AccessAction.Approve);
        user.EnsureAuthenticated();
        ValidateIdempotencyKey(idempotencyKey);
        var scope = $"configuration:{(disable ? "disable" : "restore")}:{configurationId:D}:{user.UserId:D}";
        var hash = Hash(new { configurationId, disable });
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(
                    ErrorCodes.Conflict,
                    "同一幂等键不能用于不同模板状态请求。");
            }

            return await GetRequiredAsync(user, configurationId, cancellationToken);
        }

        var entity = await LoadVisibleTrackedAsync(user, configurationId, cancellationToken);
        await EnsurePublishScopeAsync(user, entity, cancellationToken);
        if (!entity.PublishedWorkflowVersionId.HasValue)
        {
            throw new ConflictException(
                ErrorCodes.WorkflowTemplateDisabled,
                "尚未发布的模板不能停用或恢复。");
        }

        var workflowVersion = await dbContext.WorkflowVersions.SingleOrDefaultAsync(
            item => item.Id == entity.PublishedWorkflowVersionId.Value,
            cancellationToken) ?? throw new NotFoundException("模板对应的流程版本不存在。");
        var definition = await dbContext.WorkflowDefinitions.SingleOrDefaultAsync(
            item => item.Id == workflowVersion.WorkflowDefinitionId,
            cancellationToken) ?? throw new NotFoundException("模板对应的流程定义不存在。");
        if (disable && entity.State != "Published")
        {
            throw new ConflictException(ErrorCodes.Conflict, "只有已发布模板可以停用。");
        }
        if (!disable && entity.State != "Retired")
        {
            throw new ConflictException(ErrorCodes.Conflict, "只有已停用模板可以恢复。");
        }
        if (!disable
            && await dbContext.ConfigurationVersions.AnyAsync(
                item =>
                    item.ConfigurationKey == entity.ConfigurationKey
                    && item.Id != entity.Id
                    && item.State == "Published"
                    && !item.IsTestData,
                cancellationToken))
        {
            throw new ConflictException(
                ErrorCodes.Conflict,
                "同一模板已有其他发布版本，请先通过版本历史回滚。");
        }

        var now = clock.UtcNow;
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            entity.State = disable ? "Retired" : "Published";
            entity.IsDefault = disable ? false : entity.IsDefault;
            entity.Revision++;
            entity.UpdatedBy = user.UserId;
            entity.UpdatedAt = now;
            definition.Status = disable ? "Retired" : "Published";
            definition.IsDefaultForDepartment = disable
                ? false
                : entity.IsDefault;
            definition.Version++;
            definition.UpdatedBy = user.UserId;
            definition.UpdatedAt = now;
            workflowVersion.Status = disable ? "Retired" : "Published";
            workflowVersion.PublishedAt = disable ? workflowVersion.PublishedAt : now;

            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Configuration",
                    disable ? "configuration.disabled" : "configuration.restored",
                    "ConfigurationVersion",
                    entity.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    before: entity.State,
                    after: disable ? "Retired" : "Published"),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "ConfigurationVersion",
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

        return (await MapAsync([entity], cancellationToken)).Single();
    }

    public async Task<WorkflowConfigurationSimulationDto> SimulateAsync(
        UserPrincipal user,
        Guid configurationId,
        SimulateWorkflowConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("configuration", AccessAction.Read);
        var entity = await LoadVisibleTrackedAsync(user, configurationId, cancellationToken);
        var validation = await ValidateEntityAsync(entity, cancellationToken);
        var payload = DeserializePayloadOrEmpty(entity.PayloadJson);
        var path = new List<string>();
        if (validation.IsValid)
        {
            IReadOnlyDictionary<string, string?>? fields = null;
            if (request.BusinessObjectId.HasValue)
            {
                var businessObject = await dbContext.BusinessObjects.AsNoTracking()
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id == request.BusinessObjectId.Value
                            && item.OrganizationId == user.OrganizationId
                            && (!entity.DepartmentId.HasValue
                                || item.DepartmentId == entity.DepartmentId.Value),
                        cancellationToken);
                if (businessObject is null)
                {
                    return new WorkflowConfigurationSimulationDto(
                        false,
                        ["试运行业务对象不存在或不在模板所属部门。"],
                        validation.Warnings,
                        [],
                        entity.RiskLevel,
                        entity.RequiresPlatformApproval);
                }
                user.EnsureDepartmentAccess(businessObject.DepartmentId);
                fields = JsonStorage.Deserialize<Dictionary<string, string?>>(
                    businessObject.FieldsJson);
            }

            var nodes = payload.Nodes.ToDictionary(
                item => item.Code,
                item => item,
                StringComparer.Ordinal);
            var current = payload.StartNode;
            var visited = new HashSet<string>(StringComparer.Ordinal);
            while (!string.IsNullOrWhiteSpace(current)
                && nodes.TryGetValue(current, out var node)
                && visited.Add(current))
            {
                if (node.Condition is not null
                    && fields is not null
                    && !EvaluateCondition(node.Condition, fields))
                {
                    current = node.NextNode ?? string.Empty;
                    continue;
                }

                path.Add(node.Name);
                if (node.CompleteOnApprove || string.IsNullOrWhiteSpace(node.NextNode))
                {
                    break;
                }
                current = node.NextNode;
            }
        }

        return new WorkflowConfigurationSimulationDto(
            validation.IsValid,
            validation.Errors,
            validation.Warnings,
            path,
            entity.RiskLevel,
            entity.RequiresPlatformApproval);
    }

    private IQueryable<ConfigurationVersionEntity> ApplyScope(UserPrincipal user) =>
        dbContext.ConfigurationVersions.AsNoTracking().Where(item =>
            item.OrganizationId == user.OrganizationId
            && (user.IsPlatformAdmin || !item.IsTestData || item.CreatedBy == user.UserId)
            && item.EnvironmentCode == environmentOptions.CurrentEnvironmentCode
            && (user.IsPlatformAdmin
                || item.State == "Published"
                || item.CreatedBy == user.UserId
                || (item.DepartmentId.HasValue && user.DepartmentIds.Contains(item.DepartmentId.Value))));

    private async Task<WorkflowConfigurationDto> GetRequiredAsync(
        UserPrincipal user,
        Guid configurationId,
        CancellationToken cancellationToken) =>
        await GetAsync(user, configurationId, cancellationToken)
        ?? throw new NotFoundException("配置版本不存在。");

    private async Task<ConfigurationVersionEntity> LoadEditableAsync(
        UserPrincipal user,
        Guid configurationId,
        CancellationToken cancellationToken,
        bool allowPublished = false)
    {
        var entity = await LoadVisibleTrackedAsync(user, configurationId, cancellationToken);
        if (!entity.DepartmentId.HasValue)
        {
            throw new PermissionException(
                ErrorCodes.DepartmentScopeDenied,
                "配置必须属于业务部门。");
        }
        await EnsureDepartmentFlowOwnerAsync(user, entity.DepartmentId.Value, cancellationToken);

        if (entity.ConfigurationType != WorkflowConfigurationType)
        {
            throw new ValidationException("当前配置不是业务流程配置。");
        }

        if (!allowPublished && entity.State == "Published")
        {
            throw new ConflictException(ErrorCodes.Conflict, "已发布配置必须通过新草稿或回滚流程修改。");
        }

        return entity;
    }

    private async Task<ConfigurationVersionEntity> LoadVisibleTrackedAsync(
        UserPrincipal user,
        Guid configurationId,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.ConfigurationVersions
            .SingleOrDefaultAsync(
                item => item.Id == configurationId
                    && item.OrganizationId == user.OrganizationId,
                cancellationToken) ?? throw new NotFoundException("配置版本不存在。");
        if (!user.IsPlatformAdmin
            && entity.State != "Published"
            && entity.CreatedBy != user.UserId
            && (!entity.DepartmentId.HasValue || !user.DepartmentIds.Contains(entity.DepartmentId.Value)))
        {
            throw new PermissionException(ErrorCodes.DepartmentScopeDenied, "当前身份不能访问该配置。");
        }

        return entity;
    }

    private async Task ValidateDepartmentAsync(
        UserPrincipal user,
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        if (departmentId == Guid.Empty)
        {
            throw new ValidationException("配置所属部门不能为空。");
        }

        user.EnsureDepartmentAccess(departmentId);
        var exists = await dbContext.Departments.AsNoTracking()
            .AnyAsync(
                item => item.Id == departmentId
                    && item.OrganizationId == user.OrganizationId
                    && item.IsActive,
                cancellationToken);
        if (!exists)
        {
            throw new ValidationException("配置所属部门不存在或已停用。");
        }
    }

    private async Task EnsureDepartmentFlowOwnerAsync(
        UserPrincipal user,
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        if (user.IsPlatformAdmin)
        {
            return;
        }

        user.EnsureDepartmentAccess(departmentId);
        if (user.HasRoleInScope("department-flow-owner", departmentId)
            || await dbContext.UserDepartmentMemberships.AsNoTracking().AnyAsync(
                item =>
                    item.UserId == user.UserId
                    && item.DepartmentId == departmentId
                    && item.IsActive
                    && item.IsManager,
                cancellationToken))
        {
            return;
        }

        throw new PermissionException(
            ErrorCodes.PermissionDenied,
            "只有本部门流程负责人可以维护本部门流程模板。");
    }

    private async Task EnsurePublishScopeAsync(
        UserPrincipal user,
        ConfigurationVersionEntity entity,
        CancellationToken cancellationToken)
    {
        if (!entity.DepartmentId.HasValue)
        {
            throw new PermissionException(
                ErrorCodes.DepartmentScopeDenied,
                "流程模板必须归属一个业务部门。");
        }

        await EnsureDepartmentFlowOwnerAsync(
            user,
            entity.DepartmentId.Value,
            cancellationToken);
        if (entity.RequiresPlatformApproval
            && !user.IsPlatformAdmin)
        {
            throw new PermissionException(
                ErrorCodes.PermissionDenied,
                "该模板涉及跨部门或高风险动作，必须由平台管理员审核发布。");
        }

        user.EnsureCan("configuration", AccessAction.Approve);
    }

    private async Task<IReadOnlyList<string>> NormalizeApplicableRolesAsync(
        IReadOnlyList<string>? roleCodes,
        CancellationToken cancellationToken)
    {
        var normalized = (roleCodes ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .Take(30)
            .ToArray();
        if (normalized.Length == 0)
        {
            return [];
        }

        var existing = await dbContext.Roles.AsNoTracking()
            .Where(item => item.IsActive && normalized.Contains(item.Code))
            .Select(item => item.Code)
            .ToListAsync(cancellationToken);
        if (existing.Count != normalized.Length)
        {
            throw new ValidationException(
                "适用岗位包含不存在或已停用的角色："
                + string.Join("、", normalized.Except(existing, StringComparer.Ordinal)));
        }

        return normalized;
    }

    private async Task<ConfigurationRisk> ClassifyRiskAsync(
        Guid departmentId,
        string objectTypeCode,
        IReadOnlyList<WorkflowConfigurationNodeRequest> nodes,
        CancellationToken cancellationToken)
    {
        var triggers = new List<string>();
        var objectCode = objectTypeCode?.Trim().ToUpperInvariant() ?? string.Empty;
        var highRiskObjectMarkers = new[]
        {
            "PAYMENT", "FINANCE", "QUALITY_RELEASE", "FORMULA", "RECIPE",
            "EQUIPMENT_CONTROL", "SAFETY", "INTERLOCK", "CONTRACT", "COMPENSATION"
        };
        if (highRiskObjectMarkers.Any(marker =>
                objectCode.Contains(marker, StringComparison.Ordinal)))
        {
            triggers.Add("对象类型属于付款/质量放行/配方/设备/联锁/合同/赔付高风险类别");
        }

        var highRiskTextMarkers = new[]
        {
            "付款", "放行", "配方", "设备控制", "联锁", "合同承诺", "重大赔付"
        };
        foreach (var node in nodes)
        {
            var text = string.Join(
                " ",
                node.Name,
                node.BusinessDescription,
                node.CompletionCondition,
                string.Join(" ", node.Deliverables ?? []));
            if (node.PermanentHumanBoundary
                || highRiskTextMarkers.Any(marker =>
                    text.Contains(marker, StringComparison.Ordinal)))
            {
                triggers.Add($"节点 {node.Code} 命中永久人工或高风险业务边界");
            }

            if (node.AllowAgentProxy)
            {
                triggers.Add($"节点 {node.Code} 启用数字员工代理");
            }
        }

        var specificUsers = nodes
            .Where(item => string.Equals(
                item.AssigneeStrategy,
                WorkflowAssignmentStrategy.SpecificUser.ToString(),
                StringComparison.OrdinalIgnoreCase))
            .Select(item => item.AssigneeUserName?.Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.Ordinal)
            .Cast<string>()
            .ToArray();
        if (specificUsers.Length > 0)
        {
            var crossDepartment = await (
                from user in dbContext.Users.AsNoTracking()
                where specificUsers.Contains(user.UserName) && user.IsActive
                where !dbContext.UserDepartmentMemberships.Any(membership =>
                    membership.UserId == user.Id
                    && membership.DepartmentId == departmentId
                    && membership.IsActive)
                where !dbContext.Departments.Any(department =>
                    department.Id == departmentId && department.ManagerUserId == user.Id)
                select user.UserName).ToListAsync(cancellationToken);
            if (crossDepartment.Count > 0)
            {
                triggers.Add("节点包含跨部门处理人：" + string.Join("、", crossDepartment));
            }
        }

        var riskLevel = triggers.Count > 0 ? "High" : "Low";
        return new ConfigurationRisk(
            riskLevel,
            riskLevel == "High",
            triggers.Distinct(StringComparer.Ordinal).ToList());
    }

    private async Task CreateNodePoliciesAsync(
        UserPrincipal user,
        Guid workflowVersionId,
        IReadOnlyList<WorkflowConfigurationNodeRequest> nodes,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var userNames = nodes
            .Where(item => string.Equals(
                item.AssigneeStrategy,
                WorkflowAssignmentStrategy.SpecificUser.ToString(),
                StringComparison.OrdinalIgnoreCase))
            .Select(item => item.AssigneeUserName?.Trim() ?? string.Empty)
            .Where(item => item.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var userIds = await dbContext.Users.AsNoTracking()
            .Where(item => userNames.Contains(item.UserName))
            .ToDictionaryAsync(
                item => item.UserName,
                item => item.Id,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken);
        var roleCodes = nodes
            .Where(item => string.Equals(
                item.AssigneeStrategy,
                WorkflowAssignmentStrategy.BusinessRole.ToString(),
                StringComparison.OrdinalIgnoreCase))
            .Select(item => item.BusinessRoleCode?.Trim().ToUpperInvariant() ?? string.Empty)
            .Where(item => item.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var roleIds = await dbContext.Roles.AsNoTracking()
            .Where(item => roleCodes.Contains(item.Code))
            .ToDictionaryAsync(
                item => item.Code,
                item => item.Id,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken);

        foreach (var node in nodes)
        {
            var strategy = Enum.Parse<WorkflowAssignmentStrategy>(
                node.AssigneeStrategy,
                ignoreCase: true);
            Guid? businessRoleId = null;
            if (strategy == WorkflowAssignmentStrategy.BusinessRole
                && node.BusinessRoleCode is not null)
            {
                if (!roleIds.TryGetValue(node.BusinessRoleCode, out var roleId))
                {
                    throw new ValidationException(
                        $"节点 {node.Code} 的业务角色 {node.BusinessRoleCode} 不存在。");
                }
                businessRoleId = roleId;
            }
            var policy = new WorkflowNodePolicyEntity
            {
                Id = Guid.NewGuid(),
                WorkflowVersionId = workflowVersionId,
                NodeCode = node.Code,
                NodeName = node.Name,
                BusinessDescription = node.BusinessDescription,
                VersionNumber = 1,
                ApproverUserId = strategy == WorkflowAssignmentStrategy.SpecificUser
                    && node.AssigneeUserName is not null
                    ? userIds.GetValueOrDefault(node.AssigneeUserName)
                    : null,
                AssigneeStrategy = strategy.ToString(),
                BusinessRoleId = businessRoleId,
                AllowTransfer = true,
                TransferTargetStrategy = "DepartmentManager",
                SlaMinutes = node.SlaMinutes,
                ReminderMinutes = node.Notification?.BeforeDueMinutes,
                EscalationMinutes = node.Notification?.EscalateAfterMinutes,
                TimeoutStrategy = node.Notification?.Enabled == false ? "None" : "Escalate",
                AllowAgentAssist = node.AllowAgentAssist || node.AiReview?.Enabled == true,
                AllowAgentProxy = node.AllowAgentProxy,
                AgentProfileId = node.AgentProfileId ?? node.AiReview?.AgentProfileId,
                AgentMaxRisk = "Low",
                AgentMaxAmount = 0,
                AgentMaxAutonomy = "A2",
                AgentProxyMode = node.AllowAgentProxy
                    ? "HumanConfirmedProxy"
                    : "RemindOnly",
                AutoProxyAfterMinutes = null,
                PermanentHumanBoundary = node.PermanentHumanBoundary,
                RequiredAttachmentCount = Math.Max(0, node.RequiredAttachmentCount),
                DeliverablesJson = JsonStorage.Serialize(node.Deliverables ?? []),
                RelatedRoleCodesJson = JsonStorage.Serialize(node.RelatedRoleCodes ?? []),
                CompletionCondition = node.CompletionCondition,
                FormSchemaJson = JsonStorage.Serialize(node.Fields ?? []),
                FormSchemaVersion = string.IsNullOrWhiteSpace(node.FormSchemaVersion)
                    ? "1.0"
                    : node.FormSchemaVersion.Trim(),
                NodeActionsJson = JsonStorage.Serialize(node.Actions ?? []),
                AiReviewJson = JsonStorage.Serialize(node.AiReview ?? new WorkflowAiReviewRequest()),
                NotificationPolicyJson = JsonStorage.Serialize(
                    node.Notification ?? new WorkflowNotificationRequest()),
                Status = "Published",
                PublishedBy = user.UserId,
                PublishedAt = now
            };
            await dbContext.WorkflowNodePolicies.AddAsync(policy, cancellationToken);
        }
    }

    private async Task<WorkflowConfigurationValidationDto> ValidateEntityAsync(
        ConfigurationVersionEntity entity,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        WorkflowConfigurationPayload payload;
        try
        {
            payload = DeserializePayload(entity.PayloadJson);
        }
        catch (Exception exception) when (exception is JsonException or ValidationException)
        {
            return new WorkflowConfigurationValidationDto(
                false,
                [exception.Message],
                warnings,
                string.Empty,
                0,
                0,
                0,
                clock.UtcNow);
        }

        Guid? objectTypeId = null;
        var boundObjectFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(payload.ObjectTypeCode))
        {
            errors.Add("业务对象类型不能为空。");
        }
        else if (!string.Equals(payload.ObjectTypeCode, "ANY", StringComparison.Ordinal))
        {
            objectTypeId = await dbContext.ObjectTypes.AsNoTracking()
                .Where(item =>
                    item.Code == payload.ObjectTypeCode
                    && item.Status == "Published")
                .Select(item => (Guid?)item.Id)
                .SingleOrDefaultAsync(cancellationToken);
            if (!objectTypeId.HasValue)
            {
                errors.Add($"业务对象类型 {payload.ObjectTypeCode} 不存在或未发布。");
            }
            else
            {
                boundObjectFields = (await dbContext.FieldDefinitions.AsNoTracking()
                        .Where(item => item.ObjectTypeId == objectTypeId.Value)
                        .Select(item => item.Key)
                        .ToListAsync(cancellationToken))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
        }

        if (payload.Nodes.Count == 0)
        {
            errors.Add("流程至少需要一个节点。");
        }

        var duplicateCodes = payload.Nodes
            .GroupBy(item => item.Code, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicateCodes.Length > 0)
        {
            errors.Add("节点编码重复：" + string.Join("、", duplicateCodes));
        }

        var configuredAgentIds = payload.Nodes
            .Where(item => string.Equals(
                item.AssigneeStrategy,
                WorkflowAssignmentStrategy.Agent.ToString(),
                StringComparison.OrdinalIgnoreCase))
            .Select(item => item.AgentProfileId)
            .Where(item => item.HasValue)
            .Select(item => item!.Value)
            .Distinct()
            .ToArray();
        if (configuredAgentIds.Length > 0)
        {
            var validAgentIds = await dbContext.AgentProfiles.AsNoTracking()
                .Where(item =>
                    configuredAgentIds.Contains(item.Id)
                    && item.LifecycleStatus == "Published"
                    && item.IsActive
                    && item.CurrentPublishedVersionId != null)
                .Select(item => item.Id)
                .ToListAsync(cancellationToken);
            var invalidAgentIds = configuredAgentIds.Except(validAgentIds).ToArray();
            if (invalidAgentIds.Length > 0)
            {
                errors.Add(
                    "以下数字员工未发布、已停用或不存在："
                    + string.Join("、", invalidAgentIds));
            }
        }

        var nodesByCode = payload.Nodes
            .GroupBy(item => item.Code, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        if (!nodesByCode.ContainsKey(payload.StartNode))
        {
            errors.Add($"开始节点 {payload.StartNode} 未定义。");
        }

        foreach (var node in payload.Nodes)
        {
            if (!StableNodeCodeRegex().IsMatch(node.Code))
            {
                errors.Add($"节点编码 {node.Code} 必须为 1—80 位大写字母、数字、下划线或短横线。");
            }

            if (string.IsNullOrWhiteSpace(node.Name))
            {
                errors.Add($"节点 {node.Code} 缺少名称。");
            }

            if (!string.IsNullOrWhiteSpace(node.NextNode) && !nodesByCode.ContainsKey(node.NextNode))
            {
                errors.Add($"节点 {node.Code} 指向不存在的下一节点 {node.NextNode}。");
            }

            if (node.Condition is not null && !SupportedOperators.Contains(node.Condition.Operator))
            {
                errors.Add($"节点 {node.Code} 使用了不支持的条件操作符 {node.Condition.Operator}。");
            }

            if (!Version.TryParse(node.FormSchemaVersion, out _))
            {
                errors.Add($"节点 {node.Code} 的表单版本号无效。");
            }

            if (node.AiReview?.Enabled == true
                && string.IsNullOrWhiteSpace(node.AiReview.RegisteredAction))
            {
                errors.Add($"节点 {node.Code} 启用了 AI 评论，但未选择注册动作。");
            }

            foreach (var field in node.Fields ?? [])
            {
                if (field.DataSource == "BusinessObjectReference"
                    && objectTypeId.HasValue
                    && (field.BoundObjectFieldCode is null
                        || !boundObjectFields.Contains(field.BoundObjectFieldCode)))
                {
                    errors.Add(
                        $"节点 {node.Code} 的字段 {field.Key} 绑定了不存在的业务对象字段。");
                }
            }

            foreach (var action in node.Actions ?? [])
            {
                if (!SupportedNodeActionTypes.Contains(action.ActionType))
                {
                    errors.Add(
                        $"节点 {node.Code} 的动作 {action.Name} 引用了未注册动作类型。");
                }
            }

            var fieldRoleCodes = (node.Fields ?? [])
                .SelectMany(field =>
                    (field.VisibleRoleCodes ?? []).Concat(field.WriteRoleCodes ?? []))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (fieldRoleCodes.Length > 0)
            {
                var activeRoleCodes = await dbContext.Roles.AsNoTracking()
                    .Where(role => role.IsActive && fieldRoleCodes.Contains(role.Code))
                    .Select(role => role.Code)
                    .ToListAsync(cancellationToken);
                var invalidRoles = fieldRoleCodes
                    .Except(activeRoleCodes, StringComparer.Ordinal)
                    .ToArray();
                if (invalidRoles.Length > 0)
                {
                    errors.Add(
                        $"节点 {node.Code} 的字段引用了无效岗位："
                        + string.Join("、", invalidRoles));
                }
            }

            if (!Enum.TryParse<WorkflowAssignmentStrategy>(node.AssigneeStrategy, true, out var strategy))
            {
                errors.Add($"节点 {node.Code} 的处理人策略无效。");
                continue;
            }

            if (strategy == WorkflowAssignmentStrategy.SpecificUser
                && string.IsNullOrWhiteSpace(node.AssigneeUserName))
            {
                errors.Add($"节点 {node.Code} 指定人员策略缺少用户名。");
                continue;
            }

            if (strategy == WorkflowAssignmentStrategy.BusinessRole
                && string.IsNullOrWhiteSpace(node.BusinessRoleCode))
            {
                errors.Add($"节点 {node.Code} 业务角色策略缺少业务角色。");
                continue;
            }

            if (entity.DepartmentId.HasValue)
            {
                var assignmentValidation = await assignmentResolver.ValidateAsync(
                    entity.OrganizationId,
                    entity.DepartmentId.Value,
                    payload.ObjectTypeCode,
                    new WorkflowAssignmentRule(
                        strategy,
                        node.AssigneeUserName,
                        node.BusinessRoleCode,
                        node.AgentProfileId),
                    cancellationToken);
                if (!assignmentValidation.IsValid)
                {
                    errors.Add($"节点 {node.Code}：{assignmentValidation.Message}");
                }
            }
        }

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = payload.StartNode;
        while (!string.IsNullOrWhiteSpace(current) && nodesByCode.TryGetValue(current, out var node))
        {
            if (!visited.Add(current))
            {
                errors.Add("流程节点存在循环路径：" + string.Join(" → ", visited.Append(current)));
                break;
            }

            current = node.NextNode;
        }

        var unreachable = nodesByCode.Keys.Where(code => !visited.Contains(code)).ToArray();
        if (unreachable.Length > 0)
        {
            errors.Add("存在无法从开始节点到达的节点：" + string.Join("、", unreachable));
        }

        var terminalNodeCount = payload.Nodes.Count(node =>
            node.CompleteOnApprove || string.IsNullOrWhiteSpace(node.NextNode));
        if (terminalNodeCount == 0)
        {
            errors.Add("流程至少需要一个完成节点。");
        }

        var assigneeNames = payload.Nodes
            .Where(item => string.Equals(
                item.AssigneeStrategy,
                WorkflowAssignmentStrategy.SpecificUser.ToString(),
                StringComparison.Ordinal))
            .Select(item => item.AssigneeUserName?.Trim() ?? string.Empty)
            .Where(item => item.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var existingAssignees = await dbContext.Users.AsNoTracking()
            .Where(item => assigneeNames.Contains(item.UserName)
                && item.IsActive
                && !item.IsServiceIdentity)
            .Select(item => item.UserName)
            .ToListAsync(cancellationToken);
        var missingAssignees = assigneeNames.Except(existingAssignees, StringComparer.Ordinal).ToArray();
        if (missingAssignees.Length > 0)
        {
            errors.Add("以下处理人不存在或已停用：" + string.Join("、", missingAssignees));
        }

        if (payload.Nodes.Count > 20)
        {
            warnings.Add("流程节点超过 20 个，建议拆分子流程以降低维护成本。");
        }

        return new WorkflowConfigurationValidationDto(
            errors.Count == 0,
            errors,
            warnings,
            payload.ObjectTypeCode,
            payload.Nodes.Count,
            payload.Nodes.Select(item => item.AssigneeStrategy).Distinct(StringComparer.Ordinal).Count(),
            terminalNodeCount,
            clock.UtcNow);
    }

    private async Task<IReadOnlyList<WorkflowConfigurationDto>> MapAsync(
        List<ConfigurationVersionEntity> entities,
        CancellationToken cancellationToken)
    {
        if (entities.Count == 0)
        {
            return [];
        }

        var userIds = entities
            .SelectMany(item => new[] { item.CreatedBy, item.UpdatedBy, item.PublishedBy })
            .Where(item => item.HasValue && item.Value != Guid.Empty)
            .Select(item => item!.Value)
            .Distinct()
            .ToArray();
        var users = await dbContext.Users.AsNoTracking()
            .Where(item => userIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.DisplayName, cancellationToken);
        var departmentIds = entities
            .Where(item => item.DepartmentId.HasValue)
            .Select(item => item.DepartmentId!.Value)
            .Distinct()
            .ToArray();
        var departments = await dbContext.Departments.AsNoTracking()
            .Where(item => departmentIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        var workflowVersionIds = entities
            .Where(item => item.PublishedWorkflowVersionId.HasValue)
            .Select(item => item.PublishedWorkflowVersionId!.Value)
            .Distinct()
            .ToArray();
        var workflowVersions = await dbContext.WorkflowVersions.AsNoTracking()
            .Where(item => workflowVersionIds.Contains(item.Id))
            .Select(item => new { item.Id, item.WorkflowDefinitionId })
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        return entities.Select(item =>
        {
            var payload = DeserializePayloadOrEmpty(item.PayloadJson);
            var validation = string.IsNullOrWhiteSpace(item.ImpactAnalysisJson)
                ? null
                : JsonSerializer.Deserialize<WorkflowConfigurationValidationDto>(
                    item.ImpactAnalysisJson,
                    JsonSerializerOptions.Web);
            var publishedVersion = item.PublishedWorkflowVersionId.HasValue
                ? workflowVersions.GetValueOrDefault(item.PublishedWorkflowVersionId.Value)
                : null;
            return new WorkflowConfigurationDto(
                item.Id.ToString("D"),
                item.ConfigurationKey,
                item.Name,
                item.Description,
                item.ConfigurationType,
                item.VersionNumber,
                item.State,
                item.Revision,
                item.OrganizationId.ToString("D"),
                item.DepartmentId?.ToString("D") ?? string.Empty,
                item.DepartmentId.HasValue
                    ? departments.GetValueOrDefault(item.DepartmentId.Value) ?? string.Empty
                    : string.Empty,
                payload.ObjectTypeCode,
                payload.StartNode,
                payload.Nodes,
                item.CreatedBy.ToString("D"),
                users.GetValueOrDefault(item.CreatedBy) ?? string.Empty,
                item.CreatedAt,
                item.UpdatedBy?.ToString("D"),
                item.UpdatedBy.HasValue ? users.GetValueOrDefault(item.UpdatedBy.Value) : null,
                item.UpdatedAt,
                item.PublishedBy?.ToString("D"),
                item.PublishedBy.HasValue ? users.GetValueOrDefault(item.PublishedBy.Value) : null,
                item.PublishedAt,
                publishedVersion?.WorkflowDefinitionId.ToString("D"),
                publishedVersion?.Id.ToString("D"),
                validation,
                item.IsDefault,
                JsonStorage.Deserialize<string[]>(item.ApplicableRoleCodesJson) ?? [],
                item.RiskLevel,
                item.RequiresPlatformApproval,
                item.EnvironmentCode,
                item.IsTestData);
        }).ToList();
    }

    private static WorkflowConfigurationPayload CreatePayload(
        string objectTypeCode,
        string startNode,
        IReadOnlyList<WorkflowConfigurationNodeRequest> nodes)
    {
        if (nodes is null || nodes.Count == 0)
        {
            throw new ValidationException("流程至少需要一个节点。");
        }

        var normalizedObjectType = DomainChecks.RequiredCode(
            objectTypeCode?.Trim().ToUpperInvariant(),
            "业务对象类型",
            64);

        var normalizedStartNode = DomainChecks.RequiredCode(
            startNode?.Trim().ToUpperInvariant(),
            "开始节点",
            80);
        var normalizedNodes = nodes.Select(node =>
        {
            ArgumentNullException.ThrowIfNull(node);
            var strategyValue = string.IsNullOrWhiteSpace(node.AssigneeStrategy)
                ? WorkflowAssignmentStrategy.SpecificUser.ToString()
                : node.AssigneeStrategy.Trim();
            if (!Enum.TryParse<WorkflowAssignmentStrategy>(strategyValue, true, out var strategy))
            {
                throw new ValidationException($"节点 {node.Code} 的处理人策略无效。");
            }

            var assigneeUserName = strategy == WorkflowAssignmentStrategy.SpecificUser
                ? DomainChecks.Required(node.AssigneeUserName, "节点处理人", 128)
                : null;
            var businessRoleCode = strategy == WorkflowAssignmentStrategy.BusinessRole
                ? DomainChecks.RequiredCode(
                    node.BusinessRoleCode?.Trim().ToUpperInvariant() ?? string.Empty,
                    "业务流程角色",
                    64)
                : null;
            Guid? agentProfileId = strategy == WorkflowAssignmentStrategy.Agent
                ? node.AgentProfileId
                    ?? throw new ValidationException($"节点 {node.Code} 的 Agent 策略缺少数字员工。")
                : null;
            var fields = (node.Fields ?? []).Select((field, index) =>
            {
                var dataType = field.DataType?.Trim() ?? string.Empty;
                if (!SupportedFieldTypes.Contains(dataType))
                {
                    throw new ValidationException(
                        $"节点 {node.Code} 的字段 {field.Key} 使用了不支持的类型 {dataType}。");
                }

                var dataSource = string.IsNullOrWhiteSpace(field.DataSource)
                    ? "TaskInput"
                    : field.DataSource.Trim();
                if (!SupportedFieldSources.Contains(dataSource))
                {
                    throw new ValidationException(
                        $"节点 {node.Code} 的字段 {field.Key} 数据来源无效。");
                }

                var sensitivity = string.IsNullOrWhiteSpace(field.Sensitivity)
                    ? "Internal"
                    : field.Sensitivity.Trim();
                if (!SupportedSensitivities.Contains(sensitivity))
                {
                    throw new ValidationException(
                        $"节点 {node.Code} 的字段 {field.Key} 敏感等级无效。");
                }

                var boundField = string.IsNullOrWhiteSpace(field.BoundObjectFieldCode)
                    ? null
                    : DomainChecks.RequiredCode(
                        field.BoundObjectFieldCode.Trim().ToUpperInvariant(),
                        "绑定业务对象字段",
                        80);
                if (dataSource == "BusinessObjectReference" && boundField is null)
                {
                    throw new ValidationException(
                        $"节点 {node.Code} 的字段 {field.Key} 必须绑定业务对象字段。");
                }

                return field with
                {
                    Key = DomainChecks.RequiredCode(
                        field.Key?.Trim().ToUpperInvariant(),
                        "字段编码",
                        80),
                    DisplayName = DomainChecks.Required(field.DisplayName, "字段显示名", 128),
                    DataType = dataType,
                    VisibleRoleCodes = (field.VisibleRoleCodes ?? [])
                        .Select(code => code.Trim().ToUpperInvariant())
                        .Where(code => code.Length > 0)
                        .Distinct(StringComparer.Ordinal)
                        .ToList(),
                    WriteRoleCodes = (field.WriteRoleCodes ?? [])
                        .Select(code => code.Trim().ToUpperInvariant())
                        .Where(code => code.Length > 0)
                        .Distinct(StringComparer.Ordinal)
                        .ToList(),
                    Options = (field.Options ?? [])
                        .Where(option => !string.IsNullOrWhiteSpace(option))
                        .Select(option => option.Trim())
                        .Distinct(StringComparer.Ordinal)
                        .Take(100)
                        .ToList(),
                    Width = string.IsNullOrWhiteSpace(field.Width) ? "Full" : field.Width.Trim(),
                    SortOrder = field.SortOrder == 0 ? index * 10 : field.SortOrder,
                    DataSource = dataSource,
                    BoundObjectFieldCode = boundField,
                    Sensitivity = sensitivity
                };
            }).ToList();
            var fieldKeys = fields.Select(field => field.Key).ToArray();
            if (fieldKeys.Distinct(StringComparer.Ordinal).Count() != fieldKeys.Length)
            {
                throw new ValidationException($"节点 {node.Code} 的字段编码重复。");
            }

            var actions = (node.Actions ?? []).Select(action => action with
            {
                Code = DomainChecks.RequiredCode(
                    action.Code?.Trim().ToUpperInvariant(),
                    "节点动作编码",
                    80),
                Name = DomainChecks.Required(action.Name, "节点动作名称", 128),
                ActionType = DomainChecks.RequiredCode(
                    action.ActionType?.Trim().ToUpperInvariant(),
                    "节点动作类型",
                    64)
            }).ToList();
            var aiReview = node.AiReview is null
                ? null
                : node.AiReview with
                {
                    RegisteredAction = string.IsNullOrWhiteSpace(node.AiReview.RegisteredAction)
                        ? null
                        : NormalizeAiAction(node.AiReview.RegisteredAction),
                    ReadableFieldKeys = (node.AiReview.ReadableFieldKeys ?? [])
                        .Where(key => fieldKeys.Contains(key, StringComparer.OrdinalIgnoreCase))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList(),
                    MaterialScopes = (node.AiReview.MaterialScopes ?? [])
                        .Where(item => item is "Attachment" or "Minutes" or "AgentRun" or "Audit")
                        .Distinct(StringComparer.Ordinal)
                        .ToList(),
                    OutputMode = "SuggestionOnly"
                };
            var permanentHumanBoundary = node.PermanentHumanBoundary
                || ShouldForceHumanBoundary(normalizedObjectType, node);
            return node with
            {
                Code = DomainChecks.RequiredCode(node.Code?.Trim().ToUpperInvariant(), "节点编码", 80),
                Name = DomainChecks.Required(node.Name, "节点名称", 128),
                AssigneeUserName = assigneeUserName,
                NextNode = string.IsNullOrWhiteSpace(node.NextNode)
                    ? null
                    : DomainChecks.RequiredCode(node.NextNode.Trim().ToUpperInvariant(), "下一节点", 80),
                AssigneeStrategy = strategy.ToString(),
                BusinessRoleCode = businessRoleCode,
                AgentProfileId = agentProfileId,
                BusinessDescription = NormalizeOptional(node.BusinessDescription, "节点业务说明", 2000),
                SlaMinutes = node.SlaMinutes is > 0 ? node.SlaMinutes : null,
                Deliverables = (node.Deliverables ?? [])
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Select(item => item.Trim())
                    .Distinct(StringComparer.Ordinal)
                    .Take(50)
                    .ToList(),
                RelatedRoleCodes = (node.RelatedRoleCodes ?? [])
                    .Select(item => item.Trim().ToUpperInvariant())
                    .Where(item => item.Length > 0)
                    .Distinct(StringComparer.Ordinal)
                    .ToList(),
                RequiredAttachmentCount = Math.Max(
                    node.RequiredAttachmentCount,
                    fields.Count(field => field.DataType == "Attachment" && field.Required)),
                CompletionCondition = NormalizeOptional(
                    node.CompletionCondition,
                    "完成条件",
                    2000),
                Fields = fields,
                Actions = actions,
                PermanentHumanBoundary = permanentHumanBoundary,
                AiReview = aiReview,
                Notification = node.Notification ?? new WorkflowNotificationRequest(),
                FormSchemaVersion = string.IsNullOrWhiteSpace(node.FormSchemaVersion)
                    ? "1.0"
                    : node.FormSchemaVersion.Trim(),
                Condition = node.Condition is null
                    ? null
                    : new WorkflowConfigurationConditionRequest(
                        DomainChecks.Required(node.Condition.Field, "条件字段", 64),
                        DomainChecks.Required(node.Condition.Operator, "条件操作符", 32),
                        DomainChecks.Required(node.Condition.Value, "条件值", 255))
            };
        }).ToList();
        return new WorkflowConfigurationPayload(normalizedObjectType, normalizedStartNode, normalizedNodes);
    }

    private static WorkflowConfigurationPayload DeserializePayload(string json)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<WorkflowConfigurationPayload>(
                json,
                JsonSerializerOptions.Web);
            if (payload is null)
            {
                throw new ValidationException("流程配置内容为空。");
            }

            return payload with
            {
                ObjectTypeCode = payload.ObjectTypeCode?.Trim().ToUpperInvariant() ?? string.Empty,
                StartNode = payload.StartNode?.Trim().ToUpperInvariant() ?? string.Empty,
                Nodes = payload.Nodes ?? []
            };
        }
        catch (JsonException exception)
        {
            throw new ValidationException($"流程配置 JSON 无效：{exception.Message}");
        }
    }

    private static WorkflowConfigurationPayload DeserializePayloadOrEmpty(string json)
    {
        try
        {
            return DeserializePayload(json);
        }
        catch (ValidationException)
        {
            return new WorkflowConfigurationPayload(string.Empty, string.Empty, []);
        }
    }

    private static string? NormalizeOptional(string? value, string fieldName, int maxLength) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : DomainChecks.Required(value, fieldName, maxLength);

    private static string NormalizeAiAction(string? value)
    {
        var normalized = value?.Trim();
        var matched = SupportedAiActions.FirstOrDefault(item =>
            string.Equals(item, normalized, StringComparison.OrdinalIgnoreCase));
        return matched ?? throw new ValidationException(
            "AI 评论只能选择完整性检查、风险识别、生成审查意见或生成草稿。");
    }

    private static bool EvaluateCondition(
        WorkflowConfigurationConditionRequest condition,
        IReadOnlyDictionary<string, string?> fields)
    {
        if (!fields.TryGetValue(condition.Field, out var raw)
            || string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        return condition.Operator switch
        {
            "equals" => string.Equals(raw, condition.Value, StringComparison.OrdinalIgnoreCase),
            "notEquals" => !string.Equals(raw, condition.Value, StringComparison.OrdinalIgnoreCase),
            "greaterThan" => decimal.TryParse(raw, out var left)
                && decimal.TryParse(condition.Value, out var right)
                && left > right,
            "greaterThanOrEqual" => decimal.TryParse(raw, out var left)
                && decimal.TryParse(condition.Value, out var right)
                && left >= right,
            _ => false
        };
    }

    private static bool ShouldForceHumanBoundary(
        string objectTypeCode,
        WorkflowConfigurationNodeRequest node)
    {
        var protectedMarkers = new[]
        {
            "付款", "放行", "配方", "设备控制", "安全联锁", "合同承诺", "重大赔付",
            "PAYMENT", "QUALITY_RELEASE", "FORMULA", "RECIPE",
            "EQUIPMENT_CONTROL", "SAFETY", "INTERLOCK", "CONTRACT", "COMPENSATION"
        };
        var text = string.Join(
            " ",
            objectTypeCode,
            node.Name,
            node.BusinessDescription,
            node.CompletionCondition,
            string.Join(" ", node.Deliverables ?? []),
            string.Join(" ", (node.Actions ?? []).Select(action => action.Name)));
        return protectedMarkers.Any(marker =>
            text.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static void EnsureMutable(ConfigurationVersionEntity entity)
    {
        if (entity.State is not ("Draft" or "Validated"))
        {
            throw new ConflictException(ErrorCodes.Conflict, "只有草稿或已校验配置可以修改和发布。");
        }
    }

    private static void ValidateIdempotencyKey(string value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length is < 8 or > 128)
        {
            throw new ValidationException("配置写操作必须提供 8—128 位幂等键。");
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

    [GeneratedRegex("^[A-Z][A-Z0-9_-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex StableNodeCodeRegex();

    private sealed record WorkflowConfigurationPayload(
        string ObjectTypeCode,
        string StartNode,
        IReadOnlyList<WorkflowConfigurationNodeRequest> Nodes);

    private sealed record ConfigurationRisk(
        string RiskLevel,
        bool RequiresPlatformApproval,
        IReadOnlyList<string> Reasons);
}
