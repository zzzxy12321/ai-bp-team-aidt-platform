using System.Globalization;
using Microsoft.Data.SqlClient;
using Shouldly;

namespace AIDT.UnitTests.Integration;

public sealed class SqlServerPersistenceInvariantTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("AIDT_SQL_CONNECTION")
        ?? @"Server=np:\\.\pipe\sql\query;Database=AIDT;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5";

    [SqlIntegrationFact]
    public async Task BusinessObjectUniqueCode_RejectsDuplicateAndCanRollback()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        var objectTypeId = await ScalarAsync<Guid>(
            connection,
            transaction,
            "SELECT Id FROM [Metadata].[ObjectType] WHERE Code=N'CUSTOMER_SAMPLE';");
        var organizationId = await ScalarAsync<Guid>(
            connection,
            transaction,
            "SELECT TOP (1) OrganizationId FROM [BusinessObject].[BusinessObject];");
        var departmentId = await ScalarAsync<Guid>(
            connection,
            transaction,
            "SELECT TOP (1) Id FROM [Identity].[Department] WHERE Code=N'RND';");
        var existingCode = await ScalarAsync<string>(
            connection,
            transaction,
            "SELECT TOP (1) ObjectCode FROM [BusinessObject].[BusinessObject] WHERE ObjectTypeId=@ObjectTypeId;",
            ("@ObjectTypeId", objectTypeId));
        var duplicateId = Guid.NewGuid();

        var exception = await Should.ThrowAsync<SqlException>(() =>
            ExecuteAsync(
                connection,
                transaction,
                """
                INSERT INTO [BusinessObject].[BusinessObject]
                    (Id, OrganizationId, DepartmentId, EnvironmentCode, IsTestData, SourceSystem,
                     ObjectTypeId, ObjectCode, Status, Version, FieldsJson, CreatedAt, UpdatedAt, IsDeleted)
                VALUES
                    (@Id, @OrganizationId, @DepartmentId, N'test', 1, N'AIDT-INTEGRATION',
                     @ObjectTypeId, @ObjectCode, N'Active', 1, N'{}', SYSUTCDATETIME(), SYSUTCDATETIME(), 0);
                """,
                ("@Id", duplicateId),
                ("@OrganizationId", organizationId),
                ("@DepartmentId", departmentId),
                ("@ObjectTypeId", objectTypeId),
                ("@ObjectCode", existingCode)));

        exception.Number.ShouldBeOneOf(2601, 2627);
        await transaction.RollbackAsync();
        var persisted = await ScalarAsync<int>(
            connection,
            null,
            "SELECT COUNT(*) FROM [BusinessObject].[BusinessObject] WHERE Id=@Id;",
            ("@Id", duplicateId));
        persisted.ShouldBe(0);
    }

    [SqlIntegrationFact]
    public async Task IdempotencyUniqueConstraint_RejectsDuplicateAndCanRollback()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        var scope = $"integration-{Guid.NewGuid():N}";
        var key = $"key-{Guid.NewGuid():N}";
        var firstId = Guid.NewGuid();
        var duplicateId = Guid.NewGuid();
        const string insertSql =
            """
            INSERT INTO [Integration].[IdempotencyRecord]
                (Id, Scope, IdempotencyKey, RequestHash, ResponseType, ResponseId, Status, CreatedAt, ExpiresAt)
            VALUES
                (@Id, @Scope, @Key, N'HASH', N'IntegrationTest', @ResponseId, N'completed', SYSUTCDATETIME(), DATEADD(day, 1, SYSUTCDATETIME()));
            """;

        await ExecuteAsync(
            connection,
            transaction,
            insertSql,
            ("@Id", firstId),
            ("@Scope", scope),
            ("@Key", key),
            ("@ResponseId", Guid.NewGuid().ToString("D")));
        var exception = await Should.ThrowAsync<SqlException>(() =>
            ExecuteAsync(
                connection,
                transaction,
                insertSql,
                ("@Id", duplicateId),
                ("@Scope", scope),
                ("@Key", key),
                ("@ResponseId", Guid.NewGuid().ToString("D"))));

        exception.Number.ShouldBeOneOf(2601, 2627);
        await transaction.RollbackAsync();
        var persisted = await ScalarAsync<int>(
            connection,
            null,
            "SELECT COUNT(*) FROM [Integration].[IdempotencyRecord] WHERE Scope=@Scope;",
            ("@Scope", scope));
        persisted.ShouldBe(0);
    }

    [SqlIntegrationFact]
    public async Task AgentAmountCheckConstraint_RejectsNegativeAmount()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        var profileId = await ScalarAsync<Guid>(
            connection,
            transaction,
            "SELECT TOP (1) Id FROM [Agent].[AgentProfile];");
        var userId = await ScalarAsync<Guid>(
            connection,
            transaction,
            "SELECT TOP (1) Id FROM [Identity].[User] WHERE IsServiceIdentity=0;");

        var exception = await Should.ThrowAsync<SqlException>(() =>
            ExecuteAsync(
                connection,
                transaction,
                """
                INSERT INTO [Agent].[AgentRun]
                    (Id, AgentProfileId, RequestedBy, Action, Amount, RiskLevel, Status, CreatedAt)
                VALUES
                    (@Id, @AgentProfileId, @RequestedBy, N'Draft', -1, N'Low', N'Failed', SYSUTCDATETIME());
                """,
                ("@Id", Guid.NewGuid()),
                ("@AgentProfileId", profileId),
                ("@RequestedBy", userId)));

        exception.Number.ShouldBe(547);
        await transaction.RollbackAsync();
    }

    [SqlIntegrationFact]
    public async Task ConcurrentVersionGuardedUpdate_AllowsOnlyOneWinner()
    {
        await using var setupConnection = new SqlConnection(ConnectionString);
        await setupConnection.OpenAsync();
        var objectTypeId = await ScalarAsync<Guid>(
            setupConnection,
            null,
            "SELECT Id FROM [Metadata].[ObjectType] WHERE Code=N'CUSTOMER_SAMPLE';");
        var organizationId = await ScalarAsync<Guid>(
            setupConnection,
            null,
            "SELECT TOP (1) OrganizationId FROM [BusinessObject].[BusinessObject];");
        var departmentId = await ScalarAsync<Guid>(
            setupConnection,
            null,
            "SELECT TOP (1) Id FROM [Identity].[Department] WHERE Code=N'RND';");
        var objectId = Guid.NewGuid();
        var objectCode = $"CONCURRENT-{Guid.NewGuid():N}";
        await ExecuteAsync(
            setupConnection,
            null,
            """
            INSERT INTO [BusinessObject].[BusinessObject]
                (Id, OrganizationId, DepartmentId, EnvironmentCode, IsTestData, SourceSystem,
                 ObjectTypeId, ObjectCode, Status, Version, FieldsJson, CreatedAt, UpdatedAt, IsDeleted)
            VALUES
                (@Id, @OrganizationId, @DepartmentId, N'test', 1, N'AIDT-INTEGRATION',
                 @ObjectTypeId, @ObjectCode, N'Active', 1, N'{}', SYSUTCDATETIME(), SYSUTCDATETIME(), 0);
            """,
            ("@Id", objectId),
            ("@OrganizationId", organizationId),
            ("@DepartmentId", departmentId),
            ("@ObjectTypeId", objectTypeId),
            ("@ObjectCode", objectCode));

        try
        {
            await using var firstConnection = new SqlConnection(ConnectionString);
            await using var secondConnection = new SqlConnection(ConnectionString);
            await firstConnection.OpenAsync();
            await secondConnection.OpenAsync();
            await using var firstTransaction =
                (SqlTransaction)await firstConnection.BeginTransactionAsync();
            var firstAffected = await ExecuteAsync(
                firstConnection,
                firstTransaction,
                "UPDATE [BusinessObject].[BusinessObject] SET Version=Version+1, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id AND Version=1;",
                ("@Id", objectId));
            firstAffected.ShouldBe(1);

            var secondUpdate = ExecuteAsync(
                secondConnection,
                null,
                "UPDATE [BusinessObject].[BusinessObject] SET Version=Version+1, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id AND Version=1;",
                ("@Id", objectId));
            await Task.Delay(150);
            await firstTransaction.CommitAsync();
            var secondAffected = await secondUpdate;

            secondAffected.ShouldBe(0);
            var finalVersion = await ScalarAsync<long>(
                setupConnection,
                null,
                "SELECT Version FROM [BusinessObject].[BusinessObject] WHERE Id=@Id;",
                ("@Id", objectId));
            finalVersion.ShouldBe(2);
        }
        finally
        {
            await ExecuteAsync(
                setupConnection,
                null,
                "DELETE FROM [BusinessObject].[BusinessObject] WHERE Id=@Id;",
                ("@Id", objectId));
        }
    }

    private static async Task<int> ExecuteAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        }

        return await command.ExecuteNonQueryAsync();
    }

    private static async Task<T> ScalarAsync<T>(
        SqlConnection connection,
        SqlTransaction? transaction,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        }

        var value = await command.ExecuteScalarAsync();
        return value is T result
            ? result
            : (T)Convert.ChangeType(value!, typeof(T), CultureInfo.InvariantCulture);
    }
}
