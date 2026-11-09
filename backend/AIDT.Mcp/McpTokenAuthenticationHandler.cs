using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AIDT.Application.Abstractions;
using AIDT.Domain.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AIDT.Mcp;

public sealed partial class McpTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    AidtMcpOptions mcpOptions) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header)
            || !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(header.Parameter))
        {
            return AuthenticateResult.NoResult();
        }

        var oauthService = Context.RequestServices.GetRequiredService<IMcpOAuthService>();
        var validation = await oauthService.ValidateAccessTokenAsync(
            header.Parameter,
            Context.RequestAborted);
        if (validation.Identity is null)
        {
            await TryAuditFailureAsync(
                validation.ErrorCode ?? "invalid_token",
                validation.ErrorDescription);
            return AuthenticateResult.Fail(
                validation.ErrorDescription ?? "MCP access token is invalid.");
        }

        var identity = validation.Identity;
        Context.Items[McpCallContextAccessor.IdentityItemKey] = identity;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, identity.UserId.ToString("D")),
            new(ClaimTypes.Name, identity.User.DisplayName),
            new("mcp:token_id", identity.TokenId.ToString("D")),
            new("mcp:client_id", identity.ClientId),
            new("mcp:client_name", identity.ClientName)
        };
        claims.AddRange(identity.Scopes.Select(scope => new Claim("mcp:scope", scope)));
        claims.AddRange(identity.DepartmentIds.Select(id => new Claim("mcp:department", id.ToString("D"))));
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, Scheme.Name, ClaimTypes.Name, ClaimTypes.Role));
        await oauthService.MarkAccessTokenUsedAsync(
            identity.TokenId,
            DateTimeOffset.UtcNow,
            Context.RequestAborted);
        return AuthenticateResult.Success(
            new AuthenticationTicket(principal, Scheme.Name));
    }

    private async Task TryAuditFailureAsync(string code, string? description)
    {
        try
        {
            var audit = Context.RequestServices.GetRequiredService<IMcpAuditService>();
            await audit.RecordSecurityEventAsync(
                mcpOptions.SystemAuditUserId,
                "auth.failed",
                "McpAccessToken",
                "unknown",
                Context.TraceIdentifier,
                "Denied",
                code,
                description is null
                    ? null
                    : System.Text.Json.JsonSerializer.Serialize(new { message = description }),
                Context.RequestAborted);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogAuditFailure(Logger, exception);
        }
    }

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Warning,
        Message = "MCP authentication failure could not be audited.")]
    private static partial void LogAuditFailure(ILogger logger, Exception exception);
}
