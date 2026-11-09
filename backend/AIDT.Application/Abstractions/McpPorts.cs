using AIDT.Application.Contracts;
using AIDT.Domain.Identity;

namespace AIDT.Application.Abstractions;

public interface IMcpToolCatalog
{
    IReadOnlyList<McpToolDescriptorDto> List();

    McpToolDescriptorDto? Find(string toolName);
}

public interface IMcpOAuthService
{
    Task<McpClientRegistrationDto> RegisterClientAsync(
        McpClientRegistrationRequest request,
        CancellationToken cancellationToken);

    Task<McpAuthorizationRequestDto> CreateAuthorizationRequestAsync(
        McpAuthorizationRequestCommand command,
        UserPrincipal user,
        CancellationToken cancellationToken);

    Task<McpAuthorizationRequestDto?> GetAuthorizationRequestAsync(
        Guid requestId,
        UserPrincipal user,
        CancellationToken cancellationToken);

    Task<McpAuthorizationDecisionDto> ApproveAuthorizationAsync(
        Guid requestId,
        UserPrincipal user,
        IReadOnlyList<Guid> departmentIds,
        IReadOnlyList<string> grantedScopes,
        CancellationToken cancellationToken);

    Task<McpTokenSetDto?> ExchangeAuthorizationCodeAsync(
        string clientId,
        string? clientSecret,
        string code,
        string redirectUri,
        string codeVerifier,
        CancellationToken cancellationToken);

    Task<McpTokenSetDto?> RefreshAsync(
        string clientId,
        string? clientSecret,
        string refreshToken,
        CancellationToken cancellationToken);

    Task RevokeAsync(
        string clientId,
        string? clientSecret,
        string token,
        CancellationToken cancellationToken);

    Task<McpServiceTokenDto> IssueServiceTokenAsync(
        UserPrincipal issuer,
        McpServiceTokenRequest request,
        CancellationToken cancellationToken);

    Task<McpTokenValidationResult> ValidateAccessTokenAsync(
        string rawToken,
        CancellationToken cancellationToken);

    Task MarkAccessTokenUsedAsync(
        Guid tokenId,
        DateTimeOffset usedAt,
        CancellationToken cancellationToken);
}

public interface IMcpAuditService
{
    Task RecordAsync(McpCallAudit audit, CancellationToken cancellationToken);

    Task RecordSecurityEventAsync(
        Guid actorId,
        string action,
        string entityType,
        string entityId,
        string traceId,
        string outcome,
        string? reason,
        string? metadataJson,
        CancellationToken cancellationToken);
}

public interface IMcpHumanConfirmationService
{
    Task<McpHumanConfirmationDto> RequestAsync(
        McpConfirmationRequest request,
        CancellationToken cancellationToken);

    Task EnsureConsumedAsync(
        Guid confirmationId,
        UserPrincipal user,
        string clientId,
        string toolName,
        string resourceType,
        string resourceId,
        string requestHash,
        CancellationToken cancellationToken);

    Task<McpHumanConfirmationDto> ConfirmAsync(
        Guid confirmationId,
        UserPrincipal user,
        string? comment,
        string traceId,
        CancellationToken cancellationToken);

    Task<McpHumanConfirmationDto> RejectAsync(
        Guid confirmationId,
        UserPrincipal user,
        string? comment,
        string traceId,
        CancellationToken cancellationToken);
}

public interface IMcpManagementService
{
    Task<McpManagementSnapshotDto> GetSnapshotAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<McpHumanConfirmationDto>> ListOwnConfirmationsAsync(
        UserPrincipal user,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<McpHumanConfirmationViewDto>> ListOwnConfirmationViewsAsync(
        UserPrincipal user,
        CancellationToken cancellationToken);

    Task RevokeClientAsync(
        UserPrincipal user,
        string clientId,
        string traceId,
        CancellationToken cancellationToken);

    Task RevokeTokenAsync(
        UserPrincipal user,
        Guid tokenId,
        string traceId,
        CancellationToken cancellationToken);
}
