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

public sealed class CreateBusinessObjectHandler(
    ICurrentUserAccessor currentUserAccessor,
    IObjectTypeRepository objectTypeRepository,
    IDepartmentRepository departmentRepository,
    IBusinessObjectRepository businessObjectRepository,
    IAuditRepository auditRepository,
    IOutboxRepository outboxRepository,
    IIdempotencyStore idempotencyStore,
    IUnitOfWork unitOfWork,
    DataEnvironmentOptions environmentOptions,
    IClock clock)
{
    public async Task<BusinessObjectDto> HandleAsync(
        CreateBusinessObjectRequest request,
        string traceId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        RequestValidation.Validate(request);
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length is < 8 or > 128)
        {
            throw new ValidationException("创建业务对象必须提供 8—128 位幂等键。");
        }

        var user = currentUserAccessor.GetRequiredUser();
        user.EnsureCan("business-object", AccessAction.Create);

        var requestHash = HashRequest(request);
        var idempotencyScope = $"business-object:create:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(idempotencyScope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同请求。");
            }

            var existingId = Guid.Parse(existing.ResponseId);
            return (await businessObjectRepository.GetByIdAsync(existingId, cancellationToken)
                ?? throw new NotFoundException("幂等结果对应的业务对象不存在。")).ToDto();
        }

        var now = clock.UtcNow;
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var created = await StageWithinTransactionAsync(
                user,
                request,
                traceId,
                now,
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    idempotencyScope,
                    idempotencyKey,
                    requestHash,
                    nameof(BusinessObject),
                    created.Id,
                    "completed",
                    now,
                    now.AddDays(30)),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return created;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<BusinessObjectDto> StageWithinTransactionAsync(
        UserPrincipal user,
        CreateBusinessObjectRequest request,
        string traceId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("business-object", AccessAction.Create);
        var objectType = await objectTypeRepository.GetPublishedByIdAsync(
                request.ObjectTypeId,
                cancellationToken)
            ?? throw new NotFoundException("对象类型不存在。");
        if (objectType.Status != ObjectTypeStatus.Published)
        {
            throw new ConflictException(
                ErrorCodes.ScenarioReferenceInvalid,
                "场景引用的业务对象类型尚未发布，不能创建业务对象。");
        }

        var departmentId = await departmentRepository.ResolveIdAsync(
            user,
            request.DepartmentId,
            objectType.Code,
            cancellationToken);
        if (await businessObjectRepository.ExistsCodeAsync(
                request.ObjectTypeId,
                request.ObjectCode,
                cancellationToken))
        {
            throw new ConflictException(
                ErrorCodes.DuplicateObjectCode,
                "同一对象类型下的业务编号不能重复。");
        }

        var environmentCode = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var fields = ApplyDefaultsAndValidate(objectType, request.Fields);
        var businessObject = BusinessObject.Create(
            user.OrganizationId,
            objectType.Id,
            request.ObjectCode,
            fields,
            now,
            departmentId,
            environmentCode,
            DataEnvironment.IsTestEnvironment(environmentCode),
            "AIDT",
            request.Fields.GetValueOrDefault("Name")
                ?? request.Fields.GetValueOrDefault("ProjectName"),
            user.UserId);
        await businessObjectRepository.AddAsync(businessObject, user.UserId, cancellationToken);
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "BusinessObject",
                "Create",
                nameof(BusinessObject),
                businessObject.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                after: businessObject.ToDto().ToString()),
            cancellationToken);
        await outboxRepository.AddAsync(
            OutboxMessage.Create(
                "BusinessObject.Created",
                nameof(BusinessObject),
                businessObject.Id,
                $"business-object-created:{businessObject.Id:D}",
                $$"""{"businessObjectId":"{{businessObject.Id:D}}","objectCode":"{{businessObject.ObjectCode}}"}""",
                now),
            cancellationToken);
        return businessObject.ToDto();
    }

    private static string HashRequest(CreateBusinessObjectRequest request)
    {
        var payload = JsonSerializer.Serialize(request, JsonSerializerOptions.Web);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    private static Dictionary<string, string?> ApplyDefaultsAndValidate(
        ObjectType objectType,
        IReadOnlyDictionary<string, string?> supplied)
    {
        var definitions = objectType.Fields.ToDictionary(
            field => field.Code,
            StringComparer.OrdinalIgnoreCase);
        var unknown = supplied.Keys.Where(key => !definitions.ContainsKey(key)).ToArray();
        if (unknown.Length > 0)
        {
            throw new ValidationException($"存在未定义字段：{string.Join("、", unknown)}。");
        }

        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in objectType.Fields)
        {
            var value = supplied.TryGetValue(field.Code, out var provided)
                ? provided
                : field.DefaultValue;
            ObjectFieldValueValidator.Validate(field, value);
            if (!string.IsNullOrWhiteSpace(value))
            {
                result[field.Code] = value.Trim();
            }
        }

        return result;
    }
}
