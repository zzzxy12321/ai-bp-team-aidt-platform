using System.Net;
using System.Security.Claims;
using AIDT.Api.Contracts;
using AIDT.Api.Identity;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIDT.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class AuthController(
    IUserRepository userRepository,
    ICurrentUserAccessor currentUserAccessor,
    IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet("auth/csrf")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<object>> GetCsrfToken(
        [FromServices] Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(ApiResponse.Ok(
            new { headerName = "X-CSRF-TOKEN", requestToken = tokens.RequestToken },
            HttpContext.TraceIdentifier));
    }

    [HttpPost("auth/demo-login")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<ActionResult<ApiResponse<DemoLoginResponse>>> DemoLogin(
        [FromBody] DemoLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            throw new ServiceUnavailableException(ErrorCodes.ServiceUnavailable, "本地演示身份仅在 Development 环境开放。");
        }

        var remoteAddress = HttpContext.Connection.RemoteIpAddress;
        if (remoteAddress is not null && !IPAddress.IsLoopback(remoteAddress))
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "本地演示身份仅允许本机回环访问。");
        }

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            throw new ValidationException("演示用户名不能为空。");
        }

        var user = await userRepository.GetPrincipalByUserNameAsync(request.UserName.Trim(), cancellationToken)
            ?? throw new PermissionException(ErrorCodes.AuthenticationRequired, "演示用户不存在或已停用。");
        if (user.IsServiceIdentity)
        {
            throw new PermissionException(
                ErrorCodes.PermissionDenied,
                "数字员工服务身份不能登录治理页面。");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString("D")),
            new(ClaimTypes.Name, user.DisplayName),
            new(HttpContextCurrentUserAccessor.OrganizationClaim, user.OrganizationId.ToString("D")),
            new(HttpContextCurrentUserAccessor.ServiceIdentityClaim, user.IsServiceIdentity.ToString())
        };
        var scopedRoleCodes = user.RoleAssignments
            .Select(assignment => assignment.RoleCode)
            .ToHashSet(StringComparer.Ordinal);
        claims.AddRange(user.Roles
            .Where(role => !scopedRoleCodes.Contains(role))
            .Select(role => new Claim(ClaimTypes.Role, role)));
        claims.Add(new Claim(
            HttpContextCurrentUserAccessor.PermissionsClaim,
            string.Join(
                ';',
                user.Permissions.Select(permission =>
                    $"{permission.Resource}:{permission.Action}"))));
        claims.Add(new Claim(
            HttpContextCurrentUserAccessor.DataScopesClaim,
            string.Join(
                ';',
                user.DataScopes.Select(scope =>
                    $"{scope.ScopeType}:{scope.ScopeReference}"))));
        claims.Add(new Claim(
            HttpContextCurrentUserAccessor.AuthorizationVersionClaim,
            HttpContextCurrentUserAccessor.ComputeAuthorizationFingerprint(user)));
        if (user.RoleAssignments.Count > 0)
        {
            claims.Add(new Claim(
                HttpContextCurrentUserAccessor.RoleAssignmentsClaim,
                string.Join(
                    ';',
                    user.RoleAssignments.Select(assignment =>
                        $"{assignment.RoleCode}|{assignment.ScopeType}|{assignment.ScopeReference}"))));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = false });

        return Ok(ApiResponse.Ok(
            new DemoLoginResponse(
                user.UserId.ToString("D"),
                user.DisplayName,
                user.Roles.ToList(),
                user.Permissions.Select(item => new PermissionDto(
                    item.Resource,
                    item.Action.ToString())).ToList()),
            HttpContext.TraceIdentifier,
            "本地演示身份已登录，不代表生产 SSO。"));
    }

    [HttpGet("auth/session")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<SessionDto>>> GetSession(
        CancellationToken cancellationToken)
    {
        var user = currentUserAccessor.GetRequiredUser();
        var authorizationState = "Current";
        string? authorizationMessage = null;
        if (user.UserId != Guid.Empty)
        {
            var current = await userRepository.GetPrincipalAsync(user.UserId, cancellationToken);
            var cookieVersion = HttpContext.User.FindFirstValue(
                HttpContextCurrentUserAccessor.AuthorizationVersionClaim);
            var currentVersion = current is null
                ? null
                : HttpContextCurrentUserAccessor.ComputeAuthorizationFingerprint(current);
            if (current is null
                || string.IsNullOrWhiteSpace(cookieVersion)
                || !string.Equals(cookieVersion, currentVersion, StringComparison.Ordinal))
            {
                authorizationState = "Stale";
                authorizationMessage = "权限已更新，请重新登录。";
            }
        }
        else
        {
            authorizationState = "Anonymous";
        }

        return Ok(ApiResponse.Ok(
            new SessionDto(
                user.UserId == Guid.Empty ? string.Empty : user.UserId.ToString("D"),
                user.UserId == Guid.Empty ? "未登录" : user.DisplayName,
                user.OrganizationId == Guid.Empty ? string.Empty : user.OrganizationId.ToString("D"),
                user.UserId != Guid.Empty,
                user.IsServiceIdentity,
                user.Roles.ToList(),
                authorizationState,
                authorizationMessage),
            HttpContext.TraceIdentifier));
    }

    [Authorize]
    [HttpGet("permissions/me")]
    public ActionResult<ApiResponse<IReadOnlyList<PermissionDto>>> GetMyPermissions()
    {
        var user = currentUserAccessor.GetRequiredUser();
        return Ok(ApiResponse.Ok<IReadOnlyList<PermissionDto>>(
            user.Permissions.Select(item => new PermissionDto(item.Resource, item.Action.ToString())).ToList(),
            HttpContext.TraceIdentifier));
    }

    [Authorize]
    [HttpPost("auth/logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(ApiResponse.Ok(new { signedOut = true }, HttpContext.TraceIdentifier));
    }
}
