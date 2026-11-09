BEGIN TRANSACTION;
DELETE FROM [Navigation].[NavigationItem] WHERE [ItemKey] = N'CONFIRMATIONS';

ALTER TABLE [Minutes].[MinutesActionItem] DROP CONSTRAINT [FK_MinutesActionItem_BusinessObject];

ALTER TABLE [Minutes].[MinutesActionItem] DROP CONSTRAINT [FK_MinutesActionItem_Project];

ALTER TABLE [Minutes].[MinutesActionItem] DROP CONSTRAINT [FK_MinutesActionItem_WorkflowInstance];

ALTER TABLE [Minutes].[MinutesTask] DROP CONSTRAINT [FK_MinutesTask_Project];

DROP INDEX [IX_MinutesTask_ProjectId] ON [Minutes].[MinutesTask];

DROP INDEX [IX_MinutesActionItem_BusinessObjectId] ON [Minutes].[MinutesActionItem];

DROP INDEX [IX_MinutesActionItem_ProjectId] ON [Minutes].[MinutesActionItem];

DROP INDEX [IX_MinutesActionItem_WorkflowInstanceId] ON [Minutes].[MinutesActionItem];

DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Minutes].[MinutesTask]') AND [c].[name] = N'ProjectId');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Minutes].[MinutesTask] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [Minutes].[MinutesTask] DROP COLUMN [ProjectId];

DECLARE @var1 nvarchar(max);
SELECT @var1 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Minutes].[MinutesActionItem]') AND [c].[name] = N'BusinessObjectId');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Minutes].[MinutesActionItem] DROP CONSTRAINT ' + @var1 + ';');
ALTER TABLE [Minutes].[MinutesActionItem] DROP COLUMN [BusinessObjectId];

DECLARE @var2 nvarchar(max);
SELECT @var2 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Minutes].[MinutesActionItem]') AND [c].[name] = N'ProjectId');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Minutes].[MinutesActionItem] DROP CONSTRAINT ' + @var2 + ';');
ALTER TABLE [Minutes].[MinutesActionItem] DROP COLUMN [ProjectId];

DECLARE @var3 nvarchar(max);
SELECT @var3 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Minutes].[MinutesActionItem]') AND [c].[name] = N'WorkflowInstanceId');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Minutes].[MinutesActionItem] DROP CONSTRAINT ' + @var3 + ';');
ALTER TABLE [Minutes].[MinutesActionItem] DROP COLUMN [WorkflowInstanceId];

DELETE FROM [__EFMigrationsHistory]
WHERE [MigrationId] = N'20261102023118_011_UX004ConfirmationCenter';

COMMIT;
GO

