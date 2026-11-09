using System.Text;
using AIDT.Api.Contracts;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Application.Documents;
using AIDT.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIDT.Api.Controllers;

[ApiController]
[Route("api/v1/inbox")]
[Authorize]
public sealed class InboxController(
    ICurrentUserAccessor currentUserAccessor,
    IEventInboxService inboxService,
    IFileCenterService fileCenterService,
    ConfirmInboxEventAndStartWorkflowHandler confirmInboxEventAndStartWorkflowHandler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<InboxEventDto>>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] bool includeTestData = false,
        CancellationToken cancellationToken = default)
    {
        var result = await inboxService.ListAsync(
            currentUserAccessor.GetRequiredUser(),
            page,
            pageSize,
            status,
            includeTestData,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<InboxEventDto>>> Get(
        Guid id,
        [FromQuery] bool includeTestData = false,
        CancellationToken cancellationToken = default)
    {
        var result = await inboxService.GetAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            includeTestData,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return result is null
            ? NotFound(ApiResponse.Error(
                "RESOURCE_NOT_FOUND",
                "事件不存在。",
                HttpContext.TraceIdentifier))
            : Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("sources")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InboxSourceOptionDto>>>> Sources(
        [FromQuery] Guid departmentId,
        CancellationToken cancellationToken)
    {
        var result = await inboxService.ListSourcesAsync(
            currentUserAccessor.GetRequiredUser(),
            departmentId,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<InboxEventDto>>> Receive(
        [FromBody] ReceiveInboxEventRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await inboxService.ReceiveAsync(
            currentUserAccessor.GetRequiredUser(),
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("upload")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<InboxEventDto>>> Upload(
        [FromForm] InboxUploadForm form,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (form.File is null || form.File.Length == 0)
        {
            return BadRequest(ApiResponse.Error(
                "VALIDATION_FAILED",
                "事件文字材料不能为空。",
                HttpContext.TraceIdentifier));
        }

        var extension = Path.GetExtension(form.File.FileName).ToLowerInvariant();
        if (extension is not (".txt" or ".md" or ".json" or ".docx"))
        {
            return BadRequest(ApiResponse.Error(
                "UNSUPPORTED_INBOX_FILE",
                "事件收件箱支持 .docx、.txt、.md、.json；其他文件请先登记为附件再选择来源。",
                HttpContext.TraceIdentifier));
        }

        string content;
        if (extension == ".docx")
        {
            await using var documentStream = form.File.OpenReadStream();
            content = MinutesDocumentParser.ParseDocx(documentStream);
        }
        else
        {
            await using var readStream = form.File.OpenReadStream();
            using var reader = new StreamReader(
                readStream,
                new UTF8Encoding(false, true),
                detectEncodingFromByteOrderMarks: true);
            content = await reader.ReadToEndAsync(cancellationToken);
        }

        await using var uploadStream = form.File.OpenReadStream();
        var attachment = await fileCenterService.UploadAsync(
            currentUserAccessor.GetRequiredUser(),
            new UploadFileCommand(
                uploadStream,
                form.File.FileName,
                form.File.ContentType,
                form.DepartmentId,
                null,
                null,
                null,
                "Internal"),
            HttpContext.TraceIdentifier,
            cancellationToken);
        var result = await inboxService.ReceiveAsync(
            currentUserAccessor.GetRequiredUser(),
            new ReceiveInboxEventRequest(
                form.DepartmentId,
                form.Title,
                content,
                "ManualUpload",
                null,
                Guid.Parse(attachment.Id)),
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/actions/analyze")]
    public async Task<ActionResult<ApiResponse<InboxEventDto>>> Analyze(
        Guid id,
        [FromBody] AnalyzeInboxEventRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await inboxService.AnalyzeAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status202Accepted,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/actions/confirm")]
    public async Task<ActionResult<ApiResponse<InboxConfirmationDto>>> Confirm(
        Guid id,
        [FromBody] ConfirmInboxEventRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await inboxService.ConfirmAsync(
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

    [HttpPost("{id:guid}/actions/confirm-and-start-flow")]
    public async Task<ActionResult<ApiResponse<InboxConfirmationDto>>> ConfirmAndStartFlow(
        Guid id,
        [FromBody] ConfirmInboxEventRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await confirmInboxEventAndStartWorkflowHandler.HandleAsync(
            id,
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("/api/v1/project-tasks/{id:guid}/trace")]
    public async Task<ActionResult<ApiResponse<InboxTaskTraceDto>>> TraceTask(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await inboxService.TraceTaskAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            cancellationToken);
        return result is null
            ? NotFound(ApiResponse.Error(
                "RESOURCE_NOT_FOUND",
                "项目任务或事件证据不存在。",
                HttpContext.TraceIdentifier))
            : Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }
}

public sealed class InboxUploadForm
{
    public IFormFile? File { get; set; }
    public Guid DepartmentId { get; set; }
    public string Title { get; set; } = string.Empty;
}
