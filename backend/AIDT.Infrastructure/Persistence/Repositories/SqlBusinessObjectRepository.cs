using AIDT.Application.Abstractions;
using AIDT.Domain.BusinessObjects;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Persistence.Repositories;

public sealed class SqlBusinessObjectRepository(AidtDbContext dbContext) : IBusinessObjectRepository
{
    private readonly Dictionary<Guid, BusinessObjectEntity> _tracked = [];

    public async Task<BusinessObject?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.BusinessObjects
            .SingleOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        _tracked[id] = entity;
        return Map(entity);
    }

    public Task<bool> ExistsCodeAsync(
        Guid objectTypeId,
        string objectCode,
        CancellationToken cancellationToken) =>
        dbContext.BusinessObjects.AnyAsync(
            item => item.ObjectTypeId == objectTypeId
                && item.ObjectCode == objectCode
                && !item.IsDeleted,
            cancellationToken);

    public async Task AddAsync(
        BusinessObject businessObject,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(businessObject);
        var entity = new BusinessObjectEntity
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
            FieldsJson = JsonStorage.Serialize(businessObject.Fields),
            CreatedAt = businessObject.CreatedAt,
            UpdatedAt = businessObject.UpdatedAt
        };
        await dbContext.BusinessObjects.AddAsync(entity, cancellationToken);
        await dbContext.BusinessObjectVersions.AddAsync(
            CreateVersion(entity, actorId, "Created"),
            cancellationToken);
        _tracked[businessObject.Id] = entity;
    }

    public async Task UpdateAsync(
        BusinessObject businessObject,
        Guid actorId,
        string reason,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(businessObject);
        if (!_tracked.TryGetValue(businessObject.Id, out var entity))
        {
            entity = await dbContext.BusinessObjects.SingleAsync(
                item => item.Id == businessObject.Id,
                cancellationToken);
            _tracked[businessObject.Id] = entity;
        }

        entity.Status = businessObject.Status.ToString();
        entity.Title = businessObject.Title;
        entity.Version = businessObject.Version;
        entity.FieldsJson = JsonStorage.Serialize(businessObject.Fields);
        entity.UpdatedAt = businessObject.UpdatedAt;
        await dbContext.BusinessObjectVersions.AddAsync(
            CreateVersion(entity, actorId, reason),
            cancellationToken);
    }

    internal static BusinessObject Map(BusinessObjectEntity entity) =>
        BusinessObject.Restore(
            entity.Id,
            entity.OrganizationId,
            entity.DepartmentId,
            entity.ObjectTypeId,
            entity.ObjectCode,
            Enum.Parse<BusinessObjectStatus>(entity.Status),
            entity.Version,
            JsonStorage.Deserialize<Dictionary<string, string?>>(entity.FieldsJson)
                ?? new Dictionary<string, string?>(),
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.EnvironmentCode,
            entity.IsTestData,
            entity.SourceSystem,
            entity.Title,
            entity.CreatedByUserId);

    private static BusinessObjectVersionEntity CreateVersion(
        BusinessObjectEntity entity,
        Guid actorId,
        string reason) =>
        new()
        {
            BusinessObjectId = entity.Id,
            Version = entity.Version,
            SnapshotJson = JsonStorage.Serialize(new
            {
                entity.ObjectCode,
                entity.ObjectTypeId,
                entity.OrganizationId,
                entity.DepartmentId,
                entity.Status,
                entity.Version,
                entity.EnvironmentCode,
                entity.IsTestData,
                entity.SourceSystem,
                entity.Title,
                entity.CreatedByUserId,
                Fields = JsonStorage.Deserialize<Dictionary<string, string?>>(entity.FieldsJson)
            }),
            ActorId = actorId,
            Reason = reason,
            CreatedAt = entity.UpdatedAt
        };
}
