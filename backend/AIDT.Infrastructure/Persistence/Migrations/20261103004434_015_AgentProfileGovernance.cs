using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIDT.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgentProfileGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_AgentProfile_Name",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AccessChangeRequest_Type",
                schema: "Identity",
                table: "AccessChangeRequest");

            migrationBuilder.AddColumn<Guid>(
                name: "AgentProfileId",
                schema: "Mcp",
                table: "Client",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessOwnerUserId",
                schema: "Agent",
                table: "AgentProfile",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "Agent",
                table: "AgentProfile",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "Agent",
                table: "AgentProfile",
                type: "datetime2(3)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                schema: "Agent",
                table: "AgentProfile",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentPublishedVersionId",
                schema: "Agent",
                table: "AgentProfile",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataScopesJson",
                schema: "Agent",
                table: "AgentProfile",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                schema: "Agent",
                table: "AgentProfile",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "Agent",
                table: "AgentProfile",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LifecycleStatus",
                schema: "Agent",
                table: "AgentProfile",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ModelScopesJson",
                schema: "Agent",
                table: "AgentProfile",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequiredConfirmationsJson",
                schema: "Agent",
                table: "AgentProfile",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "Revision",
                schema: "Agent",
                table: "AgentProfile",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "StopConditionsJson",
                schema: "Agent",
                table: "AgentProfile",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "TechnicalOwnerUserId",
                schema: "Agent",
                table: "AgentProfile",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "Agent",
                table: "AgentProfile",
                type: "datetime2(3)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                schema: "Agent",
                table: "AgentProfile",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<long>(
                name: "Version",
                schema: "Agent",
                table: "AgentProfile",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<Guid>(
                name: "AgentProfileId",
                schema: "Mcp",
                table: "AccessToken",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevocationReason",
                schema: "Mcp",
                table: "AccessToken",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RevokedBy",
                schema: "Mcp",
                table: "AccessToken",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AgentProfileVersion",
                schema: "Agent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ServiceUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessOwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TechnicalOwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AutonomyLevel = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    AmountLimit = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    RiskLimit = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    DataScopesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModelScopesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AllowedToolsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredConfirmationsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StopConditionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValidationJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsHighRisk = table.Column<bool>(type: "bit", nullable: false),
                    HighRiskReasonsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ValidatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValidatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    PublishedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentProfileVersion", x => x.Id);
                    table.CheckConstraint("CK_AgentProfileVersion_AllowedToolsJson", "ISJSON([AllowedToolsJson]) = 1");
                    table.CheckConstraint("CK_AgentProfileVersion_AmountLimit", "[AmountLimit] >= 0");
                    table.CheckConstraint("CK_AgentProfileVersion_ConfirmationsJson", "ISJSON([RequiredConfirmationsJson]) = 1");
                    table.CheckConstraint("CK_AgentProfileVersion_DataScopesJson", "ISJSON([DataScopesJson]) = 1");
                    table.CheckConstraint("CK_AgentProfileVersion_HighRiskReasonsJson", "ISJSON([HighRiskReasonsJson]) = 1");
                    table.CheckConstraint("CK_AgentProfileVersion_ModelScopesJson", "ISJSON([ModelScopesJson]) = 1");
                    table.CheckConstraint("CK_AgentProfileVersion_Status", "[Status] IN ('Draft','Validated','Published','Retired')");
                    table.CheckConstraint("CK_AgentProfileVersion_StopConditionsJson", "ISJSON([StopConditionsJson]) = 1");
                    table.ForeignKey(
                        name: "FK_AgentProfileVersion_BusinessOwner",
                        column: x => x.BusinessOwnerUserId,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgentProfileVersion_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgentProfileVersion_Department",
                        column: x => x.DepartmentId,
                        principalSchema: "Identity",
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgentProfileVersion_Profile",
                        column: x => x.AgentProfileId,
                        principalSchema: "Agent",
                        principalTable: "AgentProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgentProfileVersion_PublishedBy",
                        column: x => x.PublishedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgentProfileVersion_ServiceUser",
                        column: x => x.ServiceUserId,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgentProfileVersion_TechnicalOwner",
                        column: x => x.TechnicalOwnerUserId,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgentProfileVersion_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgentProfileVersion_ValidatedBy",
                        column: x => x.ValidatedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                DECLARE @Now datetime2(3) = SYSUTCDATETIME();
                DECLARE @OrgId uniqueidentifier =
                    (SELECT TOP (1) Id FROM [Identity].[Organization] WHERE IsActive = 1 ORDER BY CreatedAt, Id);
                DECLARE @AdminUserId uniqueidentifier =
                    (SELECT TOP (1) Id FROM [Identity].[User] WHERE UserName = N'demo.admin' AND IsActive = 1);
                DECLARE @RndDepartmentId uniqueidentifier =
                    (SELECT TOP (1) Id FROM [Identity].[Department]
                     WHERE OrganizationId = @OrgId AND Code = N'RND' AND IsActive = 1);
                DECLARE @ConfirmationsJson nvarchar(max) =
                    N'["contract-commitment","customer-compensation","equipment-control","formula-change","payment","quality-release","safety-interlock"]';

                IF @OrgId IS NULL OR @AdminUserId IS NULL OR @RndDepartmentId IS NULL
                    THROW 51000, N'015 requires the demo organization, admin and RND department.', 1;

                UPDATE [Agent].[AgentProfile]
                SET Code = CASE Name
                        WHEN N'业务协同数字员工' THEN N'AGENT-DEMO'
                        WHEN N'流程代理数字员工' THEN N'AGENT-PROXY'
                        WHEN N'项目事件建议数字员工' THEN N'AGENT-PROJECT-EVENT'
                        ELSE N'AGENT-LEGACY-' + REPLACE(CONVERT(nvarchar(36), Id), N'-', N'')
                    END,
                    Description = N'既有数字员工身份迁移回填',
                    DepartmentId = @RndDepartmentId,
                    BusinessOwnerUserId = @AdminUserId,
                    TechnicalOwnerUserId = @AdminUserId,
                    DataScopesJson = CONCAT(
                        N'["organization:', CONVERT(nvarchar(36), @OrgId),
                        N'","department:', CONVERT(nvarchar(36), @RndDepartmentId), N'"]'),
                    ModelScopesJson = N'["aidt-rule-sim"]',
                    AllowedToolsJson = CASE
                        WHEN Name = N'业务协同数字员工'
                            THEN N'["workflow.assist","workflow.read"]'
                        ELSE AllowedToolsJson
                    END,
                    RequiredConfirmationsJson = @ConfirmationsJson,
                    StopConditionsJson =
                        N'["amount-limit","manual-stop","risk-limit","unauthorized-tool"]',
                    LifecycleStatus = CASE WHEN IsActive = 1 THEN N'Published' ELSE N'Disabled' END,
                    Version = 1,
                    Revision = 1,
                    CreatedBy = @AdminUserId,
                    CreatedAt = @Now,
                    UpdatedBy = @AdminUserId,
                    UpdatedAt = @Now;

                INSERT INTO [Agent].[AgentProfileVersion]
                    (Id, AgentProfileId, VersionNumber, Revision, Status, Code, Name, Description,
                     ServiceUserId, DepartmentId, BusinessOwnerUserId, TechnicalOwnerUserId,
                     AutonomyLevel, AmountLimit, RiskLimit, DataScopesJson, ModelScopesJson,
                     AllowedToolsJson, RequiredConfirmationsJson, StopConditionsJson,
                     ValidationJson, IsHighRisk, HighRiskReasonsJson,
                     CreatedBy, CreatedAt, UpdatedBy, UpdatedAt,
                     ValidatedBy, ValidatedAt, PublishedBy, PublishedAt, RetiredAt)
                SELECT
                    CASE profile.Code
                        WHEN N'AGENT-DEMO' THEN '70000000-0000-0000-0000-000000000001'
                        WHEN N'AGENT-PROXY' THEN '70000000-0000-0000-0000-000000000002'
                        WHEN N'AGENT-PROJECT-EVENT' THEN '70000000-0000-0000-0000-000000000003'
                        ELSE NEWID()
                    END,
                    profile.Id, 1, 1, N'Published', profile.Code, profile.Name, profile.Description,
                    profile.ServiceUserId, profile.DepartmentId, profile.BusinessOwnerUserId,
                    profile.TechnicalOwnerUserId, profile.AutonomyLevel, profile.AmountLimit,
                    profile.RiskLimit, profile.DataScopesJson, profile.ModelScopesJson,
                    profile.AllowedToolsJson, profile.RequiredConfirmationsJson,
                    profile.StopConditionsJson,
                    N'{"isValid":true,"errors":[],"warnings":["历史身份自动回填"],"isHighRisk":false,"highRiskReasons":[],"validatedAt":"2026-11-03T00:00:00Z"}',
                    0, N'[]', @AdminUserId, @Now, @AdminUserId, @Now,
                    @AdminUserId, @Now, @AdminUserId, @Now, NULL
                FROM [Agent].[AgentProfile] profile;

                UPDATE profile
                SET CurrentPublishedVersionId = version.Id
                FROM [Agent].[AgentProfile] profile
                JOIN [Agent].[AgentProfileVersion] version
                    ON version.AgentProfileId = profile.Id
                   AND version.VersionNumber = 1;

                IF NOT EXISTS (SELECT 1 FROM [Identity].[User] WHERE UserName = N'demo.agentmanager')
                    INSERT INTO [Identity].[User]
                        (Id, OrganizationId, UserName, DisplayName, IsServiceIdentity, IsActive, CreatedAt)
                    VALUES
                        ('10000000-0000-0000-0000-000000000027', @OrgId,
                         N'demo.agentmanager', N'研发数字员工负责人', 0, 1, @Now);

                IF NOT EXISTS (SELECT 1 FROM [Identity].[User] WHERE UserName = N'agent.lowrisk')
                    INSERT INTO [Identity].[User]
                        (Id, OrganizationId, UserName, DisplayName, IsServiceIdentity, IsActive, CreatedAt)
                    VALUES
                        ('10000000-0000-0000-0000-000000000028', @OrgId,
                         N'agent.lowrisk', N'低风险治理演练数字员工', 1, 1, @Now);

                IF NOT EXISTS (SELECT 1 FROM [Identity].[User] WHERE UserName = N'agent.highrisk')
                    INSERT INTO [Identity].[User]
                        (Id, OrganizationId, UserName, DisplayName, IsServiceIdentity, IsActive, CreatedAt)
                    VALUES
                        ('10000000-0000-0000-0000-000000000029', @OrgId,
                         N'agent.highrisk', N'高风险治理演练数字员工', 1, 1, @Now);

                IF NOT EXISTS (SELECT 1 FROM [Identity].[Role] WHERE Code = N'agent-department-manager')
                    INSERT INTO [Identity].[Role]
                        (Id, Code, Name, RoleType, Description, IsSystem, IsActive, Version, CreatedAt, UpdatedAt)
                    VALUES
                        ('2a000000-0000-0000-0000-000000000001', N'agent-department-manager',
                         N'数字员工部门负责人', N'Business', N'授权部门内管理数字员工；高风险发布需平台复核。',
                         0, 1, 1, @Now, @Now);

                IF NOT EXISTS (SELECT 1 FROM [Identity].[Role] WHERE Code = N'agent-service')
                    INSERT INTO [Identity].[Role]
                        (Id, Code, Name, RoleType, Description, IsSystem, IsActive, Version, CreatedAt, UpdatedAt)
                    VALUES
                        ('2a000000-0000-0000-0000-000000000002', N'agent-service',
                         N'数字员工服务角色', N'AgentService', N'最小业务读取与流程执行权限。',
                         0, 1, 1, @Now, @Now);

                INSERT INTO [Identity].[Permission] (Id, Code, Resource, Action)
                SELECT seed.Id, seed.Code, seed.Resource, seed.Action
                FROM (VALUES
                    (CONVERT(uniqueidentifier, '3a000000-0000-0000-0000-000000000001'), N'agent-create', N'agent', N'Create'),
                    (CONVERT(uniqueidentifier, '3a000000-0000-0000-0000-000000000002'), N'agent-update', N'agent', N'Update'),
                    (CONVERT(uniqueidentifier, '3a000000-0000-0000-0000-000000000003'), N'agent-manage', N'agent', N'Manage'),
                    (CONVERT(uniqueidentifier, '3a000000-0000-0000-0000-000000000004'), N'agent-approve', N'agent', N'Approve')
                ) seed(Id, Code, Resource, Action)
                WHERE NOT EXISTS (
                    SELECT 1 FROM [Identity].[Permission] existing
                    WHERE existing.Resource = seed.Resource AND existing.Action = seed.Action
                );

                INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
                SELECT roleRow.Id, permissionRow.Id
                FROM [Identity].[Role] roleRow
                JOIN [Identity].[Permission] permissionRow
                    ON permissionRow.Resource = N'agent'
                   AND permissionRow.Action IN (N'Read', N'Create', N'Update', N'Manage', N'Approve')
                WHERE roleRow.Code IN (N'platform-admin', N'agent-department-manager', N'project-manager')
                  AND NOT EXISTS (
                      SELECT 1 FROM [Identity].[RolePermission] existing
                      WHERE existing.RoleId = roleRow.Id
                        AND existing.PermissionId = permissionRow.Id);

                INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
                SELECT roleRow.Id, permissionRow.Id
                FROM [Identity].[Role] roleRow
                JOIN [Identity].[Permission] permissionRow
                    ON permissionRow.Code IN (
                        N'agent-read', N'workflow-instance-read', N'workflow-task-read',
                        N'workflow-task-update', N'workflow-task-approve', N'event-inbox-read',
                        N'project-read', N'project-task-read', N'project-task-create',
                        N'project-task-update', N'audit-read', N'file-read', N'department-read')
                WHERE roleRow.Code IN (N'agent-service', N'agent-department-manager')
                  AND NOT EXISTS (
                      SELECT 1 FROM [Identity].[RolePermission] existing
                      WHERE existing.RoleId = roleRow.Id
                        AND existing.PermissionId = permissionRow.Id);

                INSERT INTO [Identity].[UserRole] (UserId, RoleId)
                SELECT userRow.Id, roleRow.Id
                FROM [Identity].[User] userRow
                JOIN [Identity].[Role] roleRow
                    ON roleRow.Code = CASE
                        WHEN userRow.UserName = N'demo.agentmanager' THEN N'agent-department-manager'
                        ELSE N'agent-service'
                    END
                WHERE userRow.UserName IN (
                    N'demo.agentmanager', N'agent.lowrisk', N'agent.highrisk',
                    N'agent.demo', N'agent.proxy', N'agent.project')
                  AND NOT EXISTS (
                      SELECT 1 FROM [Identity].[UserRole] existing
                      WHERE existing.UserId = userRow.Id
                        AND existing.RoleId = roleRow.Id);

                INSERT INTO [Identity].[UserDepartmentMembership]
                    (UserId, DepartmentId, IsPrimary, IsManager, IsActive, JoinedAt)
                SELECT userRow.Id, @RndDepartmentId, 1,
                       0,
                       1, @Now
                FROM [Identity].[User] userRow
                WHERE userRow.UserName IN (
                    N'demo.agentmanager', N'agent.lowrisk', N'agent.highrisk',
                    N'agent.demo', N'agent.proxy', N'agent.project')
                  AND NOT EXISTS (
                      SELECT 1 FROM [Identity].[UserDepartmentMembership] existing
                      WHERE existing.UserId = userRow.Id
                        AND existing.DepartmentId = @RndDepartmentId);

                INSERT INTO [Identity].[DataScope] (Id, UserId, ScopeType, ScopeReference)
                SELECT NEWID(), userRow.Id, N'organization', CONVERT(nvarchar(36), @OrgId)
                FROM [Identity].[User] userRow
                WHERE userRow.UserName IN (
                    N'demo.agentmanager', N'agent.lowrisk', N'agent.highrisk',
                    N'agent.demo', N'agent.proxy', N'agent.project')
                  AND NOT EXISTS (
                      SELECT 1 FROM [Identity].[DataScope] existing
                      WHERE existing.UserId = userRow.Id
                        AND existing.ScopeType = N'organization'
                        AND existing.ScopeReference = CONVERT(nvarchar(36), @OrgId));

                INSERT INTO [Identity].[DataScope] (Id, UserId, ScopeType, ScopeReference)
                SELECT NEWID(), userRow.Id, N'department', CONVERT(nvarchar(36), @RndDepartmentId)
                FROM [Identity].[User] userRow
                WHERE userRow.UserName IN (
                    N'demo.agentmanager', N'agent.lowrisk', N'agent.highrisk',
                    N'agent.demo', N'agent.proxy', N'agent.project')
                  AND NOT EXISTS (
                      SELECT 1 FROM [Identity].[DataScope] existing
                      WHERE existing.UserId = userRow.Id
                        AND existing.ScopeType = N'department'
                        AND existing.ScopeReference = CONVERT(nvarchar(36), @RndDepartmentId));
                """);

            migrationBuilder.CreateIndex(
                name: "IX_McpClient_AgentProfileId",
                schema: "Mcp",
                table: "Client",
                column: "AgentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentProfile_BusinessOwnerUserId",
                schema: "Agent",
                table: "AgentProfile",
                column: "BusinessOwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentProfile_CreatedBy",
                schema: "Agent",
                table: "AgentProfile",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AgentProfile_CurrentPublishedVersionId",
                schema: "Agent",
                table: "AgentProfile",
                column: "CurrentPublishedVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentProfile_Department_Lifecycle",
                schema: "Agent",
                table: "AgentProfile",
                columns: new[] { "DepartmentId", "LifecycleStatus", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentProfile_Name",
                schema: "Agent",
                table: "AgentProfile",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_AgentProfile_TechnicalOwnerUserId",
                schema: "Agent",
                table: "AgentProfile",
                column: "TechnicalOwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentProfile_UpdatedBy",
                schema: "Agent",
                table: "AgentProfile",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UX_AgentProfile_Code",
                schema: "Agent",
                table: "AgentProfile",
                column: "Code",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_AgentProfile_AllowedToolsJson",
                schema: "Agent",
                table: "AgentProfile",
                sql: "ISJSON([AllowedToolsJson]) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AgentProfile_ConfirmationsJson",
                schema: "Agent",
                table: "AgentProfile",
                sql: "ISJSON([RequiredConfirmationsJson]) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AgentProfile_DataScopesJson",
                schema: "Agent",
                table: "AgentProfile",
                sql: "ISJSON([DataScopesJson]) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AgentProfile_Lifecycle",
                schema: "Agent",
                table: "AgentProfile",
                sql: "[LifecycleStatus] IN ('Draft','Published','Disabled','Retired')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AgentProfile_ModelScopesJson",
                schema: "Agent",
                table: "AgentProfile",
                sql: "ISJSON([ModelScopesJson]) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AgentProfile_StopConditionsJson",
                schema: "Agent",
                table: "AgentProfile",
                sql: "ISJSON([StopConditionsJson]) = 1");

            migrationBuilder.CreateIndex(
                name: "IX_AccessToken_RevokedBy",
                schema: "Mcp",
                table: "AccessToken",
                column: "RevokedBy");

            migrationBuilder.CreateIndex(
                name: "IX_McpAccessToken_AgentProfile_Active",
                schema: "Mcp",
                table: "AccessToken",
                columns: new[] { "AgentProfileId", "RevokedAt", "ExpiresAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_AccessChangeRequest_Type",
                schema: "Identity",
                table: "AccessChangeRequest",
                sql: "[RequestType] IN ('RoleAssignment','RolePermissions','AgentProfilePublish')");

            migrationBuilder.CreateIndex(
                name: "IX_AgentProfileVersion_BusinessOwnerUserId",
                schema: "Agent",
                table: "AgentProfileVersion",
                column: "BusinessOwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentProfileVersion_CreatedBy",
                schema: "Agent",
                table: "AgentProfileVersion",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AgentProfileVersion_Department_Status_UpdatedAt",
                schema: "Agent",
                table: "AgentProfileVersion",
                columns: new[] { "DepartmentId", "Status", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentProfileVersion_PublishedBy",
                schema: "Agent",
                table: "AgentProfileVersion",
                column: "PublishedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AgentProfileVersion_ServiceUserId",
                schema: "Agent",
                table: "AgentProfileVersion",
                column: "ServiceUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentProfileVersion_TechnicalOwnerUserId",
                schema: "Agent",
                table: "AgentProfileVersion",
                column: "TechnicalOwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentProfileVersion_UpdatedBy",
                schema: "Agent",
                table: "AgentProfileVersion",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AgentProfileVersion_ValidatedBy",
                schema: "Agent",
                table: "AgentProfileVersion",
                column: "ValidatedBy");

            migrationBuilder.CreateIndex(
                name: "UX_AgentProfileVersion_Profile_Published",
                schema: "Agent",
                table: "AgentProfileVersion",
                column: "AgentProfileId",
                unique: true,
                filter: "[Status] = 'Published'");

            migrationBuilder.CreateIndex(
                name: "UX_AgentProfileVersion_Profile_Version",
                schema: "Agent",
                table: "AgentProfileVersion",
                columns: new[] { "AgentProfileId", "VersionNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_McpAccessToken_AgentProfile",
                schema: "Mcp",
                table: "AccessToken",
                column: "AgentProfileId",
                principalSchema: "Agent",
                principalTable: "AgentProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_McpAccessToken_RevokedBy",
                schema: "Mcp",
                table: "AccessToken",
                column: "RevokedBy",
                principalSchema: "Identity",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentProfile_BusinessOwner",
                schema: "Agent",
                table: "AgentProfile",
                column: "BusinessOwnerUserId",
                principalSchema: "Identity",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentProfile_CreatedBy",
                schema: "Agent",
                table: "AgentProfile",
                column: "CreatedBy",
                principalSchema: "Identity",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentProfile_CurrentPublishedVersion",
                schema: "Agent",
                table: "AgentProfile",
                column: "CurrentPublishedVersionId",
                principalSchema: "Agent",
                principalTable: "AgentProfileVersion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentProfile_Department",
                schema: "Agent",
                table: "AgentProfile",
                column: "DepartmentId",
                principalSchema: "Identity",
                principalTable: "Department",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentProfile_TechnicalOwner",
                schema: "Agent",
                table: "AgentProfile",
                column: "TechnicalOwnerUserId",
                principalSchema: "Identity",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentProfile_UpdatedBy",
                schema: "Agent",
                table: "AgentProfile",
                column: "UpdatedBy",
                principalSchema: "Identity",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_McpClient_AgentProfile",
                schema: "Mcp",
                table: "Client",
                column: "AgentProfileId",
                principalSchema: "Agent",
                principalTable: "AgentProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_McpAccessToken_AgentProfile",
                schema: "Mcp",
                table: "AccessToken");

            migrationBuilder.DropForeignKey(
                name: "FK_McpAccessToken_RevokedBy",
                schema: "Mcp",
                table: "AccessToken");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentProfile_BusinessOwner",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentProfile_CreatedBy",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentProfile_CurrentPublishedVersion",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentProfile_Department",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentProfile_TechnicalOwner",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentProfile_UpdatedBy",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropForeignKey(
                name: "FK_McpClient_AgentProfile",
                schema: "Mcp",
                table: "Client");

            migrationBuilder.Sql(
                """
                DECLARE @NewPermissionIds TABLE (Id uniqueidentifier PRIMARY KEY);
                INSERT INTO @NewPermissionIds (Id)
                SELECT Id FROM [Identity].[Permission]
                WHERE Resource = N'agent'
                  AND Action IN (N'Create', N'Update', N'Manage', N'Approve');

                DELETE rolePermission
                FROM [Identity].[RolePermission] rolePermission
                JOIN @NewPermissionIds permissionRow ON permissionRow.Id = rolePermission.PermissionId;

                DELETE FROM [Identity].[UserRole]
                WHERE UserId IN (
                    SELECT Id FROM [Identity].[User]
                    WHERE UserName IN (
                        N'demo.agentmanager', N'agent.lowrisk', N'agent.highrisk'));

                DELETE FROM [Identity].[UserDepartmentMembership]
                WHERE UserId IN (
                    SELECT Id FROM [Identity].[User]
                    WHERE UserName IN (
                        N'demo.agentmanager', N'agent.lowrisk', N'agent.highrisk'));

                DELETE FROM [Identity].[DataScope]
                WHERE UserId IN (
                    SELECT Id FROM [Identity].[User]
                    WHERE UserName IN (
                        N'demo.agentmanager', N'agent.lowrisk', N'agent.highrisk',
                        N'agent.demo', N'agent.proxy', N'agent.project'));

                DELETE rolePermission
                FROM [Identity].[RolePermission] rolePermission
                JOIN [Identity].[Role] roleRow ON roleRow.Id = rolePermission.RoleId
                WHERE roleRow.Code IN (N'agent-department-manager', N'agent-service');

                DELETE FROM [Identity].[Role]
                WHERE Code IN (N'agent-department-manager', N'agent-service');

                DELETE FROM [Identity].[User]
                WHERE UserName IN (
                    N'demo.agentmanager', N'agent.lowrisk', N'agent.highrisk');

                DELETE FROM [Identity].[Permission]
                WHERE Resource = N'agent'
                  AND Action IN (N'Create', N'Update', N'Manage', N'Approve');
                """);

            migrationBuilder.DropTable(
                name: "AgentProfileVersion",
                schema: "Agent");

            migrationBuilder.DropIndex(
                name: "IX_McpClient_AgentProfileId",
                schema: "Mcp",
                table: "Client");

            migrationBuilder.DropIndex(
                name: "IX_AgentProfile_BusinessOwnerUserId",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropIndex(
                name: "IX_AgentProfile_CreatedBy",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropIndex(
                name: "IX_AgentProfile_CurrentPublishedVersionId",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropIndex(
                name: "IX_AgentProfile_Department_Lifecycle",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropIndex(
                name: "IX_AgentProfile_Name",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropIndex(
                name: "IX_AgentProfile_TechnicalOwnerUserId",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropIndex(
                name: "IX_AgentProfile_UpdatedBy",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropIndex(
                name: "UX_AgentProfile_Code",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AgentProfile_AllowedToolsJson",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AgentProfile_ConfirmationsJson",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AgentProfile_DataScopesJson",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AgentProfile_Lifecycle",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AgentProfile_ModelScopesJson",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AgentProfile_StopConditionsJson",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropIndex(
                name: "IX_AccessToken_RevokedBy",
                schema: "Mcp",
                table: "AccessToken");

            migrationBuilder.DropIndex(
                name: "IX_McpAccessToken_AgentProfile_Active",
                schema: "Mcp",
                table: "AccessToken");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AccessChangeRequest_Type",
                schema: "Identity",
                table: "AccessChangeRequest");

            migrationBuilder.DropColumn(
                name: "AgentProfileId",
                schema: "Mcp",
                table: "Client");

            migrationBuilder.DropColumn(
                name: "BusinessOwnerUserId",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropColumn(
                name: "Code",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropColumn(
                name: "CurrentPublishedVersionId",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropColumn(
                name: "DataScopesJson",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropColumn(
                name: "LifecycleStatus",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropColumn(
                name: "ModelScopesJson",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropColumn(
                name: "RequiredConfirmationsJson",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropColumn(
                name: "Revision",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropColumn(
                name: "StopConditionsJson",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropColumn(
                name: "TechnicalOwnerUserId",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropColumn(
                name: "AgentProfileId",
                schema: "Mcp",
                table: "AccessToken");

            migrationBuilder.DropColumn(
                name: "RevocationReason",
                schema: "Mcp",
                table: "AccessToken");

            migrationBuilder.DropColumn(
                name: "RevokedBy",
                schema: "Mcp",
                table: "AccessToken");

            migrationBuilder.CreateIndex(
                name: "UX_AgentProfile_Name",
                schema: "Agent",
                table: "AgentProfile",
                column: "Name",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_AccessChangeRequest_Type",
                schema: "Identity",
                table: "AccessChangeRequest",
                sql: "[RequestType] IN ('RoleAssignment','RolePermissions')");
        }
    }
}
