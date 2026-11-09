BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET CONCAT_NULL_YIELDS_NULL ON; SET NUMERIC_ROUNDABORT OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    IF SCHEMA_ID(N'PMO') IS NULL EXEC(N'CREATE SCHEMA [PMO];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    ALTER TABLE [BusinessObject].[BusinessObject] ADD [CreatedByUserId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    ALTER TABLE [BusinessObject].[BusinessObject] ADD [Title] nvarchar(255) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE TABLE [PMO].[EventInbox] (
        [Id] uniqueidentifier NOT NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [SourceType] nvarchar(32) NOT NULL,
        [SourceId] uniqueidentifier NULL,
        [AttachmentId] uniqueidentifier NULL,
        [Title] nvarchar(255) NOT NULL,
        [RawText] nvarchar(max) NOT NULL,
        [Status] nvarchar(450) NOT NULL,
        [SubmittedBy] uniqueidentifier NOT NULL,
        [AnalysisRunId] uniqueidentifier NULL,
        [EnvironmentCode] nvarchar(16) NOT NULL,
        [IsTestData] bit NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_EventInbox] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_EventInbox_Environment] CHECK ([EnvironmentCode] IN ('demo','test','prod')),
        CONSTRAINT [CK_EventInbox_Source] CHECK ([SourceType] IN ('ManualText','ManualUpload','MinutesSource','MinutesActionItem','WorkflowNotification','Attachment')),
        CONSTRAINT [CK_EventInbox_Status] CHECK ([Status] IN ('Received','AwaitingConfirmation','Confirmed','Rejected')),
        CONSTRAINT [FK_EventInbox_AnalysisRun] FOREIGN KEY ([AnalysisRunId]) REFERENCES [Agent].[AgentRun] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EventInbox_Attachment] FOREIGN KEY ([AttachmentId]) REFERENCES [Document].[Attachment] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EventInbox_Department] FOREIGN KEY ([DepartmentId]) REFERENCES [Identity].[Department] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EventInbox_Organization] FOREIGN KEY ([OrganizationId]) REFERENCES [Identity].[Organization] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EventInbox_SubmittedBy] FOREIGN KEY ([SubmittedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE TABLE [PMO].[ProjectMember] (
        [BusinessObjectId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [ProjectRole] nvarchar(24) NOT NULL,
        [IsLead] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedBy] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [RemovedBy] uniqueidentifier NULL,
        [RemovedAt] datetime2(3) NULL,
        CONSTRAINT [PK_ProjectMember] PRIMARY KEY ([BusinessObjectId], [UserId]),
        CONSTRAINT [CK_ProjectMember_Role] CHECK ([ProjectRole] IN ('Lead','Manager','Contributor','Viewer')),
        CONSTRAINT [FK_ProjectMember_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectMember_Project] FOREIGN KEY ([BusinessObjectId]) REFERENCES [BusinessObject].[BusinessObject] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectMember_RemovedBy] FOREIGN KEY ([RemovedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectMember_User] FOREIGN KEY ([UserId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE TABLE [PMO].[ProjectMilestone] (
        [Id] uniqueidentifier NOT NULL,
        [ProjectId] uniqueidentifier NOT NULL,
        [Title] nvarchar(255) NOT NULL,
        [OwnerUserId] uniqueidentifier NOT NULL,
        [PlannedDate] datetime2(3) NOT NULL,
        [Status] nvarchar(450) NOT NULL,
        [CompletedAt] datetime2(3) NULL,
        [Version] bigint NOT NULL,
        [CreatedBy] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_ProjectMilestone] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ProjectMilestone_Status] CHECK ([Status] IN ('Planned','InProgress','Completed','AtRisk','Cancelled')),
        CONSTRAINT [FK_ProjectMilestone_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectMilestone_Owner] FOREIGN KEY ([OwnerUserId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectMilestone_Project] FOREIGN KEY ([ProjectId]) REFERENCES [BusinessObject].[BusinessObject] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE TABLE [PMO].[ProjectProfile] (
        [BusinessObjectId] uniqueidentifier NOT NULL,
        [OwnerUserId] uniqueidentifier NOT NULL,
        [PlannedStartDate] datetime2(3) NULL,
        [PlannedEndDate] datetime2(3) NULL,
        [Objective] nvarchar(2000) NOT NULL,
        [CurrentStage] nvarchar(80) NOT NULL,
        [ConfidentialityLevel] nvarchar(16) NOT NULL,
        [ProgressPercent] int NOT NULL,
        [CreatedBy] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_ProjectProfile] PRIMARY KEY ([BusinessObjectId]),
        CONSTRAINT [CK_ProjectProfile_Confidentiality] CHECK ([ConfidentialityLevel] IN ('Internal','Sensitive','Core')),
        CONSTRAINT [CK_ProjectProfile_Dates] CHECK ([PlannedEndDate] IS NULL OR [PlannedStartDate] IS NULL OR [PlannedEndDate] >= [PlannedStartDate]),
        CONSTRAINT [CK_ProjectProfile_Progress] CHECK ([ProgressPercent] BETWEEN 0 AND 100),
        CONSTRAINT [FK_ProjectProfile_BusinessObject] FOREIGN KEY ([BusinessObjectId]) REFERENCES [BusinessObject].[BusinessObject] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectProfile_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectProfile_Owner] FOREIGN KEY ([OwnerUserId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE TABLE [PMO].[ProjectRelation] (
        [Id] uniqueidentifier NOT NULL,
        [ProjectId] uniqueidentifier NOT NULL,
        [EntityType] nvarchar(64) NOT NULL,
        [EntityId] uniqueidentifier NOT NULL,
        [RelationCode] nvarchar(64) NOT NULL,
        [EvidenceExcerpt] nvarchar(4000) NULL,
        [CreatedBy] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_ProjectRelation] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectRelation_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectRelation_Project] FOREIGN KEY ([ProjectId]) REFERENCES [BusinessObject].[BusinessObject] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE TABLE [PMO].[EventSuggestion] (
        [Id] uniqueidentifier NOT NULL,
        [EventInboxId] uniqueidentifier NOT NULL,
        [AgentRunId] uniqueidentifier NOT NULL,
        [SuggestedProjectId] uniqueidentifier NULL,
        [SuggestedBusinessObjectId] uniqueidentifier NULL,
        [SuggestedAssigneeId] uniqueidentifier NULL,
        [SuggestedWorkflowVersionId] uniqueidentifier NULL,
        [SuggestedDueAt] datetime2(3) NULL,
        [Priority] nvarchar(8) NOT NULL,
        [RiskLevel] nvarchar(16) NOT NULL,
        [RequiresHumanBoundary] bit NOT NULL,
        [BoundaryReason] nvarchar(500) NULL,
        [MissingInformation] nvarchar(1000) NOT NULL,
        [EvidenceExcerpt] nvarchar(4000) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_EventSuggestion] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_EventSuggestion_Priority] CHECK ([Priority] IN ('P0','P1','P2')),
        CONSTRAINT [CK_EventSuggestion_Risk] CHECK ([RiskLevel] IN ('Low','Medium','High')),
        CONSTRAINT [CK_EventSuggestion_Status] CHECK ([Status] IN ('Suggested','HighRisk','Confirmed','Rejected')),
        CONSTRAINT [FK_EventSuggestion_AgentRun] FOREIGN KEY ([AgentRunId]) REFERENCES [Agent].[AgentRun] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EventSuggestion_Assignee] FOREIGN KEY ([SuggestedAssigneeId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EventSuggestion_BusinessObject] FOREIGN KEY ([SuggestedBusinessObjectId]) REFERENCES [BusinessObject].[BusinessObject] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EventSuggestion_EventInbox] FOREIGN KEY ([EventInboxId]) REFERENCES [PMO].[EventInbox] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EventSuggestion_Project] FOREIGN KEY ([SuggestedProjectId]) REFERENCES [BusinessObject].[BusinessObject] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EventSuggestion_WorkflowVersion] FOREIGN KEY ([SuggestedWorkflowVersionId]) REFERENCES [Workflow].[WorkflowVersion] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE TABLE [PMO].[ProjectTask] (
        [Id] uniqueidentifier NOT NULL,
        [ProjectId] uniqueidentifier NOT NULL,
        [EventInboxId] uniqueidentifier NULL,
        [AssigneeId] uniqueidentifier NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [Title] nvarchar(255) NOT NULL,
        [Content] nvarchar(4000) NOT NULL,
        [DueAt] datetime2(3) NULL,
        [Priority] nvarchar(8) NOT NULL,
        [Status] nvarchar(450) NOT NULL,
        [Version] bigint NOT NULL,
        [BusinessObjectId] uniqueidentifier NULL,
        [WorkflowInstanceId] uniqueidentifier NULL,
        [WorkflowTaskId] uniqueidentifier NULL,
        [EnvironmentCode] nvarchar(16) NOT NULL,
        [IsTestData] bit NOT NULL,
        [CreatedBy] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [CompletedAt] datetime2(3) NULL,
        CONSTRAINT [PK_ProjectTask] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ProjectTask_Environment] CHECK ([EnvironmentCode] IN ('demo','test','prod')),
        CONSTRAINT [CK_ProjectTask_Priority] CHECK ([Priority] IN ('P0','P1','P2')),
        CONSTRAINT [CK_ProjectTask_Status] CHECK ([Status] IN ('Pending','InProgress','Completed','Cancelled')),
        CONSTRAINT [FK_ProjectTask_Assignee] FOREIGN KEY ([AssigneeId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectTask_BusinessObject] FOREIGN KEY ([BusinessObjectId]) REFERENCES [BusinessObject].[BusinessObject] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectTask_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectTask_Department] FOREIGN KEY ([DepartmentId]) REFERENCES [Identity].[Department] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectTask_EventInbox] FOREIGN KEY ([EventInboxId]) REFERENCES [PMO].[EventInbox] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectTask_Project] FOREIGN KEY ([ProjectId]) REFERENCES [BusinessObject].[BusinessObject] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectTask_WorkflowInstance] FOREIGN KEY ([WorkflowInstanceId]) REFERENCES [Workflow].[WorkflowInstance] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectTask_WorkflowTask] FOREIGN KEY ([WorkflowTaskId]) REFERENCES [Workflow].[WorkflowTask] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_BusinessObject_CreatedByUserId] ON [BusinessObject].[BusinessObject] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_EventInbox_AnalysisRunId] ON [PMO].[EventInbox] ([AnalysisRunId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_EventInbox_AttachmentId] ON [PMO].[EventInbox] ([AttachmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_EventInbox_Department_Environment_Status] ON [PMO].[EventInbox] ([DepartmentId], [EnvironmentCode], [Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_EventInbox_OrganizationId] ON [PMO].[EventInbox] ([OrganizationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_EventInbox_Submitter_CreatedAt] ON [PMO].[EventInbox] ([SubmittedBy], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_EventSuggestion_AgentRunId] ON [PMO].[EventSuggestion] ([AgentRunId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_EventSuggestion_SuggestedAssigneeId] ON [PMO].[EventSuggestion] ([SuggestedAssigneeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_EventSuggestion_SuggestedBusinessObjectId] ON [PMO].[EventSuggestion] ([SuggestedBusinessObjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_EventSuggestion_SuggestedProjectId] ON [PMO].[EventSuggestion] ([SuggestedProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_EventSuggestion_SuggestedWorkflowVersionId] ON [PMO].[EventSuggestion] ([SuggestedWorkflowVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE UNIQUE INDEX [UX_EventSuggestion_EventInbox] ON [PMO].[EventSuggestion] ([EventInboxId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_ProjectMember_CreatedBy] ON [PMO].[ProjectMember] ([CreatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_ProjectMember_RemovedBy] ON [PMO].[ProjectMember] ([RemovedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_ProjectMember_User_Active_Project] ON [PMO].[ProjectMember] ([UserId], [IsActive], [BusinessObjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_ProjectMilestone_CreatedBy] ON [PMO].[ProjectMilestone] ([CreatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_ProjectMilestone_Owner_Status] ON [PMO].[ProjectMilestone] ([OwnerUserId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_ProjectMilestone_Project_Date] ON [PMO].[ProjectMilestone] ([ProjectId], [PlannedDate], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_ProjectProfile_CreatedBy] ON [PMO].[ProjectProfile] ([CreatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_ProjectProfile_Owner_UpdatedAt] ON [PMO].[ProjectProfile] ([OwnerUserId], [UpdatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_ProjectRelation_CreatedBy] ON [PMO].[ProjectRelation] ([CreatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_ProjectRelation_Entity] ON [PMO].[ProjectRelation] ([EntityType], [EntityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE UNIQUE INDEX [UX_ProjectRelation_Project_Entity_Code] ON [PMO].[ProjectRelation] ([ProjectId], [EntityType], [EntityId], [RelationCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_ProjectTask_Assignee_Status_Due] ON [PMO].[ProjectTask] ([AssigneeId], [Status], [DueAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_ProjectTask_BusinessObjectId] ON [PMO].[ProjectTask] ([BusinessObjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_ProjectTask_CreatedBy] ON [PMO].[ProjectTask] ([CreatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_ProjectTask_DepartmentId] ON [PMO].[ProjectTask] ([DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_ProjectTask_Project_Status_Due] ON [PMO].[ProjectTask] ([ProjectId], [Status], [DueAt], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_ProjectTask_WorkflowInstanceId] ON [PMO].[ProjectTask] ([WorkflowInstanceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    CREATE INDEX [IX_ProjectTask_WorkflowTaskId] ON [PMO].[ProjectTask] ([WorkflowTaskId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_ProjectTask_EventInbox] ON [PMO].[ProjectTask] ([EventInboxId]) WHERE [EventInboxId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    ALTER TABLE [BusinessObject].[BusinessObject] ADD CONSTRAINT [FK_BusinessObject_CreatedByUser] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    DECLARE @Now datetime2(3) = SYSUTCDATETIME();
    DECLARE @OrgId uniqueidentifier = '10000000-0000-0000-0000-000000000001';
    DECLARE @AdminUserId uniqueidentifier = '10000000-0000-0000-0000-000000000010';
    DECLARE @PmUserId uniqueidentifier = '10000000-0000-0000-0000-000000000021';
    DECLARE @PmRoleId uniqueidentifier = '20000000-0000-0000-0000-000000000007';
    DECLARE @AgentUserId uniqueidentifier = '10000000-0000-0000-0000-000000000022';
    DECLARE @AgentRoleId uniqueidentifier = '20000000-0000-0000-0000-000000000008';
    DECLARE @AgentProfileId uniqueidentifier = '60000000-0000-0000-0000-000000000003';
    DECLARE @ProjectTypeId uniqueidentifier = '40000000-0000-0000-0000-000000000005';
    DECLARE @RndDepartmentId uniqueidentifier =
        (SELECT TOP (1) Id
         FROM [Identity].[Department]
         WHERE OrganizationId = @OrgId AND Code = N'RND' AND IsActive = 1);

    IF NOT EXISTS (SELECT 1 FROM [Metadata].[ObjectType] WHERE Code = N'PROJECT')
    BEGIN
        INSERT INTO [Metadata].[ObjectType] (Id, Code, Name, Version, CreatedAt, UpdatedAt)
        VALUES (@ProjectTypeId, N'PROJECT', N'项目', 1, @Now, @Now);
    END;

    DECLARE @ObjectTypeId uniqueidentifier =
        (SELECT Id FROM [Metadata].[ObjectType] WHERE Code = N'PROJECT');

    INSERT INTO [Metadata].[FieldDefinition]
        (Id, ObjectTypeId, [Key], DisplayName, DataType, IsRequired, MaxLength, Sensitivity, SchemaVersion)
    SELECT fieldSeed.Id, @ObjectTypeId, fieldSeed.[Key], fieldSeed.DisplayName,
           fieldSeed.DataType, fieldSeed.IsRequired, fieldSeed.MaxLength,
           fieldSeed.Sensitivity, 1
    FROM (VALUES
        ('41000000-0000-0000-0000-000000000001', N'ProjectName', N'项目名称', N'string', 1, 255, N'Internal'),
        ('41000000-0000-0000-0000-000000000002', N'OwnerUserId', N'项目负责人', N'string', 1, 36, N'Internal'),
        ('41000000-0000-0000-0000-000000000003', N'PlannedStartDate', N'计划开始日期', N'datetime', 0, 32, N'Internal'),
        ('41000000-0000-0000-0000-000000000004', N'PlannedEndDate', N'计划结束日期', N'datetime', 0, 32, N'Internal'),
        ('41000000-0000-0000-0000-000000000005', N'Objective', N'项目目标', N'text', 1, 2000, N'Sensitive'),
        ('41000000-0000-0000-0000-000000000006', N'CurrentStage', N'当前阶段', N'string', 1, 80, N'Internal'),
        ('41000000-0000-0000-0000-000000000007', N'ConfidentialityLevel', N'保密等级', N'string', 1, 16, N'Sensitive')
    ) fieldSeed(Id, [Key], DisplayName, DataType, IsRequired, MaxLength, Sensitivity)
    WHERE NOT EXISTS (
        SELECT 1 FROM [Metadata].[FieldDefinition] existing
        WHERE existing.ObjectTypeId = @ObjectTypeId
          AND existing.[Key] = fieldSeed.[Key]
    );

    IF NOT EXISTS (SELECT 1 FROM [Identity].[User] WHERE UserName = N'demo.pm')
    BEGIN
        INSERT INTO [Identity].[User]
            (Id, OrganizationId, UserName, DisplayName, IsServiceIdentity, IsActive, CreatedAt)
        VALUES
            (@PmUserId, @OrgId, N'demo.pm', N'项目经理', 0, 1, @Now);
    END;

    IF NOT EXISTS (SELECT 1 FROM [Identity].[Role] WHERE Code = N'project-manager')
    BEGIN
        INSERT INTO [Identity].[Role] (Id, Code, Name)
        VALUES (@PmRoleId, N'project-manager', N'项目经理');
    END;

    IF NOT EXISTS (SELECT 1 FROM [Identity].[User] WHERE UserName = N'agent.project')
    BEGIN
        INSERT INTO [Identity].[User]
            (Id, OrganizationId, UserName, DisplayName, IsServiceIdentity, IsActive, CreatedAt)
        VALUES
            (@AgentUserId, @OrgId, N'agent.project', N'项目事件建议数字员工', 1, 1, @Now);
    END;

    IF NOT EXISTS (SELECT 1 FROM [Identity].[Role] WHERE Code = N'project-event-agent')
    BEGIN
        INSERT INTO [Identity].[Role] (Id, Code, Name)
        VALUES (@AgentRoleId, N'project-event-agent', N'项目事件建议数字员工');
    END;

    IF NOT EXISTS (SELECT 1 FROM [Agent].[AgentProfile] WHERE Name = N'项目事件建议数字员工')
    BEGIN
        INSERT INTO [Agent].[AgentProfile]
            (Id, Name, AutonomyLevel, AmountLimit, RiskLimit, AllowedToolsJson, IsActive, ServiceUserId)
        VALUES
            (@AgentProfileId, N'项目事件建议数字员工', N'A2', 0, N'Low',
             N'["event.inbox.review","project.task.draft"]', 1, @AgentUserId);
    END;

    INSERT INTO [Identity].[Permission] (Id, Code, Resource, Action)
    SELECT permissionSeed.Id, permissionSeed.Code, permissionSeed.Resource, permissionSeed.Action
    FROM (VALUES
        ('31000000-0000-0000-0000-000000000030', N'project-read', N'project', N'Read'),
        ('31000000-0000-0000-0000-000000000031', N'project-create', N'project', N'Create'),
        ('31000000-0000-0000-0000-000000000032', N'project-update', N'project', N'Update'),
        ('31000000-0000-0000-0000-000000000033', N'project-member-read', N'project-member', N'Read'),
        ('31000000-0000-0000-0000-000000000034', N'project-member-manage', N'project-member', N'Manage'),
        ('31000000-0000-0000-0000-000000000035', N'milestone-read', N'milestone', N'Read'),
        ('31000000-0000-0000-0000-000000000036', N'milestone-manage', N'milestone', N'Manage'),
        ('31000000-0000-0000-0000-000000000037', N'event-inbox-read', N'event-inbox', N'Read'),
        ('31000000-0000-0000-0000-000000000038', N'event-inbox-create', N'event-inbox', N'Create'),
        ('31000000-0000-0000-0000-000000000039', N'event-inbox-update', N'event-inbox', N'Update'),
        ('31000000-0000-0000-0000-000000000040', N'event-inbox-confirm', N'event-inbox', N'Confirm'),
        ('31000000-0000-0000-0000-000000000041', N'project-task-read', N'project-task', N'Read'),
        ('31000000-0000-0000-0000-000000000042', N'project-task-create', N'project-task', N'Create'),
        ('31000000-0000-0000-0000-000000000043', N'project-task-update', N'project-task', N'Update'),
        ('31000000-0000-0000-0000-000000000044', N'project-audit-read', N'project-audit', N'Read')
    ) permissionSeed(Id, Code, Resource, Action)
    WHERE NOT EXISTS (
        SELECT 1 FROM [Identity].[Permission] existing
        WHERE existing.Code = permissionSeed.Code
           OR (existing.Resource = permissionSeed.Resource AND existing.Action = permissionSeed.Action)
    );

    INSERT INTO [Identity].[UserRole] (UserId, RoleId)
    SELECT @PmUserId, roleRow.Id
    FROM [Identity].[Role] roleRow
    WHERE roleRow.Code = N'project-manager'
      AND NOT EXISTS (
          SELECT 1 FROM [Identity].[UserRole] existing
          WHERE existing.UserId = @PmUserId AND existing.RoleId = roleRow.Id
      );

    INSERT INTO [Identity].[UserRole] (UserId, RoleId)
    SELECT @AgentUserId, roleRow.Id
    FROM [Identity].[Role] roleRow
    WHERE roleRow.Code = N'project-event-agent'
      AND NOT EXISTS (
          SELECT 1 FROM [Identity].[UserRole] existing
          WHERE existing.UserId = @AgentUserId AND existing.RoleId = roleRow.Id
      );

    INSERT INTO [Identity].[UserDepartmentMembership]
        (UserId, DepartmentId, IsPrimary, IsManager, IsActive, JoinedAt)
    SELECT @PmUserId, departmentRow.Id, 1, 0, 1, @Now
    FROM [Identity].[Department] departmentRow
    WHERE departmentRow.Id = @RndDepartmentId
      AND NOT EXISTS (
          SELECT 1 FROM [Identity].[UserDepartmentMembership] existing
          WHERE existing.UserId = @PmUserId AND existing.DepartmentId = departmentRow.Id
      );

    INSERT INTO [Identity].[DataScope] (Id, UserId, ScopeType, ScopeReference)
    SELECT NEWID(), @PmUserId, N'organization', CONVERT(nvarchar(36), @OrgId)
    WHERE NOT EXISTS (
        SELECT 1 FROM [Identity].[DataScope] existing
        WHERE existing.UserId = @PmUserId
          AND existing.ScopeType = N'organization'
          AND existing.ScopeReference = CONVERT(nvarchar(36), @OrgId)
    );

    INSERT INTO [Identity].[DataScope] (Id, UserId, ScopeType, ScopeReference)
    SELECT NEWID(), @PmUserId, N'department', CONVERT(nvarchar(36), @RndDepartmentId)
    WHERE NOT EXISTS (
        SELECT 1 FROM [Identity].[DataScope] existing
        WHERE existing.UserId = @PmUserId
          AND existing.ScopeType = N'department'
          AND existing.ScopeReference = CONVERT(nvarchar(36), @RndDepartmentId)
    );

    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT roleRow.Id, permissionRow.Id
    FROM [Identity].[Role] roleRow
    CROSS JOIN [Identity].[Permission] permissionRow
    WHERE roleRow.Code IN (N'platform-admin', N'project-manager', N'project-event-agent')
      AND permissionRow.Code IN (
          N'project-read', N'project-create', N'project-update',
          N'project-member-read', N'project-member-manage',
          N'milestone-read', N'milestone-manage',
          N'event-inbox-read', N'event-inbox-create', N'event-inbox-update', N'event-inbox-confirm',
          N'project-task-read', N'project-task-create', N'project-task-update',
          N'project-audit-read')
      AND NOT EXISTS (
          SELECT 1 FROM [Identity].[RolePermission] existing
          WHERE existing.RoleId = roleRow.Id AND existing.PermissionId = permissionRow.Id
      );

    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT roleRow.Id, permissionRow.Id
    FROM [Identity].[Role] roleRow
    CROSS JOIN [Identity].[Permission] permissionRow
    WHERE roleRow.Code = N'project-manager'
      AND permissionRow.Code IN (
          N'dashboard-read', N'department-read', N'business-object-read',
          N'workflow-instance-read', N'workflow-task-read', N'audit-read',
          N'file-read', N'file-create', N'file-update',
          N'agent-read', N'agent-run-read', N'agent-run-submit',
          N'minutes-read', N'minutes-create', N'minutes-update', N'minutes-approve')
      AND NOT EXISTS (
          SELECT 1 FROM [Identity].[RolePermission] existing
          WHERE existing.RoleId = roleRow.Id AND existing.PermissionId = permissionRow.Id
      );

    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT roleRow.Id, permissionRow.Id
    FROM [Identity].[Role] roleRow
    CROSS JOIN [Identity].[Permission] permissionRow
    WHERE roleRow.Code = N'project-event-agent'
      AND permissionRow.Code IN (
          N'project-read', N'project-task-read',
          N'event-inbox-read', N'event-inbox-update')
      AND NOT EXISTS (
          SELECT 1 FROM [Identity].[RolePermission] existing
          WHERE existing.RoleId = roleRow.Id AND existing.PermissionId = permissionRow.Id
      );

    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT roleRow.Id, permissionRow.Id
    FROM [Identity].[Role] roleRow
    CROSS JOIN [Identity].[Permission] permissionRow
    WHERE roleRow.Code IN (
        N'rd-engineer', N'quality-engineer', N'supply-specialist',
        N'finance-approver', N'production-team')
      AND permissionRow.Code IN (
          N'project-read', N'project-member-read', N'milestone-read',
          N'event-inbox-read', N'project-task-read', N'project-task-update')
      AND NOT EXISTS (
          SELECT 1 FROM [Identity].[RolePermission] existing
          WHERE existing.RoleId = roleRow.Id AND existing.PermissionId = permissionRow.Id
      );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031165645_Batch007ProjectInboxTasks', N'10.0.14');
END;

COMMIT;
GO
