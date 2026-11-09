using AIDT.Api.Contracts;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIDT.Api.Controllers;

[ApiController]
[Route("api/v1/audit-events")]
[Authorize]
public sealed class AuditController(
    ICurrentUserAccessor currentUserAccessor,
    IWorkspaceQueryService queryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditEventDto>>>> GetAuditEvents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        _ = from;
        _ = to;
        var result = await queryService.ListAuditEventsAsync(
            currentUserAccessor.GetRequiredUser(),
            Math.Max(1, page),
            Math.Clamp(pageSize, 1, 100),
            entityType,
            entityId,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    public ActionResult GetAuditEvent(Guid id) =>
        StatusCode(
            StatusCodes.Status501NotImplemented,
            ApiResponse.Error(
                "FEATURE_NOT_IMPLEMENTED",
                "审计详情使用列表明细展示；独立详情端点将在 P1 补齐。",
                HttpContext.TraceIdentifier));
}
