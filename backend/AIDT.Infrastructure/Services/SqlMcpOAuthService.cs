using System.Text.Json;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Agents;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;
using AIDT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Services;

public sealed class SqlMcpOAuthService(
    AidtDbContext dbContext,
    IClock clock,
    IUserRepository userRepository) : IMcpOAuthService
{
    private static readonly string[] ServiceGrantTypes = ["service_token"];
    private static readonly TimeSpan AuthorizationRequestLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan AuthorizationCodeLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromHours(1);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public async Task<McpClientRegistrationDto> RegisterClientAsync(
        McpClientRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClientName) || request.ClientName.Trim().Length > 200)
        {
            throw new ValidationException("MCP_CLIENT_NAME_INVALID", "MCP 客户端名称不能为空且不能超过 200 字。");
        }

        var redirectUris = request.RedirectUris
            .Where(uri => !string.IsNullOrWhiteSpace(uri))
            .Select(uri => uri.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (redirectUris.Count == 0 || redirectUris.Count > 20)
        {
            throw new ValidationException("MCP_REDIRECT_URI_INVALID", "MCP 客户端必须登记 1—20 个回调地址。");
        }

        foreach (var redirectUri in redirectUris)
        {
            ValidateRedirectUri(redirectUri);
        }

        var grantTypes = ParseList(request.GrantTypes, ["authorization_code", "refresh_token"]);
        if (grantTypes.Any(item => item is not ("authorization_code" or "refresh_token")))
        {
            throw new ValidationException("MCP_GRANT_TYPE_INVALID", "MCP 客户端只支持授权码和刷新令牌。");
        }

        var responseTypes = ParseList(request.ResponseTypes, ["code"]);
        if (responseTypes.Count != 1 || responseTypes[0] != "code")
        {
            throw new ValidationException("MCP_RESPONSE_TYPE_INVALID", "MCP 客户端只支持 code 响应类型。");
        }

        var requestedScopes = ParseRequestedScopes(request.Scope);
        if (requestedScopes.Count == 0)
        {
            requestedScopes = [McpScopeNames.Read];
        }

        var authMethod = string.IsNullOrWhiteSpace(request.TokenEndpointAuthMethod)
            ? "none"
            : request.TokenEndpointAuthMethod.Trim().ToLowerInvariant();
        if (authMethod is not ("none" or "client_secret_post" or "client_secret_basic"))
        {
            throw new ValidationException("MCP_CLIENT_AUTH_METHOD_INVALID", "客户端认证方式不受支持。");
        }

        var clientSecret = authMethod == "none"
            ? null
            : McpSecurity.CreateOpaqueToken("aidt_mcp_secret");
        var now = clock.UtcNow;
        var entity = new McpClientEntity
        {
            Id = Guid.NewGuid(),
            ClientId = McpSecurity.CreateClientId(),
            ClientName = request.ClientName.Trim(),
            ClientSecretHash = clientSecret is null ? null : McpSecurity.Hash(clientSecret),
            RedirectUrisJson = McpSecurity.WriteJson(redirectUris),
            GrantTypesJson = McpSecurity.WriteJson(grantTypes),
            ScopesJson = McpSecurity.WriteJson(requestedScopes),
            TokenEndpointAuthMethod = authMethod,
            IsActive = true,
            CreatedAt = now
        };
        await dbContext.McpClients.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new McpClientRegistrationDto(
            entity.ClientId,
            entity.ClientName,
            entity.TokenEndpointAuthMethod,
            redirectUris,
            grantTypes,
            requestedScopes,
            clientSecret,
            entity.CreatedAt);
    }

    public async Task<McpAuthorizationRequestDto> CreateAuthorizationRequestAsync(
        McpAuthorizationRequestCommand command,
        UserPrincipal user,
        CancellationToken cancellationToken)
    {
        user.EnsureAuthenticated();
        if (!string.Equals(command.ResponseType, "code", StringComparison.Ordinal))
        {
            throw new ValidationException("MCP_RESPONSE_TYPE_INVALID", "只支持授权码响应类型。");
        }

        if (!string.Equals(command.CodeChallengeMethod, "S256", StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(command.CodeChallenge)
            || command.CodeChallenge.Length > 256)
        {
            throw new ValidationException("MCP_PKCE_INVALID", "MCP OAuth 必须使用 PKCE S256。");
        }

        if (command.State.Length > 512)
        {
            throw new ValidationException("MCP_STATE_INVALID", "OAuth state 不能超过 512 字。");
        }

        var client = await dbContext.McpClients
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ClientId == command.ClientId && item.IsActive, cancellationToken)
            ?? throw new PermissionException("MCP_CLIENT_INVALID", "MCP 客户端不存在或已停用。");
        var redirectUris = McpSecurity.ReadScopes(client.RedirectUrisJson);
        if (!redirectUris.Contains(command.RedirectUri, StringComparer.Ordinal))
        {
            throw new ValidationException("MCP_REDIRECT_URI_INVALID", "OAuth 回调地址未在客户端登记。");
        }

        var requestedScopes = ParseRequestedScopes(command.Scope);
        if (requestedScopes.Count == 0)
        {
            requestedScopes = [McpScopeNames.Read];
        }

        McpSecurity.EnsureScopeSubset(requestedScopes, McpSecurity.ReadScopes(client.ScopesJson));
        var now = clock.UtcNow;
        var entity = new McpAuthorizationRequestEntity
        {
            Id = Guid.NewGuid(),
            ClientId = client.ClientId,
            UserId = user.UserId,
            OrganizationId = user.OrganizationId,
            RedirectUri = command.RedirectUri,
            RequestedScopesJson = McpSecurity.WriteJson(requestedScopes),
            State = command.State ?? string.Empty,
            CodeChallenge = command.CodeChallenge,
            CodeChallengeMethod = command.CodeChallengeMethod,
            ExpiresAt = now.Add(AuthorizationRequestLifetime),
            CreatedAt = now
        };
        await dbContext.McpAuthorizationRequests.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapAuthorizationRequest(entity, client.ClientName);
    }

    public async Task<McpAuthorizationRequestDto?> GetAuthorizationRequestAsync(
        Guid requestId,
        UserPrincipal user,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.McpAuthorizationRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == requestId
                    && item.UserId == user.UserId
                    && item.OrganizationId == user.OrganizationId
                    && item.ConsumedAt == null
                    && item.ExpiresAt > clock.UtcNow,
                cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var client = await dbContext.McpClients.AsNoTracking()
            .SingleOrDefaultAsync(item => item.ClientId == entity.ClientId && item.IsActive, cancellationToken);
        return client is null ? null : MapAuthorizationRequest(entity, client.ClientName);
    }

    public async Task<McpAuthorizationDecisionDto> ApproveAuthorizationAsync(
        Guid requestId,
        UserPrincipal user,
        IReadOnlyList<Guid> departmentIds,
        IReadOnlyList<string> grantedScopes,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var request = await dbContext.McpAuthorizationRequests
            .SingleOrDefaultAsync(
                item => item.Id == requestId
                    && item.UserId == user.UserId
                    && item.OrganizationId == user.OrganizationId
                    && item.ConsumedAt == null
                    && item.ExpiresAt > now,
                cancellationToken)
            ?? throw new ConflictException("MCP_AUTHORIZATION_REQUEST_EXPIRED", "浏览器授权请求不存在或已过期。");

        var selectedDepartments = departmentIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
        if (selectedDepartments.Count == 0)
        {
            throw new ValidationException("MCP_DEPARTMENT_REQUIRED", "至少选择一个 MCP 可访问部门。");
        }

        var accessibleDepartmentIds = user.IsPlatformAdmin
            ? await dbContext.Departments
                .AsNoTracking()
                .Where(item =>
                    item.OrganizationId == user.OrganizationId
                    && item.IsActive
                    && item.HierarchyType == "Department")
                .Select(item => item.Id)
                .ToListAsync(cancellationToken)
            : user.DepartmentIds.ToList();
        if (selectedDepartments.Any(id => !accessibleDepartmentIds.Contains(id)))
        {
            throw new PermissionException(ErrorCodes.DepartmentScopeDenied, "所选部门超出当前用户可授权范围。");
        }

        var requestedScopes = McpSecurity.ReadScopes(request.RequestedScopesJson);
        var selectedScopes = McpScopeNames.Parse(string.Join(' ', grantedScopes));
        McpSecurity.EnsureScopeSubset(selectedScopes, requestedScopes);

        var code = McpSecurity.CreateOpaqueToken("aidt_mcp_code");
        await dbContext.McpAuthorizationCodes.AddAsync(
            new McpAuthorizationCodeEntity
            {
                Id = Guid.NewGuid(),
                CodeHash = McpSecurity.Hash(code),
                ClientId = request.ClientId,
                UserId = request.UserId,
                OrganizationId = request.OrganizationId,
                RedirectUri = request.RedirectUri,
                GrantedScopesJson = McpSecurity.WriteJson(selectedScopes),
                DepartmentIdsJson = McpSecurity.WriteJson(selectedDepartments),
                CodeChallenge = request.CodeChallenge,
                CodeChallengeMethod = request.CodeChallengeMethod,
                ExpiresAt = now.Add(AuthorizationCodeLifetime),
                CreatedAt = now
            },
            cancellationToken);
        request.ConsumedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new McpAuthorizationDecisionDto(
            request.RedirectUri,
            request.State,
            code,
            now.Add(AuthorizationCodeLifetime));
    }

    public async Task<McpTokenSetDto?> ExchangeAuthorizationCodeAsync(
        string clientId,
        string? clientSecret,
        string code,
        string redirectUri,
        string codeVerifier,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(codeVerifier) || codeVerifier.Length is < 43 or > 128)
        {
            throw new ValidationException("MCP_PKCE_INVALID", "PKCE code_verifier 长度必须为 43—128。");
        }

        var client = await AuthenticateClientAsync(clientId, clientSecret, cancellationToken);
        var now = clock.UtcNow;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var codeHash = McpSecurity.Hash(code);
        var entity = await dbContext.McpAuthorizationCodes
            .SingleOrDefaultAsync(
                item => item.CodeHash == codeHash
                    && item.ClientId == client.ClientId
                    && item.ConsumedAt == null
                    && item.ExpiresAt > now,
                cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (!string.Equals(entity.RedirectUri, redirectUri, StringComparison.Ordinal)
            || !McpSecurity.FixedTimeEquals(
                entity.CodeChallenge,
                McpSecurity.ComputeCodeChallenge(codeVerifier)))
        {
            return null;
        }

        entity.ConsumedAt = now;
        var tokens = await IssueTokenPairAsync(
            client.ClientId,
            entity.UserId,
            entity.OrganizationId,
            McpSecurity.ReadScopes(entity.GrantedScopesJson),
            McpSecurity.ReadDepartmentIds(entity.DepartmentIdsJson),
            Guid.NewGuid(),
            parentTokenId: null,
            now,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return tokens;
    }

    public async Task<McpTokenSetDto?> RefreshAsync(
        string clientId,
        string? clientSecret,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var client = await AuthenticateClientAsync(clientId, clientSecret, cancellationToken);
        var now = clock.UtcNow;
        var hash = McpSecurity.Hash(refreshToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var existing = await dbContext.McpAccessTokens
            .SingleOrDefaultAsync(
                item => item.TokenHash == hash
                    && item.ClientId == client.ClientId
                    && item.TokenKind == "Refresh"
                    && item.RevokedAt == null
                    && item.ExpiresAt > now,
                cancellationToken);
        if (existing is null)
        {
            return null;
        }

        await dbContext.McpAccessTokens
            .Where(item => item.TokenFamilyId == existing.TokenFamilyId && item.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.RevokedAt, now),
                cancellationToken);
        var tokens = await IssueTokenPairAsync(
            existing.ClientId,
            existing.UserId,
            existing.OrganizationId,
            McpSecurity.ReadScopes(existing.GrantedScopesJson),
            McpSecurity.ReadDepartmentIds(existing.DepartmentIdsJson),
            existing.TokenFamilyId,
            existing.Id,
            now,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return tokens;
    }

    public async Task RevokeAsync(
        string clientId,
        string? clientSecret,
        string token,
        CancellationToken cancellationToken)
    {
        var client = await AuthenticateClientAsync(clientId, clientSecret, cancellationToken);
        var hash = McpSecurity.Hash(token);
        var entity = await dbContext.McpAccessTokens.SingleOrDefaultAsync(
            item => item.TokenHash == hash && item.ClientId == client.ClientId,
            cancellationToken);
        if (entity is null)
        {
            return;
        }

        var now = clock.UtcNow;
        await dbContext.McpAccessTokens
            .Where(item => item.TokenFamilyId == entity.TokenFamilyId && item.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.RevokedAt, now),
                cancellationToken);
    }

    public async Task<McpServiceTokenDto> IssueServiceTokenAsync(
        UserPrincipal issuer,
        McpServiceTokenRequest request,
        CancellationToken cancellationToken)
    {
        issuer.EnsureCan("mcp", AccessAction.Manage);
        if (request.LifetimeHours is < 1 or > 168)
        {
            throw new ValidationException("MCP_SERVICE_TOKEN_LIFETIME_INVALID", "服务令牌有效期必须在 1—168 小时。");
        }

        if (string.IsNullOrWhiteSpace(request.ClientName) || request.ClientName.Length > 200)
        {
            throw new ValidationException("MCP_SERVICE_TOKEN_NAME_INVALID", "服务令牌名称不能为空且不能超过 200 字。");
        }

        var scopes = request.Scopes
            .Select(scope => scope.Trim().ToLowerInvariant())
            .Where(scope => scope.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        McpSecurity.EnsureScopeSubset(scopes, McpScopeNames.All);
        var departmentIds = request.DepartmentIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (departmentIds.Count == 0)
        {
            throw new ValidationException("MCP_DEPARTMENT_REQUIRED", "服务令牌至少需要一个部门范围。");
        }

        if (!request.AgentProfileId.HasValue)
        {
            throw new ValidationException(
                "MCP_AGENT_PROFILE_REQUIRED",
                "服务身份令牌必须关联已发布且有效的数字员工身份。");
        }

        var profile = await dbContext.AgentProfiles.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == request.AgentProfileId.Value,
            cancellationToken);
        if (profile is null
            || !profile.IsActive
            || profile.LifecycleStatus != AgentProfileLifecycleStatus.Published.ToString()
            || !profile.CurrentPublishedVersionId.HasValue
            || profile.ServiceUserId != request.ServiceUserId)
        {
            throw new ValidationException(
                "MCP_AGENT_PROFILE_INVALID",
                "数字员工未发布、已停用或服务身份不匹配。");
        }

        var allowedCapabilities = JsonSerializer.Deserialize<string[]>(
            profile.AllowedToolsJson,
            JsonSerializerOptions.Web) ?? [];
        var profileDataScopes = JsonSerializer.Deserialize<string[]>(
            profile.DataScopesJson,
            JsonSerializerOptions.Web) ?? [];
        var organizationScope = profileDataScopes.Contains(
            $"organization:{issuer.OrganizationId:D}",
            StringComparer.OrdinalIgnoreCase);
        var allowedDepartmentIds = profileDataScopes
            .Where(scope => scope.StartsWith("department:", StringComparison.OrdinalIgnoreCase))
            .Select(scope => Guid.TryParse(scope["department:".Length..], out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToHashSet();
        if (!organizationScope
            && departmentIds.Any(id => !allowedDepartmentIds.Contains(id)))
        {
            throw new PermissionException(
                ErrorCodes.DepartmentScopeDenied,
                "服务令牌部门范围超过数字员工的数据授权范围。");
        }

        var unsupportedScopes = scopes
            .Where(scope =>
                string.Equals(scope, McpScopeNames.Manage, StringComparison.Ordinal)
                || !AgentCapabilityRegistry.IsMcpScopeSupported(scope, allowedCapabilities))
            .ToArray();
        if (unsupportedScopes.Length > 0)
        {
            throw new PermissionException(
                ErrorCodes.PermissionDenied,
                "数字员工能力白名单不支持请求的 MCP 作用域："
                + string.Join("、", unsupportedScopes));
        }

        var serviceUser = await userRepository.GetPrincipalAsync(request.ServiceUserId, cancellationToken);
        if (serviceUser is null
            || !serviceUser.IsServiceIdentity
            || serviceUser.OrganizationId != issuer.OrganizationId
            || serviceUser.Permissions.Count == 0)
        {
            throw new ValidationException(
                "MCP_SERVICE_IDENTITY_INVALID",
                "服务身份不存在、已停用、无业务权限或不属于当前组织。");
        }

        var activeDepartments = await dbContext.Departments
            .AsNoTracking()
            .Where(item =>
                item.OrganizationId == issuer.OrganizationId
                && item.IsActive
                && item.HierarchyType == "Department"
                && departmentIds.Contains(item.Id))
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        if (activeDepartments.Count != departmentIds.Count)
        {
            throw new PermissionException(ErrorCodes.DepartmentScopeDenied, "服务令牌包含无效或越权部门。");
        }

        var now = clock.UtcNow;
        var clientId = McpSecurity.CreateClientId();
        await dbContext.McpClients.AddAsync(
            new McpClientEntity
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                ClientName = request.ClientName.Trim(),
                RedirectUrisJson = "[]",
                GrantTypesJson = McpSecurity.WriteJson(ServiceGrantTypes),
                ScopesJson = McpSecurity.WriteJson(scopes),
                TokenEndpointAuthMethod = "none",
                IsActive = true,
                AgentProfileId = profile.Id,
                CreatedByUserId = issuer.UserId,
                CreatedAt = now
            },
            cancellationToken);
        var accessToken = McpSecurity.CreateOpaqueToken("aidt_mcp_service");
        var tokenId = Guid.NewGuid();
        var expiresAt = now.AddHours(request.LifetimeHours);
        await dbContext.McpAccessTokens.AddAsync(
            new McpAccessTokenEntity
            {
                Id = tokenId,
                TokenHash = McpSecurity.Hash(accessToken),
                TokenKind = "Access",
                TokenFamilyId = Guid.NewGuid(),
                ClientId = clientId,
                UserId = serviceUser.UserId,
                OrganizationId = issuer.OrganizationId,
                AgentProfileId = profile.Id,
                GrantedScopesJson = McpSecurity.WriteJson(scopes),
                DepartmentIdsJson = McpSecurity.WriteJson(departmentIds),
                ExpiresAt = expiresAt,
                CreatedAt = now
            },
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new McpServiceTokenDto(
            clientId,
            tokenId,
            accessToken,
            "Bearer",
            string.Join(' ', scopes),
            serviceUser.DisplayName,
            expiresAt);
    }

    public async Task<McpTokenValidationResult> ValidateAccessTokenAsync(
        string rawToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return new McpTokenValidationResult(null, "invalid_token", "缺少访问令牌。");
        }

        var now = clock.UtcNow;
        var hash = McpSecurity.Hash(rawToken);
        var entity = await dbContext.McpAccessTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.TokenHash == hash
                    && item.TokenKind == "Access"
                    && item.RevokedAt == null
                    && item.ExpiresAt > now,
                cancellationToken);
        if (entity is null)
        {
            return new McpTokenValidationResult(null, "invalid_token", "访问令牌无效、过期或已撤销。");
        }

        var client = await dbContext.McpClients
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ClientId == entity.ClientId && item.IsActive, cancellationToken);
        if (client is null)
        {
            return new McpTokenValidationResult(null, "invalid_token", "MCP 客户端已停用。");
        }

        var user = await userRepository.GetPrincipalAsync(entity.UserId, cancellationToken);
        if (user is null
            || user.OrganizationId != entity.OrganizationId)
        {
            return new McpTokenValidationResult(null, "invalid_token", "令牌关联身份已停用。");
        }

        var selectedDepartmentIds = McpSecurity.ReadDepartmentIds(entity.DepartmentIdsJson);
        if (selectedDepartmentIds.Count == 0)
        {
            return new McpTokenValidationResult(null, "invalid_token", "令牌没有部门范围。");
        }

        var scopes = McpSecurity.ReadScopes(entity.GrantedScopesJson);
        if (scopes.Count == 0)
        {
            return new McpTokenValidationResult(null, "insufficient_scope", "令牌没有工具作用域。");
        }

        IReadOnlyList<string>? allowedCapabilities = null;
        if (entity.AgentProfileId.HasValue)
        {
            var profile = await dbContext.AgentProfiles.AsNoTracking().SingleOrDefaultAsync(
                item => item.Id == entity.AgentProfileId.Value,
                cancellationToken);
            if (profile is null
                || !profile.IsActive
                || profile.LifecycleStatus != AgentProfileLifecycleStatus.Published.ToString()
                || !profile.CurrentPublishedVersionId.HasValue
                || profile.ServiceUserId != entity.UserId)
            {
                return new McpTokenValidationResult(
                    null,
                    "invalid_token",
                    "数字员工身份未发布、已停用或失效。");
            }

            allowedCapabilities = JsonSerializer.Deserialize<string[]>(
                profile.AllowedToolsJson,
                JsonSerializerOptions.Web) ?? [];
        }

        var sanitizedRoles = user.Roles
            .Where(role => !string.Equals(role, "platform-admin", StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);
        var sanitizedScopes = user.DataScopes
            .Where(scope =>
                string.Equals(scope.ScopeType, "organization", StringComparison.OrdinalIgnoreCase))
            .Concat(selectedDepartmentIds.Select(DataScope.Department))
            .Distinct()
            .ToList();
        if (user.IsServiceIdentity
            && sanitizedScopes.All(scope =>
                !string.Equals(scope.ScopeType, "organization", StringComparison.OrdinalIgnoreCase)))
        {
            sanitizedScopes.Insert(0, DataScope.Organization(user.OrganizationId));
        }
        var scopedUser = new UserPrincipal(
            user.UserId,
            user.OrganizationId,
            user.IsServiceIdentity,
            sanitizedRoles,
            user.Permissions,
            sanitizedScopes)
        {
            DisplayName = user.DisplayName,
            RoleAssignments = user.RoleAssignments
        };
        return new McpTokenValidationResult(
            new McpTokenIdentity(
                entity.Id,
                "Access",
                entity.ClientId,
                client.ClientName,
                entity.UserId,
                entity.OrganizationId,
                scopes,
                selectedDepartmentIds,
                scopedUser,
                entity.AgentProfileId,
                allowedCapabilities),
            null,
            null);
    }

    public async Task MarkAccessTokenUsedAsync(
        Guid tokenId,
        DateTimeOffset usedAt,
        CancellationToken cancellationToken)
    {
        var normalized = usedAt.ToUniversalTime();
        await dbContext.McpAccessTokens
            .Where(item => item.Id == tokenId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.LastUsedAt, normalized),
                cancellationToken);
        var token = await dbContext.McpAccessTokens
            .AsNoTracking()
            .Where(item => item.Id == tokenId)
            .Select(item => item.ClientId)
            .SingleOrDefaultAsync(cancellationToken);
        if (!string.IsNullOrEmpty(token))
        {
            await dbContext.McpClients
                .Where(item => item.ClientId == token)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(item => item.LastUsedAt, normalized),
                    cancellationToken);
        }
    }

    private async Task<McpClientEntity> AuthenticateClientAsync(
        string clientId,
        string? clientSecret,
        CancellationToken cancellationToken)
    {
        var client = await dbContext.McpClients
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ClientId == clientId && item.IsActive, cancellationToken)
            ?? throw new PermissionException("MCP_CLIENT_INVALID", "MCP 客户端不存在或已停用。");
        if (client.TokenEndpointAuthMethod != "none")
        {
            if (string.IsNullOrWhiteSpace(clientSecret)
                || string.IsNullOrWhiteSpace(client.ClientSecretHash)
                || !McpSecurity.FixedTimeEquals(
                    client.ClientSecretHash,
                    McpSecurity.Hash(clientSecret)))
            {
                throw new PermissionException("MCP_CLIENT_AUTH_FAILED", "MCP 客户端认证失败。");
            }
        }

        return client;
    }

    private async Task<McpTokenSetDto> IssueTokenPairAsync(
        string clientId,
        Guid userId,
        Guid organizationId,
        IReadOnlyList<string> scopes,
        IReadOnlyList<Guid> departmentIds,
        Guid tokenFamilyId,
        Guid? parentTokenId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var accessToken = McpSecurity.CreateOpaqueToken("aidt_mcp_access");
        var refreshToken = McpSecurity.CreateOpaqueToken("aidt_mcp_refresh");
        var accessExpiresAt = now.Add(AccessTokenLifetime);
        var refreshExpiresAt = now.Add(RefreshTokenLifetime);
        var scopeJson = McpSecurity.WriteJson(scopes);
        var departmentJson = McpSecurity.WriteJson(departmentIds);
        await dbContext.McpAccessTokens.AddRangeAsync(
            new[]
            {
                new McpAccessTokenEntity
                {
                    Id = Guid.NewGuid(),
                    TokenHash = McpSecurity.Hash(accessToken),
                    TokenKind = "Access",
                    TokenFamilyId = tokenFamilyId,
                    ClientId = clientId,
                    UserId = userId,
                    OrganizationId = organizationId,
                    GrantedScopesJson = scopeJson,
                    DepartmentIdsJson = departmentJson,
                    ParentTokenId = parentTokenId,
                    ExpiresAt = accessExpiresAt,
                    CreatedAt = now
                },
                new McpAccessTokenEntity
                {
                    Id = Guid.NewGuid(),
                    TokenHash = McpSecurity.Hash(refreshToken),
                    TokenKind = "Refresh",
                    TokenFamilyId = tokenFamilyId,
                    ClientId = clientId,
                    UserId = userId,
                    OrganizationId = organizationId,
                    GrantedScopesJson = scopeJson,
                    DepartmentIdsJson = departmentJson,
                    ParentTokenId = parentTokenId,
                    ExpiresAt = refreshExpiresAt,
                    CreatedAt = now
                }
            },
            cancellationToken);
        return new McpTokenSetDto(
            accessToken,
            "Bearer",
            (int)AccessTokenLifetime.TotalSeconds,
            string.Join(' ', scopes),
            refreshToken,
            refreshExpiresAt,
            userId);
    }

    private static McpAuthorizationRequestDto MapAuthorizationRequest(
        McpAuthorizationRequestEntity entity,
        string clientName) =>
        new(
            entity.Id,
            entity.ClientId,
            clientName,
            entity.RedirectUri,
            McpSecurity.ReadScopes(entity.RequestedScopesJson),
            entity.State,
            entity.ExpiresAt);

    private static IReadOnlyList<string> ParseList(
        IReadOnlyList<string>? values,
        IReadOnlyList<string> defaults) =>
        values is null || values.Count == 0
            ? defaults
            : values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

    private static IReadOnlyList<string> ParseRequestedScopes(string? value)
    {
        var raw = (value ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var parsed = McpScopeNames.Parse(value);
        if (raw.Count != parsed.Count)
        {
            throw new ValidationException("MCP_SCOPE_INVALID", "请求包含未登记的 MCP 工具作用域。");
        }

        return parsed;
    }

    private static void ValidateRedirectUri(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || uri.Fragment.Length > 0
            || !string.IsNullOrEmpty(uri.UserInfo)
            || (uri.Scheme != Uri.UriSchemeHttps
                && !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)))
        {
            throw new ValidationException(
                "MCP_REDIRECT_URI_INVALID",
                "MCP 回调地址必须为 HTTPS；本机回环允许 HTTP，且不得包含片段或用户信息。");
        }
    }
}
