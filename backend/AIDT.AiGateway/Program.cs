using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AIDT.AiGateway;
using Microsoft.Extensions.Options;

namespace AIDT.AiGateway;

public sealed class Program
{
    public static void Main(string[] args) => CreateApp(args).Run();

    public static WebApplication CreateApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services
            .AddOptions<AiGatewayOptions>()
            .Bind(builder.Configuration.GetSection(AiGatewayOptions.SectionName))
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<AiGatewayOptions>, AiGatewayOptionsValidation>();
        builder.Services.AddSingleton<IGatewayModelInvoker, RuleSimulatorModelInvoker>();

        var app = builder.Build();

        app.MapGet("/health/live", () => Results.Ok(new
        {
            status = "healthy",
            service = "AIDT.AiGateway",
            mode = "RuleSimulator",
            checkedAt = DateTimeOffset.UtcNow
        })).AllowAnonymous();

        app.MapGet("/health/ready", (IOptions<AiGatewayOptions> options) =>
        {
            var settings = options.Value;
            return Results.Ok(new
            {
                status = "ready",
                mode = settings.Mode,
                externalModelEnabled = settings.ExternalModelEnabled,
                timeoutSeconds = settings.TimeoutSeconds,
                allowedModels = settings.AllowedModels
            });
        }).AllowAnonymous();

        app.MapGet("/v1/models", (IOptions<AiGatewayOptions> options) =>
        {
            var data = options.Value.AllowedModels.Select(model => new
            {
                id = model,
                @object = "model",
                owned_by = "AIDT.RuleSimulator",
                mode = "mock"
            });
            return Results.Ok(new { @object = "list", data });
        }).AllowAnonymous();

        app.MapPost("/v1/chat/completions", async (
            ChatCompletionRequest request,
            IOptions<AiGatewayOptions> options,
            IGatewayModelInvoker invoker,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var settings = options.Value;
            if (!settings.AllowedModels.Contains(request.Model, StringComparer.Ordinal))
            {
                return Results.Json(
                    new
                    {
                        error = new
                        {
                            code = "MODEL_NOT_ALLOWED",
                            message = "请求模型不在 AIDT AiGateway 白名单中。",
                            traceId = context.TraceIdentifier
                        }
                    },
                    statusCode: StatusCodes.Status400BadRequest);
            }

            if (request.Messages.Count == 0)
            {
                return Results.Json(
                    new
                    {
                        error = new
                        {
                            code = "VALIDATION_FAILED",
                            message = "至少需要一条消息。",
                            traceId = context.TraceIdentifier
                        }
                    },
                    statusCode: StatusCodes.Status400BadRequest);
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));
            try
            {
                if (request.SimulateDelayMs is > 0)
                {
                    await Task.Delay(
                        TimeSpan.FromMilliseconds(Math.Min(request.SimulateDelayMs.Value, 15_000)),
                        timeout.Token);
                }
                var content = await invoker.InvokeAsync(request.Model, request.Messages, timeout.Token);
                var inputDigest = HashMessages(request.Messages);
                var outputDigest = HashText(content);
                context.Response.Headers["X-AIDT-Gateway-Mode"] = settings.Mode;
                context.Response.Headers["X-AIDT-External-Model"] = "false";
                context.Response.Headers["X-AIDT-Input-Digest"] = inputDigest;
                AiGatewayLogs.RequestCompleted(
                    app.Logger,
                    request.Model,
                    request.Messages.Count,
                    inputDigest);

                return Results.Ok(new ChatCompletionResponse(
                    $"chatcmpl-mock-{Guid.NewGuid():N}",
                    "chat.completion",
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    request.Model,
                    [new ChatChoice(0, new ChatMessage("assistant", content), "stop")],
                    new GatewayAudit(
                        settings.Mode,
                        false,
                        inputDigest,
                        outputDigest,
                        settings.TimeoutSeconds * 1000)));
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                AiGatewayLogs.RequestTimedOut(app.Logger, request.Model, settings.TimeoutSeconds);
                return Results.Json(
                    new
                    {
                        error = new
                        {
                            code = "GATEWAY_TIMEOUT",
                            message = "AiGateway 规则模拟超时，请稍后重试。",
                            traceId = context.TraceIdentifier
                        }
                    },
                    statusCode: StatusCodes.Status504GatewayTimeout);
            }
        }).AllowAnonymous();

        return app;
    }

    private static string HashMessages(IReadOnlyList<ChatMessage> messages)
    {
        var canonical = JsonSerializer.Serialize(messages, JsonSerializerOptions.Web);
        return HashText(canonical);
    }

    private static string HashText(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}

internal static partial class AiGatewayLogs
{
    [LoggerMessage(
        EventId = 3101,
        Level = LogLevel.Information,
        Message = "AiGateway rule simulation completed. Model={Model}, Messages={MessageCount}, InputDigest={InputDigest}")]
    public static partial void RequestCompleted(
        ILogger logger,
        string model,
        int messageCount,
        string inputDigest);

    [LoggerMessage(
        EventId = 3102,
        Level = LogLevel.Warning,
        Message = "AiGateway rule simulation timed out. Model={Model}, TimeoutSeconds={TimeoutSeconds}")]
    public static partial void RequestTimedOut(
        ILogger logger,
        string model,
        int timeoutSeconds);
}
