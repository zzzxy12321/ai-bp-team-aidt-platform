using AIDT.Api.Contracts;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIDT.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public sealed class WorkbenchController(
    ICurrentUserAccessor currentUserAccessor,
    IWorkbenchService workbenchService) : ControllerBase
{
    [HttpGet("departments/{departmentId:guid}/workbench")]
    public async Task<ActionResult<ApiResponse<DepartmentWorkbenchDto>>> DepartmentWorkbench(
        Guid departmentId,
        [FromQuery] bool includeTestData = false,
        CancellationToken cancellationToken = default)
    {
        var result = await workbenchService.ResolveAsync(
            currentUserAccessor.GetRequiredUser(),
            departmentId,
            includeTestData,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("workbench-templates")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkbenchTemplateDto>>>> ListTemplates(
        CancellationToken cancellationToken)
    {
        var result = await workbenchService.ListTemplatesAsync(
            currentUserAccessor.GetRequiredUser(),
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("workbench-templates/{id:guid}")]
    public async Task<ActionResult<ApiResponse<WorkbenchTemplateDto>>> GetTemplate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await workbenchService.GetTemplateAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("workbench-templates")]
    public async Task<ActionResult<ApiResponse<WorkbenchTemplateDto>>> CreateTemplate(
        [FromBody] CreateWorkbenchTemplateRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await workbenchService.CreateTemplateAsync(
            currentUserAccessor.GetRequiredUser(),
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("workbench-templates/{id:guid}/actions/copy")]
    public async Task<ActionResult<ApiResponse<WorkbenchTemplateDto>>> CopyTemplate(
        Guid id,
        [FromBody] CopyWorkbenchTemplateRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await workbenchService.CopyTemplateAsync(
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

    [HttpPut("workbench-templates/{id:guid}/draft")]
    public async Task<ActionResult<ApiResponse<WorkbenchTemplateDto>>> UpdateDraft(
        Guid id,
        [FromBody] UpdateWorkbenchTemplateDraftRequest request,
        CancellationToken cancellationToken)
    {
        var result = await workbenchService.UpdateDraftAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("workbench-templates/{id:guid}/actions/validate")]
    public async Task<ActionResult<ApiResponse<WorkbenchTemplateValidationDto>>> ValidateTemplate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await workbenchService.ValidateAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("workbench-templates/{id:guid}/actions/publish")]
    public async Task<ActionResult<ApiResponse<WorkbenchTemplateDto>>> PublishTemplate(
        Guid id,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await workbenchService.PublishAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("workbench-templates/{id:guid}/actions/retire")]
    public async Task<ActionResult<ApiResponse<WorkbenchTemplateDto>>> RetireTemplate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await workbenchService.SetStatusAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            true,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("workbench-templates/{id:guid}/actions/restore")]
    public async Task<ActionResult<ApiResponse<WorkbenchTemplateDto>>> RestoreTemplate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await workbenchService.SetStatusAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            false,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("workbench-templates/{id:guid}/versions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkbenchTemplateVersionDto>>>> ListVersions(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await workbenchService.ListVersionsAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }
}
