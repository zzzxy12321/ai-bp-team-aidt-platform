namespace AIDT.Api.Middleware;

public sealed class TraceIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Trace-Id";
    public const string ItemKey = "AIDT.TraceId";

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var incoming = context.Request.Headers[HeaderName].FirstOrDefault();
        var traceId = IsSafeTraceId(incoming)
            ? incoming!
            : context.TraceIdentifier;

        context.TraceIdentifier = traceId;
        context.Items[ItemKey] = traceId;
        context.Response.Headers[HeaderName] = traceId;
        await next(context);
    }

    private static bool IsSafeTraceId(string? traceId) =>
        !string.IsNullOrWhiteSpace(traceId)
        && traceId.Length <= 128
        && traceId.All(character => char.IsLetterOrDigit(character) || character is '-' or '_' or '.');
}
