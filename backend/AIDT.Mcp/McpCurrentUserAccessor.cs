using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;

namespace AIDT.Mcp;

public sealed class McpCurrentUserAccessor(
    IHttpContextAccessor httpContextAccessor,
    McpAmbientIdentity ambientIdentity) : ICurrentUserAccessor
{
    public UserPrincipal GetRequiredUser()
    {
        if (httpContextAccessor.HttpContext?.Items.TryGetValue(
                McpCallContextAccessor.IdentityItemKey,
                out var value) == true
            && value is McpTokenIdentity identity)
        {
            return identity.User;
        }

        if (ambientIdentity.Current is { } current)
        {
            return current.User;
        }

        throw new PermissionException(
            ErrorCodes.AuthenticationRequired,
            "MCP 请求缺少已经验证的用户身份。");
    }
}
