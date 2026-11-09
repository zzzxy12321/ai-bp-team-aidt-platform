using AIDT.Api.Contracts;
using AIDT.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIDT.Api.Controllers;

[ApiController]
[Route("api/v1/platform")]
public sealed class PlatformController(IPlatformCapabilityService capabilityService) : ControllerBase
{
    [HttpGet("capabilities")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<object>> GetCapabilities() =>
        Ok(ApiResponse.Ok(capabilityService.GetCapabilities(), HttpContext.TraceIdentifier));
}
