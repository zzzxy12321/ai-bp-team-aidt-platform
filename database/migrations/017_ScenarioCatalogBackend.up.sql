BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    ALTER TABLE [Identity].[AccessChangeRequest] DROP CONSTRAINT [CK_AccessChangeRequest_Type];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    EXEC(N'ALTER TABLE [Identity].[AccessChangeRequest] ADD CONSTRAINT [CK_AccessChangeRequest_Type] CHECK ([RequestType] IN (''RoleAssignment'',''RolePermissions'',''AgentProfilePublish'',''ScenarioPublish''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    ALTER TABLE [Document].[AttachmentLink] DROP CONSTRAINT [CK_AttachmentLink_EntityType];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    EXEC(N'ALTER TABLE [Document].[AttachmentLink] ADD CONSTRAINT [CK_AttachmentLink_EntityType] CHECK ([EntityType] IN (''Department'',''BusinessObject'',''WorkflowInstance'',''WorkflowTask'',''ProjectTask'',''EventInbox'',''ScenarioLaunch''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    IF SCHEMA_ID(N'Scenario') IS NULL EXEC(N'CREATE SCHEMA [Scenario];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE TABLE [Scenario].[ScenarioDefinition] (
        [Id] uniqueidentifier NOT NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [OwnerDepartmentId] uniqueidentifier NOT NULL,
        [Code] nvarchar(80) NOT NULL,
        [Name] nvarchar(160) NOT NULL,
        [Description] nvarchar(2000) NOT NULL,
        [LifecycleStatus] nvarchar(24) NOT NULL,
        [CurrentPublishedVersionId] uniqueidentifier NULL,
        [Version] bigint NOT NULL DEFAULT CAST(1 AS bigint),
        [Revision] bigint NOT NULL DEFAULT CAST(1 AS bigint),
        [CreatedBy] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedBy] uniqueidentifier NULL,
        [UpdatedAt] datetime2(3) NOT NULL,
        [DisabledBy] uniqueidentifier NULL,
        [DisabledAt] datetime2(3) NULL,
        [DisabledReason] nvarchar(1000) NULL,
        CONSTRAINT [PK_ScenarioDefinition] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ScenarioDefinition_Disabled] CHECK ([LifecycleStatus] <> 'Disabled' OR ([DisabledBy] IS NOT NULL AND [DisabledAt] IS NOT NULL)),
        CONSTRAINT [CK_ScenarioDefinition_Status] CHECK ([LifecycleStatus] IN ('Draft','Published','Disabled')),
        CONSTRAINT [FK_ScenarioDefinition_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ScenarioDefinition_Department] FOREIGN KEY ([OwnerDepartmentId]) REFERENCES [Identity].[Department] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ScenarioDefinition_DisabledBy] FOREIGN KEY ([DisabledBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ScenarioDefinition_Organization] FOREIGN KEY ([OrganizationId]) REFERENCES [Identity].[Organization] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ScenarioDefinition_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE TABLE [Scenario].[ScenarioVersion] (
        [Id] uniqueidentifier NOT NULL,
        [ScenarioId] uniqueidentifier NOT NULL,
        [VersionNumber] int NOT NULL,
        [Status] nvarchar(24) NOT NULL,
        [Revision] bigint NOT NULL DEFAULT CAST(1 AS bigint),
        [PayloadJson] nvarchar(max) NOT NULL,
        [ReferenceSnapshotJson] nvarchar(max) NULL,
        [ValidationJson] nvarchar(max) NULL,
        [ImpactAnalysisJson] nvarchar(max) NULL,
        [ChangeSummary] nvarchar(1000) NULL,
        [CreatedBy] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [UpdatedBy] uniqueidentifier NULL,
        [UpdatedAt] datetime2(3) NOT NULL,
        [ValidatedBy] uniqueidentifier NULL,
        [ValidatedAt] datetime2(3) NULL,
        [PublishedBy] uniqueidentifier NULL,
        [PublishedAt] datetime2(3) NULL,
        [RetiredBy] uniqueidentifier NULL,
        [RetiredAt] datetime2(3) NULL,
        CONSTRAINT [PK_ScenarioVersion] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ScenarioVersion_ImpactAnalysisJson] CHECK ([ImpactAnalysisJson] IS NULL OR ISJSON([ImpactAnalysisJson]) = 1),
        CONSTRAINT [CK_ScenarioVersion_PayloadJson] CHECK (ISJSON([PayloadJson]) = 1),
        CONSTRAINT [CK_ScenarioVersion_PublishedAt] CHECK ([Status] <> 'Published' OR [PublishedAt] IS NOT NULL),
        CONSTRAINT [CK_ScenarioVersion_ReferenceSnapshotJson] CHECK ([ReferenceSnapshotJson] IS NULL OR ISJSON([ReferenceSnapshotJson]) = 1),
        CONSTRAINT [CK_ScenarioVersion_Status] CHECK ([Status] IN ('Draft','Validated','Published','Retired')),
        CONSTRAINT [CK_ScenarioVersion_ValidationJson] CHECK ([ValidationJson] IS NULL OR ISJSON([ValidationJson]) = 1),
        CONSTRAINT [FK_ScenarioVersion_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ScenarioVersion_PublishedBy] FOREIGN KEY ([PublishedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ScenarioVersion_RetiredBy] FOREIGN KEY ([RetiredBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ScenarioVersion_Scenario] FOREIGN KEY ([ScenarioId]) REFERENCES [Scenario].[ScenarioDefinition] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ScenarioVersion_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ScenarioVersion_ValidatedBy] FOREIGN KEY ([ValidatedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE TABLE [Scenario].[ScenarioLaunch] (
        [Id] uniqueidentifier NOT NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [ScenarioId] uniqueidentifier NOT NULL,
        [ScenarioVersionId] uniqueidentifier NOT NULL,
        [ScenarioVersionNumber] int NOT NULL,
        [DepartmentId] uniqueidentifier NOT NULL,
        [EnvironmentCode] nvarchar(16) NOT NULL,
        [IsTestData] bit NOT NULL,
        [StartedBy] uniqueidentifier NOT NULL,
        [StartedAt] datetime2(3) NOT NULL,
        [BusinessObjectId] uniqueidentifier NULL,
        [WorkflowInstanceId] uniqueidentifier NULL,
        [WorkflowTaskId] uniqueidentifier NULL,
        [ProjectId] uniqueidentifier NULL,
        [ProjectTaskId] uniqueidentifier NULL,
        [EventInboxId] uniqueidentifier NULL,
        [AgentRunId] uniqueidentifier NULL,
        [AttachmentIdsJson] nvarchar(max) NOT NULL,
        [MinutesSourceIdsJson] nvarchar(max) NOT NULL,
        [BusinessLinksJson] nvarchar(max) NOT NULL,
        [IdempotencyKey] nvarchar(128) NOT NULL,
        [RequestHash] nvarchar(64) NOT NULL,
        CONSTRAINT [PK_ScenarioLaunch] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ScenarioLaunch_AttachmentIdsJson] CHECK (ISJSON([AttachmentIdsJson]) = 1),
        CONSTRAINT [CK_ScenarioLaunch_BusinessLinksJson] CHECK (ISJSON([BusinessLinksJson]) = 1),
        CONSTRAINT [CK_ScenarioLaunch_Environment] CHECK ([EnvironmentCode] IN ('demo','test','prod')),
        CONSTRAINT [CK_ScenarioLaunch_MinutesSourceIdsJson] CHECK (ISJSON([MinutesSourceIdsJson]) = 1),
        CONSTRAINT [FK_ScenarioLaunch_AgentRun] FOREIGN KEY ([AgentRunId]) REFERENCES [Agent].[AgentRun] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ScenarioLaunch_BusinessObject] FOREIGN KEY ([BusinessObjectId]) REFERENCES [BusinessObject].[BusinessObject] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ScenarioLaunch_Department] FOREIGN KEY ([DepartmentId]) REFERENCES [Identity].[Department] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ScenarioLaunch_EventInbox] FOREIGN KEY ([EventInboxId]) REFERENCES [PMO].[EventInbox] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ScenarioLaunch_Organization] FOREIGN KEY ([OrganizationId]) REFERENCES [Identity].[Organization] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ScenarioLaunch_Project] FOREIGN KEY ([ProjectId]) REFERENCES [BusinessObject].[BusinessObject] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ScenarioLaunch_ProjectTask] FOREIGN KEY ([ProjectTaskId]) REFERENCES [PMO].[ProjectTask] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ScenarioLaunch_Scenario] FOREIGN KEY ([ScenarioId]) REFERENCES [Scenario].[ScenarioDefinition] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ScenarioLaunch_ScenarioVersion] FOREIGN KEY ([ScenarioVersionId]) REFERENCES [Scenario].[ScenarioVersion] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ScenarioLaunch_StartedBy] FOREIGN KEY ([StartedBy]) REFERENCES [Identity].[User] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ScenarioLaunch_WorkflowInstance] FOREIGN KEY ([WorkflowInstanceId]) REFERENCES [Workflow].[WorkflowInstance] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ScenarioLaunch_WorkflowTask] FOREIGN KEY ([WorkflowTaskId]) REFERENCES [Workflow].[WorkflowTask] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE INDEX [IX_ScenarioDefinition_CreatedBy] ON [Scenario].[ScenarioDefinition] ([CreatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE INDEX [IX_ScenarioDefinition_Department_Status_UpdatedAt] ON [Scenario].[ScenarioDefinition] ([OwnerDepartmentId], [LifecycleStatus], [UpdatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE INDEX [IX_ScenarioDefinition_DisabledBy] ON [Scenario].[ScenarioDefinition] ([DisabledBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE INDEX [IX_ScenarioDefinition_Organization_Status_Code] ON [Scenario].[ScenarioDefinition] ([OrganizationId], [LifecycleStatus], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE INDEX [IX_ScenarioDefinition_UpdatedBy] ON [Scenario].[ScenarioDefinition] ([UpdatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE UNIQUE INDEX [UX_ScenarioDefinition_Organization_Code] ON [Scenario].[ScenarioDefinition] ([OrganizationId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE INDEX [IX_ScenarioLaunch_AgentRunId] ON [Scenario].[ScenarioLaunch] ([AgentRunId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE INDEX [IX_ScenarioLaunch_BusinessObjectId] ON [Scenario].[ScenarioLaunch] ([BusinessObjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE INDEX [IX_ScenarioLaunch_Department_StartedAt] ON [Scenario].[ScenarioLaunch] ([DepartmentId], [StartedAt], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE INDEX [IX_ScenarioLaunch_EventInboxId] ON [Scenario].[ScenarioLaunch] ([EventInboxId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE INDEX [IX_ScenarioLaunch_ProjectId] ON [Scenario].[ScenarioLaunch] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE INDEX [IX_ScenarioLaunch_ProjectTaskId] ON [Scenario].[ScenarioLaunch] ([ProjectTaskId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE INDEX [IX_ScenarioLaunch_Scenario_Version_StartedAt] ON [Scenario].[ScenarioLaunch] ([ScenarioId], [ScenarioVersionNumber], [StartedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE INDEX [IX_ScenarioLaunch_ScenarioVersionId] ON [Scenario].[ScenarioLaunch] ([ScenarioVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE INDEX [IX_ScenarioLaunch_StartedBy] ON [Scenario].[ScenarioLaunch] ([StartedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE INDEX [IX_ScenarioLaunch_WorkflowInstanceId] ON [Scenario].[ScenarioLaunch] ([WorkflowInstanceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE INDEX [IX_ScenarioLaunch_WorkflowTaskId] ON [Scenario].[ScenarioLaunch] ([WorkflowTaskId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE UNIQUE INDEX [UX_ScenarioLaunch_Org_User_Idempotency] ON [Scenario].[ScenarioLaunch] ([OrganizationId], [StartedBy], [IdempotencyKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE INDEX [IX_ScenarioVersion_CreatedBy] ON [Scenario].[ScenarioVersion] ([CreatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE INDEX [IX_ScenarioVersion_PublishedBy] ON [Scenario].[ScenarioVersion] ([PublishedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE INDEX [IX_ScenarioVersion_RetiredBy] ON [Scenario].[ScenarioVersion] ([RetiredBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE INDEX [IX_ScenarioVersion_Scenario_Status_Version] ON [Scenario].[ScenarioVersion] ([ScenarioId], [Status], [VersionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE INDEX [IX_ScenarioVersion_UpdatedBy] ON [Scenario].[ScenarioVersion] ([UpdatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE INDEX [IX_ScenarioVersion_ValidatedBy] ON [Scenario].[ScenarioVersion] ([ValidatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    CREATE UNIQUE INDEX [UX_ScenarioVersion_Scenario_Version] ON [Scenario].[ScenarioVersion] ([ScenarioId], [VersionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    DECLARE @Now datetime2(3) = SYSUTCDATETIME();
    DECLARE @OrgId uniqueidentifier =
        (SELECT TOP (1) Id FROM [Identity].[Organization] WHERE IsActive = 1 ORDER BY CreatedAt, Id);
    DECLARE @AdminUserId uniqueidentifier =
        (SELECT TOP (1) Id FROM [Identity].[User] WHERE UserName = N'demo.admin' AND IsActive = 1);
    IF @OrgId IS NULL OR @AdminUserId IS NULL
        THROW 51700, N'017 requires the active demo organization and admin.', 1;

    INSERT INTO [Identity].[Permission] (Id, Code, Resource, Action)
    SELECT seed.Id, seed.Code, N'scenario', seed.Action
    FROM (VALUES
        (CONVERT(uniqueidentifier, '31000000-0000-0000-0000-000000000001'), N'scenario-read', N'Read'),
        (CONVERT(uniqueidentifier, '31000000-0000-0000-0000-000000000002'), N'scenario-create', N'Create'),
        (CONVERT(uniqueidentifier, '31000000-0000-0000-0000-000000000003'), N'scenario-update', N'Update'),
        (CONVERT(uniqueidentifier, '31000000-0000-0000-0000-000000000004'), N'scenario-manage', N'Manage'),
        (CONVERT(uniqueidentifier, '31000000-0000-0000-0000-000000000005'), N'scenario-approve', N'Approve')
    ) seed(Id, Code, Action)
    WHERE NOT EXISTS (
        SELECT 1 FROM [Identity].[Permission] existing WHERE existing.Code = seed.Code);

    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT roleRow.Id, permissionRow.Id
    FROM [Identity].[Role] roleRow
    JOIN [Identity].[Permission] permissionRow
        ON permissionRow.Resource = N'scenario'
    WHERE (
        roleRow.Code = N'platform-admin'
        OR roleRow.Code = N'agent-department-manager'
        OR (
            roleRow.Code IN (N'rd-engineer', N'quality-engineer', N'supply-specialist',
                             N'finance-approver', N'production-team', N'project-manager')
            AND permissionRow.Action = N'Read'
        )
    )
    AND NOT EXISTS (
        SELECT 1 FROM [Identity].[RolePermission] existing
        WHERE existing.RoleId = roleRow.Id
          AND existing.PermissionId = permissionRow.Id);

    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT roleRow.Id, permissionRow.Id
    FROM [Identity].[Role] roleRow
    JOIN [Identity].[Permission] permissionRow
        ON permissionRow.Code IN (N'business-object-create', N'workflow-instance-submit')
    WHERE roleRow.Code = N'quality-engineer'
      AND NOT EXISTS (
          SELECT 1 FROM [Identity].[RolePermission] existing
          WHERE existing.RoleId = roleRow.Id
            AND existing.PermissionId = permissionRow.Id);

    IF NOT EXISTS (SELECT 1 FROM [Workflow].[WorkflowDefinition] WHERE Code = N'FINANCE_PAYMENT_REVIEW')
    BEGIN
        INSERT INTO [Workflow].[WorkflowDefinition] (Id, Code, Name, Status, CreatedAt)
        VALUES ('50000000-0000-0000-0000-000000000005',
                N'FINANCE_PAYMENT_REVIEW', N'财务付款人工复核', N'Published', @Now);
        INSERT INTO [Workflow].[WorkflowVersion]
            (Id, WorkflowDefinitionId, VersionNumber, Status, DefinitionJson, PublishedAt)
        VALUES ('51000000-0000-0000-0000-000000000005',
                '50000000-0000-0000-0000-000000000005', 1, N'Published',
                N'{"objectTypeCode":"PURCHASE_REQUEST","startNode":"PAYMENT_APPROVAL","nodes":[{"code":"PAYMENT_APPROVAL","name":"财务付款人工复核","assigneeUserName":"demo.finance","nextNode":null,"completeOnApprove":true}]}',
                @Now);
    END;

    DECLARE @RndId uniqueidentifier =
        (SELECT Id FROM [Identity].[Department] WHERE OrganizationId=@OrgId AND Code=N'RND');
    DECLARE @QaId uniqueidentifier =
        (SELECT Id FROM [Identity].[Department] WHERE OrganizationId=@OrgId AND Code=N'QA');
    DECLARE @SupplyId uniqueidentifier =
        (SELECT Id FROM [Identity].[Department] WHERE OrganizationId=@OrgId AND Code=N'SUPPLY');
    DECLARE @FinanceId uniqueidentifier =
        (SELECT Id FROM [Identity].[Department] WHERE OrganizationId=@OrgId AND Code=N'FINANCE');
    DECLARE @ProductionId uniqueidentifier =
        (SELECT Id FROM [Identity].[Department] WHERE OrganizationId=@OrgId AND Code=N'PRODUCTION');
    DECLARE @ProjectId uniqueidentifier =
        (SELECT Id FROM [Identity].[Department] WHERE OrganizationId=@OrgId AND Code=N'PROJECT');

    DECLARE @Boundaries nvarchar(max) =
        N'["财务付款","质量放行","配方变更","设备控制","安全联锁","外部合同承诺","客户重大赔付"]';
    DECLARE @SceneSeed TABLE
    (
        ScenarioId uniqueidentifier PRIMARY KEY,
        VersionId uniqueidentifier NOT NULL,
        OwnerDepartmentId uniqueidentifier NOT NULL,
        Code nvarchar(80) NOT NULL,
        Name nvarchar(160) NOT NULL,
        Description nvarchar(2000) NOT NULL,
        PayloadJson nvarchar(max) NOT NULL
    );

    INSERT INTO @SceneSeed
        (ScenarioId, VersionId, OwnerDepartmentId, Code, Name, Description, PayloadJson)
    VALUES
    (
        '81000000-0000-0000-0000-000000000001',
        '82000000-0000-0000-0000-000000000001',
        @RndId,
        N'SCN-RND-SAMPLE-TO-PRODUCTION',
        N'研发样品到转产',
        N'从客户样品和技术需求发起，完成实验、质量判定、试产复核与转产人工确认。',
        CONCAT(N'{"problemSolved":"避免客户样品、实验记录和转产结论分散","startMode":"CreateBusinessObjectAndWorkflow","triggerDescription":"研发人员收到客户样品或技术需求后发起","applicableDepartmentCodes":["RND","QA","PRODUCTION"],"participantRoles":["研发工程师","质量工程师","生产负责人","研发负责人"],"objectTypeCodes":["CUSTOMER_SAMPLE"],"workflowDefinitionCode":"CUSTOMER_SAMPLE_TO_PRODUCTION","workflowVersionNumber":1,"steps":[{"sequence":1,"code":"RD_INITIATE","name":"研发发起","businessDescription":"登记客户样品、技术目标和配方权限要求","responsibleRole":"研发工程师","action":"创建客户样品主对象并发起流程","expectedResult":"形成受权限保护的样品主对象","evidence":"客户需求、样品信息和对象版本"},{"sequence":2,"code":"QA_REVIEW","name":"技术评审","businessDescription":"质量人员完成实验与测试判定","responsibleRole":"质量工程师","action":"提交检测结论和原始证据","expectedResult":"形成技术评审结论","evidence":"实验记录、检测数据和评审意见"},{"sequence":3,"code":"PRODUCTION_RELEASE","name":"生产接收","businessDescription":"生产确认资源和工艺可行性","responsibleRole":"生产负责人","action":"确认试产条件","expectedResult":"试产准备完成","evidence":"生产确认记录"},{"sequence":4,"code":"TRIAL_REVIEW","name":"试产复核","businessDescription":"质量与研发复核试产结果","responsibleRole":"质量工程师","action":"确认是否需要继续试产","expectedResult":"试产结论可追溯","evidence":"试产记录与复核意见"},{"sequence":5,"code":"FINAL_RELEASE","name":"转产确认","businessDescription":"授权负责人完成永久人工确认","responsibleRole":"研发负责人","action":"人工签认转产，不自动修改批准配方","expectedResult":"形成真实转产确认","evidence":"流程任务、审计和附件"}],"preparationItems":[{"code":"CUSTOMER_REQUIREMENT","name":"客户样品或技术需求","isRequired":true,"sourceType":"BusinessObject","description":"必须形成客户样品主对象"},{"code":"TECH_FILE","name":"技术或检测材料","isRequired":false,"sourceType":"Attachment","description":"受权限控制的附件"}],"supportedFileSources":["Attachment"],"supportedMinutesSources":["MinutesSource"],"allowedAgents":[{"agentProfileCode":"AGENT-DEMO","capability":"workflow.assist","boundary":"只生成检查建议，不确认转产"}],"permanentHumanBoundaries":',@Boundaries,N',"permissionRequirements":["business-object:Create","workflow-instance:Submit","workflow-task:Read","file:Create"],"completionEvidence":["客户样品主对象","流程实例与人工任务","实验和检测附件","审计时间线"]}')
    ),
    (
        '81000000-0000-0000-0000-000000000002',
        '82000000-0000-0000-0000-000000000002',
        @SupplyId,
        N'SCN-SCM-PURCHASE-TO-RECEIPT',
        N'采购请购到到货验收',
        N'从请购、预算审核、采购确认到验收，付款节点始终由财务人员人工签认。',
        CONCAT(N'{"problemSolved":"让请购、审核、采购、验收和付款证据进入同一业务链","startMode":"CreateBusinessObjectAndWorkflow","triggerDescription":"申请部门提交采购请购时发起","applicableDepartmentCodes":["SUPPLY","FINANCE","PRODUCTION"],"participantRoles":["采购专员","财务复核人","生产验收人"],"objectTypeCodes":["PURCHASE_REQUEST"],"workflowDefinitionCode":"PURCHASE_TO_PAYMENT","workflowVersionNumber":1,"steps":[{"sequence":1,"code":"REQUEST","name":"申请人发起","businessDescription":"登记采购内容、金额和部门需求","responsibleRole":"采购专员","action":"创建采购请购主对象","expectedResult":"请购信息进入受控流程","evidence":"采购请购对象与材料"},{"sequence":2,"code":"BUDGET_REVIEW","name":"部门预算审核","businessDescription":"核对预算和使用必要性","responsibleRole":"采购专员","action":"提交预算审核意见","expectedResult":"预算判断可追溯","evidence":"审核意见和对象版本"},{"sequence":3,"code":"PURCHASE_REVIEW","name":"采购确认","businessDescription":"确认供应商与采购执行计划","responsibleRole":"采购专员","action":"完成采购确认","expectedResult":"采购执行信息完整","evidence":"采购确认记录"},{"sequence":4,"code":"RECEIPT_CHECK","name":"到货验收","businessDescription":"核对到货数量和质量材料","responsibleRole":"生产验收人","action":"提交验收结果","expectedResult":"验收结论形成","evidence":"验收记录和附件"},{"sequence":5,"code":"PAYMENT_APPROVAL","name":"付款人工审批","businessDescription":"财务展示金额、预算、发票和验收材料后人工处理","responsibleRole":"财务复核人","action":"人工签认或退回，不允许数字员工自动付款","expectedResult":"付款边界保持人工","evidence":"财务任务、审计和全部材料"}],"preparationItems":[{"code":"PURCHASE_REQUEST","name":"采购请购信息","isRequired":true,"sourceType":"BusinessObject","description":"金额、用途和需求部门"},{"code":"QUOTE_EVIDENCE","name":"报价或合同材料","isRequired":true,"sourceType":"Attachment","description":"受控采购依据"}],"supportedFileSources":["Attachment"],"supportedMinutesSources":["MinutesSource"],"allowedAgents":[{"agentProfileCode":"AGENT-DEMO","capability":"workflow.read","boundary":"只核对缺口，不审批付款"}],"permanentHumanBoundaries":',@Boundaries,N',"permissionRequirements":["business-object:Create","workflow-instance:Submit","workflow-task:Approve","file:Create"],"completionEvidence":["采购请购主对象","采购与验收流程","报价和验收附件","财务人工审计"]}')
    ),
    (
        '81000000-0000-0000-0000-000000000003',
        '82000000-0000-0000-0000-000000000003',
        @FinanceId,
        N'SCN-FIN-PAYMENT-REVIEW',
        N'财务付款人工复核',
        N'将付款作为受控流程中的永久人工节点，集中展示金额、预算、发票和验收材料。',
        CONCAT(N'{"problemSolved":"防止数字员工或场景入口绕过财务签认","startMode":"ExistingBusinessObjectAndWorkflow","triggerDescription":"财务人员选择已存在的采购请购并进入付款复核","applicableDepartmentCodes":["FINANCE","SUPPLY"],"participantRoles":["财务复核人","采购专员"],"objectTypeCodes":["PURCHASE_REQUEST"],"workflowDefinitionCode":"FINANCE_PAYMENT_REVIEW","workflowVersionNumber":1,"steps":[{"sequence":1,"code":"PAYMENT_APPROVAL","name":"财务付款人工复核","businessDescription":"核对金额、预算、发票和到货验收材料","responsibleRole":"财务复核人","action":"由真人审阅并签认或退回","expectedResult":"付款结论由授权财务人员人工形成","evidence":"采购对象、预算与发票、验收材料、流程审计"}],"preparationItems":[{"code":"EXISTING_PURCHASE","name":"已存在采购请购","isRequired":true,"sourceType":"BusinessObject","description":"必须选择有效采购主对象"},{"code":"PAYMENT_EVIDENCE","name":"发票、预算和验收材料","isRequired":true,"sourceType":"Attachment","description":"财务人工复核依据"}],"supportedFileSources":["Attachment"],"supportedMinutesSources":["MinutesSource"],"allowedAgents":[{"agentProfileCode":"AGENT-DEMO","capability":"workflow.read","boundary":"只提示材料缺失，永不付款或代替签认"}],"permanentHumanBoundaries":',@Boundaries,N',"permissionRequirements":["business-object:Read","workflow-instance:Submit","workflow-task:Approve","file:Read"],"completionEvidence":["真实采购对象","财务复核流程任务","发票与验收附件","不可替代的人工审计"]}')
    ),
    (
        '81000000-0000-0000-0000-000000000004',
        '82000000-0000-0000-0000-000000000004',
        @RndId,
        N'SCN-MGR-ASSIGNMENT',
        N'部门经理事项分派与审批',
        N'从手动事项、附件或 AI 听记行动项开始，由经理确认负责人、期限和优先级。',
        CONCAT(N'{"problemSolved":"把口头事项和听记行动项转入可追踪的部门处理链","startMode":"EventInbox","triggerDescription":"经理收到手动事项、附件或听记行动项后发起","applicableDepartmentCodes":["RND","QA","SUPPLY","FINANCE","PRODUCTION","PROJECT"],"participantRoles":["部门负责人","事项发起人","被指派处理人"],"objectTypeCodes":[],"workflowDefinitionCode":null,"workflowVersionNumber":null,"steps":[{"sequence":1,"code":"INTAKE","name":"事项进入收件箱","businessDescription":"保留事项原文和来源","responsibleRole":"事项发起人","action":"创建人工事件收件箱记录","expectedResult":"事项原文与附件可追溯","evidence":"事件原文、来源和附件"},{"sequence":2,"code":"MANAGER_CONFIRM","name":"经理确认分派","businessDescription":"经理确认负责人、期限、优先级和最终内容","responsibleRole":"部门负责人","action":"真人确认并生成真实任务或流程","expectedResult":"分派结果由真人形成","evidence":"确认人、任务或流程和审计"},{"sequence":3,"code":"FOLLOW_UP","name":"处理与追溯","businessDescription":"被指派人员处理并回填证据","responsibleRole":"被指派处理人","action":"完成现有任务","expectedResult":"事项闭环","evidence":"任务状态、结果和审计"}],"preparationItems":[{"code":"EVENT_CONTENT","name":"事项标题与内容","isRequired":true,"sourceType":"EventInbox","description":"必须保留业务原文"},{"code":"SOURCE_EVIDENCE","name":"附件或听记来源","isRequired":false,"sourceType":"Attachment","description":"可选来源证据"}],"supportedFileSources":["Attachment"],"supportedMinutesSources":["MinutesSource","MinutesActionItem"],"allowedAgents":[{"agentProfileCode":"AGENT-PROJECT-EVENT","capability":"event.inbox.review","boundary":"只建议分类和负责人，最终分派由经理确认"}],"permanentHumanBoundaries":',@Boundaries,N',"permissionRequirements":["event-inbox:Create","event-inbox:Confirm","project-task:Create"],"completionEvidence":["事件收件箱","经理人工确认","真实任务或流程","来源附件与审计"]}')
    ),
    (
        '81000000-0000-0000-0000-000000000005',
        '82000000-0000-0000-0000-000000000005',
        @ProjectId,
        N'SCN-PM-MILESTONE-RISK',
        N'项目经理里程碑与风险',
        N'将事项、任务、文件、听记和流程关联到项目，推进里程碑并升级风险。',
        CONCAT(N'{"problemSolved":"让项目里程碑、风险和证据统一落到真实项目","startMode":"ProjectTask","triggerDescription":"项目经理识别里程碑风险或偏差后发起","applicableDepartmentCodes":["PROJECT","RND","QA","SUPPLY","PRODUCTION"],"participantRoles":["项目经理","任务负责人","项目成员"],"objectTypeCodes":["PROJECT"],"workflowDefinitionCode":null,"workflowVersionNumber":null,"steps":[{"sequence":1,"code":"PROJECT_TASK","name":"风险行动任务","businessDescription":"在真实项目中指派风险和期限","responsibleRole":"项目经理","action":"创建项目任务并关联来源证据","expectedResult":"风险进入项目工作台","evidence":"项目、任务、期限和负责人"},{"sequence":2,"code":"MILESTONE_UPDATE","name":"里程碑推进","businessDescription":"任务完成后重算进度并检查里程碑","responsibleRole":"任务负责人","action":"完成现有项目任务","expectedResult":"项目进度真实更新","evidence":"任务完成、项目版本和进度"},{"sequence":3,"code":"PROJECT_CLOSE","name":"项目关闭人工确认","businessDescription":"项目关闭继续是明确人工动作","responsibleRole":"项目经理","action":"人工确认关闭，场景不自动关闭项目","expectedResult":"项目关闭由真人负责","evidence":"项目状态、任务统计和审计"}],"preparationItems":[{"code":"PROJECT","name":"真实项目","isRequired":true,"sourceType":"Project","description":"必须是当前部门有效项目"},{"code":"RISK_EVIDENCE","name":"风险来源或证据","isRequired":false,"sourceType":"Attachment","description":"可选择附件或听记"}],"supportedFileSources":["Attachment"],"supportedMinutesSources":["MinutesSource","MinutesActionItem"],"allowedAgents":[{"agentProfileCode":"AGENT-PROJECT-EVENT","capability":"event.inbox.review","boundary":"只生成风险建议，不推进里程碑或关闭项目"}],"permanentHumanBoundaries":',@Boundaries,N',"permissionRequirements":["project:Read","project-task:Create","project:Update"],"completionEvidence":["真实项目","项目任务","证据关系","进度与审计"]}')
    ),
    (
        '81000000-0000-0000-0000-000000000006',
        '82000000-0000-0000-0000-000000000006',
        @QaId,
        N'SCN-QA-DEVIATION-TO-CAPA',
        N'质量异常到 CAPA',
        N'从批次异常进入现场确认、质量调查、CAPA 决策，质量放行保持永久人工。',
        CONCAT(N'{"problemSolved":"统一批次异常、调查、CAPA 和放行证据","startMode":"CreateBusinessObjectAndWorkflow","triggerDescription":"批次异常或质量偏差被发现时发起","applicableDepartmentCodes":["QA","PRODUCTION"],"participantRoles":["质量工程师","生产人员","质量负责人"],"objectTypeCodes":["QUALITY_DEVIATION"],"workflowDefinitionCode":"BATCH_DEVIATION_CAPA","workflowVersionNumber":1,"steps":[{"sequence":1,"code":"BATCH_DEVIATION","name":"批次异常登记","businessDescription":"登记批次、现象和受影响范围","responsibleRole":"质量工程师","action":"创建质量偏差主对象","expectedResult":"异常主记录形成","evidence":"批次异常对象和原始材料"},{"sequence":2,"code":"SHIFT_CONFIRM","name":"现场确认","businessDescription":"生产确认现场事实，不修改设备或联锁","responsibleRole":"生产人员","action":"提交现场确认","expectedResult":"现场事实可追溯","evidence":"现场记录和附件"},{"sequence":3,"code":"JOINT_REVIEW","name":"质量调查","businessDescription":"质量组织调查根因","responsibleRole":"质量工程师","action":"记录调查结论","expectedResult":"根因分析完成","evidence":"调查记录"},{"sequence":4,"code":"CAPA_DECISION","name":"CAPA 决策","businessDescription":"决定纠正预防措施，质量放行继续人工","responsibleRole":"质量负责人","action":"人工审批 CAPA，不自动质量放行","expectedResult":"CAPA 结论与后续任务形成","evidence":"流程任务、审计和全部证据"}],"preparationItems":[{"code":"DEVIATION","name":"批次异常信息","isRequired":true,"sourceType":"BusinessObject","description":"批次、现象和受影响范围"},{"code":"BATCH_EVIDENCE","name":"批次或检测材料","isRequired":true,"sourceType":"Attachment","description":"受控原始证据"}],"supportedFileSources":["Attachment"],"supportedMinutesSources":["MinutesSource"],"allowedAgents":[{"agentProfileCode":"AGENT-DEMO","capability":"workflow.read","boundary":"只提供调查建议，不质量放行"}],"permanentHumanBoundaries":',@Boundaries,N',"permissionRequirements":["business-object:Create","workflow-instance:Submit","workflow-task:Approve","file:Create"],"completionEvidence":["质量偏差主对象","CAPA 流程","批次与检测附件","质量人工放行审计"]}')
    );

    INSERT INTO [Scenario].[ScenarioDefinition]
        (Id, OrganizationId, OwnerDepartmentId, Code, Name, Description,
         LifecycleStatus, CurrentPublishedVersionId, Version, Revision,
         CreatedBy, CreatedAt, UpdatedBy, UpdatedAt,
         DisabledBy, DisabledAt, DisabledReason)
    SELECT
        seed.ScenarioId, @OrgId, seed.OwnerDepartmentId, seed.Code, seed.Name,
        seed.Description, N'Published', seed.VersionId, 2, 2,
        @AdminUserId, @Now, @AdminUserId, @Now, NULL, NULL, NULL
    FROM @SceneSeed seed
    WHERE seed.OwnerDepartmentId IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM [Scenario].[ScenarioDefinition] existing
          WHERE existing.OrganizationId = @OrgId
            AND existing.Code = seed.Code);

    INSERT INTO [Scenario].[ScenarioVersion]
        (Id, ScenarioId, VersionNumber, Status, Revision, PayloadJson,
         ReferenceSnapshotJson, ValidationJson, ImpactAnalysisJson,
         ChangeSummary, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt,
         ValidatedBy, ValidatedAt, PublishedBy, PublishedAt,
         RetiredBy, RetiredAt)
    SELECT
        seed.VersionId, seed.ScenarioId, 1, N'Published', 1, seed.PayloadJson,
        NULL,
        N'{"isValid":true,"errors":[],"warnings":["系统基础场景已冻结引用"],"references":null,"validatedAt":"2026-11-03T04:36:27Z"}',
        N'{"affectedCapabilities":["BusinessObject","Workflow","Task","Document","Minutes","Project","Agent","Audit","Outbox"]}',
        N'系统基础场景初始化', @AdminUserId, @Now, @AdminUserId, @Now,
        @AdminUserId, @Now, @AdminUserId, @Now, NULL, NULL
    FROM @SceneSeed seed
    JOIN [Scenario].[ScenarioDefinition] definition
        ON definition.Id = seed.ScenarioId
    WHERE seed.OwnerDepartmentId IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM [Scenario].[ScenarioVersion] existing
          WHERE existing.ScenarioId = seed.ScenarioId
            AND existing.VersionNumber = 1);

    UPDATE versionRow
    SET ReferenceSnapshotJson = CONCAT(
        N'{"objectTypes":',
        COALESCE((
            SELECT objectType.Id AS id, objectType.Code AS code,
                   objectType.Name AS name, objectType.Version AS version,
                   objectType.Status AS status
            FROM OPENJSON(seed.PayloadJson, N'$.objectTypeCodes') objectCode
            JOIN [Metadata].[ObjectType] objectType
                ON objectType.Code = CONVERT(nvarchar(64), objectCode.[value])
            FOR JSON PATH
        ), N'[]'),
        N',"workflow":',
        COALESCE((
            SELECT TOP (1)
                   definitionRow.Id AS definitionId,
                   definitionRow.Code AS definitionCode,
                   definitionRow.Name AS definitionName,
                   workflowVersion.Id AS versionId,
                   workflowVersion.VersionNumber AS versionNumber,
                   workflowVersion.Status AS status
            FROM [Workflow].[WorkflowDefinition] definitionRow
            JOIN [Workflow].[WorkflowVersion] workflowVersion
                ON workflowVersion.WorkflowDefinitionId = definitionRow.Id
            WHERE definitionRow.Code = JSON_VALUE(seed.PayloadJson, N'$.workflowDefinitionCode')
              AND workflowVersion.VersionNumber =
                  TRY_CONVERT(int, JSON_VALUE(seed.PayloadJson, N'$.workflowVersionNumber'))
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        ), N'null'),
        N',"agents":',
        COALESCE((
            SELECT profileRow.Id AS id, profileRow.Code AS code,
                   profileRow.Name AS name,
                   profileVersion.VersionNumber AS versionNumber,
                   profileRow.LifecycleStatus AS lifecycleStatus,
                   JSON_QUERY(profileVersion.AllowedToolsJson) AS allowedTools
            FROM OPENJSON(seed.PayloadJson, N'$.allowedAgents') agentRow
            JOIN [Agent].[AgentProfile] profileRow
                ON profileRow.Code = JSON_VALUE(agentRow.[value], N'$.agentProfileCode')
            JOIN [Agent].[AgentProfileVersion] profileVersion
                ON profileVersion.Id = profileRow.CurrentPublishedVersionId
            FOR JSON PATH
        ), N'[]'),
        N',"frozenAt":"2026-11-03T04:36:27Z"}')
    FROM [Scenario].[ScenarioVersion] versionRow
    JOIN @SceneSeed seed ON seed.VersionId = versionRow.Id;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261103043627_017_ScenarioCatalogBackend', N'10.0.14');
END;

COMMIT;
GO

