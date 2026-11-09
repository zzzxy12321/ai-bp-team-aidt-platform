using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Application.Validation;
using AIDT.Domain.Audit;
using AIDT.Domain.BusinessObjects;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;
using AIDT.Domain.Integrations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AIDT.Application.UseCases;

public sealed class UpdateBusinessObjectHandler(
    ICurrentUserAccessor currentUserAccessor,
    IObjectTypeRepository objectTypeRepository,
    IBusinessObjectRepository businessObjectRepository,
    IAuditRepository auditRepository,
    IOutboxRepository outboxRepository,
    IIdempotencyStore idempotencyStore,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<BusinessObjectDto> HandleAsync(
        Guid id,
        UpdateBusinessObjectRequest request,
        string traceId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        RequestValidation.Validate(request);
        EnsureIdempotencyKey(idempotencyKey);
        var user = currentUserAccessor.GetRequiredUser();
        user.EnsureCan("business-object", AccessAction.Update);
        user.EnsureOrganizationAccess(user.OrganizationId);

        var requestHash = Hash(new { id, request });
        var scope = $"business-object:update:{user.UserId:D}:{id:D}";
        var replay = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (replay is not null)
        {
            if (!string.Equals(replay.RequestHash, requestHash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同请求。");
            }

            var existing = await businessObjectRepository.GetByIdAsync(
                Guid.Parse(replay.ResponseId),
                cancellationToken) ?? throw new NotFoundException("幂等结果对应的业务对象不存在。");
            var replayType = await objectTypeRepository.GetPublishedByIdAsync(
                existing.ObjectTypeId,
                cancellationToken) ?? throw new ConflictException(
                    ErrorCodes.Conflict,
                    "业务对象当前没有可用发布版本。");
            return Mask(existing, replayType.Fields, user);
        }

        var businessObject = await businessObjectRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("业务对象不存在。");
        if (businessObject.OrganizationId != user.OrganizationId)
        {
            throw new PermissionException(ErrorCodes.DataScopeDenied, "当前身份没有该组织的数据范围。");
        }

        user.EnsureDepartmentAccess(businessObject.DepartmentId);
        if (request.ObjectTypeId is { } requestedTypeId && requestedTypeId != businessObject.ObjectTypeId)
        {
            throw new ValidationException("业务对象类型创建后不可修改。");
        }

        if (request.ObjectCode is not null
            && !string.Equals(
                request.ObjectCode.Trim(),
                businessObject.ObjectCode,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("业务对象稳定编码创建后不可修改。");
        }

        var objectType = await objectTypeRepository.GetPublishedByIdAsync(
            businessObject.ObjectTypeId,
            cancellationToken)
            ?? throw new ConflictException(
                ErrorCodes.Conflict,
                "业务对象当前没有可用发布版本，不能编辑。");
        var before = businessObject.ToDto();
        var merged = MergeAndValidate(businessObject.Fields, request.Fields, objectType.Fields);
        var now = clock.UtcNow;
        businessObject.UpdateFields(merged, request.ExpectedVersion, now);
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await businessObjectRepository.UpdateAsync(
                businessObject,
                user.UserId,
                "FieldsUpdated",
                cancellationToken);
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "BusinessObject",
                    "Update",
                    nameof(BusinessObject),
                    businessObject.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    before: JsonSerializer.Serialize(before, JsonSerializerOptions.Web),
                    after: JsonSerializer.Serialize(businessObject.ToDto(), JsonSerializerOptions.Web)),
                cancellationToken);
            await outboxRepository.AddAsync(
                OutboxMessage.Create(
                    "BusinessObject.Updated",
                    nameof(BusinessObject),
                    businessObject.Id,
                    $"business-object-updated:{businessObject.Id:D}:v{businessObject.Version}",
                    $$"""{"businessObjectId":"{{businessObject.Id:D}}","version":{{businessObject.Version}}}""",
                    now),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    requestHash,
                    nameof(BusinessObject),
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

        return Mask(businessObject, objectType.Fields, user);
    }

    private static Dictionary<string, string?> MergeAndValidate(
        IReadOnlyDictionary<string, string?> existingValues,
        IReadOnlyDictionary<string, string?> submittedValues,
        IReadOnlyList<ObjectFieldDefinition> definitions)
    {
        var definitionsByCode = definitions.ToDictionary(
            field => field.Code,
            StringComparer.OrdinalIgnoreCase);
        var unsupported = submittedValues.Keys
            .Where(key => !definitionsByCode.ContainsKey(key)
                && !existingValues.ContainsKey(key))
            .ToArray();
        if (unsupported.Length > 0)
        {
            throw new ValidationException($"存在未定义字段：{string.Join("、", unsupported)}。");
        }

        var merged = new Dictionary<string, string?>(
            existingValues,
            StringComparer.OrdinalIgnoreCase);
        foreach (var supplied in submittedValues)
        {
            if (definitionsByCode.ContainsKey(supplied.Key))
            {
                merged[supplied.Key] = supplied.Value;
            }
        }

        foreach (var definition in definitions)
        {
            if (!merged.TryGetValue(definition.Code, out var value))
            {
                value = definition.DefaultValue;
            }

            ObjectFieldValueValidator.Validate(definition, value);
            if (!string.IsNullOrWhiteSpace(value))
            {
                merged[definition.Code] = value.Trim();
            }
            else
            {
                merged.Remove(definition.Code);
            }
        }

        return merged;
    }

    private static BusinessObjectDto Mask(
        BusinessObject businessObject,
        IReadOnlyList<ObjectFieldDefinition> definitions,
        UserPrincipal user)
    {
        var dto = businessObject.ToDto();
        if (user.IsPlatformAdmin)
        {
            return dto;
        }

        var values = new Dictionary<string, string?>(dto.Fields, StringComparer.OrdinalIgnoreCase);
        foreach (var field in definitions.Where(field =>
                     field.Sensitivity == FieldSensitivity.Core
                     && values.ContainsKey(field.Code)))
        {
            values[field.Code] = "***";
        }

        return dto with { Fields = values };
    }

    private static void EnsureIdempotencyKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length is < 8 or > 128)
        {
            throw new ValidationException("编辑业务对象必须提供 8—128 位幂等键。");
        }
    }

    private static string Hash<T>(T value) =>
        Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, JsonSerializerOptions.Web))));
}
