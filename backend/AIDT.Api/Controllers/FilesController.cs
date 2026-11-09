using AIDT.Api.Contracts;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIDT.Api.Controllers;

[ApiController]
[Route("api/v1/files")]
[Authorize]
public sealed class FilesController(
    ICurrentUserAccessor currentUserAccessor,
    IFileCenterService fileCenterService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<FileAttachmentDto>>>> List(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? businessObjectId = null,
        [FromQuery] Guid? workflowInstanceId = null,
        [FromQuery] Guid? workflowTaskId = null,
        [FromQuery] string? environmentCode = null,
        [FromQuery] bool includeTestData = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await fileCenterService.ListAsync(
            currentUserAccessor.GetRequiredUser(),
            new AttachmentListQuery(
                departmentId,
                businessObjectId,
                workflowInstanceId,
                workflowTaskId,
                environmentCode,
                includeTestData,
                page,
                pageSize),
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    [RequestSizeLimit(500 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<FileAttachmentDto>>> Upload(
        [FromForm] UploadFileForm form,
        CancellationToken cancellationToken)
    {
        if (form.File is null || form.File.Length == 0)
        {
            return BadRequest(ApiResponse.Error(
                "VALIDATION_FAILED",
                "上传文件不能为空。",
                HttpContext.TraceIdentifier));
        }

        await using var stream = form.File.OpenReadStream();
        var result = await fileCenterService.UploadAsync(
            currentUserAccessor.GetRequiredUser(),
            new UploadFileCommand(
                stream,
                Path.GetFileName(form.File.FileName),
                form.File.ContentType,
                form.DepartmentId,
                form.BusinessObjectId,
                form.WorkflowInstanceId,
                form.WorkflowTaskId,
                form.Sensitivity),
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<FileAttachmentDto>>> Get(
        Guid id,
        [FromQuery] bool includeTestData,
        CancellationToken cancellationToken)
    {
        var result = await fileCenterService.GetAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            includeTestData,
            cancellationToken);
        return result is null
            ? NotFound(ApiResponse.Error("RESOURCE_NOT_FOUND", "文件不存在。", HttpContext.TraceIdentifier))
            : Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}/versions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FileVersionDto>>>> Versions(
        Guid id,
        [FromQuery] bool includeTestData,
        CancellationToken cancellationToken)
    {
        var result = await fileCenterService.ListVersionsAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            includeTestData,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/versions")]
    [RequestSizeLimit(500 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<FileAttachmentDto>>> AddVersion(
        Guid id,
        [FromForm] UploadFileForm form,
        CancellationToken cancellationToken)
    {
        if (form.File is null || form.File.Length == 0)
        {
            return BadRequest(ApiResponse.Error(
                "VALIDATION_FAILED",
                "上传文件不能为空。",
                HttpContext.TraceIdentifier));
        }

        await using var stream = form.File.OpenReadStream();
        var result = await fileCenterService.AddVersionAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            new UploadFileCommand(
                stream,
                Path.GetFileName(form.File.FileName),
                form.File.ContentType,
                form.DepartmentId,
                form.BusinessObjectId,
                form.WorkflowInstanceId,
                form.WorkflowTaskId,
                form.Sensitivity),
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(
        Guid id,
        [FromQuery] bool includeTestData,
        CancellationToken cancellationToken)
    {
        var content = await fileCenterService.OpenContentAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            preview: false,
            includeTestData,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return File(content.Content, content.MimeType, content.DownloadName, enableRangeProcessing: true);
    }

    [HttpGet("{id:guid}/preview")]
    public async Task<IActionResult> Preview(
        Guid id,
        [FromQuery] bool includeTestData,
        CancellationToken cancellationToken)
    {
        var content = await fileCenterService.OpenContentAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            preview: true,
            includeTestData,
            HttpContext.TraceIdentifier,
            cancellationToken);
        Response.Headers.ContentDisposition =
            $"inline; filename*=UTF-8''{Uri.EscapeDataString(content.DownloadName)}";
        return File(content.Content, content.MimeType, enableRangeProcessing: true);
    }

    [HttpPost("{id:guid}/actions/delete-request")]
    public async Task<ActionResult<ApiResponse<FileAttachmentDto>>> RequestDeletion(
        Guid id,
        [FromBody] RequestFileDeletionCommand request,
        CancellationToken cancellationToken)
    {
        var result = await fileCenterService.RequestDeletionAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }
}

public sealed class UploadFileForm
{
    public IFormFile? File { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid? BusinessObjectId { get; set; }
    public Guid? WorkflowInstanceId { get; set; }
    public Guid? WorkflowTaskId { get; set; }
    public string Sensitivity { get; set; } = "Internal";
}
