using AIDT.Application.Abstractions;
using AIDT.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Persistence.Repositories;

public sealed class SqlUserRepository(AidtDbContext dbContext) : IUserRepository
{
    public Task<UserPrincipal?> GetPrincipalAsync(Guid userId, CancellationToken cancellationToken) =>
        LoadAsync(user => user.Id == userId, cancellationToken);

    public Task<UserPrincipal?> GetPrincipalByUserNameAsync(
        string userName,
        CancellationToken cancellationToken) =>
        LoadAsync(user => user.UserName == userName && user.IsActive, cancellationToken);

    private async Task<UserPrincipal?> LoadAsync(
        System.Linq.Expressions.Expression<Func<UserEntity, bool>> predicate,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(predicate, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var legacyRoles = await (
            from userRole in dbContext.UserRoles
            join role in dbContext.Roles on userRole.RoleId equals role.Id
            where userRole.UserId == user.Id && role.IsActive
            select role.Code).ToListAsync(cancellationToken);
        var scopedAssignments = await (
            from assignment in dbContext.UserRoleAssignments.AsNoTracking()
            join role in dbContext.Roles.AsNoTracking() on assignment.RoleId equals role.Id
            where assignment.UserId == user.Id && assignment.IsActive && role.IsActive
            select new
            {
                role.Id,
                role.Code,
                assignment.ScopeType,
                assignment.ScopeReference
            }).ToListAsync(cancellationToken);
        var roles = legacyRoles
            .Concat(scopedAssignments.Select(item => item.Code))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var legacyPermissions = await (
            from userRole in dbContext.UserRoles
            join role in dbContext.Roles on userRole.RoleId equals role.Id
            join rolePermission in dbContext.RolePermissions on userRole.RoleId equals rolePermission.RoleId
            join permission in dbContext.Permissions on rolePermission.PermissionId equals permission.Id
            where userRole.UserId == user.Id && role.IsActive
            select new { permission.Resource, permission.Action }).Distinct().ToListAsync(cancellationToken);
        var scopedPermissions = await (
            from assignment in dbContext.UserRoleAssignments.AsNoTracking()
            join role in dbContext.Roles.AsNoTracking() on assignment.RoleId equals role.Id
            join rolePermission in dbContext.RolePermissions.AsNoTracking()
                on assignment.RoleId equals rolePermission.RoleId
            join permission in dbContext.Permissions.AsNoTracking()
                on rolePermission.PermissionId equals permission.Id
            where assignment.UserId == user.Id && assignment.IsActive && role.IsActive
            select new { permission.Resource, permission.Action })
            .Distinct()
            .ToListAsync(cancellationToken);
        var permissions = legacyPermissions
            .Concat(scopedPermissions)
            .Distinct()
            .ToList();

        var scopes = await dbContext.DataScopes.AsNoTracking()
            .Where(scope => scope.UserId == user.Id)
            .Select(scope => new DataScope(scope.ScopeType, scope.ScopeReference))
            .ToListAsync(cancellationToken);
        var departmentScopes = await dbContext.UserDepartmentMemberships.AsNoTracking()
            .Where(membership => membership.UserId == user.Id && membership.IsActive)
            .Select(membership => new DataScope("department", membership.DepartmentId.ToString()))
            .ToListAsync(cancellationToken);
        scopes = scopes
            .Concat(departmentScopes)
            .DistinctBy(scope => $"{scope.ScopeType}:{scope.ScopeReference}", StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new UserPrincipal(
            user.Id,
            user.OrganizationId,
            user.IsServiceIdentity,
            roles.ToHashSet(StringComparer.Ordinal),
            permissions.Select(item => new PermissionGrant(
                item.Resource,
                Enum.Parse<AccessAction>(item.Action, ignoreCase: true))).ToList(),
            scopes)
        {
            DisplayName = user.DisplayName,
            RoleAssignments = scopedAssignments
                .Select(item => new RoleAssignment(
                    item.Id,
                    item.Code,
                    item.ScopeType,
                    item.ScopeReference))
                .ToList()
        };
    }
}

public sealed class SqlWorkflowCatalogRepository(AidtDbContext dbContext) : IWorkflowCatalogRepository
{
    public async Task<WorkflowVersionDefinition?> GetVersionAsync(
        Guid workflowVersionId,
        CancellationToken cancellationToken)
    {
        var entity = await (
            from version in dbContext.WorkflowVersions.AsNoTracking()
            join definition in dbContext.WorkflowDefinitions.AsNoTracking()
                on version.WorkflowDefinitionId equals definition.Id
            where version.Id == workflowVersionId
            select new { definition, version }).SingleOrDefaultAsync(cancellationToken);
        return entity is null
            ? null
            : new WorkflowVersionDefinition(
                entity.definition.Id,
                entity.version.Id,
                entity.definition.Code,
                entity.definition.Name,
                entity.version.VersionNumber,
                entity.version.DefinitionJson);
    }

    public async Task<IReadOnlyList<WorkflowVersionDefinition>> ListPublishedAsync(
        CancellationToken cancellationToken) =>
        await (
            from version in dbContext.WorkflowVersions.AsNoTracking()
            join definition in dbContext.WorkflowDefinitions.AsNoTracking()
                on version.WorkflowDefinitionId equals definition.Id
            where version.Status == "Published" && definition.Status == "Published"
            orderby definition.Code, version.VersionNumber descending
            select new WorkflowVersionDefinition(
                definition.Id,
                version.Id,
                definition.Code,
                definition.Name,
                version.VersionNumber,
                version.DefinitionJson))
            .ToListAsync(cancellationToken);
}
