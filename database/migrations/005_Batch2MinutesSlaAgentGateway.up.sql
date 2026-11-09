BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    IF SCHEMA_ID(N'Minutes') IS NULL EXEC(N'CREATE SCHEMA [Minutes];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD [AgentRunId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD [DecisionMode] nvarchar(32) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD [DueAt] datetime2(3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD [EscalatedAt] datetime2(3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD [NodePolicyId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD [ReminderSentAt] datetime2(3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD [SlaStatus] nvarchar(32) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    ALTER TABLE [Agent].[AgentRun] ADD [BusinessObjectId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    ALTER TABLE [Agent].[AgentRun] ADD [DepartmentId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    ALTER TABLE [Agent].[AgentRun] ADD [EnvironmentCode] nvarchar(16) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    ALTER TABLE [Agent].[AgentRun] ADD [ExternalModelCalled] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    ALTER TABLE [Agent].[AgentRun] ADD [GatewayCallId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    ALTER TABLE [Agent].[AgentRun] ADD [GatewayMode] nvarchar(32) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    ALTER TABLE [Agent].[AgentRun] ADD [IsTestData] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    ALTER TABLE [Agent].[AgentRun] ADD [MinutesSourceId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    ALTER TABLE [Agent].[AgentRun] ADD [OrganizationId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    ALTER TABLE [Agent].[AgentRun] ADD [SubjectType] nvarchar(64) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    ALTER TABLE [Agent].[AgentRun] ADD [WorkflowInstanceId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    ALTER TABLE [Agent].[AgentRun] ADD [WorkflowTaskId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    ALTER TABLE [Agent].[AgentProfile] ADD [ServiceUserId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [Identity].[User] WHERE Id='10000000-0000-0000-0000-000000000019')
        INSERT INTO [Identity].[User]
            (Id, OrganizationId, UserName, DisplayName, IsServiceIdentity, IsActive, CreatedAt)
        VALUES
            ('10000000-0000-0000-0000-000000000019',
             '10000000-0000-0000-0000-000000000001',
             N'agent.demo', N'业务协同数字员工', 1, 1, SYSUTCDATETIME());

    IF NOT EXISTS (SELECT 1 FROM [Identity].[User] WHERE Id='10000000-0000-0000-0000-000000000020')
        INSERT INTO [Identity].[User]
            (Id, OrganizationId, UserName, DisplayName, IsServiceIdentity, IsActive, CreatedAt)
        VALUES
            ('10000000-0000-0000-0000-000000000020',
             '10000000-0000-0000-0000-000000000001',
             N'agent.proxy', N'流程代理数字员工', 1, 1, SYSUTCDATETIME());

    EXEC(N'UPDATE [Agent].[AgentProfile]
           SET [ServiceUserId]=''10000000-0000-0000-0000-000000000019''
           WHERE [ServiceUserId]=''00000000-0000-0000-0000-000000000000'';');

    IF NOT EXISTS (SELECT 1 FROM [Agent].[AgentProfile] WHERE Name=N'流程代理数字员工')
        EXEC(N'INSERT INTO [Agent].[AgentProfile]
            (Id, ServiceUserId, Name, AutonomyLevel, AmountLimit, RiskLimit, AllowedToolsJson, IsActive)
        VALUES
            (''60000000-0000-0000-0000-000000000002'',
             ''10000000-0000-0000-0000-000000000020'',
             N''流程代理数字员工'', N''A2'', 0, N''Low'',
             N''["workflow.read","workflow.assist","workflow.proxy-low-risk"]'', 1);');

    EXEC(N'UPDATE runRow
    SET OrganizationId = userRow.OrganizationId,
        DepartmentId = COALESCE(
            (SELECT TOP (1) membership.DepartmentId
             FROM [Identity].[UserDepartmentMembership] membership
             WHERE membership.UserId=runRow.RequestedBy AND membership.IsActive=1
             ORDER BY membership.IsPrimary DESC, membership.JoinedAt),
            (SELECT TOP (1) dept.Id FROM [Identity].[Department] dept
             WHERE dept.OrganizationId=userRow.OrganizationId AND dept.IsActive=1
             ORDER BY dept.Code)),
        EnvironmentCode=''demo'',
        IsTestData=1,
        SubjectType=COALESCE(SubjectType, ''Legacy'')
    FROM [Agent].[AgentRun] runRow
    JOIN [Identity].[User] userRow ON userRow.Id=runRow.RequestedBy;');

    EXEC(N'UPDATE [Workflow].[WorkflowTask]
           SET DecisionMode=''Human'', SlaStatus=''None''
           WHERE DecisionMode=N'''' OR SlaStatus=N'''';');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE TABLE [Agent].[AgentGatewayCall] (
        [Id] uniqueidentifier NOT NULL,
        [AgentRunId] uniqueidentifier NOT NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [EnvironmentCode] nvarchar(16) NOT NULL,
        [Model] nvarchar(128) NOT NULL,
        [Mode] nvarchar(32) NOT NULL,
        [GatewayCallId] nvarchar(128) NULL,
        [ExternalModelCalled] bit NOT NULL,
        [InputDigest] nvarchar(128) NOT NULL,
        [OutputDigest] nvarchar(128) NULL,
        [Status] nvarchar(32) NOT NULL,
        [ErrorCode] nvarchar(64) NULL,
        [ErrorMessage] nvarchar(2000) NULL,
        [DurationMs] int NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_AgentGatewayCall] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AgentGatewayCall_Department] FOREIGN KEY ([DepartmentId]) REFERENCES [Identity].[Department] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AgentGatewayCall_Organization] FOREIGN KEY ([OrganizationId]) REFERENCES [Identity].[Organization] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AgentGatewayCall_Run] FOREIGN KEY ([AgentRunId]) REFERENCES [Agent].[AgentRun] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE TABLE [Minutes].[MinutesSource] (
        [Id] uniqueidentifier NOT NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [AttachmentId] uniqueidentifier NULL,
        [Title] nvarchar(255) NOT NULL,
        [MeetingAt] datetime2(3) NOT NULL,
        [SourceType] nvarchar(32) NOT NULL,
        [Transcript] nvarchar(max) NOT NULL,
        [EnvironmentCode] nvarchar(16) NOT NULL,
        [IsTestData] bit NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [AnalysisVersion] nvarchar(64) NOT NULL,
        [AnalysisRunId] uniqueidentifier NULL,
        [CreatedBy] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_MinutesSource] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MinutesSource_Environment] CHECK ([EnvironmentCode] IN ('demo','test','prod')),
        CONSTRAINT [CK_MinutesSource_Type] CHECK ([SourceType] IN ('ManualText','ManualUpload')),
        CONSTRAINT [FK_MinutesSource_AnalysisRun] FOREIGN KEY ([AnalysisRunId]) REFERENCES [Agent].[AgentRun] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MinutesSource_Attachment] FOREIGN KEY ([AttachmentId]) REFERENCES [Document].[Attachment] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MinutesSource_Department] FOREIGN KEY ([DepartmentId]) REFERENCES [Identity].[Department] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MinutesSource_Organization] FOREIGN KEY ([OrganizationId]) REFERENCES [Identity].[Organization] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE TABLE [Workflow].[WorkflowNodePolicy] (
        [Id] uniqueidentifier NOT NULL,
        [WorkflowVersionId] uniqueidentifier NOT NULL,
        [NodeCode] nvarchar(80) NOT NULL,
        [VersionNumber] int NOT NULL,
        [ApproverUserId] uniqueidentifier NULL,
        [BackupApproverUserId] uniqueidentifier NULL,
        [SlaMinutes] int NULL,
        [ReminderMinutes] int NULL,
        [EscalationMinutes] int NULL,
        [TimeoutStrategy] nvarchar(32) NOT NULL,
        [AllowAgentAssist] bit NOT NULL,
        [AllowAgentProxy] bit NOT NULL,
        [AgentProfileId] uniqueidentifier NULL,
        [AgentMaxRisk] nvarchar(16) NOT NULL,
        [AgentMaxAmount] decimal(18,4) NOT NULL,
        [AgentMaxAutonomy] nvarchar(8) NOT NULL,
        [PermanentHumanBoundary] bit NOT NULL,
        [RequiredAttachmentCount] int NOT NULL,
        [PrecheckItems] nvarchar(2000) NULL,
        [Status] nvarchar(32) NOT NULL,
        [PublishedBy] uniqueidentifier NOT NULL,
        [PublishedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_WorkflowNodePolicy] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_WorkflowNodePolicy_Escalation] CHECK ([EscalationMinutes] IS NULL OR [EscalationMinutes] >= 0),
        CONSTRAINT [CK_WorkflowNodePolicy_Reminder] CHECK ([ReminderMinutes] IS NULL OR [ReminderMinutes] >= 0),
        CONSTRAINT [CK_WorkflowNodePolicy_Risk] CHECK ([AgentMaxRisk] IN ('Low','Medium','High')),
        CONSTRAINT [CK_WorkflowNodePolicy_Sla] CHECK ([SlaMinutes] IS NULL OR [SlaMinutes] > 0),
        CONSTRAINT [CK_WorkflowNodePolicy_Status] CHECK ([Status] IN ('Draft','Published','Retired')),
        CONSTRAINT [FK_WorkflowNodePolicy_AgentProfile] FOREIGN KEY ([AgentProfileId]) REFERENCES [Agent].[AgentProfile] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkflowNodePolicy_Approver] FOREIGN KEY ([ApproverUserId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkflowNodePolicy_BackupApprover] FOREIGN KEY ([BackupApproverUserId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkflowNodePolicy_PublishedBy] FOREIGN KEY ([PublishedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkflowNodePolicy_WorkflowVersion] FOREIGN KEY ([WorkflowVersionId]) REFERENCES [Workflow].[WorkflowVersion] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE TABLE [Workflow].[WorkflowNotification] (
        [Id] uniqueidentifier NOT NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [RecipientId] uniqueidentifier NOT NULL,
        [WorkflowTaskId] uniqueidentifier NOT NULL,
        [Type] nvarchar(32) NOT NULL,
        [Title] nvarchar(255) NOT NULL,
        [Message] nvarchar(2000) NOT NULL,
        [IdempotencyKey] nvarchar(160) NOT NULL,
        [IsRead] bit NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_WorkflowNotification] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkflowNotification_Department] FOREIGN KEY ([DepartmentId]) REFERENCES [Identity].[Department] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkflowNotification_Organization] FOREIGN KEY ([OrganizationId]) REFERENCES [Identity].[Organization] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkflowNotification_Recipient] FOREIGN KEY ([RecipientId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkflowNotification_Task] FOREIGN KEY ([WorkflowTaskId]) REFERENCES [Workflow].[WorkflowTask] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE TABLE [Minutes].[MinutesActionItem] (
        [Id] uniqueidentifier NOT NULL,
        [MinutesSourceId] uniqueidentifier NOT NULL,
        [Speaker] nvarchar(128) NOT NULL,
        [SourceTimestamp] nvarchar(32) NOT NULL,
        [SourceExcerpt] nvarchar(2000) NOT NULL,
        [OriginalContent] nvarchar(2000) NOT NULL,
        [Content] nvarchar(2000) NOT NULL,
        [SuggestedAssigneeId] uniqueidentifier NULL,
        [AssigneeId] uniqueidentifier NULL,
        [SuggestedDueAt] datetime2(3) NULL,
        [DueAt] datetime2(3) NULL,
        [Priority] nvarchar(16) NOT NULL,
        [SuggestedObjectTypeId] uniqueidentifier NULL,
        [ObjectTypeId] uniqueidentifier NULL,
        [SuggestedWorkflowVersionId] uniqueidentifier NULL,
        [WorkflowVersionId] uniqueidentifier NULL,
        [MissingInformation] nvarchar(1000) NOT NULL,
        [Confidence] decimal(5,4) NOT NULL,
        [AnalysisVersion] nvarchar(64) NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [ConfirmedBy] uniqueidentifier NULL,
        [ConfirmedAt] datetime2(3) NULL,
        [MinutesTaskId] uniqueidentifier NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_MinutesActionItem] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MinutesActionItem_Confidence] CHECK ([Confidence] >= 0 AND [Confidence] <= 1),
        CONSTRAINT [CK_MinutesActionItem_Status] CHECK ([Status] IN ('Draft','Confirmed')),
        CONSTRAINT [FK_MinutesActionItem_Assignee] FOREIGN KEY ([AssigneeId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MinutesActionItem_ObjectType] FOREIGN KEY ([ObjectTypeId]) REFERENCES [Metadata].[ObjectType] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MinutesActionItem_Source] FOREIGN KEY ([MinutesSourceId]) REFERENCES [Minutes].[MinutesSource] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MinutesActionItem_WorkflowVersion] FOREIGN KEY ([WorkflowVersionId]) REFERENCES [Workflow].[WorkflowVersion] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE TABLE [Minutes].[MinutesTask] (
        [Id] uniqueidentifier NOT NULL,
        [MinutesSourceId] uniqueidentifier NOT NULL,
        [ActionItemId] uniqueidentifier NOT NULL,
        [AttachmentId] uniqueidentifier NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [AssigneeId] uniqueidentifier NOT NULL,
        [Title] nvarchar(255) NOT NULL,
        [Content] nvarchar(2000) NOT NULL,
        [DueAt] datetime2(3) NULL,
        [Priority] nvarchar(16) NOT NULL,
        [BusinessObjectId] uniqueidentifier NULL,
        [WorkflowInstanceId] uniqueidentifier NULL,
        [EnvironmentCode] nvarchar(16) NOT NULL,
        [IsTestData] bit NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [CreatedBy] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_MinutesTask] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MinutesTask_Environment] CHECK ([EnvironmentCode] IN ('demo','test','prod')),
        CONSTRAINT [CK_MinutesTask_Status] CHECK ([Status] IN ('Pending','Completed','Cancelled')),
        CONSTRAINT [FK_MinutesTask_ActionItem] FOREIGN KEY ([ActionItemId]) REFERENCES [Minutes].[MinutesActionItem] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MinutesTask_Assignee] FOREIGN KEY ([AssigneeId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MinutesTask_Attachment] FOREIGN KEY ([AttachmentId]) REFERENCES [Document].[Attachment] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MinutesTask_BusinessObject] FOREIGN KEY ([BusinessObjectId]) REFERENCES [BusinessObject].[BusinessObject] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MinutesTask_Source] FOREIGN KEY ([MinutesSourceId]) REFERENCES [Minutes].[MinutesSource] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MinutesTask_WorkflowInstance] FOREIGN KEY ([WorkflowInstanceId]) REFERENCES [Workflow].[WorkflowInstance] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    INSERT INTO [Identity].[Permission] (Id, Code, Resource, Action)
    SELECT permissionSeed.Id, permissionSeed.Code, permissionSeed.Resource, permissionSeed.Action
    FROM (VALUES
        (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000028'), N'minutes-read', N'minutes', N'Read'),
        (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000029'), N'minutes-create', N'minutes', N'Create'),
        (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000030'), N'minutes-update', N'minutes', N'Update'),
        (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000031'), N'minutes-approve', N'minutes', N'Approve'),
        (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000032'), N'workflow-policy-read', N'workflow-policy', N'Read'),
        (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000033'), N'workflow-policy-update', N'workflow-policy', N'Update'),
        (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000034'), N'workflow-sla-update', N'workflow-sla', N'Update'),
        (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000035'), N'agent-run-update', N'agent-run', N'Update')
    ) permissionSeed(Id, Code, Resource, Action)
    WHERE NOT EXISTS (
        SELECT 1 FROM [Identity].[Permission] existing WHERE existing.Code=permissionSeed.Code
    );

    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT roleRow.Id, permissionRow.Id
    FROM [Identity].[Role] roleRow
    CROSS JOIN [Identity].[Permission] permissionRow
    WHERE roleRow.Code IN
        (N'platform-admin', N'rd-engineer', N'quality-engineer', N'supply-specialist',
         N'finance-approver', N'production-team')
      AND permissionRow.Code IN
        (N'minutes-read', N'minutes-create', N'minutes-update', N'minutes-approve',
         N'workflow-policy-read', N'agent-run-update')
      AND NOT EXISTS (
          SELECT 1 FROM [Identity].[RolePermission] existing
          WHERE existing.RoleId=roleRow.Id AND existing.PermissionId=permissionRow.Id
      );

    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT roleRow.Id, permissionRow.Id
    FROM [Identity].[Role] roleRow
    CROSS JOIN [Identity].[Permission] permissionRow
    WHERE roleRow.Code=N'platform-admin'
      AND permissionRow.Code IN (N'workflow-policy-update', N'workflow-sla-update')
      AND NOT EXISTS (
          SELECT 1 FROM [Identity].[RolePermission] existing
          WHERE existing.RoleId=roleRow.Id AND existing.PermissionId=permissionRow.Id
      );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_WorkflowTask_Sla] ON [Workflow].[WorkflowTask] ([Status], [DueAt], [SlaStatus]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_AgentRun_DepartmentId] ON [Agent].[AgentRun] ([DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_AgentRun_OrganizationId] ON [Agent].[AgentRun] ([OrganizationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE UNIQUE INDEX [UX_AgentProfile_ServiceUser] ON [Agent].[AgentProfile] ([ServiceUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_AgentGatewayCall_DepartmentId] ON [Agent].[AgentGatewayCall] ([DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_AgentGatewayCall_OrganizationId] ON [Agent].[AgentGatewayCall] ([OrganizationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_AgentGatewayCall_Run_CreatedAt] ON [Agent].[AgentGatewayCall] ([AgentRunId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_MinutesActionItem_AssigneeId] ON [Minutes].[MinutesActionItem] ([AssigneeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_MinutesActionItem_ObjectTypeId] ON [Minutes].[MinutesActionItem] ([ObjectTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_MinutesActionItem_Source_Status] ON [Minutes].[MinutesActionItem] ([MinutesSourceId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_MinutesActionItem_WorkflowVersionId] ON [Minutes].[MinutesActionItem] ([WorkflowVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_MinutesSource_AnalysisRunId] ON [Minutes].[MinutesSource] ([AnalysisRunId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_MinutesSource_AttachmentId] ON [Minutes].[MinutesSource] ([AttachmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_MinutesSource_Department_Environment] ON [Minutes].[MinutesSource] ([DepartmentId], [EnvironmentCode], [CreatedAt], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_MinutesSource_OrganizationId] ON [Minutes].[MinutesSource] ([OrganizationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_MinutesTask_Assignee_Status_Due] ON [Minutes].[MinutesTask] ([AssigneeId], [Status], [DueAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_MinutesTask_AttachmentId] ON [Minutes].[MinutesTask] ([AttachmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_MinutesTask_BusinessObjectId] ON [Minutes].[MinutesTask] ([BusinessObjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_MinutesTask_MinutesSourceId] ON [Minutes].[MinutesTask] ([MinutesSourceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_MinutesTask_WorkflowInstanceId] ON [Minutes].[MinutesTask] ([WorkflowInstanceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE UNIQUE INDEX [UX_MinutesTask_ActionItem] ON [Minutes].[MinutesTask] ([ActionItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_WorkflowNodePolicy_AgentProfileId] ON [Workflow].[WorkflowNodePolicy] ([AgentProfileId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_WorkflowNodePolicy_ApproverUserId] ON [Workflow].[WorkflowNodePolicy] ([ApproverUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_WorkflowNodePolicy_BackupApproverUserId] ON [Workflow].[WorkflowNodePolicy] ([BackupApproverUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_WorkflowNodePolicy_PublishedBy] ON [Workflow].[WorkflowNodePolicy] ([PublishedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_WorkflowNodePolicy_Version_Node_Status] ON [Workflow].[WorkflowNodePolicy] ([WorkflowVersionId], [NodeCode], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE UNIQUE INDEX [UX_WorkflowNodePolicy_Version_Node_VersionNumber] ON [Workflow].[WorkflowNodePolicy] ([WorkflowVersionId], [NodeCode], [VersionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_WorkflowNotification_DepartmentId] ON [Workflow].[WorkflowNotification] ([DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_WorkflowNotification_OrganizationId] ON [Workflow].[WorkflowNotification] ([OrganizationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_WorkflowNotification_Recipient] ON [Workflow].[WorkflowNotification] ([RecipientId], [IsRead], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE INDEX [IX_WorkflowNotification_WorkflowTaskId] ON [Workflow].[WorkflowNotification] ([WorkflowTaskId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    CREATE UNIQUE INDEX [UX_WorkflowNotification_Idempotency] ON [Workflow].[WorkflowNotification] ([IdempotencyKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    ALTER TABLE [Agent].[AgentProfile] ADD CONSTRAINT [FK_AgentProfile_ServiceUser] FOREIGN KEY ([ServiceUserId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    ALTER TABLE [Agent].[AgentRun] ADD CONSTRAINT [FK_AgentRun_Department] FOREIGN KEY ([DepartmentId]) REFERENCES [Identity].[Department] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    ALTER TABLE [Agent].[AgentRun] ADD CONSTRAINT [FK_AgentRun_Organization] FOREIGN KEY ([OrganizationId]) REFERENCES [Identity].[Organization] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031014600_005_Batch2MinutesSlaAgentGateway'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031014600_005_Batch2MinutesSlaAgentGateway', N'10.0.14');
END;

COMMIT;
GO
