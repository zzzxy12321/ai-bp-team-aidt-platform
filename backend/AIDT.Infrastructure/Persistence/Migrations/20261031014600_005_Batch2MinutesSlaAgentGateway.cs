using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIDT.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Batch2MinutesSlaAgentGateway : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Minutes");

            migrationBuilder.AddColumn<Guid>(
                name: "AgentRunId",
                schema: "Workflow",
                table: "WorkflowTask",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionMode",
                schema: "Workflow",
                table: "WorkflowTask",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DueAt",
                schema: "Workflow",
                table: "WorkflowTask",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EscalatedAt",
                schema: "Workflow",
                table: "WorkflowTask",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "NodePolicyId",
                schema: "Workflow",
                table: "WorkflowTask",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderSentAt",
                schema: "Workflow",
                table: "WorkflowTask",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SlaStatus",
                schema: "Workflow",
                table: "WorkflowTask",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessObjectId",
                schema: "Agent",
                table: "AgentRun",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                schema: "Agent",
                table: "AgentRun",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "EnvironmentCode",
                schema: "Agent",
                table: "AgentRun",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "ExternalModelCalled",
                schema: "Agent",
                table: "AgentRun",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "GatewayCallId",
                schema: "Agent",
                table: "AgentRun",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GatewayMode",
                schema: "Agent",
                table: "AgentRun",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTestData",
                schema: "Agent",
                table: "AgentRun",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "MinutesSourceId",
                schema: "Agent",
                table: "AgentRun",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                schema: "Agent",
                table: "AgentRun",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "SubjectType",
                schema: "Agent",
                table: "AgentRun",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkflowInstanceId",
                schema: "Agent",
                table: "AgentRun",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkflowTaskId",
                schema: "Agent",
                table: "AgentRun",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceUserId",
                schema: "Agent",
                table: "AgentProfile",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM [Identity].[User] WHERE Id='10000000-0000-0000-0000-000000000019')
                    INSERT INTO [Identity].[User]
                        (Id, OrganizationId, UserName, DisplayName, IsServiceIdentity, IsActive, CreatedAt)
                    VALUES
                        ('10000000-0000-0000-0000-000000000019',
                         '10000000-0000-0000-0000-000000000001',
                         N'agent.demo', N'业务协同数字员工', 1, 1, SYSUTCDATETIME());

                IF NOT EXISTS (SELECT 1 FROM [Identity].[User] WHERE Id='10000000-0000-0000-0000-000000000020')
                    INSERT INTO [Identity].[User]
                        (Id, OrganizationId, UserName, DisplayName, IsServiceIdentity, IsActive, CreatedAt)
                    VALUES
                        ('10000000-0000-0000-0000-000000000020',
                         '10000000-0000-0000-0000-000000000001',
                         N'agent.proxy', N'流程代理数字员工', 1, 1, SYSUTCDATETIME());

                EXEC(N'UPDATE [Agent].[AgentProfile]
                       SET [ServiceUserId]=''10000000-0000-0000-0000-000000000019''
                       WHERE [ServiceUserId]=''00000000-0000-0000-0000-000000000000'';');

                IF NOT EXISTS (SELECT 1 FROM [Agent].[AgentProfile] WHERE Name=N'流程代理数字员工')
                    EXEC(N'INSERT INTO [Agent].[AgentProfile]
                        (Id, ServiceUserId, Name, AutonomyLevel, AmountLimit, RiskLimit, AllowedToolsJson, IsActive)
                    VALUES
                        (''60000000-0000-0000-0000-000000000002'',
                         ''10000000-0000-0000-0000-000000000020'',
                         N''流程代理数字员工'', N''A2'', 0, N''Low'',
                         N''["workflow.read","workflow.assist","workflow.proxy-low-risk"]'', 1);');

                EXEC(N'UPDATE runRow
                SET OrganizationId = userRow.OrganizationId,
                    DepartmentId = COALESCE(
                        (SELECT TOP (1) membership.DepartmentId
                         FROM [Identity].[UserDepartmentMembership] membership
                         WHERE membership.UserId=runRow.RequestedBy AND membership.IsActive=1
                         ORDER BY membership.IsPrimary DESC, membership.JoinedAt),
                        (SELECT TOP (1) dept.Id FROM [Identity].[Department] dept
                         WHERE dept.OrganizationId=userRow.OrganizationId AND dept.IsActive=1
                         ORDER BY dept.Code)),
                    EnvironmentCode=''demo'',
                    IsTestData=1,
                    SubjectType=COALESCE(SubjectType, ''Legacy'')
                FROM [Agent].[AgentRun] runRow
                JOIN [Identity].[User] userRow ON userRow.Id=runRow.RequestedBy;');

                EXEC(N'UPDATE [Workflow].[WorkflowTask]
                       SET DecisionMode=''Human'', SlaStatus=''None''
                       WHERE DecisionMode=N'''' OR SlaStatus=N'''';');
                """);

            migrationBuilder.CreateTable(
                name: "AgentGatewayCall",
                schema: "Agent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnvironmentCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    GatewayCallId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ExternalModelCalled = table.Column<bool>(type: "bit", nullable: false),
                    InputDigest = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OutputDigest = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DurationMs = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentGatewayCall", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentGatewayCall_Department",
                        column: x => x.DepartmentId,
                        principalSchema: "Identity",
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgentGatewayCall_Organization",
                        column: x => x.OrganizationId,
                        principalSchema: "Identity",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgentGatewayCall_Run",
                        column: x => x.AgentRunId,
                        principalSchema: "Agent",
                        principalTable: "AgentRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MinutesSource",
                schema: "Minutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttachmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MeetingAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Transcript = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EnvironmentCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    IsTestData = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AnalysisVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AnalysisRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MinutesSource", x => x.Id);
                    table.CheckConstraint("CK_MinutesSource_Environment", "[EnvironmentCode] IN ('demo','test','prod')");
                    table.CheckConstraint("CK_MinutesSource_Type", "[SourceType] IN ('ManualText','ManualUpload')");
                    table.ForeignKey(
                        name: "FK_MinutesSource_AnalysisRun",
                        column: x => x.AnalysisRunId,
                        principalSchema: "Agent",
                        principalTable: "AgentRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MinutesSource_Attachment",
                        column: x => x.AttachmentId,
                        principalSchema: "Document",
                        principalTable: "Attachment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MinutesSource_Department",
                        column: x => x.DepartmentId,
                        principalSchema: "Identity",
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MinutesSource_Organization",
                        column: x => x.OrganizationId,
                        principalSchema: "Identity",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowNodePolicy",
                schema: "Workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    ApproverUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BackupApproverUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SlaMinutes = table.Column<int>(type: "int", nullable: true),
                    ReminderMinutes = table.Column<int>(type: "int", nullable: true),
                    EscalationMinutes = table.Column<int>(type: "int", nullable: true),
                    TimeoutStrategy = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AllowAgentAssist = table.Column<bool>(type: "bit", nullable: false),
                    AllowAgentProxy = table.Column<bool>(type: "bit", nullable: false),
                    AgentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AgentMaxRisk = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    AgentMaxAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AgentMaxAutonomy = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    PermanentHumanBoundary = table.Column<bool>(type: "bit", nullable: false),
                    RequiredAttachmentCount = table.Column<int>(type: "int", nullable: false),
                    PrecheckItems = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PublishedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowNodePolicy", x => x.Id);
                    table.CheckConstraint("CK_WorkflowNodePolicy_Escalation", "[EscalationMinutes] IS NULL OR [EscalationMinutes] >= 0");
                    table.CheckConstraint("CK_WorkflowNodePolicy_Reminder", "[ReminderMinutes] IS NULL OR [ReminderMinutes] >= 0");
                    table.CheckConstraint("CK_WorkflowNodePolicy_Risk", "[AgentMaxRisk] IN ('Low','Medium','High')");
                    table.CheckConstraint("CK_WorkflowNodePolicy_Sla", "[SlaMinutes] IS NULL OR [SlaMinutes] > 0");
                    table.CheckConstraint("CK_WorkflowNodePolicy_Status", "[Status] IN ('Draft','Published','Retired')");
                    table.ForeignKey(
                        name: "FK_WorkflowNodePolicy_AgentProfile",
                        column: x => x.AgentProfileId,
                        principalSchema: "Agent",
                        principalTable: "AgentProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowNodePolicy_Approver",
                        column: x => x.ApproverUserId,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowNodePolicy_BackupApprover",
                        column: x => x.BackupApproverUserId,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowNodePolicy_PublishedBy",
                        column: x => x.PublishedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowNodePolicy_WorkflowVersion",
                        column: x => x.WorkflowVersionId,
                        principalSchema: "Workflow",
                        principalTable: "WorkflowVersion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowNotification",
                schema: "Workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowNotification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowNotification_Department",
                        column: x => x.DepartmentId,
                        principalSchema: "Identity",
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowNotification_Organization",
                        column: x => x.OrganizationId,
                        principalSchema: "Identity",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowNotification_Recipient",
                        column: x => x.RecipientId,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowNotification_Task",
                        column: x => x.WorkflowTaskId,
                        principalSchema: "Workflow",
                        principalTable: "WorkflowTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MinutesActionItem",
                schema: "Minutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MinutesSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Speaker = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SourceTimestamp = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SourceExcerpt = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OriginalContent = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SuggestedAssigneeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssigneeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SuggestedDueAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    DueAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    SuggestedObjectTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ObjectTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SuggestedWorkflowVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkflowVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MissingInformation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    AnalysisVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ConfirmedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    MinutesTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MinutesActionItem", x => x.Id);
                    table.CheckConstraint("CK_MinutesActionItem_Confidence", "[Confidence] >= 0 AND [Confidence] <= 1");
                    table.CheckConstraint("CK_MinutesActionItem_Status", "[Status] IN ('Draft','Confirmed')");
                    table.ForeignKey(
                        name: "FK_MinutesActionItem_Assignee",
                        column: x => x.AssigneeId,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MinutesActionItem_ObjectType",
                        column: x => x.ObjectTypeId,
                        principalSchema: "Metadata",
                        principalTable: "ObjectType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MinutesActionItem_Source",
                        column: x => x.MinutesSourceId,
                        principalSchema: "Minutes",
                        principalTable: "MinutesSource",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MinutesActionItem_WorkflowVersion",
                        column: x => x.WorkflowVersionId,
                        principalSchema: "Workflow",
                        principalTable: "WorkflowVersion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MinutesTask",
                schema: "Minutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MinutesSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttachmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssigneeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DueAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    BusinessObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkflowInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EnvironmentCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    IsTestData = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MinutesTask", x => x.Id);
                    table.CheckConstraint("CK_MinutesTask_Environment", "[EnvironmentCode] IN ('demo','test','prod')");
                    table.CheckConstraint("CK_MinutesTask_Status", "[Status] IN ('Pending','Completed','Cancelled')");
                    table.ForeignKey(
                        name: "FK_MinutesTask_ActionItem",
                        column: x => x.ActionItemId,
                        principalSchema: "Minutes",
                        principalTable: "MinutesActionItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MinutesTask_Assignee",
                        column: x => x.AssigneeId,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MinutesTask_Attachment",
                        column: x => x.AttachmentId,
                        principalSchema: "Document",
                        principalTable: "Attachment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MinutesTask_BusinessObject",
                        column: x => x.BusinessObjectId,
                        principalSchema: "BusinessObject",
                        principalTable: "BusinessObject",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MinutesTask_Source",
                        column: x => x.MinutesSourceId,
                        principalSchema: "Minutes",
                        principalTable: "MinutesSource",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MinutesTask_WorkflowInstance",
                        column: x => x.WorkflowInstanceId,
                        principalSchema: "Workflow",
                        principalTable: "WorkflowInstance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO [Identity].[Permission] (Id, Code, Resource, Action)
                SELECT permissionSeed.Id, permissionSeed.Code, permissionSeed.Resource, permissionSeed.Action
                FROM (VALUES
                    (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000028'), N'minutes-read', N'minutes', N'Read'),
                    (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000029'), N'minutes-create', N'minutes', N'Create'),
                    (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000030'), N'minutes-update', N'minutes', N'Update'),
                    (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000031'), N'minutes-approve', N'minutes', N'Approve'),
                    (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000032'), N'workflow-policy-read', N'workflow-policy', N'Read'),
                    (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000033'), N'workflow-policy-update', N'workflow-policy', N'Update'),
                    (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000034'), N'workflow-sla-update', N'workflow-sla', N'Update'),
                    (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000035'), N'agent-run-update', N'agent-run', N'Update')
                ) permissionSeed(Id, Code, Resource, Action)
                WHERE NOT EXISTS (
                    SELECT 1 FROM [Identity].[Permission] existing WHERE existing.Code=permissionSeed.Code
                );

                INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
                SELECT roleRow.Id, permissionRow.Id
                FROM [Identity].[Role] roleRow
                CROSS JOIN [Identity].[Permission] permissionRow
                WHERE roleRow.Code IN
                    (N'platform-admin', N'rd-engineer', N'quality-engineer', N'supply-specialist',
                     N'finance-approver', N'production-team')
                  AND permissionRow.Code IN
                    (N'minutes-read', N'minutes-create', N'minutes-update', N'minutes-approve',
                     N'workflow-policy-read', N'agent-run-update')
                  AND NOT EXISTS (
                      SELECT 1 FROM [Identity].[RolePermission] existing
                      WHERE existing.RoleId=roleRow.Id AND existing.PermissionId=permissionRow.Id
                  );

                INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
                SELECT roleRow.Id, permissionRow.Id
                FROM [Identity].[Role] roleRow
                CROSS JOIN [Identity].[Permission] permissionRow
                WHERE roleRow.Code=N'platform-admin'
                  AND permissionRow.Code IN (N'workflow-policy-update', N'workflow-sla-update')
                  AND NOT EXISTS (
                      SELECT 1 FROM [Identity].[RolePermission] existing
                      WHERE existing.RoleId=roleRow.Id AND existing.PermissionId=permissionRow.Id
                  );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTask_Sla",
                schema: "Workflow",
                table: "WorkflowTask",
                columns: new[] { "Status", "DueAt", "SlaStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentRun_DepartmentId",
                schema: "Agent",
                table: "AgentRun",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRun_OrganizationId",
                schema: "Agent",
                table: "AgentRun",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "UX_AgentProfile_ServiceUser",
                schema: "Agent",
                table: "AgentProfile",
                column: "ServiceUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentGatewayCall_DepartmentId",
                schema: "Agent",
                table: "AgentGatewayCall",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentGatewayCall_OrganizationId",
                schema: "Agent",
                table: "AgentGatewayCall",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentGatewayCall_Run_CreatedAt",
                schema: "Agent",
                table: "AgentGatewayCall",
                columns: new[] { "AgentRunId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MinutesActionItem_AssigneeId",
                schema: "Minutes",
                table: "MinutesActionItem",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_MinutesActionItem_ObjectTypeId",
                schema: "Minutes",
                table: "MinutesActionItem",
                column: "ObjectTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MinutesActionItem_Source_Status",
                schema: "Minutes",
                table: "MinutesActionItem",
                columns: new[] { "MinutesSourceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MinutesActionItem_WorkflowVersionId",
                schema: "Minutes",
                table: "MinutesActionItem",
                column: "WorkflowVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_MinutesSource_AnalysisRunId",
                schema: "Minutes",
                table: "MinutesSource",
                column: "AnalysisRunId");

            migrationBuilder.CreateIndex(
                name: "IX_MinutesSource_AttachmentId",
                schema: "Minutes",
                table: "MinutesSource",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MinutesSource_Department_Environment",
                schema: "Minutes",
                table: "MinutesSource",
                columns: new[] { "DepartmentId", "EnvironmentCode", "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_MinutesSource_OrganizationId",
                schema: "Minutes",
                table: "MinutesSource",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_MinutesTask_Assignee_Status_Due",
                schema: "Minutes",
                table: "MinutesTask",
                columns: new[] { "AssigneeId", "Status", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MinutesTask_AttachmentId",
                schema: "Minutes",
                table: "MinutesTask",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MinutesTask_BusinessObjectId",
                schema: "Minutes",
                table: "MinutesTask",
                column: "BusinessObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_MinutesTask_MinutesSourceId",
                schema: "Minutes",
                table: "MinutesTask",
                column: "MinutesSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_MinutesTask_WorkflowInstanceId",
                schema: "Minutes",
                table: "MinutesTask",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "UX_MinutesTask_ActionItem",
                schema: "Minutes",
                table: "MinutesTask",
                column: "ActionItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowNodePolicy_AgentProfileId",
                schema: "Workflow",
                table: "WorkflowNodePolicy",
                column: "AgentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowNodePolicy_ApproverUserId",
                schema: "Workflow",
                table: "WorkflowNodePolicy",
                column: "ApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowNodePolicy_BackupApproverUserId",
                schema: "Workflow",
                table: "WorkflowNodePolicy",
                column: "BackupApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowNodePolicy_PublishedBy",
                schema: "Workflow",
                table: "WorkflowNodePolicy",
                column: "PublishedBy");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowNodePolicy_Version_Node_Status",
                schema: "Workflow",
                table: "WorkflowNodePolicy",
                columns: new[] { "WorkflowVersionId", "NodeCode", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_WorkflowNodePolicy_Version_Node_VersionNumber",
                schema: "Workflow",
                table: "WorkflowNodePolicy",
                columns: new[] { "WorkflowVersionId", "NodeCode", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowNotification_DepartmentId",
                schema: "Workflow",
                table: "WorkflowNotification",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowNotification_OrganizationId",
                schema: "Workflow",
                table: "WorkflowNotification",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowNotification_Recipient",
                schema: "Workflow",
                table: "WorkflowNotification",
                columns: new[] { "RecipientId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowNotification_WorkflowTaskId",
                schema: "Workflow",
                table: "WorkflowNotification",
                column: "WorkflowTaskId");

            migrationBuilder.CreateIndex(
                name: "UX_WorkflowNotification_Idempotency",
                schema: "Workflow",
                table: "WorkflowNotification",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentProfile_ServiceUser",
                schema: "Agent",
                table: "AgentProfile",
                column: "ServiceUserId",
                principalSchema: "Identity",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentRun_Department",
                schema: "Agent",
                table: "AgentRun",
                column: "DepartmentId",
                principalSchema: "Identity",
                principalTable: "Department",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentRun_Organization",
                schema: "Agent",
                table: "AgentRun",
                column: "OrganizationId",
                principalSchema: "Identity",
                principalTable: "Organization",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgentProfile_ServiceUser",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentRun_Department",
                schema: "Agent",
                table: "AgentRun");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentRun_Organization",
                schema: "Agent",
                table: "AgentRun");

            migrationBuilder.DropTable(
                name: "AgentGatewayCall",
                schema: "Agent");

            migrationBuilder.DropTable(
                name: "MinutesTask",
                schema: "Minutes");

            migrationBuilder.DropTable(
                name: "WorkflowNodePolicy",
                schema: "Workflow");

            migrationBuilder.Sql(
                """
                DELETE FROM [Agent].[AgentProfile]
                WHERE Id='60000000-0000-0000-0000-000000000002';

                DELETE FROM [Identity].[User]
                WHERE Id='10000000-0000-0000-0000-000000000020'
                  AND UserName=N'agent.proxy';

                DELETE rolePermission
                FROM [Identity].[RolePermission] rolePermission
                JOIN [Identity].[Permission] permissionRow
                    ON permissionRow.Id=rolePermission.PermissionId
                WHERE permissionRow.Code IN
                    (N'minutes-read', N'minutes-create', N'minutes-update', N'minutes-approve',
                     N'workflow-policy-read', N'workflow-policy-update',
                     N'workflow-sla-update', N'agent-run-update');

                DELETE FROM [Identity].[Permission]
                WHERE Code IN
                    (N'minutes-read', N'minutes-create', N'minutes-update', N'minutes-approve',
                     N'workflow-policy-read', N'workflow-policy-update',
                     N'workflow-sla-update', N'agent-run-update');
                """);

            migrationBuilder.DropTable(
                name: "WorkflowNotification",
                schema: "Workflow");

            migrationBuilder.DropTable(
                name: "MinutesActionItem",
                schema: "Minutes");

            migrationBuilder.DropTable(
                name: "MinutesSource",
                schema: "Minutes");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowTask_Sla",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropIndex(
                name: "IX_AgentRun_DepartmentId",
                schema: "Agent",
                table: "AgentRun");

            migrationBuilder.DropIndex(
                name: "IX_AgentRun_OrganizationId",
                schema: "Agent",
                table: "AgentRun");

            migrationBuilder.DropIndex(
                name: "UX_AgentProfile_ServiceUser",
                schema: "Agent",
                table: "AgentProfile");

            migrationBuilder.DropColumn(
                name: "AgentRunId",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropColumn(
                name: "DecisionMode",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropColumn(
                name: "DueAt",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropColumn(
                name: "EscalatedAt",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropColumn(
                name: "NodePolicyId",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropColumn(
                name: "ReminderSentAt",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropColumn(
                name: "SlaStatus",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropColumn(
                name: "BusinessObjectId",
                schema: "Agent",
                table: "AgentRun");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                schema: "Agent",
                table: "AgentRun");

            migrationBuilder.DropColumn(
                name: "EnvironmentCode",
                schema: "Agent",
                table: "AgentRun");

            migrationBuilder.DropColumn(
                name: "ExternalModelCalled",
                schema: "Agent",
                table: "AgentRun");

            migrationBuilder.DropColumn(
                name: "GatewayCallId",
                schema: "Agent",
                table: "AgentRun");

            migrationBuilder.DropColumn(
                name: "GatewayMode",
                schema: "Agent",
                table: "AgentRun");

            migrationBuilder.DropColumn(
                name: "IsTestData",
                schema: "Agent",
                table: "AgentRun");

            migrationBuilder.DropColumn(
                name: "MinutesSourceId",
                schema: "Agent",
                table: "AgentRun");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                schema: "Agent",
                table: "AgentRun");

            migrationBuilder.DropColumn(
                name: "SubjectType",
                schema: "Agent",
                table: "AgentRun");

            migrationBuilder.DropColumn(
                name: "WorkflowInstanceId",
                schema: "Agent",
                table: "AgentRun");

            migrationBuilder.DropColumn(
                name: "WorkflowTaskId",
                schema: "Agent",
                table: "AgentRun");

            migrationBuilder.DropColumn(
                name: "ServiceUserId",
                schema: "Agent",
                table: "AgentProfile");
        }
    }
}
