using AIDT.Api.Contracts;
using AIDT.Infrastructure.Configuration;

namespace AIDT.Api.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapAidtHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/live", (HttpContext context) =>
            Results.Ok(new
            {
                status = "healthy",
                service = "AIDT.Api",
                projectState = "PHASE_A",
                traceId = context.TraceIdentifier,
                checkedAt = DateTimeOffset.UtcNow
            })).AllowAnonymous();

        endpoints.MapGet("/health/ready", (HttpContext context, AidtRuntimeOptions options) =>
        {
            var ready = options.Database.IsReady;
            var payload = new
            {
                status = ready ? "ready" : "blocked",
                database = new
                {
                    provider = options.Database.Provider,
                    isReady = options.Database.IsReady,
                    migrationState = options.Database.MigrationState
                },
                workflow = new
                {
                    provider = options.Workflow.Provider,
                    isConfigured = options.Workflow.IsConfigured
                },
                externalSystems = new
                {
                    mode = options.ExternalSystems.Mode,
                    allowWrites = options.ExternalSystems.AllowWrites
                },
                traceId = context.TraceIdentifier
            };

            return ready
                ? Results.Ok(payload)
                : Results.Json(payload, statusCode: StatusCodes.Status503ServiceUnavailable);
        }).AllowAnonymous();

        return endpoints;
    }
}
