SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @OrganizationId uniqueidentifier =
    (SELECT TOP (1) Id FROM [Identity].[Organization] WHERE IsActive = 1 ORDER BY CreatedAt);

IF @OrganizationId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM [Identity].[User] WHERE UserName = N'agent.mcp')
BEGIN
    INSERT INTO [Identity].[User]
        (Id, OrganizationId, UserName, DisplayName, IsServiceIdentity, IsActive, CreatedAt)
    VALUES
        ('10000000-0000-0000-0000-000000000024', @OrganizationId,
         N'agent.mcp', N'MCP 接入网关服务身份', 1, 1, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [Identity].[Permission] WHERE Code = N'mcp-read')
BEGIN
    INSERT INTO [Identity].[Permission] (Id, Code, Resource, Action)
    VALUES ('32000000-0000-0000-0000-000000000001', N'mcp-read', N'mcp', N'Read');
END;

IF NOT EXISTS (SELECT 1 FROM [Identity].[Permission] WHERE Code = N'mcp-manage')
BEGIN
    INSERT INTO [Identity].[Permission] (Id, Code, Resource, Action)
    VALUES ('32000000-0000-0000-0000-000000000002', N'mcp-manage', N'mcp', N'Manage');
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

DECLARE @NavigationVersionId uniqueidentifier =
    (SELECT TOP (1) Id
     FROM [Navigation].[NavigationVersion]
     WHERE Status = N'Published'
     ORDER BY PublishedAt DESC, VersionNumber DESC);

IF @NavigationVersionId IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM [Navigation].[NavigationItem]
       WHERE NavigationVersionId = @NavigationVersionId
         AND ItemKey = N'MCP_CENTER')
BEGIN
    INSERT INTO [Navigation].[NavigationItem]
        (Id, NavigationVersionId, ItemKey, Name, ItemType, Route,
         RequiredResource, RequiredAction, SortOrder, IsVisible, IsEnabled,
         IsSystem, Icon, CreatedAt, UpdatedAt)
    VALUES
        ('92000000-0000-0000-0000-000000000016', @NavigationVersionId,
         N'MCP_CENTER', N'MCP 接入治理', N'Administration', N'/mcp-center',
         N'mcp', N'Read', 160, 1, 1, 0, N'M', SYSUTCDATETIME(), SYSUTCDATETIME());
END;
