BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    IF SCHEMA_ID(N'Mcp') IS NULL EXEC(N'CREATE SCHEMA [Mcp];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    CREATE TABLE [Mcp].[AccessToken] (
        [Id] uniqueidentifier NOT NULL,
        [TokenHash] nvarchar(64) NOT NULL,
        [TokenKind] nvarchar(16) NOT NULL,
        [TokenFamilyId] uniqueidentifier NOT NULL,
        [ClientId] nvarchar(80) NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [GrantedScopesJson] nvarchar(500) NOT NULL,
        [DepartmentIdsJson] nvarchar(4000) NOT NULL,
        [ParentTokenId] uniqueidentifier NULL,
        [ExpiresAt] datetime2(3) NOT NULL,
        [RevokedAt] datetime2(3) NULL,
        [LastUsedAt] datetime2(3) NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_McpAccessToken] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_McpAccessToken_Kind] CHECK ([TokenKind] IN ('Access','Refresh'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    CREATE TABLE [Mcp].[AuthorizationCode] (
        [Id] uniqueidentifier NOT NULL,
        [CodeHash] nvarchar(64) NOT NULL,
        [ClientId] nvarchar(80) NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [RedirectUri] nvarchar(2000) NOT NULL,
        [GrantedScopesJson] nvarchar(500) NOT NULL,
        [DepartmentIdsJson] nvarchar(4000) NOT NULL,
        [CodeChallenge] nvarchar(256) NOT NULL,
        [CodeChallengeMethod] nvarchar(16) NOT NULL,
        [ExpiresAt] datetime2(3) NOT NULL,
        [ConsumedAt] datetime2(3) NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_McpAuthorizationCode] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    CREATE TABLE [Mcp].[AuthorizationRequest] (
        [Id] uniqueidentifier NOT NULL,
        [ClientId] nvarchar(80) NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [RedirectUri] nvarchar(2000) NOT NULL,
        [RequestedScopesJson] nvarchar(500) NOT NULL,
        [State] nvarchar(512) NOT NULL,
        [CodeChallenge] nvarchar(256) NOT NULL,
        [CodeChallengeMethod] nvarchar(16) NOT NULL,
        [ExpiresAt] datetime2(3) NOT NULL,
        [ConsumedAt] datetime2(3) NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_McpAuthorizationRequest] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_McpAuthorizationRequest_ChallengeMethod] CHECK ([CodeChallengeMethod]='S256')
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    CREATE TABLE [Mcp].[Client] (
        [Id] uniqueidentifier NOT NULL,
        [ClientId] nvarchar(80) NOT NULL,
        [ClientName] nvarchar(200) NOT NULL,
        [ClientSecretHash] nvarchar(128) NULL,
        [RedirectUrisJson] nvarchar(max) NOT NULL,
        [GrantTypesJson] nvarchar(500) NOT NULL,
        [ScopesJson] nvarchar(500) NOT NULL,
        [TokenEndpointAuthMethod] nvarchar(32) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [LastUsedAt] datetime2(3) NULL,
        CONSTRAINT [PK_McpClient] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_McpClient_AuthMethod] CHECK ([TokenEndpointAuthMethod] IN ('none','client_secret_post','client_secret_basic'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    CREATE TABLE [Mcp].[HumanConfirmation] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [ClientId] nvarchar(80) NOT NULL,
        [ToolName] nvarchar(128) NOT NULL,
        [ResourceType] nvarchar(128) NOT NULL,
        [ResourceId] nvarchar(128) NOT NULL,
        [RequestHash] nvarchar(64) NOT NULL,
        [Summary] nvarchar(2000) NOT NULL,
        [Status] nvarchar(16) NOT NULL,
        [DecisionComment] nvarchar(1000) NULL,
        [ConfirmedBy] uniqueidentifier NULL,
        [ConfirmedAt] datetime2(3) NULL,
        [ConsumedAt] datetime2(3) NULL,
        [ExpiresAt] datetime2(3) NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_McpHumanConfirmation] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_McpConfirmation_Status] CHECK ([Status] IN ('Pending','Confirmed','Rejected','Consumed'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    CREATE INDEX [IX_McpAccessToken_Client_Kind_Expiry] ON [Mcp].[AccessToken] ([ClientId], [TokenKind], [ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    CREATE INDEX [IX_McpAccessToken_Family_Expiry] ON [Mcp].[AccessToken] ([TokenFamilyId], [ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    CREATE INDEX [IX_McpAccessToken_User_Expiry] ON [Mcp].[AccessToken] ([UserId], [ExpiresAt], [RevokedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    CREATE UNIQUE INDEX [UX_McpAccessToken_Hash] ON [Mcp].[AccessToken] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    CREATE INDEX [IX_McpAuthorizationCode_Client_Expires] ON [Mcp].[AuthorizationCode] ([ClientId], [ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    CREATE UNIQUE INDEX [UX_McpAuthorizationCode_Hash] ON [Mcp].[AuthorizationCode] ([CodeHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    CREATE INDEX [IX_McpAuthorizationRequest_Client_Expires] ON [Mcp].[AuthorizationRequest] ([ClientId], [ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    CREATE INDEX [IX_McpAuthorizationRequest_User_Expires] ON [Mcp].[AuthorizationRequest] ([UserId], [ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    CREATE INDEX [IX_McpClient_Active_LastUsed] ON [Mcp].[Client] ([IsActive], [LastUsedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    CREATE UNIQUE INDEX [UX_McpClient_ClientId] ON [Mcp].[Client] ([ClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    CREATE INDEX [IX_McpConfirmation_Request] ON [Mcp].[HumanConfirmation] ([ClientId], [ToolName], [RequestHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    CREATE INDEX [IX_McpConfirmation_User_Status_Expiry] ON [Mcp].[HumanConfirmation] ([UserId], [Status], [ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [Identity].[User] WHERE UserName = N'agent.mcp')
    BEGIN
        DECLARE @McpOrganizationId uniqueidentifier =
            (SELECT TOP (1) Id FROM [Identity].[Organization] WHERE IsActive = 1 ORDER BY CreatedAt);
        IF @McpOrganizationId IS NOT NULL
        BEGIN
            INSERT INTO [Identity].[User]
                (Id, OrganizationId, UserName, DisplayName, IsServiceIdentity, IsActive, CreatedAt)
            VALUES
                ('10000000-0000-0000-0000-000000000024', @McpOrganizationId,
                 N'agent.mcp', N'MCP 接入网关服务身份', 1, 1, SYSUTCDATETIME());
        END;
    END;

    IF NOT EXISTS (SELECT 1 FROM [Identity].[Permission] WHERE Code = N'mcp-read')
    BEGIN
        INSERT INTO [Identity].[Permission] (Id, Code, Resource, Action)
        VALUES
            ('32000000-0000-0000-0000-000000000001', N'mcp-read', N'mcp', N'Read');
    END;

    IF NOT EXISTS (SELECT 1 FROM [Identity].[Permission] WHERE Code = N'mcp-manage')
    BEGIN
        INSERT INTO [Identity].[Permission] (Id, Code, Resource, Action)
        VALUES
            ('32000000-0000-0000-0000-000000000002', N'mcp-manage', N'mcp', N'Manage');
    END;

    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT roleRow.Id, permissionRow.Id
    FROM [Identity].[Role] roleRow
    CROSS JOIN [Identity].[Permission] permissionRow
    WHERE roleRow.Code = N'platform-admin'
      AND permissionRow.Code IN (N'mcp-read', N'mcp-manage')
      AND NOT EXISTS (
          SELECT 1
          FROM [Identity].[RolePermission] existing
          WHERE existing.RoleId = roleRow.Id
            AND existing.PermissionId = permissionRow.Id);

    DECLARE @McpNavigationVersionId uniqueidentifier =
        (SELECT TOP (1) Id
         FROM [Navigation].[NavigationVersion]
         WHERE Status = N'Published'
         ORDER BY PublishedAt DESC, VersionNumber DESC);
    IF @McpNavigationVersionId IS NOT NULL
       AND NOT EXISTS (
           SELECT 1 FROM [Navigation].[NavigationItem]
           WHERE NavigationVersionId = @McpNavigationVersionId
             AND ItemKey = N'MCP_CENTER')
    BEGIN
        INSERT INTO [Navigation].[NavigationItem]
            (Id, NavigationVersionId, ItemKey, Name, ItemType, Route,
             RequiredResource, RequiredAction, SortOrder, IsVisible, IsEnabled,
             IsSystem, Icon, CreatedAt, UpdatedAt)
        VALUES
            ('92000000-0000-0000-0000-000000000016', @McpNavigationVersionId,
             N'MCP_CENTER', N'MCP 接入治理', N'Administration', N'/mcp-center',
             N'mcp', N'Read', 160, 1, 1, 0, N'M', SYSUTCDATETIME(), SYSUTCDATETIME());
    END;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101122556_010_McpStandardServer'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261101122556_010_McpStandardServer', N'10.0.14');
END;

COMMIT;
GO

