using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AIDT.Application.Contracts;
using AIDT.Domain.Common;

namespace AIDT.Infrastructure.Services;

internal static class McpSecurity
{
    public static string CreateOpaqueToken(string prefix) =>
        $"{prefix}_{Base64Url(RandomNumberGenerator.GetBytes(32))}";

    public static string CreateClientId() =>
        $"aidt_mcp_client_{Base64Url(RandomNumberGenerator.GetBytes(18))}";

    public static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public static string ComputeCodeChallenge(string verifier) =>
        Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

    public static bool FixedTimeEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(left),
            Encoding.UTF8.GetBytes(right));

    public static IReadOnlyList<string> ReadScopes(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static IReadOnlyList<Guid> ReadDepartmentIds(string json)
    {
        try
        {
            return (JsonSerializer.Deserialize<List<Guid>>(json) ?? [])
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string WriteJson<T>(T value) =>
        JsonSerializer.Serialize(value);

    public static void EnsureScopeSubset(
        IReadOnlyList<string> requested,
        IReadOnlyList<string> allowed)
    {
        if (requested.Count == 0 || requested.Any(scope => !allowed.Contains(scope, StringComparer.Ordinal)))
        {
            throw new ValidationException("MCP_SCOPE_INVALID", "请求的 MCP 工具作用域不受支持或超出客户端注册范围。");
        }
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
