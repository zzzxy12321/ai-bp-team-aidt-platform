using AIDT.Domain.Identity;

namespace AIDT.UnitTests;

internal static class TestUsers
{
    private static readonly Guid DepartmentId =
        Guid.Parse("51000000-0000-0000-0000-000000000001");

    public static UserPrincipal Authorized(
        Guid? userId = null,
        bool isServiceIdentity = false,
        params PermissionGrant[] permissions) =>
        new(
            userId ?? Guid.NewGuid(),
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            isServiceIdentity,
            new HashSet<string>(["tester"], StringComparer.Ordinal),
            permissions,
            [
                DataScope.Organization(Guid.Parse("10000000-0000-0000-0000-000000000001")),
                DataScope.Department(DepartmentId)
            ]);

    public static UserPrincipal WithoutPermissions(Guid? userId = null) =>
        new(
            userId ?? Guid.NewGuid(),
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            false,
            new HashSet<string>(StringComparer.Ordinal),
            [],
            []);
}
