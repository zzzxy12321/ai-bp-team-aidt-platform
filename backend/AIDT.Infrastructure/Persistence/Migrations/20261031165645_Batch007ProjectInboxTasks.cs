using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIDT.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Batch007ProjectInboxTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; SET ANSI_PADDING ON; " +
                "SET ANSI_WARNINGS ON; SET CONCAT_NULL_YIELDS_NULL ON; SET NUMERIC_ROUNDABORT OFF;");

            migrationBuilder.EnsureSchema(
                name: "PMO");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                schema: "BusinessObject",
                table: "BusinessObject",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                schema: "BusinessObject",
                table: "BusinessObject",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EventInbox",
                schema: "PMO",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AttachmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RawText = table.Column<string>(type: "nvarchar(max)", maxLength: 2000000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SubmittedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnalysisRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EnvironmentCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    IsTestData = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventInbox", x => x.Id);
                    table.CheckConstraint("CK_EventInbox_Environment", "[EnvironmentCode] IN ('demo','test','prod')");
                    table.CheckConstraint("CK_EventInbox_Source", "[SourceType] IN ('ManualText','ManualUpload','MinutesSource','MinutesActionItem','WorkflowNotification','Attachment')");
                    table.CheckConstraint("CK_EventInbox_Status", "[Status] IN ('Received','AwaitingConfirmation','Confirmed','Rejected')");
                    table.ForeignKey(
                        name: "FK_EventInbox_AnalysisRun",
                        column: x => x.AnalysisRunId,
                        principalSchema: "Agent",
                        principalTable: "AgentRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventInbox_Attachment",
                        column: x => x.AttachmentId,
                        principalSchema: "Document",
                        principalTable: "Attachment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventInbox_Department",
                        column: x => x.DepartmentId,
                        principalSchema: "Identity",
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventInbox_Organization",
                        column: x => x.OrganizationId,
                        principalSchema: "Identity",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventInbox_SubmittedBy",
                        column: x => x.SubmittedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMember",
                schema: "PMO",
                columns: table => new
                {
                    BusinessObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectRole = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    IsLead = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    RemovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RemovedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMember", x => new { x.BusinessObjectId, x.UserId });
                    table.CheckConstraint("CK_ProjectMember_Role", "[ProjectRole] IN ('Lead','Manager','Contributor','Viewer')");
                    table.ForeignKey(
                        name: "FK_ProjectMember_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectMember_Project",
                        column: x => x.BusinessObjectId,
                        principalSchema: "BusinessObject",
                        principalTable: "BusinessObject",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectMember_RemovedBy",
                        column: x => x.RemovedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectMember_User",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMilestone",
                schema: "PMO",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlannedDate = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMilestone", x => x.Id);
                    table.CheckConstraint("CK_ProjectMilestone_Status", "[Status] IN ('Planned','InProgress','Completed','AtRisk','Cancelled')");
                    table.ForeignKey(
                        name: "FK_ProjectMilestone_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectMilestone_Owner",
                        column: x => x.OwnerUserId,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectMilestone_Project",
                        column: x => x.ProjectId,
                        principalSchema: "BusinessObject",
                        principalTable: "BusinessObject",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectProfile",
                schema: "PMO",
                columns: table => new
                {
                    BusinessObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlannedStartDate = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    PlannedEndDate = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    Objective = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CurrentStage = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ConfidentialityLevel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ProgressPercent = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectProfile", x => x.BusinessObjectId);
                    table.CheckConstraint("CK_ProjectProfile_Confidentiality", "[ConfidentialityLevel] IN ('Internal','Sensitive','Core')");
                    table.CheckConstraint("CK_ProjectProfile_Dates", "[PlannedEndDate] IS NULL OR [PlannedStartDate] IS NULL OR [PlannedEndDate] >= [PlannedStartDate]");
                    table.CheckConstraint("CK_ProjectProfile_Progress", "[ProgressPercent] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_ProjectProfile_BusinessObject",
                        column: x => x.BusinessObjectId,
                        principalSchema: "BusinessObject",
                        principalTable: "BusinessObject",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectProfile_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectProfile_Owner",
                        column: x => x.OwnerUserId,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectRelation",
                schema: "PMO",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EvidenceExcerpt = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectRelation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectRelation_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectRelation_Project",
                        column: x => x.ProjectId,
                        principalSchema: "BusinessObject",
                        principalTable: "BusinessObject",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventSuggestion",
                schema: "PMO",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventInboxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SuggestedProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SuggestedBusinessObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SuggestedAssigneeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SuggestedWorkflowVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SuggestedDueAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    RequiresHumanBoundary = table.Column<bool>(type: "bit", nullable: false),
                    BoundaryReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MissingInformation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EvidenceExcerpt = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventSuggestion", x => x.Id);
                    table.CheckConstraint("CK_EventSuggestion_Priority", "[Priority] IN ('P0','P1','P2')");
                    table.CheckConstraint("CK_EventSuggestion_Risk", "[RiskLevel] IN ('Low','Medium','High')");
                    table.CheckConstraint("CK_EventSuggestion_Status", "[Status] IN ('Suggested','HighRisk','Confirmed','Rejected')");
                    table.ForeignKey(
                        name: "FK_EventSuggestion_AgentRun",
                        column: x => x.AgentRunId,
                        principalSchema: "Agent",
                        principalTable: "AgentRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventSuggestion_Assignee",
                        column: x => x.SuggestedAssigneeId,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventSuggestion_BusinessObject",
                        column: x => x.SuggestedBusinessObjectId,
                        principalSchema: "BusinessObject",
                        principalTable: "BusinessObject",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventSuggestion_EventInbox",
                        column: x => x.EventInboxId,
                        principalSchema: "PMO",
                        principalTable: "EventInbox",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventSuggestion_Project",
                        column: x => x.SuggestedProjectId,
                        principalSchema: "BusinessObject",
                        principalTable: "BusinessObject",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventSuggestion_WorkflowVersion",
                        column: x => x.SuggestedWorkflowVersionId,
                        principalSchema: "Workflow",
                        principalTable: "WorkflowVersion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTask",
                schema: "PMO",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventInboxId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssigneeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    DueAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    BusinessObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkflowInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkflowTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EnvironmentCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    IsTestData = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTask", x => x.Id);
                    table.CheckConstraint("CK_ProjectTask_Environment", "[EnvironmentCode] IN ('demo','test','prod')");
                    table.CheckConstraint("CK_ProjectTask_Priority", "[Priority] IN ('P0','P1','P2')");
                    table.CheckConstraint("CK_ProjectTask_Status", "[Status] IN ('Pending','InProgress','Completed','Cancelled')");
                    table.ForeignKey(
                        name: "FK_ProjectTask_Assignee",
                        column: x => x.AssigneeId,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectTask_BusinessObject",
                        column: x => x.BusinessObjectId,
                        principalSchema: "BusinessObject",
                        principalTable: "BusinessObject",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectTask_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectTask_Department",
                        column: x => x.DepartmentId,
                        principalSchema: "Identity",
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectTask_EventInbox",
                        column: x => x.EventInboxId,
                        principalSchema: "PMO",
                        principalTable: "EventInbox",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectTask_Project",
                        column: x => x.ProjectId,
                        principalSchema: "BusinessObject",
                        principalTable: "BusinessObject",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectTask_WorkflowInstance",
                        column: x => x.WorkflowInstanceId,
                        principalSchema: "Workflow",
                        principalTable: "WorkflowInstance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectTask_WorkflowTask",
                        column: x => x.WorkflowTaskId,
                        principalSchema: "Workflow",
                        principalTable: "WorkflowTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessObject_CreatedByUserId",
                schema: "BusinessObject",
                table: "BusinessObject",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EventInbox_AnalysisRunId",
                schema: "PMO",
                table: "EventInbox",
                column: "AnalysisRunId");

            migrationBuilder.CreateIndex(
                name: "IX_EventInbox_AttachmentId",
                schema: "PMO",
                table: "EventInbox",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EventInbox_Department_Environment_Status",
                schema: "PMO",
                table: "EventInbox",
                columns: new[] { "DepartmentId", "EnvironmentCode", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EventInbox_OrganizationId",
                schema: "PMO",
                table: "EventInbox",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_EventInbox_Submitter_CreatedAt",
                schema: "PMO",
                table: "EventInbox",
                columns: new[] { "SubmittedBy", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EventSuggestion_AgentRunId",
                schema: "PMO",
                table: "EventSuggestion",
                column: "AgentRunId");

            migrationBuilder.CreateIndex(
                name: "IX_EventSuggestion_SuggestedAssigneeId",
                schema: "PMO",
                table: "EventSuggestion",
                column: "SuggestedAssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_EventSuggestion_SuggestedBusinessObjectId",
                schema: "PMO",
                table: "EventSuggestion",
                column: "SuggestedBusinessObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_EventSuggestion_SuggestedProjectId",
                schema: "PMO",
                table: "EventSuggestion",
                column: "SuggestedProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_EventSuggestion_SuggestedWorkflowVersionId",
                schema: "PMO",
                table: "EventSuggestion",
                column: "SuggestedWorkflowVersionId");

            migrationBuilder.CreateIndex(
                name: "UX_EventSuggestion_EventInbox",
                schema: "PMO",
                table: "EventSuggestion",
                column: "EventInboxId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMember_CreatedBy",
                schema: "PMO",
                table: "ProjectMember",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMember_RemovedBy",
                schema: "PMO",
                table: "ProjectMember",
                column: "RemovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMember_User_Active_Project",
                schema: "PMO",
                table: "ProjectMember",
                columns: new[] { "UserId", "IsActive", "BusinessObjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMilestone_CreatedBy",
                schema: "PMO",
                table: "ProjectMilestone",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMilestone_Owner_Status",
                schema: "PMO",
                table: "ProjectMilestone",
                columns: new[] { "OwnerUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMilestone_Project_Date",
                schema: "PMO",
                table: "ProjectMilestone",
                columns: new[] { "ProjectId", "PlannedDate", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectProfile_CreatedBy",
                schema: "PMO",
                table: "ProjectProfile",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectProfile_Owner_UpdatedAt",
                schema: "PMO",
                table: "ProjectProfile",
                columns: new[] { "OwnerUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectRelation_CreatedBy",
                schema: "PMO",
                table: "ProjectRelation",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectRelation_Entity",
                schema: "PMO",
                table: "ProjectRelation",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "UX_ProjectRelation_Project_Entity_Code",
                schema: "PMO",
                table: "ProjectRelation",
                columns: new[] { "ProjectId", "EntityType", "EntityId", "RelationCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTask_Assignee_Status_Due",
                schema: "PMO",
                table: "ProjectTask",
                columns: new[] { "AssigneeId", "Status", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTask_BusinessObjectId",
                schema: "PMO",
                table: "ProjectTask",
                column: "BusinessObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTask_CreatedBy",
                schema: "PMO",
                table: "ProjectTask",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTask_DepartmentId",
                schema: "PMO",
                table: "ProjectTask",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTask_Project_Status_Due",
                schema: "PMO",
                table: "ProjectTask",
                columns: new[] { "ProjectId", "Status", "DueAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTask_WorkflowInstanceId",
                schema: "PMO",
                table: "ProjectTask",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTask_WorkflowTaskId",
                schema: "PMO",
                table: "ProjectTask",
                column: "WorkflowTaskId");

            migrationBuilder.CreateIndex(
                name: "UX_ProjectTask_EventInbox",
                schema: "PMO",
                table: "ProjectTask",
                column: "EventInboxId",
                unique: true,
                filter: "[EventInboxId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessObject_CreatedByUser",
                schema: "BusinessObject",
                table: "BusinessObject",
                column: "CreatedByUserId",
                principalSchema: "Identity",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
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
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; SET ANSI_PADDING ON; " +
                "SET ANSI_WARNINGS ON; SET CONCAT_NULL_YIELDS_NULL ON; SET NUMERIC_ROUNDABORT OFF;");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessObject_CreatedByUser",
                schema: "BusinessObject",
                table: "BusinessObject");

            migrationBuilder.DropTable(
                name: "EventSuggestion",
                schema: "PMO");

            migrationBuilder.DropTable(
                name: "ProjectMember",
                schema: "PMO");

            migrationBuilder.DropTable(
                name: "ProjectMilestone",
                schema: "PMO");

            migrationBuilder.DropTable(
                name: "ProjectProfile",
                schema: "PMO");

            migrationBuilder.DropTable(
                name: "ProjectRelation",
                schema: "PMO");

            migrationBuilder.DropTable(
                name: "ProjectTask",
                schema: "PMO");

            migrationBuilder.DropTable(
                name: "EventInbox",
                schema: "PMO");

            migrationBuilder.Sql(
                """
                DECLARE @ProjectTypeId uniqueidentifier =
                    (SELECT Id FROM [Metadata].[ObjectType] WHERE Code = N'PROJECT');
                IF @ProjectTypeId IS NOT NULL
                BEGIN
                    DELETE candidate
                    FROM [Workflow].[WorkflowTaskCandidate] candidate
                    JOIN [Workflow].[WorkflowTask] task ON task.Id = candidate.WorkflowTaskId
                    JOIN [Workflow].[WorkflowInstance] instance ON instance.Id = task.WorkflowInstanceId
                    JOIN [BusinessObject].[BusinessObject] businessObject ON businessObject.Id = instance.BusinessObjectId
                    WHERE businessObject.ObjectTypeId = @ProjectTypeId;

                    DELETE notification
                    FROM [Workflow].[WorkflowNotification] notification
                    JOIN [Workflow].[WorkflowTask] task ON task.Id = notification.WorkflowTaskId
                    JOIN [Workflow].[WorkflowInstance] instance ON instance.Id = task.WorkflowInstanceId
                    JOIN [BusinessObject].[BusinessObject] businessObject ON businessObject.Id = instance.BusinessObjectId
                    WHERE businessObject.ObjectTypeId = @ProjectTypeId;

                    DELETE taskAction
                    FROM [Workflow].[WorkflowTaskAction] taskAction
                    JOIN [Workflow].[WorkflowTask] task ON task.Id = taskAction.WorkflowTaskId
                    JOIN [Workflow].[WorkflowInstance] instance ON instance.Id = task.WorkflowInstanceId
                    JOIN [BusinessObject].[BusinessObject] businessObject ON businessObject.Id = instance.BusinessObjectId
                    WHERE businessObject.ObjectTypeId = @ProjectTypeId;

                    DELETE task
                    FROM [Workflow].[WorkflowTask] task
                    JOIN [Workflow].[WorkflowInstance] instance ON instance.Id = task.WorkflowInstanceId
                    JOIN [BusinessObject].[BusinessObject] businessObject ON businessObject.Id = instance.BusinessObjectId
                    WHERE businessObject.ObjectTypeId = @ProjectTypeId;

                    DELETE instance
                    FROM [Workflow].[WorkflowInstance] instance
                    JOIN [BusinessObject].[BusinessObject] businessObject ON businessObject.Id = instance.BusinessObjectId
                    WHERE businessObject.ObjectTypeId = @ProjectTypeId;

                    UPDATE attachment
                    SET BusinessObjectId = NULL
                    FROM [Document].[Attachment] attachment
                    JOIN [BusinessObject].[BusinessObject] businessObject ON businessObject.Id = attachment.BusinessObjectId
                    WHERE businessObject.ObjectTypeId = @ProjectTypeId;

                    DELETE relation
                    FROM [BusinessObject].[BusinessObjectRelation] relation
                    WHERE relation.SourceObjectId IN (
                              SELECT Id FROM [BusinessObject].[BusinessObject] WHERE ObjectTypeId = @ProjectTypeId)
                       OR relation.TargetObjectId IN (
                              SELECT Id FROM [BusinessObject].[BusinessObject] WHERE ObjectTypeId = @ProjectTypeId);

                    DELETE versionRow
                    FROM [BusinessObject].[BusinessObjectVersion] versionRow
                    JOIN [BusinessObject].[BusinessObject] businessObject ON businessObject.Id = versionRow.BusinessObjectId
                    WHERE businessObject.ObjectTypeId = @ProjectTypeId;

                    DELETE FROM [BusinessObject].[BusinessObject] WHERE ObjectTypeId = @ProjectTypeId;
                    DELETE FROM [Metadata].[FieldDefinition] WHERE ObjectTypeId = @ProjectTypeId;
                    DELETE FROM [Metadata].[ObjectType] WHERE Id = @ProjectTypeId;
                END;

                DELETE rolePermission
                FROM [Identity].[RolePermission] rolePermission
                LEFT JOIN [Identity].[Role] roleRow ON roleRow.Id = rolePermission.RoleId
                LEFT JOIN [Identity].[Permission] permissionRow ON permissionRow.Id = rolePermission.PermissionId
                WHERE roleRow.Code = N'project-manager'
                   OR permissionRow.Code IN (
                    N'project-read', N'project-create', N'project-update',
                    N'project-member-read', N'project-member-manage',
                    N'milestone-read', N'milestone-manage',
                    N'event-inbox-read', N'event-inbox-create', N'event-inbox-update', N'event-inbox-confirm',
                    N'project-task-read', N'project-task-create', N'project-task-update',
                    N'project-audit-read');

                DELETE FROM [Identity].[DataScope] WHERE UserId = '10000000-0000-0000-0000-000000000021';
                DELETE FROM [Identity].[UserDepartmentMembership] WHERE UserId = '10000000-0000-0000-0000-000000000021';
                DELETE FROM [Identity].[UserRole] WHERE UserId = '10000000-0000-0000-0000-000000000021';
                DELETE FROM [Agent].[AgentProfile] WHERE Name = N'项目事件建议数字员工';
                DELETE FROM [Identity].[UserRole] WHERE UserId = '10000000-0000-0000-0000-000000000022';
                DELETE FROM [Identity].[User] WHERE UserName = N'agent.project';
                DELETE FROM [Identity].[User] WHERE UserName = N'demo.pm';
                DELETE FROM [Identity].[Permission]
                WHERE Code IN (
                    N'project-read', N'project-create', N'project-update',
                    N'project-member-read', N'project-member-manage',
                    N'milestone-read', N'milestone-manage',
                    N'event-inbox-read', N'event-inbox-create', N'event-inbox-update', N'event-inbox-confirm',
                    N'project-task-read', N'project-task-create', N'project-task-update',
                    N'project-audit-read');
                DELETE FROM [Identity].[Role] WHERE Code = N'project-manager';
                DELETE FROM [Identity].[Role] WHERE Code = N'project-event-agent';
                IF SCHEMA_ID(N'PMO') IS NOT NULL
                    EXEC(N'DROP SCHEMA [PMO]');
                """);

            migrationBuilder.DropIndex(
                name: "IX_BusinessObject_CreatedByUserId",
                schema: "BusinessObject",
                table: "BusinessObject");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                schema: "BusinessObject",
                table: "BusinessObject");

            migrationBuilder.DropColumn(
                name: "Title",
                schema: "BusinessObject",
                table: "BusinessObject");

            migrationBuilder.Sql(
                """
                UPDATE [BusinessObject].[BusinessObject]
                SET [Status] = N'Archived'
                WHERE [Status] IN (N'Blocked', N'AwaitingAcceptance', N'Completed', N'Terminated');
                """);
        }
    }
}
