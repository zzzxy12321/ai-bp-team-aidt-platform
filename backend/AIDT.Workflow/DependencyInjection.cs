using Elsa.Extensions;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AIDT.Workflow;

public static class DependencyInjection
{
    public static IServiceCollection AddAidtWorkflow(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var isConfigured = configuration.GetValue<bool>("AIDT:Workflow:IsConfigured");
        if (!isConfigured)
        {
            // Phase A keeps the workflow seam available without opening a SQL Server connection.
            return services;
        }

        var connectionString = configuration.GetConnectionString("Aidt")
            ?? @"Server=np:\\.\pipe\sql\query;Database=AIDT;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5";
        var runMigrations = configuration.GetValue<bool>("AIDT:Workflow:RunMigrationsOnStartup");

        services.AddElsa(elsa =>
        {
            elsa
                .AddWorkflow<AidtTransitionWorkflow>()
                .UseWorkflows()
                .UseWorkflowManagement(management =>
                {
                    management.UseEntityFrameworkCore(ef =>
                    {
                        ef.UseSqlServer(connectionString);
                        ef.RunMigrations = runMigrations;
                    });
                    management.UseCache();
                })
                .UseWorkflowRuntime(runtime =>
                {
                    runtime.UseEntityFrameworkCore(ef =>
                    {
                        ef.UseSqlServer(connectionString);
                        ef.RunMigrations = runMigrations;
                    });
                    runtime.UseCache();
                });
        });
        services.AddScoped<ElsaTransitionRecorder>();
        return services;
    }
}
