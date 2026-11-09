using AIDT.Domain.Common;
using System.Globalization;

namespace AIDT.Domain.BusinessObjects;

public enum FieldSensitivity
{
    Internal,
    Sensitive,
    Core
}

public enum ObjectTypeStatus
{
    Draft,
    Validated,
    Published,
    Retired,
    Superseded
}

public static class ObjectFieldTypes
{
    public const string Text = "Text";
    public const string LongText = "LongText";
    public const string Number = "Number";
    public const string Date = "Date";
    public const string DateTime = "DateTime";
    public const string Boolean = "Boolean";
    public const string Select = "Select";
    public const string User = "User";
    public const string Department = "Department";

    public static IReadOnlySet<string> Supported { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Text, LongText, Number, Date, DateTime, Boolean, Select, User, Department
        };

    public static string Normalize(string dataType)
    {
        var normalized = dataType?.Trim();
        var canonical = normalized is null
            ? null
            : Supported.FirstOrDefault(item =>
                string.Equals(item, normalized, StringComparison.Ordinal));
        if (canonical is not null)
        {
            return canonical;
        }

        return normalized?.ToLowerInvariant() switch
        {
            "string" or "text-short" => Text,
            "text" or "long-text" => LongText,
            "decimal" or "int" or "integer" or "double" or "number" => Number,
            "datetimeoffset" or "datetime" => DateTime,
            "date" => Date,
            "bool" or "boolean" => Boolean,
            "select" => Select,
            "user" => User,
            "department" => Department,
            _ => throw new ValidationException(
                $"字段类型必须是 {string.Join("、", Supported)} 之一。")
        };
    }
}

public sealed record ObjectFieldDefinition(
    string Code,
    string DisplayName,
    string DataType,
    bool IsRequired,
    int MaxLength,
    FieldSensitivity Sensitivity,
    string? DefaultValue = null,
    IReadOnlyList<string>? Options = null,
    bool ShowInList = true,
    bool Filterable = false,
    int SortOrder = 0)
{
    public string Key => Code;

    public string NormalizedDataType => ObjectFieldTypes.Normalize(DataType);

    public IReadOnlyList<string> NormalizedOptions => (Options ?? [])
        .Select(value => value.Trim())
        .Where(value => value.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public void Validate()
    {
        DomainChecks.RequiredIdentifier(Code, "字段 Code");
        DomainChecks.Required(DisplayName, "字段名称", 128);
        _ = ObjectFieldTypes.Normalize(DataType);
        if (MaxLength is < 1 or > 100_000)
        {
            throw new ValidationException("字段最大长度必须在 1 到 100000 之间。");
        }

        if (SortOrder is < 0 or > 100_000)
        {
            throw new ValidationException("字段排序值必须在 0 到 100000 之间。");
        }

        var options = NormalizedOptions;
        if (NormalizedDataType == ObjectFieldTypes.Select && options.Count == 0)
        {
            throw new ValidationException($"字段 {Code} 为 Select 时必须提供选项。");
        }

        if (NormalizedDataType != ObjectFieldTypes.Select && options.Count > 0)
        {
            throw new ValidationException($"只有 Select 字段 {Code} 可以配置选项。");
        }

        if (DefaultValue is not null)
        {
            ObjectFieldValueValidator.Validate(this, DefaultValue);
        }
    }
}

public static class ObjectFieldValueValidator
{
    public static void Validate(ObjectFieldDefinition field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (field.IsRequired)
            {
                throw new ValidationException($"字段 {field.DisplayName} 为必填项。");
            }

            return;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > field.MaxLength)
        {
            throw new ValidationException(
                $"字段 {field.DisplayName} 不能超过 {field.MaxLength} 个字符。");
        }

        switch (field.NormalizedDataType)
        {
            case ObjectFieldTypes.Number when !decimal.TryParse(
                trimmed,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out _):
                throw new ValidationException($"字段 {field.DisplayName} 必须是有效数字。");
            case ObjectFieldTypes.Date when !DateOnly.TryParseExact(
                trimmed,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _):
                throw new ValidationException($"字段 {field.DisplayName} 必须是 yyyy-MM-dd 日期。");
            case ObjectFieldTypes.DateTime when !DateTimeOffset.TryParse(
                trimmed,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out _):
                throw new ValidationException($"字段 {field.DisplayName} 必须是有效日期时间。");
            case ObjectFieldTypes.Boolean when !bool.TryParse(trimmed, out _):
                throw new ValidationException($"字段 {field.DisplayName} 必须是 true 或 false。");
            case ObjectFieldTypes.Select when !field.NormalizedOptions.Contains(
                trimmed,
                StringComparer.OrdinalIgnoreCase):
                throw new ValidationException($"字段 {field.DisplayName} 的值不在允许选项中。");
            case ObjectFieldTypes.User or ObjectFieldTypes.Department when !Guid.TryParse(trimmed, out var id)
                || id == Guid.Empty:
                throw new ValidationException(
                    $"字段 {field.DisplayName} 必须是有效的{(field.NormalizedDataType == ObjectFieldTypes.User ? "用户" : "部门")} ID。");
        }
    }
}

public sealed class ObjectType
{
    private readonly List<ObjectFieldDefinition> _fields;

    private ObjectType(
        Guid id,
        string code,
        string name,
        long version,
        long revision,
        ObjectTypeStatus status,
        IEnumerable<ObjectFieldDefinition> fields,
        DateTimeOffset? publishedAt,
        DateTimeOffset? retiredAt)
    {
        Id = id;
        Code = code;
        Name = name;
        Version = version;
        Revision = revision;
        Status = status;
        _fields = [.. fields];
        PublishedAt = publishedAt;
        RetiredAt = retiredAt;
    }

    public Guid Id { get; }

    public string Code { get; }

    public string Name { get; private set; }

    public long Version { get; private set; }

    public long Revision { get; private set; }

    public ObjectTypeStatus Status { get; private set; }

    public IReadOnlyList<ObjectFieldDefinition> Fields => _fields;

    public DateTimeOffset? PublishedAt { get; private set; }

    public DateTimeOffset? RetiredAt { get; private set; }

    public static ObjectType Create(
        string code,
        string name,
        IEnumerable<ObjectFieldDefinition> fields,
        ObjectTypeStatus status = ObjectTypeStatus.Published)
    {
        var normalizedFields = ValidateFields(fields);
        return new ObjectType(
            Guid.NewGuid(),
            DomainChecks.RequiredCode(code, "对象类型编码"),
            DomainChecks.Required(name, "对象类型名称", 128),
            1,
            1,
            status,
            normalizedFields,
            status == ObjectTypeStatus.Published ? DateTimeOffset.UtcNow : null,
            null);
    }

    public static ObjectType CreateDraft(
        string code,
        string name,
        IEnumerable<ObjectFieldDefinition> fields) =>
        Create(code, name, fields, ObjectTypeStatus.Draft);

    public static ObjectType Restore(
        Guid id,
        string code,
        string name,
        long version,
        IEnumerable<ObjectFieldDefinition> fields,
        ObjectTypeStatus status = ObjectTypeStatus.Published,
        long revision = 1,
        DateTimeOffset? publishedAt = null,
        DateTimeOffset? retiredAt = null)
    {
        var normalizedFields = ValidateFields(fields);
        return new ObjectType(
            DomainChecks.RequiredId(id, "对象类型"),
            DomainChecks.RequiredCode(code, "对象类型编码"),
            DomainChecks.Required(name, "对象类型名称", 128),
            version,
            revision,
            status,
            normalizedFields,
            publishedAt,
            retiredAt);
    }

    public void UpdateDraft(
        string name,
        IEnumerable<ObjectFieldDefinition> fields,
        long expectedRevision)
    {
        EnsureRevision(expectedRevision);
        if (Status == ObjectTypeStatus.Retired)
        {
            throw new ConflictException(ErrorCodes.Conflict, "已停用对象类型必须先恢复。");
        }

        var normalizedFields = ValidateFields(fields);
        Name = DomainChecks.Required(name, "对象类型名称", 128);
        _fields.Clear();
        _fields.AddRange(normalizedFields);
        Version++;
        Revision++;
        Status = ObjectTypeStatus.Draft;
        PublishedAt = null;
        RetiredAt = null;
    }

    public void ValidateModel(long expectedRevision)
    {
        EnsureRevision(expectedRevision);
        if (Status is not (ObjectTypeStatus.Draft or ObjectTypeStatus.Validated))
        {
            throw new ConflictException(ErrorCodes.Conflict, "只有草稿版本可以校验。");
        }

        _ = ValidateFields(_fields);
        Status = ObjectTypeStatus.Validated;
        Revision++;
    }

    public void Publish(long expectedRevision, DateTimeOffset now)
    {
        EnsureRevision(expectedRevision);
        if (Status is not (ObjectTypeStatus.Draft or ObjectTypeStatus.Validated))
        {
            throw new ConflictException(ErrorCodes.Conflict, "只有草稿或已校验版本可以发布。");
        }

        _ = ValidateFields(_fields);
        Status = ObjectTypeStatus.Published;
        PublishedAt = now.ToUniversalTime();
        RetiredAt = null;
        Revision++;
    }

    public void Retire(long expectedRevision, DateTimeOffset now)
    {
        EnsureRevision(expectedRevision);
        if (Status != ObjectTypeStatus.Published)
        {
            throw new ConflictException(ErrorCodes.Conflict, "只有已发布对象类型可以停用。");
        }

        Status = ObjectTypeStatus.Retired;
        RetiredAt = now.ToUniversalTime();
        Revision++;
    }

    public void Restore(long expectedRevision, DateTimeOffset now)
    {
        EnsureRevision(expectedRevision);
        if (Status != ObjectTypeStatus.Retired)
        {
            throw new ConflictException(ErrorCodes.Conflict, "只有已停用对象类型可以恢复。");
        }

        Status = ObjectTypeStatus.Published;
        PublishedAt = now.ToUniversalTime();
        RetiredAt = null;
        Revision++;
    }

    private void EnsureRevision(long expectedRevision)
    {
        if (expectedRevision != Revision)
        {
            throw new ConflictException(ErrorCodes.Conflict, "对象类型已被其他操作修改，请刷新后重试。");
        }
    }

    private static ObjectFieldDefinition[] ValidateFields(
        IEnumerable<ObjectFieldDefinition> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        var normalizedFields = fields.ToArray();
        if (normalizedFields.Length == 0)
        {
            throw new ValidationException("对象类型至少需要一个字段。");
        }

        foreach (var field in normalizedFields)
        {
            field.Validate();
        }

        if (normalizedFields.Select(field => field.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() != normalizedFields.Length)
        {
            throw new ValidationException("对象字段 Code 不能重复。");
        }

        return normalizedFields;
    }
}
