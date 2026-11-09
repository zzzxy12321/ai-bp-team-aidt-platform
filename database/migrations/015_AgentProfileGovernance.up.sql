BEGIN TRANSACTION;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    DROP INDEX [UX_AgentProfile_Name] ON [Agent].[AgentProfile];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Identity].[AccessChangeRequest] DROP CONSTRAINT [CK_AccessChangeRequest_Type];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Mcp].[Client] ADD [AgentProfileId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Agent].[AgentProfile] ADD [BusinessOwnerUserId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Agent].[AgentProfile] ADD [Code] nvarchar(64) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Agent].[AgentProfile] ADD [CreatedAt] datetime2(3) NOT NULL DEFAULT '0001-01-01T00:00:00.000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Agent].[AgentProfile] ADD [CreatedBy] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Agent].[AgentProfile] ADD [CurrentPublishedVersionId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Agent].[AgentProfile] ADD [DataScopesJson] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Agent].[AgentProfile] ADD [DepartmentId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Agent].[AgentProfile] ADD [Description] nvarchar(2000) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Agent].[AgentProfile] ADD [LifecycleStatus] nvarchar(16) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Agent].[AgentProfile] ADD [ModelScopesJson] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Agent].[AgentProfile] ADD [RequiredConfirmationsJson] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Agent].[AgentProfile] ADD [Revision] bigint NOT NULL DEFAULT CAST(0 AS bigint);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Agent].[AgentProfile] ADD [StopConditionsJson] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Agent].[AgentProfile] ADD [TechnicalOwnerUserId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Agent].[AgentProfile] ADD [UpdatedAt] datetime2(3) NOT NULL DEFAULT '0001-01-01T00:00:00.000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Agent].[AgentProfile] ADD [UpdatedBy] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Agent].[AgentProfile] ADD [Version] bigint NOT NULL DEFAULT CAST(0 AS bigint);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Mcp].[AccessToken] ADD [AgentProfileId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Mcp].[AccessToken] ADD [RevocationReason] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Mcp].[AccessToken] ADD [RevokedBy] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    CREATE TABLE [Agent].[AgentProfileVersion] (
        [Id] uniqueidentifier NOT NULL,
        [AgentProfileId] uniqueidentifier NOT NULL,
        [VersionNumber] int NOT NULL,
        [Revision] bigint NOT NULL,
        [Status] nvarchar(16) NOT NULL,
        [Code] nvarchar(64) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [Description] nvarchar(2000) NOT NULL,
        [ServiceUserId] uniqueidentifier NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [BusinessOwnerUserId] uniqueidentifier NOT NULL,
        [TechnicalOwnerUserId] uniqueidentifier NOT NULL,
        [AutonomyLevel] nvarchar(8) NOT NULL,
        [AmountLimit] decimal(18,4) NOT NULL,
        [RiskLimit] nvarchar(16) NOT NULL,
        [DataScopesJson] nvarchar(max) NOT NULL,
        [ModelScopesJson] nvarchar(max) NOT NULL,
        [AllowedToolsJson] nvarchar(max) NOT NULL,
        [RequiredConfirmationsJson] nvarchar(max) NOT NULL,
        [StopConditionsJson] nvarchar(max) NOT NULL,
        [ValidationJson] nvarchar(max) NULL,
        [IsHighRisk] bit NOT NULL,
        [HighRiskReasonsJson] nvarchar(max) NOT NULL,
        [CreatedBy] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedBy] uniqueidentifier NOT NULL,
        [UpdatedAt] datetime2(3) NOT NULL,
        [ValidatedBy] uniqueidentifier NULL,
        [ValidatedAt] datetime2(3) NULL,
        [PublishedBy] uniqueidentifier NULL,
        [PublishedAt] datetime2(3) NULL,
        [RetiredAt] datetime2(3) NULL,
        CONSTRAINT [PK_AgentProfileVersion] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_AgentProfileVersion_AllowedToolsJson] CHECK (ISJSON([AllowedToolsJson]) = 1),
        CONSTRAINT [CK_AgentProfileVersion_AmountLimit] CHECK ([AmountLimit] >= 0),
        CONSTRAINT [CK_AgentProfileVersion_ConfirmationsJson] CHECK (ISJSON([RequiredConfirmationsJson]) = 1),
        CONSTRAINT [CK_AgentProfileVersion_DataScopesJson] CHECK (ISJSON([DataScopesJson]) = 1),
        CONSTRAINT [CK_AgentProfileVersion_HighRiskReasonsJson] CHECK (ISJSON([HighRiskReasonsJson]) = 1),
        CONSTRAINT [CK_AgentProfileVersion_ModelScopesJson] CHECK (ISJSON([ModelScopesJson]) = 1),
        CONSTRAINT [CK_AgentProfileVersion_Status] CHECK ([Status] IN ('Draft','Validated','Published','Retired')),
        CONSTRAINT [CK_AgentProfileVersion_StopConditionsJson] CHECK (ISJSON([StopConditionsJson]) = 1),
        CONSTRAINT [FK_AgentProfileVersion_BusinessOwner] FOREIGN KEY ([BusinessOwnerUserId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AgentProfileVersion_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AgentProfileVersion_Department] FOREIGN KEY ([DepartmentId]) REFERENCES [Identity].[Department] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AgentProfileVersion_Profile] FOREIGN KEY ([AgentProfileId]) REFERENCES [Agent].[AgentProfile] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AgentProfileVersion_PublishedBy] FOREIGN KEY ([PublishedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AgentProfileVersion_ServiceUser] FOREIGN KEY ([ServiceUserId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AgentProfileVersion_TechnicalOwner] FOREIGN KEY ([TechnicalOwnerUserId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AgentProfileVersion_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AgentProfileVersion_ValidatedBy] FOREIGN KEY ([ValidatedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION
    );
END;

GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    DECLARE @Now datetime2(3) = SYSUTCDATETIME();
    DECLARE @OrgId uniqueidentifier =
        (SELECT TOP (1) Id FROM [Identity].[Organization] WHERE IsActive = 1 ORDER BY CreatedAt, Id);
    DECLARE @AdminUserId uniqueidentifier =
        (SELECT TOP (1) Id FROM [Identity].[User] WHERE UserName = N'demo.admin' AND IsActive = 1);
    DECLARE @RndDepartmentId uniqueidentifier =
        (SELECT TOP (1) Id FROM [Identity].[Department]
         WHERE OrganizationId = @OrgId AND Code = N'RND' AND IsActive = 1);
    DECLARE @ConfirmationsJson nvarchar(max) =
        N'["contract-commitment","customer-compensation","equipment-control","formula-change","payment","quality-release","safety-interlock"]';

    IF @OrgId IS NULL OR @AdminUserId IS NULL OR @RndDepartmentId IS NULL
        THROW 51000, N'015 requires the demo organization, admin and RND department.', 1;

    UPDATE [Agent].[AgentProfile]
    SET Code = CASE Name
            WHEN N'业务协同数字员工' THEN N'AGENT-DEMO'
            WHEN N'流程代理数字员工' THEN N'AGENT-PROXY'
            WHEN N'项目事件建议数字员工' THEN N'AGENT-PROJECT-EVENT'
            ELSE N'AGENT-LEGACY-' + REPLACE(CONVERT(nvarchar(36), Id), N'-', N'')
        END,
        Description = N'既有数字员工身份迁移回填',
        DepartmentId = @RndDepartmentId,
        BusinessOwnerUserId = @AdminUserId,
        TechnicalOwnerUserId = @AdminUserId,
        DataScopesJson = CONCAT(
            N'["organization:', CONVERT(nvarchar(36), @OrgId),
            N'","department:', CONVERT(nvarchar(36), @RndDepartmentId), N'"]'),
        ModelScopesJson = N'["aidt-rule-sim"]',
    AllowedToolsJson = CASE
        WHEN Name = N'业务协同数字员工'
            THEN N'["workflow.assist","workflow.read"]'
        ELSE AllowedToolsJson
    END,
        RequiredConfirmationsJson = @ConfirmationsJson,
        StopConditionsJson =
            N'["amount-limit","manual-stop","risk-limit","unauthorized-tool"]',
        LifecycleStatus = CASE WHEN IsActive = 1 THEN N'Published' ELSE N'Disabled' END,
        Version = 1,
        Revision = 1,
        CreatedBy = @AdminUserId,
        CreatedAt = @Now,
        UpdatedBy = @AdminUserId,
        UpdatedAt = @Now;

    INSERT INTO [Agent].[AgentProfileVersion]
        (Id, AgentProfileId, VersionNumber, Revision, Status, Code, Name, Description,
         ServiceUserId, DepartmentId, BusinessOwnerUserId, TechnicalOwnerUserId,
         AutonomyLevel, AmountLimit, RiskLimit, DataScopesJson, ModelScopesJson,
         AllowedToolsJson, RequiredConfirmationsJson, StopConditionsJson,
         ValidationJson, IsHighRisk, HighRiskReasonsJson,
         CreatedBy, CreatedAt, UpdatedBy, UpdatedAt,
         ValidatedBy, ValidatedAt, PublishedBy, PublishedAt, RetiredAt)
    SELECT
        CASE profile.Code
            WHEN N'AGENT-DEMO' THEN '70000000-0000-0000-0000-000000000001'
            WHEN N'AGENT-PROXY' THEN '70000000-0000-0000-0000-000000000002'
            WHEN N'AGENT-PROJECT-EVENT' THEN '70000000-0000-0000-0000-000000000003'
            ELSE NEWID()
        END,
        profile.Id, 1, 1, N'Published', profile.Code, profile.Name, profile.Description,
        profile.ServiceUserId, profile.DepartmentId, profile.BusinessOwnerUserId,
        profile.TechnicalOwnerUserId, profile.AutonomyLevel, profile.AmountLimit,
        profile.RiskLimit, profile.DataScopesJson, profile.ModelScopesJson,
        profile.AllowedToolsJson, profile.RequiredConfirmationsJson,
        profile.StopConditionsJson,
        N'{"isValid":true,"errors":[],"warnings":["历史身份自动回填"],"isHighRisk":false,"highRiskReasons":[],"validatedAt":"2026-11-03T00:00:00Z"}',
        0, N'[]', @AdminUserId, @Now, @AdminUserId, @Now,
        @AdminUserId, @Now, @AdminUserId, @Now, NULL
    FROM [Agent].[AgentProfile] profile;

    UPDATE profile
    SET CurrentPublishedVersionId = version.Id
    FROM [Agent].[AgentProfile] profile
    JOIN [Agent].[AgentProfileVersion] version
        ON version.AgentProfileId = profile.Id
       AND version.VersionNumber = 1;

    IF NOT EXISTS (SELECT 1 FROM [Identity].[User] WHERE UserName = N'demo.agentmanager')
        INSERT INTO [Identity].[User]
            (Id, OrganizationId, UserName, DisplayName, IsServiceIdentity, IsActive, CreatedAt)
        VALUES
            ('10000000-0000-0000-0000-000000000027', @OrgId,
             N'demo.agentmanager', N'研发数字员工负责人', 0, 1, @Now);

    IF NOT EXISTS (SELECT 1 FROM [Identity].[User] WHERE UserName = N'agent.lowrisk')
        INSERT INTO [Identity].[User]
            (Id, OrganizationId, UserName, DisplayName, IsServiceIdentity, IsActive, CreatedAt)
        VALUES
            ('10000000-0000-0000-0000-000000000028', @OrgId,
             N'agent.lowrisk', N'低风险治理演练数字员工', 1, 1, @Now);

    IF NOT EXISTS (SELECT 1 FROM [Identity].[User] WHERE UserName = N'agent.highrisk')
        INSERT INTO [Identity].[User]
            (Id, OrganizationId, UserName, DisplayName, IsServiceIdentity, IsActive, CreatedAt)
        VALUES
            ('10000000-0000-0000-0000-000000000029', @OrgId,
             N'agent.highrisk', N'高风险治理演练数字员工', 1, 1, @Now);

    IF NOT EXISTS (SELECT 1 FROM [Identity].[Role] WHERE Code = N'agent-department-manager')
        INSERT INTO [Identity].[Role]
            (Id, Code, Name, RoleType, Description, IsSystem, IsActive, Version, CreatedAt, UpdatedAt)
        VALUES
            ('2a000000-0000-0000-0000-000000000001', N'agent-department-manager',
             N'数字员工部门负责人', N'Business', N'授权部门内管理数字员工；高风险发布需平台复核。',
             0, 1, 1, @Now, @Now);

    IF NOT EXISTS (SELECT 1 FROM [Identity].[Role] WHERE Code = N'agent-service')
        INSERT INTO [Identity].[Role]
            (Id, Code, Name, RoleType, Description, IsSystem, IsActive, Version, CreatedAt, UpdatedAt)
        VALUES
            ('2a000000-0000-0000-0000-000000000002', N'agent-service',
             N'数字员工服务角色', N'AgentService', N'最小业务读取与流程执行权限。',
             0, 1, 1, @Now, @Now);

    INSERT INTO [Identity].[Permission] (Id, Code, Resource, Action)
    SELECT seed.Id, seed.Code, seed.Resource, seed.Action
    FROM (VALUES
        (CONVERT(uniqueidentifier, '3a000000-0000-0000-0000-000000000001'), N'agent-create', N'agent', N'Create'),
        (CONVERT(uniqueidentifier, '3a000000-0000-0000-0000-000000000002'), N'agent-update', N'agent', N'Update'),
        (CONVERT(uniqueidentifier, '3a000000-0000-0000-0000-000000000003'), N'agent-manage', N'agent', N'Manage'),
        (CONVERT(uniqueidentifier, '3a000000-0000-0000-0000-000000000004'), N'agent-approve', N'agent', N'Approve')
    ) seed(Id, Code, Resource, Action)
    WHERE NOT EXISTS (
        SELECT 1 FROM [Identity].[Permission] existing
        WHERE existing.Resource = seed.Resource AND existing.Action = seed.Action
    );

    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT roleRow.Id, permissionRow.Id
    FROM [Identity].[Role] roleRow
    JOIN [Identity].[Permission] permissionRow
        ON permissionRow.Resource = N'agent'
       AND permissionRow.Action IN (N'Read', N'Create', N'Update', N'Manage', N'Approve')
    WHERE roleRow.Code IN (N'platform-admin', N'agent-department-manager', N'project-manager')
      AND NOT EXISTS (
          SELECT 1 FROM [Identity].[RolePermission] existing
          WHERE existing.RoleId = roleRow.Id
            AND existing.PermissionId = permissionRow.Id);

    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT roleRow.Id, permissionRow.Id
    FROM [Identity].[Role] roleRow
    JOIN [Identity].[Permission] permissionRow
        ON permissionRow.Code IN (
            N'agent-read', N'workflow-instance-read', N'workflow-task-read',
            N'workflow-task-update', N'workflow-task-approve', N'event-inbox-read',
            N'project-read', N'project-task-read', N'project-task-create',
        N'project-task-update', N'audit-read', N'file-read', N'department-read')
    WHERE roleRow.Code IN (N'agent-service', N'agent-department-manager')
      AND NOT EXISTS (
          SELECT 1 FROM [Identity].[RolePermission] existing
          WHERE existing.RoleId = roleRow.Id
            AND existing.PermissionId = permissionRow.Id);

    INSERT INTO [Identity].[UserRole] (UserId, RoleId)
    SELECT userRow.Id, roleRow.Id
    FROM [Identity].[User] userRow
    JOIN [Identity].[Role] roleRow
        ON roleRow.Code = CASE
            WHEN userRow.UserName = N'demo.agentmanager' THEN N'agent-department-manager'
            ELSE N'agent-service'
        END
    WHERE userRow.UserName IN (
        N'demo.agentmanager', N'agent.lowrisk', N'agent.highrisk',
        N'agent.demo', N'agent.proxy', N'agent.project')
      AND NOT EXISTS (
          SELECT 1 FROM [Identity].[UserRole] existing
          WHERE existing.UserId = userRow.Id
            AND existing.RoleId = roleRow.Id);

    INSERT INTO [Identity].[UserDepartmentMembership]
        (UserId, DepartmentId, IsPrimary, IsManager, IsActive, JoinedAt)
            SELECT userRow.Id, @RndDepartmentId, 1,
                   0,
                   1, @Now
    FROM [Identity].[User] userRow
    WHERE userRow.UserName IN (
        N'demo.agentmanager', N'agent.lowrisk', N'agent.highrisk',
        N'agent.demo', N'agent.proxy', N'agent.project')
      AND NOT EXISTS (
          SELECT 1 FROM [Identity].[UserDepartmentMembership] existing
          WHERE existing.UserId = userRow.Id
            AND existing.DepartmentId = @RndDepartmentId);

    INSERT INTO [Identity].[DataScope] (Id, UserId, ScopeType, ScopeReference)
    SELECT NEWID(), userRow.Id, N'organization', CONVERT(nvarchar(36), @OrgId)
    FROM [Identity].[User] userRow
    WHERE userRow.UserName IN (
        N'demo.agentmanager', N'agent.lowrisk', N'agent.highrisk',
        N'agent.demo', N'agent.proxy', N'agent.project')
      AND NOT EXISTS (
          SELECT 1 FROM [Identity].[DataScope] existing
          WHERE existing.UserId = userRow.Id
            AND existing.ScopeType = N'organization'
            AND existing.ScopeReference = CONVERT(nvarchar(36), @OrgId));

    INSERT INTO [Identity].[DataScope] (Id, UserId, ScopeType, ScopeReference)
    SELECT NEWID(), userRow.Id, N'department', CONVERT(nvarchar(36), @RndDepartmentId)
    FROM [Identity].[User] userRow
    WHERE userRow.UserName IN (
        N'demo.agentmanager', N'agent.lowrisk', N'agent.highrisk',
        N'agent.demo', N'agent.proxy', N'agent.project')
      AND NOT EXISTS (
          SELECT 1 FROM [Identity].[DataScope] existing
          WHERE existing.UserId = userRow.Id
            AND existing.ScopeType = N'department'
            AND existing.ScopeReference = CONVERT(nvarchar(36), @RndDepartmentId));
END;

GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    CREATE INDEX [IX_McpClient_AgentProfileId] ON [Mcp].[Client] ([AgentProfileId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    CREATE INDEX [IX_AgentProfile_BusinessOwnerUserId] ON [Agent].[AgentProfile] ([BusinessOwnerUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    CREATE INDEX [IX_AgentProfile_CreatedBy] ON [Agent].[AgentProfile] ([CreatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    CREATE INDEX [IX_AgentProfile_CurrentPublishedVersionId] ON [Agent].[AgentProfile] ([CurrentPublishedVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    CREATE INDEX [IX_AgentProfile_Department_Lifecycle] ON [Agent].[AgentProfile] ([DepartmentId], [LifecycleStatus], [IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    CREATE INDEX [IX_AgentProfile_Name] ON [Agent].[AgentProfile] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    CREATE INDEX [IX_AgentProfile_TechnicalOwnerUserId] ON [Agent].[AgentProfile] ([TechnicalOwnerUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    CREATE INDEX [IX_AgentProfile_UpdatedBy] ON [Agent].[AgentProfile] ([UpdatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    CREATE UNIQUE INDEX [UX_AgentProfile_Code] ON [Agent].[AgentProfile] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    EXEC(N'ALTER TABLE [Agent].[AgentProfile] ADD CONSTRAINT [CK_AgentProfile_AllowedToolsJson] CHECK (ISJSON([AllowedToolsJson]) = 1)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    EXEC(N'ALTER TABLE [Agent].[AgentProfile] ADD CONSTRAINT [CK_AgentProfile_ConfirmationsJson] CHECK (ISJSON([RequiredConfirmationsJson]) = 1)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    EXEC(N'ALTER TABLE [Agent].[AgentProfile] ADD CONSTRAINT [CK_AgentProfile_DataScopesJson] CHECK (ISJSON([DataScopesJson]) = 1)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    EXEC(N'ALTER TABLE [Agent].[AgentProfile] ADD CONSTRAINT [CK_AgentProfile_Lifecycle] CHECK ([LifecycleStatus] IN (''Draft'',''Published'',''Disabled'',''Retired''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    EXEC(N'ALTER TABLE [Agent].[AgentProfile] ADD CONSTRAINT [CK_AgentProfile_ModelScopesJson] CHECK (ISJSON([ModelScopesJson]) = 1)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    EXEC(N'ALTER TABLE [Agent].[AgentProfile] ADD CONSTRAINT [CK_AgentProfile_StopConditionsJson] CHECK (ISJSON([StopConditionsJson]) = 1)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    CREATE INDEX [IX_AccessToken_RevokedBy] ON [Mcp].[AccessToken] ([RevokedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    CREATE INDEX [IX_McpAccessToken_AgentProfile_Active] ON [Mcp].[AccessToken] ([AgentProfileId], [RevokedAt], [ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    EXEC(N'ALTER TABLE [Identity].[AccessChangeRequest] ADD CONSTRAINT [CK_AccessChangeRequest_Type] CHECK ([RequestType] IN (''RoleAssignment'',''RolePermissions'',''AgentProfilePublish''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    CREATE INDEX [IX_AgentProfileVersion_BusinessOwnerUserId] ON [Agent].[AgentProfileVersion] ([BusinessOwnerUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    CREATE INDEX [IX_AgentProfileVersion_CreatedBy] ON [Agent].[AgentProfileVersion] ([CreatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    CREATE INDEX [IX_AgentProfileVersion_Department_Status_UpdatedAt] ON [Agent].[AgentProfileVersion] ([DepartmentId], [Status], [UpdatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    CREATE INDEX [IX_AgentProfileVersion_PublishedBy] ON [Agent].[AgentProfileVersion] ([PublishedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    CREATE INDEX [IX_AgentProfileVersion_ServiceUserId] ON [Agent].[AgentProfileVersion] ([ServiceUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    CREATE INDEX [IX_AgentProfileVersion_TechnicalOwnerUserId] ON [Agent].[AgentProfileVersion] ([TechnicalOwnerUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    CREATE INDEX [IX_AgentProfileVersion_UpdatedBy] ON [Agent].[AgentProfileVersion] ([UpdatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    CREATE INDEX [IX_AgentProfileVersion_ValidatedBy] ON [Agent].[AgentProfileVersion] ([ValidatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_AgentProfileVersion_Profile_Published] ON [Agent].[AgentProfileVersion] ([AgentProfileId]) WHERE [Status] = ''Published''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    CREATE UNIQUE INDEX [UX_AgentProfileVersion_Profile_Version] ON [Agent].[AgentProfileVersion] ([AgentProfileId], [VersionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Mcp].[AccessToken] ADD CONSTRAINT [FK_McpAccessToken_AgentProfile] FOREIGN KEY ([AgentProfileId]) REFERENCES [Agent].[AgentProfile] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Mcp].[AccessToken] ADD CONSTRAINT [FK_McpAccessToken_RevokedBy] FOREIGN KEY ([RevokedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Agent].[AgentProfile] ADD CONSTRAINT [FK_AgentProfile_BusinessOwner] FOREIGN KEY ([BusinessOwnerUserId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Agent].[AgentProfile] ADD CONSTRAINT [FK_AgentProfile_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Agent].[AgentProfile] ADD CONSTRAINT [FK_AgentProfile_CurrentPublishedVersion] FOREIGN KEY ([CurrentPublishedVersionId]) REFERENCES [Agent].[AgentProfileVersion] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Agent].[AgentProfile] ADD CONSTRAINT [FK_AgentProfile_Department] FOREIGN KEY ([DepartmentId]) REFERENCES [Identity].[Department] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Agent].[AgentProfile] ADD CONSTRAINT [FK_AgentProfile_TechnicalOwner] FOREIGN KEY ([TechnicalOwnerUserId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Agent].[AgentProfile] ADD CONSTRAINT [FK_AgentProfile_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    ALTER TABLE [Mcp].[Client] ADD CONSTRAINT [FK_McpClient_AgentProfile] FOREIGN KEY ([AgentProfileId]) REFERENCES [Agent].[AgentProfile] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261103004434_015_AgentProfileGovernance', N'10.0.14');
END;

COMMIT;
GO

