using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIDT.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class McpStandardServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;");
            migrationBuilder.EnsureSchema(
                name: "Mcp");

            migrationBuilder.CreateTable(
                name: "AccessToken",
                schema: "Mcp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TokenKind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    TokenFamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrantedScopesJson = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DepartmentIdsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ParentTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_McpAccessToken", x => x.Id);
                    table.CheckConstraint("CK_McpAccessToken_Kind", "[TokenKind] IN ('Access','Refresh')");
                });

            migrationBuilder.CreateTable(
                name: "AuthorizationCode",
                schema: "Mcp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RedirectUri = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    GrantedScopesJson = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DepartmentIdsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CodeChallenge = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CodeChallengeMethod = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_McpAuthorizationCode", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthorizationRequest",
                schema: "Mcp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RedirectUri = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RequestedScopesJson = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    State = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    CodeChallenge = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CodeChallengeMethod = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_McpAuthorizationRequest", x => x.Id);
                    table.CheckConstraint("CK_McpAuthorizationRequest_ChallengeMethod", "[CodeChallengeMethod]='S256'");
                });

            migrationBuilder.CreateTable(
                name: "Client",
                schema: "Mcp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ClientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ClientSecretHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RedirectUrisJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GrantTypesJson = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ScopesJson = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TokenEndpointAuthMethod = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_McpClient", x => x.Id);
                    table.CheckConstraint("CK_McpClient_AuthMethod", "[TokenEndpointAuthMethod] IN ('none','client_secret_post','client_secret_basic')");
                });

            migrationBuilder.CreateTable(
                name: "HumanConfirmation",
                schema: "Mcp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ToolName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ResourceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    DecisionComment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ConfirmedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ConsumedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_McpHumanConfirmation", x => x.Id);
                    table.CheckConstraint("CK_McpConfirmation_Status", "[Status] IN ('Pending','Confirmed','Rejected','Consumed')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_McpAccessToken_Client_Kind_Expiry",
                schema: "Mcp",
                table: "AccessToken",
                columns: new[] { "ClientId", "TokenKind", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_McpAccessToken_Family_Expiry",
                schema: "Mcp",
                table: "AccessToken",
                columns: new[] { "TokenFamilyId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_McpAccessToken_User_Expiry",
                schema: "Mcp",
                table: "AccessToken",
                columns: new[] { "UserId", "ExpiresAt", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_McpAccessToken_Hash",
                schema: "Mcp",
                table: "AccessToken",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_McpAuthorizationCode_Client_Expires",
                schema: "Mcp",
                table: "AuthorizationCode",
                columns: new[] { "ClientId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "UX_McpAuthorizationCode_Hash",
                schema: "Mcp",
                table: "AuthorizationCode",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_McpAuthorizationRequest_Client_Expires",
                schema: "Mcp",
                table: "AuthorizationRequest",
                columns: new[] { "ClientId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_McpAuthorizationRequest_User_Expires",
                schema: "Mcp",
                table: "AuthorizationRequest",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_McpClient_Active_LastUsed",
                schema: "Mcp",
                table: "Client",
                columns: new[] { "IsActive", "LastUsedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_McpClient_ClientId",
                schema: "Mcp",
                table: "Client",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_McpConfirmation_Request",
                schema: "Mcp",
                table: "HumanConfirmation",
                columns: new[] { "ClientId", "ToolName", "RequestHash" });

            migrationBuilder.CreateIndex(
                name: "IX_McpConfirmation_User_Status_Expiry",
                schema: "Mcp",
                table: "HumanConfirmation",
                columns: new[] { "UserId", "Status", "ExpiresAt" });

            migrationBuilder.Sql(
                """
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
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;");
            migrationBuilder.DropTable(
                name: "AccessToken",
                schema: "Mcp");

            migrationBuilder.DropTable(
                name: "AuthorizationCode",
                schema: "Mcp");

            migrationBuilder.DropTable(
                name: "AuthorizationRequest",
                schema: "Mcp");

            migrationBuilder.DropTable(
                name: "Client",
                schema: "Mcp");

            migrationBuilder.DropTable(
                name: "HumanConfirmation",
                schema: "Mcp");

            migrationBuilder.Sql(
                """
                DELETE FROM [Navigation].[NavigationItem]
                WHERE ItemKey = N'MCP_CENTER'
                  AND Id = '92000000-0000-0000-0000-000000000016';

                DELETE FROM [Identity].[RolePermission]
                WHERE PermissionId IN (
                    SELECT Id FROM [Identity].[Permission]
                    WHERE Code = N'mcp-read'
                       OR Code = N'mcp-manage');

                DELETE FROM [Identity].[Permission]
                WHERE Code = N'mcp-read'
                   OR Code = N'mcp-manage';

                DELETE FROM [Identity].[DataScope]
                WHERE UserId = '10000000-0000-0000-0000-000000000024';
                DELETE FROM [Identity].[UserRole]
                WHERE UserId = '10000000-0000-0000-0000-000000000024';
                DELETE FROM [Identity].[UserDepartmentMembership]
                WHERE UserId = '10000000-0000-0000-0000-000000000024';
                DELETE FROM [Identity].[User]
                WHERE Id = '10000000-0000-0000-0000-000000000024'
                  AND UserName = N'agent.mcp';
                """);
            migrationBuilder.Sql(
                """
                IF SCHEMA_ID(N'Mcp') IS NOT NULL
                    EXEC(N'DROP SCHEMA [Mcp]');
                """);
        }
    }
}
