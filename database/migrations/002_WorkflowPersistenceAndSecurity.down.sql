BEGIN TRANSACTION;
ALTER TABLE [BusinessObject].[BusinessObject] DROP CONSTRAINT [FK_BusinessObject_Organization];

DROP TABLE [Integration].[IdempotencyRecord];

DROP INDEX [IX_BusinessObject_Organization_UpdatedAt_Id] ON [BusinessObject].[BusinessObject];

DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BusinessObject].[BusinessObject]') AND [c].[name] = N'OrganizationId');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [BusinessObject].[BusinessObject] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [BusinessObject].[BusinessObject] DROP COLUMN [OrganizationId];

DELETE FROM [__EFMigrationsHistory]
WHERE [MigrationId] = N'20261030004100_WorkflowPersistenceAndSecurity';

COMMIT;
GO

