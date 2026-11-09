using AIDT.Domain.Common;

namespace AIDT.Domain.Mcp;

public enum McpTokenKind
{
    Access,
    Refresh
}

public enum McpHumanConfirmationStatus
{
    Pending,
    Confirmed,
    Rejected,
    Consumed
}

public sealed class McpAccessTokenGrant
{
    private McpAccessTokenGrant()
    {
    }

    public Guid Id { get; private init; }

    public McpTokenKind Kind { get; private init; }

    public DateTimeOffset ExpiresAt { get; private init; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsActive(DateTimeOffset now) =>
        RevokedAt is null && ExpiresAt > now.ToUniversalTime();

    public void Revoke(DateTimeOffset now)
    {
        if (RevokedAt is not null)
        {
            throw new ConflictException("MCP_TOKEN_ALREADY_REVOKED", "令牌已经撤销。");
        }

        RevokedAt = now.ToUniversalTime();
    }

    public static McpAccessTokenGrant Issue(
        Guid id,
        McpTokenKind kind,
        DateTimeOffset expiresAt) =>
        new()
        {
            Id = DomainChecks.RequiredId(id, "MCP 令牌"),
            Kind = kind,
            ExpiresAt = expiresAt.ToUniversalTime()
        };
}

public sealed class McpHumanConfirmation
{
    private McpHumanConfirmation()
    {
    }

    public Guid Id { get; private init; }

    public Guid UserId { get; private init; }

    public string ToolName { get; private init; } = string.Empty;

    public string RequestHash { get; private init; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private init; }

    public McpHumanConfirmationStatus Status { get; private set; }

    public Guid? ConfirmedBy { get; private set; }

    public DateTimeOffset? ConfirmedAt { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    public static McpHumanConfirmation Create(
        Guid id,
        Guid userId,
        string toolName,
        string requestHash,
        DateTimeOffset expiresAt) =>
        new()
        {
            Id = DomainChecks.RequiredId(id, "真人确认"),
            UserId = DomainChecks.RequiredId(userId, "确认用户"),
            ToolName = DomainChecks.Required(toolName, "MCP 工具", 128),
            RequestHash = DomainChecks.Required(requestHash, "请求摘要", 64),
            ExpiresAt = expiresAt.ToUniversalTime(),
            Status = McpHumanConfirmationStatus.Pending
        };

    public void Confirm(Guid confirmedBy, DateTimeOffset now)
    {
        if (Status != McpHumanConfirmationStatus.Pending)
        {
            throw new ConflictException("MCP_CONFIRMATION_INVALID", "真人确认不处于待确认状态。");
        }

        if (confirmedBy != UserId)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "真人确认必须由发起 MCP 调用的用户完成。");
        }

        if (ExpiresAt <= now.ToUniversalTime())
        {
            throw new ConflictException("MCP_CONFIRMATION_EXPIRED", "真人确认已过期。");
        }

        Status = McpHumanConfirmationStatus.Confirmed;
        ConfirmedBy = confirmedBy;
        ConfirmedAt = now.ToUniversalTime();
    }

    public void Reject(Guid rejectedBy, DateTimeOffset now)
    {
        if (Status != McpHumanConfirmationStatus.Pending)
        {
            throw new ConflictException("MCP_CONFIRMATION_INVALID", "真人确认不处于待确认状态。");
        }

        if (rejectedBy != UserId)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "只能拒绝本人的 MCP 真人确认。");
        }

        Status = McpHumanConfirmationStatus.Rejected;
        ConfirmedBy = rejectedBy;
        ConfirmedAt = now.ToUniversalTime();
    }

    public void Consume(DateTimeOffset now)
    {
        if (Status != McpHumanConfirmationStatus.Confirmed)
        {
            throw new ConflictException("MCP_CONFIRMATION_REQUIRED", "缺少已确认的真人确认。");
        }

        if (ExpiresAt <= now.ToUniversalTime())
        {
            throw new ConflictException("MCP_CONFIRMATION_EXPIRED", "真人确认已过期。");
        }

        Status = McpHumanConfirmationStatus.Consumed;
        ConsumedAt = now.ToUniversalTime();
    }
}
