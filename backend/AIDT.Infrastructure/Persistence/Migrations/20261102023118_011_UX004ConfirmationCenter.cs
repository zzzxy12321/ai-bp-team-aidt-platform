using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIDT.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Batch011UX004ConfirmationCenter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                schema: "Minutes",
                table: "MinutesTask",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessObjectId",
                schema: "Minutes",
                table: "MinutesActionItem",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                schema: "Minutes",
                table: "MinutesActionItem",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkflowInstanceId",
                schema: "Minutes",
                table: "MinutesActionItem",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MinutesTask_ProjectId",
                schema: "Minutes",
                table: "MinutesTask",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_MinutesActionItem_BusinessObjectId",
                schema: "Minutes",
                table: "MinutesActionItem",
                column: "BusinessObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_MinutesActionItem_ProjectId",
                schema: "Minutes",
                table: "MinutesActionItem",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_MinutesActionItem_WorkflowInstanceId",
                schema: "Minutes",
                table: "MinutesActionItem",
                column: "WorkflowInstanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_MinutesActionItem_BusinessObject",
                schema: "Minutes",
                table: "MinutesActionItem",
                column: "BusinessObjectId",
                principalSchema: "BusinessObject",
                principalTable: "BusinessObject",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MinutesActionItem_Project",
                schema: "Minutes",
                table: "MinutesActionItem",
                column: "ProjectId",
                principalSchema: "BusinessObject",
                principalTable: "BusinessObject",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MinutesActionItem_WorkflowInstance",
                schema: "Minutes",
                table: "MinutesActionItem",
                column: "WorkflowInstanceId",
                principalSchema: "Workflow",
                principalTable: "WorkflowInstance",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MinutesTask_Project",
                schema: "Minutes",
                table: "MinutesTask",
                column: "ProjectId",
                principalSchema: "BusinessObject",
                principalTable: "BusinessObject",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                INSERT INTO [Navigation].[NavigationItem]
                    ([Id], [NavigationVersionId], [ItemKey], [Name], [ItemType], [Route],
                     [RequiredResource], [RequiredAction], [SortOrder], [IsVisible], [IsEnabled],
                     [IsSystem], [Icon], [CreatedAt], [UpdatedAt])
                SELECT NEWID(), versionRow.[Id], N'CONFIRMATIONS', N'统一确认', N'Inbox',
                       N'/confirmations', NULL, NULL, 15, 1, 1, 1, N'确',
                       SYSUTCDATETIME(), SYSUTCDATETIME()
                FROM [Navigation].[NavigationVersion] AS versionRow
                WHERE versionRow.[Status] = N'Published'
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM [Navigation].[NavigationItem] AS itemRow
                      WHERE itemRow.[NavigationVersionId] = versionRow.[Id]
                        AND itemRow.[ItemKey] = N'CONFIRMATIONS'
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM [Navigation].[NavigationItem] WHERE [ItemKey] = N'CONFIRMATIONS';");

            migrationBuilder.DropForeignKey(
                name: "FK_MinutesActionItem_BusinessObject",
                schema: "Minutes",
                table: "MinutesActionItem");

            migrationBuilder.DropForeignKey(
                name: "FK_MinutesActionItem_Project",
                schema: "Minutes",
                table: "MinutesActionItem");

            migrationBuilder.DropForeignKey(
                name: "FK_MinutesActionItem_WorkflowInstance",
                schema: "Minutes",
                table: "MinutesActionItem");

            migrationBuilder.DropForeignKey(
                name: "FK_MinutesTask_Project",
                schema: "Minutes",
                table: "MinutesTask");

            migrationBuilder.DropIndex(
                name: "IX_MinutesTask_ProjectId",
                schema: "Minutes",
                table: "MinutesTask");

            migrationBuilder.DropIndex(
                name: "IX_MinutesActionItem_BusinessObjectId",
                schema: "Minutes",
                table: "MinutesActionItem");

            migrationBuilder.DropIndex(
                name: "IX_MinutesActionItem_ProjectId",
                schema: "Minutes",
                table: "MinutesActionItem");

            migrationBuilder.DropIndex(
                name: "IX_MinutesActionItem_WorkflowInstanceId",
                schema: "Minutes",
                table: "MinutesActionItem");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                schema: "Minutes",
                table: "MinutesTask");

            migrationBuilder.DropColumn(
                name: "BusinessObjectId",
                schema: "Minutes",
                table: "MinutesActionItem");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                schema: "Minutes",
                table: "MinutesActionItem");

            migrationBuilder.DropColumn(
                name: "WorkflowInstanceId",
                schema: "Minutes",
                table: "MinutesActionItem");
        }
    }
}
