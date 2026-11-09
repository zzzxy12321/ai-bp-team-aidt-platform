using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIDT.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Batch009FrontendNavigationIamWorkflowRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Navigation");

            migrationBuilder.AddColumn<string>(
                name: "AssigneeSnapshotJson",
                schema: "Workflow",
                table: "WorkflowTask",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "AssigneeStrategy",
                schema: "Workflow",
                table: "WorkflowTask",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "SpecificUser");

            migrationBuilder.AddColumn<Guid>(
                name: "BackupAssigneeId",
                schema: "Workflow",
                table: "WorkflowTask",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessRoleCode",
                schema: "Workflow",
                table: "WorkflowTask",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessRoleId",
                schema: "Workflow",
                table: "WorkflowTask",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BusinessRoleVersion",
                schema: "Workflow",
                table: "WorkflowTask",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CandidateUserIdsJson",
                schema: "Workflow",
                table: "WorkflowTask",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<Guid>(
                name: "OriginalAssigneeId",
                schema: "Workflow",
                table: "WorkflowTask",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransferReason",
                schema: "Workflow",
                table: "WorkflowTask",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TransferredAt",
                schema: "Workflow",
                table: "WorkflowTask",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TransferredBy",
                schema: "Workflow",
                table: "WorkflowTask",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowTransfer",
                schema: "Workflow",
                table: "WorkflowNodePolicy",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AssigneeStrategy",
                schema: "Workflow",
                table: "WorkflowNodePolicy",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "SpecificUser");

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessRoleId",
                schema: "Workflow",
                table: "WorkflowNodePolicy",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransferTargetStrategy",
                schema: "Workflow",
                table: "WorkflowNodePolicy",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActivatedAt",
                schema: "Identity",
                table: "User",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvitationStatus",
                schema: "Identity",
                table: "User",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<DateTime>(
                name: "InvitedAt",
                schema: "Identity",
                table: "User",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                schema: "Identity",
                table: "User",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "Identity",
                table: "Role",
                type: "datetime2(3)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "Identity",
                table: "Role",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "Identity",
                table: "Role",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystem",
                schema: "Identity",
                table: "Role",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RoleType",
                schema: "Identity",
                table: "Role",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Business");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "Identity",
                table: "Role",
                type: "datetime2(3)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<long>(
                name: "Version",
                schema: "Identity",
                table: "Role",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<string>(
                name: "HierarchyType",
                schema: "Identity",
                table: "Department",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Department");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentDepartmentId",
                schema: "Identity",
                table: "Department",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Path",
                schema: "Identity",
                table: "Department",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "/");

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                schema: "Identity",
                table: "Department",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                schema: "Identity",
                table: "Department",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.CreateTable(
                name: "AccessChangeRequest",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RiskReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ReviewedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ReviewComment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessChangeRequest", x => x.Id);
                    table.CheckConstraint("CK_AccessChangeRequest_PayloadJson", "ISJSON([PayloadJson]) = 1");
                    table.CheckConstraint("CK_AccessChangeRequest_Status", "[Status] IN ('Pending','Approved','Rejected','Applied','Cancelled')");
                    table.CheckConstraint("CK_AccessChangeRequest_Type", "[RequestType] IN ('RoleAssignment','RolePermissions')");
                    table.ForeignKey(
                        name: "FK_AccessChangeRequest_Organization",
                        column: x => x.OrganizationId,
                        principalSchema: "Identity",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessChangeRequest_RequestedBy",
                        column: x => x.RequestedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessChangeRequest_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessChangeRequest_TargetRole",
                        column: x => x.TargetRoleId,
                        principalSchema: "Identity",
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessChangeRequest_TargetUser",
                        column: x => x.TargetUserId,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NavigationUserPreference",
                schema: "Navigation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ItemKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsFavorite = table.Column<bool>(type: "bit", nullable: false),
                    IsHidden = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NavigationUserPreference", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NavigationUserPreference_User",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NavigationWorkspace",
                schema: "Navigation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkspaceKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    WorkspaceType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    RetiredBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NavigationWorkspace", x => x.Id);
                    table.CheckConstraint("CK_NavigationWorkspace_Status", "[Status] IN ('Draft','Published','Retired')");
                    table.CheckConstraint("CK_NavigationWorkspace_Type", "[WorkspaceType] IN ('System','OrganizationWorkspace','DepartmentWorkspace','BusinessView')");
                    table.ForeignKey(
                        name: "FK_NavigationWorkspace_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NavigationWorkspace_Department",
                        column: x => x.DepartmentId,
                        principalSchema: "Identity",
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NavigationWorkspace_Organization",
                        column: x => x.OrganizationId,
                        principalSchema: "Identity",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NavigationWorkspace_RetiredBy",
                        column: x => x.RetiredBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NavigationWorkspace_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserRoleAssignment",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ScopeReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AssignedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    RevokedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleAssignment", x => x.Id);
                    table.CheckConstraint("CK_UserRoleAssignment_ScopeReference", "LEN([ScopeReference]) > 0");
                    table.CheckConstraint("CK_UserRoleAssignment_ScopeType", "[ScopeType] IN ('organization','department')");
                    table.ForeignKey(
                        name: "FK_UserRoleAssignment_AssignedBy",
                        column: x => x.AssignedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoleAssignment_RevokedBy",
                        column: x => x.RevokedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoleAssignment_Role",
                        column: x => x.RoleId,
                        principalSchema: "Identity",
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoleAssignment_User",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NavigationVersion",
                schema: "Navigation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    ImpactAnalysisJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    PublishedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NavigationVersion", x => x.Id);
                    table.CheckConstraint("CK_NavigationVersion_ImpactAnalysis", "[ImpactAnalysisJson] IS NULL OR ISJSON([ImpactAnalysisJson]) = 1");
                    table.CheckConstraint("CK_NavigationVersion_Status", "[Status] IN ('Draft','Published','Retired')");
                    table.ForeignKey(
                        name: "FK_NavigationVersion_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NavigationVersion_PublishedBy",
                        column: x => x.PublishedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NavigationVersion_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NavigationVersion_Workspace",
                        column: x => x.WorkspaceId,
                        principalSchema: "Navigation",
                        principalTable: "NavigationWorkspace",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NavigationItem",
                schema: "Navigation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NavigationVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Route = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequiredResource = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RequiredAction = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NavigationItem", x => x.Id);
                    table.CheckConstraint("CK_NavigationItem_Route", "LEN([Route]) > 0");
                    table.CheckConstraint("CK_NavigationItem_Type", "[ItemType] IN ('System','Workspace','Task','Inbox','Workflow','BusinessObject','File','Project','Minutes','Report','Administration')");
                    table.ForeignKey(
                        name: "FK_NavigationItem_Version",
                        column: x => x.NavigationVersionId,
                        principalSchema: "Navigation",
                        principalTable: "NavigationVersion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTask_BackupAssigneeId",
                schema: "Workflow",
                table: "WorkflowTask",
                column: "BackupAssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTask_BusinessRole_Status",
                schema: "Workflow",
                table: "WorkflowTask",
                columns: new[] { "BusinessRoleId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTask_OriginalAssigneeId",
                schema: "Workflow",
                table: "WorkflowTask",
                column: "OriginalAssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTask_TransferredBy",
                schema: "Workflow",
                table: "WorkflowTask",
                column: "TransferredBy");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowNodePolicy_BusinessRoleId",
                schema: "Workflow",
                table: "WorkflowNodePolicy",
                column: "BusinessRoleId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkflowNodePolicy_AssigneeStrategy",
                schema: "Workflow",
                table: "WorkflowNodePolicy",
                sql: "[AssigneeStrategy] IN ('SpecificUser','BusinessRole','DepartmentManager','ProjectOwner','InitiatorManager','Agent')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkflowNodePolicy_TransferTargetStrategy",
                schema: "Workflow",
                table: "WorkflowNodePolicy",
                sql: "[TransferTargetStrategy] IS NULL OR [TransferTargetStrategy] IN ('BackupApprover','DepartmentManager')");

            migrationBuilder.CreateIndex(
                name: "IX_Role_Type_Active",
                schema: "Identity",
                table: "Role",
                columns: new[] { "RoleType", "IsActive" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Role_Type",
                schema: "Identity",
                table: "Role",
                sql: "[RoleType] IN ('System','Business','AgentService')");

            migrationBuilder.CreateIndex(
                name: "IX_Department_ParentDepartmentId",
                schema: "Identity",
                table: "Department",
                column: "ParentDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Department_Tree",
                schema: "Identity",
                table: "Department",
                columns: new[] { "OrganizationId", "ParentDepartmentId", "SortOrder" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Department_HierarchyType",
                schema: "Identity",
                table: "Department",
                sql: "[HierarchyType] IN ('Center','Department','Team')");

            migrationBuilder.CreateIndex(
                name: "IX_AccessChangeRequest_Org_Status_RequestedAt",
                schema: "Identity",
                table: "AccessChangeRequest",
                columns: new[] { "OrganizationId", "Status", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessChangeRequest_ReviewedBy",
                schema: "Identity",
                table: "AccessChangeRequest",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AccessChangeRequest_TargetRoleId",
                schema: "Identity",
                table: "AccessChangeRequest",
                column: "TargetRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessChangeRequest_TargetUserId",
                schema: "Identity",
                table: "AccessChangeRequest",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "UX_AccessChangeRequest_Requester_Idempotency",
                schema: "Identity",
                table: "AccessChangeRequest",
                columns: new[] { "RequestedBy", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NavigationItem_Version_Sort",
                schema: "Navigation",
                table: "NavigationItem",
                columns: new[] { "NavigationVersionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_NavigationItem_Version_Key",
                schema: "Navigation",
                table: "NavigationItem",
                columns: new[] { "NavigationVersionId", "ItemKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_NavigationUserPreference_User_Workspace_Item",
                schema: "Navigation",
                table: "NavigationUserPreference",
                columns: new[] { "UserId", "WorkspaceKey", "ItemKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NavigationVersion_CreatedBy",
                schema: "Navigation",
                table: "NavigationVersion",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationVersion_PublishedBy",
                schema: "Navigation",
                table: "NavigationVersion",
                column: "PublishedBy");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationVersion_UpdatedBy",
                schema: "Navigation",
                table: "NavigationVersion",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationVersion_Workspace_Status",
                schema: "Navigation",
                table: "NavigationVersion",
                columns: new[] { "WorkspaceId", "Status", "Revision" });

            migrationBuilder.CreateIndex(
                name: "UX_NavigationVersion_Workspace_Version",
                schema: "Navigation",
                table: "NavigationVersion",
                columns: new[] { "WorkspaceId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NavigationWorkspace_CreatedBy",
                schema: "Navigation",
                table: "NavigationWorkspace",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationWorkspace_DepartmentId",
                schema: "Navigation",
                table: "NavigationWorkspace",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationWorkspace_Org_Department_Status",
                schema: "Navigation",
                table: "NavigationWorkspace",
                columns: new[] { "OrganizationId", "DepartmentId", "Status", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_NavigationWorkspace_RetiredBy",
                schema: "Navigation",
                table: "NavigationWorkspace",
                column: "RetiredBy");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationWorkspace_UpdatedBy",
                schema: "Navigation",
                table: "NavigationWorkspace",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UX_NavigationWorkspace_Organization_Key",
                schema: "Navigation",
                table: "NavigationWorkspace",
                columns: new[] { "OrganizationId", "WorkspaceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignment_AssignedBy",
                schema: "Identity",
                table: "UserRoleAssignment",
                column: "AssignedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignment_RevokedBy",
                schema: "Identity",
                table: "UserRoleAssignment",
                column: "RevokedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignment_Role_Active",
                schema: "Identity",
                table: "UserRoleAssignment",
                columns: new[] { "RoleId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignment_User_Active",
                schema: "Identity",
                table: "UserRoleAssignment",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UX_UserRoleAssignment_User_Role_Scope",
                schema: "Identity",
                table: "UserRoleAssignment",
                columns: new[] { "UserId", "RoleId", "ScopeType", "ScopeReference" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Department_Parent",
                schema: "Identity",
                table: "Department",
                column: "ParentDepartmentId",
                principalSchema: "Identity",
                principalTable: "Department",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowNodePolicy_BusinessRole",
                schema: "Workflow",
                table: "WorkflowNodePolicy",
                column: "BusinessRoleId",
                principalSchema: "Identity",
                principalTable: "Role",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowTask_BackupAssignee",
                schema: "Workflow",
                table: "WorkflowTask",
                column: "BackupAssigneeId",
                principalSchema: "Identity",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowTask_BusinessRole",
                schema: "Workflow",
                table: "WorkflowTask",
                column: "BusinessRoleId",
                principalSchema: "Identity",
                principalTable: "Role",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowTask_OriginalAssignee",
                schema: "Workflow",
                table: "WorkflowTask",
                column: "OriginalAssigneeId",
                principalSchema: "Identity",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowTask_TransferredBy",
                schema: "Workflow",
                table: "WorkflowTask",
                column: "TransferredBy",
                principalSchema: "Identity",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
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
                """);

            migrationBuilder.Sql(
                """
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
                """);

            migrationBuilder.Sql(
                """
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
                """);

            migrationBuilder.Sql(
                """
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
                """);

            migrationBuilder.Sql(
                """
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
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Department_Parent",
                schema: "Identity",
                table: "Department");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowNodePolicy_BusinessRole",
                schema: "Workflow",
                table: "WorkflowNodePolicy");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowTask_BackupAssignee",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowTask_BusinessRole",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowTask_OriginalAssignee",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowTask_TransferredBy",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropTable(
                name: "AccessChangeRequest",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "NavigationItem",
                schema: "Navigation");

            migrationBuilder.DropTable(
                name: "NavigationUserPreference",
                schema: "Navigation");

            migrationBuilder.DropTable(
                name: "UserRoleAssignment",
                schema: "Identity");

            migrationBuilder.Sql(
                """
                DELETE FROM [Identity].[DataScope]
                WHERE UserId = '10000000-0000-0000-0000-000000000023';
                DELETE FROM [Identity].[UserRole]
                WHERE UserId = '10000000-0000-0000-0000-000000000023';
                DELETE FROM [Identity].[UserDepartmentMembership]
                WHERE UserId = '10000000-0000-0000-0000-000000000023';
                DELETE FROM [Identity].[User]
                WHERE UserName = N'demo.security';

                DELETE rolePermission
                FROM [Identity].[RolePermission] rolePermission
                JOIN [Identity].[Permission] permissionRow
                    ON permissionRow.Id = rolePermission.PermissionId
                WHERE permissionRow.Code IN (
                    N'identity-admin-read', N'identity-admin-manage',
                    N'role-read', N'role-create', N'role-update', N'role-manage',
                    N'permission-read', N'permission-assign',
                    N'navigation-read', N'navigation-manage', N'navigation-publish',
                    N'data-scope-update', N'workflow-role-read', N'workflow-role-manage',
                    N'workflow-task-update');

                DELETE rolePermission
                FROM [Identity].[RolePermission] rolePermission
                JOIN [Identity].[Role] roleRow ON roleRow.Id = rolePermission.RoleId
                JOIN [Identity].[Permission] permissionRow ON permissionRow.Id = rolePermission.PermissionId
                WHERE roleRow.Code = N'project-manager'
                  AND permissionRow.Code IN (
                    N'workflow-task-approve', N'workflow-task-return', N'workflow-task-terminate');

                DELETE FROM [Identity].[Permission]
                WHERE Code IN (
                    N'identity-admin-read', N'identity-admin-manage',
                    N'role-read', N'role-create', N'role-update', N'role-manage',
                    N'permission-read', N'permission-assign',
                    N'navigation-read', N'navigation-manage', N'navigation-publish',
                    N'data-scope-update', N'workflow-role-read', N'workflow-role-manage',
                    N'workflow-task-update');

                DELETE FROM [Identity].[Role]
                WHERE Code IN (
                    N'business-rd-engineer',
                    N'business-manager-reviewer',
                    N'business-quality-reviewer');
                """);

            migrationBuilder.DropTable(
                name: "NavigationVersion",
                schema: "Navigation");

            migrationBuilder.DropTable(
                name: "NavigationWorkspace",
                schema: "Navigation");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowTask_BackupAssigneeId",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowTask_BusinessRole_Status",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowTask_OriginalAssigneeId",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowTask_TransferredBy",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowNodePolicy_BusinessRoleId",
                schema: "Workflow",
                table: "WorkflowNodePolicy");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkflowNodePolicy_AssigneeStrategy",
                schema: "Workflow",
                table: "WorkflowNodePolicy");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkflowNodePolicy_TransferTargetStrategy",
                schema: "Workflow",
                table: "WorkflowNodePolicy");

            migrationBuilder.DropIndex(
                name: "IX_Role_Type_Active",
                schema: "Identity",
                table: "Role");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Role_Type",
                schema: "Identity",
                table: "Role");

            migrationBuilder.DropIndex(
                name: "IX_Department_ParentDepartmentId",
                schema: "Identity",
                table: "Department");

            migrationBuilder.DropIndex(
                name: "IX_Department_Tree",
                schema: "Identity",
                table: "Department");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Department_HierarchyType",
                schema: "Identity",
                table: "Department");

            migrationBuilder.DropColumn(
                name: "AssigneeSnapshotJson",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropColumn(
                name: "AssigneeStrategy",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropColumn(
                name: "BackupAssigneeId",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropColumn(
                name: "BusinessRoleCode",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropColumn(
                name: "BusinessRoleId",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropColumn(
                name: "BusinessRoleVersion",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropColumn(
                name: "CandidateUserIdsJson",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropColumn(
                name: "OriginalAssigneeId",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropColumn(
                name: "TransferReason",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropColumn(
                name: "TransferredAt",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropColumn(
                name: "TransferredBy",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropColumn(
                name: "AllowTransfer",
                schema: "Workflow",
                table: "WorkflowNodePolicy");

            migrationBuilder.DropColumn(
                name: "AssigneeStrategy",
                schema: "Workflow",
                table: "WorkflowNodePolicy");

            migrationBuilder.DropColumn(
                name: "BusinessRoleId",
                schema: "Workflow",
                table: "WorkflowNodePolicy");

            migrationBuilder.DropColumn(
                name: "TransferTargetStrategy",
                schema: "Workflow",
                table: "WorkflowNodePolicy");

            migrationBuilder.DropColumn(
                name: "ActivatedAt",
                schema: "Identity",
                table: "User");

            migrationBuilder.DropColumn(
                name: "InvitationStatus",
                schema: "Identity",
                table: "User");

            migrationBuilder.DropColumn(
                name: "InvitedAt",
                schema: "Identity",
                table: "User");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "Identity",
                table: "User");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "Identity",
                table: "Role");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "Identity",
                table: "Role");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "Identity",
                table: "Role");

            migrationBuilder.DropColumn(
                name: "IsSystem",
                schema: "Identity",
                table: "Role");

            migrationBuilder.DropColumn(
                name: "RoleType",
                schema: "Identity",
                table: "Role");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "Identity",
                table: "Role");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "Identity",
                table: "Role");

            migrationBuilder.DropColumn(
                name: "HierarchyType",
                schema: "Identity",
                table: "Department");

            migrationBuilder.DropColumn(
                name: "ParentDepartmentId",
                schema: "Identity",
                table: "Department");

            migrationBuilder.DropColumn(
                name: "Path",
                schema: "Identity",
                table: "Department");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                schema: "Identity",
                table: "Department");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "Identity",
                table: "Department");
        }
    }
}
