using AIDT.Domain.Identity;
using AIDT.Infrastructure.Security;

namespace AIDT.Mcp;

public interface IMcpCookieSessionAccessor
{
    UserPrincipal GetRequiredUser();
}

public sealed class McpCookieSessionAccessor(IHttpContextAccessor httpContextAccessor) : IMcpCookieSessionAccessor
{
    public UserPrincipal GetRequiredUser() =>
        AidtClaimsPrincipalMapper.Map(httpContextAccessor.HttpContext?.User);
}
