using AIDT.Domain.Common;

namespace AIDT.Domain.BusinessObjects;

public enum BusinessObjectStatus
{
    Draft,
    Active,
    Suspended,
    Archived,
    Blocked,
    AwaitingAcceptance,
    Completed,
    Terminated
}

public sealed class BusinessObject
{
    private readonly Dictionary<string, string?> _fields;

    private BusinessObject(
        Guid id,
        Guid organizationId,
        Guid departmentId,
        Guid objectTypeId,
        string objectCode,
        BusinessObjectStatus status,
        long version,
        Dictionary<string, string?> fields,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string environmentCode,
        bool isTestData,
        string sourceSystem,
        string? title,
        Guid? createdByUserId)
    {
        Id = id;
        OrganizationId = organizationId;
        DepartmentId = departmentId;
        ObjectTypeId = objectTypeId;
        ObjectCode = objectCode;
        Status = status;
        Version = version;
        _fields = fields;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        EnvironmentCode = environmentCode;
        IsTestData = isTestData;
        SourceSystem = sourceSystem;
        Title = title;
        CreatedByUserId = createdByUserId;
    }

    public Guid Id { get; }

    public Guid OrganizationId { get; }

    public Guid DepartmentId { get; }

    public Guid ObjectTypeId { get; }

    public string ObjectCode { get; private set; }

    public BusinessObjectStatus Status { get; private set; }

    public long Version { get; private set; }

    public IReadOnlyDictionary<string, string?> Fields => _fields;

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public string EnvironmentCode { get; }

    public bool IsTestData { get; }

    public string SourceSystem { get; }

    public string? Title { get; private set; }

    public Guid? CreatedByUserId { get; }

    public static BusinessObject Create(
        Guid organizationId,
        Guid objectTypeId,
        string objectCode,
        IReadOnlyDictionary<string, string?> fields,
        DateTimeOffset now,
        Guid departmentId = default,
        string environmentCode = "demo",
        bool isTestData = true,
        string sourceSystem = "AIDT",
        string? title = null,
        Guid? createdByUserId = null)
    {
        DomainChecks.RequiredId(objectTypeId, "对象类型");
        ArgumentNullException.ThrowIfNull(fields);

        return new BusinessObject(
            Guid.NewGuid(),
            DomainChecks.RequiredId(organizationId, "组织"),
            DomainChecks.RequiredId(departmentId, "部门"),
            objectTypeId,
            DomainChecks.RequiredCode(objectCode, "对象编号", 80),
            BusinessObjectStatus.Draft,
            1,
            NormalizeFields(fields),
            now.ToUniversalTime(),
            now.ToUniversalTime(),
            DataEnvironment.Normalize(environmentCode),
            isTestData,
            sourceSystem,
            string.IsNullOrWhiteSpace(title) ? null : DomainChecks.Required(title, "业务对象标题", 255),
            createdByUserId);
    }

    public static BusinessObject Restore(
        Guid id,
        Guid organizationId,
        Guid departmentId,
        Guid objectTypeId,
        string objectCode,
        BusinessObjectStatus status,
        long version,
        IReadOnlyDictionary<string, string?> fields,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string environmentCode = "demo",
        bool isTestData = true,
        string sourceSystem = "AIDT",
        string? title = null,
        Guid? createdByUserId = null) =>
        new(
            DomainChecks.RequiredId(id, "业务对象"),
            DomainChecks.RequiredId(organizationId, "组织"),
            DomainChecks.RequiredId(departmentId, "部门"),
            DomainChecks.RequiredId(objectTypeId, "对象类型"),
            DomainChecks.RequiredCode(objectCode, "对象编号", 80),
            status,
            version,
            NormalizeFields(fields),
            createdAt.ToUniversalTime(),
            updatedAt.ToUniversalTime(),
            DataEnvironment.Normalize(environmentCode),
            isTestData,
            sourceSystem,
            string.IsNullOrWhiteSpace(title) ? null : DomainChecks.Required(title, "业务对象标题", 255),
            createdByUserId);

    public void UpdateFields(IReadOnlyDictionary<string, string?> fields, long expectedVersion, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        if (Status == BusinessObjectStatus.Archived)
        {
            throw new ConflictException(ErrorCodes.Conflict, "已归档对象不能修改。");
        }

        _fields.Clear();
        foreach (var field in NormalizeFields(fields))
        {
            _fields[field.Key] = field.Value;
        }

        Version++;
        UpdatedAt = now.ToUniversalTime();
    }

    public void Activate(long expectedVersion, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        if (Status is not (BusinessObjectStatus.Draft or BusinessObjectStatus.Suspended))
        {
            throw new ConflictException(ErrorCodes.Conflict, "当前对象状态不能启用。");
        }

        Status = BusinessObjectStatus.Active;
        Version++;
        UpdatedAt = now.ToUniversalTime();
    }

    public void Archive(long expectedVersion, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        if (Status == BusinessObjectStatus.Archived)
        {
            return;
        }

        Status = BusinessObjectStatus.Archived;
        Version++;
        UpdatedAt = now.ToUniversalTime();
    }

    public void Suspend(long expectedVersion, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        if (Status == BusinessObjectStatus.Archived)
        {
            throw new ConflictException(ErrorCodes.Conflict, "已归档对象不能暂停。");
        }

        Status = BusinessObjectStatus.Suspended;
        Version++;
        UpdatedAt = now.ToUniversalTime();
    }

    public void ChangeStatus(BusinessObjectStatus status, long expectedVersion, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        Status = status;
        Version++;
        UpdatedAt = now.ToUniversalTime();
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (expectedVersion != Version)
        {
            throw new ConflictException(ErrorCodes.Conflict, "业务对象已被其他操作修改，请刷新后重试。");
        }
    }

    private static Dictionary<string, string?> NormalizeFields(IReadOnlyDictionary<string, string?> fields)
    {
        var normalized = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields)
        {
            var key = DomainChecks.Required(field.Key, "字段 Key", 64);
            if (field.Value?.Length > 100_000)
            {
                throw new ValidationException($"字段 {key} 的值不能超过 100000 个字符。");
            }

            normalized[key] = field.Value?.Trim();
        }

        return normalized;
    }
}
