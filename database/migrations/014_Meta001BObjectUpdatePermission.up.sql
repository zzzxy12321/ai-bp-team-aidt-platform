BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102155319_014_Meta001BObjectUpdatePermission'
)
BEGIN
    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT roleRow.Id, permissionRow.Id
    FROM [Identity].[Role] roleRow
    CROSS JOIN [Identity].[Permission] permissionRow
    WHERE roleRow.Code IN (
        N'rd-engineer',
        N'quality-engineer',
        N'supply-specialist',
        N'finance-approver',
        N'production-team',
        N'project-manager')
      AND permissionRow.Code = N'business-object-update'
      AND NOT EXISTS (
          SELECT 1
          FROM [Identity].[RolePermission] existing
          WHERE existing.RoleId = roleRow.Id
            AND existing.PermissionId = permissionRow.Id);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102155319_014_Meta001BObjectUpdatePermission'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102155319_014_Meta001BObjectUpdatePermission', N'10.0.14');
END;

COMMIT;
GO

