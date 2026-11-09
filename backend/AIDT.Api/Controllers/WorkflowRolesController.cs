using AIDT.Api.Contracts;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIDT.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public sealed class WorkflowRolesController(
    ICurrentUserAccessor currentUserAccessor,
    IWorkflowRoleService workflowRoleService,
    IWorkflowTaskAssignmentService taskAssignmentService) : ControllerBase
{
    [HttpGet("workflow-roles")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleDto>>>> Roles(
        CancellationToken cancellationToken)
    {
        var result = await workflowRoleService.ListAsync(
            currentUserAccessor.GetRequiredUser(),
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("workflow-roles/{id:guid}/members")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleMemberDto>>>> Members(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await workflowRoleService.ListMembersAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("workflow-roles/{id:guid}/members")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleMemberDto>>>> SetMembers(
        Guid id,
        [FromBody] SetRoleMembersRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await workflowRoleService.SetMembersAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("workflow-tasks/{id:guid}/actions/transfer")]
    public async Task<ActionResult<ApiResponse<WorkflowTaskDto>>> Transfer(
        Guid id,
        [FromBody] TransferWorkflowTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await taskAssignmentService.TransferAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("workflow-tasks/{id:guid}/actions/escalate")]
    public async Task<ActionResult<ApiResponse<WorkflowTaskDto>>> Escalate(
        Guid id,
        [FromBody] EscalateWorkflowTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await taskAssignmentService.EscalateAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }
}
