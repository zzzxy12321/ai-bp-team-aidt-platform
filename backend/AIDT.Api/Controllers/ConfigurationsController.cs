using AIDT.Api.Contracts;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIDT.Api.Controllers;

[ApiController]
[Route("api/v1/configurations")]
[Authorize]
public sealed class ConfigurationsController(
    ICurrentUserAccessor currentUserAccessor,
    IWorkflowConfigurationService configurationService) : ControllerBase
{
    [HttpGet("member-candidates")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkflowPolicyUserDto>>>> MemberCandidates(
        CancellationToken cancellationToken)
    {
        var result = await configurationService.ListUsersAsync(
            currentUserAccessor.GetRequiredUser(),
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<WorkflowConfigurationDto>>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? state = null,
        [FromQuery] string? configurationKey = null,
        CancellationToken cancellationToken = default)
    {
        var result = await configurationService.ListAsync(
            currentUserAccessor.GetRequiredUser(),
            page,
            pageSize,
            state,
            configurationKey,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<WorkflowConfigurationDto>>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await configurationService.GetAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            cancellationToken);
        return result is null
            ? NotFound(ApiResponse.Error(
                "RESOURCE_NOT_FOUND",
                "配置版本不存在。",
                HttpContext.TraceIdentifier))
            : Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<WorkflowConfigurationDto>>> Create(
        [FromBody] CreateWorkflowConfigurationRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await configurationService.CreateAsync(
            currentUserAccessor.GetRequiredUser(),
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<WorkflowConfigurationDto>>> Update(
        Guid id,
        [FromBody] UpdateWorkflowConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await configurationService.UpdateAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/actions/validate")]
    public async Task<ActionResult<ApiResponse<WorkflowConfigurationValidationDto>>> Validate(
        Guid id,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await configurationService.ValidateAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/actions/publish")]
    public async Task<ActionResult<ApiResponse<WorkflowConfigurationDto>>> Publish(
        Guid id,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await configurationService.PublishAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/actions/rollback")]
    public async Task<ActionResult<ApiResponse<WorkflowConfigurationDto>>> Rollback(
        Guid id,
        [FromBody] RollbackWorkflowConfigurationRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await configurationService.RollbackAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request.Comment,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}/versions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkflowConfigurationDto>>>> Versions(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await configurationService.ListVersionsAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/actions/disable")]
    public async Task<ActionResult<ApiResponse<WorkflowConfigurationDto>>> Disable(
        Guid id,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await configurationService.SetStatusAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            true,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/actions/restore")]
    public async Task<ActionResult<ApiResponse<WorkflowConfigurationDto>>> Restore(
        Guid id,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await configurationService.SetStatusAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            false,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/actions/simulate")]
    public async Task<ActionResult<ApiResponse<WorkflowConfigurationSimulationDto>>> Simulate(
        Guid id,
        [FromBody] SimulateWorkflowConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await configurationService.SimulateAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }
}
