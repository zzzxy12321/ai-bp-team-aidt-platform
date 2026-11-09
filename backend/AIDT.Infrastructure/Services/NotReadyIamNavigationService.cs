using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;

namespace AIDT.Infrastructure.Services;

public sealed class NotReadyIamNavigationService :
    IIdentityConfigurationService,
    INavigationService,
    IWorkflowAssignmentResolver,
    IWorkflowRoleService,
    IWorkflowTaskAssignmentService
{
    public Task<IdentityAdministrationSnapshotDto> GetSnapshotAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken) =>
        Fail<IdentityAdministrationSnapshotDto>();

    public Task<IReadOnlyList<DepartmentTreeNodeDto>> GetOrganizationTreeAsync(
        UserPrincipal user,
        CancellationToken cancellationToken) =>
        Fail<IReadOnlyList<DepartmentTreeNodeDto>>();

    public Task<DepartmentTreeNodeDto> CreateDepartmentAsync(
        UserPrincipal user,
        CreateIamDepartmentRequest request,
        string traceId,
        CancellationToken cancellationToken) =>
        Fail<DepartmentTreeNodeDto>();

    public Task<DepartmentTreeNodeDto> UpdateDepartmentAsync(
        UserPrincipal user,
        Guid departmentId,
        UpdateIamDepartmentRequest request,
        string traceId,
        CancellationToken cancellationToken) =>
        Fail<DepartmentTreeNodeDto>();

    public Task<IamImpactPreviewDto> PreviewDepartmentStatusAsync(
        UserPrincipal user,
        Guid departmentId,
        bool deactivate,
        CancellationToken cancellationToken) =>
        Fail<IamImpactPreviewDto>();

    public Task<DepartmentTreeNodeDto> SetDepartmentStatusAsync(
        UserPrincipal user,
        Guid departmentId,
        bool activate,
        string traceId,
        CancellationToken cancellationToken) =>
        Fail<DepartmentTreeNodeDto>();

    public Task<PagedResult<IamUserListItemDto>> ListUsersAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken) =>
        Fail<PagedResult<IamUserListItemDto>>();

    public Task<IamUserDetailDto?> GetUserAsync(
        UserPrincipal user,
        Guid userId,
        CancellationToken cancellationToken) =>
        Fail<IamUserDetailDto?>();

    public Task<IamUserDetailDto> InviteUserAsync(
        UserPrincipal user,
        InviteUserRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) =>
        Fail<IamUserDetailDto>();

    public Task<IReadOnlyList<IamUserListItemDto>> ImportUsersAsync(
        UserPrincipal user,
        BulkImportUsersRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) =>
        Fail<IReadOnlyList<IamUserListItemDto>>();

    public Task<IamUserDetailDto> SetUserMembershipsAsync(
        UserPrincipal user,
        Guid userId,
        SetUserMembershipsRequest request,
        string traceId,
        CancellationToken cancellationToken) =>
        Fail<IamUserDetailDto>();

    public Task<IamUserDetailDto> SetUserDataScopesAsync(
        UserPrincipal user,
        Guid userId,
        SetUserDataScopesRequest request,
        string traceId,
        CancellationToken cancellationToken) =>
        Fail<IamUserDetailDto>();

    public Task<IamUserDetailDto> SetUserStatusAsync(
        UserPrincipal user,
        Guid userId,
        bool activate,
        string traceId,
        CancellationToken cancellationToken) =>
        Fail<IamUserDetailDto>();

    public Task<IReadOnlyList<PermissionCatalogItemDto>> ListPermissionsAsync(
        UserPrincipal user,
        CancellationToken cancellationToken) =>
        Fail<IReadOnlyList<PermissionCatalogItemDto>>();

    public Task<IReadOnlyList<RoleDto>> ListRolesAsync(
        UserPrincipal user,
        string? roleType,
        CancellationToken cancellationToken) =>
        Fail<IReadOnlyList<RoleDto>>();

    public Task<RoleDto> CreateRoleAsync(
        UserPrincipal user,
        CreateRoleRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) =>
        Fail<RoleDto>();

    public Task<RoleDto> CopyRoleAsync(
        UserPrincipal user,
        Guid roleId,
        CopyRoleRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) =>
        Fail<RoleDto>();

    public Task<RoleDto> UpdateRoleAsync(
        UserPrincipal user,
        Guid roleId,
        UpdateRoleRequest request,
        string traceId,
        CancellationToken cancellationToken) =>
        Fail<RoleDto>();

    public Task<IReadOnlyList<RoleMemberDto>> ListRoleMembersAsync(
        UserPrincipal user,
        Guid roleId,
        CancellationToken cancellationToken) =>
        Fail<IReadOnlyList<RoleMemberDto>>();

    public Task<RoleDto> SetRolePermissionsAsync(
        UserPrincipal user,
        Guid roleId,
        SetRolePermissionsRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) =>
        Fail<RoleDto>();

    public Task<IReadOnlyList<RoleMemberDto>> SetRoleMembersAsync(
        UserPrincipal user,
        Guid roleId,
        SetRoleMembersRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) =>
        Fail<IReadOnlyList<RoleMemberDto>>();

    public Task<RoleDto> SetRoleStatusAsync(
        UserPrincipal user,
        Guid roleId,
        bool activate,
        string traceId,
        CancellationToken cancellationToken) =>
        Fail<RoleDto>();

    public Task<IReadOnlyList<AccessChangeRequestDto>> ListAccessRequestsAsync(
        UserPrincipal user,
        string? status,
        CancellationToken cancellationToken) =>
        Fail<IReadOnlyList<AccessChangeRequestDto>>();

    public Task<AccessChangeRequestDto> ReviewAccessRequestAsync(
        UserPrincipal user,
        Guid requestId,
        bool approve,
        ReviewAccessChangeRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) =>
        Fail<AccessChangeRequestDto>();

    public Task<CurrentNavigationDto> GetCurrentAsync(
        UserPrincipal user,
        CancellationToken cancellationToken) =>
        Fail<CurrentNavigationDto>();

    public Task<IReadOnlyList<NavigationWorkspaceDto>> ListWorkspacesAsync(
        UserPrincipal user,
        CancellationToken cancellationToken) =>
        Fail<IReadOnlyList<NavigationWorkspaceDto>>();

    public Task<NavigationWorkspaceDto?> GetWorkspaceAsync(
        UserPrincipal user,
        Guid workspaceId,
        CancellationToken cancellationToken) =>
        Fail<NavigationWorkspaceDto?>();

    public Task<NavigationWorkspaceDto> CreateWorkspaceAsync(
        UserPrincipal user,
        CreateNavigationWorkspaceRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) =>
        Fail<NavigationWorkspaceDto>();

    public Task<NavigationWorkspaceDto> UpdateDraftAsync(
        UserPrincipal user,
        Guid workspaceId,
        UpdateNavigationWorkspaceRequest request,
        string traceId,
        CancellationToken cancellationToken) =>
        Fail<NavigationWorkspaceDto>();

    public Task<NavigationWorkspaceDto> PublishAsync(
        UserPrincipal user,
        Guid workspaceId,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) =>
        Fail<NavigationWorkspaceDto>();

    public Task<NavigationWorkspaceDto> SetStatusAsync(
        UserPrincipal user,
        Guid workspaceId,
        bool retire,
        string traceId,
        CancellationToken cancellationToken) =>
        Fail<NavigationWorkspaceDto>();

    public Task<IReadOnlyList<NavigationPreferenceDto>> GetPreferencesAsync(
        UserPrincipal user,
        CancellationToken cancellationToken) =>
        Fail<IReadOnlyList<NavigationPreferenceDto>>();

    public Task<IReadOnlyList<NavigationPreferenceDto>> SetPreferencesAsync(
        UserPrincipal user,
        SetNavigationPreferencesRequest request,
        string traceId,
        CancellationToken cancellationToken) =>
        Fail<IReadOnlyList<NavigationPreferenceDto>>();

    public Task<ResolvedWorkflowAssignment> ResolveAsync(
        Guid organizationId,
        Guid workflowDepartmentId,
        Guid? initiatorUserId,
        Guid businessObjectId,
        WorkflowAssignmentRule rule,
        CancellationToken cancellationToken) =>
        Fail<ResolvedWorkflowAssignment>();

    public Task<WorkflowAssignmentValidation> ValidateAsync(
        Guid organizationId,
        Guid workflowDepartmentId,
        string objectTypeCode,
        WorkflowAssignmentRule rule,
        CancellationToken cancellationToken) =>
        Fail<WorkflowAssignmentValidation>();

    public Task<IReadOnlyList<RoleDto>> ListAsync(
        UserPrincipal user,
        CancellationToken cancellationToken) =>
        Fail<IReadOnlyList<RoleDto>>();

    public Task<IReadOnlyList<RoleMemberDto>> ListMembersAsync(
        UserPrincipal user,
        Guid roleId,
        CancellationToken cancellationToken) =>
        Fail<IReadOnlyList<RoleMemberDto>>();

    public Task<IReadOnlyList<RoleMemberDto>> SetMembersAsync(
        UserPrincipal user,
        Guid roleId,
        SetRoleMembersRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) =>
        Fail<IReadOnlyList<RoleMemberDto>>();

    public Task<WorkflowTaskDto> TransferAsync(
        UserPrincipal user,
        Guid taskId,
        TransferWorkflowTaskRequest request,
        string traceId,
        CancellationToken cancellationToken) =>
        Fail<WorkflowTaskDto>();

    public Task<WorkflowTaskDto> EscalateAsync(
        UserPrincipal user,
        Guid taskId,
        EscalateWorkflowTaskRequest request,
        string traceId,
        CancellationToken cancellationToken) =>
        Fail<WorkflowTaskDto>();

    private static Task<T> Fail<T>() =>
        Task.FromException<T>(
            new ServiceUnavailableException(
                ErrorCodes.PersistenceNotReady,
                "IAM、导航和流程角色配置需要 SQL Server 持久化。"));
}
