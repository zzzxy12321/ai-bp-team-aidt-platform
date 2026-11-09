namespace AIDT.Domain.Common;

public static class DataEnvironment
{
    public static readonly IReadOnlySet<string> AllowedCodes =
        new HashSet<string>(["demo", "test", "prod"], StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string code)
    {
        var normalized = DomainChecks.Required(code, "数据环境", 16).ToLowerInvariant();
        if (!AllowedCodes.Contains(normalized))
        {
            throw new ValidationException("数据环境必须是 demo、test 或 prod。");
        }

        return normalized;
    }

    public static bool IsTestEnvironment(string code) =>
        !string.Equals(Normalize(code), "prod", StringComparison.Ordinal);
}
