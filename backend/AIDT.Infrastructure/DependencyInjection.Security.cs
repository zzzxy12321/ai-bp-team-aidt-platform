using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AIDT.Infrastructure;

public static class SecurityDependencyInjection
{
    public static IServiceCollection AddAidtSharedDataProtection(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var configuredPath = configuration.GetValue<string>(
            "AIDT:Authentication:DataProtectionKeysPath");
        var keyPath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "AIDT",
                "keys")
            : configuredPath.Trim();
        Directory.CreateDirectory(keyPath);
        services
            .AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keyPath))
            .SetApplicationName("AIDT");
        return services;
    }
}
