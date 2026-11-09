using AIDT.Api.Contracts;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIDT.Api.Controllers;

[ApiController]
[Route("api/v1/connectors")]
[Authorize]
public sealed class ConnectorsController(
    ICurrentUserAccessor currentUserAccessor,
    IWorkspaceQueryService queryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ConnectorDto>>>> GetConnectors(
        CancellationToken cancellationToken)
    {
        var connectors = await queryService.ListConnectorsAsync(
            currentUserAccessor.GetRequiredUser(),
            cancellationToken);
        return Ok(ApiResponse.Ok<IReadOnlyList<ConnectorDto>>(
            connectors,
            HttpContext.TraceIdentifier,
            "Mock 与未连接状态来自数据库，不代表真实外部集成。"));
    }

    [HttpGet("{id:guid}/outbox")]
    public async Task<ActionResult<ApiResponse<PagedResult<OutboxMessageDto>>>> GetOutbox(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var messages = await queryService.ListOutboxMessagesAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            Math.Max(1, page),
            Math.Clamp(pageSize, 1, 100),
            cancellationToken);
        return Ok(ApiResponse.Ok(
            messages,
            HttpContext.TraceIdentifier,
            "Demo Outbox 快照；事件未发送到真实外部系统。"));
    }
}
