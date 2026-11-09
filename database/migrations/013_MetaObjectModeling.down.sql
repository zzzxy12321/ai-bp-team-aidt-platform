BEGIN TRANSACTION;
IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    DELETE rolePermission
    FROM [Identity].[RolePermission] rolePermission
    INNER JOIN [Identity].[Permission] permissionRow
        ON permissionRow.[Id] = rolePermission.[PermissionId]
    WHERE permissionRow.[Resource] = N'object-model';

    DELETE FROM [Identity].[Permission]
    WHERE [Resource] = N'object-model';
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    DROP TABLE [Metadata].[ObjectTypeVersion];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    DROP INDEX [IX_ObjectType_Status_UpdatedAt] ON [Metadata].[ObjectType];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    ALTER TABLE [Metadata].[ObjectType] DROP CONSTRAINT [CK_ObjectType_Status];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Metadata].[ObjectType]') AND [c].[name] = N'PublishedAt');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Metadata].[ObjectType] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Metadata].[ObjectType] DROP COLUMN [PublishedAt];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Metadata].[ObjectType]') AND [c].[name] = N'RetiredAt');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Metadata].[ObjectType] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Metadata].[ObjectType] DROP COLUMN [RetiredAt];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Metadata].[ObjectType]') AND [c].[name] = N'Revision');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Metadata].[ObjectType] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [Metadata].[ObjectType] DROP COLUMN [Revision];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Metadata].[ObjectType]') AND [c].[name] = N'RowVersion');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Metadata].[ObjectType] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [Metadata].[ObjectType] DROP COLUMN [RowVersion];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Metadata].[ObjectType]') AND [c].[name] = N'Status');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Metadata].[ObjectType] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [Metadata].[ObjectType] DROP COLUMN [Status];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Metadata].[FieldDefinition]') AND [c].[name] = N'DefaultValue');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Metadata].[FieldDefinition] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [Metadata].[FieldDefinition] DROP COLUMN [DefaultValue];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Metadata].[FieldDefinition]') AND [c].[name] = N'Filterable');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Metadata].[FieldDefinition] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [Metadata].[FieldDefinition] DROP COLUMN [Filterable];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Metadata].[FieldDefinition]') AND [c].[name] = N'OptionsJson');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Metadata].[FieldDefinition] DROP CONSTRAINT ' + @var7 + ';');
    ALTER TABLE [Metadata].[FieldDefinition] DROP COLUMN [OptionsJson];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    DECLARE @var8 nvarchar(max);
    SELECT @var8 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Metadata].[FieldDefinition]') AND [c].[name] = N'ShowInList');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Metadata].[FieldDefinition] DROP CONSTRAINT ' + @var8 + ';');
    ALTER TABLE [Metadata].[FieldDefinition] DROP COLUMN [ShowInList];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    DECLARE @var9 nvarchar(max);
    SELECT @var9 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Metadata].[FieldDefinition]') AND [c].[name] = N'SortOrder');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Metadata].[FieldDefinition] DROP CONSTRAINT ' + @var9 + ';');
    ALTER TABLE [Metadata].[FieldDefinition] DROP COLUMN [SortOrder];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    DELETE FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling';
END;

COMMIT;
GO

