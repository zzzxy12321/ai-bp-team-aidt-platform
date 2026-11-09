IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    IF SCHEMA_ID(N'Agent') IS NULL EXEC(N'CREATE SCHEMA [Agent];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    IF SCHEMA_ID(N'Document') IS NULL EXEC(N'CREATE SCHEMA [Document];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    IF SCHEMA_ID(N'Audit') IS NULL EXEC(N'CREATE SCHEMA [Audit];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    IF SCHEMA_ID(N'BusinessObject') IS NULL EXEC(N'CREATE SCHEMA [BusinessObject];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    IF SCHEMA_ID(N'Configuration') IS NULL EXEC(N'CREATE SCHEMA [Configuration];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    IF SCHEMA_ID(N'Integration') IS NULL EXEC(N'CREATE SCHEMA [Integration];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    IF SCHEMA_ID(N'Identity') IS NULL EXEC(N'CREATE SCHEMA [Identity];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    IF SCHEMA_ID(N'Metadata') IS NULL EXEC(N'CREATE SCHEMA [Metadata];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    IF SCHEMA_ID(N'Workflow') IS NULL EXEC(N'CREATE SCHEMA [Workflow];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Agent].[AgentProfile] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [AutonomyLevel] nvarchar(8) NOT NULL,
        [AmountLimit] decimal(18,4) NOT NULL,
        [RiskLimit] nvarchar(16) NOT NULL,
        [AllowedToolsJson] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_AgentProfile] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_AgentProfile_AmountLimit] CHECK ([AmountLimit] >= 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Document].[Attachment] (
        [Id] uniqueidentifier NOT NULL,
        [BusinessObjectId] uniqueidentifier NULL,
        [StoragePath] nvarchar(500) NOT NULL,
        [OriginalFileName] nvarchar(255) NOT NULL,
        [MimeType] nvarchar(128) NOT NULL,
        [SizeBytes] bigint NOT NULL,
        [Sha256] nvarchar(64) NOT NULL,
        [VirusScanStatus] nvarchar(32) NOT NULL,
        [Sensitivity] nvarchar(32) NOT NULL,
        [CreatedBy] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_Attachment] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Audit].[AuditEvent] (
        [Id] uniqueidentifier NOT NULL,
        [ActorId] uniqueidentifier NOT NULL,
        [Module] nvarchar(64) NOT NULL,
        [Action] nvarchar(64) NOT NULL,
        [EntityType] nvarchar(128) NOT NULL,
        [EntityId] nvarchar(128) NOT NULL,
        [TraceId] nvarchar(128) NOT NULL,
        [Outcome] nvarchar(32) NOT NULL,
        [BeforeJson] nvarchar(max) NULL,
        [AfterJson] nvarchar(max) NULL,
        [Reason] nvarchar(2000) NULL,
        [IpHash] nvarchar(128) NULL,
        [OccurredAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_AuditEvent] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Configuration].[ConfigurationVersion] (
        [Id] uniqueidentifier NOT NULL,
        [ConfigurationKey] nvarchar(128) NOT NULL,
        [VersionNumber] int NOT NULL,
        [State] nvarchar(32) NOT NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        [ImpactAnalysisJson] nvarchar(max) NULL,
        [CreatedBy] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [PublishedAt] datetime2(3) NULL,
        CONSTRAINT [PK_ConfigurationVersion] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ConfigurationVersion_PayloadJson] CHECK (ISJSON([PayloadJson]) = 1)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Integration].[Connector] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(64) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [AdapterType] nvarchar(64) NOT NULL,
        [ConnectionStatus] nvarchar(32) NOT NULL,
        [AuthorityOwner] nvarchar(64) NOT NULL,
        CONSTRAINT [PK_Connector] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Document].[DocumentVersion] (
        [Id] uniqueidentifier NOT NULL,
        [DocumentId] uniqueidentifier NOT NULL,
        [VersionNumber] int NOT NULL,
        [StoragePath] nvarchar(500) NOT NULL,
        [Sha256] nvarchar(64) NOT NULL,
        [CreatedBy] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_DocumentVersion] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Audit].[EvidenceAsset] (
        [Id] uniqueidentifier NOT NULL,
        [BusinessObjectId] uniqueidentifier NULL,
        [WorkflowInstanceId] uniqueidentifier NULL,
        [StoragePath] nvarchar(500) NOT NULL,
        [Sha256] nvarchar(64) NOT NULL,
        [MimeType] nvarchar(128) NOT NULL,
        [Sensitivity] nvarchar(32) NOT NULL,
        [CreatedBy] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_EvidenceAsset] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Metadata].[ObjectType] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(64) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [Version] bigint NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_ObjectType] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Identity].[Organization] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(64) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Organization] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Organization_Code] CHECK ([Code] <> '')
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Integration].[OutboxEvent] (
        [Id] uniqueidentifier NOT NULL,
        [EventType] nvarchar(128) NOT NULL,
        [AggregateType] nvarchar(128) NOT NULL,
        [AggregateId] uniqueidentifier NOT NULL,
        [IdempotencyKey] nvarchar(128) NOT NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [AttemptCount] int NOT NULL,
        [NextAttemptAt] datetime2(3) NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [ProcessedAt] datetime2(3) NULL,
        CONSTRAINT [PK_OutboxEvent] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_OutboxEvent_PayloadJson] CHECK (ISJSON([PayloadJson]) = 1)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Identity].[Permission] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(128) NOT NULL,
        [Resource] nvarchar(128) NOT NULL,
        [Action] nvarchar(64) NOT NULL,
        CONSTRAINT [PK_Permission] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Identity].[Role] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(64) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        CONSTRAINT [PK_Role] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Document].[Signature] (
        [Id] uniqueidentifier NOT NULL,
        [EntityType] nvarchar(128) NOT NULL,
        [EntityId] nvarchar(128) NOT NULL,
        [SignedBy] uniqueidentifier NOT NULL,
        [EvidenceReference] nvarchar(500) NOT NULL,
        [SignedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_Signature] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Workflow].[WorkflowDefinition] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(80) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_WorkflowDefinition] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Integration].[ConnectorCommand] (
        [Id] uniqueidentifier NOT NULL,
        [ConnectorId] uniqueidentifier NOT NULL,
        [CommandType] nvarchar(64) NOT NULL,
        [IdempotencyKey] nvarchar(128) NOT NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        [ApprovalEvidenceJson] nvarchar(max) NULL,
        [Status] nvarchar(32) NOT NULL,
        [AttemptCount] int NOT NULL,
        [MaxAttempts] int NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_ConnectorCommand] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ConnectorCommand_PayloadJson] CHECK (ISJSON([PayloadJson]) = 1),
        CONSTRAINT [FK_ConnectorCommand_Connector] FOREIGN KEY ([ConnectorId]) REFERENCES [Integration].[Connector] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Integration].[Reconciliation] (
        [Id] uniqueidentifier NOT NULL,
        [ConnectorId] uniqueidentifier NOT NULL,
        [PeriodStart] datetime2(3) NOT NULL,
        [PeriodEnd] datetime2(3) NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [DifferenceCount] int NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_Reconciliation] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Reconciliation_Period] CHECK ([PeriodEnd] >= [PeriodStart]),
        CONSTRAINT [FK_Reconciliation_Connector] FOREIGN KEY ([ConnectorId]) REFERENCES [Integration].[Connector] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [BusinessObject].[BusinessObject] (
        [Id] uniqueidentifier NOT NULL,
        [ObjectTypeId] uniqueidentifier NOT NULL,
        [ObjectCode] nvarchar(80) NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [Version] bigint NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [FieldsJson] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2(3) NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_BusinessObject] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BusinessObject_FieldsJson] CHECK (ISJSON([FieldsJson]) = 1),
        CONSTRAINT [FK_BusinessObject_ObjectType] FOREIGN KEY ([ObjectTypeId]) REFERENCES [Metadata].[ObjectType] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Metadata].[FieldDefinition] (
        [Id] uniqueidentifier NOT NULL,
        [ObjectTypeId] uniqueidentifier NOT NULL,
        [Key] nvarchar(64) NOT NULL,
        [DisplayName] nvarchar(128) NOT NULL,
        [DataType] nvarchar(32) NOT NULL,
        [IsRequired] bit NOT NULL,
        [MaxLength] int NOT NULL,
        [Sensitivity] nvarchar(32) NOT NULL,
        [SchemaVersion] int NOT NULL,
        CONSTRAINT [PK_FieldDefinition] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FieldDefinition_ObjectType] FOREIGN KEY ([ObjectTypeId]) REFERENCES [Metadata].[ObjectType] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Metadata].[ObjectRelation] (
        [Id] uniqueidentifier NOT NULL,
        [ObjectTypeId] uniqueidentifier NOT NULL,
        [TargetObjectTypeId] uniqueidentifier NOT NULL,
        [Code] nvarchar(64) NOT NULL,
        [IsRequired] bit NOT NULL,
        CONSTRAINT [PK_ObjectRelation] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ObjectRelation_SourceType] FOREIGN KEY ([ObjectTypeId]) REFERENCES [Metadata].[ObjectType] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ObjectRelation_TargetType] FOREIGN KEY ([TargetObjectTypeId]) REFERENCES [Metadata].[ObjectType] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Identity].[User] (
        [Id] uniqueidentifier NOT NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [UserName] nvarchar(128) NOT NULL,
        [DisplayName] nvarchar(128) NOT NULL,
        [IsServiceIdentity] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_User] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_User_Organization] FOREIGN KEY ([OrganizationId]) REFERENCES [Identity].[Organization] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Integration].[DeadLetter] (
        [Id] bigint NOT NULL IDENTITY,
        [OutboxEventId] uniqueidentifier NOT NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [ResolutionNote] nvarchar(2000) NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [ResolvedAt] datetime2(3) NULL,
        CONSTRAINT [PK_DeadLetter] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DeadLetter_OutboxEvent] FOREIGN KEY ([OutboxEventId]) REFERENCES [Integration].[OutboxEvent] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Identity].[RolePermission] (
        [RoleId] uniqueidentifier NOT NULL,
        [PermissionId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_RolePermission] PRIMARY KEY ([RoleId], [PermissionId]),
        CONSTRAINT [FK_RolePermission_Permission] FOREIGN KEY ([PermissionId]) REFERENCES [Identity].[Permission] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RolePermission_Role] FOREIGN KEY ([RoleId]) REFERENCES [Identity].[Role] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Workflow].[WorkflowVersion] (
        [Id] uniqueidentifier NOT NULL,
        [WorkflowDefinitionId] uniqueidentifier NOT NULL,
        [VersionNumber] int NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [DefinitionJson] nvarchar(max) NOT NULL,
        [PublishedAt] datetime2(3) NULL,
        CONSTRAINT [PK_WorkflowVersion] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_WorkflowVersion_DefinitionJson] CHECK (ISJSON([DefinitionJson]) = 1),
        CONSTRAINT [FK_WorkflowVersion_Definition] FOREIGN KEY ([WorkflowDefinitionId]) REFERENCES [Workflow].[WorkflowDefinition] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Integration].[ConnectorReceipt] (
        [Id] uniqueidentifier NOT NULL,
        [ConnectorCommandId] uniqueidentifier NOT NULL,
        [ExternalId] nvarchar(128) NULL,
        [StatusCode] int NOT NULL,
        [ResponseDigest] nvarchar(128) NOT NULL,
        [ReceivedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_ConnectorReceipt] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ConnectorReceipt_Command] FOREIGN KEY ([ConnectorCommandId]) REFERENCES [Integration].[ConnectorCommand] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [BusinessObject].[BusinessObjectRelation] (
        [Id] uniqueidentifier NOT NULL,
        [SourceObjectId] uniqueidentifier NOT NULL,
        [TargetObjectId] uniqueidentifier NOT NULL,
        [RelationCode] nvarchar(64) NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_BusinessObjectRelation] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BusinessObjectRelation_Source] FOREIGN KEY ([SourceObjectId]) REFERENCES [BusinessObject].[BusinessObject] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BusinessObjectRelation_Target] FOREIGN KEY ([TargetObjectId]) REFERENCES [BusinessObject].[BusinessObject] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [BusinessObject].[BusinessObjectVersion] (
        [Id] bigint NOT NULL IDENTITY,
        [BusinessObjectId] uniqueidentifier NOT NULL,
        [Version] bigint NOT NULL,
        [SnapshotJson] nvarchar(max) NOT NULL,
        [ActorId] uniqueidentifier NOT NULL,
        [Reason] nvarchar(1000) NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_BusinessObjectVersion] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BusinessObjectVersion_SnapshotJson] CHECK (ISJSON([SnapshotJson]) = 1),
        CONSTRAINT [FK_BusinessObjectVersion_BusinessObject] FOREIGN KEY ([BusinessObjectId]) REFERENCES [BusinessObject].[BusinessObject] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Agent].[AgentRun] (
        [Id] uniqueidentifier NOT NULL,
        [AgentProfileId] uniqueidentifier NOT NULL,
        [RequestedBy] uniqueidentifier NOT NULL,
        [Action] nvarchar(64) NOT NULL,
        [Amount] decimal(18,4) NOT NULL,
        [RiskLevel] nvarchar(16) NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [ConfirmationId] uniqueidentifier NULL,
        [Model] nvarchar(128) NULL,
        [SkillVersion] nvarchar(64) NULL,
        [InputDigest] nvarchar(128) NULL,
        [OutputDigest] nvarchar(128) NULL,
        [Cost] decimal(18,6) NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [StartedAt] datetime2(3) NULL,
        [EndedAt] datetime2(3) NULL,
        CONSTRAINT [PK_AgentRun] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_AgentRun_Amount] CHECK ([Amount] >= 0),
        CONSTRAINT [CK_AgentRun_Cost] CHECK ([Cost] IS NULL OR [Cost] >= 0),
        CONSTRAINT [FK_AgentRun_Profile] FOREIGN KEY ([AgentProfileId]) REFERENCES [Agent].[AgentProfile] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AgentRun_RequestedBy] FOREIGN KEY ([RequestedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Identity].[DataScope] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [ScopeType] nvarchar(64) NOT NULL,
        [ScopeReference] nvarchar(128) NOT NULL,
        CONSTRAINT [PK_DataScope] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DataScope_User] FOREIGN KEY ([UserId]) REFERENCES [Identity].[User] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Identity].[UserRole] (
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_UserRole] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRole_Role] FOREIGN KEY ([RoleId]) REFERENCES [Identity].[Role] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRole_User] FOREIGN KEY ([UserId]) REFERENCES [Identity].[User] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Workflow].[WorkflowInstance] (
        [Id] uniqueidentifier NOT NULL,
        [WorkflowDefinitionId] uniqueidentifier NOT NULL,
        [WorkflowVersionId] uniqueidentifier NOT NULL,
        [BusinessObjectId] uniqueidentifier NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [Version] bigint NOT NULL,
        [CurrentNodeCode] nvarchar(80) NULL,
        [StartedAt] datetime2(3) NOT NULL,
        [EndedAt] datetime2(3) NULL,
        CONSTRAINT [PK_WorkflowInstance] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkflowInstance_BusinessObject] FOREIGN KEY ([BusinessObjectId]) REFERENCES [BusinessObject].[BusinessObject] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkflowInstance_Definition] FOREIGN KEY ([WorkflowDefinitionId]) REFERENCES [Workflow].[WorkflowDefinition] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkflowInstance_Version] FOREIGN KEY ([WorkflowVersionId]) REFERENCES [Workflow].[WorkflowVersion] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Agent].[AgentConfirmation] (
        [Id] uniqueidentifier NOT NULL,
        [AgentRunId] uniqueidentifier NOT NULL,
        [ConfirmedBy] uniqueidentifier NOT NULL,
        [Decision] nvarchar(32) NOT NULL,
        [Comment] nvarchar(2000) NULL,
        [ConfirmedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_AgentConfirmation] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AgentConfirmation_Run] FOREIGN KEY ([AgentRunId]) REFERENCES [Agent].[AgentRun] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AgentConfirmation_User] FOREIGN KEY ([ConfirmedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Workflow].[WorkflowTask] (
        [Id] uniqueidentifier NOT NULL,
        [WorkflowInstanceId] uniqueidentifier NOT NULL,
        [NodeCode] nvarchar(80) NOT NULL,
        [AssigneeId] uniqueidentifier NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [Version] bigint NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [DecisionComment] nvarchar(2000) NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [CompletedAt] datetime2(3) NULL,
        CONSTRAINT [PK_WorkflowTask] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkflowTask_Assignee] FOREIGN KEY ([AssigneeId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkflowTask_WorkflowInstance] FOREIGN KEY ([WorkflowInstanceId]) REFERENCES [Workflow].[WorkflowInstance] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Workflow].[WorkflowTaskAction] (
        [Id] bigint NOT NULL IDENTITY,
        [WorkflowTaskId] uniqueidentifier NOT NULL,
        [ActorId] uniqueidentifier NOT NULL,
        [Action] nvarchar(64) NOT NULL,
        [Comment] nvarchar(2000) NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_WorkflowTaskAction] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkflowTaskAction_Actor] FOREIGN KEY ([ActorId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkflowTaskAction_Task] FOREIGN KEY ([WorkflowTaskId]) REFERENCES [Workflow].[WorkflowTask] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE TABLE [Workflow].[WorkflowTaskCandidate] (
        [WorkflowTaskId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_WorkflowTaskCandidate] PRIMARY KEY ([WorkflowTaskId], [UserId]),
        CONSTRAINT [FK_WorkflowTaskCandidate_Task] FOREIGN KEY ([WorkflowTaskId]) REFERENCES [Workflow].[WorkflowTask] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WorkflowTaskCandidate_User] FOREIGN KEY ([UserId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_AgentConfirmation_ConfirmedBy] ON [Agent].[AgentConfirmation] ([ConfirmedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_AgentConfirmation_Run_ConfirmedAt] ON [Agent].[AgentConfirmation] ([AgentRunId], [ConfirmedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_AgentProfile_Name] ON [Agent].[AgentProfile] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_AgentRun_Profile_CreatedAt] ON [Agent].[AgentRun] ([AgentProfileId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_AgentRun_RequestedBy] ON [Agent].[AgentRun] ([RequestedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_AgentRun_Status_CreatedAt] ON [Agent].[AgentRun] ([Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Attachment_BusinessObjectId] ON [Document].[Attachment] ([BusinessObjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Attachment_Sha256] ON [Document].[Attachment] ([Sha256]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_AuditEvent_Entity_OccurredAt] ON [Audit].[AuditEvent] ([EntityType], [EntityId], [OccurredAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_AuditEvent_OccurredAt_Id] ON [Audit].[AuditEvent] ([OccurredAt], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_AuditEvent_Trace_OccurredAt] ON [Audit].[AuditEvent] ([TraceId], [OccurredAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_BusinessObject_Status_UpdatedAt_Id] ON [BusinessObject].[BusinessObject] ([Status], [UpdatedAt], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_BusinessObject_Type_Code] ON [BusinessObject].[BusinessObject] ([ObjectTypeId], [ObjectCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_BusinessObjectRelation_TargetObjectId] ON [BusinessObject].[BusinessObjectRelation] ([TargetObjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_BusinessObjectRelation_Source_Code_Target] ON [BusinessObject].[BusinessObjectRelation] ([SourceObjectId], [RelationCode], [TargetObjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_BusinessObjectVersion_Object_Version] ON [BusinessObject].[BusinessObjectVersion] ([BusinessObjectId], [Version]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_ConfigurationVersion_Key_Version] ON [Configuration].[ConfigurationVersion] ([ConfigurationKey], [VersionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Connector_Code] ON [Integration].[Connector] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ConnectorCommand_Status_UpdatedAt] ON [Integration].[ConnectorCommand] ([Status], [UpdatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_ConnectorCommand_Connector_Idempotency] ON [Integration].[ConnectorCommand] ([ConnectorId], [IdempotencyKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ConnectorReceipt_Command_ReceivedAt] ON [Integration].[ConnectorReceipt] ([ConnectorCommandId], [ReceivedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_DataScope_User_Type_Reference] ON [Identity].[DataScope] ([UserId], [ScopeType], [ScopeReference]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_DeadLetter_Outbox_CreatedAt] ON [Integration].[DeadLetter] ([OutboxEventId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_DocumentVersion_Document_Version] ON [Document].[DocumentVersion] ([DocumentId], [VersionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_EvidenceAsset_Sha256] ON [Audit].[EvidenceAsset] ([Sha256]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_EvidenceAsset_Workflow_CreatedAt] ON [Audit].[EvidenceAsset] ([WorkflowInstanceId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_FieldDefinition_ObjectType_Key] ON [Metadata].[FieldDefinition] ([ObjectTypeId], [Key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ObjectRelation_TargetObjectTypeId] ON [Metadata].[ObjectRelation] ([TargetObjectTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_ObjectRelation_Type_Target_Code] ON [Metadata].[ObjectRelation] ([ObjectTypeId], [TargetObjectTypeId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_ObjectType_Code] ON [Metadata].[ObjectType] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Organization_Code] ON [Identity].[Organization] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_OutboxEvent_Aggregate] ON [Integration].[OutboxEvent] ([AggregateType], [AggregateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_OutboxEvent_Status_NextAttemptAt] ON [Integration].[OutboxEvent] ([Status], [NextAttemptAt], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_OutboxEvent_Idempotency] ON [Integration].[OutboxEvent] ([IdempotencyKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Permission_Code] ON [Identity].[Permission] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Permission_Resource_Action] ON [Identity].[Permission] ([Resource], [Action]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Reconciliation_Connector_Period] ON [Integration].[Reconciliation] ([ConnectorId], [PeriodStart], [PeriodEnd]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Role_Code] ON [Identity].[Role] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_RolePermission_PermissionId] ON [Identity].[RolePermission] ([PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Signature_Entity_SignedAt] ON [Document].[Signature] ([EntityType], [EntityId], [SignedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_User_OrganizationId] ON [Identity].[User] ([OrganizationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_User_Organization_UserName] ON [Identity].[User] ([OrganizationId], [UserName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_UserRole_RoleId] ON [Identity].[UserRole] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_WorkflowDefinition_Code] ON [Workflow].[WorkflowDefinition] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_WorkflowInstance_Object_StartedAt] ON [Workflow].[WorkflowInstance] ([BusinessObjectId], [StartedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_WorkflowInstance_Status_StartedAt_Id] ON [Workflow].[WorkflowInstance] ([Status], [StartedAt], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_WorkflowInstance_WorkflowDefinitionId] ON [Workflow].[WorkflowInstance] ([WorkflowDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_WorkflowInstance_WorkflowVersionId] ON [Workflow].[WorkflowInstance] ([WorkflowVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_WorkflowTask_Assignee_Status_CreatedAt] ON [Workflow].[WorkflowTask] ([AssigneeId], [Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_WorkflowTask_Instance_CreatedAt] ON [Workflow].[WorkflowTask] ([WorkflowInstanceId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_WorkflowTaskAction_ActorId] ON [Workflow].[WorkflowTaskAction] ([ActorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_WorkflowTaskAction_Task_CreatedAt] ON [Workflow].[WorkflowTaskAction] ([WorkflowTaskId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_WorkflowTaskCandidate_UserId] ON [Workflow].[WorkflowTaskCandidate] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_WorkflowVersion_Definition_Version] ON [Workflow].[WorkflowVersion] ([WorkflowDefinitionId], [VersionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029170022_InitialSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261029170022_InitialSchema', N'10.0.14');
END;

COMMIT;
GO

