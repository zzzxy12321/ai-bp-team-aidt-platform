using System.Text.RegularExpressions;

namespace AIDT.Domain.Common;

public static partial class DomainChecks
{
    public static string Required(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException($"{fieldName} 不能为空。");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ValidationException($"{fieldName} 长度不能超过 {maxLength}。");
        }

        return normalized;
    }

    public static Guid RequiredId(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
        {
            throw new ValidationException($"{fieldName} 不能为空。");
        }

        return value;
    }

    public static string RequiredCode(string? value, string fieldName, int maxLength = 64)
    {
        var normalized = Required(value, fieldName, maxLength);
        if (!StableCodeRegex().IsMatch(normalized))
        {
            throw new ValidationException($"{fieldName} 只能包含大写字母、数字、下划线和短横线。");
        }

        return normalized;
    }

    public static string RequiredIdentifier(string? value, string fieldName, int maxLength = 64)
    {
        var normalized = Required(value, fieldName, maxLength);
        if (!IdentifierRegex().IsMatch(normalized))
        {
            throw new ValidationException($"{fieldName} 只能包含字母、数字、下划线和短横线，且必须以字母开头。");
        }

        return normalized;
    }

    public static string RequiredSha256(string? value)
    {
        var normalized = Required(value, "SHA256", 64).ToUpperInvariant();
        if (normalized.Length != 64 || normalized.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ValidationException("SHA256 必须是 64 位十六进制字符串。");
        }

        return normalized;
    }

    [GeneratedRegex("^[A-Z][A-Z0-9_-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex StableCodeRegex();

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierRegex();
}
