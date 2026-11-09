using AIDT.Api.Contracts;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIDT.Api.Controllers;

[ApiController]
[Route("api/v1/departments")]
[Authorize]
public sealed class DepartmentsController(
    ICurrentUserAccessor currentUserAccessor,
    IEmployeeWorkspaceQueryService workspaceQueryService,
    IDepartmentCommandService commandService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DepartmentDto>>>> Get(
        CancellationToken cancellationToken)
    {
        var departments = await workspaceQueryService.ListDepartmentsAsync(
            currentUserAccessor.GetRequiredUser(),
            cancellationToken);
        return Ok(ApiResponse.Ok(departments, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> Create(
        [FromBody] CreateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        var department = await commandService.CreateAsync(
            currentUserAccessor.GetRequiredUser(),
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(department, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/members")]
    public async Task<ActionResult<ApiResponse<object>>> AddMember(
        Guid id,
        [FromBody] AddDepartmentMemberRequest request,
        CancellationToken cancellationToken)
    {
        await commandService.AddMemberAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(new { added = true }, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}/workspace")]
    public async Task<ActionResult<ApiResponse<DepartmentWorkspaceDto>>> GetWorkspace(
        Guid id,
        [FromQuery] bool includeTestData = false,
        CancellationToken cancellationToken = default)
    {
        var result = await workspaceQueryService.GetDepartmentWorkspaceAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            includeTestData,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }
}
