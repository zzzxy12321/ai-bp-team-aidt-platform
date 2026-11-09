BEGIN TRANSACTION;
DELETE rolePermission
FROM [Identity].[RolePermission] rolePermission
JOIN [Identity].[Permission] permissionRow ON permissionRow.Id = rolePermission.PermissionId
WHERE permissionRow.Code IN (
    N'workbench-template-read',
    N'workbench-template-manage',
    N'workbench-template-approve'
);

DELETE FROM [Identity].[Permission]
WHERE Code IN (
    N'workbench-template-read',
    N'workbench-template-manage',
    N'workbench-template-approve'
);

DROP TABLE [Workspace].[WorkbenchAssignment];

DROP TABLE [Workspace].[WorkbenchCard];

DROP TABLE [Workspace].[WorkbenchTemplateVersion];

DROP TABLE [Workspace].[WorkbenchTemplate];

DELETE FROM [__EFMigrationsHistory]
WHERE [MigrationId] = N'20261102060037_012_WS002RoleWorkbenchTemplates';

COMMIT;
GO

