using AIDT.Api.Contracts;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIDT.Api.Controllers;

[ApiController]
[Route("api/v1/navigation")]
[Authorize]
public sealed class NavigationController(
    ICurrentUserAccessor currentUserAccessor,
    INavigationService navigationService) : ControllerBase
{
    [HttpGet("current")]
    public async Task<ActionResult<ApiResponse<CurrentNavigationDto>>> Current(
        CancellationToken cancellationToken)
    {
        var result = await navigationService.GetCurrentAsync(
            currentUserAccessor.GetRequiredUser(),
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("workspaces")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NavigationWorkspaceDto>>>> Workspaces(
        CancellationToken cancellationToken)
    {
        var result = await navigationService.ListWorkspacesAsync(
            currentUserAccessor.GetRequiredUser(),
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("workspaces/{id:guid}")]
    public async Task<ActionResult<ApiResponse<NavigationWorkspaceDto>>> Workspace(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await navigationService.GetWorkspaceAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            cancellationToken);
        return result is null
            ? NotFound(ApiResponse.Error("RESOURCE_NOT_FOUND", "导航工作区不存在。", HttpContext.TraceIdentifier))
            : Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("workspaces")]
    public async Task<ActionResult<ApiResponse<NavigationWorkspaceDto>>> CreateWorkspace(
        [FromBody] CreateNavigationWorkspaceRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await navigationService.CreateWorkspaceAsync(
            currentUserAccessor.GetRequiredUser(),
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("workspaces/{id:guid}/draft")]
    public async Task<ActionResult<ApiResponse<NavigationWorkspaceDto>>> UpdateDraft(
        Guid id,
        [FromBody] UpdateNavigationWorkspaceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await navigationService.UpdateDraftAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("workspaces/{id:guid}/actions/publish")]
    public async Task<ActionResult<ApiResponse<NavigationWorkspaceDto>>> Publish(
        Guid id,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await navigationService.PublishAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("workspaces/{id:guid}/actions/retire")]
    public async Task<ActionResult<ApiResponse<NavigationWorkspaceDto>>> Retire(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await navigationService.SetStatusAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            true,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("workspaces/{id:guid}/actions/restore")]
    public async Task<ActionResult<ApiResponse<NavigationWorkspaceDto>>> Restore(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await navigationService.SetStatusAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            false,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NavigationPreferenceDto>>>> Preferences(
        CancellationToken cancellationToken)
    {
        var result = await navigationService.GetPreferencesAsync(
            currentUserAccessor.GetRequiredUser(),
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("preferences")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NavigationPreferenceDto>>>> SetPreferences(
        [FromBody] SetNavigationPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await navigationService.SetPreferencesAsync(
            currentUserAccessor.GetRequiredUser(),
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }
}
