using AIDT.Domain.Common;

namespace AIDT.Domain.Identity;

public enum AccessAction
{
    Read,
    Create,
    Update,
    Delete,
    Submit,
    Approve,
    Return,
    Terminate,
    WriteBack,
    PaymentRequest,
    QualityRelease,
    Stop,
    Manage,
    Confirm,
    Assign
}

public sealed record DataScope(string ScopeType, string ScopeReference)
{
    public static DataScope Organization(Guid organizationId) => new("organization", organizationId.ToString("D"));

    public static DataScope Department(Guid departmentId) => new("department", departmentId.ToString("D"));
}

public sealed record PermissionGrant(string Resource, AccessAction Action)
{
    public bool Matches(string resource, AccessAction action) =>
        string.Equals(Resource, resource, StringComparison.OrdinalIgnoreCase) && Action == action;
}

public enum IdentityRoleType
{
    System,
    Business,
    AgentService
}

public sealed record RoleAssignment(
    Guid RoleId,
    string RoleCode,
    string ScopeType,
    string ScopeReference);

public sealed record UserPrincipal(
    Guid UserId,
    Guid OrganizationId,
    bool IsServiceIdentity,
    IReadOnlySet<string> Roles,
    IReadOnlyList<PermissionGrant> Permissions,
    IReadOnlyList<DataScope> DataScopes)
{
    public string DisplayName { get; init; } = string.Empty;

    public IReadOnlyList<RoleAssignment> RoleAssignments { get; init; } = [];

    public bool IsPlatformAdmin => Roles.Contains("platform-admin");

    public IReadOnlySet<Guid> DepartmentIds => DataScopes
        .Where(scope => string.Equals(scope.ScopeType, "department", StringComparison.OrdinalIgnoreCase))
        .Select(scope => Guid.TryParse(scope.ScopeReference, out var id) ? id : Guid.Empty)
        .Where(id => id != Guid.Empty)
        .ToHashSet();

    public bool HasPermission(string resource, AccessAction action) =>
        Permissions.Any(permission => permission.Matches(resource, action));

    public bool HasRoleInScope(string roleCode, Guid departmentId)
    {
        var assignments = RoleAssignments
            .Where(item => string.Equals(item.RoleCode, roleCode, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (assignments.Count == 0)
        {
            return Roles.Contains(roleCode);
        }

        return assignments.Any(item =>
            string.Equals(item.ScopeType, "organization", StringComparison.OrdinalIgnoreCase)
            && string.Equals(
                item.ScopeReference,
                OrganizationId.ToString("D"),
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.ScopeType, "department", StringComparison.OrdinalIgnoreCase)
            && Guid.TryParse(item.ScopeReference, out var assignedDepartmentId)
            && assignedDepartmentId == departmentId);
    }

    public void EnsureAuthenticated()
    {
        if (UserId == Guid.Empty)
        {
            throw new PermissionException(ErrorCodes.AuthenticationRequired, "需要登录后继续。");
        }
    }

    public void EnsureCan(string resource, AccessAction action)
    {
        EnsureAuthenticated();
        if (!HasPermission(resource, action))
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "当前身份没有执行该动作的权限。");
        }
    }

    public void EnsureOrganizationAccess(Guid organizationId)
    {
        if (organizationId != OrganizationId
            || DataScopes.All(scope =>
                !string.Equals(scope.ScopeType, "organization", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(scope.ScopeReference, OrganizationId.ToString("D"), StringComparison.OrdinalIgnoreCase)))
        {
            throw new PermissionException(ErrorCodes.DataScopeDenied, "当前身份没有该组织的数据范围。");
        }
    }

    public void EnsureDepartmentAccess(Guid departmentId)
    {
        EnsureAuthenticated();
        if (IsPlatformAdmin)
        {
            return;
        }

        if (!DepartmentIds.Contains(departmentId))
        {
            throw new PermissionException(ErrorCodes.DepartmentScopeDenied, "当前身份没有该部门的数据范围。");
        }
    }
}
