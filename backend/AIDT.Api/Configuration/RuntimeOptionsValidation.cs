using AIDT.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace AIDT.Api.Configuration;

public sealed class RuntimeOptionsValidation : IValidateOptions<AidtRuntimeOptions>
{
    public ValidateOptionsResult Validate(string? name, AidtRuntimeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var failures = new List<string>();

        if (!string.Equals(
                options.Database.Provider,
                "Microsoft.EntityFrameworkCore.SqlServer",
                StringComparison.Ordinal))
        {
            failures.Add("AIDT:Database:Provider 必须为 Microsoft.EntityFrameworkCore.SqlServer；禁止替代数据库。");
        }

        if (options.Database.MigrationState is not ("authored_not_applied" or "applied_not_verified" or "applied_verified" or "verified"))
        {
            failures.Add("AIDT:Database:MigrationState 值不合法。");
        }

        if (options.ExternalSystems.Mode is not ("Disconnected" or "Mock" or "Sandbox" or "Connected"))
        {
            failures.Add("AIDT:ExternalSystems:Mode 值不合法。");
        }

        if (options.ExternalSystems.Mode == "Disconnected" && options.ExternalSystems.AllowWrites)
        {
            failures.Add("外部系统为 Disconnected 时禁止 AllowWrites=true。");
        }

        if (!string.Equals(options.Workflow.Provider, "ElsaWorkflows", StringComparison.Ordinal))
        {
            failures.Add("AIDT:Workflow:Provider 必须为 ElsaWorkflows。");
        }

        if (options.Data.CurrentEnvironmentCode is not ("demo" or "test" or "prod"))
        {
            failures.Add("AIDT:Data:CurrentEnvironmentCode 必须为 demo、test 或 prod。");
        }

        if (options.BusinessTime.DeadlineHour is < 0 or > 23)
        {
            failures.Add("AIDT:BusinessTime:DeadlineHour 必须在 0—23 之间。");
        }

        try
        {
            _ = options.BusinessTime.ResolveTimeZone();
        }
        catch (TimeZoneNotFoundException)
        {
            failures.Add("AIDT:BusinessTime:TimeZoneId 不是本机可识别的时区。");
        }
        catch (InvalidTimeZoneException)
        {
            failures.Add("AIDT:BusinessTime:TimeZoneId 时区数据无效。");
        }

        if (string.IsNullOrWhiteSpace(options.FileStorage.RootPath))
        {
            failures.Add("AIDT:FileStorage:RootPath 不能为空。");
        }

        if (options.FileStorage.MaxSizeBytes is < 1024 or > 524_288_000)
        {
            failures.Add("AIDT:FileStorage:MaxSizeBytes 必须在 1 KiB 到 500 MiB 之间。");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
