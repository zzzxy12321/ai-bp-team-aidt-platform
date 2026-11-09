namespace AIDT.Api.Contracts;

public sealed record ApiResponse<T>(
    string Code,
    string Message,
    T? Data,
    string TraceId);

public static class ApiResponse
{
    public static ApiResponse<T> Ok<T>(T data, string traceId, string message = "success") =>
        new("OK", message, data, traceId);

    public static ApiResponse<object?> Error(
        string code,
        string message,
        string traceId,
        object? data = null) =>
        new(code, message, data, traceId);
}
