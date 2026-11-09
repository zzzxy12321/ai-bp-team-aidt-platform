BEGIN TRANSACTION;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD [DepartmentId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD [EnvironmentCode] nvarchar(16) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD [IsTestData] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowInstance] ADD [DepartmentId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowInstance] ADD [EnvironmentCode] nvarchar(16) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowInstance] ADD [IsTestData] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowInstance] ADD [SourceSystem] nvarchar(64) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowInstance] ADD [StartedByUserId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[DocumentVersion] ADD [MimeType] nvarchar(128) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[DocumentVersion] ADD [OriginalFileName] nvarchar(255) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[DocumentVersion] ADD [Sensitivity] nvarchar(32) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[DocumentVersion] ADD [SizeBytes] bigint NOT NULL DEFAULT CAST(0 AS bigint);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[DocumentVersion] ADD [VirusScanStatus] nvarchar(32) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [BusinessObject].[BusinessObject] ADD [DepartmentId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [BusinessObject].[BusinessObject] ADD [EnvironmentCode] nvarchar(16) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [BusinessObject].[BusinessObject] ADD [IsTestData] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [BusinessObject].[BusinessObject] ADD [SourceSystem] nvarchar(64) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[Attachment] ADD [CurrentVersionNumber] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[Attachment] ADD [DeletionReason] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[Attachment] ADD [DeletionRequestedAt] datetime2(3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[Attachment] ADD [DeletionRequestedBy] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[Attachment] ADD [DepartmentId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[Attachment] ADD [EnvironmentCode] nvarchar(16) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[Attachment] ADD [IsTestData] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[Attachment] ADD [OrganizationId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[Attachment] ADD [SourceSystem] nvarchar(64) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[Attachment] ADD [Status] nvarchar(32) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[Attachment] ADD [UpdatedAt] datetime2(3) NOT NULL DEFAULT '0001-01-01T00:00:00.000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    CREATE TABLE [Document].[AttachmentLink] (
        [Id] uniqueidentifier NOT NULL,
        [AttachmentId] uniqueidentifier NOT NULL,
        [EntityType] nvarchar(64) NOT NULL,
        [EntityId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_AttachmentLink] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_AttachmentLink_EntityType] CHECK ([EntityType] IN ('Department','BusinessObject','WorkflowInstance','WorkflowTask')),
        CONSTRAINT [FK_AttachmentLink_Attachment] FOREIGN KEY ([AttachmentId]) REFERENCES [Document].[Attachment] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    CREATE TABLE [Identity].[Department] (
        [Id] uniqueidentifier NOT NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [Code] nvarchar(64) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [ManagerUserId] uniqueidentifier NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_Department] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Department_Code] CHECK (LEN([Code]) > 0),
        CONSTRAINT [CK_Department_Name] CHECK (LEN([Name]) > 0),
        CONSTRAINT [FK_Department_Manager] FOREIGN KEY ([ManagerUserId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Department_Organization] FOREIGN KEY ([OrganizationId]) REFERENCES [Identity].[Organization] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    CREATE TABLE [Identity].[UserDepartmentMembership] (
        [UserId] uniqueidentifier NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [IsPrimary] bit NOT NULL,
        [IsManager] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [JoinedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_UserDepartmentMembership] PRIMARY KEY ([UserId], [DepartmentId]),
        CONSTRAINT [CK_UserDepartmentMembership_PrimaryActive] CHECK ([IsPrimary] = 0 OR [IsActive] = 1),
        CONSTRAINT [FK_UserDepartmentMembership_Department] FOREIGN KEY ([DepartmentId]) REFERENCES [Identity].[Department] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserDepartmentMembership_User] FOREIGN KEY ([UserId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION
    );
END;

-- Newly added columns must be visible to the backfill batch at compile time.
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    DECLARE @Now datetime2(3) = SYSUTCDATETIME();

    INSERT INTO [Identity].[Department]
        (Id, OrganizationId, Code, Name, ManagerUserId, IsActive, CreatedAt, UpdatedAt)
    SELECT
        NEWID(),
        organizationRow.Id,
        departmentSeed.Code,
        departmentSeed.Name,
        NULL,
        1,
        @Now,
        @Now
    FROM [Identity].[Organization] organizationRow
    CROSS APPLY (VALUES
        (N'RND', N'研发'),
        (N'QA', N'质量'),
        (N'SUPPLY', N'供应链'),
        (N'FINANCE', N'财务'),
        (N'PRODUCTION', N'生产'),
        (N'PROJECT', N'项目')
    ) departmentSeed(Code, Name)
    WHERE NOT EXISTS (
        SELECT 1
        FROM [Identity].[Department] existing
        WHERE existing.OrganizationId = organizationRow.Id
          AND existing.Code = departmentSeed.Code
    );

    INSERT INTO [Identity].[UserDepartmentMembership]
        (UserId, DepartmentId, IsPrimary, IsManager, IsActive, JoinedAt)
    SELECT
        userRow.Id,
        departmentRow.Id,
        1,
        membershipSeed.IsManager,
        1,
        @Now
    FROM (VALUES
        (N'demo.admin', N'RND', 1),
        (N'demo.researcher', N'RND', 0),
        (N'demo.qa', N'QA', 1),
        (N'demo.supply', N'SUPPLY', 1),
        (N'demo.finance', N'FINANCE', 1),
        (N'demo.production', N'PRODUCTION', 1)
    ) membershipSeed(UserName, DepartmentCode, IsManager)
    JOIN [Identity].[User] userRow
        ON userRow.UserName = membershipSeed.UserName
    JOIN [Identity].[Department] departmentRow
        ON departmentRow.OrganizationId = userRow.OrganizationId
       AND departmentRow.Code = membershipSeed.DepartmentCode
    WHERE NOT EXISTS (
        SELECT 1
        FROM [Identity].[UserDepartmentMembership] existing
        WHERE existing.UserId = userRow.Id
          AND existing.DepartmentId = departmentRow.Id
    );

    INSERT INTO [Identity].[UserDepartmentMembership]
        (UserId, DepartmentId, IsPrimary, IsManager, IsActive, JoinedAt)
    SELECT
        userRow.Id,
        departmentRow.Id,
        1,
        0,
        1,
        @Now
    FROM [Identity].[User] userRow
    JOIN [Identity].[Department] departmentRow
        ON departmentRow.OrganizationId = userRow.OrganizationId
       AND departmentRow.Code = N'RND'
    WHERE userRow.IsActive = 1
      AND NOT EXISTS (
          SELECT 1
          FROM [Identity].[UserDepartmentMembership] existing
          WHERE existing.UserId = userRow.Id
            AND existing.IsActive = 1
      );

    UPDATE membershipRow
    SET membershipRow.IsManager = 1
    FROM [Identity].[UserDepartmentMembership] membershipRow
    WHERE membershipRow.IsActive = 1
      AND EXISTS (
          SELECT 1
          FROM [Identity].[User] userRow
          WHERE userRow.Id = membershipRow.UserId
            AND userRow.UserName IN (N'demo.admin', N'demo.qa', N'demo.supply', N'demo.finance', N'demo.production')
      );

    UPDATE departmentRow
    SET departmentRow.ManagerUserId = membershipRow.UserId,
        departmentRow.UpdatedAt = @Now
    FROM [Identity].[Department] departmentRow
    JOIN [Identity].[UserDepartmentMembership] membershipRow
        ON membershipRow.DepartmentId = departmentRow.Id
       AND membershipRow.IsManager = 1
       AND membershipRow.IsActive = 1;

    INSERT INTO [Identity].[DataScope] (Id, UserId, ScopeType, ScopeReference)
    SELECT NEWID(), membershipRow.UserId, N'department', CONVERT(nvarchar(36), membershipRow.DepartmentId)
    FROM [Identity].[UserDepartmentMembership] membershipRow
    WHERE membershipRow.IsActive = 1
      AND NOT EXISTS (
          SELECT 1
          FROM [Identity].[DataScope] existing
          WHERE existing.UserId = membershipRow.UserId
            AND existing.ScopeType = N'department'
            AND existing.ScopeReference = CONVERT(nvarchar(36), membershipRow.DepartmentId)
      );

    INSERT INTO [Identity].[Permission] (Id, Code, Resource, Action)
    SELECT permissionSeed.Id, permissionSeed.Code, permissionSeed.Resource, permissionSeed.Action
    FROM (VALUES
        (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000021'), N'department-read', N'department', N'Read'),
        (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000022'), N'department-create', N'department', N'Create'),
        (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000023'), N'department-update', N'department', N'Update'),
        (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000024'), N'file-read', N'file', N'Read'),
        (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000025'), N'file-create', N'file', N'Create'),
        (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000026'), N'file-update', N'file', N'Update'),
        (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000027'), N'file-delete', N'file', N'Delete')
    ) permissionSeed(Id, Code, Resource, Action)
    WHERE NOT EXISTS (
        SELECT 1
        FROM [Identity].[Permission] existing
        WHERE existing.Code = permissionSeed.Code
           OR (existing.Resource = permissionSeed.Resource AND existing.Action = permissionSeed.Action)
    );

    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT roleRow.Id, permissionRow.Id
    FROM [Identity].[Role] roleRow
    JOIN [Identity].[Permission] permissionRow
        ON permissionRow.Code IN
        (
            N'department-read',
            N'file-read',
            N'file-create',
            N'file-update',
            N'file-delete'
        )
    WHERE roleRow.Code IN
        (
            N'platform-admin',
            N'rd-engineer',
            N'quality-engineer',
            N'supply-specialist',
            N'finance-approver',
            N'production-team'
        )
      AND NOT EXISTS (
          SELECT 1
          FROM [Identity].[RolePermission] existing
          WHERE existing.RoleId = roleRow.Id
            AND existing.PermissionId = permissionRow.Id
      );

    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT roleRow.Id, permissionRow.Id
    FROM [Identity].[Role] roleRow
    JOIN [Identity].[Permission] permissionRow
        ON permissionRow.Code IN (N'department-create', N'department-update')
    WHERE roleRow.Code = N'platform-admin'
      AND NOT EXISTS (
          SELECT 1
          FROM [Identity].[RolePermission] existing
          WHERE existing.RoleId = roleRow.Id
            AND existing.PermissionId = permissionRow.Id
      );

    UPDATE businessObject
    SET businessObject.DepartmentId = COALESCE(departmentRow.Id, defaultDepartment.Id),
        businessObject.EnvironmentCode = N'demo',
        businessObject.IsTestData = 1,
        businessObject.SourceSystem = N'AIDT-SEED'
    FROM [BusinessObject].[BusinessObject] businessObject
    LEFT JOIN [Metadata].[ObjectType] objectType
        ON objectType.Id = businessObject.ObjectTypeId
    OUTER APPLY (
        SELECT TOP (1) candidate.Id
        FROM [Identity].[Department] candidate
        WHERE candidate.OrganizationId = businessObject.OrganizationId
          AND candidate.Code = CASE objectType.Code
              WHEN N'CUSTOMER_SAMPLE' THEN N'RND'
              WHEN N'QUALITY_DEVIATION' THEN N'QA'
              WHEN N'CUSTOMER_COMPLAINT' THEN N'QA'
              WHEN N'PURCHASE_REQUEST' THEN N'SUPPLY'
              ELSE N'RND'
          END
    ) departmentRow
    OUTER APPLY (
        SELECT TOP (1) fallback.Id
        FROM [Identity].[Department] fallback
        WHERE fallback.OrganizationId = businessObject.OrganizationId
        ORDER BY fallback.Code
    ) defaultDepartment;

    UPDATE workflowInstance
    SET DepartmentId = businessObject.DepartmentId,
        EnvironmentCode = N'demo',
        IsTestData = 1,
        SourceSystem = N'AIDT-SEED',
        StartedByUserId = NULL
    FROM [Workflow].[WorkflowInstance] workflowInstance
    JOIN [BusinessObject].[BusinessObject] businessObject
        ON businessObject.Id = workflowInstance.BusinessObjectId;

    UPDATE workflowTask
    SET DepartmentId = COALESCE(assigneeDepartment.Id, workflowInstance.DepartmentId),
        EnvironmentCode = workflowInstance.EnvironmentCode,
        IsTestData = workflowInstance.IsTestData
    FROM [Workflow].[WorkflowTask] workflowTask
    JOIN [Workflow].[WorkflowInstance] workflowInstance
        ON workflowInstance.Id = workflowTask.WorkflowInstanceId
    OUTER APPLY (
        SELECT TOP (1) departmentRow.Id
        FROM [Identity].[UserDepartmentMembership] membershipRow
        JOIN [Identity].[Department] departmentRow
            ON departmentRow.Id = membershipRow.DepartmentId
        WHERE membershipRow.UserId = workflowTask.AssigneeId
          AND membershipRow.IsActive = 1
          AND departmentRow.IsActive = 1
        ORDER BY membershipRow.IsPrimary DESC, membershipRow.JoinedAt
    ) assigneeDepartment;

    UPDATE attachment
    SET OrganizationId = COALESCE(
            businessObject.OrganizationId,
            (SELECT TOP (1) organizationRow.Id FROM [Identity].[Organization] organizationRow ORDER BY organizationRow.Id)
        ),
        DepartmentId = COALESCE(
            businessObject.DepartmentId,
            (SELECT TOP (1) departmentRow.Id FROM [Identity].[Department] departmentRow ORDER BY departmentRow.Code)
        ),
        EnvironmentCode = N'demo',
        IsTestData = 1,
        SourceSystem = N'AIDT-SEED',
        Status = N'Active',
        Sensitivity = CASE
            WHEN attachment.Sensitivity IN (N'Internal', N'Sensitive', N'Core') THEN attachment.Sensitivity
            ELSE N'Internal'
        END,
        VirusScanStatus = CASE
            WHEN attachment.VirusScanStatus IN (N'NotConfigured', N'PendingManualReview', N'Clean', N'Infected')
                THEN attachment.VirusScanStatus
            ELSE N'NotConfigured'
        END,
        CurrentVersionNumber = COALESCE((
            SELECT MAX(documentVersion.VersionNumber)
            FROM [Document].[DocumentVersion] documentVersion
            WHERE documentVersion.DocumentId = attachment.Id
        ), 1),
        UpdatedAt = COALESCE(attachment.UpdatedAt, attachment.CreatedAt)
    FROM [Document].[Attachment] attachment
    LEFT JOIN [BusinessObject].[BusinessObject] businessObject
        ON businessObject.Id = attachment.BusinessObjectId;

    UPDATE attachment
    SET UpdatedAt = CreatedAt
    FROM [Document].[Attachment] attachment
    WHERE attachment.UpdatedAt < CONVERT(datetime2(3), '19000101');

    UPDATE documentVersion
    SET OriginalFileName = attachment.OriginalFileName,
        MimeType = attachment.MimeType,
        SizeBytes = CASE WHEN attachment.SizeBytes > 0 THEN attachment.SizeBytes ELSE 1 END,
        VirusScanStatus = attachment.VirusScanStatus,
        Sensitivity = attachment.Sensitivity
    FROM [Document].[DocumentVersion] documentVersion
    JOIN [Document].[Attachment] attachment
        ON attachment.Id = documentVersion.DocumentId;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    CREATE INDEX [IX_WorkflowTask_Department_Environment_Status_CreatedAt] ON [Workflow].[WorkflowTask] ([DepartmentId], [EnvironmentCode], [Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] ADD CONSTRAINT [CK_WorkflowTask_Environment] CHECK ([EnvironmentCode] IN (''demo'',''test'',''prod''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    CREATE INDEX [IX_WorkflowInstance_Department_Environment_StartedAt] ON [Workflow].[WorkflowInstance] ([DepartmentId], [EnvironmentCode], [StartedAt], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    EXEC(N'ALTER TABLE [Workflow].[WorkflowInstance] ADD CONSTRAINT [CK_WorkflowInstance_Environment] CHECK ([EnvironmentCode] IN (''demo'',''test'',''prod''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    EXEC(N'ALTER TABLE [Document].[DocumentVersion] ADD CONSTRAINT [CK_DocumentVersion_ScanStatus] CHECK ([VirusScanStatus] IN (''NotConfigured'',''PendingManualReview'',''Clean'',''Infected''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    EXEC(N'ALTER TABLE [Document].[DocumentVersion] ADD CONSTRAINT [CK_DocumentVersion_Sensitivity] CHECK ([Sensitivity] IN (''Internal'',''Sensitive'',''Core''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    EXEC(N'ALTER TABLE [Document].[DocumentVersion] ADD CONSTRAINT [CK_DocumentVersion_Size] CHECK ([SizeBytes] > 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    CREATE INDEX [IX_BusinessObject_Department_Environment_UpdatedAt] ON [BusinessObject].[BusinessObject] ([DepartmentId], [EnvironmentCode], [UpdatedAt], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    EXEC(N'ALTER TABLE [BusinessObject].[BusinessObject] ADD CONSTRAINT [CK_BusinessObject_Environment] CHECK ([EnvironmentCode] IN (''demo'',''test'',''prod''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    CREATE INDEX [IX_Attachment_Department_Environment_CreatedAt] ON [Document].[Attachment] ([DepartmentId], [EnvironmentCode], [CreatedAt], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    CREATE INDEX [IX_Attachment_Organization_Department] ON [Document].[Attachment] ([OrganizationId], [DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    EXEC(N'ALTER TABLE [Document].[Attachment] ADD CONSTRAINT [CK_Attachment_Environment] CHECK ([EnvironmentCode] IN (''demo'',''test'',''prod''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    EXEC(N'ALTER TABLE [Document].[Attachment] ADD CONSTRAINT [CK_Attachment_ScanStatus] CHECK ([VirusScanStatus] IN (''NotConfigured'',''PendingManualReview'',''Clean'',''Infected''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    EXEC(N'ALTER TABLE [Document].[Attachment] ADD CONSTRAINT [CK_Attachment_Sensitivity] CHECK ([Sensitivity] IN (''Internal'',''Sensitive'',''Core''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    EXEC(N'ALTER TABLE [Document].[Attachment] ADD CONSTRAINT [CK_Attachment_Size] CHECK ([SizeBytes] > 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    EXEC(N'ALTER TABLE [Document].[Attachment] ADD CONSTRAINT [CK_Attachment_Status] CHECK ([Status] IN (''Active'',''DeletionRequested''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    CREATE INDEX [IX_AttachmentLink_Entity_CreatedAt] ON [Document].[AttachmentLink] ([EntityType], [EntityId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    CREATE UNIQUE INDEX [UX_AttachmentLink_Attachment_Entity] ON [Document].[AttachmentLink] ([AttachmentId], [EntityType], [EntityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    CREATE INDEX [IX_Department_ManagerUserId] ON [Identity].[Department] ([ManagerUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    CREATE INDEX [IX_Department_Organization_Active] ON [Identity].[Department] ([OrganizationId], [IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Department_Organization_Code] ON [Identity].[Department] ([OrganizationId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    CREATE INDEX [IX_UserDepartmentMembership_Department_Active] ON [Identity].[UserDepartmentMembership] ([DepartmentId], [IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    CREATE INDEX [IX_UserDepartmentMembership_User_Primary] ON [Identity].[UserDepartmentMembership] ([UserId], [IsPrimary]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_UserDepartmentMembership_User_ActivePrimary] ON [Identity].[UserDepartmentMembership] ([UserId]) WHERE [IsPrimary] = 1 AND [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[Attachment] ADD CONSTRAINT [FK_Attachment_Department] FOREIGN KEY ([DepartmentId]) REFERENCES [Identity].[Department] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[Attachment] ADD CONSTRAINT [FK_Attachment_Organization] FOREIGN KEY ([OrganizationId]) REFERENCES [Identity].[Organization] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [BusinessObject].[BusinessObject] ADD CONSTRAINT [FK_BusinessObject_Department] FOREIGN KEY ([DepartmentId]) REFERENCES [Identity].[Department] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Document].[DocumentVersion] ADD CONSTRAINT [FK_DocumentVersion_Attachment] FOREIGN KEY ([DocumentId]) REFERENCES [Document].[Attachment] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowInstance] ADD CONSTRAINT [FK_WorkflowInstance_Department] FOREIGN KEY ([DepartmentId]) REFERENCES [Identity].[Department] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD CONSTRAINT [FK_WorkflowTask_Department] FOREIGN KEY ([DepartmentId]) REFERENCES [Identity].[Department] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030172128_003_DepartmentWorkspaceFilesAndEnvironment', N'10.0.14');
END;

COMMIT;
GO
