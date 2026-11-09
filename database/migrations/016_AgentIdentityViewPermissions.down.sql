BEGIN TRANSACTION;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
DECLARE @SeedUserIds TABLE (Id uniqueidentifier PRIMARY KEY);
INSERT INTO @SeedUserIds (Id)
SELECT userRow.Id
FROM [Identity].[User] userRow
WHERE userRow.Id IN (
    '10000000-0000-0000-0000-000000000030',
    '10000000-0000-0000-0000-000000000031',
    '10000000-0000-0000-0000-000000000032',
    '10000000-0000-0000-0000-000000000033',
    '10000000-0000-0000-0000-000000000034',
    '10000000-0000-0000-0000-000000000035')
  AND NOT EXISTS (
      SELECT 1 FROM [Agent].[AgentProfile] profile
      WHERE profile.ServiceUserId = userRow.Id);

DELETE rolePermission
FROM [Identity].[RolePermission] rolePermission
JOIN [Identity].[Role] roleRow ON roleRow.Id = rolePermission.RoleId
JOIN [Identity].[Permission] permissionRow ON permissionRow.Id = rolePermission.PermissionId
WHERE roleRow.Code IN (
    N'supply-specialist',
    N'finance-approver',
    N'production-team')
  AND permissionRow.Code = N'agent-read';

DELETE FROM [Identity].[UserRole] WHERE UserId IN (SELECT Id FROM @SeedUserIds);
DELETE FROM [Identity].[UserDepartmentMembership] WHERE UserId IN (SELECT Id FROM @SeedUserIds);
DELETE FROM [Identity].[DataScope] WHERE UserId IN (SELECT Id FROM @SeedUserIds);
DELETE FROM [Identity].[User] WHERE Id IN (SELECT Id FROM @SeedUserIds);

DELETE FROM [__EFMigrationsHistory]
WHERE [MigrationId] = N'20261103024548_016_AgentIdentityViewPermissions';

COMMIT;
GO

