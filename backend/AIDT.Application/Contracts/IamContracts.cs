namespace AIDT.Application.Contracts;

public sealed record DepartmentTreeNodeDto(
    string Id,
    string? ParentId,
    string Code,
    string Name,
    string HierarchyType,
    string Path,
    int SortOrder,
    string? ManagerUserId,
    string? ManagerDisplayName,
    long Version,
    bool IsActive,
    IReadOnlyList<DepartmentTreeNodeDto> Children);

public sealed record CreateIamDepartmentRequest(
    string Code,
    string Name,
    string? ParentId,
    string HierarchyType,
    Guid? ManagerUserId,
    int SortOrder = 0);

public sealed record UpdateIamDepartmentRequest(
    long ExpectedVersion,
    string Name,
    string? ParentId,
    string HierarchyType,
    Guid? ManagerUserId,
    int SortOrder,
    bool IsActive);

public sealed record IamUserListItemDto(
    string Id,
    string UserName,
    string DisplayName,
    bool IsServiceIdentity,
    bool IsActive,
    string InvitationStatus,
    string PrimaryDepartmentId,
    string PrimaryDepartmentName,
    long Version,
    IReadOnlyList<string> RoleCodes,
    IReadOnlyList<string> DataScopeTypes,
    DateTimeOffset CreatedAt);

public sealed record UserDepartmentMembershipDto(
    string DepartmentId,
    string DepartmentName,
    bool IsPrimary,
    bool IsManager,
    bool IsActive);

public sealed record UserDataScopeDto(
    string ScopeType,
    string ScopeReference,
    string DisplayName);

public sealed record IamUserDetailDto(
    IamUserListItemDto User,
    IReadOnlyList<UserDepartmentMembershipDto> Memberships,
    IReadOnlyList<UserDataScopeDto> DataScopes,
    IReadOnlyList<RoleMemberDto> Roles);

public sealed record InviteUserRequest(
    string UserName,
    string DisplayName,
    Guid DepartmentId,
    bool IsPrimary = true,
    bool IsManager = false);

public sealed record BulkImportUserItem(
    string UserName,
    string DisplayName,
    Guid DepartmentId,
    bool IsPrimary = true,
    bool IsManager = false);

public sealed record BulkImportUsersRequest(IReadOnlyList<BulkImportUserItem> Users);

public sealed record SetUserMembershipsRequest(
    IReadOnlyList<SetUserMembershipItem> Memberships,
    long ExpectedVersion);

public sealed record SetUserMembershipItem(
    Guid DepartmentId,
    bool IsPrimary,
    bool IsManager,
    bool IsActive = true);

public sealed record SetUserDataScopesRequest(
    IReadOnlyList<SetUserDataScopeItem> Scopes,
    long ExpectedVersion);

public sealed record SetUserDataScopeItem(
    string ScopeType,
    string? ScopeReference);

public sealed record IamImpactPreviewDto(
    string EntityType,
    string EntityId,
    IReadOnlyList<string> AffectedItems,
    IReadOnlyList<string> Warnings,
    bool CanApply);

public sealed record PermissionCatalogItemDto(
    string Id,
    string Code,
    string Resource,
    string Action,
    string DisplayName,
    string RiskLevel);

public sealed record RoleDto(
    string Id,
    string Code,
    string Name,
    string RoleType,
    string? Description,
    bool IsSystem,
    bool IsActive,
    long Version,
    int MemberCount,
    int PermissionCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<string>? PermissionIds = null);

public sealed record CreateRoleRequest(
    string Code,
    string Name,
    string RoleType,
    string? Description);

public sealed record CopyRoleRequest(string Code, string Name, string? Description);

public sealed record UpdateRoleRequest(
    long ExpectedVersion,
    string Name,
    string RoleType,
    string? Description,
    bool IsActive);

public sealed record SetRolePermissionsRequest(
    IReadOnlyList<Guid> PermissionIds,
    long ExpectedRoleVersion);

public sealed record RoleMemberDto(
    string UserId,
    string UserName,
    string DisplayName,
    string ScopeType,
    string ScopeReference,
    string ScopeDisplayName,
    bool IsActive,
    DateTimeOffset AssignedAt);

public sealed record SetRoleMembersRequest(
    IReadOnlyList<SetRoleMemberItem> Members,
    long ExpectedRoleVersion);

public sealed record SetRoleMemberItem(
    Guid UserId,
    string ScopeType,
    string ScopeReference,
    bool IsActive = true);

public sealed record AccessChangeRequestDto(
    string Id,
    string RequestType,
    string? TargetUserId,
    string? TargetRoleId,
    string RiskReason,
    string Status,
    string RequestedBy,
    string RequestedByDisplayName,
    DateTimeOffset RequestedAt,
    string? ReviewedBy,
    DateTimeOffset? ReviewedAt,
    string? ReviewComment);

public sealed record ReviewAccessChangeRequest(string? Comment);

public sealed record IdentityAdministrationSnapshotDto(
    IReadOnlyList<DepartmentTreeNodeDto> OrganizationTree,
    PagedResult<IamUserListItemDto> Users,
    IReadOnlyList<PermissionCatalogItemDto> Permissions,
    IReadOnlyList<RoleDto> Roles,
    IReadOnlyList<AccessChangeRequestDto> PendingAccessRequests);
