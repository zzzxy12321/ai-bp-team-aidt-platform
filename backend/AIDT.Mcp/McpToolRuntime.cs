using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Common;
using Microsoft.Extensions.Options;
using ModelContextProtocol;

namespace AIDT.Mcp;

public sealed record McpToolResponse(
    string Status,
    object? Data,
    string TraceId,
    McpHumanConfirmationDto? Confirmation = null,
    bool Replayed = false);

public sealed class McpToolRuntime(
    IMcpCallContextAccessor contextAccessor,
    IMcpToolCatalog toolCatalog,
    IMcpHumanConfirmationService confirmationService,
    IMcpAuditService auditService,
    IIdempotencyStore idempotencyStore,
    IUnitOfWork unitOfWork,
    IClock clock,
    IOptions<AidtMcpOptions> options)
{
    private static readonly JsonSerializerOptions CanonicalJsonOptions =
        new(JsonSerializerDefaults.Web);

    public Task<McpToolResponse> ExecuteReadAsync(
        string toolName,
        string resourceType,
        string resourceId,
        Func<McpTokenIdentity, CancellationToken, Task<object?>> action,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            toolName,
            "tool.call",
            resourceType,
            resourceId,
            requestBody: new { },
            idempotencyKey: null,
            humanConfirmationId: null,
            highRisk: false,
            action,
            cancellationToken);

    public Task<McpToolResponse> ExecuteWriteAsync(
        string toolName,
        string resourceType,
        string resourceId,
        object requestBody,
        string idempotencyKey,
        Func<McpTokenIdentity, CancellationToken, Task<object?>> action,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            toolName,
            "tool.call",
            resourceType,
            resourceId,
            requestBody,
            idempotencyKey,
            humanConfirmationId: null,
            highRisk: false,
            action,
            cancellationToken);

    public Task<McpToolResponse> ExecuteHighRiskAsync(
        string toolName,
        string resourceType,
        string resourceId,
        object requestBody,
        string idempotencyKey,
        Guid? humanConfirmationId,
        Func<McpTokenIdentity, CancellationToken, Task<object?>> action,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            toolName,
            "tool.call",
            resourceType,
            resourceId,
            requestBody,
            idempotencyKey,
            humanConfirmationId,
            highRisk: true,
            action,
            cancellationToken);

    public async Task<string> ExecuteResourceReadAsync(
        string resourceName,
        string scope,
        string resourceType,
        string resourceId,
        Func<McpTokenIdentity, CancellationToken, Task<object?>> action,
        CancellationToken cancellationToken)
    {
        var context = await contextAccessor.GetRequiredAsync(cancellationToken);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            EnsureScope(context.Identity, scope);
            EnsureAgentCapability(
                context.Identity,
                CapabilityForResourceType(resourceType));
            var data = await action(context.Identity, cancellationToken);
            EnsureResponseSize(data);
            stopwatch.Stop();
            await AuditAsync(
                context,
                "resource.read",
                resourceName,
                resourceType,
                resourceId,
                scopes: [scope],
                true,
                null,
                Duration(stopwatch),
                Hash(new { resourceId }),
                Hash(data),
                cancellationToken);
            return JsonSerializer.Serialize(
                new
                {
                    status = "OK",
                    traceId = context.TraceId,
                    data
                });
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            var code = ErrorCode(exception);
            await AuditAsync(
                context,
                "resource.read",
                resourceName,
                resourceType,
                resourceId,
                scopes: [scope],
                false,
                code,
                Duration(stopwatch),
                Hash(new { resourceId }),
                null,
                cancellationToken);
            throw ToMcpException(exception, code);
        }
    }

    private async Task<McpToolResponse> ExecuteAsync(
        string toolName,
        string operation,
        string resourceType,
        string resourceId,
        object requestBody,
        string? idempotencyKey,
        Guid? humanConfirmationId,
        bool highRisk,
        Func<McpTokenIdentity, CancellationToken, Task<object?>> action,
        CancellationToken cancellationToken)
    {
        var descriptor = toolCatalog.Find(toolName)
            ?? throw new McpException($"UNKNOWN_MCP_TOOL: 工具 {toolName} 未登记。");
        var context = await contextAccessor.GetRequiredAsync(cancellationToken);
        var requestHash = HashForRequest(requestBody);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            EnsureScope(context.Identity, descriptor.Scope);
            EnsureAgentCapability(context.Identity, descriptor.AgentCapability);
            if (!descriptor.ReadOnly)
            {
                ValidateIdempotencyKey(idempotencyKey);
                var replay = await FindIdempotentResponseAsync(
                    context.Identity,
                    toolName,
                    idempotencyKey!,
                    requestHash,
                    cancellationToken);
                if (replay is not null)
                {
                    stopwatch.Stop();
                    await AuditAsync(
                        context,
                        operation,
                        toolName,
                        resourceType,
                        resourceId,
                        context.Identity.Scopes,
                        true,
                        "IDEMPOTENT_REPLAY",
                        Duration(stopwatch),
                        requestHash,
                        replay.ResponseId,
                        cancellationToken,
                        idempotencyKey);
                    return new McpToolResponse(
                        "REPLAYED",
                        new { id = replay.ResponseId },
                        context.TraceId,
                        Replayed: true);
                }
            }

            if (highRisk)
            {
                if (!humanConfirmationId.HasValue)
                {
                    var confirmation = await confirmationService.RequestAsync(
                        new McpConfirmationRequest(
                            context.Identity.UserId,
                            context.Identity.OrganizationId,
                            context.Identity.ClientId,
                            toolName,
                            resourceType,
                            string.IsNullOrWhiteSpace(resourceId) ? toolName : resourceId,
                            requestHash,
                            BuildConfirmationSummary(toolName, resourceType, resourceId)),
                        cancellationToken);
                    stopwatch.Stop();
                    await AuditAsync(
                        context,
                        operation,
                        toolName,
                        resourceType,
                        resourceId,
                        context.Identity.Scopes,
                        false,
                        "HUMAN_CONFIRMATION_REQUIRED",
                        Duration(stopwatch),
                        requestHash,
                        null,
                        cancellationToken,
                        idempotencyKey);
                    return new McpToolResponse(
                        "HUMAN_CONFIRMATION_REQUIRED",
                        new
                        {
                            confirmation.Id,
                            confirmationUrl =
                                $"{options.Value.PublicBaseUrl.TrimEnd('/')}/mcp-confirmations?confirmationId={confirmation.Id:D}",
                            confirmation.ExpiresAt,
                            confirmation.RequestHash,
                            instruction = "请当前用户打开 confirmationUrl 在 AIDT 浏览器中确认；确认后将 humanConfirmationId 原值重试本工具。"
                        },
                        context.TraceId,
                        confirmation);
                }

                await confirmationService.EnsureConsumedAsync(
                    humanConfirmationId.Value,
                    context.Identity.User,
                    context.Identity.ClientId,
                    toolName,
                    resourceType,
                    string.IsNullOrWhiteSpace(resourceId) ? toolName : resourceId,
                    requestHash,
                    cancellationToken);
            }

            var data = await action(context.Identity, cancellationToken);
            EnsureResponseSize(data);
            if (!descriptor.ReadOnly)
            {
                await SaveIdempotencyAsync(
                    context.Identity,
                    toolName,
                    idempotencyKey!,
                    requestHash,
                    data,
                    cancellationToken);
            }

            stopwatch.Stop();
            await AuditAsync(
                context,
                operation,
                toolName,
                resourceType,
                resourceId,
                context.Identity.Scopes,
                true,
                null,
                Duration(stopwatch),
                requestHash,
                Hash(data),
                cancellationToken,
                idempotencyKey);
            return new McpToolResponse("OK", data, context.TraceId);
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            var code = ErrorCode(exception);
            await AuditAsync(
                context,
                operation,
                toolName,
                resourceType,
                resourceId,
                context.Identity.Scopes,
                false,
                code,
                Duration(stopwatch),
                requestHash,
                null,
                cancellationToken,
                idempotencyKey);
            throw ToMcpException(exception, code);
        }
    }

    private async Task<IdempotencyEntry?> FindIdempotentResponseAsync(
        McpTokenIdentity identity,
        string toolName,
        string idempotencyKey,
        string requestHash,
        CancellationToken cancellationToken)
    {
        var entry = await idempotencyStore.FindAsync(
            Scope(identity, toolName),
            idempotencyKey,
            cancellationToken);
        if (entry is not null
            && !string.Equals(entry.RequestHash, requestHash, StringComparison.Ordinal))
        {
            throw new ConflictException(
                "MCP_IDEMPOTENCY_CONFLICT",
                "同一幂等键已用于不同请求内容。");
        }

        return entry;
    }

    private async Task SaveIdempotencyAsync(
        McpTokenIdentity identity,
        string toolName,
        string idempotencyKey,
        string requestHash,
        object? data,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await idempotencyStore.AddAsync(
            new IdempotencyEntry(
                Guid.NewGuid(),
                Scope(identity, toolName),
                idempotencyKey,
                requestHash,
                data?.GetType().Name ?? "Empty",
                ExtractResourceId(data) ?? toolName,
                "completed",
                now,
                now.AddDays(7)),
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task AuditAsync(
        McpCallContext context,
        string operation,
        string toolOrResource,
        string resourceType,
        string resourceId,
        IReadOnlyList<string> scopes,
        bool succeeded,
        string? code,
        int durationMs,
        string requestHash,
        string? responseHash,
        CancellationToken cancellationToken,
        string? idempotencyKey = null)
    {
        await auditService.RecordAsync(
            new McpCallAudit(
                context.Identity.UserId,
                context.Identity.TokenId,
                context.Identity.ClientId,
                operation,
                toolOrResource,
                resourceType,
                string.IsNullOrWhiteSpace(resourceId) ? toolOrResource : resourceId,
                context.TraceId,
                idempotencyKey,
                null,
                scopes,
                context.Identity.DepartmentIds,
                succeeded,
                code,
                durationMs,
                requestHash,
                responseHash),
            cancellationToken);
    }

    private static void EnsureScope(McpTokenIdentity identity, string scope)
    {
        if (!identity.Scopes.Contains(scope, StringComparer.Ordinal))
        {
            throw new PermissionException(
                "MCP_INSUFFICIENT_SCOPE",
                $"当前 MCP 令牌缺少 {scope} 作用域。");
        }
    }

    private static void EnsureAgentCapability(
        McpTokenIdentity identity,
        string capability)
    {
        if (!identity.AgentProfileId.HasValue)
        {
            return;
        }

        if (!(identity.AllowedCapabilities ?? [])
            .Contains(capability, StringComparer.OrdinalIgnoreCase))
        {
            throw new PermissionException(
                ErrorCodes.PermissionDenied,
                $"数字员工未获授权 MCP 能力 {capability}。");
        }
    }

    private static string CapabilityForResourceType(string resourceType) =>
        resourceType switch
        {
            "Project" or "ProjectTask" => "project.read",
            "EventInbox" => "event.inbox.review",
            "WorkflowInstance" or "WorkflowTask" => "workflow.read",
            "FileAttachment" => "file.metadata.read",
            _ => "identity.read"
        };

    private static void ValidateIdempotencyKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length is < 8 or > 128)
        {
            throw new ValidationException("MCP_IDEMPOTENCY_KEY_REQUIRED", "写工具必须提供 8—128 字幂等键。");
        }
    }

    private static string Scope(McpTokenIdentity identity, string toolName) =>
        $"mcp:{identity.UserId:D}:{toolName}";

    private static string Hash(object? value)
    {
        var json = JsonSerializer.Serialize(value, CanonicalJsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }

    private static string HashForRequest(object? value)
    {
        if (value is null)
        {
            return Hash(null);
        }

        var node = JsonSerializer.SerializeToNode(value, CanonicalJsonOptions);
        if (node is System.Text.Json.Nodes.JsonObject jsonObject)
        {
            foreach (var propertyName in jsonObject
                         .Select(item => item.Key)
                         .Where(key => string.Equals(
                             key,
                             "humanConfirmationId",
                             StringComparison.OrdinalIgnoreCase))
                         .ToList())
            {
                jsonObject.Remove(propertyName);
            }
        }

        var json = node?.ToJsonString() ?? "null";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }

    private static string? ExtractResourceId(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var property = value.GetType()
            .GetProperty("Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        return property?.GetValue(value)?.ToString();
    }

    private static string BuildConfirmationSummary(
        string toolName,
        string resourceType,
        string resourceId) =>
        $"{toolName} 请求对 {resourceType} {resourceId} 执行高风险动作。请核对调用方、资源和参数摘要后确认。";

    private static string ErrorCode(Exception exception) =>
        exception switch
        {
            AidtDomainException domain => domain.ErrorCode,
            McpException => "MCP_PROTOCOL_ERROR",
            _ => ErrorCodes.UnexpectedError
        };

    private static McpException ToMcpException(Exception exception, string code) =>
        exception is McpException mcp
            ? mcp
            : new McpException(
                exception is AidtDomainException domain
                    ? $"{code}: {domain.Message}"
                    : $"{code}: MCP 工具执行失败，请使用 TraceId 联系管理员。");

    private static int Duration(Stopwatch stopwatch) =>
        (int)Math.Min(int.MaxValue, stopwatch.ElapsedMilliseconds);

    private void EnsureResponseSize(object? data)
    {
        if (data is null)
        {
            return;
        }

        var bytes = JsonSerializer.SerializeToUtf8Bytes(data, CanonicalJsonOptions);
        if (bytes.Length > options.Value.MaxResponseBytes)
        {
            throw new ValidationException(
                "MCP_RESPONSE_TOO_LARGE",
                $"MCP 工具响应超过 {options.Value.MaxResponseBytes} 字节上限，请缩小分页或改用 AIDT 文件中心。");
        }
    }
}
