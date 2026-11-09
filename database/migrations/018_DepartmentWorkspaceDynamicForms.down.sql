IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    SET ANSI_NULLS ON;
    SET QUOTED_IDENTIFIER ON;

    DELETE item
    FROM [Navigation].[NavigationItem] AS item
    JOIN [Navigation].[NavigationVersion] AS version
      ON version.[Id] = item.[NavigationVersionId]
    JOIN [Navigation].[NavigationWorkspace] AS workspace
      ON workspace.[Id] = version.[WorkspaceId]
    WHERE workspace.[WorkspaceKey] LIKE N'DEPT-%';

    DELETE version
    FROM [Navigation].[NavigationVersion] AS version
    JOIN [Navigation].[NavigationWorkspace] AS workspace
      ON workspace.[Id] = version.[WorkspaceId]
    WHERE workspace.[WorkspaceKey] LIKE N'DEPT-%';

    DELETE FROM [Navigation].[NavigationWorkspace]
    WHERE [WorkspaceKey] LIKE N'DEPT-%';

    DELETE item
    FROM [Navigation].[NavigationItem] AS item
    JOIN [Navigation].[NavigationVersion] AS version
      ON version.[Id] = item.[NavigationVersionId]
    JOIN [Navigation].[NavigationWorkspace] AS workspace
      ON workspace.[Id] = version.[WorkspaceId]
    WHERE workspace.[WorkspaceKey] = N'SYSTEM'
      AND item.[ItemKey] IN (N'MODEL_STUDIO', N'MCP_CENTER');

    UPDATE [Navigation].[NavigationWorkspace]
    SET [Status] = N'Published',
        [RetiredAt] = NULL
    WHERE [WorkspaceKey] LIKE N'NAV-E2E-%'
      AND [Status] = N'Retired';

    DECLARE @FlowOwnerRoleId uniqueidentifier;
    SELECT @FlowOwnerRoleId = [Id]
    FROM [Identity].[Role]
    WHERE [Code] = N'department-flow-owner';
    IF @FlowOwnerRoleId IS NOT NULL
    BEGIN
        DELETE FROM [Identity].[UserRoleAssignment]
        WHERE [RoleId] = @FlowOwnerRoleId;
        DELETE FROM [Identity].[RolePermission]
        WHERE [RoleId] = @FlowOwnerRoleId;
        DELETE FROM [Identity].[Role]
        WHERE [Id] = @FlowOwnerRoleId;
    END;

    DECLARE @DeliveryUserId uniqueidentifier;
    DECLARE @DeliveryDepartmentId uniqueidentifier;
    SELECT @DeliveryUserId = [Id]
    FROM [Identity].[User]
    WHERE [UserName] = N'demo.delivery';
    SELECT @DeliveryDepartmentId = [Id]
    FROM [Identity].[Department]
    WHERE [Code] = N'DELIVERY' AND [Name] LIKE N'%Demo%';

    DELETE FROM [Identity].[UserDepartmentMembership]
    WHERE [UserId] = @DeliveryUserId
       OR [DepartmentId] = @DeliveryDepartmentId;

    IF @DeliveryDepartmentId IS NOT NULL
    BEGIN
        UPDATE [Identity].[Department]
        SET [IsActive] = 0,
            [Version] = [Version] + 1,
            [UpdatedAt] = SYSUTCDATETIME()
        WHERE [Id] = @DeliveryDepartmentId;
    END;
    IF @DeliveryUserId IS NOT NULL
    BEGIN
        UPDATE [Identity].[User]
        SET [IsActive] = 0
        WHERE [Id] = @DeliveryUserId;
    END;
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

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowDefinition] DROP CONSTRAINT [FK_WorkflowDefinition_CreatedBy];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowDefinition] DROP CONSTRAINT [FK_WorkflowDefinition_Department];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowDefinition] DROP CONSTRAINT [FK_WorkflowDefinition_UpdatedBy];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DROP INDEX [IX_WorkflowTask_Instance_Node_CreatedAt] ON [Workflow].[WorkflowTask];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT [CK_WorkflowTask_FormDataJson];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT [CK_WorkflowTask_FormDefinitionJson];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT [CK_WorkflowNodePolicy_AiReviewJson];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT [CK_WorkflowNodePolicy_DeliverablesJson];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT [CK_WorkflowNodePolicy_FormSchemaJson];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT [CK_WorkflowNodePolicy_NodeActionsJson];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT [CK_WorkflowNodePolicy_NotificationJson];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT [CK_WorkflowNodePolicy_RelatedRolesJson];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DROP INDEX [IX_WorkflowDefinition_CreatedBy] ON [Workflow].[WorkflowDefinition];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DROP INDEX [IX_WorkflowDefinition_Department_Object_Default_Risk] ON [Workflow].[WorkflowDefinition];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DROP INDEX [IX_WorkflowDefinition_UpdatedBy] ON [Workflow].[WorkflowDefinition];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DROP INDEX [UX_WorkflowDefinition_ActiveDepartmentDefault] ON [Workflow].[WorkflowDefinition];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowDefinition] DROP CONSTRAINT [CK_WorkflowDefinition_ApplicableRolesJson];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowDefinition] DROP CONSTRAINT [CK_WorkflowDefinition_Environment];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowDefinition] DROP CONSTRAINT [CK_WorkflowDefinition_RiskLevel];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DROP INDEX [IX_NavigationWorkspace_Org_Test_Status] ON [Navigation].[NavigationWorkspace];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DROP INDEX [UX_NavigationWorkspace_ActiveDepartmentDefault] ON [Navigation].[NavigationWorkspace];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Navigation].[NavigationWorkspace] DROP CONSTRAINT [CK_NavigationWorkspace_Environment];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DROP INDEX [IX_ConfigurationVersion_Org_Dept_Test_Env_Updated] ON [Configuration].[ConfigurationVersion];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT [CK_ConfigurationVersion_ApplicableRolesJson];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT [CK_ConfigurationVersion_Environment];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT [CK_ConfigurationVersion_RiskLevel];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowTask]') AND [c].[name] = N'FormDataJson');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Workflow].[WorkflowTask] DROP COLUMN [FormDataJson];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowTask]') AND [c].[name] = N'FormDefinitionHash');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Workflow].[WorkflowTask] DROP COLUMN [FormDefinitionHash];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowTask]') AND [c].[name] = N'FormDefinitionJson');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [Workflow].[WorkflowTask] DROP COLUMN [FormDefinitionJson];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowTask]') AND [c].[name] = N'FormSchemaVersion');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [Workflow].[WorkflowTask] DROP COLUMN [FormSchemaVersion];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowTask]') AND [c].[name] = N'NodeName');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [Workflow].[WorkflowTask] DROP COLUMN [NodeName];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowNodePolicy]') AND [c].[name] = N'AiReviewJson');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP COLUMN [AiReviewJson];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowNodePolicy]') AND [c].[name] = N'BusinessDescription');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP COLUMN [BusinessDescription];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowNodePolicy]') AND [c].[name] = N'CompletionCondition');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT ' + @var7 + ';');
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP COLUMN [CompletionCondition];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var8 nvarchar(max);
    SELECT @var8 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowNodePolicy]') AND [c].[name] = N'DeliverablesJson');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT ' + @var8 + ';');
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP COLUMN [DeliverablesJson];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var9 nvarchar(max);
    SELECT @var9 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowNodePolicy]') AND [c].[name] = N'FormSchemaJson');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT ' + @var9 + ';');
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP COLUMN [FormSchemaJson];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var10 nvarchar(max);
    SELECT @var10 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowNodePolicy]') AND [c].[name] = N'FormSchemaVersion');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT ' + @var10 + ';');
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP COLUMN [FormSchemaVersion];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var11 nvarchar(max);
    SELECT @var11 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowNodePolicy]') AND [c].[name] = N'NodeActionsJson');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT ' + @var11 + ';');
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP COLUMN [NodeActionsJson];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var12 nvarchar(max);
    SELECT @var12 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowNodePolicy]') AND [c].[name] = N'NodeName');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT ' + @var12 + ';');
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP COLUMN [NodeName];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var13 nvarchar(max);
    SELECT @var13 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowNodePolicy]') AND [c].[name] = N'NotificationPolicyJson');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT ' + @var13 + ';');
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP COLUMN [NotificationPolicyJson];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var14 nvarchar(max);
    SELECT @var14 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowNodePolicy]') AND [c].[name] = N'RelatedRoleCodesJson');
    IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT ' + @var14 + ';');
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP COLUMN [RelatedRoleCodesJson];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var15 nvarchar(max);
    SELECT @var15 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowDefinition]') AND [c].[name] = N'ApplicableRoleCodesJson');
    IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowDefinition] DROP CONSTRAINT ' + @var15 + ';');
    ALTER TABLE [Workflow].[WorkflowDefinition] DROP COLUMN [ApplicableRoleCodesJson];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var16 nvarchar(max);
    SELECT @var16 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowDefinition]') AND [c].[name] = N'CreatedBy');
    IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowDefinition] DROP CONSTRAINT ' + @var16 + ';');
    ALTER TABLE [Workflow].[WorkflowDefinition] DROP COLUMN [CreatedBy];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var17 nvarchar(max);
    SELECT @var17 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowDefinition]') AND [c].[name] = N'DepartmentId');
    IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowDefinition] DROP CONSTRAINT ' + @var17 + ';');
    ALTER TABLE [Workflow].[WorkflowDefinition] DROP COLUMN [DepartmentId];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var18 nvarchar(max);
    SELECT @var18 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowDefinition]') AND [c].[name] = N'Description');
    IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowDefinition] DROP CONSTRAINT ' + @var18 + ';');
    ALTER TABLE [Workflow].[WorkflowDefinition] DROP COLUMN [Description];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var19 nvarchar(max);
    SELECT @var19 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowDefinition]') AND [c].[name] = N'EnvironmentCode');
    IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowDefinition] DROP CONSTRAINT ' + @var19 + ';');
    ALTER TABLE [Workflow].[WorkflowDefinition] DROP COLUMN [EnvironmentCode];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var20 nvarchar(max);
    SELECT @var20 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowDefinition]') AND [c].[name] = N'IsDefaultForDepartment');
    IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowDefinition] DROP CONSTRAINT ' + @var20 + ';');
    ALTER TABLE [Workflow].[WorkflowDefinition] DROP COLUMN [IsDefaultForDepartment];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var21 nvarchar(max);
    SELECT @var21 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowDefinition]') AND [c].[name] = N'IsTestData');
    IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowDefinition] DROP CONSTRAINT ' + @var21 + ';');
    ALTER TABLE [Workflow].[WorkflowDefinition] DROP COLUMN [IsTestData];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var22 nvarchar(max);
    SELECT @var22 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowDefinition]') AND [c].[name] = N'ObjectTypeCode');
    IF @var22 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowDefinition] DROP CONSTRAINT ' + @var22 + ';');
    ALTER TABLE [Workflow].[WorkflowDefinition] DROP COLUMN [ObjectTypeCode];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var23 nvarchar(max);
    SELECT @var23 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowDefinition]') AND [c].[name] = N'RequiresPlatformApproval');
    IF @var23 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowDefinition] DROP CONSTRAINT ' + @var23 + ';');
    ALTER TABLE [Workflow].[WorkflowDefinition] DROP COLUMN [RequiresPlatformApproval];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var24 nvarchar(max);
    SELECT @var24 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowDefinition]') AND [c].[name] = N'RiskLevel');
    IF @var24 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowDefinition] DROP CONSTRAINT ' + @var24 + ';');
    ALTER TABLE [Workflow].[WorkflowDefinition] DROP COLUMN [RiskLevel];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var25 nvarchar(max);
    SELECT @var25 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowDefinition]') AND [c].[name] = N'UpdatedAt');
    IF @var25 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowDefinition] DROP CONSTRAINT ' + @var25 + ';');
    ALTER TABLE [Workflow].[WorkflowDefinition] DROP COLUMN [UpdatedAt];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var26 nvarchar(max);
    SELECT @var26 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowDefinition]') AND [c].[name] = N'UpdatedBy');
    IF @var26 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowDefinition] DROP CONSTRAINT ' + @var26 + ';');
    ALTER TABLE [Workflow].[WorkflowDefinition] DROP COLUMN [UpdatedBy];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var27 nvarchar(max);
    SELECT @var27 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowDefinition]') AND [c].[name] = N'Version');
    IF @var27 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowDefinition] DROP CONSTRAINT ' + @var27 + ';');
    ALTER TABLE [Workflow].[WorkflowDefinition] DROP COLUMN [Version];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var28 nvarchar(max);
    SELECT @var28 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Navigation].[NavigationWorkspace]') AND [c].[name] = N'EnvironmentCode');
    IF @var28 IS NOT NULL EXEC(N'ALTER TABLE [Navigation].[NavigationWorkspace] DROP CONSTRAINT ' + @var28 + ';');
    ALTER TABLE [Navigation].[NavigationWorkspace] DROP COLUMN [EnvironmentCode];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var29 nvarchar(max);
    SELECT @var29 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Navigation].[NavigationWorkspace]') AND [c].[name] = N'IsDefault');
    IF @var29 IS NOT NULL EXEC(N'ALTER TABLE [Navigation].[NavigationWorkspace] DROP CONSTRAINT ' + @var29 + ';');
    ALTER TABLE [Navigation].[NavigationWorkspace] DROP COLUMN [IsDefault];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var30 nvarchar(max);
    SELECT @var30 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Navigation].[NavigationWorkspace]') AND [c].[name] = N'IsDemoData');
    IF @var30 IS NOT NULL EXEC(N'ALTER TABLE [Navigation].[NavigationWorkspace] DROP CONSTRAINT ' + @var30 + ';');
    ALTER TABLE [Navigation].[NavigationWorkspace] DROP COLUMN [IsDemoData];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var31 nvarchar(max);
    SELECT @var31 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Navigation].[NavigationWorkspace]') AND [c].[name] = N'IsTestData');
    IF @var31 IS NOT NULL EXEC(N'ALTER TABLE [Navigation].[NavigationWorkspace] DROP CONSTRAINT ' + @var31 + ';');
    ALTER TABLE [Navigation].[NavigationWorkspace] DROP COLUMN [IsTestData];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var32 nvarchar(max);
    SELECT @var32 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Configuration].[ConfigurationVersion]') AND [c].[name] = N'ApplicableRoleCodesJson');
    IF @var32 IS NOT NULL EXEC(N'ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT ' + @var32 + ';');
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP COLUMN [ApplicableRoleCodesJson];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var33 nvarchar(max);
    SELECT @var33 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Configuration].[ConfigurationVersion]') AND [c].[name] = N'EnvironmentCode');
    IF @var33 IS NOT NULL EXEC(N'ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT ' + @var33 + ';');
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP COLUMN [EnvironmentCode];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var34 nvarchar(max);
    SELECT @var34 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Configuration].[ConfigurationVersion]') AND [c].[name] = N'IsDefault');
    IF @var34 IS NOT NULL EXEC(N'ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT ' + @var34 + ';');
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP COLUMN [IsDefault];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var35 nvarchar(max);
    SELECT @var35 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Configuration].[ConfigurationVersion]') AND [c].[name] = N'IsTestData');
    IF @var35 IS NOT NULL EXEC(N'ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT ' + @var35 + ';');
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP COLUMN [IsTestData];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var36 nvarchar(max);
    SELECT @var36 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Configuration].[ConfigurationVersion]') AND [c].[name] = N'RequiresPlatformApproval');
    IF @var36 IS NOT NULL EXEC(N'ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT ' + @var36 + ';');
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP COLUMN [RequiresPlatformApproval];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var37 nvarchar(max);
    SELECT @var37 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Configuration].[ConfigurationVersion]') AND [c].[name] = N'RiskLevel');
    IF @var37 IS NOT NULL EXEC(N'ALTER TABLE [Configuration].[ConfigurationVersion] DROP CONSTRAINT ' + @var37 + ';');
    ALTER TABLE [Configuration].[ConfigurationVersion] DROP COLUMN [RiskLevel];
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DECLARE @var38 nvarchar(max);
    SELECT @var38 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentRun]') AND [c].[name] = N'RiskLevel');
    IF @var38 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentRun] DROP CONSTRAINT ' + @var38 + ';');
END;
GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms'
)
BEGIN
    DELETE FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103153650_018DepartmentWorkspaceDynamicForms';
END;
GO

