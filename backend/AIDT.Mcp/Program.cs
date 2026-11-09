using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AIDT.Application.Abstractions;
using AIDT.Infrastructure;
using AIDT.Infrastructure.Configuration;
using AIDT.Infrastructure.Persistence;
using AIDT.Mcp;
using AIDT.Workflow;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;
using ModelContextProtocol.Protocol;
using Microsoft.EntityFrameworkCore;

if (args.Contains("--stdio", StringComparer.OrdinalIgnoreCase))
{
    var hostBuilder = Host.CreateApplicationBuilder(Array.Empty<string>());
    ConfigureAidtServices(hostBuilder.Services, hostBuilder.Configuration, httpMode: false);
    hostBuilder.Logging.ClearProviders();
    hostBuilder.Logging.AddConsole(options =>
    {
        options.LogToStandardErrorThreshold = LogLevel.Trace;
    });
    hostBuilder.Services
        .AddMcpServer(options =>
        {
            options.ServerInfo = new Implementation { Name = "AIDT", Version = "1.6.0" };
            options.ServerInstructions =
                "AIDT tools inherit the connected user's permissions and selected department scope. "
                + "Use read tools first. High-risk tools return HUMAN_CONFIRMATION_REQUIRED until the human user confirms in AIDT.";
        })
        .WithStdioServerTransport()
        .WithTools<AidtBusinessTools>()
        .WithResources<AidtResources>()
        .WithAidtPrimitiveFilters();
    await hostBuilder.Build().RunAsync();
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    var maxRequestBytes = builder.Configuration.GetValue<long?>("AIDT:Mcp:MaxRequestBytes")
        ?? 10 * 1024 * 1024;
    options.Limits.MaxRequestBodySize = maxRequestBytes;
});
ConfigureAidtServices(builder.Services, builder.Configuration, httpMode: true);
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddCors(options =>
{
    var origins = builder.Configuration
        .GetSection("AIDT:Mcp:AllowedOrigins")
        .Get<string[]>() ?? [];
    if (origins.Length > 0)
    {
        options.AddPolicy("McpBrowserOrigins", policy =>
            policy.WithOrigins(origins)
                .WithMethods("GET", "POST", "DELETE")
                .WithHeaders("Authorization", "Content-Type", "MCP-Protocol-Version", "Mcp-Session-Id")
                .WithExposedHeaders("Mcp-Session-Id", "WWW-Authenticate"));
    }
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "AIDT.Mcp.CSRF";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});
builder.Services.AddAidtSharedDataProtection(builder.Configuration);
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "Bearer";
        options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
    })
    .AddScheme<AuthenticationSchemeOptions, McpTokenAuthenticationHandler>("Bearer", _ => { })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "AIDT.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Events.OnRedirectToLogin = context =>
        {
            var returnUrl = context.Request.Path + context.Request.QueryString;
            context.Response.Redirect($"/login?redirect={Uri.EscapeDataString(returnUrl)}");
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    })
    .AddMcp(options =>
    {
        var publicBaseUrl = builder.Configuration["AIDT:Mcp:PublicBaseUrl"]
            ?? "http://127.0.0.1:8088";
        options.ResourceMetadata = new ProtectedResourceMetadata
        {
            Resource = $"{publicBaseUrl.TrimEnd('/')}/mcp",
            AuthorizationServers = { publicBaseUrl.TrimEnd('/') },
            ScopesSupported = [.. AIDT.Application.Contracts.McpScopeNames.All]
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.AddPolicy(
        McpOAuthEndpoints.CookiePolicy,
        policy => policy
            .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser());
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("mcp", context =>
    {
        var clientId = context.User.FindFirstValue("mcp:client_id")
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";
        var limit = context.RequestServices
            .GetRequiredService<IOptions<AidtMcpOptions>>()
            .Value
            .MaxRequestsPerMinute;
        return RateLimitPartition.GetFixedWindowLimiter(
            clientId,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = Math.Max(1, limit),
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation { Name = "AIDT", Version = "1.6.0" };
        options.ServerInstructions =
            "AIDT tools inherit the connected user's permissions and selected department scope. "
            + "Use read tools first. High-risk tools return HUMAN_CONFIRMATION_REQUIRED until the human user confirms in AIDT.";
    })
    .WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless)
    .WithTools<AidtBusinessTools>()
    .WithResources<AidtResources>()
    .WithAidtPrimitiveFilters();

var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.Use(async (context, next) =>
{
    var incoming = context.Request.Headers["X-Trace-Id"].FirstOrDefault();
    if (!string.IsNullOrWhiteSpace(incoming)
        && incoming.Length <= 128
        && incoming.All(character =>
            char.IsLetterOrDigit(character) || character is '-' or '_' or '.'))
    {
        context.TraceIdentifier = incoming;
    }

    context.Response.Headers["X-Trace-Id"] = context.TraceIdentifier;
    await next();
});
if (app.Configuration.GetSection("AIDT:Mcp:AllowedOrigins").Exists())
{
    app.UseCors("McpBrowserOrigins");
}

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapGet("/health/live", () => Results.Json(new
{
    status = "healthy",
    service = "AIDT.Mcp",
    mcp = "StreamableHttp",
    checkedAt = DateTimeOffset.UtcNow
})).AllowAnonymous();
app.MapGet("/mcp/health/live", () => Results.Json(new
{
    status = "healthy",
    service = "AIDT.Mcp",
    transport = "StreamableHttp",
    checkedAt = DateTimeOffset.UtcNow
})).AllowAnonymous();
app.MapGet("/health/ready", CheckMcpReadinessAsync).AllowAnonymous();
app.MapGet("/mcp/health/ready", CheckMcpReadinessAsync).AllowAnonymous();

app.MapMcpOAuthEndpoints();
app.MapMcp("/mcp")
    .RequireAuthorization()
    .RequireRateLimiting("mcp");
await app.RunAsync();

static void ConfigureAidtServices(
    IServiceCollection services,
    IConfiguration configuration,
    bool httpMode)
{
    services
        .AddOptions<AidtMcpOptions>()
        .Bind(configuration.GetSection(AidtMcpOptions.SectionName))
        .Validate(
            options => Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out _),
            "AIDT:Mcp:PublicBaseUrl 必须是绝对 URL。")
        .Validate(
            options => options.MaxRequestsPerMinute is >= 1 and <= 10_000,
            "AIDT:Mcp:MaxRequestsPerMinute 必须在 1—10000 之间。")
        .Validate(
            options => options.MaxRequestBytes is >= 1024 and <= 524_288_000,
            "AIDT:Mcp:MaxRequestBytes 必须在 1 KiB 到 500 MiB 之间。")
        .ValidateOnStart();
    services.AddSingleton(provider => provider.GetRequiredService<IOptions<AidtMcpOptions>>().Value);
    services.AddHttpContextAccessor();
    services.AddAidtInfrastructure(configuration);
    services.AddSingleton<McpAmbientIdentity>();
    services.AddScoped<IMcpCallContextAccessor, McpCallContextAccessor>();
    services.AddScoped<ICurrentUserAccessor, McpCurrentUserAccessor>();
    services.AddScoped<IMcpCookieSessionAccessor, McpCookieSessionAccessor>();
    services.AddScoped<IWorkflowEngine, WorkflowEngineAdapter>();
    services.AddScoped<AIDT.Application.UseCases.ProcessWorkflowTaskHandler>();
    services.AddScoped<McpToolRuntime>();
    services.AddScoped<AidtBusinessTools>();
    services.AddScoped<AidtResources>();
    _ = httpMode;
}

static async Task<IResult> CheckMcpReadinessAsync(
    IPlatformCapabilityService capabilities,
    AidtDbContext dbContext,
    CancellationToken cancellationToken)
{
    var status = capabilities.GetCapabilities();
    var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
    var schemaReady = canConnect
        && await dbContext.Database
            .SqlQueryRaw<int>(
                "SELECT CASE WHEN OBJECT_ID(N'Mcp.AccessToken', N'U') IS NOT NULL THEN 1 ELSE 0 END AS [Value]")
            .SingleAsync(cancellationToken) == 1;
    var ready = canConnect && schemaReady;
    return Results.Json(
        new
        {
            status = ready ? "ready" : "blocked",
            databaseMigrationState = status.DatabaseMigrationState,
            mcp = ready ? "ready" : "migration_or_database_missing",
            checkedAt = DateTimeOffset.UtcNow
        },
        statusCode: ready ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);
}

public partial class Program;
