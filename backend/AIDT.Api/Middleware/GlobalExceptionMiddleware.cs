using AIDT.Api.Contracts;
using AIDT.Application.Abstractions;
using AIDT.Domain.Common;
using System.Security.Claims;

namespace AIDT.Api.Middleware;

public sealed partial class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.RequestAborted.IsCancellationRequested)
        {
            await WriteErrorAsync(context, exception);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, Exception exception)
    {
        var (status, code, message) = Map(exception);
        if (status == StatusCodes.Status403Forbidden)
        {
            await TryRecordDeniedAuditAsync(context, code, message);
        }
        if (status >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandledFailure(
                logger,
                exception,
                context.TraceIdentifier,
                context.Request.Path.Value);
        }
        else
        {
            LogRejectedRequest(
                logger,
                context.TraceIdentifier,
                code,
                status);
        }

        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(
            ApiResponse.Error(code, message, context.TraceIdentifier),
            cancellationToken: context.RequestAborted);
    }

    private static (int Status, string Code, string Message) Map(Exception exception) =>
        exception switch
        {
            ValidationException domain => (StatusCodes.Status400BadRequest, domain.ErrorCode, domain.Message),
            AidtDomainException { ErrorCode: ErrorCodes.AuthenticationRequired } domain =>
                (StatusCodes.Status401Unauthorized, domain.ErrorCode, domain.Message),
            AidtDomainException { ErrorCode: ErrorCodes.PermissionDenied or ErrorCodes.DataScopeDenied or ErrorCodes.DepartmentScopeDenied or ErrorCodes.ProjectScopeDenied or ErrorCodes.TaskAssigneeMismatch or ErrorCodes.FileScanRequired or ErrorCodes.AgentSelfReviewDenied } domain =>
                    (StatusCodes.Status403Forbidden, domain.ErrorCode, domain.Message),
            UnsupportedMediaTypeException domain =>
                (StatusCodes.Status415UnsupportedMediaType, domain.ErrorCode, domain.Message),
            NotFoundException domain => (StatusCodes.Status404NotFound, domain.ErrorCode, domain.Message),
            AidtDomainException { ErrorCode: ErrorCodes.PersistenceNotReady } domain =>
                (StatusCodes.Status503ServiceUnavailable, domain.ErrorCode, domain.Message),
            AidtDomainException { ErrorCode: ErrorCodes.ServiceUnavailable } domain =>
                (StatusCodes.Status503ServiceUnavailable, domain.ErrorCode, domain.Message),
            ConflictException domain => (StatusCodes.Status409Conflict, domain.ErrorCode, domain.Message),
            AidtDomainException domain => (StatusCodes.Status400BadRequest, domain.ErrorCode, domain.Message),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, ErrorCodes.ValidationFailed, "请求格式不正确。"),
            _ => (StatusCodes.Status500InternalServerError, ErrorCodes.UnexpectedError, "服务器发生未预期错误，请使用 TraceId 联系管理员。")
        };

    private static async Task TryRecordDeniedAuditAsync(
        HttpContext context,
        string code,
        string reason)
    {
        if (!Guid.TryParse(
                context.User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var actorId)
            || actorId == Guid.Empty)
        {
            return;
        }

        try
        {
            var auditService = context.RequestServices.GetService<ISecurityAuditService>();
            if (auditService is null)
            {
                return;
            }

            await auditService.RecordDeniedRequestAsync(
                actorId,
                context.Request.Method,
                context.Request.Path.Value ?? "/",
                context.TraceIdentifier,
                code,
                reason,
                context.RequestAborted);
        }
        catch (Exception)
        {
            // Authorization remains fail-closed when the audit sink is unavailable.
        }
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Error,
        Message = "Unhandled request failure. TraceId={TraceId}, Path={Path}")]
    private static partial void LogUnhandledFailure(
        ILogger logger,
        Exception exception,
        string traceId,
        string? path);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Request rejected. TraceId={TraceId}, Code={Code}, Status={Status}")]
    private static partial void LogRejectedRequest(
        ILogger logger,
        string traceId,
        string code,
        int status);
}
