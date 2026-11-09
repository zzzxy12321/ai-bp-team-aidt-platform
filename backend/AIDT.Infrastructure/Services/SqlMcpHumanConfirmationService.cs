using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;
using AIDT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Services;

public sealed class SqlMcpHumanConfirmationService(
    AidtDbContext dbContext,
    IClock clock,
    IMcpAuditService auditService) : IMcpHumanConfirmationService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    public async Task<McpHumanConfirmationDto> RequestAsync(
        McpConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        if (string.IsNullOrWhiteSpace(request.Summary) || request.Summary.Length > 2000)
        {
            throw new ValidationException("MCP_CONFIRMATION_SUMMARY_INVALID", "真人确认摘要不能为空且不能超过 2000 字。");
        }

        var existing = await dbContext.McpHumanConfirmations
            .AsNoTracking()
            .Where(item =>
                item.UserId == request.UserId
                && item.OrganizationId == request.OrganizationId
                && item.ClientId == request.ClientId
                && item.ToolName == request.ToolName
                && item.ResourceType == request.ResourceType
                && item.ResourceId == request.ResourceId
                && item.RequestHash == request.RequestHash
                && item.Status == "Pending"
                && item.ExpiresAt > now)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            return Map(existing);
        }

        var entity = new McpHumanConfirmationEntity
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            OrganizationId = request.OrganizationId,
            ClientId = request.ClientId,
            ToolName = request.ToolName,
            ResourceType = request.ResourceType,
            ResourceId = request.ResourceId,
            RequestHash = request.RequestHash,
            Summary = request.Summary,
            Status = "Pending",
            ExpiresAt = now.Add(Lifetime),
            CreatedAt = now
        };
        await dbContext.McpHumanConfirmations.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task EnsureConsumedAsync(
        Guid confirmationId,
        UserPrincipal user,
        string clientId,
        string toolName,
        string resourceType,
        string resourceId,
        string requestHash,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var count = await dbContext.McpHumanConfirmations
            .Where(item =>
                item.Id == confirmationId
                && item.UserId == user.UserId
                && item.OrganizationId == user.OrganizationId
                && item.ClientId == clientId
                && item.ToolName == toolName
                && item.ResourceType == resourceType
                && item.ResourceId == resourceId
                && item.RequestHash == requestHash
                && item.Status == "Confirmed"
                && item.ConsumedAt == null
                && item.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(item => item.Status, "Consumed")
                    .SetProperty(item => item.ConsumedAt, now),
                cancellationToken);
        if (count != 1)
        {
            throw new ConflictException(
                "MCP_CONFIRMATION_REQUIRED",
                "高风险操作需要当前用户在 AIDT 浏览器中确认同一请求；确认可能已被使用、拒绝或过期。");
        }
    }

    public async Task<McpHumanConfirmationDto> ConfirmAsync(
        Guid confirmationId,
        UserPrincipal user,
        string? comment,
        string traceId,
        CancellationToken cancellationToken)
    {
        var normalizedComment = DomainChecks.Required(comment, "MCP 确认意见", 1000);
        var now = clock.UtcNow;
        var count = await dbContext.McpHumanConfirmations
            .Where(item =>
                item.Id == confirmationId
                && item.UserId == user.UserId
                && item.OrganizationId == user.OrganizationId
                && item.Status == "Pending"
                && item.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(item => item.Status, "Confirmed")
                    .SetProperty(item => item.ConfirmedBy, user.UserId)
                    .SetProperty(item => item.ConfirmedAt, now)
                    .SetProperty(item => item.DecisionComment, normalizedComment),
                cancellationToken);
        if (count != 1)
        {
            throw new ConflictException("MCP_CONFIRMATION_INVALID", "真人确认不存在、已处理或已过期。");
        }

        var entity = await dbContext.McpHumanConfirmations
            .AsNoTracking()
            .SingleAsync(item => item.Id == confirmationId, cancellationToken);
        await auditService.RecordSecurityEventAsync(
            user.UserId,
            "confirmation.confirmed",
            "McpHumanConfirmation",
            entity.Id.ToString("D"),
            traceId,
            "Succeeded",
            normalizedComment,
            $"{{\"tool\":\"{entity.ToolName}\",\"clientId\":\"{entity.ClientId}\"}}",
            cancellationToken);
        return Map(entity);
    }

    public async Task<McpHumanConfirmationDto> RejectAsync(
        Guid confirmationId,
        UserPrincipal user,
        string? comment,
        string traceId,
        CancellationToken cancellationToken)
    {
        var normalizedComment = DomainChecks.Required(comment, "MCP 拒绝意见", 1000);
        var now = clock.UtcNow;
        var count = await dbContext.McpHumanConfirmations
            .Where(item =>
                item.Id == confirmationId
                && item.UserId == user.UserId
                && item.OrganizationId == user.OrganizationId
                && item.Status == "Pending"
                && item.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(item => item.Status, "Rejected")
                    .SetProperty(item => item.ConfirmedBy, user.UserId)
                    .SetProperty(item => item.ConfirmedAt, now)
                    .SetProperty(item => item.DecisionComment, normalizedComment),
                cancellationToken);
        if (count != 1)
        {
            throw new ConflictException("MCP_CONFIRMATION_INVALID", "真人确认不存在、已处理或已过期。");
        }

        var entity = await dbContext.McpHumanConfirmations
            .AsNoTracking()
            .SingleAsync(item => item.Id == confirmationId, cancellationToken);
        await auditService.RecordSecurityEventAsync(
            user.UserId,
            "confirmation.rejected",
            "McpHumanConfirmation",
            entity.Id.ToString("D"),
            traceId,
            "Denied",
            normalizedComment,
            $"{{\"tool\":\"{entity.ToolName}\",\"clientId\":\"{entity.ClientId}\"}}",
            cancellationToken);
        return Map(entity);
    }

    public static McpHumanConfirmationDto Map(McpHumanConfirmationEntity entity) =>
        new(
            entity.Id,
            entity.ClientId,
            entity.ToolName,
            entity.ResourceType,
            entity.ResourceId,
            entity.Summary,
            entity.RequestHash,
            entity.Status,
            entity.ExpiresAt,
            entity.CreatedAt);
}
