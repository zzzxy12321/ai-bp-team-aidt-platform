using System.Security.Claims;
using AIDT.Api.Identity;
using AIDT.Domain.Identity;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace AIDT.UnitTests.Api;

public sealed class IdentityAccessorTests
{
    [Fact]
    public void PackedClaims_AreParsedWithoutLosingPermissionsOrScopes()
    {
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString("D")),
                new Claim(ClaimTypes.Name, "项目经理"),
                new Claim(HttpContextCurrentUserAccessor.OrganizationClaim, organizationId.ToString("D")),
                new Claim(HttpContextCurrentUserAccessor.PermissionsClaim, "project:Read;project-task:Create;milestone:Manage"),
                new Claim(HttpContextCurrentUserAccessor.DataScopesClaim, $"organization:{organizationId:D};department:{departmentId:D}")
            ], "Test"))
        };
        var accessor = new HttpContextCurrentUserAccessor(new HttpContextAccessor { HttpContext = context });

        var user = accessor.GetRequiredUser();

        user.UserId.ShouldBe(userId);
        user.OrganizationId.ShouldBe(organizationId);
        user.DisplayName.ShouldBe("项目经理");
        user.HasPermission("project", AccessAction.Read).ShouldBeTrue();
        user.HasPermission("project-task", AccessAction.Create).ShouldBeTrue();
        user.HasPermission("milestone", AccessAction.Manage).ShouldBeTrue();
        user.DepartmentIds.ShouldContain(departmentId);
    }

    [Fact]
    public void CompactRoleAssignments_RestoreScopedRoleWithoutResendingRoleClaim()
    {
        var organizationId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("D")),
                new Claim(HttpContextCurrentUserAccessor.OrganizationClaim, organizationId.ToString("D")),
                new Claim(
                    HttpContextCurrentUserAccessor.RoleAssignmentsClaim,
                    $"department-flow-owner|department|{departmentId:D}")
            ], "Test"))
        };
        var accessor = new HttpContextCurrentUserAccessor(new HttpContextAccessor { HttpContext = context });

        var user = accessor.GetRequiredUser();

        user.Roles.ShouldContain("department-flow-owner");
        user.HasRoleInScope("department-flow-owner", departmentId).ShouldBeTrue();
        user.HasRoleInScope("department-flow-owner", Guid.NewGuid()).ShouldBeFalse();
    }

    [Fact]
    public void LegacyPermissionAndScopeClaims_RemainCompatible()
    {
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString("D")),
                new Claim(HttpContextCurrentUserAccessor.OrganizationClaim, organizationId.ToString("D")),
                new Claim(HttpContextCurrentUserAccessor.PermissionClaim, "event-inbox:Confirm"),
                new Claim(HttpContextCurrentUserAccessor.DataScopeClaim, $"department:{departmentId:D}")
            ], "Test"))
        };
        var accessor = new HttpContextCurrentUserAccessor(new HttpContextAccessor { HttpContext = context });

        var user = accessor.GetRequiredUser();

        user.HasPermission("event-inbox", AccessAction.Confirm).ShouldBeTrue();
        user.DepartmentIds.ShouldContain(departmentId);
    }
}
