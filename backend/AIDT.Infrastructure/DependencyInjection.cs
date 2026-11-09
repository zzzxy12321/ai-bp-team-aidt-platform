using AIDT.Application.Abstractions;
using AIDT.Application.Mcp;
using AIDT.Application.UseCases;
using AIDT.Infrastructure.Configuration;
using AIDT.Infrastructure.Persistence;
using AIDT.Infrastructure.Persistence.Repositories;
using AIDT.Infrastructure.Queries;
using AIDT.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AIDT.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAidtInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var runtimeOptions = configuration
            .GetSection(AidtRuntimeOptions.SectionName)
            .Get<AidtRuntimeOptions>() ?? new AidtRuntimeOptions();
        services.AddSingleton(runtimeOptions);
        services.AddSingleton(runtimeOptions.Data);
        services.AddSingleton(runtimeOptions.BusinessTime);
        services.AddSingleton(runtimeOptions.FileStorage);
        services.AddSingleton(new AIDT.Application.Documents.FileContentValidator(runtimeOptions.FileStorage.MaxSizeBytes));
        services.AddSingleton<IFileStorage, AIDT.Infrastructure.Storage.LocalFileStorage>();
        services.AddHttpClient("AiGateway", client =>
        {
            client.BaseAddress = new Uri(
                configuration.GetValue<string>("AIDT:AiGateway:BaseUrl")
                ?? "http://127.0.0.1:5100");
            client.Timeout = TimeSpan.FromSeconds(
                configuration.GetValue<int?>("AIDT:AiGateway:TimeoutSeconds") ?? 10);
        });
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPlatformCapabilityService, PlatformCapabilityService>();
        services.AddSingleton<IPersistenceGate, PersistenceGate>();
        services.AddSingleton<IMcpToolCatalog, McpToolCatalog>();
        services.AddScoped<ICurrentUserAccessor, UnavailableCurrentUserAccessor>();
        services.AddScoped<CreateBusinessObjectHandler>();
        services.AddScoped<StartWorkflowInstanceHandler>();
        services.AddScoped<StartAgentRunHandler>();

        var connectionString = configuration.GetConnectionString("Aidt")
            ?? @"Server=np:\\.\pipe\sql\query;Database=AIDT;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5";
        services.AddDbContext<AidtDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sqlServer => sqlServer.MigrationsAssembly(typeof(AidtDbContext).Assembly.FullName)));

        if (!runtimeOptions.Database.IsReady)
        {
            services.AddScoped<IObjectTypeRepository, NotReadyObjectTypeRepository>();
            services.AddScoped<IObjectModelService, NotReadyObjectModelService>();
            services.AddScoped<IBusinessObjectRepository, NotReadyBusinessObjectRepository>();
            services.AddScoped<IWorkflowRepository, NotReadyWorkflowRepository>();
            services.AddScoped<IAuditRepository, NotReadyAuditRepository>();
            services.AddScoped<IOutboxRepository, NotReadyOutboxRepository>();
            services.AddScoped<IAgentRepository, NotReadyAgentRepository>();
            services.AddScoped<IUnitOfWork, NotReadyUnitOfWork>();
            services.AddScoped<IUserRepository, NotReadyUserRepository>();
            services.AddScoped<IDepartmentRepository, NotReadyDepartmentRepository>();
            services.AddScoped<IWorkflowCatalogRepository, NotReadyWorkflowCatalogRepository>();
            services.AddScoped<IIdempotencyStore, NotReadyIdempotencyStore>();
            services.AddScoped<IWorkspaceQueryService, NotReadyWorkspaceQueryService>();
            services.AddScoped<IEmployeeWorkspaceQueryService, NotReadyEmployeeWorkspaceQueryService>();
            services.AddScoped<IDepartmentCommandService, NotReadyDepartmentCommandService>();
            services.AddScoped<IFileCenterService, NotReadyFileCenterService>();
            services.AddScoped<IMinutesService, NotReadyBatch2Service>();
            services.AddScoped<IWorkflowPolicyService, NotReadyBatch2Service>();
            services.AddScoped<IWorkflowTaskFormService, NotReadyWorkflowTaskFormService>();
            services.AddScoped<IWorkflowTaskFormValidator, NotReadyWorkflowTaskFormValidator>();
            services.AddScoped<IWorkflowTaskAiReviewService, NotReadyWorkflowTaskAiReviewService>();
            services.AddScoped<ISlaProcessor, NotReadyBatch2Service>();
            services.AddScoped<IAgentGatewayService, NotReadyBatch2Service>();
            services.AddScoped<IProjectService, NotReadyProjectService>();
            services.AddScoped<IEventInboxService, NotReadyEventInboxService>();
            services.AddScoped<IAtomicEventInboxConfirmationService, NotReadyEventInboxService>();
            services.AddScoped<IWorkflowConfigurationService, NotReadyWorkflowConfigurationService>();
            services.AddScoped<IIdentityConfigurationService, NotReadyIamNavigationService>();
            services.AddScoped<INavigationService, NotReadyIamNavigationService>();
            services.AddScoped<IWorkbenchService, NotReadyWorkbenchService>();
            services.AddScoped<IWorkflowAssignmentResolver, NotReadyIamNavigationService>();
            services.AddScoped<IWorkflowRoleService, NotReadyIamNavigationService>();
            services.AddScoped<IWorkflowTaskAssignmentService, NotReadyIamNavigationService>();
            services.AddScoped<IMcpOAuthService, NotReadyMcpService>();
            services.AddScoped<IMcpAuditService, NotReadyMcpService>();
            services.AddScoped<IMcpHumanConfirmationService, NotReadyMcpService>();
            services.AddScoped<IMcpManagementService, NotReadyMcpService>();
            services.AddScoped<IAgentGovernanceService, NotReadyAgentGovernanceService>();
            services.AddScoped<IScenarioService, NotReadyScenarioService>();
            services.AddScoped<ISecurityAuditService, NotReadySecurityAuditService>();
        }
        else
        {
            services.AddScoped<IObjectTypeRepository, SqlObjectTypeRepository>();
            services.AddScoped<IObjectModelService, SqlObjectModelService>();
            services.AddScoped<IBusinessObjectRepository, SqlBusinessObjectRepository>();
            services.AddScoped<IWorkflowRepository, SqlWorkflowRepository>();
            services.AddScoped<IAuditRepository, SqlAuditRepository>();
            services.AddScoped<IOutboxRepository, SqlOutboxRepository>();
            services.AddScoped<IAgentRepository, SqlAgentRepository>();
            services.AddScoped<IUserRepository, SqlUserRepository>();
            services.AddScoped<IDepartmentRepository, SqlDepartmentRepository>();
            services.AddScoped<IWorkflowCatalogRepository, SqlWorkflowCatalogRepository>();
            services.AddScoped<IIdempotencyStore, SqlIdempotencyStore>();
            services.AddScoped<IWorkspaceQueryService, SqlWorkspaceQueryService>();
            services.AddScoped<IEmployeeWorkspaceQueryService, SqlEmployeeWorkspaceQueryService>();
            services.AddScoped<IDepartmentCommandService, SqlDepartmentCommandService>();
            services.AddScoped<SqlFileCenterService>();
            services.AddScoped<IFileCenterService>(
                provider => provider.GetRequiredService<SqlFileCenterService>());
            services.AddScoped<SqlBatch2Service>();
            services.AddScoped<IMinutesService>(provider => provider.GetRequiredService<SqlBatch2Service>());
            services.AddScoped<IWorkflowPolicyService>(provider => provider.GetRequiredService<SqlBatch2Service>());
            services.AddScoped<IWorkflowTaskFormService, SqlWorkflowTaskFormService>();
            services.AddScoped<IWorkflowTaskFormValidator, SqlWorkflowTaskFormValidator>();
            services.AddScoped<IWorkflowTaskAiReviewService, SqlWorkflowTaskAiReviewService>();
            services.AddScoped<ISlaProcessor>(provider => provider.GetRequiredService<SqlBatch2Service>());
            services.AddScoped<IAgentGatewayService>(provider => provider.GetRequiredService<SqlBatch2Service>());
            services.AddScoped<SqlProjectService>();
            services.AddScoped<IProjectService>(provider => provider.GetRequiredService<SqlProjectService>());
            services.AddScoped<SqlEventInboxService>();
            services.AddScoped<IEventInboxService>(provider => provider.GetRequiredService<SqlEventInboxService>());
            services.AddScoped<IAtomicEventInboxConfirmationService>(provider => provider.GetRequiredService<SqlEventInboxService>());
            services.AddScoped<IWorkflowConfigurationService, SqlWorkflowConfigurationService>();
            services.AddScoped<IIdentityConfigurationService, SqlIdentityConfigurationService>();
            services.AddScoped<INavigationService, SqlNavigationService>();
            services.AddScoped<IWorkbenchService, SqlWorkbenchService>();
            services.AddScoped<IWorkflowAssignmentResolver, SqlWorkflowAssignmentResolver>();
            services.AddScoped<IWorkflowRoleService, SqlWorkflowRoleService>();
            services.AddScoped<IWorkflowTaskAssignmentService, SqlWorkflowTaskAssignmentService>();
            services.AddScoped<IMcpOAuthService, SqlMcpOAuthService>();
            services.AddScoped<IMcpAuditService, SqlMcpAuditService>();
            services.AddScoped<IMcpHumanConfirmationService, SqlMcpHumanConfirmationService>();
            services.AddScoped<IMcpManagementService, SqlMcpManagementService>();
            services.AddScoped<IAgentGovernanceService, SqlAgentGovernanceService>();
            services.AddScoped<IScenarioService, SqlScenarioService>();
            services.AddScoped<ISecurityAuditService, SqlSecurityAuditService>();
            services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        }

        return services;
    }
}
