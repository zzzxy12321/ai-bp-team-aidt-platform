using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Application.UseCases;
using AIDT.Domain.Audit;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;
using AIDT.Domain.Integrations;
using AIDT.Domain.Scenarios;
using AIDT.Infrastructure.Configuration;
using AIDT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Services;

public sealed partial class SqlScenarioService(
    AidtDbContext dbContext,
    CreateBusinessObjectHandler createBusinessObjectHandler,
    StartWorkflowInstanceHandler startWorkflowInstanceHandler,
    StartAgentRunHandler startAgentRunHandler,
    SqlEventInboxService eventInboxService,
    SqlProjectService projectService,
    SqlFileCenterService fileCenterService,
    IMinutesService minutesService,
    IAuditRepository auditRepository,
    IOutboxRepository outboxRepository,
    IIdempotencyStore idempotencyStore,
    IUnitOfWork unitOfWork,
    IClock clock,
    DataEnvironmentOptions environmentOptions) : IScenarioService
{
    private const string ModuleName = "Scenario";
    private static readonly JsonSerializerOptions ScenarioJsonOptions = CreateScenarioJsonOptions();

    public async Task<PagedResult<ScenarioDto>> ListVisibleAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        bool includeTestData,
        CancellationToken cancellationToken)
    {
        EnsureRead(user);
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var definitions = await dbContext.ScenarioDefinitions.AsNoTracking()
            .Where(item =>
                item.OrganizationId == user.OrganizationId
                && item.LifecycleStatus == nameof(ScenarioLifecycleStatus.Published)
                && item.CurrentPublishedVersionId != null)
            .OrderBy(item => item.Name)
            .ThenBy(item => item.Code)
            .ToListAsync(cancellationToken);
        var versionIds = definitions
            .Select(item => item.CurrentPublishedVersionId!.Value)
            .ToArray();
        var versions = await dbContext.ScenarioVersions.AsNoTracking()
            .Where(item => versionIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var departments = await LoadDepartmentsAsync(user.OrganizationId, cancellationToken);
        var users = await LoadUserNamesAsync(
            definitions.SelectMany(item => new Guid?[] { item.CreatedBy, item.UpdatedBy, item.DisabledBy })
                .Where(item => item.HasValue)
                .Select(item => item!.Value)
                .Distinct()
                .ToArray(),
            cancellationToken);
        var manageableDepartments = await LoadManageableDepartmentIdsAsync(user, cancellationToken);
        var pendingApprovals = await LoadPendingApprovalsAsync(
            user.OrganizationId,
            cancellationToken);
        var visible = new List<ScenarioDto>();
        foreach (var definition in definitions)
        {
            if (!versions.TryGetValue(definition.CurrentPublishedVersionId!.Value, out var version))
            {
                continue;
            }

            var payload = DeserializePayload(version.PayloadJson);
            if (!IsDepartmentApplicable(user, definition, payload, departments))
            {
                continue;
            }

            visible.Add(MapScenario(
                definition,
                version,
                null,
                departments,
                users,
                manageableDepartments.Contains(definition.OwnerDepartmentId),
                pendingApprovals
                    .FirstOrDefault(item =>
                        item.ScenarioId == definition.Id
                        && item.VersionId == version.Id)?.RequestId,
                pendingApprovals
                    .FirstOrDefault(item =>
                        item.ScenarioId == definition.Id
                        && item.VersionId == version.Id)?.Status));
        }

        var total = visible.Count;
        return new PagedResult<ScenarioDto>(
            visible.Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .ToList(),
            normalizedPage,
            normalizedPageSize,
            total);
    }

    public async Task<ScenarioDto?> GetVisibleAsync(
        UserPrincipal user,
        Guid scenarioId,
        CancellationToken cancellationToken)
    {
        EnsureRead(user);
        var definition = await dbContext.ScenarioDefinitions.AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.Id == scenarioId
                && item.OrganizationId == user.OrganizationId,
                cancellationToken);
        if (definition is null || definition.CurrentPublishedVersionId is null)
        {
            return null;
        }

        var version = await dbContext.ScenarioVersions.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == definition.CurrentPublishedVersionId.Value,
            cancellationToken);
        if (version is null)
        {
            return null;
        }

        var payload = DeserializePayload(version.PayloadJson);
        var departments = await LoadDepartmentsAsync(user.OrganizationId, cancellationToken);
        if (definition.LifecycleStatus == nameof(ScenarioLifecycleStatus.Disabled)
            || !IsDepartmentApplicable(user, definition, payload, departments))
        {
            return null;
        }

        var users = await LoadUserNamesAsync(
            new[] { definition.CreatedBy, definition.UpdatedBy, definition.DisabledBy }
                .Where(item => item.HasValue)
                .Select(item => item!.Value)
                .Distinct()
                .ToArray(),
            cancellationToken);
        var canManage = (await LoadManageableDepartmentIdsAsync(user, cancellationToken))
            .Contains(definition.OwnerDepartmentId);
        var pending = (await LoadPendingApprovalsAsync(
                user.OrganizationId,
                cancellationToken))
            .FirstOrDefault(item =>
                item.ScenarioId == definition.Id
                && item.VersionId == version.Id);
        return MapScenario(
            definition,
            version,
            null,
            departments,
            users,
            canManage,
            pending?.RequestId,
            pending?.Status);
    }

    public async Task<PagedResult<ScenarioDto>> ListManageAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        string? status,
        string? search,
        CancellationToken cancellationToken)
    {
        EnsureRead(user);
        var manageableDepartments = await LoadManageableDepartmentIdsAsync(user, cancellationToken);
        var pendingApprovals = await LoadPendingApprovalsAsync(
            user.OrganizationId,
            cancellationToken);
        if (manageableDepartments.Count == 0)
        {
            return new PagedResult<ScenarioDto>([], Math.Max(1, page), Math.Clamp(pageSize, 1, 100), 0);
        }

        var query = dbContext.ScenarioDefinitions.AsNoTracking()
            .Where(item =>
                item.OrganizationId == user.OrganizationId
                && manageableDepartments.Contains(item.OwnerDepartmentId));
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(item => item.LifecycleStatus == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(item => item.Code.Contains(term) || item.Name.Contains(term));
        }

        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        query = query.OrderByDescending(item => item.UpdatedAt).ThenByDescending(item => item.Id);
        var total = await query.LongCountAsync(cancellationToken);
        var definitions = await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);
        var scenarioIds = definitions.Select(item => item.Id).ToArray();
        var versions = await dbContext.ScenarioVersions.AsNoTracking()
            .Where(item => scenarioIds.Contains(item.ScenarioId))
            .ToListAsync(cancellationToken);
        var departments = await LoadDepartmentsAsync(user.OrganizationId, cancellationToken);
        var users = await LoadUserNamesAsync(
            definitions.SelectMany(item => new Guid?[] { item.CreatedBy, item.UpdatedBy, item.DisabledBy })
                .Where(item => item.HasValue)
                .Select(item => item!.Value)
                .Distinct()
                .ToArray(),
            cancellationToken);
        var items = definitions.Select(definition =>
        {
            var scenarioVersions = versions.Where(item => item.ScenarioId == definition.Id).ToList();
            var current = definition.CurrentPublishedVersionId.HasValue
                ? scenarioVersions.SingleOrDefault(item => item.Id == definition.CurrentPublishedVersionId.Value)
                : null;
            var draft = scenarioVersions
                .Where(item => item.Status is nameof(ScenarioVersionStatus.Draft)
                    or nameof(ScenarioVersionStatus.Validated))
                .OrderByDescending(item => item.VersionNumber)
                .FirstOrDefault();
            var pending = pendingApprovals.FirstOrDefault(item =>
                item.ScenarioId == definition.Id
                && item.VersionId == draft?.Id);
            return MapScenario(
                definition,
                current,
                draft,
                departments,
                users,
                manageableDepartments.Contains(definition.OwnerDepartmentId),
                pending?.RequestId,
                pending?.Status);
        }).ToList();
        return new PagedResult<ScenarioDto>(
            items,
            normalizedPage,
            normalizedPageSize,
            total);
    }

    public async Task<ScenarioDto> CreateDraftAsync(
        UserPrincipal user,
        CreateScenarioDraftRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureScenarioEditorAsync(
            user,
            request.OwnerDepartmentId,
            AccessAction.Create,
            cancellationToken);
        ValidateIdempotencyKey(idempotencyKey);
        var hash = Hash(request);
        var scope = $"scenario:create:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(
                    ErrorCodes.Conflict,
                    "同一幂等键不能用于不同场景草稿。");
            }

            return await LoadManagedScenarioAsync(
                    user,
                    Guid.Parse(existing.ResponseId),
                    cancellationToken)
                ?? throw new NotFoundException("幂等结果对应的场景不存在。");
        }

        var code = DomainChecks.RequiredCode(request.Code.ToUpperInvariant(), "场景编码", 80);
        if (await dbContext.ScenarioDefinitions.AnyAsync(
                item => item.OrganizationId == user.OrganizationId && item.Code == code,
                cancellationToken))
        {
            throw new ConflictException(
                ErrorCodes.Conflict,
                "场景编码已存在，请使用不同的稳定编码。");
        }

        await EnsurePayloadReferencesReadableAsync(
            user,
            request.OwnerDepartmentId,
            request.Payload,
            cancellationToken);
        var now = clock.UtcNow;
        var definition = new ScenarioDefinitionEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = user.OrganizationId,
            OwnerDepartmentId = request.OwnerDepartmentId,
            Code = code,
            Name = DomainChecks.Required(request.Name, "场景名称", 160),
            Description = DomainChecks.Required(request.Description, "场景说明", 2000),
            LifecycleStatus = nameof(ScenarioLifecycleStatus.Draft),
            Version = 1,
            Revision = 1,
            CreatedBy = user.UserId,
            CreatedAt = now,
            UpdatedBy = user.UserId,
            UpdatedAt = now
        };
        var version = new ScenarioVersionEntity
        {
            Id = Guid.NewGuid(),
            ScenarioId = definition.Id,
            VersionNumber = 1,
            Status = nameof(ScenarioVersionStatus.Draft),
            Revision = 1,
            PayloadJson = Serialize(request.Payload),
            ChangeSummary = "初始草稿",
            CreatedBy = user.UserId,
            CreatedAt = now,
            UpdatedBy = user.UserId,
            UpdatedAt = now
        };

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.ScenarioDefinitions.Add(definition);
            dbContext.ScenarioVersions.Add(version);
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    ModuleName,
                    "scenario.draft_created",
                    "Scenario",
                    definition.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: JsonSerializer.Serialize(
                        new { definition.Code, definition.Name, definition.OwnerDepartmentId },
                        ScenarioJsonOptions)),
                cancellationToken);
            await outboxRepository.AddAsync(
                OutboxMessage.Create(
                    "Scenario.DraftCreated",
                    "Scenario",
                    definition.Id,
                    $"scenario-draft-created:{definition.Id:D}:{version.VersionNumber}",
                    JsonSerializer.Serialize(
                        new { scenarioId = definition.Id, versionId = version.Id, definition.Code },
                        ScenarioJsonOptions),
                    now),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "Scenario",
                    definition.Id.ToString("D"),
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

        return await LoadManagedScenarioAsync(user, definition.Id, cancellationToken)
            ?? throw new InvalidOperationException("场景草稿保存后不可读取。");
    }

    public async Task<ScenarioDto> CreateNextDraftAsync(
        UserPrincipal user,
        Guid scenarioId,
        CreateNextScenarioVersionDraftRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var definition = await LoadTrackedManagedDefinitionAsync(
            user,
            scenarioId,
            AccessAction.Update,
            cancellationToken);
        if (definition.Version != request.ExpectedVersion)
        {
            throw new ConflictException(
                ErrorCodes.ScenarioVersionConflict,
                "场景版本已变化，请刷新后重试。");
        }

        var versions = await dbContext.ScenarioVersions
            .Where(item => item.ScenarioId == scenarioId)
            .OrderByDescending(item => item.VersionNumber)
            .ToListAsync(cancellationToken);
        if (versions.Any(item => item.Status is nameof(ScenarioVersionStatus.Draft)
            or nameof(ScenarioVersionStatus.Validated)))
        {
            throw new ConflictException(
                ErrorCodes.ScenarioVersionConflict,
                "当前场景已有未发布草稿，请先编辑或发布该草稿。");
        }

        var baseVersion = definition.CurrentPublishedVersionId.HasValue
            ? versions.SingleOrDefault(item => item.Id == definition.CurrentPublishedVersionId.Value)
            : versions.FirstOrDefault();
        if (baseVersion is null)
        {
            throw new ConflictException(
                ErrorCodes.ScenarioNotPublished,
                "当前场景没有可作为修改基线的发布版本。");
        }

        ValidateIdempotencyKey(idempotencyKey);
        var scope = $"scenario:next-draft:{scenarioId:D}:{user.UserId:D}";
        var hash = Hash(new { scenarioId, request.ExpectedVersion });
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同场景草稿。");
            }

            return await LoadManagedScenarioAsync(user, scenarioId, cancellationToken)
                ?? throw new NotFoundException("幂等结果对应的场景不存在。");
        }

        var now = clock.UtcNow;
        var next = new ScenarioVersionEntity
        {
            Id = Guid.NewGuid(),
            ScenarioId = scenarioId,
            VersionNumber = versions.Max(item => item.VersionNumber) + 1,
            Status = nameof(ScenarioVersionStatus.Draft),
            Revision = 1,
            PayloadJson = baseVersion.PayloadJson,
            ChangeSummary = DomainChecks.Required(request.ChangeSummary, "版本变更说明", 1000),
            CreatedBy = user.UserId,
            CreatedAt = now,
            UpdatedBy = user.UserId,
            UpdatedAt = now
        };
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.ScenarioVersions.Add(next);
            definition.UpdatedBy = user.UserId;
            definition.UpdatedAt = now;
            definition.Revision++;
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    ModuleName,
                    "scenario.next_draft_created",
                    "Scenario",
                    scenarioId.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: $"{{\"versionNumber\":{next.VersionNumber},\"baseVersion\":{baseVersion.VersionNumber}}}"),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "ScenarioVersion",
                    next.Id.ToString("D"),
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

        return await LoadManagedScenarioAsync(user, scenarioId, cancellationToken)
            ?? throw new InvalidOperationException("新草稿保存后不可读取。");
    }

    public async Task<ScenarioDto> UpdateDraftAsync(
        UserPrincipal user,
        Guid scenarioId,
        Guid versionId,
        UpdateScenarioDraftRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var definition = await LoadTrackedManagedDefinitionAsync(
            user,
            scenarioId,
            AccessAction.Update,
            cancellationToken);
        var version = await dbContext.ScenarioVersions.SingleOrDefaultAsync(
            item => item.Id == versionId && item.ScenarioId == scenarioId,
            cancellationToken) ?? throw new NotFoundException("场景草稿版本不存在。");
        ScenarioRules.EnsureVersionEditable(
            ScenarioRules.ParseVersionStatus(version.Status),
            user.IsServiceIdentity);
        if (version.Revision != request.ExpectedRevision)
        {
            throw new ConflictException(
                ErrorCodes.ScenarioVersionConflict,
                "场景草稿已变化，请刷新后重试。");
        }

        await EnsureScenarioEditorAsync(
            user,
            request.OwnerDepartmentId,
            AccessAction.Update,
            cancellationToken);
        await EnsurePayloadReferencesReadableAsync(
            user,
            request.OwnerDepartmentId,
            request.Payload,
            cancellationToken);
        ValidateIdempotencyKey(idempotencyKey);
        var hash = Hash(new { scenarioId, versionId, request.ExpectedRevision, request });
        var scope = $"scenario:update:{scenarioId:D}:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同场景编辑。");
            }

            return await LoadManagedScenarioAsync(user, scenarioId, cancellationToken)
                ?? throw new NotFoundException("幂等结果对应的场景不存在。");
        }

        var now = clock.UtcNow;
        var before = JsonSerializer.Serialize(
            new { definition.Name, definition.Description, version.PayloadJson },
            ScenarioJsonOptions);
        definition.Name = DomainChecks.Required(request.Name, "场景名称", 160);
        definition.Description = DomainChecks.Required(request.Description, "场景说明", 2000);
        definition.OwnerDepartmentId = request.OwnerDepartmentId;
        definition.UpdatedBy = user.UserId;
        definition.UpdatedAt = now;
        definition.Revision++;
        version.PayloadJson = Serialize(request.Payload);
        version.Status = nameof(ScenarioVersionStatus.Draft);
        version.Revision++;
        version.ValidationJson = null;
        version.ReferenceSnapshotJson = null;
        version.UpdatedBy = user.UserId;
        version.UpdatedAt = now;
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    ModuleName,
                    "scenario.draft_updated",
                    "Scenario",
                    scenarioId.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    before,
                    JsonSerializer.Serialize(
                        new { definition.Name, definition.Description, version.PayloadJson },
                        ScenarioJsonOptions)),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "ScenarioVersion",
                    version.Id.ToString("D"),
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

        return await LoadManagedScenarioAsync(user, scenarioId, cancellationToken)
            ?? throw new InvalidOperationException("场景草稿更新后不可读取。");
    }

    public async Task<ScenarioValidationDto> ValidateAsync(
        UserPrincipal user,
        Guid scenarioId,
        Guid versionId,
        ScenarioActionRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var definition = await LoadTrackedManagedDefinitionAsync(
            user,
            scenarioId,
            AccessAction.Manage,
            cancellationToken);
        var version = await LoadDraftVersionAsync(scenarioId, versionId, cancellationToken);
        if (definition.Version != request.ExpectedVersion)
        {
            throw new ConflictException(ErrorCodes.ScenarioVersionConflict, "场景版本已变化，请刷新后重试。");
        }

        ValidateIdempotencyKey(idempotencyKey);
        var scope = $"scenario:validate:{scenarioId:D}:{user.UserId:D}";
        var hash = Hash(new { scenarioId, versionId, request.ExpectedVersion, version.Revision });
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同场景校验。");
            }

            return DeserializeValidation(version.ValidationJson)
                ?? throw new ConflictException(ErrorCodes.Conflict, "场景校验结果不可用，请重新校验。");
        }

        var payload = DeserializePayload(version.PayloadJson);
        var validation = await ValidatePayloadAsync(
            user,
            definition.OwnerDepartmentId,
            payload,
            cancellationToken);
        var now = clock.UtcNow;
        version.ValidationJson = Serialize(validation);
        version.ReferenceSnapshotJson = validation.References is null
            ? null
            : Serialize(validation.References);
        version.Status = validation.IsValid
            ? nameof(ScenarioVersionStatus.Validated)
            : nameof(ScenarioVersionStatus.Draft);
        version.ValidatedBy = user.UserId;
        version.ValidatedAt = now;
        version.UpdatedBy = user.UserId;
        version.UpdatedAt = now;
        version.Revision++;
        definition.UpdatedBy = user.UserId;
        definition.UpdatedAt = now;
        definition.Revision++;
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    ModuleName,
                    "scenario.validated",
                    "Scenario",
                    scenarioId.ToString("D"),
                    traceId,
                    validation.IsValid ? AuditOutcome.Succeeded : AuditOutcome.Failed,
                    now,
                    after: Serialize(validation)),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "ScenarioValidation",
                    version.Id.ToString("D"),
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

        return validation;
    }

    public async Task<ScenarioDto> PublishAsync(
        UserPrincipal user,
        Guid scenarioId,
        Guid versionId,
        ScenarioActionRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var definition = await LoadTrackedManagedDefinitionAsync(
            user,
            scenarioId,
            AccessAction.Approve,
            cancellationToken);
        var version = await LoadDraftVersionAsync(scenarioId, versionId, cancellationToken);
        if (definition.Version != request.ExpectedVersion)
        {
            throw new ConflictException(ErrorCodes.ScenarioVersionConflict, "场景版本已变化，请刷新后重试。");
        }

        ValidateIdempotencyKey(idempotencyKey);
        var scope = $"scenario:publish:{scenarioId:D}:{user.UserId:D}";
        var hash = Hash(new { scenarioId, versionId, request.ExpectedVersion, request.Comment });
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同场景发布。");
            }

            return await LoadManagedScenarioAsync(user, scenarioId, cancellationToken)
                ?? throw new NotFoundException("幂等结果对应的场景不存在。");
        }

        var payload = DeserializePayload(version.PayloadJson);
        var validation = await ValidatePayloadAsync(
            user,
            definition.OwnerDepartmentId,
            payload,
            cancellationToken);
        ScenarioRules.EnsureCanPublish(
            validation.Errors,
            ScenarioRules.ParseVersionStatus(version.Status));
        if (validation.References is null)
        {
            throw new ConflictException(
                ErrorCodes.ScenarioReferenceInvalid,
                "场景引用快照生成失败，不能发布。");
        }

        AccessChangeRequestEntity? approvedRequest = null;
        if (IsHighRisk(payload))
        {
            approvedRequest = await FindApprovedApprovalAsync(
                user.OrganizationId,
                scenarioId,
                version.Id,
                user.UserId,
                cancellationToken);
            if (approvedRequest is null)
            {
                var pending = await FindPendingApprovalAsync(
                    user.OrganizationId,
                    scenarioId,
                    version.Id,
                    cancellationToken);
                if (pending is null)
                {
                    var nowPending = clock.UtcNow;
                    pending = new AccessChangeRequestEntity
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = user.OrganizationId,
                        RequestType = "ScenarioPublish",
                        PayloadJson = Serialize(new ScenarioPublishApprovalPayload(
                            scenarioId,
                            version.Id,
                            request.ExpectedVersion,
                            request.Comment,
                            version.VersionNumber)),
                        RiskReason = "场景涉及采购付款或质量放行相关能力，发布需要第二位平台管理员审核。",
                        Status = "Pending",
                        RequestedBy = user.UserId,
                        RequestedAt = nowPending,
                        IdempotencyKey = idempotencyKey
                    };
                    dbContext.AccessChangeRequests.Add(pending);
                    await auditRepository.AddAsync(
                        AuditEvent.Record(
                            user.UserId,
                            ModuleName,
                            "scenario.publish_approval_requested",
                            "Scenario",
                            scenarioId.ToString("D"),
                            traceId,
                            AuditOutcome.Succeeded,
                            nowPending,
                            after: pending.PayloadJson),
                        cancellationToken);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                return await LoadManagedScenarioAsync(user, scenarioId, cancellationToken)
                    ?? throw new InvalidOperationException("场景审批申请保存后不可读取。");
            }
        }

        var now = clock.UtcNow;
        var previousPublished = definition.CurrentPublishedVersionId.HasValue
            ? await dbContext.ScenarioVersions.SingleOrDefaultAsync(
                item => item.Id == definition.CurrentPublishedVersionId.Value,
                cancellationToken)
            : null;
        if (previousPublished is not null)
        {
            previousPublished.Status = nameof(ScenarioVersionStatus.Retired);
            previousPublished.RetiredBy = user.UserId;
            previousPublished.RetiredAt = now;
        }

        version.Status = nameof(ScenarioVersionStatus.Published);
        version.ValidationJson = Serialize(validation);
        version.ReferenceSnapshotJson = Serialize(validation.References);
        version.ImpactAnalysisJson = Serialize(BuildImpactAnalysis(payload));
        version.ValidatedBy = user.UserId;
        version.ValidatedAt = now;
        version.PublishedBy = user.UserId;
        version.PublishedAt = now;
        version.UpdatedBy = user.UserId;
        version.UpdatedAt = now;
        version.Revision++;
        definition.LifecycleStatus = nameof(ScenarioLifecycleStatus.Published);
        definition.CurrentPublishedVersionId = version.Id;
        definition.Version++;
        definition.Revision++;
        definition.UpdatedBy = user.UserId;
        definition.UpdatedAt = now;
        definition.DisabledBy = null;
        definition.DisabledAt = null;
        definition.DisabledReason = null;
        if (approvedRequest is not null)
        {
            approvedRequest.Status = "Applied";
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    ModuleName,
                    "scenario.published",
                    "Scenario",
                    scenarioId.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    before: previousPublished is null
                        ? null
                        : $"{{\"previousVersion\":{previousPublished.VersionNumber}}}",
                    after: JsonSerializer.Serialize(
                        new
                        {
                            versionNumber = version.VersionNumber,
                            version.ReferenceSnapshotJson,
                            Comment = request.Comment
                        },
                        ScenarioJsonOptions)),
                cancellationToken);
            await outboxRepository.AddAsync(
                OutboxMessage.Create(
                    "Scenario.Published",
                    "Scenario",
                    scenarioId,
                    $"scenario-published:{scenarioId:D}:{version.VersionNumber}",
                    JsonSerializer.Serialize(
                        new
                        {
                            scenarioId,
                            versionId = version.Id,
                            versionNumber = version.VersionNumber,
                            definition.Code
                        },
                        ScenarioJsonOptions),
                    now),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "ScenarioVersion",
                    version.Id.ToString("D"),
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

        return await LoadManagedScenarioAsync(user, scenarioId, cancellationToken)
            ?? throw new InvalidOperationException("场景发布后不可读取。");
    }

    public async Task<ScenarioDto> DisableAsync(
        UserPrincipal user,
        Guid scenarioId,
        ScenarioActionRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var definition = await LoadTrackedManagedDefinitionAsync(
            user,
            scenarioId,
            AccessAction.Manage,
            cancellationToken);
        if (definition.Version != request.ExpectedVersion)
        {
            throw new ConflictException(ErrorCodes.ScenarioVersionConflict, "场景版本已变化，请刷新后重试。");
        }

        if (await IsSimpleActionReplayAsync(
                user,
                definition,
                "scenario.disabled",
                request.ExpectedVersion,
                request.Comment,
                idempotencyKey,
                cancellationToken))
        {
            return await LoadManagedScenarioAsync(user, scenarioId, cancellationToken)
                ?? throw new NotFoundException("场景不存在。");
        }

        if (definition.LifecycleStatus == nameof(ScenarioLifecycleStatus.Disabled))
        {
            return await LoadManagedScenarioAsync(user, scenarioId, cancellationToken)
                ?? throw new NotFoundException("场景不存在。");
        }

        var reason = DomainChecks.Required(request.Comment, "停用原因", 1000);
        var now = clock.UtcNow;
        definition.LifecycleStatus = nameof(ScenarioLifecycleStatus.Disabled);
        definition.DisabledBy = user.UserId;
        definition.DisabledAt = now;
        definition.DisabledReason = reason;
        definition.Version++;
        definition.Revision++;
        definition.UpdatedBy = user.UserId;
        definition.UpdatedAt = now;
        await CompleteSimpleActionAsync(
            user,
            definition,
            "scenario.disabled",
            request.ExpectedVersion,
            reason,
            idempotencyKey,
            traceId,
            cancellationToken);
        return await LoadManagedScenarioAsync(user, scenarioId, cancellationToken)
            ?? throw new InvalidOperationException("场景停用后不可读取。");
    }

    public async Task<ScenarioDto> RestoreAsync(
        UserPrincipal user,
        Guid scenarioId,
        ScenarioActionRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var definition = await LoadTrackedManagedDefinitionAsync(
            user,
            scenarioId,
            AccessAction.Manage,
            cancellationToken);
        if (definition.Version != request.ExpectedVersion)
        {
            throw new ConflictException(ErrorCodes.ScenarioVersionConflict, "场景版本已变化，请刷新后重试。");
        }

        if (await IsSimpleActionReplayAsync(
                user,
                definition,
                "scenario.restored",
                request.ExpectedVersion,
                request.Comment,
                idempotencyKey,
                cancellationToken))
        {
            return await LoadManagedScenarioAsync(user, scenarioId, cancellationToken)
                ?? throw new NotFoundException("场景不存在。");
        }

        if (!definition.CurrentPublishedVersionId.HasValue)
        {
            throw new ConflictException(
                ErrorCodes.ScenarioNotPublished,
                "当前场景没有有效发布版本，不能恢复。");
        }

        var published = await dbContext.ScenarioVersions.SingleOrDefaultAsync(
            item => item.Id == definition.CurrentPublishedVersionId.Value,
            cancellationToken) ?? throw new ConflictException(
                ErrorCodes.ScenarioReferenceInvalid,
                "当前场景发布版本不存在，不能恢复。");
        var payload = DeserializePayload(published.PayloadJson);
        var validation = await ValidatePayloadAsync(
            user,
            definition.OwnerDepartmentId,
            payload,
            cancellationToken);
        if (!validation.IsValid)
        {
            throw new ConflictException(
                ErrorCodes.ScenarioReferenceInvalid,
                $"场景引用已失效，不能恢复：{string.Join("；", validation.Errors.Take(5))}。");
        }

        var now = clock.UtcNow;
        definition.LifecycleStatus = nameof(ScenarioLifecycleStatus.Published);
        definition.DisabledBy = null;
        definition.DisabledAt = null;
        definition.DisabledReason = null;
        definition.Version++;
        definition.Revision++;
        definition.UpdatedBy = user.UserId;
        definition.UpdatedAt = now;
        await CompleteSimpleActionAsync(
            user,
            definition,
            "scenario.restored",
            request.ExpectedVersion,
            request.Comment,
            idempotencyKey,
            traceId,
            cancellationToken);
        return await LoadManagedScenarioAsync(user, scenarioId, cancellationToken)
            ?? throw new InvalidOperationException("场景恢复后不可读取。");
    }

    public async Task<IReadOnlyList<ScenarioVersionDto>> ListVersionsAsync(
        UserPrincipal user,
        Guid scenarioId,
        CancellationToken cancellationToken)
    {
        EnsureRead(user);
        var definition = await dbContext.ScenarioDefinitions.AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.Id == scenarioId
                && item.OrganizationId == user.OrganizationId,
                cancellationToken) ?? throw new NotFoundException("场景不存在。");
        var manageableDepartments = await LoadManageableDepartmentIdsAsync(user, cancellationToken);
        if (!manageableDepartments.Contains(definition.OwnerDepartmentId))
        {
            if (definition.LifecycleStatus != nameof(ScenarioLifecycleStatus.Published))
            {
                throw new PermissionException(ErrorCodes.PermissionDenied, "当前身份不能查看该场景版本。");
            }

            var payload = definition.CurrentPublishedVersionId.HasValue
                ? DeserializePayload((await dbContext.ScenarioVersions.AsNoTracking().SingleAsync(
                    item => item.Id == definition.CurrentPublishedVersionId.Value,
                    cancellationToken)).PayloadJson)
                : null;
            if (payload is null
                || !IsDepartmentApplicable(
                    user,
                    definition,
                    payload,
                    await LoadDepartmentsAsync(user.OrganizationId, cancellationToken)))
            {
                throw new PermissionException(ErrorCodes.PermissionDenied, "当前身份不能查看该场景版本。");
            }
        }

        var versions = await dbContext.ScenarioVersions.AsNoTracking()
            .Where(item => item.ScenarioId == scenarioId)
            .OrderByDescending(item => item.VersionNumber)
            .ToListAsync(cancellationToken);
        var userIds = versions.SelectMany(item => new Guid?[]
            {
                item.CreatedBy, item.UpdatedBy, item.ValidatedBy,
                item.PublishedBy, item.RetiredBy
            })
            .Where(item => item.HasValue)
            .Select(item => item!.Value)
            .Distinct()
            .ToArray();
        var users = await LoadUserNamesAsync(userIds, cancellationToken);
        return versions.Select(item => MapVersion(item, users)).ToList();
    }

    public async Task<ScenarioDto> RollbackAsync(
        UserPrincipal user,
        Guid scenarioId,
        RollbackScenarioRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var definition = await LoadTrackedManagedDefinitionAsync(
            user,
            scenarioId,
            AccessAction.Approve,
            cancellationToken);
        if (definition.Version != request.ExpectedVersion)
        {
            throw new ConflictException(ErrorCodes.ScenarioVersionConflict, "场景版本已变化，请刷新后重试。");
        }

        ValidateIdempotencyKey(idempotencyKey);
        var rollbackScope = $"scenario:rollback:{scenarioId:D}:{user.UserId:D}";
        var rollbackHash = Hash(new
        {
            scenarioId,
            request.ExpectedVersion,
            request.TargetVersionNumber,
            request.Reason
        });
        var existingRollback = await idempotencyStore.FindAsync(
            rollbackScope,
            idempotencyKey,
            cancellationToken);
        if (existingRollback is not null)
        {
            if (!string.Equals(
                    existingRollback.RequestHash,
                    rollbackHash,
                    StringComparison.Ordinal))
            {
                throw new ConflictException(
                    ErrorCodes.Conflict,
                    "同一幂等键不能用于不同场景回滚。");
            }

            return await LoadManagedScenarioAsync(user, scenarioId, cancellationToken)
                ?? throw new NotFoundException("幂等结果对应的场景不存在。");
        }

        var versions = await dbContext.ScenarioVersions
            .Where(item => item.ScenarioId == scenarioId)
            .OrderByDescending(item => item.VersionNumber)
            .ToListAsync(cancellationToken);
        if (versions.Any(item => item.Status is nameof(ScenarioVersionStatus.Draft)
            or nameof(ScenarioVersionStatus.Validated)))
        {
            throw new ConflictException(
                ErrorCodes.ScenarioVersionConflict,
                "当前场景有未发布草稿，回滚前请先处理草稿。");
        }

        var target = versions.SingleOrDefault(item =>
            item.VersionNumber == request.TargetVersionNumber
            && item.Status is nameof(ScenarioVersionStatus.Published)
                or nameof(ScenarioVersionStatus.Retired))
            ?? throw new NotFoundException("目标场景历史版本不存在。");
        var payload = DeserializePayload(target.PayloadJson);
        var validation = await ValidatePayloadAsync(
            user,
            definition.OwnerDepartmentId,
            payload,
            cancellationToken);
        if (!validation.IsValid || validation.References is null)
        {
            throw new ConflictException(
                ErrorCodes.ScenarioReferenceInvalid,
                $"目标历史版本的引用已失效，不能回滚：{string.Join("；", validation.Errors.Take(5))}。");
        }

        var now = clock.UtcNow;
        var next = new ScenarioVersionEntity
        {
            Id = Guid.NewGuid(),
            ScenarioId = scenarioId,
            VersionNumber = versions.Max(item => item.VersionNumber) + 1,
            Status = nameof(ScenarioVersionStatus.Published),
            Revision = 1,
            PayloadJson = target.PayloadJson,
            ReferenceSnapshotJson = Serialize(validation.References),
            ValidationJson = Serialize(validation),
            ImpactAnalysisJson = Serialize(BuildImpactAnalysis(payload)),
            ChangeSummary = DomainChecks.Required(request.Reason, "回滚原因", 1000),
            CreatedBy = user.UserId,
            CreatedAt = now,
            UpdatedBy = user.UserId,
            UpdatedAt = now,
            ValidatedBy = user.UserId,
            ValidatedAt = now,
            PublishedBy = user.UserId,
            PublishedAt = now
        };
        var current = definition.CurrentPublishedVersionId.HasValue
            ? versions.SingleOrDefault(item => item.Id == definition.CurrentPublishedVersionId.Value)
            : null;
        if (current is not null)
        {
            current.Status = nameof(ScenarioVersionStatus.Retired);
            current.RetiredBy = user.UserId;
            current.RetiredAt = now;
        }

        definition.CurrentPublishedVersionId = next.Id;
        definition.LifecycleStatus = nameof(ScenarioLifecycleStatus.Published);
        definition.Version++;
        definition.Revision++;
        definition.UpdatedBy = user.UserId;
        definition.UpdatedAt = now;
        definition.DisabledBy = null;
        definition.DisabledAt = null;
        definition.DisabledReason = null;
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.ScenarioVersions.Add(next);
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    ModuleName,
                    "scenario.rolled_back",
                    "Scenario",
                    scenarioId.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: $"{{\"sourceVersion\":{target.VersionNumber},\"newVersion\":{next.VersionNumber}}}",
                    reason: next.ChangeSummary),
                cancellationToken);
            await outboxRepository.AddAsync(
                OutboxMessage.Create(
                    "Scenario.RolledBack",
                    "Scenario",
                    scenarioId,
                    $"scenario-rolled-back:{scenarioId:D}:{next.VersionNumber}",
                    JsonSerializer.Serialize(
                        new
                        {
                            scenarioId,
                            sourceVersion = target.VersionNumber,
                            newVersion = next.VersionNumber
                        },
                        ScenarioJsonOptions),
                    now),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    rollbackScope,
                    idempotencyKey,
                    rollbackHash,
                    "ScenarioVersion",
                    next.Id.ToString("D"),
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

        return await LoadManagedScenarioAsync(user, scenarioId, cancellationToken)
            ?? throw new InvalidOperationException("场景回滚后不可读取。");
    }

    private async Task CompleteSimpleActionAsync(
        UserPrincipal user,
        ScenarioDefinitionEntity definition,
        string action,
        long requestVersion,
        string? comment,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ValidateIdempotencyKey(idempotencyKey);
        var scope = $"scenario:{action}:{definition.Id:D}:{user.UserId:D}";
        var hash = Hash(new { definition.Id, action, requestVersion, comment });
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同场景动作。");
            }

            return;
        }

        var now = clock.UtcNow;
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    ModuleName,
                    action,
                    "Scenario",
                    definition.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    reason: comment),
                cancellationToken);
            await outboxRepository.AddAsync(
                OutboxMessage.Create(
                    $"Scenario.{action.Split('.').Last()}",
                    "Scenario",
                    definition.Id,
                    $"scenario:{action}:{definition.Id:D}:{definition.Version}",
                    JsonSerializer.Serialize(
                        new { scenarioId = definition.Id, definition.Code, definition.Version },
                        ScenarioJsonOptions),
                    now),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "Scenario",
                    definition.Id.ToString("D"),
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
    }

    private async Task<bool> IsSimpleActionReplayAsync(
        UserPrincipal user,
        ScenarioDefinitionEntity definition,
        string action,
        long requestVersion,
        string? comment,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        ValidateIdempotencyKey(idempotencyKey);
        var scope = $"scenario:{action}:{definition.Id:D}:{user.UserId:D}";
        var hash = Hash(new
        {
            definition.Id,
            action,
            requestVersion,
            comment
        });
        var existing = await idempotencyStore.FindAsync(
            scope,
            idempotencyKey,
            cancellationToken);
        if (existing is null)
        {
            return false;
        }

        if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
        {
            throw new ConflictException(
                ErrorCodes.Conflict,
                "同一幂等键不能用于不同场景动作。");
        }

        return true;
    }

    private async Task<ScenarioDto?> LoadManagedScenarioAsync(
        UserPrincipal user,
        Guid scenarioId,
        CancellationToken cancellationToken)
    {
        var definitions = await dbContext.ScenarioDefinitions.AsNoTracking()
            .Where(item => item.Id == scenarioId && item.OrganizationId == user.OrganizationId)
            .ToListAsync(cancellationToken);
        if (definitions.Count == 0)
        {
            return null;
        }

        var versions = await dbContext.ScenarioVersions.AsNoTracking()
            .Where(item => item.ScenarioId == scenarioId)
            .ToListAsync(cancellationToken);
        var definition = definitions.Single();
        var current = definition.CurrentPublishedVersionId.HasValue
            ? versions.SingleOrDefault(item => item.Id == definition.CurrentPublishedVersionId.Value)
            : null;
        var draft = versions
            .Where(item => item.Status is nameof(ScenarioVersionStatus.Draft)
                or nameof(ScenarioVersionStatus.Validated))
            .OrderByDescending(item => item.VersionNumber)
            .FirstOrDefault();
        var users = await LoadUserNamesAsync(
            new[] { definition.CreatedBy, definition.UpdatedBy, definition.DisabledBy }
                .Where(item => item.HasValue)
                .Select(item => item!.Value)
                .Distinct()
                .ToArray(),
            cancellationToken);
        var departments = await LoadDepartmentsAsync(user.OrganizationId, cancellationToken);
        var manageable = (await LoadManageableDepartmentIdsAsync(user, cancellationToken))
            .Contains(definition.OwnerDepartmentId);
        var pending = (await LoadPendingApprovalsAsync(
                user.OrganizationId,
                cancellationToken))
            .FirstOrDefault(item =>
                item.ScenarioId == definition.Id
                && item.VersionId == draft?.Id);
        return MapScenario(
            definition,
            current,
            draft,
            departments,
            users,
            manageable,
            pending?.RequestId,
            pending?.Status);
    }

    private async Task<ScenarioDefinitionEntity> LoadTrackedManagedDefinitionAsync(
        UserPrincipal user,
        Guid scenarioId,
        AccessAction action,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("scenario", action);
        user.EnsureOrganizationAccess(user.OrganizationId);
        if (user.IsServiceIdentity)
        {
            throw new PermissionException(
                ErrorCodes.PermissionDenied,
                "数字员工服务身份不能治理或启动业务场景。");
        }

        var definition = await dbContext.ScenarioDefinitions.SingleOrDefaultAsync(
            item => item.Id == scenarioId && item.OrganizationId == user.OrganizationId,
            cancellationToken) ?? throw new NotFoundException("场景不存在。");
        if (!await CanManageDepartmentAsync(user, definition.OwnerDepartmentId, cancellationToken))
        {
            throw new PermissionException(
                ErrorCodes.DepartmentScopeDenied,
                "当前身份无权管理该部门场景。");
        }

        return definition;
    }

    private async Task<ScenarioVersionEntity> LoadDraftVersionAsync(
        Guid scenarioId,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var version = await dbContext.ScenarioVersions.SingleOrDefaultAsync(
            item => item.Id == versionId && item.ScenarioId == scenarioId,
            cancellationToken) ?? throw new NotFoundException("场景草稿版本不存在。");
        ScenarioRules.EnsureVersionEditable(
            ScenarioRules.ParseVersionStatus(version.Status),
            isServiceIdentity: false);
        return version;
    }

    private async Task EnsureScenarioEditorAsync(
        UserPrincipal user,
        Guid departmentId,
        AccessAction action,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("scenario", action);
        user.EnsureOrganizationAccess(user.OrganizationId);
        if (user.IsServiceIdentity)
        {
            throw new PermissionException(
                ErrorCodes.PermissionDenied,
                "数字员工服务身份不能创建或编辑业务场景。");
        }

        if (!await CanManageDepartmentAsync(user, departmentId, cancellationToken))
        {
            throw new PermissionException(
                ErrorCodes.DepartmentScopeDenied,
                "当前身份无权管理该部门场景。");
        }
    }

    private static void EnsureRead(UserPrincipal user)
    {
        user.EnsureCan("scenario", AccessAction.Read);
        user.EnsureOrganizationAccess(user.OrganizationId);
    }

    private static bool IsDepartmentApplicable(
        UserPrincipal user,
        ScenarioDefinitionEntity definition,
        ScenarioDefinitionPayloadDto payload,
        IReadOnlyDictionary<Guid, DepartmentEntity> departments)
    {
        if (user.IsPlatformAdmin)
        {
            return true;
        }

        var applicableDepartmentIds = payload.ApplicableDepartmentCodes
            .Select(code => departments.Values.SingleOrDefault(item =>
                string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase)))
            .Where(item => item is not null)
            .Select(item => item!.Id)
            .Append(definition.OwnerDepartmentId)
            .Distinct()
            .ToHashSet();
        return user.DepartmentIds.Overlaps(applicableDepartmentIds);
    }

    private async Task<Dictionary<Guid, DepartmentEntity>> LoadDepartmentsAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var departments = await dbContext.Departments.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.IsActive)
            .ToListAsync(cancellationToken);
        return departments.ToDictionary(item => item.Id);
    }

    private async Task<Dictionary<Guid, string>> LoadUserNamesAsync(
        Guid[] userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Length == 0)
        {
            return [];
        }

        return await dbContext.Users.AsNoTracking()
            .Where(item => userIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.DisplayName, cancellationToken);
    }

    private async Task<HashSet<Guid>> LoadManageableDepartmentIdsAsync(
        UserPrincipal user,
        CancellationToken cancellationToken)
    {
        if (user.IsServiceIdentity)
        {
            return [];
        }

        var visible = await dbContext.Departments.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId && item.IsActive)
            .Where(item => user.IsPlatformAdmin || user.DepartmentIds.Contains(item.Id))
            .Select(item => new { item.Id, item.ManagerUserId })
            .ToListAsync(cancellationToken);
        if (user.IsPlatformAdmin)
        {
            return visible.Select(item => item.Id).ToHashSet();
        }

        if (user.Roles.Contains("agent-department-manager", StringComparer.OrdinalIgnoreCase))
        {
            return visible.Select(item => item.Id).ToHashSet();
        }

        var managerIds = visible
            .Where(item => item.ManagerUserId == user.UserId)
            .Select(item => item.Id)
            .ToHashSet();
        var membershipManagerIds = await dbContext.UserDepartmentMemberships.AsNoTracking()
            .Where(item =>
                item.UserId == user.UserId
                && item.IsActive
                && item.IsManager
                && user.DepartmentIds.Contains(item.DepartmentId))
            .Select(item => item.DepartmentId)
            .ToListAsync(cancellationToken);
        managerIds.UnionWith(membershipManagerIds);
        return managerIds;
    }

    private async Task<bool> CanManageDepartmentAsync(
        UserPrincipal user,
        Guid departmentId,
        CancellationToken cancellationToken) =>
        (await LoadManageableDepartmentIdsAsync(user, cancellationToken)).Contains(departmentId);

    private static ScenarioDto MapScenario(
        ScenarioDefinitionEntity definition,
        ScenarioVersionEntity? current,
        ScenarioVersionEntity? draft,
        IReadOnlyDictionary<Guid, DepartmentEntity> departments,
        IReadOnlyDictionary<Guid, string> users,
        bool canManage,
        string? pendingApprovalRequestId = null,
        string? pendingApprovalStatus = null) =>
        new(
            definition.Id.ToString("D"),
            definition.Code,
            definition.Name,
            definition.Description,
            definition.OwnerDepartmentId.ToString("D"),
            departments.GetValueOrDefault(definition.OwnerDepartmentId)?.Name ?? "未知部门",
            ScenarioRules.ParseLifecycleStatus(definition.LifecycleStatus),
            definition.Version,
            definition.Revision,
            definition.CreatedBy.ToString("D"),
            users.GetValueOrDefault(definition.CreatedBy) ?? "未知用户",
            definition.CreatedAt,
            definition.UpdatedBy?.ToString("D"),
            definition.UpdatedBy.HasValue
                ? users.GetValueOrDefault(definition.UpdatedBy.Value)
                : null,
            definition.UpdatedAt,
            definition.DisabledBy?.ToString("D"),
            definition.DisabledBy.HasValue
                ? users.GetValueOrDefault(definition.DisabledBy.Value)
                : null,
            definition.DisabledAt,
            definition.DisabledReason,
            current is null ? null : MapVersion(current, users),
            draft is null ? null : MapVersion(draft, users),
            canManage,
            pendingApprovalRequestId,
            pendingApprovalStatus);

    private static ScenarioVersionDto MapVersion(
        ScenarioVersionEntity version,
        IReadOnlyDictionary<Guid, string> users) =>
        new(
            version.Id.ToString("D"),
            version.ScenarioId.ToString("D"),
            version.VersionNumber,
            ScenarioRules.ParseVersionStatus(version.Status),
            version.Revision,
            DeserializePayload(version.PayloadJson),
            DeserializeReferenceSnapshot(version.ReferenceSnapshotJson),
            DeserializeValidation(version.ValidationJson),
            version.ChangeSummary,
            version.CreatedBy.ToString("D"),
            users.GetValueOrDefault(version.CreatedBy) ?? "未知用户",
            version.CreatedAt,
            version.UpdatedBy?.ToString("D"),
            version.UpdatedBy.HasValue
                ? users.GetValueOrDefault(version.UpdatedBy.Value)
                : null,
            version.UpdatedAt,
            version.PublishedBy?.ToString("D"),
            version.PublishedBy.HasValue
                ? users.GetValueOrDefault(version.PublishedBy.Value)
                : null,
            version.PublishedAt,
            version.RetiredBy?.ToString("D"),
            version.RetiredBy.HasValue
                ? users.GetValueOrDefault(version.RetiredBy.Value)
                : null,
            version.RetiredAt);

    private static ScenarioDefinitionPayloadDto DeserializePayload(string json) =>
        JsonSerializer.Deserialize<ScenarioDefinitionPayloadDto>(
            json,
            ScenarioJsonOptions)
        ?? throw new ValidationException("场景配置 JSON 无法解析。");

    private static ScenarioReferenceSnapshotDto? DeserializeReferenceSnapshot(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<ScenarioReferenceSnapshotDto>(
                json,
                ScenarioJsonOptions);

    private static ScenarioValidationDto? DeserializeValidation(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<ScenarioValidationDto>(
                json,
                ScenarioJsonOptions);

    private static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, ScenarioJsonOptions);

    private static string Hash<T>(T value) =>
        Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(Serialize(value))));

    private static void ValidateIdempotencyKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length is < 8 or > 128)
        {
            throw new ValidationException(
                "场景写操作必须提供 8—128 位幂等键。");
        }
    }

    private static object BuildImpactAnalysis(ScenarioDefinitionPayloadDto payload) =>
        new
        {
            payload.StartMode,
            payload.ObjectTypeCodes,
            payload.WorkflowDefinitionCode,
            payload.WorkflowVersionNumber,
            payload.ParticipantRoles,
            payload.AllowedAgents,
            payload.PermanentHumanBoundaries,
            AffectedCapabilities = new[]
            {
                "BusinessObject",
                "Workflow",
                "Task",
                "Document",
                "Minutes",
                "Project",
                "Agent",
                "Audit",
                "Outbox"
            }
        };

    public async Task<IReadOnlyList<ScenarioApprovalRequestDto>> ListApprovalRequestsAsync(
        UserPrincipal user,
        string? status,
        CancellationToken cancellationToken)
    {
        if (!user.IsPlatformAdmin || !user.HasPermission("scenario", AccessAction.Approve))
        {
            throw new PermissionException(
                ErrorCodes.PermissionDenied,
                "只有平台管理员可以查看高风险场景发布审核。");
        }

        var query = dbContext.AccessChangeRequests.AsNoTracking()
            .Where(item =>
                item.OrganizationId == user.OrganizationId
                && item.RequestType == "ScenarioPublish");
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(item => item.Status == status);
        }

        var rows = await query.OrderByDescending(item => item.RequestedAt)
            .Take(200)
            .ToListAsync(cancellationToken);
        var scenarioIds = rows.Select(item =>
                TryReadApprovalPayload(item.PayloadJson)?.ScenarioId)
            .Where(item => item.HasValue)
            .Select(item => item!.Value)
            .Distinct()
            .ToArray();
        var scenarios = await dbContext.ScenarioDefinitions.AsNoTracking()
            .Where(item => scenarioIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var userIds = rows
            .SelectMany(item => new[] { item.RequestedBy, item.ReviewedBy })
            .Where(item => item.HasValue)
            .Select(item => item!.Value)
            .Distinct()
            .ToArray();
        var users = await LoadUserNamesAsync(userIds, cancellationToken);
        return rows.Select(item =>
        {
            var payload = TryReadApprovalPayload(item.PayloadJson);
            var scenario = payload is not null
                ? scenarios.GetValueOrDefault(payload.ScenarioId)
                : null;
            return new ScenarioApprovalRequestDto(
                item.Id.ToString("D"),
                payload?.ScenarioId.ToString("D") ?? string.Empty,
                scenario?.Code ?? "未知场景",
                scenario?.Name ?? "未知场景",
                payload?.VersionNumber ?? 0,
                item.RiskReason,
                item.Status,
                item.RequestedBy.ToString("D"),
                users.GetValueOrDefault(item.RequestedBy) ?? "未知申请人",
                item.RequestedAt,
                item.ReviewedBy?.ToString("D"),
                item.ReviewedBy.HasValue
                    ? users.GetValueOrDefault(item.ReviewedBy.Value)
                    : null,
                item.ReviewedAt,
                item.ReviewComment);
        }).ToList();
    }

    public async Task<ScenarioDto> ReviewPublishAsync(
        UserPrincipal user,
        Guid requestId,
        ReviewScenarioPublishRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!user.IsPlatformAdmin || !user.HasPermission("scenario", AccessAction.Approve))
        {
            throw new PermissionException(
                ErrorCodes.PermissionDenied,
                "高风险场景发布只能由平台管理员复核。");
        }

        ValidateIdempotencyKey(idempotencyKey);
        var entity = await dbContext.AccessChangeRequests.SingleOrDefaultAsync(
            item => item.Id == requestId
                && item.OrganizationId == user.OrganizationId
                && item.RequestType == "ScenarioPublish",
            cancellationToken) ?? throw new NotFoundException("场景发布审核记录不存在。");
        var payload = TryReadApprovalPayload(entity.PayloadJson)
            ?? throw new ValidationException("场景发布审核内容已损坏。");
        if (entity.RequestedBy == user.UserId)
        {
            throw new PermissionException(
                ErrorCodes.PermissionDenied,
                "申请人不能审核自己的高风险场景发布。");
        }

        if (entity.Status == "Applied")
        {
            return await LoadManagedScenarioAsync(
                    user,
                    payload.ScenarioId,
                    cancellationToken)
                ?? throw new NotFoundException("场景不存在。");
        }

        if (entity.Status != "Pending" && entity.Status != "Approved")
        {
            throw new ConflictException(
                ErrorCodes.Conflict,
                "当前场景发布审核记录不能再次处理。");
        }

        var comment = DomainChecks.Required(request.Comment, "审核意见", 1000);
        var now = clock.UtcNow;
        entity.ReviewedBy = user.UserId;
        entity.ReviewedAt = now;
        entity.ReviewComment = comment;
        if (!request.Approve)
        {
            entity.Status = "Rejected";
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    ModuleName,
                    "scenario.publish_rejected",
                    "Scenario",
                    payload.ScenarioId.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    reason: comment),
                cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return await LoadManagedScenarioAsync(
                    user,
                    payload.ScenarioId,
                    cancellationToken)
                ?? throw new NotFoundException("场景不存在。");
        }

        entity.Status = "Approved";
        await dbContext.SaveChangesAsync(cancellationToken);
        return await PublishAsync(
            user,
            payload.ScenarioId,
            payload.VersionId,
            new ScenarioActionRequest(request.ExpectedVersion, comment),
            idempotencyKey,
            traceId,
            cancellationToken);
    }

    private static bool IsHighRisk(ScenarioDefinitionPayloadDto payload) =>
        payload.ObjectTypeCodes.Contains("PURCHASE_REQUEST", StringComparer.OrdinalIgnoreCase)
        || payload.ObjectTypeCodes.Contains("QUALITY_DEVIATION", StringComparer.OrdinalIgnoreCase)
        || string.Equals(
            payload.WorkflowDefinitionCode,
            "FINANCE_PAYMENT_REVIEW",
            StringComparison.OrdinalIgnoreCase);

    private async Task<AccessChangeRequestEntity?> FindPendingApprovalAsync(
        Guid organizationId,
        Guid scenarioId,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.AccessChangeRequests
            .Where(item =>
                item.OrganizationId == organizationId
                && item.RequestType == "ScenarioPublish"
                && item.Status == "Pending")
            .ToListAsync(cancellationToken);
        return rows.FirstOrDefault(item =>
        {
            var payload = TryReadApprovalPayload(item.PayloadJson);
            return payload?.ScenarioId == scenarioId && payload.VersionId == versionId;
        });
    }

    private async Task<AccessChangeRequestEntity?> FindApprovedApprovalAsync(
        Guid organizationId,
        Guid scenarioId,
        Guid versionId,
        Guid reviewerId,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.AccessChangeRequests
            .Where(item =>
                item.OrganizationId == organizationId
                && item.RequestType == "ScenarioPublish"
                && item.Status == "Approved"
                && item.ReviewedBy == reviewerId)
            .ToListAsync(cancellationToken);
        return rows.FirstOrDefault(item =>
        {
            var payload = TryReadApprovalPayload(item.PayloadJson);
            return payload?.ScenarioId == scenarioId
                && payload.VersionId == versionId
                && item.RequestedBy != reviewerId;
        });
    }

    private async Task<IReadOnlyList<PendingScenarioApproval>> LoadPendingApprovalsAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.AccessChangeRequests.AsNoTracking()
            .Where(item =>
                item.OrganizationId == organizationId
                && item.RequestType == "ScenarioPublish"
                && (item.Status == "Pending" || item.Status == "Approved"))
            .ToListAsync(cancellationToken);
        return rows
            .Select(item =>
            {
                var payload = TryReadApprovalPayload(item.PayloadJson);
                return payload is null
                    ? null
                    : new PendingScenarioApproval(
                        payload.ScenarioId,
                        payload.VersionId,
                        item.Id.ToString("D"),
                        item.Status);
            })
            .Where(item => item is not null)
            .Select(item => item!)
            .ToList();
    }

    private static ScenarioPublishApprovalPayload? TryReadApprovalPayload(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ScenarioPublishApprovalPayload>(
                json,
                ScenarioJsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record ScenarioPublishApprovalPayload(
        Guid ScenarioId,
        Guid VersionId,
        long ExpectedVersion,
        string? Comment,
        int VersionNumber);

    private sealed record PendingScenarioApproval(
        Guid ScenarioId,
        Guid VersionId,
        string RequestId,
        string Status);

    private static JsonSerializerOptions CreateScenarioJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
