using System.Text.Json.Serialization;
using AIDT.Api.Configuration;
using AIDT.Api.Contracts;
using AIDT.Api.Health;
using AIDT.Api.Identity;
using AIDT.Api.Middleware;
using AIDT.Application.Abstractions;
using AIDT.Application.UseCases;
using AIDT.Infrastructure;
using AIDT.Infrastructure.Configuration;
using AIDT.Workflow;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<AidtRuntimeOptions>()
    .Bind(builder.Configuration.GetSection(AidtRuntimeOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<AidtRuntimeOptions>, RuntimeOptionsValidation>();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "AIDT.CSRF";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});
builder.Services.AddAidtSharedDataProtection(builder.Configuration);
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "AIDT.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Events.OnRedirectToLogin = context => WriteAuthFailureAsync(
            context.HttpContext,
            StatusCodes.Status401Unauthorized,
            "AUTHENTICATION_REQUIRED",
            "需要登录后继续。");
        options.Events.OnRedirectToAccessDenied = context => WriteAuthFailureAsync(
            context.HttpContext,
            StatusCodes.Status403Forbidden,
            "PERMISSION_DENIED",
            "当前身份没有访问权限。");
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddAidtInfrastructure(builder.Configuration);
builder.Services.AddAidtWorkflow(builder.Configuration);
builder.Services.AddScoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>();
builder.Services.AddScoped<IWorkflowEngine, WorkflowEngineAdapter>();
builder.Services.AddScoped<CreateBusinessObjectHandler>();
builder.Services.AddScoped<UpdateBusinessObjectHandler>();
builder.Services.AddScoped<ProcessWorkflowTaskHandler>();
builder.Services.AddScoped<ConfirmAgentRunHandler>();
builder.Services.AddScoped<StartWorkflowInstanceHandler>();
builder.Services.AddScoped<ConfirmInboxEventAndStartWorkflowHandler>();
builder.Services.AddScoped<StartAgentRunHandler>();
builder.Services.AddScoped<StopAgentRunHandler>();

var app = builder.Build();

app.UseMiddleware<TraceIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.UseMiddleware<CsrfProtectionMiddleware>();

app.MapGet("/", () => Results.Redirect("/health/live")).AllowAnonymous();
app.MapAidtHealthEndpoints();
app.MapGet("/openapi/v1.json", () =>
{
    var path = Path.Combine(AppContext.BaseDirectory, "docs", "contracts", "openapi", "v1.json");
    return File.Exists(path)
        ? Results.File(path, "application/json; charset=utf-8")
        : Results.Problem("OpenAPI contract is missing from the deployment.", statusCode: 500);
}).AllowAnonymous();
app.MapControllers();

app.Run();

static Task WriteAuthFailureAsync(HttpContext context, int statusCode, string code, string message)
{
    context.Response.StatusCode = statusCode;
    context.Response.ContentType = "application/json; charset=utf-8";
    return context.Response.WriteAsJsonAsync(
        ApiResponse.Error(code, message, context.TraceIdentifier),
        cancellationToken: context.RequestAborted);
}

public partial class Program;
