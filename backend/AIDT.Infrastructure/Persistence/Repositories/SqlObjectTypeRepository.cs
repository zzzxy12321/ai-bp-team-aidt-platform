using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Persistence.Repositories;

public sealed class SqlObjectTypeRepository(AidtDbContext dbContext) : IObjectTypeRepository
{
    public async Task<ObjectType?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ObjectTypes
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var fields = await dbContext.FieldDefinitions.AsNoTracking()
            .Where(item => item.ObjectTypeId == id)
            .ToListAsync(cancellationToken);
        return ToDomain(entity, fields);
    }

    public async Task<ObjectType?> GetPublishedByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var current = await dbContext.ObjectTypes.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (current is null)
        {
            return null;
        }

        if (current.Status == ObjectTypeStatus.Published.ToString())
        {
            var currentFields = await dbContext.FieldDefinitions.AsNoTracking()
                .Where(item => item.ObjectTypeId == id)
                .ToListAsync(cancellationToken);
            return ToDomain(current, currentFields);
        }

        var snapshot = await dbContext.ObjectTypeVersions.AsNoTracking()
            .Where(item => item.ObjectTypeId == id
                && item.Status == ObjectTypeStatus.Published.ToString())
            .OrderByDescending(item => item.Version)
            .FirstOrDefaultAsync(cancellationToken);
        if (snapshot is null)
        {
            return null;
        }

        var fields = JsonStorage.DeserializeObjectFields(snapshot.FieldsJson)
            .Select(ToDefinition)
            .ToList();
        return ObjectType.Restore(
            current.Id,
            current.Code,
            snapshot.Name,
            snapshot.Version,
            fields,
            ObjectTypeStatus.Published,
            current.Revision,
            snapshot.PublishedAt,
            null);
    }

    public async Task AddAsync(ObjectType objectType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(objectType);
        var now = DateTimeOffset.UtcNow;
        await dbContext.ObjectTypes.AddAsync(
            new ObjectTypeEntity
            {
                Id = objectType.Id,
                Code = objectType.Code,
                Name = objectType.Name,
                Version = objectType.Version,
                Revision = objectType.Revision,
                Status = objectType.Status.ToString(),
                PublishedAt = objectType.PublishedAt,
                RetiredAt = objectType.RetiredAt,
                CreatedAt = now,
                UpdatedAt = now
            },
            cancellationToken);
        await dbContext.FieldDefinitions.AddRangeAsync(
            objectType.Fields.Select(field => ToEntity(objectType.Id, field)),
            cancellationToken);
    }

    public async Task<List<ObjectType>> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var entities = await dbContext.ObjectTypes
            .AsNoTracking()
            .OrderBy(item => item.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var ids = entities.Select(item => item.Id).ToArray();
        var fields = await dbContext.FieldDefinitions.AsNoTracking()
            .Where(item => ids.Contains(item.ObjectTypeId))
            .ToListAsync(cancellationToken);
        var fieldsByType = fields.GroupBy(item => item.ObjectTypeId)
            .ToDictionary(group => group.Key, group => group.ToList());
        return entities
            .Select(entity => ToDomain(entity, fieldsByType.GetValueOrDefault(entity.Id) ?? []))
            .ToList();
    }

    public Task<long> CountAsync(CancellationToken cancellationToken) =>
        dbContext.ObjectTypes.LongCountAsync(cancellationToken);

    internal static ObjectFieldDefinition ToDefinition(ObjectFieldDto field) =>
        new(
            field.Code ?? field.Key,
            field.DisplayName,
            ObjectFieldTypes.Normalize(field.DataType),
            field.IsRequired,
            field.MaxLength,
            Enum.Parse<FieldSensitivity>(field.Sensitivity, ignoreCase: true),
            field.DefaultValue,
            field.Options,
            field.ShowInList,
            field.Filterable,
            field.SortOrder);

    internal static ObjectFieldDto ToDto(ObjectFieldDefinition field) =>
        new(
            field.Code,
            field.DisplayName,
            field.NormalizedDataType,
            field.IsRequired,
            field.MaxLength,
            field.Sensitivity.ToString(),
            field.DefaultValue,
            field.NormalizedOptions,
            field.ShowInList,
            field.Filterable,
            field.SortOrder,
            field.Code);

    internal static ObjectTypeDto ToDto(ObjectType objectType) =>
        new(
            objectType.Id.ToString("D"),
            objectType.Code,
            objectType.Name,
            objectType.Version,
            objectType.Fields.Select(ToDto).ToList(),
            objectType.Status.ToString(),
            objectType.Revision,
            objectType.PublishedAt,
            objectType.RetiredAt);

    internal static ObjectType ToDomain(
        ObjectTypeEntity entity,
        IEnumerable<FieldDefinitionEntity> fields) =>
        ObjectType.Restore(
            entity.Id,
            entity.Code,
            entity.Name,
            entity.Version,
            fields.OrderBy(field => field.SortOrder).ThenBy(field => field.Key)
                .Select(field => new ObjectFieldDefinition(
                    field.Key,
                    field.DisplayName,
                    ObjectFieldTypes.Normalize(field.DataType),
                    field.IsRequired,
                    field.MaxLength,
                    Enum.Parse<FieldSensitivity>(field.Sensitivity, ignoreCase: true),
                    field.DefaultValue,
                    field.OptionsJson is null
                        ? []
                        : JsonStorage.Deserialize<List<string>>(field.OptionsJson) ?? [],
                    field.ShowInList,
                    field.Filterable,
                    field.SortOrder)),
            Enum.Parse<ObjectTypeStatus>(entity.Status, ignoreCase: true),
            entity.Revision,
            entity.PublishedAt,
            entity.RetiredAt);

    private static FieldDefinitionEntity ToEntity(
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
}
