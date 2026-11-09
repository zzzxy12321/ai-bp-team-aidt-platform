using AIDT.Api.Contracts;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIDT.Api.Controllers;

[ApiController]
[Route("api/v1/agents")]
[Authorize]
public sealed class AgentsController(
    ICurrentUserAccessor currentUserAccessor,
    IWorkspaceQueryService queryService,
    IAgentGovernanceService governanceService,
    IAgentGatewayService agentGatewayService,
    StartAgentRunHandler startAgentRunHandler,
    ConfirmAgentRunHandler confirmAgentRunHandler,
    StopAgentRunHandler stopAgentRunHandler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AgentProfileDto>>>> GetAgents(
        CancellationToken cancellationToken)
    {
        var result = await queryService.ListAgentProfilesAsync(
            currentUserAccessor.GetRequiredUser(),
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("available")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AgentProfileDto>>>> GetAvailableAgents(
        [FromQuery] string? departmentCode = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await governanceService.ListAvailableAsync(
            currentUserAccessor.GetRequiredUser(),
            departmentCode,
            search,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("options")]
    public async Task<ActionResult<ApiResponse<AgentIdentityOptionsDto>>> GetOptions(
        [FromQuery] Guid? profileId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await governanceService.GetOptionsAsync(
            currentUserAccessor.GetRequiredUser(),
            profileId,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("manage")]
    public async Task<ActionResult<ApiResponse<PagedResult<AgentProfileDetailDto>>>> GetManagedAgents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? lifecycleStatus = null,
        [FromQuery] string? departmentCode = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await governanceService.ListProfilesAsync(
            currentUserAccessor.GetRequiredUser(),
            Math.Max(1, page),
            Math.Clamp(pageSize, 1, 100),
            lifecycleStatus,
            departmentCode,
            search,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AgentProfileDetailDto>>> GetAgent(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await governanceService.GetProfileAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            cancellationToken)
            ?? throw new AIDT.Domain.Common.NotFoundException("数字员工不存在。");
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}/context")]
    public async Task<ActionResult<ApiResponse<AgentProfileContextDto>>> GetAgentContext(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await governanceService.GetContextAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("drafts")]
    public async Task<ActionResult<ApiResponse<AgentProfileDetailDto>>> CreateDraft(
        [FromBody] CreateAgentProfileDraftRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await governanceService.CreateDraftAsync(
            currentUserAccessor.GetRequiredUser(),
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/drafts")]
    public async Task<ActionResult<ApiResponse<AgentProfileVersionDto>>> CreateNextDraft(
        Guid id,
        [FromBody] CreateNextAgentProfileDraftRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await governanceService.CreateNextDraftAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}/drafts/{versionId:guid}")]
    public async Task<ActionResult<ApiResponse<AgentProfileVersionDto>>> UpdateDraft(
        Guid id,
        Guid versionId,
        [FromBody] UpdateAgentProfileDraftRequest request,
        CancellationToken cancellationToken)
    {
        var result = await governanceService.UpdateDraftAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            versionId,
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/versions/{versionId:guid}/actions/validate")]
    public async Task<ActionResult<ApiResponse<AgentProfileValidationDto>>> ValidateVersion(
        Guid id,
        Guid versionId,
        [FromBody] ValidateAgentProfileVersionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await governanceService.ValidateAsync(
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
    public async Task<ActionResult<ApiResponse<AgentProfilePublishResultDto>>> PublishVersion(
        Guid id,
        Guid versionId,
        [FromBody] PublishAgentProfileVersionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await governanceService.PublishAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            versionId,
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            result.Status == "AwaitingApproval"
                ? StatusCodes.Status202Accepted
                : StatusCodes.Status200OK,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/actions/disable")]
    public async Task<ActionResult<ApiResponse<AgentProfileDetailDto>>> Disable(
        Guid id,
        [FromBody] DisableAgentProfileRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await governanceService.DisableAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/actions/resume")]
    public async Task<ActionResult<ApiResponse<AgentProfileDetailDto>>> Resume(
        Guid id,
        [FromBody] ResumeAgentProfileRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await governanceService.ResumeAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}/versions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AgentProfileVersionDto>>>> Versions(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await governanceService.ListVersionsAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/actions/rollback")]
    public async Task<ActionResult<ApiResponse<AgentProfilePublishResultDto>>> Rollback(
        Guid id,
        [FromBody] RollbackAgentProfileRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await governanceService.RollbackAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            result.Status == "AwaitingApproval"
                ? StatusCodes.Status202Accepted
                : StatusCodes.Status200OK,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("change-requests")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AgentProfileChangeRequestDto>>>> ChangeRequests(
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await governanceService.ListChangeRequestsAsync(
            currentUserAccessor.GetRequiredUser(),
            status,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("change-requests/{requestId:guid}/actions/review")]
    public async Task<ActionResult<ApiResponse<AgentProfilePublishResultDto>>> ReviewChangeRequest(
        Guid requestId,
        [FromBody] ReviewAgentProfileChangeRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await governanceService.ReviewChangeRequestAsync(
            currentUserAccessor.GetRequiredUser(),
            requestId,
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("runs")]
    public async Task<ActionResult<ApiResponse<PagedResult<AgentRunDto>>>> GetRuns(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await queryService.ListAgentRunsAsync(
            currentUserAccessor.GetRequiredUser(),
            Math.Max(1, page),
            Math.Clamp(pageSize, 1, 100),
            status,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("runs")]
    public async Task<ActionResult<ApiResponse<AgentRunDto>>> StartRun(
        [FromBody] CreateAgentRunRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await startAgentRunHandler.HandleAsync(
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("runs/{id:guid}/actions/confirm")]
    public async Task<ActionResult<ApiResponse<AgentRunDto>>> Confirm(
        Guid id,
        [FromBody] ConfirmAgentRunRequest request,
        CancellationToken cancellationToken)
    {
        var result = await confirmAgentRunHandler.HandleAsync(
            id,
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("runs/{id:guid}/actions/stop")]
    public async Task<ActionResult<ApiResponse<AgentRunDto>>> Stop(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await stopAgentRunHandler.HandleAsync(
            id,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("runs/{id:guid}/actions/execute")]
    public async Task<ActionResult<ApiResponse<AgentExecutionResultDto>>> Execute(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await agentGatewayService.ExecuteConfirmedRunAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("runs/{id:guid}/gateway-calls")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AgentGatewayCallDto>>>> GatewayCalls(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await agentGatewayService.ListCallsAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("runs/{id:guid}/actions/confirm-proxy")]
    public async Task<ActionResult<ApiResponse<AgentRunDto>>> ConfirmProxy(
        Guid id,
        [FromBody] ConfirmAgentProxyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await agentGatewayService.ConfirmProxyAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }
}
