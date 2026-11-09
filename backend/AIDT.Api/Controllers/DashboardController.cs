using AIDT.Api.Contracts;
using AIDT.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIDT.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public sealed class DashboardController(
    ICurrentUserAccessor currentUserAccessor,
    IWorkspaceQueryService queryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get(
        [FromQuery] bool includeTestData = false,
        CancellationToken cancellationToken = default)
    {
        var result = await queryService.GetDashboardAsync(
            currentUserAccessor.GetRequiredUser(),
            includeTestData,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }
}
