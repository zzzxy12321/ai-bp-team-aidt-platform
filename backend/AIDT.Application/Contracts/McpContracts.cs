using AIDT.Domain.Identity;

using System.Text.Json.Serialization;

namespace AIDT.Application.Contracts;

public static class McpScopeNames
{
    public const string Read = "aidt:read";
    public const string Write = "aidt:write";
    public const string Files = "aidt:files";
    public const string Approve = "aidt:approve";
    public const string Manage = "aidt:manage";

    public static IReadOnlyList<string> All { get; } =
        [Read, Write, Files, Approve, Manage];

    public static IReadOnlyList<string> Parse(string? value) =>
        (value ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => item.ToLowerInvariant())
            .Where(item => All.Contains(item, StringComparer.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
}

public sealed record McpToolDescriptorDto(
    string Name,
    string Title,
    string Description,
    string Scope,
    bool ReadOnly,
    bool HighRisk,
    string ResourceType,
    IReadOnlyList<string> ExistingPermissions,
    string AgentCapability = "identity.read");

public sealed record McpClientRegistrationRequest(
    [property: JsonPropertyName("client_name")]
    string ClientName,
    [property: JsonPropertyName("redirect_uris")]
    IReadOnlyList<string> RedirectUris,
    [property: JsonPropertyName("scope")]
    string? Scope,
    [property: JsonPropertyName("grant_types")]
    IReadOnlyList<string>? GrantTypes,
    [property: JsonPropertyName("response_types")]
    IReadOnlyList<string>? ResponseTypes,
    [property: JsonPropertyName("token_endpoint_auth_method")]
    string? TokenEndpointAuthMethod);

public sealed record McpClientRegistrationDto(
    string ClientId,
    string ClientName,
    string TokenEndpointAuthMethod,
    IReadOnlyList<string> RedirectUris,
    IReadOnlyList<string> GrantTypes,
    IReadOnlyList<string> Scopes,
    string? ClientSecret,
    DateTimeOffset CreatedAt);

public sealed record McpAuthorizationRequestCommand(
    string ClientId,
    string RedirectUri,
    string ResponseType,
    string CodeChallenge,
    string CodeChallengeMethod,
    string State,
    string Scope);

public sealed record McpAuthorizationRequestDto(
    Guid Id,
    string ClientId,
    string ClientName,
    string RedirectUri,
    IReadOnlyList<string> RequestedScopes,
    string State,
    DateTimeOffset ExpiresAt);

public sealed record McpAuthorizationDecisionDto(
    string RedirectUri,
    string State,
    string Code,
    DateTimeOffset ExpiresAt);

public sealed record McpTokenSetDto(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string Scope,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    Guid UserId);

public sealed record McpServiceTokenRequest(
    Guid ServiceUserId,
    string ClientName,
    IReadOnlyList<string> Scopes,
    IReadOnlyList<Guid> DepartmentIds,
    int LifetimeHours = 24,
    Guid? AgentProfileId = null);

public sealed record McpServiceTokenDto(
    string ClientId,
    Guid TokenId,
    string AccessToken,
    string TokenType,
    string Scope,
    string ServiceIdentityName,
    DateTimeOffset ExpiresAt);

public sealed record McpTokenIdentity(
    Guid TokenId,
    string TokenKind,
    string ClientId,
    string ClientName,
    Guid UserId,
    Guid OrganizationId,
    IReadOnlyList<string> Scopes,
    IReadOnlyList<Guid> DepartmentIds,
    UserPrincipal User,
    Guid? AgentProfileId = null,
    IReadOnlyList<string>? AllowedCapabilities = null);

public sealed record McpTokenValidationResult(
    McpTokenIdentity? Identity,
    string? ErrorCode,
    string? ErrorDescription);

public sealed record McpCallAudit(
    Guid ActorId,
    Guid TokenId,
    string ClientId,
    string Operation,
    string ToolOrResource,
    string ResourceType,
    string ResourceId,
    string TraceId,
    string? IdempotencyKey,
    string? AgentRunId,
    IReadOnlyList<string> Scopes,
    IReadOnlyList<Guid> DepartmentIds,
    bool Succeeded,
    string? Code,
    int DurationMs,
    string RequestHash,
    string? ResponseHash);

public sealed record McpHumanConfirmationDto(
    Guid Id,
    string ClientId,
    string ToolName,
    string ResourceType,
    string ResourceId,
    string Summary,
    string RequestHash,
    string Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt);

public sealed record McpConfirmationTechnicalInfoDto(
    string ClientId,
    string ResourceType,
    string ResourceId,
    string RequestHash);

public sealed record McpHumanConfirmationViewDto(
    Guid Id,
    string ActionTitle,
    string Status,
    string Summary,
    string ResourceDisplayName,
    string? ProjectName,
    string WillExecute,
    string WillNotExecute,
    string? DecisionComment,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    McpConfirmationTechnicalInfoDto Technical);

public sealed record McpConfirmationRequest(
    Guid UserId,
    Guid OrganizationId,
    string ClientId,
    string ToolName,
    string ResourceType,
    string ResourceId,
    string RequestHash,
    string Summary);

public sealed record McpClientSummaryDto(
    string ClientId,
    string ClientName,
    bool IsActive,
    int ActiveTokenCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt,
    string? AgentProfileId = null,
    string? AgentProfileName = null,
    bool? AgentProfileAvailable = null);

public sealed record McpTokenSummaryDto(
    Guid Id,
    string ClientId,
    string ClientName,
    string UserDisplayName,
    string Kind,
    IReadOnlyList<string> Scopes,
    IReadOnlyList<Guid> DepartmentIds,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt,
    DateTimeOffset? LastUsedAt,
    string? AgentProfileId = null,
    string? AgentProfileName = null,
    bool? AgentProfileAvailable = null,
    string? RevokedBy = null,
    string? RevocationReason = null);

public sealed record McpInvocationSummaryDto(
    Guid AuditId,
    string ToolOrResource,
    string Operation,
    string ClientId,
    Guid ActorId,
    string TraceId,
    string Outcome,
    string? Code,
    int DurationMs,
    DateTimeOffset OccurredAt);

public sealed record McpManagementSnapshotDto(
    IReadOnlyList<McpToolDescriptorDto> Tools,
    IReadOnlyList<McpClientSummaryDto> Clients,
    IReadOnlyList<McpTokenSummaryDto> Tokens,
    IReadOnlyList<McpHumanConfirmationDto> Confirmations,
    IReadOnlyList<McpInvocationSummaryDto> RecentInvocations);
