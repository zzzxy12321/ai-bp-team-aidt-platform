using System.Text;
using AIDT.Api.Contracts;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Application.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIDT.Api.Controllers;

[ApiController]
[Route("api/v1/minutes")]
[Authorize]
public sealed class MinutesController(
    ICurrentUserAccessor currentUserAccessor,
    IMinutesService minutesService,
    IFileCenterService fileCenterService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<MinutesSourceDto>>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeTestData = false,
        [FromQuery] string? search = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? actionItemStatus = null,
        [FromQuery] string? sourceType = null,
        [FromQuery] DateTimeOffset? meetingFrom = null,
        [FromQuery] DateTimeOffset? meetingTo = null,
        CancellationToken cancellationToken = default)
    {
        var result = await minutesService.ListSourcesAsync(
            currentUserAccessor.GetRequiredUser(),
            new MinutesSourceQuery(
                page,
                pageSize,
                includeTestData,
                search,
                departmentId,
                status,
                actionItemStatus,
                sourceType,
                meetingFrom,
                meetingTo),
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<MinutesSummaryDto>>> Summary(
        [FromQuery] bool includeTestData = false,
        CancellationToken cancellationToken = default)
    {
        var result = await minutesService.GetSummaryAsync(
            currentUserAccessor.GetRequiredUser(),
            includeTestData,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<MinutesSourceDto>>> Get(
        Guid id,
        [FromQuery] bool includeTestData,
        CancellationToken cancellationToken)
    {
        var result = await minutesService.GetSourceAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            includeTestData,
            cancellationToken);
        return result is null
            ? NotFound(ApiResponse.Error("RESOURCE_NOT_FOUND", "听记不存在。", HttpContext.TraceIdentifier))
            : Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<MinutesSourceDto>>> Register(
        [FromBody] RegisterMinutesRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await minutesService.RegisterAsync(
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
    public async Task<ActionResult<ApiResponse<MinutesSourceDto>>> Upload(
        [FromForm] MinutesUploadForm form,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (form.File is null || form.File.Length == 0)
        {
            return BadRequest(ApiResponse.Error(
                "VALIDATION_FAILED",
                "听记文字稿不能为空。",
                HttpContext.TraceIdentifier));
        }
        var extension = Path.GetExtension(form.File.FileName).ToLowerInvariant();
        if (extension is not (".txt" or ".md" or ".json" or ".docx"))
        {
            return BadRequest(ApiResponse.Error(
                "UNSUPPORTED_MINUTES_FILE",
                "本批次解析 .docx、.txt、.md、.json 文字稿；PDF 请粘贴文字内容或转换为 DOCX。",
                HttpContext.TraceIdentifier));
        }

        string transcript;
        if (extension == ".docx")
        {
            await using var documentStream = form.File.OpenReadStream();
            transcript = MinutesDocumentParser.ParseDocx(documentStream);
        }
        else
        {
            await using (var readStream = form.File.OpenReadStream())
            using (var reader = new StreamReader(
                readStream,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
                detectEncodingFromByteOrderMarks: true))
            {
                try
                {
                    transcript = await reader.ReadToEndAsync(cancellationToken);
                }
                catch (DecoderFallbackException)
                {
                    return BadRequest(ApiResponse.Error(
                        "VALIDATION_FAILED",
                        "听记文字稿必须是 UTF-8 文本。",
                        HttpContext.TraceIdentifier));
                }
            }
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
        var result = await minutesService.RegisterAsync(
            currentUserAccessor.GetRequiredUser(),
            new RegisterMinutesRequest(
                form.DepartmentId,
                form.Title,
                form.MeetingAt,
                transcript,
                Guid.Parse(attachment.Id),
                "ManualUpload"),
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/actions/analyze")]
    public async Task<ActionResult<ApiResponse<AgentRunDto>>> Analyze(
        Guid id,
        [FromBody] AnalyzeMinutesRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await minutesService.RequestAnalysisAsync(
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

    [HttpGet("{id:guid}/action-items")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MinutesActionItemDto>>>> ActionItems(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await minutesService.ListActionItemsAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("confirmation-users")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MinutesConfirmationUserDto>>>> ConfirmationUsers(
        [FromQuery] Guid departmentId,
        CancellationToken cancellationToken)
    {
        var result = await minutesService.ListConfirmationUsersAsync(
            currentUserAccessor.GetRequiredUser(),
            departmentId,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}/action-items/{actionItemId:guid}")]
    public async Task<ActionResult<ApiResponse<MinutesActionItemDto>>> UpdateActionItem(
        Guid id,
        Guid actionItemId,
        [FromBody] UpdateMinutesActionItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await minutesService.UpdateActionItemAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            actionItemId,
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("action-items/{actionItemId:guid}/actions/confirm")]
    public async Task<ActionResult<ApiResponse<MinutesTaskDto>>> Confirm(
        Guid actionItemId,
        [FromBody] ConfirmMinutesActionItemRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await minutesService.ConfirmActionItemAsync(
            currentUserAccessor.GetRequiredUser(),
            actionItemId,
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("by-task/{taskId:guid}")]
    public async Task<ActionResult<ApiResponse<MinutesTaskTraceDto>>> Trace(
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var result = await minutesService.TraceTaskAsync(
            currentUserAccessor.GetRequiredUser(),
            taskId,
            cancellationToken);
        return result is null
            ? NotFound(ApiResponse.Error("RESOURCE_NOT_FOUND", "听记任务不存在。", HttpContext.TraceIdentifier))
            : Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }
}

public sealed class MinutesUploadForm
{
    public IFormFile? File { get; set; }
    public Guid DepartmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset? MeetingAt { get; set; }
}
