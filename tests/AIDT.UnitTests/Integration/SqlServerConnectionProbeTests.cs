using Microsoft.Data.SqlClient;
using Shouldly;

namespace AIDT.UnitTests.Integration;

public sealed class SqlServerConnectionProbeTests
{
    [SqlIntegrationFact]
    [Trait("Category", "RequiresSqlServer")]
    public async Task NamedPipeConnection_WithCurrentWindowsIdentity_CanQueryAidt()
    {
        var connectionString = Environment.GetEnvironmentVariable("AIDT_SQL_CONNECTION")
            ?? @"Server=np:\\.\pipe\sql\query;Database=AIDT;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5";
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT DB_NAME(), CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(64)), SUSER_SNAME();";
        await using var reader = await command.ExecuteReaderAsync();

        (await reader.ReadAsync()).ShouldBeTrue();
        reader.GetString(0).ShouldBe(connection.Database);
        reader.GetString(1).ShouldStartWith("17.");
        reader.GetString(2).ShouldNotBeNullOrWhiteSpace();
    }
}
