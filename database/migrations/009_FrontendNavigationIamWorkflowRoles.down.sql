BEGIN TRANSACTION;
IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Identity].[Department] DROP CONSTRAINT [FK_Department_Parent];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT [FK_WorkflowNodePolicy_BusinessRole];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    SET ANSI_NULLS ON;
    SET QUOTED_IDENTIFIER ON;
    SET ANSI_PADDING ON;
    SET ANSI_WARNINGS ON;
    SET CONCAT_NULL_YIELDS_NULL ON;
    SET NUMERIC_ROUNDABORT OFF;
    ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT [FK_WorkflowTask_BackupAssignee];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT [FK_WorkflowTask_BusinessRole];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT [FK_WorkflowTask_OriginalAssignee];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT [FK_WorkflowTask_TransferredBy];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DROP TABLE [Identity].[AccessChangeRequest];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DROP TABLE [Navigation].[NavigationItem];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DROP TABLE [Navigation].[NavigationUserPreference];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DROP TABLE [Identity].[UserRoleAssignment];
END;

GO

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DELETE FROM [Identity].[DataScope]
    WHERE UserId = '10000000-0000-0000-0000-000000000023';
    DELETE FROM [Identity].[UserRole]
    WHERE UserId = '10000000-0000-0000-0000-000000000023';
    DELETE FROM [Identity].[UserDepartmentMembership]
    WHERE UserId = '10000000-0000-0000-0000-000000000023';
    DELETE FROM [Identity].[User]
    WHERE UserName = N'demo.security';

    DELETE rolePermission
    FROM [Identity].[RolePermission] rolePermission
    JOIN [Identity].[Permission] permissionRow
        ON permissionRow.Id = rolePermission.PermissionId
    WHERE permissionRow.Code IN (
        N'identity-admin-read', N'identity-admin-manage',
        N'role-read', N'role-create', N'role-update', N'role-manage',
        N'permission-read', N'permission-assign',
        N'navigation-read', N'navigation-manage', N'navigation-publish',
        N'data-scope-update', N'workflow-role-read', N'workflow-role-manage',
        N'workflow-task-update');

    DELETE rolePermission
    FROM [Identity].[RolePermission] rolePermission
    JOIN [Identity].[Role] roleRow ON roleRow.Id = rolePermission.RoleId
    JOIN [Identity].[Permission] permissionRow ON permissionRow.Id = rolePermission.PermissionId
    WHERE roleRow.Code = N'project-manager'
      AND permissionRow.Code IN (
        N'workflow-task-approve', N'workflow-task-return', N'workflow-task-terminate');

    DELETE FROM [Identity].[Permission]
    WHERE Code IN (
        N'identity-admin-read', N'identity-admin-manage',
        N'role-read', N'role-create', N'role-update', N'role-manage',
        N'permission-read', N'permission-assign',
        N'navigation-read', N'navigation-manage', N'navigation-publish',
        N'data-scope-update', N'workflow-role-read', N'workflow-role-manage',
        N'workflow-task-update');

    DELETE FROM [Identity].[Role]
    WHERE Code IN (
        N'business-rd-engineer',
        N'business-manager-reviewer',
        N'business-quality-reviewer');
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DROP TABLE [Navigation].[NavigationVersion];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DROP TABLE [Navigation].[NavigationWorkspace];
END;

IF SCHEMA_ID(N'Navigation') IS NOT NULL
    EXEC(N'DROP SCHEMA [Navigation];');

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DROP INDEX [IX_WorkflowTask_BackupAssigneeId] ON [Workflow].[WorkflowTask];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DROP INDEX [IX_WorkflowTask_BusinessRole_Status] ON [Workflow].[WorkflowTask];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DROP INDEX [IX_WorkflowTask_OriginalAssigneeId] ON [Workflow].[WorkflowTask];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DROP INDEX [IX_WorkflowTask_TransferredBy] ON [Workflow].[WorkflowTask];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DROP INDEX [IX_WorkflowNodePolicy_BusinessRoleId] ON [Workflow].[WorkflowNodePolicy];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT [CK_WorkflowNodePolicy_AssigneeStrategy];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT [CK_WorkflowNodePolicy_TransferTargetStrategy];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DROP INDEX [IX_Role_Type_Active] ON [Identity].[Role];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Identity].[Role] DROP CONSTRAINT [CK_Role_Type];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DROP INDEX [IX_Department_ParentDepartmentId] ON [Identity].[Department];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DROP INDEX [IX_Department_Tree] ON [Identity].[Department];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Identity].[Department] DROP CONSTRAINT [CK_Department_HierarchyType];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowTask]') AND [c].[name] = N'AssigneeSnapshotJson');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Workflow].[WorkflowTask] DROP COLUMN [AssigneeSnapshotJson];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowTask]') AND [c].[name] = N'AssigneeStrategy');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Workflow].[WorkflowTask] DROP COLUMN [AssigneeStrategy];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowTask]') AND [c].[name] = N'BackupAssigneeId');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [Workflow].[WorkflowTask] DROP COLUMN [BackupAssigneeId];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowTask]') AND [c].[name] = N'BusinessRoleCode');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [Workflow].[WorkflowTask] DROP COLUMN [BusinessRoleCode];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowTask]') AND [c].[name] = N'BusinessRoleId');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [Workflow].[WorkflowTask] DROP COLUMN [BusinessRoleId];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowTask]') AND [c].[name] = N'BusinessRoleVersion');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [Workflow].[WorkflowTask] DROP COLUMN [BusinessRoleVersion];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowTask]') AND [c].[name] = N'CandidateUserIdsJson');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [Workflow].[WorkflowTask] DROP COLUMN [CandidateUserIdsJson];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowTask]') AND [c].[name] = N'OriginalAssigneeId');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT ' + @var7 + ';');
    ALTER TABLE [Workflow].[WorkflowTask] DROP COLUMN [OriginalAssigneeId];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var8 nvarchar(max);
    SELECT @var8 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowTask]') AND [c].[name] = N'TransferReason');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT ' + @var8 + ';');
    ALTER TABLE [Workflow].[WorkflowTask] DROP COLUMN [TransferReason];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var9 nvarchar(max);
    SELECT @var9 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowTask]') AND [c].[name] = N'TransferredAt');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT ' + @var9 + ';');
    ALTER TABLE [Workflow].[WorkflowTask] DROP COLUMN [TransferredAt];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var10 nvarchar(max);
    SELECT @var10 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowTask]') AND [c].[name] = N'TransferredBy');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT ' + @var10 + ';');
    ALTER TABLE [Workflow].[WorkflowTask] DROP COLUMN [TransferredBy];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var11 nvarchar(max);
    SELECT @var11 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowNodePolicy]') AND [c].[name] = N'AllowTransfer');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT ' + @var11 + ';');
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP COLUMN [AllowTransfer];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var12 nvarchar(max);
    SELECT @var12 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowNodePolicy]') AND [c].[name] = N'AssigneeStrategy');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT ' + @var12 + ';');
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP COLUMN [AssigneeStrategy];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var13 nvarchar(max);
    SELECT @var13 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowNodePolicy]') AND [c].[name] = N'BusinessRoleId');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT ' + @var13 + ';');
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP COLUMN [BusinessRoleId];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var14 nvarchar(max);
    SELECT @var14 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowNodePolicy]') AND [c].[name] = N'TransferTargetStrategy');
    IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT ' + @var14 + ';');
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP COLUMN [TransferTargetStrategy];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var15 nvarchar(max);
    SELECT @var15 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Identity].[User]') AND [c].[name] = N'ActivatedAt');
    IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [Identity].[User] DROP CONSTRAINT ' + @var15 + ';');
    ALTER TABLE [Identity].[User] DROP COLUMN [ActivatedAt];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var16 nvarchar(max);
    SELECT @var16 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Identity].[User]') AND [c].[name] = N'InvitationStatus');
    IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [Identity].[User] DROP CONSTRAINT ' + @var16 + ';');
    ALTER TABLE [Identity].[User] DROP COLUMN [InvitationStatus];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var17 nvarchar(max);
    SELECT @var17 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Identity].[User]') AND [c].[name] = N'InvitedAt');
    IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [Identity].[User] DROP CONSTRAINT ' + @var17 + ';');
    ALTER TABLE [Identity].[User] DROP COLUMN [InvitedAt];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var18 nvarchar(max);
    SELECT @var18 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Identity].[User]') AND [c].[name] = N'Version');
    IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [Identity].[User] DROP CONSTRAINT ' + @var18 + ';');
    ALTER TABLE [Identity].[User] DROP COLUMN [Version];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var19 nvarchar(max);
    SELECT @var19 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Identity].[Role]') AND [c].[name] = N'CreatedAt');
    IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [Identity].[Role] DROP CONSTRAINT ' + @var19 + ';');
    ALTER TABLE [Identity].[Role] DROP COLUMN [CreatedAt];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var20 nvarchar(max);
    SELECT @var20 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Identity].[Role]') AND [c].[name] = N'Description');
    IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [Identity].[Role] DROP CONSTRAINT ' + @var20 + ';');
    ALTER TABLE [Identity].[Role] DROP COLUMN [Description];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var21 nvarchar(max);
    SELECT @var21 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Identity].[Role]') AND [c].[name] = N'IsActive');
    IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [Identity].[Role] DROP CONSTRAINT ' + @var21 + ';');
    ALTER TABLE [Identity].[Role] DROP COLUMN [IsActive];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var22 nvarchar(max);
    SELECT @var22 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Identity].[Role]') AND [c].[name] = N'IsSystem');
    IF @var22 IS NOT NULL EXEC(N'ALTER TABLE [Identity].[Role] DROP CONSTRAINT ' + @var22 + ';');
    ALTER TABLE [Identity].[Role] DROP COLUMN [IsSystem];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var23 nvarchar(max);
    SELECT @var23 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Identity].[Role]') AND [c].[name] = N'RoleType');
    IF @var23 IS NOT NULL EXEC(N'ALTER TABLE [Identity].[Role] DROP CONSTRAINT ' + @var23 + ';');
    ALTER TABLE [Identity].[Role] DROP COLUMN [RoleType];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var24 nvarchar(max);
    SELECT @var24 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Identity].[Role]') AND [c].[name] = N'UpdatedAt');
    IF @var24 IS NOT NULL EXEC(N'ALTER TABLE [Identity].[Role] DROP CONSTRAINT ' + @var24 + ';');
    ALTER TABLE [Identity].[Role] DROP COLUMN [UpdatedAt];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var25 nvarchar(max);
    SELECT @var25 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Identity].[Role]') AND [c].[name] = N'Version');
    IF @var25 IS NOT NULL EXEC(N'ALTER TABLE [Identity].[Role] DROP CONSTRAINT ' + @var25 + ';');
    ALTER TABLE [Identity].[Role] DROP COLUMN [Version];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var26 nvarchar(max);
    SELECT @var26 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Identity].[Department]') AND [c].[name] = N'HierarchyType');
    IF @var26 IS NOT NULL EXEC(N'ALTER TABLE [Identity].[Department] DROP CONSTRAINT ' + @var26 + ';');
    ALTER TABLE [Identity].[Department] DROP COLUMN [HierarchyType];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var27 nvarchar(max);
    SELECT @var27 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Identity].[Department]') AND [c].[name] = N'ParentDepartmentId');
    IF @var27 IS NOT NULL EXEC(N'ALTER TABLE [Identity].[Department] DROP CONSTRAINT ' + @var27 + ';');
    ALTER TABLE [Identity].[Department] DROP COLUMN [ParentDepartmentId];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var28 nvarchar(max);
    SELECT @var28 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Identity].[Department]') AND [c].[name] = N'Path');
    IF @var28 IS NOT NULL EXEC(N'ALTER TABLE [Identity].[Department] DROP CONSTRAINT ' + @var28 + ';');
    ALTER TABLE [Identity].[Department] DROP COLUMN [Path];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var29 nvarchar(max);
    SELECT @var29 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Identity].[Department]') AND [c].[name] = N'SortOrder');
    IF @var29 IS NOT NULL EXEC(N'ALTER TABLE [Identity].[Department] DROP CONSTRAINT ' + @var29 + ';');
    ALTER TABLE [Identity].[Department] DROP COLUMN [SortOrder];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @var30 nvarchar(max);
    SELECT @var30 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Identity].[Department]') AND [c].[name] = N'Version');
    IF @var30 IS NOT NULL EXEC(N'ALTER TABLE [Identity].[Department] DROP CONSTRAINT ' + @var30 + ';');
    ALTER TABLE [Identity].[Department] DROP COLUMN [Version];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DELETE FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles';
END;

COMMIT;
GO

