using AIDT.Api.Contracts;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIDT.Api.Controllers;

[ApiController]
[Route("api/v1/scenarios")]
[Authorize]
public sealed class ScenariosController(
    ICurrentUserAccessor currentUserAccessor,
    IScenarioService scenarioService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ScenarioDto>>>> ListVisible(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeTestData = false,
        CancellationToken cancellationToken = default)
    {
        var result = await scenarioService.ListVisibleAsync(
            currentUserAccessor.GetRequiredUser(),
            page,
            pageSize,
            includeTestData,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("manage")]
    public async Task<ActionResult<ApiResponse<PagedResult<ScenarioDto>>>> ListManage(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await scenarioService.ListManageAsync(
            currentUserAccessor.GetRequiredUser(),
            page,
            pageSize,
            status,
            search,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ScenarioDto>>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await scenarioService.GetVisibleAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            cancellationToken);
        return result is null
            ? NotFound(ApiResponse.Error(
                "SCENARIO_NOT_FOUND",
                "场景不存在或当前身份无权使用。",
                HttpContext.TraceIdentifier))
            : Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("approval-requests")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ScenarioApprovalRequestDto>>>> ApprovalRequests(
        [FromQuery] string? status = "Pending",
        CancellationToken cancellationToken = default)
    {
        var result = await scenarioService.ListApprovalRequestsAsync(
            currentUserAccessor.GetRequiredUser(),
            status,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("approval-requests/{requestId:guid}/actions/review")]
    public async Task<ActionResult<ApiResponse<ScenarioDto>>> ReviewApproval(
        Guid requestId,
        [FromBody] ReviewScenarioPublishRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await scenarioService.ReviewPublishAsync(
            currentUserAccessor.GetRequiredUser(),
            requestId,
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("drafts")]
    public async Task<ActionResult<ApiResponse<ScenarioDto>>> CreateDraft(
        [FromBody] CreateScenarioDraftRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await scenarioService.CreateDraftAsync(
            currentUserAccessor.GetRequiredUser(),
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/drafts")]
    public async Task<ActionResult<ApiResponse<ScenarioDto>>> CreateNextDraft(
        Guid id,
        [FromBody] CreateNextScenarioVersionDraftRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await scenarioService.CreateNextDraftAsync(
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

    [HttpPut("{id:guid}/drafts/{versionId:guid}")]
    public async Task<ActionResult<ApiResponse<ScenarioDto>>> UpdateDraft(
        Guid id,
        Guid versionId,
        [FromBody] UpdateScenarioDraftRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await scenarioService.UpdateDraftAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            versionId,
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/versions/{versionId:guid}/actions/validate")]
    public async Task<ActionResult<ApiResponse<ScenarioValidationDto>>> Validate(
        Guid id,
        Guid versionId,
        [FromBody] ScenarioActionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await scenarioService.ValidateAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            versionId,
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/versions/{versionId:guid}/actions/publish")]
    public async Task<ActionResult<ApiResponse<ScenarioDto>>> Publish(
        Guid id,
        Guid versionId,
        [FromBody] ScenarioActionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await scenarioService.PublishAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            versionId,
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/actions/disable")]
    public async Task<ActionResult<ApiResponse<ScenarioDto>>> Disable(
        Guid id,
        [FromBody] ScenarioActionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await scenarioService.DisableAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/actions/restore")]
    public async Task<ActionResult<ApiResponse<ScenarioDto>>> Restore(
        Guid id,
        [FromBody] ScenarioActionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await scenarioService.RestoreAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/actions/rollback")]
    public async Task<ActionResult<ApiResponse<ScenarioDto>>> Rollback(
        Guid id,
        [FromBody] RollbackScenarioRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await scenarioService.RollbackAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}/versions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ScenarioVersionDto>>>> Versions(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await scenarioService.ListVersionsAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/actions/preflight")]
    public async Task<ActionResult<ApiResponse<ScenarioPreflightDto>>> Preflight(
        Guid id,
        [FromBody] ScenarioStartInputDto input,
        CancellationToken cancellationToken)
    {
        var result = await scenarioService.PreflightAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            input,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/actions/start")]
    public async Task<ActionResult<ApiResponse<ScenarioLaunchDto>>> Start(
        Guid id,
        [FromBody] ScenarioStartInputDto input,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await scenarioService.StartAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            input,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("launches/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ScenarioLaunchDto>>> GetLaunch(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await scenarioService.GetLaunchAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            cancellationToken);
        return result is null
            ? NotFound(ApiResponse.Error(
                "RESOURCE_NOT_FOUND",
                "场景启动记录不存在。",
                HttpContext.TraceIdentifier))
            : Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("launches")]
    public async Task<ActionResult<ApiResponse<PagedResult<ScenarioLaunchSummaryDto>>>> ListMyLaunches(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await scenarioService.ListMyLaunchesAsync(
            currentUserAccessor.GetRequiredUser(),
            page,
            pageSize,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }
}
