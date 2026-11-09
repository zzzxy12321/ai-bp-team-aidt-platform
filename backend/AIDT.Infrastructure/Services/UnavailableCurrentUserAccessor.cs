using AIDT.Application.Abstractions;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;

namespace AIDT.Infrastructure.Services;

internal sealed class UnavailableCurrentUserAccessor : ICurrentUserAccessor
{
    public UserPrincipal GetRequiredUser() =>
        throw new PermissionException(
            ErrorCodes.AuthenticationRequired,
            "当前宿主未配置真人身份访问器。");
}
