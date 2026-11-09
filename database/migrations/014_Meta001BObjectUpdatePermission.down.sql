BEGIN TRANSACTION;
IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102155319_014_Meta001BObjectUpdatePermission'
)
BEGIN
    DELETE rolePermission
    FROM [Identity].[RolePermission] rolePermission
    INNER JOIN [Identity].[Role] roleRow
        ON roleRow.Id = rolePermission.RoleId
    INNER JOIN [Identity].[Permission] permissionRow
        ON permissionRow.Id = rolePermission.PermissionId
    WHERE roleRow.Code IN (
        N'rd-engineer',
        N'quality-engineer',
        N'supply-specialist',
        N'finance-approver',
        N'production-team',
        N'project-manager')
      AND permissionRow.Code = N'business-object-update';
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102155319_014_Meta001BObjectUpdatePermission'
)
BEGIN
    DELETE FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102155319_014_Meta001BObjectUpdatePermission';
END;

COMMIT;
GO

