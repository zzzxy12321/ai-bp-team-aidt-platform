namespace AIDT.McpTests;

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
            Skip = "SQL integration tests require AIDT_RUN_SQL_INTEGRATION_TESTS=1.";
        }
    }
}
