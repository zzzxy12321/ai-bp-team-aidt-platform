using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Audit;
using AIDT.Domain.BusinessObjects;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;
using AIDT.Infrastructure.Persistence;
using AIDT.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AIDT.Infrastructure.Services;

public sealed class SqlObjectModelService(
    AidtDbContext dbContext,
    IObjectTypeRepository objectTypeRepository,
    IAuditRepository auditRepository,
    IIdempotencyStore idempotencyStore,
    IUnitOfWork unitOfWork,
    IClock clock) : IObjectModelService
{
    public async Task<ObjectTypeDto> CreateAsync(
        UserPrincipal user,
        CreateObjectTypeRequest request,
        string traceId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        RequireUser(user, AccessAction.Create);
        EnsureIdempotencyKey(idempotencyKey);
        var requestHash = Hash(request);
        var scope = $"object-model:create:{user.UserId:D}";
        if (await FindReplayAsync(scope, idempotencyKey, requestHash, cancellationToken) is { } replay)
        {
            return replay;
        }

        var normalizedCode = request.Code.Trim();
        if (await dbContext.ObjectTypes.AnyAsync(
                item => item.Code == normalizedCode,
                cancellationToken))
        {
            throw new ConflictException(ErrorCodes.Conflict, "对象类型编码已存在。");
        }

        var fields = MapFields(request.Fields);
        var objectType = ObjectType.CreateDraft(request.Code, request.Name, fields);
        var now = clock.UtcNow;
        var result = SqlObjectTypeRepository.ToDto(objectType);
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await objectTypeRepository.AddAsync(objectType, cancellationToken);
            await dbContext.ObjectTypeVersions.AddAsync(
                CreateVersion(objectType, user.UserId, now),
                cancellationToken);
            await AddAuditAsync(user, "CreateDraft", objectType, traceId, now, cancellationToken);
            await AddIdempotencyAsync(
                scope,
                idempotencyKey,
                requestHash,
                objectType.Id,
                now,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return result;
    }

    public async Task<ObjectTypeDto> UpdateDraftAsync(
        UserPrincipal user,
        Guid id,
        UpdateObjectTypeDraftRequest request,
        string traceId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        RequireUser(user, AccessAction.Update);
        EnsureIdempotencyKey(idempotencyKey);
        var requestHash = Hash(new { id, request });
        var scope = $"object-model:update-draft:{user.UserId:D}:{id:D}";
        if (await FindReplayAsync(scope, idempotencyKey, requestHash, cancellationToken) is { } replay)
        {
            return replay;
        }

        var (entity, currentFields) = await LoadTrackedAsync(id, cancellationToken);
        var current = SqlObjectTypeRepository.ToDomain(entity, currentFields);
        var nextFields = MapFields(request.Fields);
        EnsureCompatible(current.Fields, nextFields);
        current.UpdateDraft(request.Name, nextFields, request.ExpectedRevision);
        var now = clock.UtcNow;
        entity.Name = current.Name;
        entity.Version = current.Version;
        entity.Revision = current.Revision;
        entity.Status = current.Status.ToString();
        entity.PublishedAt = current.PublishedAt;
        entity.RetiredAt = current.RetiredAt;
        entity.UpdatedAt = now;
        dbContext.FieldDefinitions.RemoveRange(currentFields);
        await dbContext.FieldDefinitions.AddRangeAsync(
            current.Fields.Select(field => CreateField(id, field)),
            cancellationToken);

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await dbContext.ObjectTypeVersions.AddAsync(
                CreateVersion(current, user.UserId, now),
                cancellationToken);
            await AddAuditAsync(user, "UpdateDraft", current, traceId, now, cancellationToken);
            await AddIdempotencyAsync(
                scope,
                idempotencyKey,
                requestHash,
                id,
                now,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return SqlObjectTypeRepository.ToDto(current);
    }

    public async Task<ObjectTypeValidationDto> ValidateAsync(
        UserPrincipal user,
        Guid id,
        ObjectTypeActionRequest request,
        string traceId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        RequireUser(user, AccessAction.Manage);
        EnsureIdempotencyKey(idempotencyKey);
        var requestHash = Hash(new { id, action = "Validate", request });
        var scope = $"object-model:validate:{user.UserId:D}:{id:D}";
        if (await FindReplayAsync(scope, idempotencyKey, requestHash, cancellationToken) is not null)
        {
            var replayed = await LoadDomainAsync(id, cancellationToken);
            return new ObjectTypeValidationDto(
                id.ToString("D"), true, [], replayed.Version, replayed.Revision, clock.UtcNow);
        }

        var (entity, currentFields) = await LoadTrackedAsync(id, cancellationToken);
        var current = SqlObjectTypeRepository.ToDomain(entity, currentFields);
        current.ValidateModel(request.ExpectedRevision);
        var now = clock.UtcNow;
        entity.Status = current.Status.ToString();
        entity.Revision = current.Revision;
        entity.UpdatedAt = now;
        var snapshot = await GetCurrentVersionAsync(id, current.Version, cancellationToken);
        snapshot.Status = ObjectTypeStatus.Validated.ToString();
        snapshot.ValidatedAt = now;
        snapshot.UpdatedBy = user.UserId;
        snapshot.UpdatedAt = now;

        await using var validationTransaction =
            await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await AddAuditAsync(user, "Validate", current, traceId, now, cancellationToken);
            await AddIdempotencyAsync(
                scope,
                idempotencyKey,
                requestHash,
                id,
                now,
                cancellationToken);
            await validationTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await validationTransaction.RollbackAsync(cancellationToken);
            throw;
        }

        return new ObjectTypeValidationDto(
            id.ToString("D"), true, [], current.Version, current.Revision, now);
    }

    public Task<ObjectTypeDto> PublishAsync(
        UserPrincipal user,
        Guid id,
        ObjectTypeActionRequest request,
        string traceId,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        ExecuteActionAsync(
            id,
            user,
            request,
            traceId,
            idempotencyKey,
            AccessAction.Approve,
            "publish",
            (model, now) => model.Publish(request.ExpectedRevision, now),
            "Publish",
            cancellationToken);

    public Task<ObjectTypeDto> RetireAsync(
        UserPrincipal user,
        Guid id,
        ObjectTypeActionRequest request,
        string traceId,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        ExecuteActionAsync(
            id,
            user,
            request,
            traceId,
            idempotencyKey,
            AccessAction.Manage,
            "retire",
            (model, now) => model.Retire(request.ExpectedRevision, now),
            "Retire",
            cancellationToken);

    public Task<ObjectTypeDto> RestoreAsync(
        UserPrincipal user,
        Guid id,
        ObjectTypeActionRequest request,
        string traceId,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        ExecuteActionAsync(
            id,
            user,
            request,
            traceId,
            idempotencyKey,
            AccessAction.Manage,
            "restore",
            (model, now) => model.Restore(request.ExpectedRevision, now),
            "Restore",
            cancellationToken);

    public async Task<IReadOnlyList<ObjectTypeVersionDto>> ListVersionsAsync(
        Guid id,
        UserPrincipal user,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("object-model", AccessAction.Read);
        if (!await dbContext.ObjectTypes.AsNoTracking().AnyAsync(
                item => item.Id == id,
                cancellationToken))
        {
            throw new NotFoundException("对象类型不存在。");
        }

        var versions = await dbContext.ObjectTypeVersions.AsNoTracking()
            .Where(item => item.ObjectTypeId == id)
            .OrderByDescending(item => item.Version)
            .ToListAsync(cancellationToken);
        return versions.Select(ToVersionDto).ToList();
    }

    private async Task<ObjectTypeDto> ExecuteActionAsync(
        Guid id,
        UserPrincipal user,
        ObjectTypeActionRequest request,
        string traceId,
        string idempotencyKey,
        AccessAction permission,
        string actionCode,
        Action<ObjectType, DateTimeOffset> action,
        string auditAction,
        CancellationToken cancellationToken)
    {
        RequireUser(user, permission);
        EnsureIdempotencyKey(idempotencyKey);
        var requestHash = Hash(new { id, action = actionCode, request });
        var scope = $"object-model:{actionCode}:{user.UserId:D}:{id:D}";
        if (await FindReplayAsync(scope, idempotencyKey, requestHash, cancellationToken) is { } replay)
        {
            return replay;
        }

        var (entity, currentFields) = await LoadTrackedAsync(id, cancellationToken);
        var current = SqlObjectTypeRepository.ToDomain(entity, currentFields);
        var now = clock.UtcNow;
        action(current, now);
        entity.Status = current.Status.ToString();
        entity.Name = current.Name;
        entity.Revision = current.Revision;
        entity.PublishedAt = current.PublishedAt;
        entity.RetiredAt = current.RetiredAt;
        entity.UpdatedAt = now;

        var snapshot = await GetCurrentVersionAsync(id, current.Version, cancellationToken);
        if (actionCode == "publish")
        {
            await dbContext.ObjectTypeVersions
                .Where(item => item.ObjectTypeId == id
                    && item.Status == ObjectTypeStatus.Published.ToString()
                    && item.Id != snapshot.Id)
                .ExecuteUpdateAsync(
                    update => update.SetProperty(item => item.Status, ObjectTypeStatus.Superseded.ToString()),
                    cancellationToken);
        }

        snapshot.Status = current.Status.ToString();
        snapshot.PublishedAt = current.PublishedAt;
        snapshot.RetiredAt = current.RetiredAt;
        snapshot.UpdatedBy = user.UserId;
        snapshot.UpdatedAt = now;
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await AddAuditAsync(user, auditAction, current, traceId, now, cancellationToken);
            await AddIdempotencyAsync(
                scope,
                idempotencyKey,
                requestHash,
                id,
                now,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return SqlObjectTypeRepository.ToDto(current);
    }

    private static void RequireUser(UserPrincipal user, AccessAction action)
    {
        user.EnsureCan("object-model", action);
        if (!user.IsPlatformAdmin)
        {
            throw new PermissionException(
                ErrorCodes.PermissionDenied,
                "业务对象建模仅允许平台管理员。");
        }

    }

    private async Task<ObjectType> LoadDomainAsync(Guid id, CancellationToken cancellationToken) =>
        await objectTypeRepository.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException("对象类型不存在。");

    private async Task<(ObjectTypeEntity Entity, List<FieldDefinitionEntity> Fields)>
        LoadTrackedAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ObjectTypes.SingleOrDefaultAsync(
            item => item.Id == id,
            cancellationToken) ?? throw new NotFoundException("对象类型不存在。");
        var fields = await dbContext.FieldDefinitions
            .Where(item => item.ObjectTypeId == id)
            .ToListAsync(cancellationToken);
        return (entity, fields);
    }

    private async Task<ObjectTypeVersionEntity> GetCurrentVersionAsync(
        Guid id,
        long version,
        CancellationToken cancellationToken) =>
        await dbContext.ObjectTypeVersions.SingleOrDefaultAsync(
            item => item.ObjectTypeId == id && item.Version == version,
            cancellationToken) ?? throw new ConflictException(
                ErrorCodes.Conflict,
                "对象类型版本快照缺失，请重建该草稿。");

    private async Task<ObjectTypeDto?> FindReplayAsync(
        string scope,
        string key,
        string requestHash,
        CancellationToken cancellationToken)
    {
        var existing = await idempotencyStore.FindAsync(scope, key, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
        {
            throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同请求。");
        }

        var model = await LoadDomainAsync(
            Guid.Parse(existing.ResponseId),
            cancellationToken);
        return SqlObjectTypeRepository.ToDto(model);
    }

    private async Task AddAuditAsync(
        UserPrincipal user,
        string action,
        ObjectType model,
        string traceId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Metadata",
                action,
                nameof(ObjectType),
                model.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                after: JsonStorage.Serialize(SqlObjectTypeRepository.ToDto(model))),
            cancellationToken);

    private async Task AddIdempotencyAsync(
        string scope,
        string key,
        string requestHash,
        Guid objectTypeId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        await idempotencyStore.AddAsync(
            new IdempotencyEntry(
                Guid.NewGuid(),
                scope,
                key,
                requestHash,
                nameof(ObjectType),
                objectTypeId.ToString("D"),
                "completed",
                now,
                now.AddDays(30)),
            cancellationToken);

    private static ObjectTypeVersionEntity CreateVersion(
        ObjectType model,
        Guid actorId,
        DateTimeOffset now) =>
        new()
        {
            ObjectTypeId = model.Id,
            Version = model.Version,
            Status = model.Status.ToString(),
            Name = model.Name,
            FieldsJson = JsonStorage.Serialize(model.Fields.Select(SqlObjectTypeRepository.ToDto)),
            ValidatedAt = model.Status == ObjectTypeStatus.Validated ? now : null,
            PublishedAt = model.PublishedAt,
            RetiredAt = model.RetiredAt,
            CreatedBy = actorId,
            UpdatedBy = actorId,
            CreatedAt = now,
            UpdatedAt = now
        };

    private static FieldDefinitionEntity CreateField(
        Guid objectTypeId,
        ObjectFieldDefinition field) =>
        new()
        {
            Id = Guid.NewGuid(),
            ObjectTypeId = objectTypeId,
            Key = field.Code,
            DisplayName = field.DisplayName,
            DataType = field.NormalizedDataType,
            IsRequired = field.IsRequired,
            MaxLength = field.MaxLength,
            Sensitivity = field.Sensitivity.ToString(),
            DefaultValue = field.DefaultValue,
            OptionsJson = field.NormalizedOptions.Count == 0
                ? null
                : JsonStorage.Serialize(field.NormalizedOptions),
            ShowInList = field.ShowInList,
            Filterable = field.Filterable,
            SortOrder = field.SortOrder,
            SchemaVersion = 1
        };

    private static List<ObjectFieldDefinition> MapFields(
        IReadOnlyList<ObjectFieldDto> fields)
    {
        if (fields is null)
        {
            throw new ValidationException("对象字段不能为空。");
        }

        return fields.Select(SqlObjectTypeRepository.ToDefinition).ToList();
    }

    private static void EnsureCompatible(
        IReadOnlyList<ObjectFieldDefinition> current,
        IReadOnlyList<ObjectFieldDefinition> next)
    {
        var currentByCode = current.ToDictionary(field => field.Code, StringComparer.OrdinalIgnoreCase);
        foreach (var field in next.Where(field => currentByCode.ContainsKey(field.Code)))
        {
            var previous = currentByCode[field.Code];
            var bothText = previous.NormalizedDataType is ObjectFieldTypes.Text or ObjectFieldTypes.LongText
                && field.NormalizedDataType is ObjectFieldTypes.Text or ObjectFieldTypes.LongText;
            if (!bothText && previous.NormalizedDataType != field.NormalizedDataType)
            {
                throw new ValidationException(
                    $"字段 {field.Code} 不允许从 {previous.NormalizedDataType} 改为 {field.NormalizedDataType}。");
            }

            if (field.MaxLength < previous.MaxLength)
            {
                throw new ValidationException(
                    $"字段 {field.Code} 已发布后不能缩小最大长度，请新建字段或保留原长度。");
            }
        }
    }

    private static ObjectTypeVersionDto ToVersionDto(ObjectTypeVersionEntity entity)
    {
        var fields = JsonStorage.DeserializeObjectFields(entity.FieldsJson);
        return new ObjectTypeVersionDto(
            entity.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            entity.ObjectTypeId.ToString("D"),
            entity.Version,
            entity.Status,
            entity.Name,
            fields,
            entity.ValidatedAt,
            entity.PublishedAt,
            entity.RetiredAt,
            entity.CreatedBy?.ToString("D"),
            entity.UpdatedBy?.ToString("D"),
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    private static void EnsureIdempotencyKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length is < 8 or > 128)
        {
            throw new ValidationException("对象模型写操作必须提供 8—128 位幂等键。");
        }
    }

    private static string Hash<T>(T value) =>
        Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, JsonSerializerOptions.Web))));
}
