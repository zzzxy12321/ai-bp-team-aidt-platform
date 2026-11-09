using AIDT.Api.Contracts;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIDT.Api.Controllers;

[ApiController]
[Route("api/v1/mcp")]
[Authorize]
public sealed class McpController(
    ICurrentUserAccessor currentUserAccessor,
    IMcpOAuthService oauthService,
    IMcpManagementService managementService,
    IMcpHumanConfirmationService confirmationService,
    IMcpAuditService auditService,
    IMcpToolCatalog toolCatalog) : ControllerBase
{
    [HttpGet("snapshot")]
    public async Task<ActionResult<ApiResponse<McpManagementSnapshotDto>>> Snapshot(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await managementService.GetSnapshotAsync(
            currentUserAccessor.GetRequiredUser(),
            Math.Max(1, page),
            Math.Clamp(pageSize, 1, 100),
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("tools")]
    public ActionResult<ApiResponse<IReadOnlyList<McpToolDescriptorDto>>> Tools()
    {
        var user = currentUserAccessor.GetRequiredUser();
        user.EnsureCan("mcp", AIDT.Domain.Identity.AccessAction.Read);
        return Ok(ApiResponse.Ok(toolCatalog.List(), HttpContext.TraceIdentifier));
    }

    [HttpGet("confirmations/me")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<McpHumanConfirmationDto>>>> OwnConfirmations(
        CancellationToken cancellationToken)
    {
        var result = await managementService.ListOwnConfirmationsAsync(
            currentUserAccessor.GetRequiredUser(),
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("confirmation-views/me")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<McpHumanConfirmationViewDto>>>> OwnConfirmationViews(
        CancellationToken cancellationToken)
    {
        var result = await managementService.ListOwnConfirmationViewsAsync(
            currentUserAccessor.GetRequiredUser(),
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("confirmations/{id:guid}/actions/confirm")]
    public async Task<ActionResult<ApiResponse<McpHumanConfirmationDto>>> Confirm(
        Guid id,
        [FromBody] ReviewMcpConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await confirmationService.ConfirmAsync(
            id,
            currentUserAccessor.GetRequiredUser(),
            request.Comment,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("confirmations/{id:guid}/actions/reject")]
    public async Task<ActionResult<ApiResponse<McpHumanConfirmationDto>>> Reject(
        Guid id,
        [FromBody] ReviewMcpConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await confirmationService.RejectAsync(
            id,
            currentUserAccessor.GetRequiredUser(),
            request.Comment,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("clients/{clientId}/actions/revoke")]
    public async Task<ActionResult<ApiResponse<object>>> RevokeClient(
        string clientId,
        CancellationToken cancellationToken)
    {
        await managementService.RevokeClientAsync(
            currentUserAccessor.GetRequiredUser(),
            clientId,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok<object>(new { clientId, isActive = false }, HttpContext.TraceIdentifier));
    }

    [HttpPost("tokens/{id:guid}/actions/revoke")]
    public async Task<ActionResult<ApiResponse<object>>> RevokeToken(
        Guid id,
        CancellationToken cancellationToken)
    {
        await managementService.RevokeTokenAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok<object>(new { tokenId = id, isRevoked = true }, HttpContext.TraceIdentifier));
    }

    [HttpPost("service-tokens")]
    public async Task<ActionResult<ApiResponse<McpServiceTokenDto>>> IssueServiceToken(
        [FromBody] McpServiceTokenRequest request,
        CancellationToken cancellationToken)
    {
        var user = currentUserAccessor.GetRequiredUser();
        var result = await oauthService.IssueServiceTokenAsync(
            user,
            request,
            cancellationToken);
        await auditService.RecordSecurityEventAsync(
            user.UserId,
            "service_token.issued",
            "McpAccessToken",
            result.TokenId.ToString("D"),
            HttpContext.TraceIdentifier,
            "Succeeded",
            null,
            System.Text.Json.JsonSerializer.Serialize(new
            {
                result.ClientId,
                result.ServiceIdentityName,
                result.Scope,
                request.AgentProfileId,
                request.DepartmentIds,
                result.ExpiresAt
            }),
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }
}

public sealed record ReviewMcpConfirmationRequest(string? Comment);
