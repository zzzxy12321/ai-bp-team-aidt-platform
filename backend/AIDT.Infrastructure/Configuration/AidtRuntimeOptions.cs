namespace AIDT.Infrastructure.Configuration;

public sealed class AidtRuntimeOptions
{
    public const string SectionName = "AIDT";

    public string ProjectState { get; set; } = "MCP_READY";

    public DatabaseOptions Database { get; set; } = new();

    public ExternalSystemsOptions ExternalSystems { get; set; } = new();

    public WorkflowRuntimeOptions Workflow { get; set; } = new();

    public WorkerRuntimeOptions Worker { get; set; } = new();

    public AIDT.Application.Contracts.DataEnvironmentOptions Data { get; set; } = new();

    public BusinessTimeOptions BusinessTime { get; set; } = new();

    public FileStorageOptions FileStorage { get; set; } = new();
}

public sealed class DatabaseOptions
{
    public string Provider { get; set; } = "Microsoft.EntityFrameworkCore.SqlServer";

    public bool IsReady { get; set; }

    public string MigrationState { get; set; } = "applied_verified";
}

public sealed class ExternalSystemsOptions
{
    public string Mode { get; set; } = "Disconnected";

    public bool AllowWrites { get; set; }
}

public sealed class WorkflowRuntimeOptions
{
    public string Provider { get; set; } = "ElsaWorkflows";

    public bool IsConfigured { get; set; }

    public bool RunMigrationsOnStartup { get; set; }
}

public sealed class WorkerRuntimeOptions
{
    public bool EnableBackgroundProcessing { get; set; }
}

public sealed class FileStorageOptions
{
    public string RootPath { get; set; } = @"C:\ProgramData\AIDT\uploads";

    public long MaxSizeBytes { get; set; } = 50 * 1024 * 1024;
}

public sealed class BusinessTimeOptions
{
    public string TimeZoneId { get; set; } = "Asia/Shanghai";

    public int DeadlineHour { get; set; } = 18;

    public TimeZoneInfo ResolveTimeZone() =>
        TimeZoneInfo.FindSystemTimeZoneById(
            string.IsNullOrWhiteSpace(TimeZoneId) ? "Asia/Shanghai" : TimeZoneId.Trim());
}
