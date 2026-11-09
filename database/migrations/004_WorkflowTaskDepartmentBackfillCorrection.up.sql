BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030174644_004_WorkflowTaskDepartmentBackfillCorrection'
)
BEGIN
    UPDATE workflowTask
    SET DepartmentId = assigneeDepartment.Id
    FROM [Workflow].[WorkflowTask] workflowTask
    OUTER APPLY (
        SELECT TOP (1) departmentRow.Id
        FROM [Identity].[UserDepartmentMembership] membershipRow
        JOIN [Identity].[Department] departmentRow
            ON departmentRow.Id = membershipRow.DepartmentId
        WHERE membershipRow.UserId = workflowTask.AssigneeId
          AND membershipRow.IsActive = 1
          AND departmentRow.IsActive = 1
        ORDER BY membershipRow.IsPrimary DESC, membershipRow.JoinedAt
    ) assigneeDepartment
    WHERE assigneeDepartment.Id IS NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030174644_004_WorkflowTaskDepartmentBackfillCorrection'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030174644_004_WorkflowTaskDepartmentBackfillCorrection', N'10.0.14');
END;

COMMIT;
GO
