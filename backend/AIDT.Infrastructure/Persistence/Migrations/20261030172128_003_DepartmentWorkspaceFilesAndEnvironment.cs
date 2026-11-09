using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIDT.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DepartmentWorkspaceFilesAndEnvironment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                schema: "Workflow",
                table: "WorkflowTask",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "EnvironmentCode",
                schema: "Workflow",
                table: "WorkflowTask",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsTestData",
                schema: "Workflow",
                table: "WorkflowTask",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                schema: "Workflow",
                table: "WorkflowInstance",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "EnvironmentCode",
                schema: "Workflow",
                table: "WorkflowInstance",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsTestData",
                schema: "Workflow",
                table: "WorkflowInstance",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SourceSystem",
                schema: "Workflow",
                table: "WorkflowInstance",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "StartedByUserId",
                schema: "Workflow",
                table: "WorkflowInstance",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MimeType",
                schema: "Document",
                table: "DocumentVersion",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                schema: "Document",
                table: "DocumentVersion",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Sensitivity",
                schema: "Document",
                table: "DocumentVersion",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "SizeBytes",
                schema: "Document",
                table: "DocumentVersion",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "VirusScanStatus",
                schema: "Document",
                table: "DocumentVersion",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                schema: "BusinessObject",
                table: "BusinessObject",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "EnvironmentCode",
                schema: "BusinessObject",
                table: "BusinessObject",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsTestData",
                schema: "BusinessObject",
                table: "BusinessObject",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SourceSystem",
                schema: "BusinessObject",
                table: "BusinessObject",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CurrentVersionNumber",
                schema: "Document",
                table: "Attachment",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DeletionReason",
                schema: "Document",
                table: "Attachment",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletionRequestedAt",
                schema: "Document",
                table: "Attachment",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletionRequestedBy",
                schema: "Document",
                table: "Attachment",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                schema: "Document",
                table: "Attachment",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "EnvironmentCode",
                schema: "Document",
                table: "Attachment",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsTestData",
                schema: "Document",
                table: "Attachment",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                schema: "Document",
                table: "Attachment",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "SourceSystem",
                schema: "Document",
                table: "Attachment",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "Document",
                table: "Attachment",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "Document",
                table: "Attachment",
                type: "datetime2(3)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "AttachmentLink",
                schema: "Document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttachmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttachmentLink", x => x.Id);
                    table.CheckConstraint("CK_AttachmentLink_EntityType", "[EntityType] IN ('Department','BusinessObject','WorkflowInstance','WorkflowTask')");
                    table.ForeignKey(
                        name: "FK_AttachmentLink_Attachment",
                        column: x => x.AttachmentId,
                        principalSchema: "Document",
                        principalTable: "Attachment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Department",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ManagerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Department", x => x.Id);
                    table.CheckConstraint("CK_Department_Code", "LEN([Code]) > 0");
                    table.CheckConstraint("CK_Department_Name", "LEN([Name]) > 0");
                    table.ForeignKey(
                        name: "FK_Department_Manager",
                        column: x => x.ManagerUserId,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Department_Organization",
                        column: x => x.OrganizationId,
                        principalSchema: "Identity",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserDepartmentMembership",
                schema: "Identity",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsManager = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDepartmentMembership", x => new { x.UserId, x.DepartmentId });
                    table.CheckConstraint("CK_UserDepartmentMembership_PrimaryActive", "[IsPrimary] = 0 OR [IsActive] = 1");
                    table.ForeignKey(
                        name: "FK_UserDepartmentMembership_Department",
                        column: x => x.DepartmentId,
                        principalSchema: "Identity",
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserDepartmentMembership_User",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                DECLARE @Now datetime2(3) = SYSUTCDATETIME();

                INSERT INTO [Identity].[Department]
                    (Id, OrganizationId, Code, Name, ManagerUserId, IsActive, CreatedAt, UpdatedAt)
                SELECT
                    NEWID(),
                    organizationRow.Id,
                    departmentSeed.Code,
                    departmentSeed.Name,
                    NULL,
                    1,
                    @Now,
                    @Now
                FROM [Identity].[Organization] organizationRow
                CROSS APPLY (VALUES
                    (N'RND', N'研发'),
                    (N'QA', N'质量'),
                    (N'SUPPLY', N'供应链'),
                    (N'FINANCE', N'财务'),
                    (N'PRODUCTION', N'生产'),
                    (N'PROJECT', N'项目')
                ) departmentSeed(Code, Name)
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM [Identity].[Department] existing
                    WHERE existing.OrganizationId = organizationRow.Id
                      AND existing.Code = departmentSeed.Code
                );

                INSERT INTO [Identity].[UserDepartmentMembership]
                    (UserId, DepartmentId, IsPrimary, IsManager, IsActive, JoinedAt)
                SELECT
                    userRow.Id,
                    departmentRow.Id,
                    1,
                    membershipSeed.IsManager,
                    1,
                    @Now
                FROM (VALUES
                    (N'demo.admin', N'RND', 1),
                    (N'demo.researcher', N'RND', 0),
                    (N'demo.qa', N'QA', 1),
                    (N'demo.supply', N'SUPPLY', 1),
                    (N'demo.finance', N'FINANCE', 1),
                    (N'demo.production', N'PRODUCTION', 1)
                ) membershipSeed(UserName, DepartmentCode, IsManager)
                JOIN [Identity].[User] userRow
                    ON userRow.UserName = membershipSeed.UserName
                JOIN [Identity].[Department] departmentRow
                    ON departmentRow.OrganizationId = userRow.OrganizationId
                   AND departmentRow.Code = membershipSeed.DepartmentCode
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM [Identity].[UserDepartmentMembership] existing
                    WHERE existing.UserId = userRow.Id
                      AND existing.DepartmentId = departmentRow.Id
                );

                INSERT INTO [Identity].[UserDepartmentMembership]
                    (UserId, DepartmentId, IsPrimary, IsManager, IsActive, JoinedAt)
                SELECT
                    userRow.Id,
                    departmentRow.Id,
                    1,
                    0,
                    1,
                    @Now
                FROM [Identity].[User] userRow
                JOIN [Identity].[Department] departmentRow
                    ON departmentRow.OrganizationId = userRow.OrganizationId
                   AND departmentRow.Code = N'RND'
                WHERE userRow.IsActive = 1
                  AND NOT EXISTS (
                      SELECT 1
                      FROM [Identity].[UserDepartmentMembership] existing
                      WHERE existing.UserId = userRow.Id
                        AND existing.IsActive = 1
                  );

                UPDATE membershipRow
                SET membershipRow.IsManager = 1
                FROM [Identity].[UserDepartmentMembership] membershipRow
                WHERE membershipRow.IsActive = 1
                  AND EXISTS (
                      SELECT 1
                      FROM [Identity].[User] userRow
                      WHERE userRow.Id = membershipRow.UserId
                        AND userRow.UserName IN (N'demo.admin', N'demo.qa', N'demo.supply', N'demo.finance', N'demo.production')
                  );

                UPDATE departmentRow
                SET departmentRow.ManagerUserId = membershipRow.UserId,
                    departmentRow.UpdatedAt = @Now
                FROM [Identity].[Department] departmentRow
                JOIN [Identity].[UserDepartmentMembership] membershipRow
                    ON membershipRow.DepartmentId = departmentRow.Id
                   AND membershipRow.IsManager = 1
                   AND membershipRow.IsActive = 1;

                INSERT INTO [Identity].[DataScope] (Id, UserId, ScopeType, ScopeReference)
                SELECT NEWID(), membershipRow.UserId, N'department', CONVERT(nvarchar(36), membershipRow.DepartmentId)
                FROM [Identity].[UserDepartmentMembership] membershipRow
                WHERE membershipRow.IsActive = 1
                  AND NOT EXISTS (
                      SELECT 1
                      FROM [Identity].[DataScope] existing
                      WHERE existing.UserId = membershipRow.UserId
                        AND existing.ScopeType = N'department'
                        AND existing.ScopeReference = CONVERT(nvarchar(36), membershipRow.DepartmentId)
                  );

                INSERT INTO [Identity].[Permission] (Id, Code, Resource, Action)
                SELECT permissionSeed.Id, permissionSeed.Code, permissionSeed.Resource, permissionSeed.Action
                FROM (VALUES
                    (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000021'), N'department-read', N'department', N'Read'),
                    (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000022'), N'department-create', N'department', N'Create'),
                    (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000023'), N'department-update', N'department', N'Update'),
                    (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000024'), N'file-read', N'file', N'Read'),
                    (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000025'), N'file-create', N'file', N'Create'),
                    (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000026'), N'file-update', N'file', N'Update'),
                    (CONVERT(uniqueidentifier, '30000000-0000-0000-0000-000000000027'), N'file-delete', N'file', N'Delete')
                ) permissionSeed(Id, Code, Resource, Action)
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM [Identity].[Permission] existing
                    WHERE existing.Code = permissionSeed.Code
                       OR (existing.Resource = permissionSeed.Resource AND existing.Action = permissionSeed.Action)
                );

                INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
                SELECT roleRow.Id, permissionRow.Id
                FROM [Identity].[Role] roleRow
                JOIN [Identity].[Permission] permissionRow
                    ON permissionRow.Code IN
                    (
                        N'department-read',
                        N'file-read',
                        N'file-create',
                        N'file-update',
                        N'file-delete'
                    )
                WHERE roleRow.Code IN
                    (
                        N'platform-admin',
                        N'rd-engineer',
                        N'quality-engineer',
                        N'supply-specialist',
                        N'finance-approver',
                        N'production-team'
                    )
                  AND NOT EXISTS (
                      SELECT 1
                      FROM [Identity].[RolePermission] existing
                      WHERE existing.RoleId = roleRow.Id
                        AND existing.PermissionId = permissionRow.Id
                  );

                INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
                SELECT roleRow.Id, permissionRow.Id
                FROM [Identity].[Role] roleRow
                JOIN [Identity].[Permission] permissionRow
                    ON permissionRow.Code IN (N'department-create', N'department-update')
                WHERE roleRow.Code = N'platform-admin'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM [Identity].[RolePermission] existing
                      WHERE existing.RoleId = roleRow.Id
                        AND existing.PermissionId = permissionRow.Id
                  );

                UPDATE businessObject
                SET businessObject.DepartmentId = COALESCE(departmentRow.Id, defaultDepartment.Id),
                    businessObject.EnvironmentCode = N'demo',
                    businessObject.IsTestData = 1,
                    businessObject.SourceSystem = N'AIDT-SEED'
                FROM [BusinessObject].[BusinessObject] businessObject
                LEFT JOIN [Metadata].[ObjectType] objectType
                    ON objectType.Id = businessObject.ObjectTypeId
                OUTER APPLY (
                    SELECT TOP (1) candidate.Id
                    FROM [Identity].[Department] candidate
                    WHERE candidate.OrganizationId = businessObject.OrganizationId
                      AND candidate.Code = CASE objectType.Code
                          WHEN N'CUSTOMER_SAMPLE' THEN N'RND'
                          WHEN N'QUALITY_DEVIATION' THEN N'QA'
                          WHEN N'CUSTOMER_COMPLAINT' THEN N'QA'
                          WHEN N'PURCHASE_REQUEST' THEN N'SUPPLY'
                          ELSE N'RND'
                      END
                ) departmentRow
                OUTER APPLY (
                    SELECT TOP (1) fallback.Id
                    FROM [Identity].[Department] fallback
                    WHERE fallback.OrganizationId = businessObject.OrganizationId
                    ORDER BY fallback.Code
                ) defaultDepartment;

                UPDATE workflowInstance
                SET DepartmentId = businessObject.DepartmentId,
                    EnvironmentCode = N'demo',
                    IsTestData = 1,
                    SourceSystem = N'AIDT-SEED',
                    StartedByUserId = NULL
                FROM [Workflow].[WorkflowInstance] workflowInstance
                JOIN [BusinessObject].[BusinessObject] businessObject
                    ON businessObject.Id = workflowInstance.BusinessObjectId;

                UPDATE workflowTask
                SET DepartmentId = COALESCE(assigneeDepartment.Id, workflowInstance.DepartmentId),
                    EnvironmentCode = workflowInstance.EnvironmentCode,
                    IsTestData = workflowInstance.IsTestData
                FROM [Workflow].[WorkflowTask] workflowTask
                JOIN [Workflow].[WorkflowInstance] workflowInstance
                    ON workflowInstance.Id = workflowTask.WorkflowInstanceId
                OUTER APPLY (
                    SELECT TOP (1) departmentRow.Id
                    FROM [Identity].[UserDepartmentMembership] membershipRow
                    JOIN [Identity].[Department] departmentRow
                        ON departmentRow.Id = membershipRow.DepartmentId
                    WHERE membershipRow.UserId = workflowTask.AssigneeId
                      AND membershipRow.IsActive = 1
                      AND departmentRow.IsActive = 1
                    ORDER BY membershipRow.IsPrimary DESC, membershipRow.JoinedAt
                ) assigneeDepartment;

                UPDATE attachment
                SET OrganizationId = COALESCE(
                        businessObject.OrganizationId,
                        (SELECT TOP (1) organizationRow.Id FROM [Identity].[Organization] organizationRow ORDER BY organizationRow.Id)
                    ),
                    DepartmentId = COALESCE(
                        businessObject.DepartmentId,
                        (SELECT TOP (1) departmentRow.Id FROM [Identity].[Department] departmentRow ORDER BY departmentRow.Code)
                    ),
                    EnvironmentCode = N'demo',
                    IsTestData = 1,
                    SourceSystem = N'AIDT-SEED',
                    Status = N'Active',
                    Sensitivity = CASE
                        WHEN attachment.Sensitivity IN (N'Internal', N'Sensitive', N'Core') THEN attachment.Sensitivity
                        ELSE N'Internal'
                    END,
                    VirusScanStatus = CASE
                        WHEN attachment.VirusScanStatus IN (N'NotConfigured', N'PendingManualReview', N'Clean', N'Infected')
                            THEN attachment.VirusScanStatus
                        ELSE N'NotConfigured'
                    END,
                    CurrentVersionNumber = COALESCE((
                        SELECT MAX(documentVersion.VersionNumber)
                        FROM [Document].[DocumentVersion] documentVersion
                        WHERE documentVersion.DocumentId = attachment.Id
                    ), 1),
                    UpdatedAt = COALESCE(attachment.UpdatedAt, attachment.CreatedAt)
                FROM [Document].[Attachment] attachment
                LEFT JOIN [BusinessObject].[BusinessObject] businessObject
                    ON businessObject.Id = attachment.BusinessObjectId;

                UPDATE attachment
                SET UpdatedAt = CreatedAt
                FROM [Document].[Attachment] attachment
                WHERE attachment.UpdatedAt < CONVERT(datetime2(3), '19000101');

                UPDATE documentVersion
                SET OriginalFileName = attachment.OriginalFileName,
                    MimeType = attachment.MimeType,
                    SizeBytes = CASE WHEN attachment.SizeBytes > 0 THEN attachment.SizeBytes ELSE 1 END,
                    VirusScanStatus = attachment.VirusScanStatus,
                    Sensitivity = attachment.Sensitivity
                FROM [Document].[DocumentVersion] documentVersion
                JOIN [Document].[Attachment] attachment
                    ON attachment.Id = documentVersion.DocumentId;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTask_Department_Environment_Status_CreatedAt",
                schema: "Workflow",
                table: "WorkflowTask",
                columns: new[] { "DepartmentId", "EnvironmentCode", "Status", "CreatedAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkflowTask_Environment",
                schema: "Workflow",
                table: "WorkflowTask",
                sql: "[EnvironmentCode] IN ('demo','test','prod')");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_Department_Environment_StartedAt",
                schema: "Workflow",
                table: "WorkflowInstance",
                columns: new[] { "DepartmentId", "EnvironmentCode", "StartedAt", "Id" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkflowInstance_Environment",
                schema: "Workflow",
                table: "WorkflowInstance",
                sql: "[EnvironmentCode] IN ('demo','test','prod')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DocumentVersion_ScanStatus",
                schema: "Document",
                table: "DocumentVersion",
                sql: "[VirusScanStatus] IN ('NotConfigured','PendingManualReview','Clean','Infected')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DocumentVersion_Sensitivity",
                schema: "Document",
                table: "DocumentVersion",
                sql: "[Sensitivity] IN ('Internal','Sensitive','Core')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DocumentVersion_Size",
                schema: "Document",
                table: "DocumentVersion",
                sql: "[SizeBytes] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessObject_Department_Environment_UpdatedAt",
                schema: "BusinessObject",
                table: "BusinessObject",
                columns: new[] { "DepartmentId", "EnvironmentCode", "UpdatedAt", "Id" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_BusinessObject_Environment",
                schema: "BusinessObject",
                table: "BusinessObject",
                sql: "[EnvironmentCode] IN ('demo','test','prod')");

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_Department_Environment_CreatedAt",
                schema: "Document",
                table: "Attachment",
                columns: new[] { "DepartmentId", "EnvironmentCode", "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_Organization_Department",
                schema: "Document",
                table: "Attachment",
                columns: new[] { "OrganizationId", "DepartmentId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Attachment_Environment",
                schema: "Document",
                table: "Attachment",
                sql: "[EnvironmentCode] IN ('demo','test','prod')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Attachment_ScanStatus",
                schema: "Document",
                table: "Attachment",
                sql: "[VirusScanStatus] IN ('NotConfigured','PendingManualReview','Clean','Infected')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Attachment_Sensitivity",
                schema: "Document",
                table: "Attachment",
                sql: "[Sensitivity] IN ('Internal','Sensitive','Core')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Attachment_Size",
                schema: "Document",
                table: "Attachment",
                sql: "[SizeBytes] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Attachment_Status",
                schema: "Document",
                table: "Attachment",
                sql: "[Status] IN ('Active','DeletionRequested')");

            migrationBuilder.CreateIndex(
                name: "IX_AttachmentLink_Entity_CreatedAt",
                schema: "Document",
                table: "AttachmentLink",
                columns: new[] { "EntityType", "EntityId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_AttachmentLink_Attachment_Entity",
                schema: "Document",
                table: "AttachmentLink",
                columns: new[] { "AttachmentId", "EntityType", "EntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Department_ManagerUserId",
                schema: "Identity",
                table: "Department",
                column: "ManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Department_Organization_Active",
                schema: "Identity",
                table: "Department",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UX_Department_Organization_Code",
                schema: "Identity",
                table: "Department",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserDepartmentMembership_Department_Active",
                schema: "Identity",
                table: "UserDepartmentMembership",
                columns: new[] { "DepartmentId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_UserDepartmentMembership_User_Primary",
                schema: "Identity",
                table: "UserDepartmentMembership",
                columns: new[] { "UserId", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "UX_UserDepartmentMembership_User_ActivePrimary",
                schema: "Identity",
                table: "UserDepartmentMembership",
                column: "UserId",
                unique: true,
                filter: "[IsPrimary] = 1 AND [IsActive] = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_Attachment_Department",
                schema: "Document",
                table: "Attachment",
                column: "DepartmentId",
                principalSchema: "Identity",
                principalTable: "Department",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Attachment_Organization",
                schema: "Document",
                table: "Attachment",
                column: "OrganizationId",
                principalSchema: "Identity",
                principalTable: "Organization",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessObject_Department",
                schema: "BusinessObject",
                table: "BusinessObject",
                column: "DepartmentId",
                principalSchema: "Identity",
                principalTable: "Department",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentVersion_Attachment",
                schema: "Document",
                table: "DocumentVersion",
                column: "DocumentId",
                principalSchema: "Document",
                principalTable: "Attachment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowInstance_Department",
                schema: "Workflow",
                table: "WorkflowInstance",
                column: "DepartmentId",
                principalSchema: "Identity",
                principalTable: "Department",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowTask_Department",
                schema: "Workflow",
                table: "WorkflowTask",
                column: "DepartmentId",
                principalSchema: "Identity",
                principalTable: "Department",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE rolePermission
                FROM [Identity].[RolePermission] rolePermission
                JOIN [Identity].[Permission] permissionRow
                    ON permissionRow.Id = rolePermission.PermissionId
                WHERE permissionRow.Code IN
                    (
                        N'department-read',
                        N'department-create',
                        N'department-update',
                        N'file-read',
                        N'file-create',
                        N'file-update',
                        N'file-delete'
                    );

                DELETE [Identity].[Permission]
                WHERE Code IN
                    (
                        N'department-read',
                        N'department-create',
                        N'department-update',
                        N'file-read',
                        N'file-create',
                        N'file-update',
                        N'file-delete'
                    );

                DELETE [Identity].[DataScope]
                WHERE ScopeType = N'department';
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Attachment_Department",
                schema: "Document",
                table: "Attachment");

            migrationBuilder.DropForeignKey(
                name: "FK_Attachment_Organization",
                schema: "Document",
                table: "Attachment");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessObject_Department",
                schema: "BusinessObject",
                table: "BusinessObject");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentVersion_Attachment",
                schema: "Document",
                table: "DocumentVersion");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowInstance_Department",
                schema: "Workflow",
                table: "WorkflowInstance");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowTask_Department",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropTable(
                name: "AttachmentLink",
                schema: "Document");

            migrationBuilder.DropTable(
                name: "UserDepartmentMembership",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "Department",
                schema: "Identity");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowTask_Department_Environment_Status_CreatedAt",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkflowTask_Environment",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowInstance_Department_Environment_StartedAt",
                schema: "Workflow",
                table: "WorkflowInstance");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkflowInstance_Environment",
                schema: "Workflow",
                table: "WorkflowInstance");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DocumentVersion_ScanStatus",
                schema: "Document",
                table: "DocumentVersion");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DocumentVersion_Sensitivity",
                schema: "Document",
                table: "DocumentVersion");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DocumentVersion_Size",
                schema: "Document",
                table: "DocumentVersion");

            migrationBuilder.DropIndex(
                name: "IX_BusinessObject_Department_Environment_UpdatedAt",
                schema: "BusinessObject",
                table: "BusinessObject");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BusinessObject_Environment",
                schema: "BusinessObject",
                table: "BusinessObject");

            migrationBuilder.DropIndex(
                name: "IX_Attachment_Department_Environment_CreatedAt",
                schema: "Document",
                table: "Attachment");

            migrationBuilder.DropIndex(
                name: "IX_Attachment_Organization_Department",
                schema: "Document",
                table: "Attachment");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Attachment_Environment",
                schema: "Document",
                table: "Attachment");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Attachment_ScanStatus",
                schema: "Document",
                table: "Attachment");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Attachment_Sensitivity",
                schema: "Document",
                table: "Attachment");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Attachment_Size",
                schema: "Document",
                table: "Attachment");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Attachment_Status",
                schema: "Document",
                table: "Attachment");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropColumn(
                name: "EnvironmentCode",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropColumn(
                name: "IsTestData",
                schema: "Workflow",
                table: "WorkflowTask");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                schema: "Workflow",
                table: "WorkflowInstance");

            migrationBuilder.DropColumn(
                name: "EnvironmentCode",
                schema: "Workflow",
                table: "WorkflowInstance");

            migrationBuilder.DropColumn(
                name: "IsTestData",
                schema: "Workflow",
                table: "WorkflowInstance");

            migrationBuilder.DropColumn(
                name: "SourceSystem",
                schema: "Workflow",
                table: "WorkflowInstance");

            migrationBuilder.DropColumn(
                name: "StartedByUserId",
                schema: "Workflow",
                table: "WorkflowInstance");

            migrationBuilder.DropColumn(
                name: "MimeType",
                schema: "Document",
                table: "DocumentVersion");

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                schema: "Document",
                table: "DocumentVersion");

            migrationBuilder.DropColumn(
                name: "Sensitivity",
                schema: "Document",
                table: "DocumentVersion");

            migrationBuilder.DropColumn(
                name: "SizeBytes",
                schema: "Document",
                table: "DocumentVersion");

            migrationBuilder.DropColumn(
                name: "VirusScanStatus",
                schema: "Document",
                table: "DocumentVersion");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                schema: "BusinessObject",
                table: "BusinessObject");

            migrationBuilder.DropColumn(
                name: "EnvironmentCode",
                schema: "BusinessObject",
                table: "BusinessObject");

            migrationBuilder.DropColumn(
                name: "IsTestData",
                schema: "BusinessObject",
                table: "BusinessObject");

            migrationBuilder.DropColumn(
                name: "SourceSystem",
                schema: "BusinessObject",
                table: "BusinessObject");

            migrationBuilder.DropColumn(
                name: "CurrentVersionNumber",
                schema: "Document",
                table: "Attachment");

            migrationBuilder.DropColumn(
                name: "DeletionReason",
                schema: "Document",
                table: "Attachment");

            migrationBuilder.DropColumn(
                name: "DeletionRequestedAt",
                schema: "Document",
                table: "Attachment");

            migrationBuilder.DropColumn(
                name: "DeletionRequestedBy",
                schema: "Document",
                table: "Attachment");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                schema: "Document",
                table: "Attachment");

            migrationBuilder.DropColumn(
                name: "EnvironmentCode",
                schema: "Document",
                table: "Attachment");

            migrationBuilder.DropColumn(
                name: "IsTestData",
                schema: "Document",
                table: "Attachment");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                schema: "Document",
                table: "Attachment");

            migrationBuilder.DropColumn(
                name: "SourceSystem",
                schema: "Document",
                table: "Attachment");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "Document",
                table: "Attachment");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "Document",
                table: "Attachment");
        }
    }
}
