using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIDT.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Batch008ConfigurationCenter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConfigurationType",
                schema: "Configuration",
                table: "ConfigurationVersion",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "System");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                schema: "Configuration",
                table: "ConfigurationVersion",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "Configuration",
                table: "ConfigurationVersion",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "Configuration",
                table: "ConfigurationVersion",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "Configuration");

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                schema: "Configuration",
                table: "ConfigurationVersion",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "PublishedBy",
                schema: "Configuration",
                table: "ConfigurationVersion",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublishedWorkflowVersionId",
                schema: "Configuration",
                table: "ConfigurationVersion",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Revision",
                schema: "Configuration",
                table: "ConfigurationVersion",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "Configuration",
                table: "ConfigurationVersion",
                type: "datetime2(3)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                schema: "Configuration",
                table: "ConfigurationVersion",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationVersion_CreatedBy",
                schema: "Configuration",
                table: "ConfigurationVersion",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationVersion_DepartmentId",
                schema: "Configuration",
                table: "ConfigurationVersion",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationVersion_Org_Type_State_UpdatedAt",
                schema: "Configuration",
                table: "ConfigurationVersion",
                columns: new[] { "OrganizationId", "ConfigurationType", "State", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationVersion_PublishedBy",
                schema: "Configuration",
                table: "ConfigurationVersion",
                column: "PublishedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationVersion_PublishedWorkflowVersionId",
                schema: "Configuration",
                table: "ConfigurationVersion",
                column: "PublishedWorkflowVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationVersion_UpdatedBy",
                schema: "Configuration",
                table: "ConfigurationVersion",
                column: "UpdatedBy");

            migrationBuilder.Sql(
                """
                DECLARE @FallbackOrganizationId uniqueidentifier =
                    (SELECT TOP (1) Id FROM [Identity].[Organization] ORDER BY CreatedAt, Id);

                UPDATE [Configuration].[ConfigurationVersion]
                SET [OrganizationId] = COALESCE(@FallbackOrganizationId, [OrganizationId]),
                    [Name] = CASE
                        WHEN [ConfigurationKey] = N'SYSTEM.WORKER.HEARTBEAT' THEN N'Worker heartbeat'
                        WHEN [Name] = N'' OR [Name] IS NULL THEN [ConfigurationKey]
                        ELSE [Name]
                    END,
                    [ConfigurationType] = CASE
                        WHEN [ConfigurationKey] LIKE N'SYSTEM.%' THEN N'System'
                        WHEN [ConfigurationType] = N'' THEN N'Workflow'
                        ELSE [ConfigurationType]
                    END,
                    [UpdatedBy] = COALESCE([UpdatedBy], [CreatedBy]),
                    [UpdatedAt] = CASE
                        WHEN [UpdatedAt] <= CONVERT(datetime2(3), '19000101') THEN [CreatedAt]
                        ELSE [UpdatedAt]
                    END;

                IF EXISTS (
                    SELECT 1
                    FROM [Configuration].[ConfigurationVersion]
                    WHERE [OrganizationId] = '00000000-0000-0000-0000-000000000000')
                    THROW 51008, N'配置中心迁移无法为历史配置确定组织。', 1;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ConfigurationVersion_State",
                schema: "Configuration",
                table: "ConfigurationVersion",
                sql: "[State] IN ('Draft','Validated','Published','Retired','Active')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ConfigurationVersion_Type",
                schema: "Configuration",
                table: "ConfigurationVersion",
                sql: "[ConfigurationType] IN ('Workflow','System')");

            migrationBuilder.AddForeignKey(
                name: "FK_ConfigurationVersion_CreatedBy",
                schema: "Configuration",
                table: "ConfigurationVersion",
                column: "CreatedBy",
                principalSchema: "Identity",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ConfigurationVersion_Department",
                schema: "Configuration",
                table: "ConfigurationVersion",
                column: "DepartmentId",
                principalSchema: "Identity",
                principalTable: "Department",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ConfigurationVersion_Organization",
                schema: "Configuration",
                table: "ConfigurationVersion",
                column: "OrganizationId",
                principalSchema: "Identity",
                principalTable: "Organization",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ConfigurationVersion_PublishedBy",
                schema: "Configuration",
                table: "ConfigurationVersion",
                column: "PublishedBy",
                principalSchema: "Identity",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ConfigurationVersion_PublishedWorkflowVersion",
                schema: "Configuration",
                table: "ConfigurationVersion",
                column: "PublishedWorkflowVersionId",
                principalSchema: "Workflow",
                principalTable: "WorkflowVersion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ConfigurationVersion_UpdatedBy",
                schema: "Configuration",
                table: "ConfigurationVersion",
                column: "UpdatedBy",
                principalSchema: "Identity",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                INSERT INTO [Identity].[Permission] (Id, Code, Resource, Action)
                SELECT permissionSeed.Id, permissionSeed.Code, permissionSeed.Resource, permissionSeed.Action
                FROM (VALUES
                    ('31000000-0000-0000-0000-000000000050', N'configuration-create', N'configuration', N'Create'),
                    ('31000000-0000-0000-0000-000000000051', N'configuration-update', N'configuration', N'Update'),
                    ('31000000-0000-0000-0000-000000000052', N'configuration-publish', N'configuration', N'Approve'),
                    ('31000000-0000-0000-0000-000000000053', N'configuration-rollback', N'configuration', N'Manage')
                ) permissionSeed(Id, Code, Resource, Action)
                WHERE NOT EXISTS (
                    SELECT 1 FROM [Identity].[Permission] existing
                    WHERE existing.Code = permissionSeed.Code
                       OR (existing.Resource = permissionSeed.Resource AND existing.Action = permissionSeed.Action)
                );

                INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
                SELECT roleRow.Id, permissionRow.Id
                FROM [Identity].[Role] roleRow
                CROSS JOIN [Identity].[Permission] permissionRow
                WHERE roleRow.Code = N'platform-admin'
                  AND permissionRow.Code IN (
                      N'configuration-read', N'configuration-create',
                      N'configuration-update', N'configuration-publish',
                      N'configuration-rollback')
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
                      N'configuration-read', N'configuration-create', N'configuration-update')
                  AND NOT EXISTS (
                      SELECT 1 FROM [Identity].[RolePermission] existing
                      WHERE existing.RoleId = roleRow.Id AND existing.PermissionId = permissionRow.Id
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConfigurationVersion_CreatedBy",
                schema: "Configuration",
                table: "ConfigurationVersion");

            migrationBuilder.DropForeignKey(
                name: "FK_ConfigurationVersion_Department",
                schema: "Configuration",
                table: "ConfigurationVersion");

            migrationBuilder.DropForeignKey(
                name: "FK_ConfigurationVersion_Organization",
                schema: "Configuration",
                table: "ConfigurationVersion");

            migrationBuilder.DropForeignKey(
                name: "FK_ConfigurationVersion_PublishedBy",
                schema: "Configuration",
                table: "ConfigurationVersion");

            migrationBuilder.DropForeignKey(
                name: "FK_ConfigurationVersion_PublishedWorkflowVersion",
                schema: "Configuration",
                table: "ConfigurationVersion");

            migrationBuilder.DropForeignKey(
                name: "FK_ConfigurationVersion_UpdatedBy",
                schema: "Configuration",
                table: "ConfigurationVersion");

            migrationBuilder.Sql(
                """
                DELETE rolePermission
                FROM [Identity].[RolePermission] rolePermission
                JOIN [Identity].[Permission] permissionRow ON permissionRow.Id = rolePermission.PermissionId
                WHERE permissionRow.Code IN (
                    N'configuration-create', N'configuration-update',
                    N'configuration-publish', N'configuration-rollback');

                DELETE FROM [Identity].[Permission]
                WHERE Code IN (
                    N'configuration-create', N'configuration-update',
                    N'configuration-publish', N'configuration-rollback');
                """);

            migrationBuilder.DropIndex(
                name: "IX_ConfigurationVersion_CreatedBy",
                schema: "Configuration",
                table: "ConfigurationVersion");

            migrationBuilder.DropIndex(
                name: "IX_ConfigurationVersion_DepartmentId",
                schema: "Configuration",
                table: "ConfigurationVersion");

            migrationBuilder.DropIndex(
                name: "IX_ConfigurationVersion_Org_Type_State_UpdatedAt",
                schema: "Configuration",
                table: "ConfigurationVersion");

            migrationBuilder.DropIndex(
                name: "IX_ConfigurationVersion_PublishedBy",
                schema: "Configuration",
                table: "ConfigurationVersion");

            migrationBuilder.DropIndex(
                name: "IX_ConfigurationVersion_PublishedWorkflowVersionId",
                schema: "Configuration",
                table: "ConfigurationVersion");

            migrationBuilder.DropIndex(
                name: "IX_ConfigurationVersion_UpdatedBy",
                schema: "Configuration",
                table: "ConfigurationVersion");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ConfigurationVersion_State",
                schema: "Configuration",
                table: "ConfigurationVersion");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ConfigurationVersion_Type",
                schema: "Configuration",
                table: "ConfigurationVersion");

            migrationBuilder.DropColumn(
                name: "ConfigurationType",
                schema: "Configuration",
                table: "ConfigurationVersion");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                schema: "Configuration",
                table: "ConfigurationVersion");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "Configuration",
                table: "ConfigurationVersion");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "Configuration",
                table: "ConfigurationVersion");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                schema: "Configuration",
                table: "ConfigurationVersion");

            migrationBuilder.DropColumn(
                name: "PublishedBy",
                schema: "Configuration",
                table: "ConfigurationVersion");

            migrationBuilder.DropColumn(
                name: "PublishedWorkflowVersionId",
                schema: "Configuration",
                table: "ConfigurationVersion");

            migrationBuilder.DropColumn(
                name: "Revision",
                schema: "Configuration",
                table: "ConfigurationVersion");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "Configuration",
                table: "ConfigurationVersion");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "Configuration",
                table: "ConfigurationVersion");
        }
    }
}
