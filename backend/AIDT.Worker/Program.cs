using AIDT.Infrastructure;
using AIDT.Application.Abstractions;
using AIDT.Infrastructure.Configuration;
using AIDT.Infrastructure.Persistence;
using AIDT.Worker.Jobs;
using AIDT.Workflow;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<AidtRuntimeOptions>()
    .Bind(builder.Configuration.GetSection(AidtRuntimeOptions.SectionName))
    .Validate(
        options => options.Database.Provider == "Microsoft.EntityFrameworkCore.SqlServer"
            && !options.ExternalSystems.AllowWrites,
        "Worker 启动配置违反冻结技术栈或断开模式边界。")
    .ValidateOnStart();

builder.Services.AddAidtInfrastructure(builder.Configuration);
builder.Services.AddAidtWorkflow(builder.Configuration);
builder.Services.AddScoped<IWorkflowEngine, WorkflowEngineAdapter>();

var enableBackgroundProcessing = builder.Configuration.GetValue<bool>(
    "AIDT:Worker:EnableBackgroundProcessing");
var prepareHangfireSchema = builder.Configuration.GetValue<bool>("AIDT:Hangfire:PrepareSchema");
if (enableBackgroundProcessing)
{
    var connectionString = builder.Configuration.GetConnectionString("Aidt")
        ?? @"Server=np:\\.\pipe\sql\query;Database=AIDT;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5";
    builder.Services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(
            connectionString,
            new SqlServerStorageOptions
            {
                PrepareSchemaIfNecessary = prepareHangfireSchema,
                QueuePollInterval = TimeSpan.FromSeconds(2),
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(2),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                UseRecommendedIsolationLevel = true
            }));
    builder.Services.AddHangfireServer(options => options.WorkerCount = 2);

    var redisConnection = builder.Configuration.GetValue<string>("AIDT:Cache:ConnectionString")
        ?? "127.0.0.1:6380,abortConnect=false,connectTimeout=1000,connectRetry=1";
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        ConnectionMultiplexer.Connect(redisConnection));
    builder.Services.AddHostedService<OutboxProcessingService>();
    builder.Services.AddHostedService<SlaProcessingService>();
    builder.Services.AddScoped<WorkerHeartbeatJob>();
}

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new
{
    status = "healthy",
    service = "AIDT.Worker",
    mode = enableBackgroundProcessing ? "hangfire_outbox_worker" : "phase_a_skeleton",
    checkedAt = DateTimeOffset.UtcNow
})).AllowAnonymous();

if (enableBackgroundProcessing)
{
    app.MapGet("/health/ready", async (
        IOptions<AidtRuntimeOptions> options,
        AidtDbContext dbContext,
        IConnectionMultiplexer redis,
        CancellationToken cancellationToken) =>
    {
        var settings = options.Value;
        var databaseReady = settings.Database.IsReady;
        var redisReady = false;
        var pendingOutbox = -1;
        try
        {
            redisReady = (await redis.GetDatabase().PingAsync()).TotalMilliseconds >= 0;
            pendingOutbox = await dbContext.OutboxEvents.CountAsync(
                item => item.Status == "Pending",
                cancellationToken);
        }
        catch
        {
            // Readiness reports the dependency state without throwing.
        }

        var ready = databaseReady && redisReady;
        return Results.Json(
            new
            {
                status = ready ? "ready" : "degraded",
                database = settings.Database.MigrationState,
                redis = redisReady ? "available" : "unavailable",
                pendingOutbox,
                backgroundExecution = true
            },
            statusCode: ready ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);
    }).AllowAnonymous();
}
else
{
    app.MapGet("/health/ready", (IOptions<AidtRuntimeOptions> options) =>
    {
        var settings = options.Value;
        return Results.Json(
            new
            {
                status = "blocked",
                projectState = settings.ProjectState,
                database = settings.Database.MigrationState,
                redis = "not_checked",
                pendingOutbox = -1,
                backgroundExecution = false
            },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }).AllowAnonymous();
}

if (enableBackgroundProcessing && prepareHangfireSchema)
{
    using var scope = app.Services.CreateScope();
    var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobs.AddOrUpdate<WorkerHeartbeatJob>(
        "aidt-worker-heartbeat",
        job => job.ExecuteAsync(),
        Cron.MinuteInterval(1),
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });
}

app.Run();
