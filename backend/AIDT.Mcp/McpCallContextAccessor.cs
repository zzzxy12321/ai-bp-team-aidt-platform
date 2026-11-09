using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Common;
using Microsoft.Extensions.Options;

namespace AIDT.Mcp;

public interface IMcpCallContextAccessor
{
    Task<McpCallContext> GetRequiredAsync(CancellationToken cancellationToken);
}

public sealed record McpCallContext(
    McpTokenIdentity Identity,
    string TraceId);

public sealed class McpCallContextAccessor(
    IHttpContextAccessor httpContextAccessor,
    IMcpOAuthService oauthService,
    McpAmbientIdentity ambientIdentity,
    IClock clock,
    IOptions<AidtMcpOptions> options) : IMcpCallContextAccessor
{
    public const string IdentityItemKey = "AIDT.Mcp.Identity";

    public async Task<McpCallContext> GetRequiredAsync(CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.Items.TryGetValue(IdentityItemKey, out var value) == true
            && value is McpTokenIdentity httpIdentity)
        {
            return new McpCallContext(httpIdentity, httpContext.TraceIdentifier);
        }

        var token = Environment.GetEnvironmentVariable("AIDT_MCP_TOKEN");
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new PermissionException(
                ErrorCodes.AuthenticationRequired,
                "stdio 模式需要 AIDT_MCP_TOKEN；请先通过 AIDT MCP 浏览器授权取得短期令牌。");
        }

        var validation = await oauthService.ValidateAccessTokenAsync(token, cancellationToken);
        if (validation.Identity is null)
        {
            throw new PermissionException(
                ErrorCodes.AuthenticationRequired,
                validation.ErrorDescription ?? "MCP 访问令牌无效。");
        }

        await oauthService.MarkAccessTokenUsedAsync(
            validation.Identity.TokenId,
            clock.UtcNow,
            cancellationToken);
        _ = options.Value;
        ambientIdentity.Current = validation.Identity;
        return new McpCallContext(
            validation.Identity,
            $"stdio-{Guid.NewGuid():N}");
    }
}
