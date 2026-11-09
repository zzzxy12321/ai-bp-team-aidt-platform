IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD [FormDataJson] nvarchar(max) NOT NULL DEFAULT N'{}';
END;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD [FormDefinitionHash] nvarchar(64) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD [FormDefinitionJson] nvarchar(max) NOT NULL DEFAULT N'[]';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD [FormSchemaVersion] nvarchar(32) NOT NULL DEFAULT N'1.0';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD [NodeName] nvarchar(200) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD [AiReviewJson] nvarchar(max) NOT NULL DEFAULT N'{}';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD [BusinessDescription] nvarchar(2000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD [CompletionCondition] nvarchar(2000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD [DeliverablesJson] nvarchar(max) NOT NULL DEFAULT N'[]';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD [FormSchemaJson] nvarchar(max) NOT NULL DEFAULT N'[]';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD [FormSchemaVersion] nvarchar(32) NOT NULL DEFAULT N'1.0';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD [NodeActionsJson] nvarchar(max) NOT NULL DEFAULT N'[]';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD [NodeName] nvarchar(200) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD [NotificationPolicyJson] nvarchar(max) NOT NULL DEFAULT N'{}';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD [RelatedRoleCodesJson] nvarchar(max) NOT NULL DEFAULT N'[]';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowDefinition] ADD [ApplicableRoleCodesJson] nvarchar(max) NOT NULL DEFAULT N'[]';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowDefinition] ADD [CreatedBy] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowDefinition] ADD [DepartmentId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowDefinition] ADD [Description] nvarchar(2000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowDefinition] ADD [EnvironmentCode] nvarchar(16) NOT NULL DEFAULT N'demo';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowDefinition] ADD [IsDefaultForDepartment] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowDefinition] ADD [IsTestData] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowDefinition] ADD [ObjectTypeCode] nvarchar(80) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowDefinition] ADD [RequiresPlatformApproval] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowDefinition] ADD [RiskLevel] nvarchar(16) NOT NULL DEFAULT N'Low';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowDefinition] ADD [UpdatedAt] datetime2(3) NOT NULL DEFAULT '0001-01-01T00:00:00.000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowDefinition] ADD [UpdatedBy] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowDefinition] ADD [Version] bigint NOT NULL DEFAULT CAST(0 AS bigint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Navigation].[NavigationWorkspace] ADD [EnvironmentCode] nvarchar(16) NOT NULL DEFAULT N'demo';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Navigation].[NavigationWorkspace] ADD [IsDefault] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Navigation].[NavigationWorkspace] ADD [IsDemoData] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Navigation].[NavigationWorkspace] ADD [IsTestData] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] ADD [ApplicableRoleCodesJson] nvarchar(max) NOT NULL DEFAULT N'[]';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] ADD [EnvironmentCode] nvarchar(16) NOT NULL DEFAULT N'demo';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] ADD [IsDefault] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] ADD [IsTestData] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] ADD [RequiresPlatformApproval] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] ADD [RiskLevel] nvarchar(16) NOT NULL DEFAULT N'Low';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentRun]') AND [c].[name] = N'RiskLevel');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentRun] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Agent].[AgentRun] ADD DEFAULT N'Low' FOR [RiskLevel];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    CREATE INDEX [IX_WorkflowTask_Instance_Node_CreatedAt] ON [Workflow].[WorkflowTask] ([WorkflowInstanceId], [NodeCode], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] ADD CONSTRAINT [CK_WorkflowTask_FormDataJson] CHECK (ISJSON([FormDataJson]) = 1)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] ADD CONSTRAINT [CK_WorkflowTask_FormDefinitionJson] CHECK (ISJSON([FormDefinitionJson]) = 1)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    EXEC(N'ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD CONSTRAINT [CK_WorkflowNodePolicy_AiReviewJson] CHECK (ISJSON([AiReviewJson]) = 1)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    EXEC(N'ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD CONSTRAINT [CK_WorkflowNodePolicy_DeliverablesJson] CHECK (ISJSON([DeliverablesJson]) = 1)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    EXEC(N'ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD CONSTRAINT [CK_WorkflowNodePolicy_FormSchemaJson] CHECK (ISJSON([FormSchemaJson]) = 1)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    EXEC(N'ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD CONSTRAINT [CK_WorkflowNodePolicy_NodeActionsJson] CHECK (ISJSON([NodeActionsJson]) = 1)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    EXEC(N'ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD CONSTRAINT [CK_WorkflowNodePolicy_NotificationJson] CHECK (ISJSON([NotificationPolicyJson]) = 1)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    EXEC(N'ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD CONSTRAINT [CK_WorkflowNodePolicy_RelatedRolesJson] CHECK (ISJSON([RelatedRoleCodesJson]) = 1)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    CREATE INDEX [IX_WorkflowDefinition_CreatedBy] ON [Workflow].[WorkflowDefinition] ([CreatedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    CREATE INDEX [IX_WorkflowDefinition_Department_Object_Default_Risk] ON [Workflow].[WorkflowDefinition] ([DepartmentId], [ObjectTypeCode], [IsDefaultForDepartment], [RiskLevel], [IsTestData]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    CREATE INDEX [IX_WorkflowDefinition_UpdatedBy] ON [Workflow].[WorkflowDefinition] ([UpdatedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_WorkflowDefinition_ActiveDepartmentDefault] ON [Workflow].[WorkflowDefinition] ([DepartmentId], [ObjectTypeCode]) WHERE [IsDefaultForDepartment] = 1 AND [IsTestData] = 0 AND [Status] = ''Published''');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    EXEC(N'ALTER TABLE [Workflow].[WorkflowDefinition] ADD CONSTRAINT [CK_WorkflowDefinition_ApplicableRolesJson] CHECK (ISJSON([ApplicableRoleCodesJson]) = 1)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    EXEC(N'ALTER TABLE [Workflow].[WorkflowDefinition] ADD CONSTRAINT [CK_WorkflowDefinition_Environment] CHECK ([EnvironmentCode] IN (''demo'',''test'',''prod''))');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    EXEC(N'ALTER TABLE [Workflow].[WorkflowDefinition] ADD CONSTRAINT [CK_WorkflowDefinition_RiskLevel] CHECK ([RiskLevel] IN (''Low'',''Medium'',''High''))');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    CREATE INDEX [IX_NavigationWorkspace_Org_Test_Status] ON [Navigation].[NavigationWorkspace] ([OrganizationId], [IsTestData], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_NavigationWorkspace_ActiveDepartmentDefault] ON [Navigation].[NavigationWorkspace] ([OrganizationId], [DepartmentId]) WHERE [IsDefault] = 1 AND [IsTestData] = 0 AND [Status] = ''Published'' AND [DepartmentId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    EXEC(N'ALTER TABLE [Navigation].[NavigationWorkspace] ADD CONSTRAINT [CK_NavigationWorkspace_Environment] CHECK ([EnvironmentCode] IN (''demo'',''test'',''prod''))');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    CREATE INDEX [IX_ConfigurationVersion_Org_Dept_Test_Env_Updated] ON [Configuration].[ConfigurationVersion] ([OrganizationId], [DepartmentId], [IsTestData], [EnvironmentCode], [UpdatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    EXEC(N'ALTER TABLE [Configuration].[ConfigurationVersion] ADD CONSTRAINT [CK_ConfigurationVersion_ApplicableRolesJson] CHECK (ISJSON([ApplicableRoleCodesJson]) = 1)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    EXEC(N'ALTER TABLE [Configuration].[ConfigurationVersion] ADD CONSTRAINT [CK_ConfigurationVersion_Environment] CHECK ([EnvironmentCode] IN (''demo'',''test'',''prod''))');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    EXEC(N'ALTER TABLE [Configuration].[ConfigurationVersion] ADD CONSTRAINT [CK_ConfigurationVersion_RiskLevel] CHECK ([RiskLevel] IN (''Low'',''Medium'',''High''))');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowDefinition] ADD CONSTRAINT [FK_WorkflowDefinition_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowDefinition] ADD CONSTRAINT [FK_WorkflowDefinition_Department] FOREIGN KEY ([DepartmentId]) REFERENCES [Identity].[Department] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowDefinition] ADD CONSTRAINT [FK_WorkflowDefinition_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    SET ANSI_NULLS ON;
    SET QUOTED_IDENTIFIER ON;

    UPDATE [Navigation].[NavigationWorkspace]
    SET [EnvironmentCode] = N'demo',
        [IsTestData] = CASE
            WHEN [WorkspaceKey] LIKE N'NAV-E2E-%'
              OR [Name] LIKE N'%验收%'
              OR [Name] LIKE N'%E2E%'
              OR [Name] LIKE N'%测试%'
              OR [Name] LIKE N'%验证%'
            THEN 1 ELSE 0 END,
        [IsDefault] = 0,
        [IsDemoData] = CASE
            WHEN [WorkspaceKey] = N'DEPT-DELIVERY' THEN 1 ELSE 0 END;

    UPDATE [Navigation].[NavigationWorkspace]
    SET [Status] = N'Retired',
        [RetiredAt] = COALESCE([RetiredAt], SYSUTCDATETIME())
    WHERE [IsTestData] = 1
      AND [WorkspaceKey] <> N'SYSTEM';

    UPDATE [Configuration].[ConfigurationVersion]
    SET [IsTestData] = 1,
        [EnvironmentCode] = N'demo',
        [RiskLevel] = CASE
            WHEN [RiskLevel] IN ('Low','Medium','High') THEN [RiskLevel]
            ELSE N'Low' END,
        [ApplicableRoleCodesJson] = CASE
            WHEN ISJSON([ApplicableRoleCodesJson]) = 1
            THEN [ApplicableRoleCodesJson] ELSE N'[]' END
    WHERE [ConfigurationType] = N'Workflow'
      AND [ConfigurationKey] <> N'SYSTEM.WORKER.HEARTBEAT';

    UPDATE [Configuration].[ConfigurationVersion]
    SET [State] = N'Retired'
    WHERE [IsTestData] = 1
      AND [State] IN (N'Draft', N'Validated', N'Published');

    UPDATE definition
    SET [DepartmentId] = sourceDefinition.[DepartmentId],
        [ObjectTypeCode] = COALESCE(
            NULLIF(JSON_VALUE(versionRow.[DefinitionJson], N'$.objectTypeCode'), N''),
            N'ANY'),
        [EnvironmentCode] = N'demo',
        [IsTestData] = 1,
        [RiskLevel] = N'Low',
        [ApplicableRoleCodesJson] = N'[]',
        [Version] = CASE WHEN [Version] < 1 THEN 1 ELSE [Version] END
    FROM [Workflow].[WorkflowDefinition] AS definition
    OUTER APPLY (
        SELECT TOP (1)
            configuration.[DepartmentId],
            configuration.[PublishedWorkflowVersionId]
        FROM [Configuration].[ConfigurationVersion] AS configuration
        WHERE configuration.[ConfigurationKey] = definition.[Code]
        ORDER BY configuration.[VersionNumber] DESC
    ) AS sourceDefinition
    OUTER APPLY (
        SELECT [DefinitionJson]
        FROM [Workflow].[WorkflowVersion]
        WHERE [Id] = sourceDefinition.[PublishedWorkflowVersionId]
    ) AS versionRow;

    UPDATE [Workflow].[WorkflowDefinition]
    SET [ObjectTypeCode] = N'ANY'
    WHERE ISNULL([ObjectTypeCode], N'') = N'';
    UPDATE [Workflow].[WorkflowDefinition]
    SET [EnvironmentCode] = N'demo'
    WHERE [EnvironmentCode] NOT IN ('demo','test','prod');
    UPDATE [Workflow].[WorkflowDefinition]
    SET [RiskLevel] = N'Low'
    WHERE [RiskLevel] NOT IN ('Low','Medium','High');
    UPDATE [Workflow].[WorkflowDefinition]
    SET [ApplicableRoleCodesJson] = N'[]'
    WHERE ISJSON([ApplicableRoleCodesJson]) <> 1;
    UPDATE [Workflow].[WorkflowDefinition]
    SET [UpdatedAt] = COALESCE(NULLIF([UpdatedAt], '0001-01-01'), SYSUTCDATETIME());
    UPDATE [Workflow].[WorkflowDefinition]
    SET [Version] = 1
    WHERE [Version] < 1;

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    SET ANSI_NULLS ON;
    SET QUOTED_IDENTIFIER ON;
    DECLARE @OrgId uniqueidentifier;
    DECLARE @AdminUserId uniqueidentifier;
    DECLARE @DeliveryDepartmentId uniqueidentifier = NEWID();
    DECLARE @DeliveryUserId uniqueidentifier = NEWID();
    DECLARE @FlowOwnerRoleId uniqueidentifier = NEWID();

    SELECT TOP (1) @OrgId = [Id]
    FROM [Identity].[Organization]
    WHERE [IsActive] = 1
    ORDER BY [CreatedAt];
    SELECT TOP (1) @AdminUserId = [Id]
    FROM [Identity].[User]
    WHERE [OrganizationId] = @OrgId AND [UserName] = N'demo.admin';

    IF @OrgId IS NULL OR @AdminUserId IS NULL
        THROW 51018, N'018 需要组织与 demo.admin 基础数据。', 1;

    IF NOT EXISTS (
        SELECT 1 FROM [Identity].[User]
        WHERE [OrganizationId] = @OrgId AND [UserName] = N'demo.delivery')
    BEGIN
        INSERT INTO [Identity].[User]
            ([Id], [OrganizationId], [UserName], [DisplayName],
             [IsServiceIdentity], [IsActive], [CreatedAt])
        VALUES
            (@DeliveryUserId, @OrgId, N'demo.delivery', N'交付专员（Demo）',
             0, 1, SYSUTCDATETIME());
    END
    ELSE
    BEGIN
        SELECT @DeliveryUserId = [Id]
        FROM [Identity].[User]
        WHERE [OrganizationId] = @OrgId AND [UserName] = N'demo.delivery';
        UPDATE [Identity].[User]
        SET [IsActive] = 1
        WHERE [Id] = @DeliveryUserId;
    END;

    IF NOT EXISTS (
        SELECT 1 FROM [Identity].[Department]
        WHERE [OrganizationId] = @OrgId AND [Code] = N'DELIVERY')
    BEGIN
        INSERT INTO [Identity].[Department]
            ([Id], [OrganizationId], [Code], [Name], [ParentDepartmentId],
             [HierarchyType], [Path], [SortOrder], [Version], [ManagerUserId],
             [IsActive], [CreatedAt], [UpdatedAt])
        VALUES
            (@DeliveryDepartmentId, @OrgId, N'DELIVERY', N'交付中心（Demo）',
             NULL, N'Department', N'/', 70, 1, @DeliveryUserId,
             1, SYSUTCDATETIME(), SYSUTCDATETIME());
    END;
    ELSE
    BEGIN
        SELECT @DeliveryDepartmentId = [Id]
        FROM [Identity].[Department]
        WHERE [OrganizationId] = @OrgId AND [Code] = N'DELIVERY';
        UPDATE [Identity].[Department]
        SET [Name] = N'交付中心（Demo）',
            [IsActive] = 1,
            [ManagerUserId] = @DeliveryUserId,
            [Version] = [Version] + 1,
            [UpdatedAt] = SYSUTCDATETIME()
        WHERE [Id] = @DeliveryDepartmentId;
    END;

    IF NOT EXISTS (
        SELECT 1 FROM [Identity].[UserDepartmentMembership]
        WHERE [UserId] = @DeliveryUserId
          AND [DepartmentId] = @DeliveryDepartmentId)
    BEGIN
        INSERT INTO [Identity].[UserDepartmentMembership]
            ([UserId], [DepartmentId], [IsPrimary], [IsManager], [IsActive], [JoinedAt])
        VALUES
            (@DeliveryUserId, @DeliveryDepartmentId, 1, 1, 1, SYSUTCDATETIME());
    END;

    IF NOT EXISTS (
        SELECT 1 FROM [Identity].[Role] WHERE [Code] = N'department-flow-owner')
    BEGIN
        INSERT INTO [Identity].[Role]
            ([Id], [Code], [Name], [RoleType], [Description],
             [IsSystem], [IsActive], [Version], [CreatedAt], [UpdatedAt])
        VALUES
            (@FlowOwnerRoleId, N'department-flow-owner', N'部门流程负责人',
             N'Business', N'仅维护授权部门普通流程；高风险与跨部门仍由平台审核。',
             0, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    END
    ELSE
    BEGIN
        SELECT @FlowOwnerRoleId = [Id]
        FROM [Identity].[Role]
        WHERE [Code] = N'department-flow-owner';
    END;

    INSERT INTO [Identity].[RolePermission] ([RoleId], [PermissionId])
    SELECT @FlowOwnerRoleId, permission.[Id]
    FROM [Identity].[Permission] AS permission
    WHERE (
        (permission.[Resource] = N'configuration'
         AND permission.[Action] IN (N'Read', N'Create', N'Update', N'Approve', N'Manage'))
        OR (permission.[Resource] = N'department' AND permission.[Action] = N'Read')
        OR (permission.[Resource] = N'dashboard' AND permission.[Action] = N'Read')
        OR (permission.[Resource] = N'event-inbox' AND permission.[Action] = N'Read')
        OR (permission.[Resource] IN (N'workflow-task', N'workflow-instance')
            AND permission.[Action] IN (N'Read', N'Submit', N'Approve', N'Return', N'Terminate', N'Update'))
        OR (permission.[Resource] IN
            (N'business-object', N'file', N'minutes', N'agent', N'scenario',
             N'workflow-role', N'audit', N'navigation')
            AND permission.[Action] = N'Read')
        OR (permission.[Resource] = N'agent-run'
            AND permission.[Action] IN (N'Read', N'Submit', N'Update'))
    )
    AND NOT EXISTS (
        SELECT 1 FROM [Identity].[RolePermission] existing
        WHERE existing.[RoleId] = @FlowOwnerRoleId
          AND existing.[PermissionId] = permission.[Id]);

    INSERT INTO [Identity].[UserRoleAssignment]
        ([Id], [UserId], [RoleId], [ScopeType], [ScopeReference],
         [IsActive], [AssignedBy], [CreatedAt])
    SELECT NEWID(), department.[ManagerUserId], @FlowOwnerRoleId,
           N'department', CONVERT(nvarchar(36), department.[Id]),
           1, @AdminUserId, SYSUTCDATETIME()
    FROM [Identity].[Department] AS department
    WHERE department.[OrganizationId] = @OrgId
      AND department.[IsActive] = 1
      AND department.[ManagerUserId] IS NOT NULL
      AND department.[Code] IN
          (N'FINANCE', N'SUPPLY', N'PRODUCTION', N'PROJECT', N'RND', N'QA', N'DELIVERY')
      AND NOT EXISTS (
          SELECT 1 FROM [Identity].[UserRoleAssignment] existing
          WHERE existing.[UserId] = department.[ManagerUserId]
            AND existing.[RoleId] = @FlowOwnerRoleId
            AND existing.[ScopeType] = N'department'
            AND existing.[ScopeReference] = CONVERT(nvarchar(36), department.[Id])
            AND existing.[IsActive] = 1);

    DECLARE @RndFlowOwnerDepartmentId uniqueidentifier;
    DECLARE @RndFlowOwnerUserId uniqueidentifier;
    SELECT @RndFlowOwnerDepartmentId = [Id]
    FROM [Identity].[Department]
    WHERE [OrganizationId] = @OrgId AND [Code] = N'RND';
    SELECT @RndFlowOwnerUserId = [Id]
    FROM [Identity].[User]
    WHERE [OrganizationId] = @OrgId AND [UserName] = N'demo.researcher';
    IF @RndFlowOwnerDepartmentId IS NOT NULL
       AND @RndFlowOwnerUserId IS NOT NULL
       AND NOT EXISTS (
           SELECT 1 FROM [Identity].[UserRoleAssignment]
           WHERE [UserId] = @RndFlowOwnerUserId
             AND [RoleId] = @FlowOwnerRoleId
             AND [ScopeType] = N'department'
             AND [ScopeReference] = CONVERT(nvarchar(36), @RndFlowOwnerDepartmentId)
             AND [IsActive] = 1)
    BEGIN
        INSERT INTO [Identity].[UserRoleAssignment]
            ([Id], [UserId], [RoleId], [ScopeType], [ScopeReference],
             [IsActive], [AssignedBy], [CreatedAt])
        VALUES
            (NEWID(), @RndFlowOwnerUserId, @FlowOwnerRoleId,
             N'department', CONVERT(nvarchar(36), @RndFlowOwnerDepartmentId),
             1, @AdminUserId, SYSUTCDATETIME());
    END;

    IF EXISTS (
        SELECT 1 FROM [Identity].[Department]
        WHERE [OrganizationId] = @OrgId AND [Code] = N'PROJECT' AND [ManagerUserId] IS NULL)
    BEGIN
        DECLARE @ProjectManagerId uniqueidentifier;
        DECLARE @ProjectDepartmentId uniqueidentifier;
        SELECT @ProjectManagerId = [Id]
        FROM [Identity].[User]
        WHERE [OrganizationId] = @OrgId AND [UserName] = N'demo.pm';
        SELECT @ProjectDepartmentId = [Id]
        FROM [Identity].[Department]
        WHERE [OrganizationId] = @OrgId AND [Code] = N'PROJECT';
        IF @ProjectManagerId IS NOT NULL
        BEGIN
            UPDATE [Identity].[Department]
            SET [ManagerUserId] = @ProjectManagerId,
                [Version] = [Version] + 1,
                [UpdatedAt] = SYSUTCDATETIME()
            WHERE [Id] = @ProjectDepartmentId;
            IF NOT EXISTS (
                SELECT 1 FROM [Identity].[UserDepartmentMembership]
                WHERE [UserId] = @ProjectManagerId
                  AND [DepartmentId] = @ProjectDepartmentId)
            BEGIN
                INSERT INTO [Identity].[UserDepartmentMembership]
                    ([UserId], [DepartmentId], [IsPrimary], [IsManager], [IsActive], [JoinedAt])
                VALUES
                    (@ProjectManagerId, @ProjectDepartmentId, 0, 1, 1, SYSUTCDATETIME());
            END;
            IF NOT EXISTS (
                SELECT 1 FROM [Identity].[UserRoleAssignment]
                WHERE [UserId] = @ProjectManagerId
                  AND [RoleId] = @FlowOwnerRoleId
                  AND [ScopeType] = N'department'
                  AND [ScopeReference] = CONVERT(nvarchar(36), @ProjectDepartmentId)
                  AND [IsActive] = 1)
            BEGIN
                INSERT INTO [Identity].[UserRoleAssignment]
                    ([Id], [UserId], [RoleId], [ScopeType], [ScopeReference],
                     [IsActive], [AssignedBy], [CreatedAt])
                VALUES
                    (NEWID(), @ProjectManagerId, @FlowOwnerRoleId,
                     N'department', CONVERT(nvarchar(36), @ProjectDepartmentId),
                     1, @AdminUserId, SYSUTCDATETIME());
            END;
        END;
    END;

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    SET ANSI_NULLS ON;
    SET QUOTED_IDENTIFIER ON;
    DECLARE @OrgId uniqueidentifier;
    DECLARE @AdminUserId uniqueidentifier;
    SELECT TOP (1) @OrgId = [Id]
    FROM [Identity].[Organization]
    WHERE [IsActive] = 1
    ORDER BY [CreatedAt];
    SELECT TOP (1) @AdminUserId = [Id]
    FROM [Identity].[User]
    WHERE [OrganizationId] = @OrgId AND [UserName] = N'demo.admin';
    IF @OrgId IS NULL OR @AdminUserId IS NULL
        THROW 51019, N'018 部门导航初始化缺少组织或平台管理员。', 1;
    DECLARE @DepartmentId uniqueidentifier;
    DECLARE @DepartmentCode nvarchar(64);
    DECLARE @DepartmentName nvarchar(128);
    DECLARE @WorkspaceId uniqueidentifier;
    DECLARE @VersionId uniqueidentifier;
    DECLARE departmentCursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT [Id], [Code], [Name]
        FROM [Identity].[Department]
        WHERE [OrganizationId] = @OrgId
          AND [IsActive] = 1
          AND [Code] IN
              (N'FINANCE', N'SUPPLY', N'PRODUCTION', N'PROJECT', N'RND', N'QA', N'DELIVERY')
        ORDER BY CASE [Code]
            WHEN N'FINANCE' THEN 10
            WHEN N'SUPPLY' THEN 20
            WHEN N'PRODUCTION' THEN 30
            WHEN N'PROJECT' THEN 40
            WHEN N'RND' THEN 50
            WHEN N'QA' THEN 60
            WHEN N'DELIVERY' THEN 70
            ELSE 100 END;
    OPEN departmentCursor;
    FETCH NEXT FROM departmentCursor INTO @DepartmentId, @DepartmentCode, @DepartmentName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @WorkspaceId = NEWID();
        SET @VersionId = NEWID();
        IF NOT EXISTS (
            SELECT 1 FROM [Navigation].[NavigationWorkspace]
            WHERE [OrganizationId] = @OrgId
              AND [DepartmentId] = @DepartmentId
              AND [IsDefault] = 1
              AND [IsTestData] = 0)
        BEGIN
            INSERT INTO [Navigation].[NavigationWorkspace]
                ([Id], [OrganizationId], [DepartmentId], [WorkspaceKey], [Name],
                 [WorkspaceType], [IsSystem], [IsDefault], [IsDemoData],
                 [EnvironmentCode], [IsTestData], [Status], [Version],
                 [CreatedBy], [CreatedAt], [UpdatedBy], [UpdatedAt])
            VALUES
                (@WorkspaceId, @OrgId, @DepartmentId,
                 CONCAT(N'DEPT-', @DepartmentCode), @DepartmentName,
                 N'DepartmentWorkspace', 0, 1,
                 CASE WHEN @DepartmentCode = N'DELIVERY' THEN 1 ELSE 0 END,
                 N'demo', 0, N'Published', 1, @AdminUserId,
                 SYSUTCDATETIME(), @AdminUserId, SYSUTCDATETIME());
            INSERT INTO [Navigation].[NavigationVersion]
                ([Id], [WorkspaceId], [VersionNumber], [Status], [Revision],
                 [CreatedBy], [CreatedAt], [UpdatedBy], [UpdatedAt],
                 [PublishedBy], [PublishedAt])
            VALUES
                (@VersionId, @WorkspaceId, 1, N'Published', 1,
                 @AdminUserId, SYSUTCDATETIME(), @AdminUserId,
                 SYSUTCDATETIME(), @AdminUserId, SYSUTCDATETIME());
            INSERT INTO [Navigation].[NavigationItem]
                ([Id], [NavigationVersionId], [ItemKey], [Name], [ItemType],
                 [Route], [RequiredResource], [RequiredAction], [SortOrder],
                 [IsVisible], [IsEnabled], [IsSystem], [Icon], [CreatedAt], [UpdatedAt])
            VALUES
                (NEWID(), @VersionId, N'DEPARTMENT_HOME', N'部门概览', N'Workspace',
                 CONCAT(N'/department?departmentId=', CONVERT(nvarchar(36), @DepartmentId)),
                 N'department', N'Read', 10, 1, 1, 0, N'部', SYSUTCDATETIME(), SYSUTCDATETIME()),
                (NEWID(), @VersionId, N'WORKFLOW_ITEMS', N'流程事项', N'Workflow',
                 CONCAT(N'/workflows?departmentId=', CONVERT(nvarchar(36), @DepartmentId)),
                 N'workflow-task', N'Read', 20, 1, 1, 0, N'流', SYSUTCDATETIME(), SYSUTCDATETIME()),
                (NEWID(), @VersionId, N'CONFIGURATIONS', N'流程配置', N'Administration',
                 CONCAT(N'/configurations?departmentId=', CONVERT(nvarchar(36), @DepartmentId)),
                 N'configuration', N'Read', 30, 1, 1, 0, N'配', SYSUTCDATETIME(), SYSUTCDATETIME()),
                (NEWID(), @VersionId, N'DEPARTMENT_FILES', N'部门文件', N'File',
                 CONCAT(N'/documents?departmentId=', CONVERT(nvarchar(36), @DepartmentId)),
                 N'file', N'Read', 40, 1, 1, 0, N'文', SYSUTCDATETIME(), SYSUTCDATETIME()),
                (NEWID(), @VersionId, N'MINUTES', N'AI 听记', N'Minutes',
                 CONCAT(N'/minutes?departmentId=', CONVERT(nvarchar(36), @DepartmentId)),
                 N'minutes', N'Read', 50, 1, 1, 0, N'记', SYSUTCDATETIME(), SYSUTCDATETIME()),
                (NEWID(), @VersionId, N'BUSINESS_VIEW', N'业务视图', N'BusinessObject',
                 CONCAT(N'/objects?departmentId=', CONVERT(nvarchar(36), @DepartmentId)),
                 N'business-object', N'Read', 60, 1, 1, 0, N'业', SYSUTCDATETIME(), SYSUTCDATETIME()),
                (NEWID(), @VersionId, N'SCENARIOS', N'岗位业务场景', N'BusinessObject',
                 CONCAT(N'/scenarios?departmentId=', CONVERT(nvarchar(36), @DepartmentId)),
                 N'scenario', N'Read', 70, 1, 1, 0, N'场', SYSUTCDATETIME(), SYSUTCDATETIME()),
                (NEWID(), @VersionId, N'AGENTS', N'数字员工身份', N'Administration',
                 CONCAT(N'/agents?departmentId=', CONVERT(nvarchar(36), @DepartmentId)),
                 N'agent', N'Read', 80, 1, 1, 0, N'员', SYSUTCDATETIME(), SYSUTCDATETIME());
        END;
        FETCH NEXT FROM departmentCursor INTO @DepartmentId, @DepartmentCode, @DepartmentName;
    END;
    CLOSE departmentCursor;
    DEALLOCATE departmentCursor;

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    SET ANSI_NULLS ON;
    SET QUOTED_IDENTIFIER ON;
    DECLARE @SystemVersionId uniqueidentifier;
    SELECT TOP (1) @SystemVersionId = version.[Id]
    FROM [Navigation].[NavigationVersion] AS version
    JOIN [Navigation].[NavigationWorkspace] AS workspace
      ON workspace.[Id] = version.[WorkspaceId]
    WHERE workspace.[WorkspaceKey] = N'SYSTEM'
      AND version.[Status] = N'Published'
    ORDER BY version.[VersionNumber] DESC;

    IF @SystemVersionId IS NOT NULL
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM [Navigation].[NavigationItem]
            WHERE [NavigationVersionId] = @SystemVersionId
              AND [ItemKey] = N'MODEL_STUDIO')
        BEGIN
            INSERT INTO [Navigation].[NavigationItem]
                ([Id], [NavigationVersionId], [ItemKey], [Name], [ItemType],
                 [Route], [RequiredResource], [RequiredAction], [SortOrder],
                 [IsVisible], [IsEnabled], [IsSystem], [Icon], [CreatedAt], [UpdatedAt])
            VALUES
                (NEWID(), @SystemVersionId, N'MODEL_STUDIO', N'业务建模工作室',
                 N'Administration', N'/modeling', N'object-model', N'Read',
                 155, 1, 1, 0, N'模', SYSUTCDATETIME(), SYSUTCDATETIME());
        END;
        IF NOT EXISTS (
            SELECT 1 FROM [Navigation].[NavigationItem]
            WHERE [NavigationVersionId] = @SystemVersionId
              AND [ItemKey] = N'MCP_CENTER')
        BEGIN
            INSERT INTO [Navigation].[NavigationItem]
                ([Id], [NavigationVersionId], [ItemKey], [Name], [ItemType],
                 [Route], [RequiredResource], [RequiredAction], [SortOrder],
                 [IsVisible], [IsEnabled], [IsSystem], [Icon], [CreatedAt], [UpdatedAt])
            VALUES
                (NEWID(), @SystemVersionId, N'MCP_CENTER', N'MCP 接入治理',
                 N'Administration', N'/mcp-center', N'mcp', N'Read',
                 160, 1, 1, 0, N'接', SYSUTCDATETIME(), SYSUTCDATETIME());
        END;
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261103153650_018DepartmentWorkspaceDynamicForms', N'10.0.14');
END;
GO

