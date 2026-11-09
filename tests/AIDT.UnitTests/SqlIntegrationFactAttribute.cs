namespace AIDT.UnitTests;

[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlIntegrationFactAttribute : FactAttribute
{
    public SqlIntegrationFactAttribute()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("AIDT_RUN_SQL_INTEGRATION_TESTS"),
                "1",
                StringComparison.Ordinal))
        {
            Skip = "SQL BLOCKED：只有显式设置 AIDT_RUN_SQL_INTEGRATION_TESTS=1 后才允许执行。";
        }
    }
}
