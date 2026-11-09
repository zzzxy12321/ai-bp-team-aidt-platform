using System.Security.Claims;
using AIDT.Domain.Identity;

namespace AIDT.Infrastructure.Security;

public static class AidtClaimsPrincipalMapper
{
    public const string OrganizationClaim = "aidt:organization_id";
    public const string ServiceIdentityClaim = "aidt:service_identity";
    public const string PermissionsClaim = "aidt:permissions";
    public const string PermissionClaim = "aidt:permission";
    public const string DataScopesClaim = "aidt:data_scopes";
    public const string DataScopeClaim = "aidt:data_scope";
    public const string RoleAssignmentsClaim = "aidt:role_assignments";
    public const string AuthorizationVersionClaim = "aidt:authorization_version";

    public static UserPrincipal Map(ClaimsPrincipal? principal)
    {
        var userId = Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedUserId)
            ? parsedUserId
            : Guid.Empty;
        var organizationId = Guid.TryParse(principal?.FindFirstValue(OrganizationClaim), out var parsedOrganizationId)
            ? parsedOrganizationId
            : Guid.Empty;
        var legacyPermissions = principal?.FindAll(PermissionClaim)
            .Select(claim => ParsePermission(claim.Value))
            .OfType<PermissionGrant>()
            .ToList() ?? [];
        var packedPermissions = principal?.FindFirstValue(PermissionsClaim)
            ?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParsePermission)
            .OfType<PermissionGrant>()
            ?? [];
        var permissions = legacyPermissions.Concat(packedPermissions)
            .Distinct()
            .ToList();
        var legacyScopes = principal?.FindAll(DataScopeClaim)
            .Select(claim => ParseDataScope(claim.Value))
            .OfType<DataScope>()
            .ToList() ?? [];
        var packedScopes = principal?.FindFirstValue(DataScopesClaim)
            ?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseDataScope)
            .OfType<DataScope>()
            ?? [];
        var scopes = legacyScopes.Concat(packedScopes)
            .Distinct()
            .ToList();
        var roleAssignments = principal?.FindFirstValue(RoleAssignmentsClaim)
            ?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseRoleAssignment)
            .OfType<RoleAssignment>()
            .ToList()
            ?? [];
        var roles = principal?.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Concat(roleAssignments.Select(assignment => assignment.RoleCode))
            .ToHashSet(StringComparer.Ordinal)
            ?? [];

        return new UserPrincipal(
            userId,
            organizationId,
            bool.TryParse(principal?.FindFirstValue(ServiceIdentityClaim), out var service) && service,
            roles,
            permissions,
            scopes)
        {
            DisplayName = principal?.Identity?.Name ?? "未登录",
            RoleAssignments = roleAssignments
        };
    }

    private static PermissionGrant? ParsePermission(string value)
    {
        var parts = value.Split(':', 2);
        return parts.Length == 2 && Enum.TryParse<AccessAction>(parts[1], true, out var action)
            ? new PermissionGrant(parts[0], action)
            : null;
    }

    private static DataScope? ParseDataScope(string value)
    {
        var parts = value.Split(':', 2);
        return parts.Length == 2 ? new DataScope(parts[0], parts[1]) : null;
    }

    private static RoleAssignment? ParseRoleAssignment(string value)
    {
        var parts = value.Split('|');
        if (parts.Length == 3 && parts[2].Length > 0)
        {
            return new RoleAssignment(Guid.Empty, parts[0], parts[1], parts[2]);
        }

        return parts.Length == 4
            && Guid.TryParse(parts[0], out var roleId)
            && parts[3].Length > 0
                ? new RoleAssignment(roleId, parts[1], parts[2], parts[3])
                : null;
    }
}
