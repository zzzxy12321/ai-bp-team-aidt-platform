BEGIN TRANSACTION;
IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DELETE rolePermission
    FROM [Identity].[RolePermission] rolePermission
    JOIN [Identity].[Permission] permissionRow
        ON permissionRow.Id = rolePermission.PermissionId
    WHERE permissionRow.Code IN
        (
            N'department-read',
            N'department-create',
            N'department-update',
            N'file-read',
            N'file-create',
            N'file-update',
            N'file-delete'
        );

    DELETE [Identity].[Permission]
    WHERE Code IN
        (
            N'department-read',
            N'department-create',
            N'department-update',
            N'file-read',
            N'file-create',
            N'file-update',
            N'file-delete'
        );

    DELETE [Identity].[DataScope]
    WHERE ScopeType = N'department';
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[Attachment] DROP CONSTRAINT [FK_Attachment_Department];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[Attachment] DROP CONSTRAINT [FK_Attachment_Organization];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [BusinessObject].[BusinessObject] DROP CONSTRAINT [FK_BusinessObject_Department];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[DocumentVersion] DROP CONSTRAINT [FK_DocumentVersion_Attachment];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowInstance] DROP CONSTRAINT [FK_WorkflowInstance_Department];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT [FK_WorkflowTask_Department];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DROP TABLE [Document].[AttachmentLink];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DROP TABLE [Identity].[UserDepartmentMembership];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DROP TABLE [Identity].[Department];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DROP INDEX [IX_WorkflowTask_Department_Environment_Status_CreatedAt] ON [Workflow].[WorkflowTask];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT [CK_WorkflowTask_Environment];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DROP INDEX [IX_WorkflowInstance_Department_Environment_StartedAt] ON [Workflow].[WorkflowInstance];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowInstance] DROP CONSTRAINT [CK_WorkflowInstance_Environment];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[DocumentVersion] DROP CONSTRAINT [CK_DocumentVersion_ScanStatus];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[DocumentVersion] DROP CONSTRAINT [CK_DocumentVersion_Sensitivity];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[DocumentVersion] DROP CONSTRAINT [CK_DocumentVersion_Size];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DROP INDEX [IX_BusinessObject_Department_Environment_UpdatedAt] ON [BusinessObject].[BusinessObject];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [BusinessObject].[BusinessObject] DROP CONSTRAINT [CK_BusinessObject_Environment];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DROP INDEX [IX_Attachment_Department_Environment_CreatedAt] ON [Document].[Attachment];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DROP INDEX [IX_Attachment_Organization_Department] ON [Document].[Attachment];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[Attachment] DROP CONSTRAINT [CK_Attachment_Environment];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[Attachment] DROP CONSTRAINT [CK_Attachment_ScanStatus];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[Attachment] DROP CONSTRAINT [CK_Attachment_Sensitivity];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[Attachment] DROP CONSTRAINT [CK_Attachment_Size];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[Attachment] DROP CONSTRAINT [CK_Attachment_Status];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowTask]') AND [c].[name] = N'DepartmentId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Workflow].[WorkflowTask] DROP COLUMN [DepartmentId];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowTask]') AND [c].[name] = N'EnvironmentCode');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Workflow].[WorkflowTask] DROP COLUMN [EnvironmentCode];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowTask]') AND [c].[name] = N'IsTestData');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [Workflow].[WorkflowTask] DROP COLUMN [IsTestData];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowInstance]') AND [c].[name] = N'DepartmentId');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowInstance] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [Workflow].[WorkflowInstance] DROP COLUMN [DepartmentId];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowInstance]') AND [c].[name] = N'EnvironmentCode');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowInstance] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [Workflow].[WorkflowInstance] DROP COLUMN [EnvironmentCode];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowInstance]') AND [c].[name] = N'IsTestData');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowInstance] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [Workflow].[WorkflowInstance] DROP COLUMN [IsTestData];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowInstance]') AND [c].[name] = N'SourceSystem');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowInstance] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [Workflow].[WorkflowInstance] DROP COLUMN [SourceSystem];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowInstance]') AND [c].[name] = N'StartedByUserId');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowInstance] DROP CONSTRAINT ' + @var7 + ';');
    ALTER TABLE [Workflow].[WorkflowInstance] DROP COLUMN [StartedByUserId];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var8 nvarchar(max);
    SELECT @var8 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Document].[DocumentVersion]') AND [c].[name] = N'MimeType');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Document].[DocumentVersion] DROP CONSTRAINT ' + @var8 + ';');
    ALTER TABLE [Document].[DocumentVersion] DROP COLUMN [MimeType];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var9 nvarchar(max);
    SELECT @var9 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Document].[DocumentVersion]') AND [c].[name] = N'OriginalFileName');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Document].[DocumentVersion] DROP CONSTRAINT ' + @var9 + ';');
    ALTER TABLE [Document].[DocumentVersion] DROP COLUMN [OriginalFileName];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var10 nvarchar(max);
    SELECT @var10 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Document].[DocumentVersion]') AND [c].[name] = N'Sensitivity');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Document].[DocumentVersion] DROP CONSTRAINT ' + @var10 + ';');
    ALTER TABLE [Document].[DocumentVersion] DROP COLUMN [Sensitivity];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var11 nvarchar(max);
    SELECT @var11 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Document].[DocumentVersion]') AND [c].[name] = N'SizeBytes');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Document].[DocumentVersion] DROP CONSTRAINT ' + @var11 + ';');
    ALTER TABLE [Document].[DocumentVersion] DROP COLUMN [SizeBytes];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var12 nvarchar(max);
    SELECT @var12 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Document].[DocumentVersion]') AND [c].[name] = N'VirusScanStatus');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Document].[DocumentVersion] DROP CONSTRAINT ' + @var12 + ';');
    ALTER TABLE [Document].[DocumentVersion] DROP COLUMN [VirusScanStatus];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var13 nvarchar(max);
    SELECT @var13 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BusinessObject].[BusinessObject]') AND [c].[name] = N'DepartmentId');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [BusinessObject].[BusinessObject] DROP CONSTRAINT ' + @var13 + ';');
    ALTER TABLE [BusinessObject].[BusinessObject] DROP COLUMN [DepartmentId];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var14 nvarchar(max);
    SELECT @var14 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BusinessObject].[BusinessObject]') AND [c].[name] = N'EnvironmentCode');
    IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [BusinessObject].[BusinessObject] DROP CONSTRAINT ' + @var14 + ';');
    ALTER TABLE [BusinessObject].[BusinessObject] DROP COLUMN [EnvironmentCode];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var15 nvarchar(max);
    SELECT @var15 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BusinessObject].[BusinessObject]') AND [c].[name] = N'IsTestData');
    IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [BusinessObject].[BusinessObject] DROP CONSTRAINT ' + @var15 + ';');
    ALTER TABLE [BusinessObject].[BusinessObject] DROP COLUMN [IsTestData];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var16 nvarchar(max);
    SELECT @var16 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BusinessObject].[BusinessObject]') AND [c].[name] = N'SourceSystem');
    IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [BusinessObject].[BusinessObject] DROP CONSTRAINT ' + @var16 + ';');
    ALTER TABLE [BusinessObject].[BusinessObject] DROP COLUMN [SourceSystem];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var17 nvarchar(max);
    SELECT @var17 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Document].[Attachment]') AND [c].[name] = N'CurrentVersionNumber');
    IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [Document].[Attachment] DROP CONSTRAINT ' + @var17 + ';');
    ALTER TABLE [Document].[Attachment] DROP COLUMN [CurrentVersionNumber];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var18 nvarchar(max);
    SELECT @var18 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Document].[Attachment]') AND [c].[name] = N'DeletionReason');
    IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [Document].[Attachment] DROP CONSTRAINT ' + @var18 + ';');
    ALTER TABLE [Document].[Attachment] DROP COLUMN [DeletionReason];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var19 nvarchar(max);
    SELECT @var19 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Document].[Attachment]') AND [c].[name] = N'DeletionRequestedAt');
    IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [Document].[Attachment] DROP CONSTRAINT ' + @var19 + ';');
    ALTER TABLE [Document].[Attachment] DROP COLUMN [DeletionRequestedAt];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var20 nvarchar(max);
    SELECT @var20 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Document].[Attachment]') AND [c].[name] = N'DeletionRequestedBy');
    IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [Document].[Attachment] DROP CONSTRAINT ' + @var20 + ';');
    ALTER TABLE [Document].[Attachment] DROP COLUMN [DeletionRequestedBy];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var21 nvarchar(max);
    SELECT @var21 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Document].[Attachment]') AND [c].[name] = N'DepartmentId');
    IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [Document].[Attachment] DROP CONSTRAINT ' + @var21 + ';');
    ALTER TABLE [Document].[Attachment] DROP COLUMN [DepartmentId];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var22 nvarchar(max);
    SELECT @var22 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Document].[Attachment]') AND [c].[name] = N'EnvironmentCode');
    IF @var22 IS NOT NULL EXEC(N'ALTER TABLE [Document].[Attachment] DROP CONSTRAINT ' + @var22 + ';');
    ALTER TABLE [Document].[Attachment] DROP COLUMN [EnvironmentCode];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var23 nvarchar(max);
    SELECT @var23 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Document].[Attachment]') AND [c].[name] = N'IsTestData');
    IF @var23 IS NOT NULL EXEC(N'ALTER TABLE [Document].[Attachment] DROP CONSTRAINT ' + @var23 + ';');
    ALTER TABLE [Document].[Attachment] DROP COLUMN [IsTestData];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var24 nvarchar(max);
    SELECT @var24 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Document].[Attachment]') AND [c].[name] = N'OrganizationId');
    IF @var24 IS NOT NULL EXEC(N'ALTER TABLE [Document].[Attachment] DROP CONSTRAINT ' + @var24 + ';');
    ALTER TABLE [Document].[Attachment] DROP COLUMN [OrganizationId];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var25 nvarchar(max);
    SELECT @var25 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Document].[Attachment]') AND [c].[name] = N'SourceSystem');
    IF @var25 IS NOT NULL EXEC(N'ALTER TABLE [Document].[Attachment] DROP CONSTRAINT ' + @var25 + ';');
    ALTER TABLE [Document].[Attachment] DROP COLUMN [SourceSystem];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var26 nvarchar(max);
    SELECT @var26 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Document].[Attachment]') AND [c].[name] = N'Status');
    IF @var26 IS NOT NULL EXEC(N'ALTER TABLE [Document].[Attachment] DROP CONSTRAINT ' + @var26 + ';');
    ALTER TABLE [Document].[Attachment] DROP COLUMN [Status];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @var27 nvarchar(max);
    SELECT @var27 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Document].[Attachment]') AND [c].[name] = N'UpdatedAt');
    IF @var27 IS NOT NULL EXEC(N'ALTER TABLE [Document].[Attachment] DROP CONSTRAINT ' + @var27 + ';');
    ALTER TABLE [Document].[Attachment] DROP COLUMN [UpdatedAt];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DELETE FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment';
END;

COMMIT;
GO
