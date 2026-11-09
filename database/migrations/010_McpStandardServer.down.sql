BEGIN TRANSACTION;
IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    DROP TABLE [Mcp].[AccessToken];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    DROP TABLE [Mcp].[AuthorizationCode];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    DROP TABLE [Mcp].[AuthorizationRequest];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    DROP TABLE [Mcp].[Client];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    DROP TABLE [Mcp].[HumanConfirmation];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    DELETE FROM [Navigation].[NavigationItem]
    WHERE ItemKey = N'MCP_CENTER'
      AND Id = '92000000-0000-0000-0000-000000000016';

    DELETE FROM [Identity].[RolePermission]
    WHERE PermissionId IN (
        SELECT Id FROM [Identity].[Permission]
        WHERE Code = N'mcp-read'
           OR Code = N'mcp-manage');

    DELETE FROM [Identity].[Permission]
    WHERE Code = N'mcp-read'
       OR Code = N'mcp-manage';

    DELETE FROM [Identity].[DataScope]
    WHERE UserId = '10000000-0000-0000-0000-000000000024';
    DELETE FROM [Identity].[UserRole]
    WHERE UserId = '10000000-0000-0000-0000-000000000024';
    DELETE FROM [Identity].[UserDepartmentMembership]
    WHERE UserId = '10000000-0000-0000-0000-000000000024';
    DELETE FROM [Identity].[User]
    WHERE Id = '10000000-0000-0000-0000-000000000024'
      AND UserName = N'agent.mcp';
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    IF SCHEMA_ID(N'Mcp') IS NOT NULL
        EXEC(N'DROP SCHEMA [Mcp]');
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    DELETE FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer';
END;

COMMIT;
GO

