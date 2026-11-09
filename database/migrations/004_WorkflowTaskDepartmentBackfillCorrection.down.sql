BEGIN TRANSACTION;
IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030174644_004_WorkflowTaskDepartmentBackfillCorrection'
)
BEGIN
    DELETE FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030174644_004_WorkflowTaskDepartmentBackfillCorrection';
END;

COMMIT;
GO
