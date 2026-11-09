BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    ALTER TABLE [Metadata].[ObjectType] ADD [PublishedAt] datetime2(3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    ALTER TABLE [Metadata].[ObjectType] ADD [RetiredAt] datetime2(3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    ALTER TABLE [Metadata].[ObjectType] ADD [Revision] bigint NOT NULL DEFAULT CAST(1 AS bigint);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    ALTER TABLE [Metadata].[ObjectType] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    ALTER TABLE [Metadata].[ObjectType] ADD [Status] nvarchar(32) NOT NULL DEFAULT N'Published';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    ALTER TABLE [Metadata].[FieldDefinition] ADD [DefaultValue] nvarchar(4000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    ALTER TABLE [Metadata].[FieldDefinition] ADD [Filterable] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    ALTER TABLE [Metadata].[FieldDefinition] ADD [OptionsJson] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    ALTER TABLE [Metadata].[FieldDefinition] ADD [ShowInList] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    ALTER TABLE [Metadata].[FieldDefinition] ADD [SortOrder] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    CREATE TABLE [Metadata].[ObjectTypeVersion] (
        [Id] bigint NOT NULL IDENTITY,
        [ObjectTypeId] uniqueidentifier NOT NULL,
        [Version] bigint NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [FieldsJson] nvarchar(max) NOT NULL,
        [ValidatedAt] datetime2(3) NULL,
        [PublishedAt] datetime2(3) NULL,
        [RetiredAt] datetime2(3) NULL,
        [CreatedBy] uniqueidentifier NULL,
        [UpdatedBy] uniqueidentifier NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_ObjectTypeVersion] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ObjectTypeVersion_FieldsJson] CHECK (ISJSON([FieldsJson]) = 1),
        CONSTRAINT [CK_ObjectTypeVersion_Status] CHECK ([Status] IN ('Draft','Validated','Published','Retired','Superseded')),
        CONSTRAINT [FK_ObjectTypeVersion_ObjectType] FOREIGN KEY ([ObjectTypeId]) REFERENCES [Metadata].[ObjectType] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    CREATE INDEX [IX_ObjectType_Status_UpdatedAt] ON [Metadata].[ObjectType] ([Status], [UpdatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    EXEC(N'ALTER TABLE [Metadata].[ObjectType] ADD CONSTRAINT [CK_ObjectType_Status] CHECK ([Status] IN (''Draft'',''Validated'',''Published'',''Retired'',''Superseded''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    CREATE INDEX [IX_ObjectTypeVersion_Type_Status_Version] ON [Metadata].[ObjectTypeVersion] ([ObjectTypeId], [Status], [Version]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    CREATE UNIQUE INDEX [UX_ObjectTypeVersion_Type_Version] ON [Metadata].[ObjectTypeVersion] ([ObjectTypeId], [Version]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    INSERT INTO [Identity].[Permission] (Id, Code, Resource, Action)
    SELECT NEWID(), N'object-model-' + actionRow.[ActionCode],
           N'object-model', actionRow.[ActionName]
    FROM (VALUES
        (N'read', N'Read'),
        (N'create', N'Create'),
        (N'update', N'Update'),
        (N'manage', N'Manage'),
        (N'approve', N'Approve')
    ) AS actionRow([ActionCode], [ActionName])
    WHERE NOT EXISTS (
        SELECT 1
        FROM [Identity].[Permission] existing
        WHERE existing.Resource = N'object-model'
          AND existing.Action = actionRow.[ActionName]);

    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT roleRow.Id, permissionRow.Id
    FROM [Identity].[Role] roleRow
    CROSS JOIN [Identity].[Permission] permissionRow
    WHERE roleRow.Code = N'platform-admin'
      AND permissionRow.Resource = N'object-model'
      AND NOT EXISTS (
          SELECT 1
          FROM [Identity].[RolePermission] existing
          WHERE existing.RoleId = roleRow.Id
            AND existing.PermissionId = permissionRow.Id);

    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT roleRow.Id, permissionRow.Id
    FROM [Identity].[Role] roleRow
    CROSS JOIN [Identity].[Permission] permissionRow
    WHERE roleRow.Code = N'platform-admin'
      AND permissionRow.Resource = N'business-object'
      AND permissionRow.Action = N'Update'
      AND NOT EXISTS (
          SELECT 1
          FROM [Identity].[RolePermission] existing
          WHERE existing.RoleId = roleRow.Id
            AND existing.PermissionId = permissionRow.Id);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN

    UPDATE [Metadata].[ObjectType]
    SET [Status] = N'Published',
        [Revision] = CASE WHEN [Revision] < 1 THEN 1 ELSE [Revision] END,
        [PublishedAt] = COALESCE([PublishedAt], [UpdatedAt])
    WHERE [Status] = N'Published';

    INSERT INTO [Metadata].[ObjectTypeVersion]
        ([ObjectTypeId], [Version], [Status], [Name], [FieldsJson],
         [ValidatedAt], [PublishedAt], [RetiredAt], [CreatedBy], [UpdatedBy],
         [CreatedAt], [UpdatedAt])
    SELECT objectType.[Id], objectType.[Version], N'Published', objectType.[Name],
           COALESCE((
               SELECT
                   field.[Key] AS [key],
                   field.[DisplayName] AS [displayName],
                   CASE field.[DataType]
                       WHEN N'string' THEN N'Text'
                       WHEN N'text' THEN N'LongText'
                       WHEN N'decimal' THEN N'Number'
                       ELSE field.[DataType]
                   END AS [dataType],
                   field.[IsRequired] AS [isRequired],
                   field.[MaxLength] AS [maxLength],
                   field.[Sensitivity] AS [sensitivity],
                   field.[DefaultValue] AS [defaultValue],
                   CASE
                       WHEN field.[OptionsJson] IS NULL THEN JSON_QUERY(N'[]')
                       ELSE JSON_QUERY(field.[OptionsJson])
                   END AS [options],
                   field.[ShowInList] AS [showInList],
                   field.[Filterable] AS [filterable],
                   field.[SortOrder] AS [sortOrder],
                   field.[Key] AS [code]
               FROM [Metadata].[FieldDefinition] field
               WHERE field.[ObjectTypeId] = objectType.[Id]
               ORDER BY field.[SortOrder], field.[Key]
               FOR JSON PATH
           ), N'[]'),
           objectType.[UpdatedAt], objectType.[UpdatedAt], NULL, NULL, NULL,
           objectType.[CreatedAt], objectType.[UpdatedAt]
    FROM [Metadata].[ObjectType] objectType
    WHERE NOT EXISTS (
        SELECT 1
        FROM [Metadata].[ObjectTypeVersion] version
        WHERE version.[ObjectTypeId] = objectType.[Id]
          AND version.[Version] = objectType.[Version]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102100413_013_MetaObjectModeling'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102100413_013_MetaObjectModeling', N'10.0.14');
END;

COMMIT;
GO

