BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102023118_011_UX004ConfirmationCenter'
)
BEGIN
    ALTER TABLE [Minutes].[MinutesTask] ADD [ProjectId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102023118_011_UX004ConfirmationCenter'
)
BEGIN
    ALTER TABLE [Minutes].[MinutesActionItem] ADD [BusinessObjectId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102023118_011_UX004ConfirmationCenter'
)
BEGIN
    ALTER TABLE [Minutes].[MinutesActionItem] ADD [ProjectId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102023118_011_UX004ConfirmationCenter'
)
BEGIN
    ALTER TABLE [Minutes].[MinutesActionItem] ADD [WorkflowInstanceId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102023118_011_UX004ConfirmationCenter'
)
BEGIN
    CREATE INDEX [IX_MinutesTask_ProjectId] ON [Minutes].[MinutesTask] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102023118_011_UX004ConfirmationCenter'
)
BEGIN
    CREATE INDEX [IX_MinutesActionItem_BusinessObjectId] ON [Minutes].[MinutesActionItem] ([BusinessObjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102023118_011_UX004ConfirmationCenter'
)
BEGIN
    CREATE INDEX [IX_MinutesActionItem_ProjectId] ON [Minutes].[MinutesActionItem] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102023118_011_UX004ConfirmationCenter'
)
BEGIN
    CREATE INDEX [IX_MinutesActionItem_WorkflowInstanceId] ON [Minutes].[MinutesActionItem] ([WorkflowInstanceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102023118_011_UX004ConfirmationCenter'
)
BEGIN
    ALTER TABLE [Minutes].[MinutesActionItem] ADD CONSTRAINT [FK_MinutesActionItem_BusinessObject] FOREIGN KEY ([BusinessObjectId]) REFERENCES [BusinessObject].[BusinessObject] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102023118_011_UX004ConfirmationCenter'
)
BEGIN
    ALTER TABLE [Minutes].[MinutesActionItem] ADD CONSTRAINT [FK_MinutesActionItem_Project] FOREIGN KEY ([ProjectId]) REFERENCES [BusinessObject].[BusinessObject] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102023118_011_UX004ConfirmationCenter'
)
BEGIN
    ALTER TABLE [Minutes].[MinutesActionItem] ADD CONSTRAINT [FK_MinutesActionItem_WorkflowInstance] FOREIGN KEY ([WorkflowInstanceId]) REFERENCES [Workflow].[WorkflowInstance] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102023118_011_UX004ConfirmationCenter'
)
BEGIN
    ALTER TABLE [Minutes].[MinutesTask] ADD CONSTRAINT [FK_MinutesTask_Project] FOREIGN KEY ([ProjectId]) REFERENCES [BusinessObject].[BusinessObject] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102023118_011_UX004ConfirmationCenter'
)
BEGIN
    INSERT INTO [Navigation].[NavigationItem]
        ([Id], [NavigationVersionId], [ItemKey], [Name], [ItemType], [Route],
         [RequiredResource], [RequiredAction], [SortOrder], [IsVisible], [IsEnabled],
         [IsSystem], [Icon], [CreatedAt], [UpdatedAt])
    SELECT NEWID(), versionRow.[Id], N'CONFIRMATIONS', N'统一确认', N'Inbox',
           N'/confirmations', NULL, NULL, 15, 1, 1, 1, N'确',
           SYSUTCDATETIME(), SYSUTCDATETIME()
    FROM [Navigation].[NavigationVersion] AS versionRow
    WHERE versionRow.[Status] = N'Published'
      AND NOT EXISTS
      (
          SELECT 1
          FROM [Navigation].[NavigationItem] AS itemRow
          WHERE itemRow.[NavigationVersionId] = versionRow.[Id]
            AND itemRow.[ItemKey] = N'CONFIRMATIONS'
      );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102023118_011_UX004ConfirmationCenter'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102023118_011_UX004ConfirmationCenter', N'10.0.14');
END;

COMMIT;
GO

