BEGIN TRANSACTION;
IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031044811_006_Batch2_1DocxPolicyAutoProxy'
)
BEGIN
    DELETE FROM [Workflow].[WorkflowNodePolicy] WHERE Id='52000000-0000-0000-0000-000000000003';
    DELETE FROM [Workflow].[WorkflowVersion] WHERE Id='52000000-0000-0000-0000-000000000002';
    DELETE FROM [Workflow].[WorkflowDefinition] WHERE Id='52000000-0000-0000-0000-000000000001';
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031044811_006_Batch2_1DocxPolicyAutoProxy'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT [CK_WorkflowNodePolicy_AutoProxyAfter];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031044811_006_Batch2_1DocxPolicyAutoProxy'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT [CK_WorkflowNodePolicy_ProxyMode];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031044811_006_Batch2_1DocxPolicyAutoProxy'
)
BEGIN
    DROP INDEX [UX_AgentRun_ProxyIdempotency] ON [Agent].[AgentRun];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031044811_006_Batch2_1DocxPolicyAutoProxy'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowNodePolicy]') AND [c].[name] = N'AgentProxyMode');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP COLUMN [AgentProxyMode];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031044811_006_Batch2_1DocxPolicyAutoProxy'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Workflow].[WorkflowNodePolicy]') AND [c].[name] = N'AutoProxyAfterMinutes');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Workflow].[WorkflowNodePolicy] DROP COLUMN [AutoProxyAfterMinutes];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031044811_006_Batch2_1DocxPolicyAutoProxy'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentRun]') AND [c].[name] = N'CompensationStatus');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentRun] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [Agent].[AgentRun] DROP COLUMN [CompensationStatus];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031044811_006_Batch2_1DocxPolicyAutoProxy'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentRun]') AND [c].[name] = N'OriginalAssigneeId');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentRun] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [Agent].[AgentRun] DROP COLUMN [OriginalAssigneeId];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031044811_006_Batch2_1DocxPolicyAutoProxy'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentRun]') AND [c].[name] = N'ProxyIdempotencyKey');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentRun] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [Agent].[AgentRun] DROP COLUMN [ProxyIdempotencyKey];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031044811_006_Batch2_1DocxPolicyAutoProxy'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentRun]') AND [c].[name] = N'ProxyTriggerMode');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentRun] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [Agent].[AgentRun] DROP COLUMN [ProxyTriggerMode];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031044811_006_Batch2_1DocxPolicyAutoProxy'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Agent].[AgentRun]') AND [c].[name] = N'ProxyTriggeredAt');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Agent].[AgentRun] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [Agent].[AgentRun] DROP COLUMN [ProxyTriggeredAt];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031044811_006_Batch2_1DocxPolicyAutoProxy'
)
BEGIN
    DELETE FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031044811_006_Batch2_1DocxPolicyAutoProxy';
END;

COMMIT;
GO

