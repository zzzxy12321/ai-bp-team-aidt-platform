using AIDT.Api.Contracts;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIDT.Api.Controllers;

[ApiController]
[Route("api/v1/my-work")]
[Authorize]
public sealed class MyWorkController(
    ICurrentUserAccessor currentUserAccessor,
    IEmployeeWorkspaceQueryService workspaceQueryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<MyWorkDto>>> Get(
        [FromQuery] bool includeTestData = false,
        CancellationToken cancellationToken = default)
    {
        var result = await workspaceQueryService.GetMyWorkAsync(
            currentUserAccessor.GetRequiredUser(),
            includeTestData,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }
}
