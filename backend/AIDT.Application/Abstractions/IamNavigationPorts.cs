using AIDT.Application.Contracts;
using AIDT.Domain.Identity;
using AIDT.Domain.Workflows;

namespace AIDT.Application.Abstractions;

public interface IIdentityConfigurationService
{
    Task<IdentityAdministrationSnapshotDto> GetSnapshotAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DepartmentTreeNodeDto>> GetOrganizationTreeAsync(
        UserPrincipal user,
        CancellationToken cancellationToken);

    Task<DepartmentTreeNodeDto> CreateDepartmentAsync(
        UserPrincipal user,
        CreateIamDepartmentRequest request,
        string traceId,
        CancellationToken cancellationToken);

    Task<DepartmentTreeNodeDto> UpdateDepartmentAsync(
        UserPrincipal user,
        Guid departmentId,
        UpdateIamDepartmentRequest request,
        string traceId,
        CancellationToken cancellationToken);

    Task<IamImpactPreviewDto> PreviewDepartmentStatusAsync(
        UserPrincipal user,
        Guid departmentId,
        bool deactivate,
        CancellationToken cancellationToken);

    Task<DepartmentTreeNodeDto> SetDepartmentStatusAsync(
        UserPrincipal user,
        Guid departmentId,
        bool activate,
        string traceId,
        CancellationToken cancellationToken);

    Task<PagedResult<IamUserListItemDto>> ListUsersAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken);

    Task<IamUserDetailDto?> GetUserAsync(
        UserPrincipal user,
        Guid userId,
        CancellationToken cancellationToken);

    Task<IamUserDetailDto> InviteUserAsync(
        UserPrincipal user,
        InviteUserRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<IamUserListItemDto>> ImportUsersAsync(
        UserPrincipal user,
        BulkImportUsersRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<IamUserDetailDto> SetUserMembershipsAsync(
        UserPrincipal user,
        Guid userId,
        SetUserMembershipsRequest request,
        string traceId,
        CancellationToken cancellationToken);

    Task<IamUserDetailDto> SetUserDataScopesAsync(
        UserPrincipal user,
        Guid userId,
        SetUserDataScopesRequest request,
        string traceId,
        CancellationToken cancellationToken);

    Task<IamUserDetailDto> SetUserStatusAsync(
        UserPrincipal user,
        Guid userId,
        bool activate,
        string traceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PermissionCatalogItemDto>> ListPermissionsAsync(
        UserPrincipal user,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RoleDto>> ListRolesAsync(
        UserPrincipal user,
        string? roleType,
        CancellationToken cancellationToken);

    Task<RoleDto> CreateRoleAsync(
        UserPrincipal user,
        CreateRoleRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<RoleDto> CopyRoleAsync(
        UserPrincipal user,
        Guid roleId,
        CopyRoleRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<RoleDto> UpdateRoleAsync(
        UserPrincipal user,
        Guid roleId,
        UpdateRoleRequest request,
        string traceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RoleMemberDto>> ListRoleMembersAsync(
        UserPrincipal user,
        Guid roleId,
        CancellationToken cancellationToken);

    Task<RoleDto> SetRolePermissionsAsync(
        UserPrincipal user,
        Guid roleId,
        SetRolePermissionsRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RoleMemberDto>> SetRoleMembersAsync(
        UserPrincipal user,
        Guid roleId,
        SetRoleMembersRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<RoleDto> SetRoleStatusAsync(
        UserPrincipal user,
        Guid roleId,
        bool activate,
        string traceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AccessChangeRequestDto>> ListAccessRequestsAsync(
        UserPrincipal user,
        string? status,
        CancellationToken cancellationToken);

    Task<AccessChangeRequestDto> ReviewAccessRequestAsync(
        UserPrincipal user,
        Guid requestId,
        bool approve,
        ReviewAccessChangeRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);
}

public interface INavigationService
{
    Task<CurrentNavigationDto> GetCurrentAsync(
        UserPrincipal user,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<NavigationWorkspaceDto>> ListWorkspacesAsync(
        UserPrincipal user,
        CancellationToken cancellationToken);

    Task<NavigationWorkspaceDto?> GetWorkspaceAsync(
        UserPrincipal user,
        Guid workspaceId,
        CancellationToken cancellationToken);

    Task<NavigationWorkspaceDto> CreateWorkspaceAsync(
        UserPrincipal user,
        CreateNavigationWorkspaceRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<NavigationWorkspaceDto> UpdateDraftAsync(
        UserPrincipal user,
        Guid workspaceId,
        UpdateNavigationWorkspaceRequest request,
        string traceId,
        CancellationToken cancellationToken);

    Task<NavigationWorkspaceDto> PublishAsync(
        UserPrincipal user,
        Guid workspaceId,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<NavigationWorkspaceDto> SetStatusAsync(
        UserPrincipal user,
        Guid workspaceId,
        bool retire,
        string traceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<NavigationPreferenceDto>> GetPreferencesAsync(
        UserPrincipal user,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<NavigationPreferenceDto>> SetPreferencesAsync(
        UserPrincipal user,
        SetNavigationPreferencesRequest request,
        string traceId,
        CancellationToken cancellationToken);
}

public sealed record WorkflowAssignmentRule(
    WorkflowAssignmentStrategy Strategy,
    string? SpecificUserName,
    string? BusinessRoleCode,
    Guid? AgentProfileId);

public sealed record ResolvedWorkflowAssignment(
    Guid PrimaryAssigneeId,
    IReadOnlyList<Guid> CandidateUserIds,
    WorkflowAssignmentStrategy Strategy,
    Guid? BusinessRoleId,
    string? BusinessRoleCode,
    int? BusinessRoleVersion,
    Guid? BackupAssigneeId,
    string SnapshotJson);

public sealed record WorkflowAssignmentValidation(
    bool IsValid,
    string Message,
    IReadOnlyList<Guid> ResolvedCandidateIds,
    string? BusinessRoleCode,
    int? BusinessRoleVersion);

public interface IWorkflowAssignmentResolver
{
    Task<ResolvedWorkflowAssignment> ResolveAsync(
        Guid organizationId,
        Guid workflowDepartmentId,
        Guid? initiatorUserId,
        Guid businessObjectId,
        WorkflowAssignmentRule rule,
        CancellationToken cancellationToken);

    Task<WorkflowAssignmentValidation> ValidateAsync(
        Guid organizationId,
        Guid workflowDepartmentId,
        string objectTypeCode,
        WorkflowAssignmentRule rule,
        CancellationToken cancellationToken);
}

public interface IWorkflowRoleService
{
    Task<IReadOnlyList<RoleDto>> ListAsync(
        UserPrincipal user,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RoleMemberDto>> ListMembersAsync(
        UserPrincipal user,
        Guid roleId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RoleMemberDto>> SetMembersAsync(
        UserPrincipal user,
        Guid roleId,
        SetRoleMembersRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);
}

public interface IWorkflowTaskAssignmentService
{
    Task<WorkflowTaskDto> TransferAsync(
        UserPrincipal user,
        Guid taskId,
        TransferWorkflowTaskRequest request,
        string traceId,
        CancellationToken cancellationToken);

    Task<WorkflowTaskDto> EscalateAsync(
        UserPrincipal user,
        Guid taskId,
        EscalateWorkflowTaskRequest request,
        string traceId,
        CancellationToken cancellationToken);
}
