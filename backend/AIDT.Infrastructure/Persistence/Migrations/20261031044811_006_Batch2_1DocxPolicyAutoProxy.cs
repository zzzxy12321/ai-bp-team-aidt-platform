using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIDT.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Batch21DocxPolicyAutoProxy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgentProxyMode",
                schema: "Workflow",
                table: "WorkflowNodePolicy",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "AutoProxyAfterMinutes",
                schema: "Workflow",
                table: "WorkflowNodePolicy",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompensationStatus",
                schema: "Agent",
                table: "AgentRun",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginalAssigneeId",
                schema: "Agent",
                table: "AgentRun",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProxyIdempotencyKey",
                schema: "Agent",
                table: "AgentRun",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProxyTriggerMode",
                schema: "Agent",
                table: "AgentRun",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProxyTriggeredAt",
                schema: "Agent",
                table: "AgentRun",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkflowNodePolicy_AutoProxyAfter",
                schema: "Workflow",
                table: "WorkflowNodePolicy",
                sql: "[AutoProxyAfterMinutes] IS NULL OR [AutoProxyAfterMinutes] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkflowNodePolicy_ProxyMode",
                schema: "Workflow",
                table: "WorkflowNodePolicy",
                sql: "[AgentProxyMode] IN ('RemindOnly','HumanConfirmedProxy','PreAuthorizedAutoProxy')");

            migrationBuilder.CreateIndex(
                name: "UX_AgentRun_ProxyIdempotency",
                schema: "Agent",
                table: "AgentRun",
                column: "ProxyIdempotencyKey",
                unique: true,
                filter: "[ProxyIdempotencyKey] IS NOT NULL");

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM [Workflow].[WorkflowDefinition] WHERE Id='52000000-0000-0000-0000-000000000001')
                    INSERT INTO [Workflow].[WorkflowDefinition] ([Id],[Code],[Name],[Status],[CreatedAt])
                    VALUES ('52000000-0000-0000-0000-000000000001',N'RND_MATERIALS_CONFIRM',N'研发资料完整性确认',N'Published',SYSUTCDATETIME());

                IF NOT EXISTS (SELECT 1 FROM [Workflow].[WorkflowVersion] WHERE Id='52000000-0000-0000-0000-000000000002')
                    INSERT INTO [Workflow].[WorkflowVersion] ([Id],[WorkflowDefinitionId],[VersionNumber],[Status],[DefinitionJson],[PublishedAt])
                    VALUES ('52000000-0000-0000-0000-000000000002','52000000-0000-0000-0000-000000000001',1,N'Published',
                    N'{"objectTypeCode":"CUSTOMER_SAMPLE","startNode":"MATERIALS_CONFIRM","nodes":[{"code":"MATERIALS_CONFIRM","name":"研发资料完整性确认","assigneeUserName":"demo.researcher","nextNode":null,"completeOnApprove":true}]}',SYSUTCDATETIME());

                DECLARE @ProxyProfileId uniqueidentifier = (SELECT Id FROM [Agent].[AgentProfile] WHERE Name=N'流程代理数字员工' AND IsActive=1);
                DECLARE @PublisherId uniqueidentifier = (SELECT Id FROM [Identity].[User] WHERE UserName=N'demo.admin' AND IsActive=1);
                IF @ProxyProfileId IS NOT NULL AND @PublisherId IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM [Workflow].[WorkflowNodePolicy] WHERE Id='52000000-0000-0000-0000-000000000003')
                    INSERT INTO [Workflow].[WorkflowNodePolicy]
                        ([Id],[WorkflowVersionId],[NodeCode],[VersionNumber],[ApproverUserId],[BackupApproverUserId],
                         [SlaMinutes],[ReminderMinutes],[EscalationMinutes],[TimeoutStrategy],[AllowAgentAssist],[AllowAgentProxy],
                         [AgentProfileId],[AgentMaxRisk],[AgentMaxAmount],[AgentMaxAutonomy],[AgentProxyMode],[AutoProxyAfterMinutes],
                         [PermanentHumanBoundary],[RequiredAttachmentCount],[PrecheckItems],[Status],[PublishedBy],[PublishedAt])
                    VALUES
                        ('52000000-0000-0000-0000-000000000003','52000000-0000-0000-0000-000000000002',N'MATERIALS_CONFIRM',1,
                         (SELECT Id FROM [Identity].[User] WHERE UserName=N'demo.researcher' AND IsActive=1),
                         (SELECT Id FROM [Identity].[User] WHERE UserName=N'demo.admin' AND IsActive=1),
                         120,30,60,N'Escalate',1,1,@ProxyProfileId,N'Low',0,N'A2',N'PreAuthorizedAutoProxy',60,
                         0,0,N'确认研发资料、附件和输入信息完整',N'Published',@PublisherId,SYSUTCDATETIME());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM [Workflow].[WorkflowNodePolicy] WHERE Id='52000000-0000-0000-0000-000000000003';
                DELETE FROM [Workflow].[WorkflowVersion] WHERE Id='52000000-0000-0000-0000-000000000002';
                DELETE FROM [Workflow].[WorkflowDefinition] WHERE Id='52000000-0000-0000-0000-000000000001';
                """);

            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkflowNodePolicy_AutoProxyAfter",
                schema: "Workflow",
                table: "WorkflowNodePolicy");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkflowNodePolicy_ProxyMode",
                schema: "Workflow",
                table: "WorkflowNodePolicy");

            migrationBuilder.DropIndex(
                name: "UX_AgentRun_ProxyIdempotency",
                schema: "Agent",
                table: "AgentRun");

            migrationBuilder.DropColumn(
                name: "AgentProxyMode",
                schema: "Workflow",
                table: "WorkflowNodePolicy");

            migrationBuilder.DropColumn(
                name: "AutoProxyAfterMinutes",
                schema: "Workflow",
                table: "WorkflowNodePolicy");

            migrationBuilder.DropColumn(
                name: "CompensationStatus",
                schema: "Agent",
                table: "AgentRun");

            migrationBuilder.DropColumn(
                name: "OriginalAssigneeId",
                schema: "Agent",
                table: "AgentRun");

            migrationBuilder.DropColumn(
                name: "ProxyIdempotencyKey",
                schema: "Agent",
                table: "AgentRun");

            migrationBuilder.DropColumn(
                name: "ProxyTriggerMode",
                schema: "Agent",
                table: "AgentRun");

            migrationBuilder.DropColumn(
                name: "ProxyTriggeredAt",
                schema: "Agent",
                table: "AgentRun");
        }
    }
}
