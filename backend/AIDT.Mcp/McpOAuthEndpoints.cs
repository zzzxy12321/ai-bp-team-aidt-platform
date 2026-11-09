using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace AIDT.Mcp;

public static class McpOAuthEndpoints
{
    public const string CookiePolicy = "AidtMcpCookieSession";
    private static readonly string[] ResponseTypes = ["code"];
    private static readonly string[] GrantTypes = ["authorization_code", "refresh_token"];
    private static readonly string[] CodeChallengeMethods = ["S256"];
    private static readonly string[] TokenEndpointAuthMethods =
        ["none", "client_secret_post", "client_secret_basic"];

    public static void MapMcpOAuthEndpoints(this WebApplication app)
    {
        app.MapGet(
            "/.well-known/oauth-authorization-server",
            (IOptions<AidtMcpOptions> options) => AuthorizationServerMetadata(options.Value))
            .AllowAnonymous();
        app.MapGet(
            "/.well-known/oauth-authorization-server/mcp/oauth",
            (IOptions<AidtMcpOptions> options) => AuthorizationServerMetadata(options.Value))
            .AllowAnonymous();

        app.MapPost("/oauth/register", RegisterClientAsync)
            .AllowAnonymous()
            .RequireRateLimiting("mcp");
        app.MapGet("/oauth/authorize", AuthorizePageAsync)
            .RequireAuthorization(CookiePolicy)
            .RequireRateLimiting("mcp");
        app.MapPost("/oauth/authorize", ApproveAuthorizationAsync)
            .RequireAuthorization(CookiePolicy)
            .RequireRateLimiting("mcp");
        app.MapPost("/oauth/token", ExchangeTokenAsync)
            .AllowAnonymous()
            .RequireRateLimiting("mcp");
        app.MapPost("/oauth/revoke", RevokeTokenAsync)
            .AllowAnonymous()
            .RequireRateLimiting("mcp");
    }

    private static IResult AuthorizationServerMetadata(AidtMcpOptions options)
    {
        var baseUrl = options.PublicBaseUrl.TrimEnd('/');
        return Results.Json(
            new
            {
                issuer = baseUrl,
                authorization_endpoint = $"{baseUrl}/oauth/authorize",
                token_endpoint = $"{baseUrl}/oauth/token",
                registration_endpoint = $"{baseUrl}/oauth/register",
                revocation_endpoint = $"{baseUrl}/oauth/revoke",
                response_types_supported = ResponseTypes,
                grant_types_supported = GrantTypes,
                code_challenge_methods_supported = CodeChallengeMethods,
                token_endpoint_auth_methods_supported = TokenEndpointAuthMethods,
                scopes_supported = McpScopeNames.All
            });
    }

    private static async Task<IResult> RegisterClientAsync(
        HttpContext context,
        IMcpOAuthService oauthService,
        IMcpAuditService auditService,
        IOptions<AidtMcpOptions> options,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = await context.Request.ReadFromJsonAsync<McpClientRegistrationRequest>(
                cancellationToken);
            if (request is null)
            {
                return OAuthError(HttpStatusCode.BadRequest, "invalid_client_metadata", "客户端注册请求为空。");
            }

            var result = await oauthService.RegisterClientAsync(request, cancellationToken);
            await auditService.RecordSecurityEventAsync(
                options.Value.SystemAuditUserId,
                "client.registered",
                "McpClient",
                result.ClientId,
                context.TraceIdentifier,
                "Succeeded",
                null,
                JsonSerializer.Serialize(new
                {
                    result.ClientName,
                    result.TokenEndpointAuthMethod,
                    result.RedirectUris,
                    result.Scopes
                }),
                cancellationToken);
            context.Response.Headers.CacheControl = "no-store";
            return Results.Json(
                new
                {
                    client_id = result.ClientId,
                    client_name = result.ClientName,
                    redirect_uris = result.RedirectUris,
                    grant_types = result.GrantTypes,
                    response_types = ResponseTypes,
                    token_endpoint_auth_method = result.TokenEndpointAuthMethod,
                    client_secret = result.ClientSecret,
                    client_id_issued_at = result.CreatedAt.ToUnixTimeSeconds(),
                    scope = string.Join(' ', result.Scopes)
                },
                statusCode: StatusCodes.Status201Created);
        }
        catch (AidtDomainException exception)
        {
            await AuditFailureAsync(
                auditService,
                options.Value.SystemAuditUserId,
                "client.registration_failed",
                "McpClient",
                context.TraceIdentifier,
                exception.ErrorCode,
                cancellationToken);
            return OAuthError(HttpStatusCode.BadRequest, "invalid_client_metadata", exception.Message);
        }
    }

    private static async Task<IResult> AuthorizePageAsync(
        HttpContext context,
        IMcpCookieSessionAccessor cookieSessionAccessor,
        IMcpOAuthService oauthService,
        IEmployeeWorkspaceQueryService workspaceQueryService,
        CancellationToken cancellationToken)
    {
        var user = cookieSessionAccessor.GetRequiredUser();
        try
        {
            var query = context.Request.Query;
            var command = new McpAuthorizationRequestCommand(
                Required(query["client_id"]),
                Required(query["redirect_uri"]),
                Required(query["response_type"]),
                Required(query["code_challenge"]),
                string.IsNullOrWhiteSpace(query["code_challenge_method"])
                    ? "S256"
                    : query["code_challenge_method"].ToString(),
                query["state"].ToString(),
                query["scope"].ToString());
            var authorization = await oauthService.CreateAuthorizationRequestAsync(
                command,
                user,
                cancellationToken);
            var departments = await workspaceQueryService.ListDepartmentsAsync(user, cancellationToken);
            var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
            var proof = antiforgery.GetAndStoreTokens(context).RequestToken
                ?? throw new InvalidOperationException("Antiforgery request token is missing.");
            return Results.Content(
                McpConsentPage.Render(user, authorization, departments, proof),
                "text/html; charset=utf-8");
        }
        catch (AidtDomainException exception)
        {
            return Results.Content(
                McpConsentPage.RenderError(exception.ErrorCode, exception.Message),
                "text/html; charset=utf-8",
                Encoding.UTF8,
                StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> ApproveAuthorizationAsync(
        HttpContext context,
        IMcpCookieSessionAccessor cookieSessionAccessor,
        IMcpOAuthService oauthService,
        IMcpAuditService auditService,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            return OAuthError(HttpStatusCode.BadRequest, "invalid_request", "授权确认表单已过期，请重新打开授权链接。");
        }

        var user = cookieSessionAccessor.GetRequiredUser();
        var form = await context.Request.ReadFormAsync(cancellationToken);
        if (!Guid.TryParse(form["request_id"], out var requestId))
        {
            return OAuthError(HttpStatusCode.BadRequest, "invalid_request", "缺少授权请求 ID。");
        }

        var departmentIds = form["departmentId"]
            .Select(value => Guid.TryParse(value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
        var scopes = form["scope"].Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!).ToList();
        try
        {
            var decision = await oauthService.ApproveAuthorizationAsync(
                requestId,
                user,
                departmentIds,
                scopes,
                cancellationToken);
            await auditService.RecordSecurityEventAsync(
                user.UserId,
                "authorization.approved",
                "McpAuthorizationCode",
                requestId.ToString("D"),
                context.TraceIdentifier,
                "Succeeded",
                null,
                JsonSerializer.Serialize(new { departmentIds, scopes }),
                cancellationToken);
            var redirect = QueryHelpers.AddQueryString(
                decision.RedirectUri,
                new Dictionary<string, string?>
                {
                    ["code"] = decision.Code,
                    ["state"] = decision.State
                });
            return Results.Redirect(redirect);
        }
        catch (AidtDomainException exception)
        {
            await auditService.RecordSecurityEventAsync(
                user.UserId,
                "authorization.denied",
                "McpAuthorizationRequest",
                requestId.ToString("D"),
                context.TraceIdentifier,
                "Denied",
                exception.ErrorCode,
                null,
                cancellationToken);
            return Results.Content(
                McpConsentPage.RenderError(exception.ErrorCode, exception.Message),
                "text/html; charset=utf-8",
                Encoding.UTF8,
                StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> ExchangeTokenAsync(
        HttpContext context,
        IMcpOAuthService oauthService,
        IMcpAuditService auditService,
        IOptions<AidtMcpOptions> options,
        CancellationToken cancellationToken)
    {
        var form = await context.Request.ReadFormAsync(cancellationToken);
        var credentials = ReadClientCredentials(context, form);
        if (credentials is null)
        {
            return OAuthError(HttpStatusCode.Unauthorized, "invalid_client", "客户端认证信息格式错误。");
        }

        try
        {
            var grantType = form["grant_type"].ToString();
            var tokens = grantType switch
            {
                "authorization_code" => await oauthService.ExchangeAuthorizationCodeAsync(
                    credentials.ClientId,
                    credentials.ClientSecret,
                    form["code"].ToString(),
                    form["redirect_uri"].ToString(),
                    form["code_verifier"].ToString(),
                    cancellationToken),
                "refresh_token" => await oauthService.RefreshAsync(
                    credentials.ClientId,
                    credentials.ClientSecret,
                    form["refresh_token"].ToString(),
                    cancellationToken),
                _ => throw new ValidationException("MCP_GRANT_TYPE_INVALID", "不支持的授权类型。")
            };
            if (tokens is null)
            {
                return OAuthError(
                    HttpStatusCode.BadRequest,
                    "invalid_grant",
                    "授权码或刷新令牌无效、过期或已使用。");
            }

            await auditService.RecordSecurityEventAsync(
                tokens.UserId,
                "token.issued",
                "McpAccessToken",
                credentials.ClientId,
                context.TraceIdentifier,
                "Succeeded",
                grantType,
                JsonSerializer.Serialize(new { tokens.Scope, tokens.ExpiresIn }),
                cancellationToken);
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.Pragma = "no-cache";
            return Results.Json(
                new
                {
                    access_token = tokens.AccessToken,
                    token_type = tokens.TokenType,
                    expires_in = tokens.ExpiresIn,
                    scope = tokens.Scope,
                    refresh_token = tokens.RefreshToken,
                    refresh_token_expires_in =
                        Math.Max(0, (int)(tokens.RefreshTokenExpiresAt - DateTimeOffset.UtcNow).TotalSeconds)
                });
        }
        catch (AidtDomainException exception)
        {
            await AuditFailureAsync(
                auditService,
                options.Value.SystemAuditUserId,
                "token.issue_failed",
                "McpAccessToken",
                context.TraceIdentifier,
                exception.ErrorCode,
                cancellationToken);
            var status = exception is PermissionException
                ? HttpStatusCode.Unauthorized
                : HttpStatusCode.BadRequest;
            return OAuthError(
                status,
                exception is PermissionException ? "invalid_client" : "invalid_grant",
                exception.Message);
        }
    }

    private static async Task<IResult> RevokeTokenAsync(
        HttpContext context,
        IMcpOAuthService oauthService,
        IMcpAuditService auditService,
        IOptions<AidtMcpOptions> options,
        CancellationToken cancellationToken)
    {
        var form = await context.Request.ReadFormAsync(cancellationToken);
        var credentials = ReadClientCredentials(context, form);
        if (credentials is null)
        {
            return OAuthError(HttpStatusCode.Unauthorized, "invalid_client", "客户端认证信息格式错误。");
        }

        try
        {
            await oauthService.RevokeAsync(
                credentials.ClientId,
                credentials.ClientSecret,
                form["token"].ToString(),
                cancellationToken);
            await auditService.RecordSecurityEventAsync(
                options.Value.SystemAuditUserId,
                "token.revoked_by_client",
                "McpAccessToken",
                credentials.ClientId,
                context.TraceIdentifier,
                "Succeeded",
                null,
                null,
                cancellationToken);
            return Results.Ok();
        }
        catch (AidtDomainException exception)
        {
            return OAuthError(
                HttpStatusCode.Unauthorized,
                "invalid_client",
                exception.Message);
        }
    }

    private static ClientCredentials? ReadClientCredentials(HttpContext context, IFormCollection form)
    {
        if (AuthenticationHeaderValue.TryParse(context.Request.Headers.Authorization, out var header)
            && string.Equals(header.Scheme, "Basic", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(header.Parameter))
        {
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header.Parameter));
                var separator = decoded.IndexOf(':');
                return separator <= 0
                    ? null
                    : new ClientCredentials(
                        Uri.UnescapeDataString(decoded[..separator]),
                        Uri.UnescapeDataString(decoded[(separator + 1)..]));
            }
            catch (FormatException)
            {
                return null;
            }
        }

        var clientId = form["client_id"].ToString();
        return string.IsNullOrWhiteSpace(clientId)
            ? null
            : new ClientCredentials(clientId, form["client_secret"].ToString());
    }

    private static async Task AuditFailureAsync(
        IMcpAuditService auditService,
        Guid actorId,
        string action,
        string entityType,
        string traceId,
        string code,
        CancellationToken cancellationToken)
    {
        await auditService.RecordSecurityEventAsync(
            actorId,
            action,
            entityType,
            "unknown",
            traceId,
            "Denied",
            code,
            null,
            cancellationToken);
    }

    private static string Required(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ValidationException("MCP_OAUTH_PARAMETER_REQUIRED", "OAuth 授权参数不完整。")
            : value;

    private static IResult OAuthError(HttpStatusCode status, string code, string description) =>
        Results.Json(
            new { error = code, error_description = description },
            statusCode: (int)status);

    private sealed record ClientCredentials(string ClientId, string ClientSecret);
}

internal static class McpConsentPage
{
    private static readonly IReadOnlyDictionary<string, string> ScopeLabels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [McpScopeNames.Read] = "读取项目、事件、任务、流程、导航和审计",
            [McpScopeNames.Write] = "创建项目/事项、分析事件和更新受控任务",
            [McpScopeNames.Files] = "读取文件元数据并受控上传/下载文件正文",
            [McpScopeNames.Approve] = "执行流程审批、转交和事件确认（仍需二次真人确认）",
            [McpScopeNames.Manage] = "读取 MCP 管理目录、客户端、令牌和调用审计"
        };

    public static string Render(
        UserPrincipal user,
        McpAuthorizationRequestDto request,
        IReadOnlyList<DepartmentDto> departments,
        string antiforgeryToken)
    {
        var encoder = HtmlEncoder.Default;
        var departmentRows = string.Join(
            Environment.NewLine,
            departments.Select((department, index) =>
            {
                var selected = department.IsPrimary || (departments.All(item => !item.IsPrimary) && index == 0);
                return $"""
                    <label class="check-row">
                      <input type="checkbox" name="departmentId" value="{department.Id}" {(selected ? "checked" : string.Empty)} />
                      <span><strong>{encoder.Encode(department.Name)}</strong><small>{encoder.Encode(department.Code)} · {encoder.Encode(department.Id)}</small></span>
                    </label>
                    """;
            }));
        var scopeRows = string.Join(
            Environment.NewLine,
            request.RequestedScopes.Select(scope => $"""
                <label class="check-row">
                  <input type="checkbox" name="scope" value="{encoder.Encode(scope)}" checked />
                  <span><strong>{encoder.Encode(scope)}</strong><small>{encoder.Encode(ScopeLabels.GetValueOrDefault(scope, scope))}</small></span>
                </label>
                """));
        return $$"""
            <!doctype html>
            <html lang="zh-CN">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>AIDT MCP 浏览器授权</title>
              <style>
                :root { color-scheme: light; font-family: "Microsoft YaHei", "Segoe UI", sans-serif; color:#18342a; background:#eef5f0; }
                * { box-sizing:border-box; }
                body { margin:0; min-height:100vh; padding:28px; background:radial-gradient(circle at 12% 8%,#cce8d8 0,transparent 26rem),linear-gradient(145deg,#f7fbf8,#dcebe2); }
                main { width:min(820px,100%); margin:0 auto; border:1px solid #d6e3dc; border-radius:26px; background:#fff; box-shadow:0 24px 72px rgb(17 58 42 / 13%); overflow:hidden; }
                header { padding:28px 32px 22px; background:linear-gradient(145deg,#153f30,#0d2b21); color:#f6fbf8; }
                header small { color:#a8c5b7; letter-spacing:.14em; font-weight:800; }
                h1 { margin:8px 0 10px; font-size:clamp(1.65rem,4vw,2.35rem); }
                header p { margin:0; color:#c6d9cf; line-height:1.7; }
                form { padding:26px 32px 32px; }
                .callout { border:1px solid #efcf91; border-radius:14px; background:#fff8e8; padding:14px 16px; line-height:1.7; }
                .meta { display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); gap:12px; margin:20px 0; }
                .meta div { border-radius:14px; background:#f3f7f5; padding:13px; }
                .meta small,.check-row small { display:block; color:#587066; margin-top:4px; overflow-wrap:anywhere; }
                fieldset { margin:18px 0; border:1px solid #d9e4de; border-radius:16px; padding:16px; }
                legend { padding:0 8px; font-weight:800; }
                .check-row { display:flex; gap:10px; align-items:flex-start; padding:10px 8px; border-radius:10px; }
                .check-row:hover { background:#f3f8f5; }
                .check-row input { margin-top:4px; }
                .actions { display:flex; justify-content:flex-end; gap:10px; margin-top:22px; }
                button { border:0; border-radius:12px; padding:12px 22px; font:inherit; font-weight:800; cursor:pointer; }
                .allow { background:#1e664d; color:#fff; }
                .deny { background:#eef3f0; color:#344b42; }
                @media (max-width:640px){ body{padding:12px} header,form{padding:22px 18px}.meta{grid-template-columns:1fr}.actions{flex-direction:column-reverse}.actions button{width:100%} }
              </style>
            </head>
            <body>
              <main>
                <header>
                  <small>AIDT · MCP AUTHORIZATION</small>
                  <h1>授权智能体连接 AIDT</h1>
                  <p>你正在为 {{encoder.Encode(user.DisplayName)}} 签发短期、可撤销且按部门收敛的 MCP 令牌。</p>
                </header>
                <form method="post" action="/oauth/authorize">
                  <input type="hidden" name="__RequestVerificationToken" value="{{encoder.Encode(antiforgeryToken)}}" />
                  <input type="hidden" name="request_id" value="{{request.Id:D}}" />
                  <div class="callout">
                    客户端只会继承你当前已有的 AIDT 权限。此处选择的部门范围不会因平台管理员身份而自动扩大。
                    高风险审批和敏感文件仍需在 AIDT 页面再次真人确认。
                  </div>
                  <div class="meta">
                    <div><small>客户端</small><strong>{{encoder.Encode(request.ClientName)}}</strong><small>{{encoder.Encode(request.ClientId)}}</small></div>
                    <div><small>回调地址</small><strong>受控回调</strong><small>{{encoder.Encode(request.RedirectUri)}}</small></div>
                  </div>
                  <fieldset>
                    <legend>可访问部门</legend>
                    {{departmentRows}}
                  </fieldset>
                  <fieldset>
                    <legend>工具作用域</legend>
                    {{scopeRows}}
                  </fieldset>
                  <div class="actions">
                    <button class="deny" type="button" onclick="history.back()">取消</button>
                    <button class="allow" type="submit">确认授权</button>
                  </div>
                </form>
              </main>
            </body>
            </html>
            """;
    }

    public static string RenderError(string code, string message)
    {
        var encoder = HtmlEncoder.Default;
        return $$"""
            <!doctype html>
            <html lang="zh-CN"><head><meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1" /><title>AIDT MCP 授权失败</title></head>
            <body style="font-family:Microsoft YaHei,sans-serif;background:#eef5f0;padding:32px;color:#18342a">
              <main style="max-width:680px;margin:auto;background:white;border:1px solid #d6e3dc;border-radius:22px;padding:28px">
                <p style="font-weight:800;color:#8b2f2f">{{encoder.Encode(code)}}</p>
                <h1>无法完成 MCP 授权</h1>
                <p style="line-height:1.8">{{encoder.Encode(message)}}</p>
                <p><a href="/login">返回 AIDT 登录</a></p>
              </main>
            </body></html>
            """;
    }
}
