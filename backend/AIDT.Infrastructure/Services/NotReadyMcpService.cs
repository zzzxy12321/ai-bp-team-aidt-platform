using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;

namespace AIDT.Infrastructure.Services;

public sealed class NotReadyMcpService :
    IMcpOAuthService,
    IMcpAuditService,
    IMcpHumanConfirmationService,
    IMcpManagementService
{
    public Task<McpClientRegistrationDto> RegisterClientAsync(
        McpClientRegistrationRequest request,
        CancellationToken cancellationToken) =>
        throw NotReady();

    public Task<McpAuthorizationRequestDto> CreateAuthorizationRequestAsync(
        McpAuthorizationRequestCommand command,
        UserPrincipal user,
        CancellationToken cancellationToken) =>
        throw NotReady();

    public Task<McpAuthorizationRequestDto?> GetAuthorizationRequestAsync(
        Guid requestId,
        UserPrincipal user,
        CancellationToken cancellationToken) =>
        throw NotReady();

    public Task<McpAuthorizationDecisionDto> ApproveAuthorizationAsync(
        Guid requestId,
        UserPrincipal user,
        IReadOnlyList<Guid> departmentIds,
        IReadOnlyList<string> grantedScopes,
        CancellationToken cancellationToken) =>
        throw NotReady();

    public Task<McpTokenSetDto?> ExchangeAuthorizationCodeAsync(
        string clientId,
        string? clientSecret,
        string code,
        string redirectUri,
        string codeVerifier,
        CancellationToken cancellationToken) =>
        throw NotReady();

    public Task<McpTokenSetDto?> RefreshAsync(
        string clientId,
        string? clientSecret,
        string refreshToken,
        CancellationToken cancellationToken) =>
        throw NotReady();

    public Task RevokeAsync(
        string clientId,
        string? clientSecret,
        string token,
        CancellationToken cancellationToken) =>
        throw NotReady();

    public Task<McpServiceTokenDto> IssueServiceTokenAsync(
        UserPrincipal issuer,
        McpServiceTokenRequest request,
        CancellationToken cancellationToken) =>
        throw NotReady();

    public Task<McpTokenValidationResult> ValidateAccessTokenAsync(
        string rawToken,
        CancellationToken cancellationToken) =>
        throw NotReady();

    public Task MarkAccessTokenUsedAsync(
        Guid tokenId,
        DateTimeOffset usedAt,
        CancellationToken cancellationToken) =>
        throw NotReady();

    public Task RecordAsync(McpCallAudit audit, CancellationToken cancellationToken) =>
        throw NotReady();

    public Task RecordSecurityEventAsync(
        Guid actorId,
        string action,
        string entityType,
        string entityId,
        string traceId,
        string outcome,
        string? reason,
        string? metadataJson,
        CancellationToken cancellationToken) =>
        throw NotReady();

    public Task<McpHumanConfirmationDto> RequestAsync(
        McpConfirmationRequest request,
        CancellationToken cancellationToken) =>
        throw NotReady();

    public Task EnsureConsumedAsync(
        Guid confirmationId,
        UserPrincipal user,
        string clientId,
        string toolName,
        string resourceType,
        string resourceId,
        string requestHash,
        CancellationToken cancellationToken) =>
        throw NotReady();

    public Task<McpHumanConfirmationDto> ConfirmAsync(
        Guid confirmationId,
        UserPrincipal user,
        string? comment,
        string traceId,
        CancellationToken cancellationToken) =>
        throw NotReady();

    public Task<McpHumanConfirmationDto> RejectAsync(
        Guid confirmationId,
        UserPrincipal user,
        string? comment,
        string traceId,
        CancellationToken cancellationToken) =>
        throw NotReady();

    public Task<McpManagementSnapshotDto> GetSnapshotAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        CancellationToken cancellationToken) =>
        throw NotReady();

    public Task<IReadOnlyList<McpHumanConfirmationDto>> ListOwnConfirmationsAsync(
        UserPrincipal user,
        CancellationToken cancellationToken) =>
        throw NotReady();

    public Task<IReadOnlyList<McpHumanConfirmationViewDto>> ListOwnConfirmationViewsAsync(
        UserPrincipal user,
        CancellationToken cancellationToken) =>
        throw NotReady();

    public Task RevokeClientAsync(
        UserPrincipal user,
        string clientId,
        string traceId,
        CancellationToken cancellationToken) =>
        throw NotReady();

    public Task RevokeTokenAsync(
        UserPrincipal user,
        Guid tokenId,
        string traceId,
        CancellationToken cancellationToken) =>
        throw NotReady();

    private static AidtDomainException NotReady() =>
        new(ErrorCodes.PersistenceNotReady, "SQL Server 尚未就绪，MCP 授权与审计不可用。");
}
