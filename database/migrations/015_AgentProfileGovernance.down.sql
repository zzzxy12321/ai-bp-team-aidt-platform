BEGIN TRANSACTION;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
ALTER TABLE [Mcp].[AccessToken] DROP CONSTRAINT [FK_McpAccessToken_AgentProfile];

ALTER TABLE [Mcp].[AccessToken] DROP CONSTRAINT [FK_McpAccessToken_RevokedBy];

ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT [FK_AgentProfile_BusinessOwner];

ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT [FK_AgentProfile_CreatedBy];

ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT [FK_AgentProfile_CurrentPublishedVersion];

ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT [FK_AgentProfile_Department];

ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT [FK_AgentProfile_TechnicalOwner];

ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT [FK_AgentProfile_UpdatedBy];

ALTER TABLE [Mcp].[Client] DROP CONSTRAINT [FK_McpClient_AgentProfile];

DECLARE @NewPermissionIds TABLE (Id uniqueidentifier PRIMARY KEY);
INSERT INTO @NewPermissionIds (Id)
SELECT Id FROM [Identity].[Permission]
WHERE Resource = N'agent'
  AND Action IN (N'Create', N'Update', N'Manage', N'Approve');

DELETE rolePermission
FROM [Identity].[RolePermission] rolePermission
JOIN @NewPermissionIds permissionRow ON permissionRow.Id = rolePermission.PermissionId;

DELETE FROM [Identity].[UserRole]
WHERE UserId IN (
    SELECT Id FROM [Identity].[User]
    WHERE UserName IN (
        N'demo.agentmanager', N'agent.lowrisk', N'agent.highrisk'));

DELETE FROM [Identity].[UserDepartmentMembership]
WHERE UserId IN (
    SELECT Id FROM [Identity].[User]
    WHERE UserName IN (
        N'demo.agentmanager', N'agent.lowrisk', N'agent.highrisk'));

DELETE FROM [Identity].[DataScope]
WHERE UserId IN (
    SELECT Id FROM [Identity].[User]
    WHERE UserName IN (
        N'demo.agentmanager', N'agent.lowrisk', N'agent.highrisk',
        N'agent.demo', N'agent.proxy', N'agent.project'));

DELETE rolePermission
FROM [Identity].[RolePermission] rolePermission
JOIN [Identity].[Role] roleRow ON roleRow.Id = rolePermission.RoleId
WHERE roleRow.Code IN (N'agent-department-manager', N'agent-service');

DELETE FROM [Identity].[Role]
WHERE Code IN (N'agent-department-manager', N'agent-service');

DELETE FROM [Identity].[User]
WHERE UserName IN (
    N'demo.agentmanager', N'agent.lowrisk', N'agent.highrisk');

DELETE FROM [Identity].[Permission]
WHERE Resource = N'agent'
  AND Action IN (N'Create', N'Update', N'Manage', N'Approve');

DROP TABLE [Agent].[AgentProfileVersion];

DROP INDEX [IX_McpClient_AgentProfileId] ON [Mcp].[Client];

DROP INDEX [IX_AgentProfile_BusinessOwnerUserId] ON [Agent].[AgentProfile];

DROP INDEX [IX_AgentProfile_CreatedBy] ON [Agent].[AgentProfile];

DROP INDEX [IX_AgentProfile_CurrentPublishedVersionId] ON [Agent].[AgentProfile];

DROP INDEX [IX_AgentProfile_Department_Lifecycle] ON [Agent].[AgentProfile];

DROP INDEX [IX_AgentProfile_Name] ON [Agent].[AgentProfile];

DROP INDEX [IX_AgentProfile_TechnicalOwnerUserId] ON [Agent].[AgentProfile];

DROP INDEX [IX_AgentProfile_UpdatedBy] ON [Agent].[AgentProfile];

DROP INDEX [UX_AgentProfile_Code] ON [Agent].[AgentProfile];

ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT [CK_AgentProfile_AllowedToolsJson];

ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT [CK_AgentProfile_ConfirmationsJson];

ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT [CK_AgentProfile_DataScopesJson];

ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT [CK_AgentProfile_Lifecycle];

ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT [CK_AgentProfile_ModelScopesJson];

ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT [CK_AgentProfile_StopConditionsJson];

DROP INDEX [IX_AccessToken_RevokedBy] ON [Mcp].[AccessToken];

DROP INDEX [IX_McpAccessToken_AgentProfile_Active] ON [Mcp].[AccessToken];

ALTER TABLE [Identity].[AccessChangeRequest] DROP CONSTRAINT [CK_AccessChangeRequest_Type];

DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Mcp].[Client]') AND [c].[name] = N'AgentProfileId');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Mcp].[Client] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [Mcp].[Client] DROP COLUMN [AgentProfileId];

DECLARE @var1 nvarchar(max);
SELECT @var1 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentProfile]') AND [c].[name] = N'BusinessOwnerUserId');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT ' + @var1 + ';');
ALTER TABLE [Agent].[AgentProfile] DROP COLUMN [BusinessOwnerUserId];

DECLARE @var2 nvarchar(max);
SELECT @var2 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentProfile]') AND [c].[name] = N'Code');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT ' + @var2 + ';');
ALTER TABLE [Agent].[AgentProfile] DROP COLUMN [Code];

DECLARE @var3 nvarchar(max);
SELECT @var3 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentProfile]') AND [c].[name] = N'CreatedAt');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT ' + @var3 + ';');
ALTER TABLE [Agent].[AgentProfile] DROP COLUMN [CreatedAt];

DECLARE @var4 nvarchar(max);
SELECT @var4 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentProfile]') AND [c].[name] = N'CreatedBy');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT ' + @var4 + ';');
ALTER TABLE [Agent].[AgentProfile] DROP COLUMN [CreatedBy];

DECLARE @var5 nvarchar(max);
SELECT @var5 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentProfile]') AND [c].[name] = N'CurrentPublishedVersionId');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT ' + @var5 + ';');
ALTER TABLE [Agent].[AgentProfile] DROP COLUMN [CurrentPublishedVersionId];

DECLARE @var6 nvarchar(max);
SELECT @var6 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentProfile]') AND [c].[name] = N'DataScopesJson');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT ' + @var6 + ';');
ALTER TABLE [Agent].[AgentProfile] DROP COLUMN [DataScopesJson];

DECLARE @var7 nvarchar(max);
SELECT @var7 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentProfile]') AND [c].[name] = N'DepartmentId');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT ' + @var7 + ';');
ALTER TABLE [Agent].[AgentProfile] DROP COLUMN [DepartmentId];

DECLARE @var8 nvarchar(max);
SELECT @var8 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentProfile]') AND [c].[name] = N'Description');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT ' + @var8 + ';');
ALTER TABLE [Agent].[AgentProfile] DROP COLUMN [Description];

DECLARE @var9 nvarchar(max);
SELECT @var9 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentProfile]') AND [c].[name] = N'LifecycleStatus');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT ' + @var9 + ';');
ALTER TABLE [Agent].[AgentProfile] DROP COLUMN [LifecycleStatus];

DECLARE @var10 nvarchar(max);
SELECT @var10 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentProfile]') AND [c].[name] = N'ModelScopesJson');
IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT ' + @var10 + ';');
ALTER TABLE [Agent].[AgentProfile] DROP COLUMN [ModelScopesJson];

DECLARE @var11 nvarchar(max);
SELECT @var11 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentProfile]') AND [c].[name] = N'RequiredConfirmationsJson');
IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT ' + @var11 + ';');
ALTER TABLE [Agent].[AgentProfile] DROP COLUMN [RequiredConfirmationsJson];

DECLARE @var12 nvarchar(max);
SELECT @var12 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentProfile]') AND [c].[name] = N'Revision');
IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT ' + @var12 + ';');
ALTER TABLE [Agent].[AgentProfile] DROP COLUMN [Revision];

DECLARE @var13 nvarchar(max);
SELECT @var13 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentProfile]') AND [c].[name] = N'StopConditionsJson');
IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT ' + @var13 + ';');
ALTER TABLE [Agent].[AgentProfile] DROP COLUMN [StopConditionsJson];

DECLARE @var14 nvarchar(max);
SELECT @var14 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentProfile]') AND [c].[name] = N'TechnicalOwnerUserId');
IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT ' + @var14 + ';');
ALTER TABLE [Agent].[AgentProfile] DROP COLUMN [TechnicalOwnerUserId];

DECLARE @var15 nvarchar(max);
SELECT @var15 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentProfile]') AND [c].[name] = N'UpdatedAt');
IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT ' + @var15 + ';');
ALTER TABLE [Agent].[AgentProfile] DROP COLUMN [UpdatedAt];

DECLARE @var16 nvarchar(max);
SELECT @var16 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentProfile]') AND [c].[name] = N'UpdatedBy');
IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT ' + @var16 + ';');
ALTER TABLE [Agent].[AgentProfile] DROP COLUMN [UpdatedBy];

DECLARE @var17 nvarchar(max);
SELECT @var17 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentProfile]') AND [c].[name] = N'Version');
IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT ' + @var17 + ';');
ALTER TABLE [Agent].[AgentProfile] DROP COLUMN [Version];

DECLARE @var18 nvarchar(max);
SELECT @var18 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Mcp].[AccessToken]') AND [c].[name] = N'AgentProfileId');
IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [Mcp].[AccessToken] DROP CONSTRAINT ' + @var18 + ';');
ALTER TABLE [Mcp].[AccessToken] DROP COLUMN [AgentProfileId];

DECLARE @var19 nvarchar(max);
SELECT @var19 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Mcp].[AccessToken]') AND [c].[name] = N'RevocationReason');
IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [Mcp].[AccessToken] DROP CONSTRAINT ' + @var19 + ';');
ALTER TABLE [Mcp].[AccessToken] DROP COLUMN [RevocationReason];

DECLARE @var20 nvarchar(max);
SELECT @var20 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Mcp].[AccessToken]') AND [c].[name] = N'RevokedBy');
IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [Mcp].[AccessToken] DROP CONSTRAINT ' + @var20 + ';');
ALTER TABLE [Mcp].[AccessToken] DROP COLUMN [RevokedBy];

CREATE UNIQUE INDEX [UX_AgentProfile_Name] ON [Agent].[AgentProfile] ([Name]);

ALTER TABLE [Identity].[AccessChangeRequest] ADD CONSTRAINT [CK_AccessChangeRequest_Type] CHECK ([RequestType] IN ('RoleAssignment','RolePermissions'));

DELETE FROM [__EFMigrationsHistory]
WHERE [MigrationId] = N'20261103004434_015_AgentProfileGovernance';

COMMIT;
GO

