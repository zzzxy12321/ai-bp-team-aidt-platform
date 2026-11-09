BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031044811_006_Batch2_1DocxPolicyAutoProxy'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD [AgentProxyMode] nvarchar(32) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031044811_006_Batch2_1DocxPolicyAutoProxy'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD [AutoProxyAfterMinutes] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031044811_006_Batch2_1DocxPolicyAutoProxy'
)
BEGIN
    ALTER TABLE [Agent].[AgentRun] ADD [CompensationStatus] nvarchar(32) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031044811_006_Batch2_1DocxPolicyAutoProxy'
)
BEGIN
    ALTER TABLE [Agent].[AgentRun] ADD [OriginalAssigneeId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031044811_006_Batch2_1DocxPolicyAutoProxy'
)
BEGIN
    ALTER TABLE [Agent].[AgentRun] ADD [ProxyIdempotencyKey] nvarchar(160) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031044811_006_Batch2_1DocxPolicyAutoProxy'
)
BEGIN
    ALTER TABLE [Agent].[AgentRun] ADD [ProxyTriggerMode] nvarchar(32) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031044811_006_Batch2_1DocxPolicyAutoProxy'
)
BEGIN
    ALTER TABLE [Agent].[AgentRun] ADD [ProxyTriggeredAt] datetime2(3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031044811_006_Batch2_1DocxPolicyAutoProxy'
)
BEGIN
    EXEC(N'ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD CONSTRAINT [CK_WorkflowNodePolicy_AutoProxyAfter] CHECK ([AutoProxyAfterMinutes] IS NULL OR [AutoProxyAfterMinutes] > 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031044811_006_Batch2_1DocxPolicyAutoProxy'
)
BEGIN
    EXEC(N'ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD CONSTRAINT [CK_WorkflowNodePolicy_ProxyMode] CHECK ([AgentProxyMode] IN (''RemindOnly'',''HumanConfirmedProxy'',''PreAuthorizedAutoProxy''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031044811_006_Batch2_1DocxPolicyAutoProxy'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_AgentRun_ProxyIdempotency] ON [Agent].[AgentRun] ([ProxyIdempotencyKey]) WHERE [ProxyIdempotencyKey] IS NOT NULL');
END;

GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031044811_006_Batch2_1DocxPolicyAutoProxy'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [Workflow].[WorkflowDefinition] WHERE Id='52000000-0000-0000-0000-000000000001')
        INSERT INTO [Workflow].[WorkflowDefinition] ([Id],[Code],[Name],[Status],[CreatedAt])
        VALUES ('52000000-0000-0000-0000-000000000001',N'RND_MATERIALS_CONFIRM',N'研发资料完整性确认',N'Published',SYSUTCDATETIME());

    IF NOT EXISTS (SELECT 1 FROM [Workflow].[WorkflowVersion] WHERE Id='52000000-0000-0000-0000-000000000002')
        INSERT INTO [Workflow].[WorkflowVersion] ([Id],[WorkflowDefinitionId],[VersionNumber],[Status],[DefinitionJson],[PublishedAt])
        VALUES ('52000000-0000-0000-0000-000000000002','52000000-0000-0000-0000-000000000001',1,N'Published',
        N'{"objectTypeCode":"CUSTOMER_SAMPLE","startNode":"MATERIALS_CONFIRM","nodes":[{"code":"MATERIALS_CONFIRM","name":"研发资料完整性确认","assigneeUserName":"demo.researcher","nextNode":null,"completeOnApprove":true}]}',SYSUTCDATETIME());

    DECLARE @ProxyProfileId uniqueidentifier = (SELECT Id FROM [Agent].[AgentProfile] WHERE Name=N'流程代理数字员工' AND IsActive=1);
    DECLARE @PublisherId uniqueidentifier = (SELECT Id FROM [Identity].[User] WHERE UserName=N'demo.admin' AND IsActive=1);
    IF @ProxyProfileId IS NOT NULL AND @PublisherId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM [Workflow].[WorkflowNodePolicy] WHERE Id='52000000-0000-0000-0000-000000000003')
        INSERT INTO [Workflow].[WorkflowNodePolicy]
            ([Id],[WorkflowVersionId],[NodeCode],[VersionNumber],[ApproverUserId],[BackupApproverUserId],
             [SlaMinutes],[ReminderMinutes],[EscalationMinutes],[TimeoutStrategy],[AllowAgentAssist],[AllowAgentProxy],
             [AgentProfileId],[AgentMaxRisk],[AgentMaxAmount],[AgentMaxAutonomy],[AgentProxyMode],[AutoProxyAfterMinutes],
             [PermanentHumanBoundary],[RequiredAttachmentCount],[PrecheckItems],[Status],[PublishedBy],[PublishedAt])
        VALUES
            ('52000000-0000-0000-0000-000000000003','52000000-0000-0000-0000-000000000002',N'MATERIALS_CONFIRM',1,
             (SELECT Id FROM [Identity].[User] WHERE UserName=N'demo.researcher' AND IsActive=1),
             (SELECT Id FROM [Identity].[User] WHERE UserName=N'demo.admin' AND IsActive=1),
             120,30,60,N'Escalate',1,1,@ProxyProfileId,N'Low',0,N'A2',N'PreAuthorizedAutoProxy',60,
             0,0,N'确认研发资料、附件和输入信息完整',N'Published',@PublisherId,SYSUTCDATETIME());
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031044811_006_Batch2_1DocxPolicyAutoProxy'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031044811_006_Batch2_1DocxPolicyAutoProxy', N'10.0.14');
END;

COMMIT;
GO

