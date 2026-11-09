BEGIN TRANSACTION;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103024548_016_AgentIdentityViewPermissions'
)
BEGIN
    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT roleRow.Id, permissionRow.Id
    FROM [Identity].[Role] roleRow
    JOIN [Identity].[Permission] permissionRow
        ON permissionRow.Code = N'agent-read'
    WHERE roleRow.Code IN (
        N'supply-specialist',
        N'finance-approver',
        N'production-team')
      AND NOT EXISTS (
          SELECT 1 FROM [Identity].[RolePermission] existing
          WHERE existing.RoleId = roleRow.Id
            AND existing.PermissionId = permissionRow.Id);

    DECLARE @OrgId uniqueidentifier =
        (SELECT TOP (1) Id FROM [Identity].[Organization] WHERE IsActive = 1 ORDER BY CreatedAt, Id);
    DECLARE @RndDepartmentId uniqueidentifier =
        (SELECT TOP (1) Id FROM [Identity].[Department]
         WHERE OrganizationId = @OrgId AND Code = N'RND' AND IsActive = 1);
    DECLARE @Now datetime2(3) = SYSUTCDATETIME();
    DECLARE @SeedUsers TABLE (Id uniqueidentifier PRIMARY KEY);
    INSERT INTO @SeedUsers (Id)
    VALUES
        ('10000000-0000-0000-0000-000000000030'),
        ('10000000-0000-0000-0000-000000000031'),
        ('10000000-0000-0000-0000-000000000032'),
        ('10000000-0000-0000-0000-000000000033'),
        ('10000000-0000-0000-0000-000000000034'),
        ('10000000-0000-0000-0000-000000000035');

    INSERT INTO [Identity].[User]
        (Id, OrganizationId, UserName, DisplayName, IsServiceIdentity, IsActive, CreatedAt)
    SELECT seed.Id, @OrgId,
           CONCAT(N'agent.identity.', RIGHT(CONVERT(nvarchar(36), seed.Id), 2)),
           CONCAT(N'数字员工服务账号 ', RIGHT(CONVERT(nvarchar(36), seed.Id), 2)),
           1, 1, @Now
    FROM @SeedUsers seed
    WHERE NOT EXISTS (
        SELECT 1 FROM [Identity].[User] existing WHERE existing.Id = seed.Id);

    INSERT INTO [Identity].[UserRole] (UserId, RoleId)
    SELECT seed.Id, roleRow.Id
    FROM @SeedUsers seed
    JOIN [Identity].[Role] roleRow ON roleRow.Code = N'agent-service'
    WHERE NOT EXISTS (
        SELECT 1 FROM [Identity].[UserRole] existing
        WHERE existing.UserId = seed.Id AND existing.RoleId = roleRow.Id);

    INSERT INTO [Identity].[UserDepartmentMembership]
        (UserId, DepartmentId, IsPrimary, IsManager, IsActive, JoinedAt)
    SELECT seed.Id, @RndDepartmentId, 1, 0, 1, @Now
    FROM @SeedUsers seed
    WHERE @RndDepartmentId IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM [Identity].[UserDepartmentMembership] existing
          WHERE existing.UserId = seed.Id AND existing.DepartmentId = @RndDepartmentId);

    INSERT INTO [Identity].[DataScope] (Id, UserId, ScopeType, ScopeReference)
    SELECT NEWID(), seed.Id, N'organization', CONVERT(nvarchar(36), @OrgId)
    FROM @SeedUsers seed
    WHERE NOT EXISTS (
        SELECT 1 FROM [Identity].[DataScope] existing
        WHERE existing.UserId = seed.Id
          AND existing.ScopeType = N'organization'
          AND existing.ScopeReference = CONVERT(nvarchar(36), @OrgId));

    INSERT INTO [Identity].[DataScope] (Id, UserId, ScopeType, ScopeReference)
    SELECT NEWID(), seed.Id, N'department', CONVERT(nvarchar(36), @RndDepartmentId)
    FROM @SeedUsers seed
    WHERE @RndDepartmentId IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM [Identity].[DataScope] existing
          WHERE existing.UserId = seed.Id
            AND existing.ScopeType = N'department'
            AND existing.ScopeReference = CONVERT(nvarchar(36), @RndDepartmentId));
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103024548_016_AgentIdentityViewPermissions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261103024548_016_AgentIdentityViewPermissions', N'10.0.14');
END;

COMMIT;
GO

