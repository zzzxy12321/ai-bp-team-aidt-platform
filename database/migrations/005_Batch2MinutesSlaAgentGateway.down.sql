BEGIN TRANSACTION;
ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT [FK_AgentProfile_ServiceUser];

ALTER TABLE [Agent].[AgentRun] DROP CONSTRAINT [FK_AgentRun_Department];

ALTER TABLE [Agent].[AgentRun] DROP CONSTRAINT [FK_AgentRun_Organization];

DROP TABLE [Agent].[AgentGatewayCall];

DROP TABLE [Minutes].[MinutesTask];

DROP TABLE [Workflow].[WorkflowNodePolicy];

DELETE FROM [Agent].[AgentProfile]
WHERE Id='60000000-0000-0000-0000-000000000002';

DELETE FROM [Identity].[User]
WHERE Id='10000000-0000-0000-0000-000000000020'
  AND UserName=N'agent.proxy';

DELETE rolePermission
FROM [Identity].[RolePermission] rolePermission
JOIN [Identity].[Permission] permissionRow
    ON permissionRow.Id=rolePermission.PermissionId
WHERE permissionRow.Code IN
    (N'minutes-read', N'minutes-create', N'minutes-update', N'minutes-approve',
     N'workflow-policy-read', N'workflow-policy-update',
     N'workflow-sla-update', N'agent-run-update');

DELETE FROM [Identity].[Permission]
WHERE Code IN
    (N'minutes-read', N'minutes-create', N'minutes-update', N'minutes-approve',
     N'workflow-policy-read', N'workflow-policy-update',
     N'workflow-sla-update', N'agent-run-update');

DROP TABLE [Workflow].[WorkflowNotification];

DROP TABLE [Minutes].[MinutesActionItem];

DROP TABLE [Minutes].[MinutesSource];

DROP INDEX [IX_WorkflowTask_Sla] ON [Workflow].[WorkflowTask];

DROP INDEX [IX_AgentRun_DepartmentId] ON [Agent].[AgentRun];

DROP INDEX [IX_AgentRun_OrganizationId] ON [Agent].[AgentRun];

DROP INDEX [UX_AgentProfile_ServiceUser] ON [Agent].[AgentProfile];

DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowTask]') AND [c].[name] = N'AgentRunId');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [Workflow].[WorkflowTask] DROP COLUMN [AgentRunId];

DECLARE @var1 nvarchar(max);
SELECT @var1 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowTask]') AND [c].[name] = N'DecisionMode');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT ' + @var1 + ';');
ALTER TABLE [Workflow].[WorkflowTask] DROP COLUMN [DecisionMode];

DECLARE @var2 nvarchar(max);
SELECT @var2 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowTask]') AND [c].[name] = N'DueAt');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT ' + @var2 + ';');
ALTER TABLE [Workflow].[WorkflowTask] DROP COLUMN [DueAt];

DECLARE @var3 nvarchar(max);
SELECT @var3 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowTask]') AND [c].[name] = N'EscalatedAt');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT ' + @var3 + ';');
ALTER TABLE [Workflow].[WorkflowTask] DROP COLUMN [EscalatedAt];

DECLARE @var4 nvarchar(max);
SELECT @var4 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowTask]') AND [c].[name] = N'NodePolicyId');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT ' + @var4 + ';');
ALTER TABLE [Workflow].[WorkflowTask] DROP COLUMN [NodePolicyId];

DECLARE @var5 nvarchar(max);
SELECT @var5 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowTask]') AND [c].[name] = N'ReminderSentAt');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT ' + @var5 + ';');
ALTER TABLE [Workflow].[WorkflowTask] DROP COLUMN [ReminderSentAt];

DECLARE @var6 nvarchar(max);
SELECT @var6 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowTask]') AND [c].[name] = N'SlaStatus');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowTask] DROP CONSTRAINT ' + @var6 + ';');
ALTER TABLE [Workflow].[WorkflowTask] DROP COLUMN [SlaStatus];

DECLARE @var7 nvarchar(max);
SELECT @var7 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentRun]') AND [c].[name] = N'BusinessObjectId');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentRun] DROP CONSTRAINT ' + @var7 + ';');
ALTER TABLE [Agent].[AgentRun] DROP COLUMN [BusinessObjectId];

DECLARE @var8 nvarchar(max);
SELECT @var8 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentRun]') AND [c].[name] = N'DepartmentId');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentRun] DROP CONSTRAINT ' + @var8 + ';');
ALTER TABLE [Agent].[AgentRun] DROP COLUMN [DepartmentId];

DECLARE @var9 nvarchar(max);
SELECT @var9 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentRun]') AND [c].[name] = N'EnvironmentCode');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentRun] DROP CONSTRAINT ' + @var9 + ';');
ALTER TABLE [Agent].[AgentRun] DROP COLUMN [EnvironmentCode];

DECLARE @var10 nvarchar(max);
SELECT @var10 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentRun]') AND [c].[name] = N'ExternalModelCalled');
IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentRun] DROP CONSTRAINT ' + @var10 + ';');
ALTER TABLE [Agent].[AgentRun] DROP COLUMN [ExternalModelCalled];

DECLARE @var11 nvarchar(max);
SELECT @var11 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentRun]') AND [c].[name] = N'GatewayCallId');
IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentRun] DROP CONSTRAINT ' + @var11 + ';');
ALTER TABLE [Agent].[AgentRun] DROP COLUMN [GatewayCallId];

DECLARE @var12 nvarchar(max);
SELECT @var12 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentRun]') AND [c].[name] = N'GatewayMode');
IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentRun] DROP CONSTRAINT ' + @var12 + ';');
ALTER TABLE [Agent].[AgentRun] DROP COLUMN [GatewayMode];

DECLARE @var13 nvarchar(max);
SELECT @var13 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentRun]') AND [c].[name] = N'IsTestData');
IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentRun] DROP CONSTRAINT ' + @var13 + ';');
ALTER TABLE [Agent].[AgentRun] DROP COLUMN [IsTestData];

DECLARE @var14 nvarchar(max);
SELECT @var14 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentRun]') AND [c].[name] = N'MinutesSourceId');
IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentRun] DROP CONSTRAINT ' + @var14 + ';');
ALTER TABLE [Agent].[AgentRun] DROP COLUMN [MinutesSourceId];

DECLARE @var15 nvarchar(max);
SELECT @var15 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentRun]') AND [c].[name] = N'OrganizationId');
IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentRun] DROP CONSTRAINT ' + @var15 + ';');
ALTER TABLE [Agent].[AgentRun] DROP COLUMN [OrganizationId];

DECLARE @var16 nvarchar(max);
SELECT @var16 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentRun]') AND [c].[name] = N'SubjectType');
IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentRun] DROP CONSTRAINT ' + @var16 + ';');
ALTER TABLE [Agent].[AgentRun] DROP COLUMN [SubjectType];

DECLARE @var17 nvarchar(max);
SELECT @var17 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentRun]') AND [c].[name] = N'WorkflowInstanceId');
IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentRun] DROP CONSTRAINT ' + @var17 + ';');
ALTER TABLE [Agent].[AgentRun] DROP COLUMN [WorkflowInstanceId];

DECLARE @var18 nvarchar(max);
SELECT @var18 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentRun]') AND [c].[name] = N'WorkflowTaskId');
IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentRun] DROP CONSTRAINT ' + @var18 + ';');
ALTER TABLE [Agent].[AgentRun] DROP COLUMN [WorkflowTaskId];

DECLARE @var19 nvarchar(max);
SELECT @var19 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentProfile]') AND [c].[name] = N'ServiceUserId');
IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentProfile] DROP CONSTRAINT ' + @var19 + ';');
ALTER TABLE [Agent].[AgentProfile] DROP COLUMN [ServiceUserId];

DELETE FROM [__EFMigrationsHistory]
WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway';

COMMIT;
GO
