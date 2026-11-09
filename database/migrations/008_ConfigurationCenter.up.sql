BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] ADD [ConfigurationType] nvarchar(32) NOT NULL DEFAULT N'System';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] ADD [DepartmentId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] ADD [Description] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] ADD [Name] nvarchar(200) NOT NULL DEFAULT N'Configuration';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] ADD [OrganizationId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] ADD [PublishedBy] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] ADD [PublishedWorkflowVersionId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] ADD [Revision] bigint NOT NULL DEFAULT CAST(0 AS bigint);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] ADD [UpdatedAt] datetime2(3) NOT NULL DEFAULT '0001-01-01T00:00:00.000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] ADD [UpdatedBy] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    CREATE INDEX [IX_ConfigurationVersion_CreatedBy] ON [Configuration].[ConfigurationVersion] ([CreatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    CREATE INDEX [IX_ConfigurationVersion_DepartmentId] ON [Configuration].[ConfigurationVersion] ([DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    CREATE INDEX [IX_ConfigurationVersion_Org_Type_State_UpdatedAt] ON [Configuration].[ConfigurationVersion] ([OrganizationId], [ConfigurationType], [State], [UpdatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    CREATE INDEX [IX_ConfigurationVersion_PublishedBy] ON [Configuration].[ConfigurationVersion] ([PublishedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    CREATE INDEX [IX_ConfigurationVersion_PublishedWorkflowVersionId] ON [Configuration].[ConfigurationVersion] ([PublishedWorkflowVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    CREATE INDEX [IX_ConfigurationVersion_UpdatedBy] ON [Configuration].[ConfigurationVersion] ([UpdatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    DECLARE @FallbackOrganizationId uniqueidentifier =
        (SELECT TOP (1) Id FROM [Identity].[Organization] ORDER BY CreatedAt, Id);

    UPDATE [Configuration].[ConfigurationVersion]
    SET [OrganizationId] = COALESCE(@FallbackOrganizationId, [OrganizationId]),
        [Name] = CASE
            WHEN [ConfigurationKey] = N'SYSTEM.WORKER.HEARTBEAT' THEN N'Worker heartbeat'
            WHEN [Name] = N'' OR [Name] IS NULL THEN [ConfigurationKey]
            ELSE [Name]
        END,
        [ConfigurationType] = CASE
            WHEN [ConfigurationKey] LIKE N'SYSTEM.%' THEN N'System'
            WHEN [ConfigurationType] = N'' THEN N'Workflow'
            ELSE [ConfigurationType]
        END,
        [UpdatedBy] = COALESCE([UpdatedBy], [CreatedBy]),
        [UpdatedAt] = CASE
            WHEN [UpdatedAt] <= CONVERT(datetime2(3), '19000101') THEN [CreatedAt]
            ELSE [UpdatedAt]
        END;

    IF EXISTS (
        SELECT 1
        FROM [Configuration].[ConfigurationVersion]
        WHERE [OrganizationId] = '00000000-0000-0000-0000-000000000000')
        THROW 51008, N'配置中心迁移无法为历史配置确定组织。', 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    EXEC(N'ALTER TABLE [Configuration].[ConfigurationVersion] ADD CONSTRAINT [CK_ConfigurationVersion_State] CHECK ([State] IN (''Draft'',''Validated'',''Published'',''Retired'',''Active''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    EXEC(N'ALTER TABLE [Configuration].[ConfigurationVersion] ADD CONSTRAINT [CK_ConfigurationVersion_Type] CHECK ([ConfigurationType] IN (''Workflow'',''System''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] ADD CONSTRAINT [FK_ConfigurationVersion_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] ADD CONSTRAINT [FK_ConfigurationVersion_Department] FOREIGN KEY ([DepartmentId]) REFERENCES [Identity].[Department] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] ADD CONSTRAINT [FK_ConfigurationVersion_Organization] FOREIGN KEY ([OrganizationId]) REFERENCES [Identity].[Organization] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] ADD CONSTRAINT [FK_ConfigurationVersion_PublishedBy] FOREIGN KEY ([PublishedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] ADD CONSTRAINT [FK_ConfigurationVersion_PublishedWorkflowVersion] FOREIGN KEY ([PublishedWorkflowVersionId]) REFERENCES [Workflow].[WorkflowVersion] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] ADD CONSTRAINT [FK_ConfigurationVersion_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    INSERT INTO [Identity].[Permission] (Id, Code, Resource, Action)
    SELECT permissionSeed.Id, permissionSeed.Code, permissionSeed.Resource, permissionSeed.Action
    FROM (VALUES
        ('31000000-0000-0000-0000-000000000050', N'configuration-create', N'configuration', N'Create'),
        ('31000000-0000-0000-0000-000000000051', N'configuration-update', N'configuration', N'Update'),
        ('31000000-0000-0000-0000-000000000052', N'configuration-publish', N'configuration', N'Approve'),
        ('31000000-0000-0000-0000-000000000053', N'configuration-rollback', N'configuration', N'Manage')
    ) permissionSeed(Id, Code, Resource, Action)
    WHERE NOT EXISTS (
        SELECT 1 FROM [Identity].[Permission] existing
        WHERE existing.Code = permissionSeed.Code
           OR (existing.Resource = permissionSeed.Resource AND existing.Action = permissionSeed.Action)
    );

    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT roleRow.Id, permissionRow.Id
    FROM [Identity].[Role] roleRow
    CROSS JOIN [Identity].[Permission] permissionRow
    WHERE roleRow.Code = N'platform-admin'
      AND permissionRow.Code IN (
          N'configuration-read', N'configuration-create',
          N'configuration-update', N'configuration-publish',
          N'configuration-rollback')
      AND NOT EXISTS (
          SELECT 1 FROM [Identity].[RolePermission] existing
          WHERE existing.RoleId = roleRow.Id AND existing.PermissionId = permissionRow.Id
      );

    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT roleRow.Id, permissionRow.Id
    FROM [Identity].[Role] roleRow
    CROSS JOIN [Identity].[Permission] permissionRow
    WHERE roleRow.Code = N'project-manager'
      AND permissionRow.Code IN (
          N'configuration-read', N'configuration-create', N'configuration-update')
      AND NOT EXISTS (
          SELECT 1 FROM [Identity].[RolePermission] existing
          WHERE existing.RoleId = roleRow.Id AND existing.PermissionId = permissionRow.Id
      );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031183454_Batch008ConfigurationCenter'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031183454_Batch008ConfigurationCenter', N'10.0.14');
END;

COMMIT;
GO

