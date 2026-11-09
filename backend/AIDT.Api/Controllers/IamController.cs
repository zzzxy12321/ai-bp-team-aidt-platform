using AIDT.Api.Contracts;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIDT.Api.Controllers;

[ApiController]
[Route("api/v1/iam")]
[Authorize]
public sealed class IamController(
    ICurrentUserAccessor currentUserAccessor,
    IIdentityConfigurationService service) : ControllerBase
{
    [HttpGet("snapshot")]
    public async Task<ActionResult<ApiResponse<IdentityAdministrationSnapshotDto>>> Snapshot(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await service.GetSnapshotAsync(
            currentUserAccessor.GetRequiredUser(),
            Math.Max(1, page),
            Math.Clamp(pageSize, 1, 100),
            search,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("organization-tree")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DepartmentTreeNodeDto>>>> OrganizationTree(
        CancellationToken cancellationToken)
    {
        var result = await service.GetOrganizationTreeAsync(
            currentUserAccessor.GetRequiredUser(),
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("departments")]
    public async Task<ActionResult<ApiResponse<DepartmentTreeNodeDto>>> CreateDepartment(
        [FromBody] CreateIamDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateDepartmentAsync(
            currentUserAccessor.GetRequiredUser(),
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("departments/{id:guid}")]
    public async Task<ActionResult<ApiResponse<DepartmentTreeNodeDto>>> UpdateDepartment(
        Guid id,
        [FromBody] UpdateIamDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateDepartmentAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("departments/{id:guid}/actions/status-preview")]
    public async Task<ActionResult<ApiResponse<IamImpactPreviewDto>>> PreviewDepartmentStatus(
        Guid id,
        [FromQuery] bool deactivate,
        CancellationToken cancellationToken)
    {
        var result = await service.PreviewDepartmentStatusAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            deactivate,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("departments/{id:guid}/actions/deactivate")]
    public async Task<ActionResult<ApiResponse<DepartmentTreeNodeDto>>> DeactivateDepartment(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await service.SetDepartmentStatusAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            false,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("departments/{id:guid}/actions/reactivate")]
    public async Task<ActionResult<ApiResponse<DepartmentTreeNodeDto>>> ReactivateDepartment(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await service.SetDepartmentStatusAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            true,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<PagedResult<IamUserListItemDto>>>> ListUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await service.ListUsersAsync(
            currentUserAccessor.GetRequiredUser(),
            Math.Max(1, page),
            Math.Clamp(pageSize, 1, 100),
            search,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("users/{id:guid}")]
    public async Task<ActionResult<ApiResponse<IamUserDetailDto>>> GetUser(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await service.GetUserAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            cancellationToken);
        return result is null
            ? NotFound(ApiResponse.Error("RESOURCE_NOT_FOUND", "人员不存在。", HttpContext.TraceIdentifier))
            : Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("users")]
    public async Task<ActionResult<ApiResponse<IamUserDetailDto>>> InviteUser(
        [FromBody] InviteUserRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await service.InviteUserAsync(
            currentUserAccessor.GetRequiredUser(),
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("users/import")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<IamUserListItemDto>>>> ImportUsers(
        [FromBody] BulkImportUsersRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await service.ImportUsersAsync(
            currentUserAccessor.GetRequiredUser(),
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("users/{id:guid}/memberships")]
    public async Task<ActionResult<ApiResponse<IamUserDetailDto>>> SetMemberships(
        Guid id,
        [FromBody] SetUserMembershipsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.SetUserMembershipsAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("users/{id:guid}/data-scopes")]
    public async Task<ActionResult<ApiResponse<IamUserDetailDto>>> SetDataScopes(
        Guid id,
        [FromBody] SetUserDataScopesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.SetUserDataScopesAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("users/{id:guid}/actions/deactivate")]
    public async Task<ActionResult<ApiResponse<IamUserDetailDto>>> DeactivateUser(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await service.SetUserStatusAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            false,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("users/{id:guid}/actions/reactivate")]
    public async Task<ActionResult<ApiResponse<IamUserDetailDto>>> ReactivateUser(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await service.SetUserStatusAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            true,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("permissions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PermissionCatalogItemDto>>>> Permissions(
        CancellationToken cancellationToken)
    {
        var result = await service.ListPermissionsAsync(
            currentUserAccessor.GetRequiredUser(),
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("roles")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleDto>>>> Roles(
        [FromQuery] string? roleType = null,
        CancellationToken cancellationToken = default)
    {
        var result = await service.ListRolesAsync(
            currentUserAccessor.GetRequiredUser(),
            roleType,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("roles")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> CreateRole(
        [FromBody] CreateRoleRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateRoleAsync(
            currentUserAccessor.GetRequiredUser(),
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("roles/{id:guid}/actions/copy")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> CopyRole(
        Guid id,
        [FromBody] CopyRoleRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await service.CopyRoleAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("roles/{id:guid}")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> UpdateRole(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateRoleAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("roles/{id:guid}/members")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleMemberDto>>>> RoleMembers(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await service.ListRoleMembersAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("roles/{id:guid}/permissions")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> SetRolePermissions(
        Guid id,
        [FromBody] SetRolePermissionsRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await service.SetRolePermissionsAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("roles/{id:guid}/members")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleMemberDto>>>> SetRoleMembers(
        Guid id,
        [FromBody] SetRoleMembersRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await service.SetRoleMembersAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("roles/{id:guid}/actions/deactivate")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> DeactivateRole(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await service.SetRoleStatusAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            false,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("roles/{id:guid}/actions/reactivate")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> ReactivateRole(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await service.SetRoleStatusAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            true,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("access-requests")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AccessChangeRequestDto>>>> AccessRequests(
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await service.ListAccessRequestsAsync(
            currentUserAccessor.GetRequiredUser(),
            status,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("access-requests/{id:guid}/actions/approve")]
    public async Task<ActionResult<ApiResponse<AccessChangeRequestDto>>> ApproveAccessRequest(
        Guid id,
        [FromBody] ReviewAccessChangeRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await service.ReviewAccessRequestAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            true,
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("access-requests/{id:guid}/actions/reject")]
    public async Task<ActionResult<ApiResponse<AccessChangeRequestDto>>> RejectAccessRequest(
        Guid id,
        [FromBody] ReviewAccessChangeRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await service.ReviewAccessRequestAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            false,
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }
}
