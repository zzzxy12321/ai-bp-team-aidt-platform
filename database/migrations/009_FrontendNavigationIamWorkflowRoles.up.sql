BEGIN TRANSACTION;
IF NOT EXISTS (
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
    IF SCHEMA_ID(N'Navigation') IS NULL EXEC(N'CREATE SCHEMA [Navigation];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD [AssigneeSnapshotJson] nvarchar(max) NOT NULL DEFAULT N'{}';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD [AssigneeStrategy] nvarchar(32) NOT NULL DEFAULT N'SpecificUser';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD [BackupAssigneeId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD [BusinessRoleCode] nvarchar(64) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD [BusinessRoleId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD [BusinessRoleVersion] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD [CandidateUserIdsJson] nvarchar(max) NOT NULL DEFAULT N'[]';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD [OriginalAssigneeId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD [TransferReason] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD [TransferredAt] datetime2(3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD [TransferredBy] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD [AllowTransfer] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD [AssigneeStrategy] nvarchar(32) NOT NULL DEFAULT N'SpecificUser';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD [BusinessRoleId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD [TransferTargetStrategy] nvarchar(32) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Identity].[User] ADD [ActivatedAt] datetime2(3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Identity].[User] ADD [InvitationStatus] nvarchar(32) NOT NULL DEFAULT N'Active';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Identity].[User] ADD [InvitedAt] datetime2(3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Identity].[User] ADD [Version] bigint NOT NULL DEFAULT CAST(1 AS bigint);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Identity].[Role] ADD [CreatedAt] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME());
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Identity].[Role] ADD [Description] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Identity].[Role] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Identity].[Role] ADD [IsSystem] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Identity].[Role] ADD [RoleType] nvarchar(32) NOT NULL DEFAULT N'Business';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Identity].[Role] ADD [UpdatedAt] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME());
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Identity].[Role] ADD [Version] bigint NOT NULL DEFAULT CAST(1 AS bigint);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Identity].[Department] ADD [HierarchyType] nvarchar(32) NOT NULL DEFAULT N'Department';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Identity].[Department] ADD [ParentDepartmentId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Identity].[Department] ADD [Path] nvarchar(1000) NOT NULL DEFAULT N'/';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Identity].[Department] ADD [SortOrder] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Identity].[Department] ADD [Version] bigint NOT NULL DEFAULT CAST(1 AS bigint);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE TABLE [Identity].[AccessChangeRequest] (
        [Id] uniqueidentifier NOT NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [RequestType] nvarchar(32) NOT NULL,
        [TargetUserId] uniqueidentifier NULL,
        [TargetRoleId] uniqueidentifier NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        [RiskReason] nvarchar(1000) NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [RequestedBy] uniqueidentifier NOT NULL,
        [RequestedAt] datetime2(3) NOT NULL,
        [ReviewedBy] uniqueidentifier NULL,
        [ReviewedAt] datetime2(3) NULL,
        [ReviewComment] nvarchar(1000) NULL,
        [IdempotencyKey] nvarchar(128) NOT NULL,
        CONSTRAINT [PK_AccessChangeRequest] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_AccessChangeRequest_PayloadJson] CHECK (ISJSON([PayloadJson]) = 1),
        CONSTRAINT [CK_AccessChangeRequest_Status] CHECK ([Status] IN ('Pending','Approved','Rejected','Applied','Cancelled')),
        CONSTRAINT [CK_AccessChangeRequest_Type] CHECK ([RequestType] IN ('RoleAssignment','RolePermissions')),
        CONSTRAINT [FK_AccessChangeRequest_Organization] FOREIGN KEY ([OrganizationId]) REFERENCES [Identity].[Organization] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AccessChangeRequest_RequestedBy] FOREIGN KEY ([RequestedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AccessChangeRequest_ReviewedBy] FOREIGN KEY ([ReviewedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AccessChangeRequest_TargetRole] FOREIGN KEY ([TargetRoleId]) REFERENCES [Identity].[Role] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AccessChangeRequest_TargetUser] FOREIGN KEY ([TargetUserId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE TABLE [Navigation].[NavigationUserPreference] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [WorkspaceKey] nvarchar(80) NOT NULL,
        [ItemKey] nvarchar(80) NOT NULL,
        [IsFavorite] bit NOT NULL,
        [IsHidden] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [UpdatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_NavigationUserPreference] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_NavigationUserPreference_User] FOREIGN KEY ([UserId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE TABLE [Navigation].[NavigationWorkspace] (
        [Id] uniqueidentifier NOT NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [DepartmentId] uniqueidentifier NULL,
        [WorkspaceKey] nvarchar(80) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [WorkspaceType] nvarchar(32) NOT NULL,
        [IsSystem] bit NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [Version] bigint NOT NULL,
        [CreatedBy] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedBy] uniqueidentifier NULL,
        [UpdatedAt] datetime2(3) NOT NULL,
        [RetiredBy] uniqueidentifier NULL,
        [RetiredAt] datetime2(3) NULL,
        CONSTRAINT [PK_NavigationWorkspace] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_NavigationWorkspace_Status] CHECK ([Status] IN ('Draft','Published','Retired')),
        CONSTRAINT [CK_NavigationWorkspace_Type] CHECK ([WorkspaceType] IN ('System','OrganizationWorkspace','DepartmentWorkspace','BusinessView')),
        CONSTRAINT [FK_NavigationWorkspace_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_NavigationWorkspace_Department] FOREIGN KEY ([DepartmentId]) REFERENCES [Identity].[Department] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_NavigationWorkspace_Organization] FOREIGN KEY ([OrganizationId]) REFERENCES [Identity].[Organization] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_NavigationWorkspace_RetiredBy] FOREIGN KEY ([RetiredBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_NavigationWorkspace_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE TABLE [Identity].[UserRoleAssignment] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        [ScopeType] nvarchar(32) NOT NULL,
        [ScopeReference] nvarchar(128) NOT NULL,
        [IsActive] bit NOT NULL,
        [AssignedBy] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [RevokedBy] uniqueidentifier NULL,
        [RevokedAt] datetime2(3) NULL,
        CONSTRAINT [PK_UserRoleAssignment] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_UserRoleAssignment_ScopeReference] CHECK (LEN([ScopeReference]) > 0),
        CONSTRAINT [CK_UserRoleAssignment_ScopeType] CHECK ([ScopeType] IN ('organization','department')),
        CONSTRAINT [FK_UserRoleAssignment_AssignedBy] FOREIGN KEY ([AssignedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserRoleAssignment_RevokedBy] FOREIGN KEY ([RevokedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserRoleAssignment_Role] FOREIGN KEY ([RoleId]) REFERENCES [Identity].[Role] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserRoleAssignment_User] FOREIGN KEY ([UserId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE TABLE [Navigation].[NavigationVersion] (
        [Id] uniqueidentifier NOT NULL,
        [WorkspaceId] uniqueidentifier NOT NULL,
        [VersionNumber] int NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [Revision] bigint NOT NULL,
        [ImpactAnalysisJson] nvarchar(max) NULL,
        [CreatedBy] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedBy] uniqueidentifier NULL,
        [UpdatedAt] datetime2(3) NOT NULL,
        [PublishedBy] uniqueidentifier NULL,
        [PublishedAt] datetime2(3) NULL,
        CONSTRAINT [PK_NavigationVersion] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_NavigationVersion_ImpactAnalysis] CHECK ([ImpactAnalysisJson] IS NULL OR ISJSON([ImpactAnalysisJson]) = 1),
        CONSTRAINT [CK_NavigationVersion_Status] CHECK ([Status] IN ('Draft','Published','Retired')),
        CONSTRAINT [FK_NavigationVersion_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_NavigationVersion_PublishedBy] FOREIGN KEY ([PublishedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_NavigationVersion_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_NavigationVersion_Workspace] FOREIGN KEY ([WorkspaceId]) REFERENCES [Navigation].[NavigationWorkspace] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE TABLE [Navigation].[NavigationItem] (
        [Id] uniqueidentifier NOT NULL,
        [NavigationVersionId] uniqueidentifier NOT NULL,
        [ItemKey] nvarchar(80) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [ItemType] nvarchar(32) NOT NULL,
        [Route] nvarchar(500) NOT NULL,
        [RequiredResource] nvarchar(128) NULL,
        [RequiredAction] nvarchar(64) NULL,
        [SortOrder] int NOT NULL,
        [IsVisible] bit NOT NULL,
        [IsEnabled] bit NOT NULL,
        [IsSystem] bit NOT NULL,
        [Icon] nvarchar(32) NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_NavigationItem] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_NavigationItem_Route] CHECK (LEN([Route]) > 0),
        CONSTRAINT [CK_NavigationItem_Type] CHECK ([ItemType] IN ('System','Workspace','Task','Inbox','Workflow','BusinessObject','File','Project','Minutes','Report','Administration')),
        CONSTRAINT [FK_NavigationItem_Version] FOREIGN KEY ([NavigationVersionId]) REFERENCES [Navigation].[NavigationVersion] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE INDEX [IX_WorkflowTask_BackupAssigneeId] ON [Workflow].[WorkflowTask] ([BackupAssigneeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE INDEX [IX_WorkflowTask_BusinessRole_Status] ON [Workflow].[WorkflowTask] ([BusinessRoleId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE INDEX [IX_WorkflowTask_OriginalAssigneeId] ON [Workflow].[WorkflowTask] ([OriginalAssigneeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE INDEX [IX_WorkflowTask_TransferredBy] ON [Workflow].[WorkflowTask] ([TransferredBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE INDEX [IX_WorkflowNodePolicy_BusinessRoleId] ON [Workflow].[WorkflowNodePolicy] ([BusinessRoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    EXEC(N'ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD CONSTRAINT [CK_WorkflowNodePolicy_AssigneeStrategy] CHECK ([AssigneeStrategy] IN (''SpecificUser'',''BusinessRole'',''DepartmentManager'',''ProjectOwner'',''InitiatorManager'',''Agent''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    EXEC(N'ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD CONSTRAINT [CK_WorkflowNodePolicy_TransferTargetStrategy] CHECK ([TransferTargetStrategy] IS NULL OR [TransferTargetStrategy] IN (''BackupApprover'',''DepartmentManager''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE INDEX [IX_Role_Type_Active] ON [Identity].[Role] ([RoleType], [IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    EXEC(N'ALTER TABLE [Identity].[Role] ADD CONSTRAINT [CK_Role_Type] CHECK ([RoleType] IN (''System'',''Business'',''AgentService''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE INDEX [IX_Department_ParentDepartmentId] ON [Identity].[Department] ([ParentDepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE INDEX [IX_Department_Tree] ON [Identity].[Department] ([OrganizationId], [ParentDepartmentId], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    EXEC(N'ALTER TABLE [Identity].[Department] ADD CONSTRAINT [CK_Department_HierarchyType] CHECK ([HierarchyType] IN (''Center'',''Department'',''Team''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE INDEX [IX_AccessChangeRequest_Org_Status_RequestedAt] ON [Identity].[AccessChangeRequest] ([OrganizationId], [Status], [RequestedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE INDEX [IX_AccessChangeRequest_ReviewedBy] ON [Identity].[AccessChangeRequest] ([ReviewedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE INDEX [IX_AccessChangeRequest_TargetRoleId] ON [Identity].[AccessChangeRequest] ([TargetRoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE INDEX [IX_AccessChangeRequest_TargetUserId] ON [Identity].[AccessChangeRequest] ([TargetUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE UNIQUE INDEX [UX_AccessChangeRequest_Requester_Idempotency] ON [Identity].[AccessChangeRequest] ([RequestedBy], [IdempotencyKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE INDEX [IX_NavigationItem_Version_Sort] ON [Navigation].[NavigationItem] ([NavigationVersionId], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE UNIQUE INDEX [UX_NavigationItem_Version_Key] ON [Navigation].[NavigationItem] ([NavigationVersionId], [ItemKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE UNIQUE INDEX [UX_NavigationUserPreference_User_Workspace_Item] ON [Navigation].[NavigationUserPreference] ([UserId], [WorkspaceKey], [ItemKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE INDEX [IX_NavigationVersion_CreatedBy] ON [Navigation].[NavigationVersion] ([CreatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE INDEX [IX_NavigationVersion_PublishedBy] ON [Navigation].[NavigationVersion] ([PublishedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE INDEX [IX_NavigationVersion_UpdatedBy] ON [Navigation].[NavigationVersion] ([UpdatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE INDEX [IX_NavigationVersion_Workspace_Status] ON [Navigation].[NavigationVersion] ([WorkspaceId], [Status], [Revision]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE UNIQUE INDEX [UX_NavigationVersion_Workspace_Version] ON [Navigation].[NavigationVersion] ([WorkspaceId], [VersionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE INDEX [IX_NavigationWorkspace_CreatedBy] ON [Navigation].[NavigationWorkspace] ([CreatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE INDEX [IX_NavigationWorkspace_DepartmentId] ON [Navigation].[NavigationWorkspace] ([DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE INDEX [IX_NavigationWorkspace_Org_Department_Status] ON [Navigation].[NavigationWorkspace] ([OrganizationId], [DepartmentId], [Status], [Version]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE INDEX [IX_NavigationWorkspace_RetiredBy] ON [Navigation].[NavigationWorkspace] ([RetiredBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE INDEX [IX_NavigationWorkspace_UpdatedBy] ON [Navigation].[NavigationWorkspace] ([UpdatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE UNIQUE INDEX [UX_NavigationWorkspace_Organization_Key] ON [Navigation].[NavigationWorkspace] ([OrganizationId], [WorkspaceKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE INDEX [IX_UserRoleAssignment_AssignedBy] ON [Identity].[UserRoleAssignment] ([AssignedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE INDEX [IX_UserRoleAssignment_RevokedBy] ON [Identity].[UserRoleAssignment] ([RevokedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE INDEX [IX_UserRoleAssignment_Role_Active] ON [Identity].[UserRoleAssignment] ([RoleId], [IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE INDEX [IX_UserRoleAssignment_User_Active] ON [Identity].[UserRoleAssignment] ([UserId], [IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    CREATE UNIQUE INDEX [UX_UserRoleAssignment_User_Role_Scope] ON [Identity].[UserRoleAssignment] ([UserId], [RoleId], [ScopeType], [ScopeReference]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Identity].[Department] ADD CONSTRAINT [FK_Department_Parent] FOREIGN KEY ([ParentDepartmentId]) REFERENCES [Identity].[Department] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowNodePolicy] ADD CONSTRAINT [FK_WorkflowNodePolicy_BusinessRole] FOREIGN KEY ([BusinessRoleId]) REFERENCES [Identity].[Role] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD CONSTRAINT [FK_WorkflowTask_BackupAssignee] FOREIGN KEY ([BackupAssigneeId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD CONSTRAINT [FK_WorkflowTask_BusinessRole] FOREIGN KEY ([BusinessRoleId]) REFERENCES [Identity].[Role] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD CONSTRAINT [FK_WorkflowTask_OriginalAssignee] FOREIGN KEY ([OriginalAssigneeId]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    ALTER TABLE [Workflow].[WorkflowTask] ADD CONSTRAINT [FK_WorkflowTask_TransferredBy] FOREIGN KEY ([TransferredBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION;
END;

GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    UPDATE [Identity].[User]
    SET [InvitationStatus] = CASE WHEN [IsActive] = 1 THEN N'Active' ELSE N'Suspended' END,
        [ActivatedAt] = CASE WHEN [IsActive] = 1 THEN COALESCE([ActivatedAt], [CreatedAt]) ELSE [ActivatedAt] END,
        [Version] = CASE WHEN [Version] < 1 THEN 1 ELSE [Version] END;

    UPDATE [Identity].[Role]
    SET [RoleType] = CASE
            WHEN [Code] = N'platform-admin' THEN N'System'
            WHEN [Code] IN (N'project-event-agent') THEN N'AgentService'
            ELSE N'Business'
        END,
        [IsSystem] = CASE WHEN [Code] = N'platform-admin' THEN 1 ELSE 0 END,
        [IsActive] = 1,
        [Version] = CASE WHEN [Version] < 1 THEN 1 ELSE [Version] END;

    UPDATE [Identity].[Department]
    SET [HierarchyType] = N'Department',
        [Path] = N'/' + [Code],
        [Version] = CASE WHEN [Version] < 1 THEN 1 ELSE [Version] END;

    UPDATE [Workflow].[WorkflowTask]
    SET [AssigneeStrategy] = N'SpecificUser',
        [CandidateUserIdsJson] = N'["' + CONVERT(nvarchar(36), [AssigneeId]) + N'"]',
        [AssigneeSnapshotJson] = N'{"strategy":"SpecificUser","legacy":true}'
    WHERE [AssigneeStrategy] = N'' OR [CandidateUserIdsJson] = N'' OR [AssigneeSnapshotJson] = N'';

    UPDATE [Workflow].[WorkflowNodePolicy]
    SET [AssigneeStrategy] = N'SpecificUser'
    WHERE [AssigneeStrategy] = N'';
END;

GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @RoleSeedNow datetime2(3) = SYSUTCDATETIME();
    DECLARE @RoleSeedAdminUserId uniqueidentifier =
        (SELECT TOP (1) Id FROM [Identity].[User] WHERE UserName = N'demo.admin' AND IsActive = 1);

    IF NOT EXISTS (SELECT 1 FROM [Identity].[User] WHERE UserName = N'demo.security')
    BEGIN
        DECLARE @RoleSeedOrgId uniqueidentifier =
            (SELECT TOP (1) Id FROM [Identity].[Organization] WHERE IsActive = 1 ORDER BY CreatedAt, Id);
        INSERT INTO [Identity].[User]
            (Id, OrganizationId, UserName, DisplayName, IsServiceIdentity, IsActive,
             InvitationStatus, InvitedAt, ActivatedAt, Version, CreatedAt)
        VALUES
            ('10000000-0000-0000-0000-000000000023', @RoleSeedOrgId, N'demo.security',
             N'安全管理员', 0, 1, N'Active', @RoleSeedNow, @RoleSeedNow, 1, @RoleSeedNow);

        INSERT INTO [Identity].[UserRole] (UserId, RoleId)
        SELECT '10000000-0000-0000-0000-000000000023', roleRow.Id
        FROM [Identity].[Role] roleRow
        WHERE roleRow.Code = N'platform-admin'
          AND NOT EXISTS (
              SELECT 1 FROM [Identity].[UserRole] existing
              WHERE existing.UserId='10000000-0000-0000-0000-000000000023'
                AND existing.RoleId=roleRow.Id);

        INSERT INTO [Identity].[UserDepartmentMembership]
            (UserId, DepartmentId, IsPrimary, IsManager, IsActive, JoinedAt)
        SELECT '10000000-0000-0000-0000-000000000023', departmentRow.Id, 1, 0, 1, @RoleSeedNow
        FROM [Identity].[Department] departmentRow
        WHERE departmentRow.Code = N'RND'
          AND NOT EXISTS (
              SELECT 1 FROM [Identity].[UserDepartmentMembership] existing
              WHERE existing.UserId='10000000-0000-0000-0000-000000000023'
                AND existing.DepartmentId=departmentRow.Id);

        INSERT INTO [Identity].[DataScope] (Id, UserId, ScopeType, ScopeReference)
        SELECT NEWID(), '10000000-0000-0000-0000-000000000023',
               N'organization', CONVERT(nvarchar(36), @RoleSeedOrgId)
        WHERE NOT EXISTS (
            SELECT 1 FROM [Identity].[DataScope] existing
            WHERE existing.UserId='10000000-0000-0000-0000-000000000023'
              AND existing.ScopeType=N'organization'
              AND existing.ScopeReference=CONVERT(nvarchar(36), @RoleSeedOrgId));
    END;

    IF NOT EXISTS (SELECT 1 FROM [Identity].[Role] WHERE Code = N'business-rd-engineer')
        INSERT INTO [Identity].[Role]
            (Id, Code, Name, RoleType, Description, IsSystem, IsActive, Version, CreatedAt, UpdatedAt)
        VALUES
            ('20000000-0000-0000-0000-000000000009', N'business-rd-engineer',
             N'研发业务角色', N'Business', N'流程研发节点资格角色', 0, 1, 1, @RoleSeedNow, @RoleSeedNow);
    IF NOT EXISTS (SELECT 1 FROM [Identity].[Role] WHERE Code = N'business-manager-reviewer')
        INSERT INTO [Identity].[Role]
            (Id, Code, Name, RoleType, Description, IsSystem, IsActive, Version, CreatedAt, UpdatedAt)
        VALUES
            ('20000000-0000-0000-0000-000000000010', N'business-manager-reviewer',
             N'经理审核角色', N'Business', N'项目经理及授权经理的流程审核资格', 0, 1, 1, @RoleSeedNow, @RoleSeedNow);
    IF NOT EXISTS (SELECT 1 FROM [Identity].[Role] WHERE Code = N'business-quality-reviewer')
        INSERT INTO [Identity].[Role]
            (Id, Code, Name, RoleType, Description, IsSystem, IsActive, Version, CreatedAt, UpdatedAt)
        VALUES
            ('20000000-0000-0000-0000-000000000011', N'business-quality-reviewer',
             N'质量复核角色', N'Business', N'质量工程师的流程复核资格', 0, 1, 1, @RoleSeedNow, @RoleSeedNow);

    INSERT INTO [Identity].[Permission] (Id, Code, Resource, Action)
    SELECT seed.Id, seed.Code, seed.Resource, seed.Action
    FROM (VALUES
        ('31000000-0000-0000-0000-000000000060', N'identity-admin-read', N'identity-admin', N'Read'),
        ('31000000-0000-0000-0000-000000000061', N'identity-admin-manage', N'identity-admin', N'Manage'),
        ('31000000-0000-0000-0000-000000000062', N'role-read', N'role', N'Read'),
        ('31000000-0000-0000-0000-000000000063', N'role-create', N'role', N'Create'),
        ('31000000-0000-0000-0000-000000000064', N'role-update', N'role', N'Update'),
        ('31000000-0000-0000-0000-000000000065', N'role-manage', N'role', N'Manage'),
        ('31000000-0000-0000-0000-000000000066', N'permission-read', N'permission', N'Read'),
        ('31000000-0000-0000-0000-000000000067', N'permission-assign', N'permission', N'Assign'),
        ('31000000-0000-0000-0000-000000000068', N'navigation-read', N'navigation', N'Read'),
        ('31000000-0000-0000-0000-000000000069', N'navigation-manage', N'navigation', N'Manage'),
        ('31000000-0000-0000-0000-000000000070', N'navigation-publish', N'navigation', N'Approve'),
        ('31000000-0000-0000-0000-000000000071', N'data-scope-update', N'data-scope', N'Update'),
        ('31000000-0000-0000-0000-000000000072', N'workflow-role-read', N'workflow-role', N'Read'),
        ('31000000-0000-0000-0000-000000000073', N'workflow-role-manage', N'workflow-role', N'Manage'),
        ('31000000-0000-0000-0000-000000000074', N'workflow-task-update', N'workflow-task', N'Update')
    ) seed(Id, Code, Resource, Action)
    WHERE NOT EXISTS (
        SELECT 1 FROM [Identity].[Permission] existing
        WHERE existing.Code = seed.Code
           OR (existing.Resource = seed.Resource AND existing.Action = seed.Action)
    );
END;

GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT roleRow.Id, permissionRow.Id
    FROM [Identity].[Role] roleRow
    CROSS JOIN [Identity].[Permission] permissionRow
    WHERE roleRow.Code = N'platform-admin'
      AND permissionRow.Code IN (
        N'identity-admin-read', N'identity-admin-manage',
        N'role-read', N'role-create', N'role-update', N'role-manage',
        N'permission-read', N'permission-assign',
        N'navigation-read', N'navigation-manage', N'navigation-publish',
        N'data-scope-update', N'workflow-role-read', N'workflow-role-manage',
        N'workflow-task-update')
      AND NOT EXISTS (
        SELECT 1 FROM [Identity].[RolePermission] existing
        WHERE existing.RoleId = roleRow.Id AND existing.PermissionId = permissionRow.Id);

    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT roleRow.Id, permissionRow.Id
    FROM [Identity].[Role] roleRow
    CROSS JOIN [Identity].[Permission] permissionRow
    WHERE roleRow.Code = N'project-manager'
      AND permissionRow.Code IN (
        N'identity-admin-read', N'role-read', N'permission-read', N'permission-assign',
        N'navigation-read', N'navigation-manage', N'navigation-publish',
        N'workflow-role-read', N'workflow-role-manage', N'workflow-task-update',
        N'workflow-task-approve', N'workflow-task-return', N'workflow-task-terminate')
      AND NOT EXISTS (
        SELECT 1 FROM [Identity].[RolePermission] existing
        WHERE existing.RoleId = roleRow.Id AND existing.PermissionId = permissionRow.Id);

    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT roleRow.Id, permissionRow.Id
    FROM [Identity].[Role] roleRow
    CROSS JOIN [Identity].[Permission] permissionRow
    WHERE roleRow.Code IN (
        N'rd-engineer', N'quality-engineer', N'supply-specialist',
        N'finance-approver', N'production-team')
      AND permissionRow.Code = N'navigation-read'
      AND NOT EXISTS (
        SELECT 1 FROM [Identity].[RolePermission] existing
        WHERE existing.RoleId = roleRow.Id AND existing.PermissionId = permissionRow.Id);
END;

GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @AssignmentNow datetime2(3) = SYSUTCDATETIME();
    DECLARE @AssignmentAdminUserId uniqueidentifier =
        (SELECT TOP (1) Id FROM [Identity].[User] WHERE UserName = N'demo.admin' AND IsActive = 1);
    DECLARE @ResearcherId uniqueidentifier =
        (SELECT TOP (1) Id FROM [Identity].[User] WHERE UserName = N'demo.researcher' AND IsActive = 1);
    DECLARE @PmId uniqueidentifier =
        (SELECT TOP (1) Id FROM [Identity].[User] WHERE UserName = N'demo.pm' AND IsActive = 1);
    DECLARE @QaId uniqueidentifier =
        (SELECT TOP (1) Id FROM [Identity].[User] WHERE UserName = N'demo.qa' AND IsActive = 1);
    DECLARE @RndDepartmentId uniqueidentifier =
        (SELECT TOP (1) Id FROM [Identity].[Department] WHERE Code = N'RND');
    DECLARE @QaDepartmentId uniqueidentifier =
        (SELECT TOP (1) Id FROM [Identity].[Department] WHERE Code = N'QA');
    DECLARE @RdRoleId uniqueidentifier =
        (SELECT TOP (1) Id FROM [Identity].[Role] WHERE Code = N'business-rd-engineer');
    DECLARE @ManagerRoleId uniqueidentifier =
        (SELECT TOP (1) Id FROM [Identity].[Role] WHERE Code = N'business-manager-reviewer');
    DECLARE @QualityRoleId uniqueidentifier =
        (SELECT TOP (1) Id FROM [Identity].[Role] WHERE Code = N'business-quality-reviewer');

    IF @ResearcherId IS NOT NULL AND @RndDepartmentId IS NOT NULL AND @RdRoleId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM [Identity].[UserRoleAssignment]
                       WHERE UserId=@ResearcherId AND RoleId=@RdRoleId
                         AND ScopeType=N'department'
                         AND ScopeReference=CONVERT(nvarchar(36),@RndDepartmentId))
        INSERT INTO [Identity].[UserRoleAssignment]
            (Id, UserId, RoleId, ScopeType, ScopeReference, IsActive, AssignedBy, CreatedAt)
        VALUES
            (NEWID(), @ResearcherId, @RdRoleId, N'department',
             CONVERT(nvarchar(36), @RndDepartmentId), 1, @AssignmentAdminUserId, @AssignmentNow);

    IF @PmId IS NOT NULL AND @RndDepartmentId IS NOT NULL AND @ManagerRoleId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM [Identity].[UserRoleAssignment]
                       WHERE UserId=@PmId AND RoleId=@ManagerRoleId
                         AND ScopeType=N'department'
                         AND ScopeReference=CONVERT(nvarchar(36),@RndDepartmentId))
        INSERT INTO [Identity].[UserRoleAssignment]
            (Id, UserId, RoleId, ScopeType, ScopeReference, IsActive, AssignedBy, CreatedAt)
        VALUES
            (NEWID(), @PmId, @ManagerRoleId, N'department',
             CONVERT(nvarchar(36), @RndDepartmentId), 1, @AssignmentAdminUserId, @AssignmentNow);

    IF @QaId IS NOT NULL AND @QaDepartmentId IS NOT NULL AND @QualityRoleId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM [Identity].[UserRoleAssignment]
                       WHERE UserId=@QaId AND RoleId=@QualityRoleId
                         AND ScopeType=N'department'
                         AND ScopeReference=CONVERT(nvarchar(36),@QaDepartmentId))
        INSERT INTO [Identity].[UserRoleAssignment]
            (Id, UserId, RoleId, ScopeType, ScopeReference, IsActive, AssignedBy, CreatedAt)
        VALUES
            (NEWID(), @QaId, @QualityRoleId, N'department',
             CONVERT(nvarchar(36), @QaDepartmentId), 1, @AssignmentAdminUserId, @AssignmentNow);
END;

GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    DECLARE @NavSeedNow datetime2(3) = SYSUTCDATETIME();
    DECLARE @NavSeedOrgId uniqueidentifier =
        (SELECT TOP (1) Id FROM [Identity].[Organization] WHERE IsActive = 1 ORDER BY CreatedAt, Id);
    DECLARE @NavSeedAdminUserId uniqueidentifier =
        (SELECT TOP (1) Id FROM [Identity].[User] WHERE UserName = N'demo.admin' AND IsActive = 1);
    DECLARE @WorkspaceId uniqueidentifier = '90000000-0000-0000-0000-000000000001';
    DECLARE @VersionId uniqueidentifier = '91000000-0000-0000-0000-000000000001';

    IF @NavSeedOrgId IS NOT NULL AND @NavSeedAdminUserId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM [Navigation].[NavigationWorkspace]
                       WHERE OrganizationId=@NavSeedOrgId AND WorkspaceKey=N'SYSTEM')
    BEGIN
        INSERT INTO [Navigation].[NavigationWorkspace]
            (Id, OrganizationId, DepartmentId, WorkspaceKey, Name, WorkspaceType, IsSystem,
             Status, Version, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt)
        VALUES
            (@WorkspaceId, @NavSeedOrgId, NULL, N'SYSTEM', N'工作台', N'System', 1,
             N'Published', 1, @NavSeedAdminUserId, @NavSeedNow, @NavSeedAdminUserId, @NavSeedNow);

        INSERT INTO [Navigation].[NavigationVersion]
            (Id, WorkspaceId, VersionNumber, Status, Revision, ImpactAnalysisJson,
             CreatedBy, CreatedAt, UpdatedBy, UpdatedAt, PublishedBy, PublishedAt)
        VALUES
            (@VersionId, @WorkspaceId, 1, N'Published', 1,
             N'{"seed":true,"reason":"migration-009-system-navigation"}',
             @NavSeedAdminUserId, @NavSeedNow, @NavSeedAdminUserId, @NavSeedNow, @NavSeedAdminUserId, @NavSeedNow);

        INSERT INTO [Navigation].[NavigationItem]
            (Id, NavigationVersionId, ItemKey, Name, ItemType, Route,
             RequiredResource, RequiredAction, SortOrder, IsVisible, IsEnabled, IsSystem,
             Icon, CreatedAt, UpdatedAt)
        VALUES
            ('92000000-0000-0000-0000-000000000001', @VersionId, N'MY_WORK', N'我的工作', N'System', N'/', N'dashboard', N'Read', 10, 1, 1, 1, N'我', @NavSeedNow, @NavSeedNow),
            ('92000000-0000-0000-0000-000000000002', @VersionId, N'TODO', N'待办', N'Task', N'/workflows', N'workflow-task', N'Read', 20, 1, 1, 1, N'办', @NavSeedNow, @NavSeedNow),
            ('92000000-0000-0000-0000-000000000003', @VersionId, N'EVENT_INBOX', N'事件收件箱', N'Inbox', N'/inbox', N'event-inbox', N'Read', 30, 1, 1, 1, N'收', @NavSeedNow, @NavSeedNow),
            ('92000000-0000-0000-0000-000000000004', @VersionId, N'DEPARTMENT_HOME', N'部门工作区', N'Workspace', N'/department', N'department', N'Read', 40, 1, 1, 0, N'部', @NavSeedNow, @NavSeedNow),
            ('92000000-0000-0000-0000-000000000005', @VersionId, N'DEPARTMENT_FILES', N'部门文件', N'File', N'/documents', N'file', N'Read', 50, 1, 1, 0, N'文', @NavSeedNow, @NavSeedNow),
            ('92000000-0000-0000-0000-000000000006', @VersionId, N'MINUTES', N'AI 听记', N'Minutes', N'/minutes', N'minutes', N'Read', 60, 1, 1, 0, N'记', @NavSeedNow, @NavSeedNow),
            ('92000000-0000-0000-0000-000000000007', @VersionId, N'PROJECTS', N'项目工作台', N'Project', N'/projects', N'project', N'Read', 70, 1, 1, 0, N'项', @NavSeedNow, @NavSeedNow),
            ('92000000-0000-0000-0000-000000000008', @VersionId, N'WORKFLOW_ITEMS', N'流程事项', N'Workflow', N'/workflows', N'workflow-task', N'Read', 80, 1, 1, 0, N'流', @NavSeedNow, @NavSeedNow),
            ('92000000-0000-0000-0000-000000000009', @VersionId, N'BUSINESS_VIEW', N'业务视图', N'BusinessObject', N'/objects', N'business-object', N'Read', 90, 1, 1, 0, N'业', @NavSeedNow, @NavSeedNow),
            ('92000000-0000-0000-0000-000000000010', @VersionId, N'MANAGEMENT', N'管理概览', N'Administration', N'/management', N'identity-admin', N'Manage', 100, 1, 1, 0, N'管', @NavSeedNow, @NavSeedNow),
            ('92000000-0000-0000-0000-000000000011', @VersionId, N'IAM_NAVIGATION', N'IAM 与导航', N'Administration', N'/iam', N'identity-admin', N'Read', 110, 1, 1, 0, N'权', @NavSeedNow, @NavSeedNow),
            ('92000000-0000-0000-0000-000000000012', @VersionId, N'AUDIT', N'审计证据', N'Administration', N'/audit', N'audit', N'Read', 120, 1, 1, 0, N'审', @NavSeedNow, @NavSeedNow),
            ('92000000-0000-0000-0000-000000000013', @VersionId, N'AGENTS', N'数字员工', N'Administration', N'/agents', N'agent', N'Read', 130, 1, 1, 0, N'智', @NavSeedNow, @NavSeedNow),
            ('92000000-0000-0000-0000-000000000014', @VersionId, N'WORKFLOW_POLICIES', N'流程策略', N'Administration', N'/workflow-policies', N'workflow-policy', N'Read', 140, 1, 1, 0, N'策', @NavSeedNow, @NavSeedNow),
            ('92000000-0000-0000-0000-000000000015', @VersionId, N'CONFIGURATIONS', N'流程配置', N'Administration', N'/configurations', N'configuration', N'Read', 150, 1, 1, 0, N'配', @NavSeedNow, @NavSeedNow);
    END;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031195740_009_FrontendNavigationIamWorkflowRoles'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031195740_009_FrontendNavigationIamWorkflowRoles', N'10.0.14');
END;

COMMIT;
GO

