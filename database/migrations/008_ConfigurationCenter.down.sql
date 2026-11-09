BEGIN TRANSACTION;
IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT [FK_ConfigurationVersion_CreatedBy];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT [FK_ConfigurationVersion_Department];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT [FK_ConfigurationVersion_Organization];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT [FK_ConfigurationVersion_PublishedBy];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT [FK_ConfigurationVersion_PublishedWorkflowVersion];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT [FK_ConfigurationVersion_UpdatedBy];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    DELETE rolePermission
    FROM [Identity].[RolePermission] rolePermission
    JOIN [Identity].[Permission] permissionRow ON permissionRow.Id = rolePermission.PermissionId
    WHERE permissionRow.Code IN (
        N'configuration-create', N'configuration-update',
        N'configuration-publish', N'configuration-rollback');

    DELETE FROM [Identity].[Permission]
    WHERE Code IN (
        N'configuration-create', N'configuration-update',
        N'configuration-publish', N'configuration-rollback');
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    DROP INDEX [IX_ConfigurationVersion_CreatedBy] ON [Configuration].[ConfigurationVersion];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    DROP INDEX [IX_ConfigurationVersion_DepartmentId] ON [Configuration].[ConfigurationVersion];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    DROP INDEX [IX_ConfigurationVersion_Org_Type_State_UpdatedAt] ON [Configuration].[ConfigurationVersion];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    DROP INDEX [IX_ConfigurationVersion_PublishedBy] ON [Configuration].[ConfigurationVersion];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    DROP INDEX [IX_ConfigurationVersion_PublishedWorkflowVersionId] ON [Configuration].[ConfigurationVersion];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    DROP INDEX [IX_ConfigurationVersion_UpdatedBy] ON [Configuration].[ConfigurationVersion];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT [CK_ConfigurationVersion_State];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT [CK_ConfigurationVersion_Type];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Configuration].[ConfigurationVersion]') AND [c].[name] = N'ConfigurationType');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP COLUMN [ConfigurationType];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Configuration].[ConfigurationVersion]') AND [c].[name] = N'DepartmentId');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP COLUMN [DepartmentId];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Configuration].[ConfigurationVersion]') AND [c].[name] = N'Description');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP COLUMN [Description];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Configuration].[ConfigurationVersion]') AND [c].[name] = N'Name');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP COLUMN [Name];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Configuration].[ConfigurationVersion]') AND [c].[name] = N'OrganizationId');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP COLUMN [OrganizationId];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Configuration].[ConfigurationVersion]') AND [c].[name] = N'PublishedBy');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP COLUMN [PublishedBy];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Configuration].[ConfigurationVersion]') AND [c].[name] = N'PublishedWorkflowVersionId');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP COLUMN [PublishedWorkflowVersionId];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Configuration].[ConfigurationVersion]') AND [c].[name] = N'Revision');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT ' + @var7 + ';');
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP COLUMN [Revision];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    DECLARE @var8 nvarchar(max);
    SELECT @var8 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Configuration].[ConfigurationVersion]') AND [c].[name] = N'UpdatedAt');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT ' + @var8 + ';');
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP COLUMN [UpdatedAt];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    DECLARE @var9 nvarchar(max);
    SELECT @var9 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Configuration].[ConfigurationVersion]') AND [c].[name] = N'UpdatedBy');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT ' + @var9 + ';');
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP COLUMN [UpdatedBy];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    DELETE FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter';
END;

COMMIT;
GO

