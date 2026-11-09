using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIDT.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WS002RoleWorkbenchTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Workspace");

            migrationBuilder.CreateTable(
                name: "WorkbenchTemplate",
                schema: "Workspace",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AudienceTitle = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PrimaryQuestion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_WorkbenchTemplate", x => x.Id);
                    table.CheckConstraint("CK_WorkbenchTemplate_Status", "[Status] IN ('Draft','Published','Retired')");
                    table.ForeignKey(
                        name: "FK_WorkbenchTemplate_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkbenchTemplate_Department",
                        column: x => x.DepartmentId,
                        principalSchema: "Identity",
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkbenchTemplate_Organization",
                        column: x => x.OrganizationId,
                        principalSchema: "Identity",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkbenchTemplate_RetiredBy",
                        column: x => x.RetiredBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkbenchTemplate_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkbenchTemplateVersion",
                schema: "Workspace",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    ValidationJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_WorkbenchTemplateVersion", x => x.Id);
                    table.CheckConstraint("CK_WorkbenchTemplateVersion_Impact", "[ImpactAnalysisJson] IS NULL OR ISJSON([ImpactAnalysisJson]) = 1");
                    table.CheckConstraint("CK_WorkbenchTemplateVersion_Status", "[Status] IN ('Draft','Published','Retired')");
                    table.CheckConstraint("CK_WorkbenchTemplateVersion_Validation", "[ValidationJson] IS NULL OR ISJSON([ValidationJson]) = 1");
                    table.ForeignKey(
                        name: "FK_WorkbenchTemplateVersion_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkbenchTemplateVersion_PublishedBy",
                        column: x => x.PublishedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkbenchTemplateVersion_Template",
                        column: x => x.TemplateId,
                        principalSchema: "Workspace",
                        principalTable: "WorkbenchTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkbenchTemplateVersion_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkbenchAssignment",
                schema: "Workspace",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ScopeValue = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkbenchAssignment", x => x.Id);
                    table.CheckConstraint("CK_WorkbenchAssignment_Type", "[AssignmentType] IN ('User','Role','Department','Organization')");
                    table.ForeignKey(
                        name: "FK_WorkbenchAssignment_Version",
                        column: x => x.TemplateVersionId,
                        principalSchema: "Workspace",
                        principalTable: "WorkbenchTemplateVersion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkbenchCard",
                schema: "Workspace",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Width = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    DataSourceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FilterJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredResource = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RequiredAction = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CapabilityState = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkbenchCard", x => x.Id);
                    table.CheckConstraint("CK_WorkbenchCard_CapabilityState", "[CapabilityState] IN ('Connected','NotConnected')");
                    table.CheckConstraint("CK_WorkbenchCard_FilterJson", "[FilterJson] IS NOT NULL AND ISJSON([FilterJson]) = 1");
                    table.CheckConstraint("CK_WorkbenchCard_Width", "[Width] IN ('Third','Half','Full')");
                    table.ForeignKey(
                        name: "FK_WorkbenchCard_Version",
                        column: x => x.TemplateVersionId,
                        principalSchema: "Workspace",
                        principalTable: "WorkbenchTemplateVersion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_WorkbenchAssignment_Version_Type_Scope",
                schema: "Workspace",
                table: "WorkbenchAssignment",
                columns: new[] { "TemplateVersionId", "AssignmentType", "ScopeValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkbenchCard_Version_Sort",
                schema: "Workspace",
                table: "WorkbenchCard",
                columns: new[] { "TemplateVersionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkbenchTemplate_CreatedBy",
                schema: "Workspace",
                table: "WorkbenchTemplate",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_WorkbenchTemplate_DepartmentId",
                schema: "Workspace",
                table: "WorkbenchTemplate",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkbenchTemplate_Org_Department_Status_Sort",
                schema: "Workspace",
                table: "WorkbenchTemplate",
                columns: new[] { "OrganizationId", "DepartmentId", "Status", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkbenchTemplate_RetiredBy",
                schema: "Workspace",
                table: "WorkbenchTemplate",
                column: "RetiredBy");

            migrationBuilder.CreateIndex(
                name: "IX_WorkbenchTemplate_UpdatedBy",
                schema: "Workspace",
                table: "WorkbenchTemplate",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UX_WorkbenchTemplate_Organization_Code",
                schema: "Workspace",
                table: "WorkbenchTemplate",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkbenchTemplateVersion_CreatedBy",
                schema: "Workspace",
                table: "WorkbenchTemplateVersion",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_WorkbenchTemplateVersion_PublishedBy",
                schema: "Workspace",
                table: "WorkbenchTemplateVersion",
                column: "PublishedBy");

            migrationBuilder.CreateIndex(
                name: "IX_WorkbenchTemplateVersion_Template_Status",
                schema: "Workspace",
                table: "WorkbenchTemplateVersion",
                columns: new[] { "TemplateId", "Status", "Revision" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkbenchTemplateVersion_UpdatedBy",
                schema: "Workspace",
                table: "WorkbenchTemplateVersion",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UX_WorkbenchTemplateVersion_Template_Version",
                schema: "Workspace",
                table: "WorkbenchTemplateVersion",
                columns: new[] { "TemplateId", "VersionNumber" },
                unique: true);

            migrationBuilder.Sql(
                """
                DECLARE @Now datetime2(3) = SYSUTCDATETIME();
                DECLARE @OrganizationId uniqueidentifier =
                    (SELECT TOP (1) Id FROM [Identity].[Organization] WHERE IsActive = 1 ORDER BY CreatedAt);
                DECLARE @PublisherId uniqueidentifier =
                    (SELECT TOP (1) Id FROM [Identity].[User] WHERE UserName = N'demo.admin' AND IsActive = 1);
                DECLARE @RndDepartmentId uniqueidentifier =
                    (SELECT TOP (1) Id FROM [Identity].[Department] WHERE OrganizationId = @OrganizationId AND Code = N'RND');
                DECLARE @QaDepartmentId uniqueidentifier =
                    (SELECT TOP (1) Id FROM [Identity].[Department] WHERE OrganizationId = @OrganizationId AND Code = N'QA');
                DECLARE @SupplyDepartmentId uniqueidentifier =
                    (SELECT TOP (1) Id FROM [Identity].[Department] WHERE OrganizationId = @OrganizationId AND Code = N'SUPPLY');
                DECLARE @FinanceDepartmentId uniqueidentifier =
                    (SELECT TOP (1) Id FROM [Identity].[Department] WHERE OrganizationId = @OrganizationId AND Code = N'FINANCE');

                IF @OrganizationId IS NOT NULL AND @PublisherId IS NOT NULL
                BEGIN
                    INSERT INTO [Workspace].[WorkbenchTemplate]
                        (Id, OrganizationId, DepartmentId, Code, Name, Description, AudienceTitle,
                         PrimaryQuestion, IsSystem, IsDefault, Status, SortOrder, Version,
                         CreatedBy, CreatedAt, UpdatedBy, UpdatedAt)
                    SELECT NEWID(), @OrganizationId, templateSeed.departmentId,
                           templateSeed.code, templateSeed.name, templateSeed.description,
                           templateSeed.audienceTitle, templateSeed.primaryQuestion,
                           1, 1, N'Published', templateSeed.sortOrder, 2,
                           @PublisherId, @Now, @PublisherId, @Now
                    FROM (VALUES
                        (@RndDepartmentId, N'WS-RND', N'研发工作台', N'面向研发岗位的实验、项目、资料与风险首屏。',
                         N'研发岗位', N'哪些实验、资料和任务需要今天处理？', 10),
                        (@QaDepartmentId, N'WS-QA', N'质量工作台', N'面向质量岗位的检测、复核、偏差和 CAPA 首屏。',
                         N'质量岗位', N'哪些检测和异常正在等待判断？', 20),
                        (@SupplyDepartmentId, N'WS-SCM', N'供应链工作台', N'面向供应链岗位的采购、合同、到货和异常首屏。',
                         N'供应链岗位', N'哪些采购和交付事项需要处理？', 30),
                        (@FinanceDepartmentId, N'WS-FIN', N'财务工作台', N'面向财务岗位的付款审批、证据和异常首屏。',
                         N'财务岗位', N'哪些付款和异常正在等待处理？', 40),
                        (CAST(NULL AS uniqueidentifier), N'WS-MGR', N'经理工作台', N'面向部门经理的团队卡点、风险、项目和协调首屏。',
                         N'部门经理', N'团队今天有哪些卡点和风险？', 50),
                        (CAST(NULL AS uniqueidentifier), N'WS-PM', N'项目经理工作台', N'面向项目经理的项目组合、里程碑、任务与 SLA 首屏。',
                         N'项目经理', N'哪些项目有风险或等待验收？', 60)
                    ) templateSeed(departmentId, code, name, description, audienceTitle, primaryQuestion, sortOrder)
                    WHERE NOT EXISTS (
                        SELECT 1 FROM [Workspace].[WorkbenchTemplate] existing
                        WHERE existing.OrganizationId = @OrganizationId
                          AND existing.Code = templateSeed.code
                    );

                    INSERT INTO [Workspace].[WorkbenchTemplateVersion]
                        (Id, TemplateId, VersionNumber, Status, Revision, ValidationJson,
                         ImpactAnalysisJson, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt,
                         PublishedBy, PublishedAt)
                    SELECT NEWID(), templateRow.Id, 1, N'Published', 1,
                           N'{"isValid":true,"errors":[],"warnings":[],"message":"系统模板已发布"}',
                           N'{"cardCount":5,"onlyExistingApis":true}',
                           @PublisherId, @Now, @PublisherId, @Now, @PublisherId, @Now
                    FROM [Workspace].[WorkbenchTemplate] templateRow
                    WHERE templateRow.OrganizationId = @OrganizationId
                      AND templateRow.Code IN (N'WS-RND', N'WS-QA', N'WS-SCM', N'WS-FIN', N'WS-MGR', N'WS-PM')
                      AND NOT EXISTS (
                          SELECT 1 FROM [Workspace].[WorkbenchTemplateVersion] existing
                          WHERE existing.TemplateId = templateRow.Id
                            AND existing.VersionNumber = 1
                      );

                    INSERT INTO [Workspace].[WorkbenchAssignment]
                        (Id, TemplateVersionId, AssignmentType, ScopeValue, Priority, CreatedAt)
                    SELECT NEWID(), versionRow.Id, assignmentSeed.assignmentType,
                           CASE assignmentSeed.assignmentType
                               WHEN N'Department' THEN CONVERT(nvarchar(128), departmentRow.Id)
                               ELSE assignmentSeed.scopeValue
                           END,
                           assignmentSeed.priority, @Now
                    FROM (VALUES
                        (N'WS-RND', N'Department', N'RND', 100),
                        (N'WS-QA', N'Department', N'QA', 100),
                        (N'WS-SCM', N'Department', N'SUPPLY', 100),
                        (N'WS-FIN', N'Department', N'FINANCE', 100),
                        (N'WS-MGR', N'Role', N'platform-admin', 100),
                        (N'WS-PM', N'Role', N'project-manager', 100)
                    ) assignmentSeed(templateCode, assignmentType, scopeValue, priority)
                    JOIN [Workspace].[WorkbenchTemplate] templateRow
                        ON templateRow.OrganizationId = @OrganizationId
                       AND templateRow.Code = assignmentSeed.templateCode
                    JOIN [Workspace].[WorkbenchTemplateVersion] versionRow
                        ON versionRow.TemplateId = templateRow.Id
                       AND versionRow.Status = N'Published'
                    LEFT JOIN [Identity].[Department] departmentRow
                        ON departmentRow.OrganizationId = @OrganizationId
                       AND departmentRow.Code = assignmentSeed.scopeValue
                    WHERE NOT EXISTS (
                        SELECT 1 FROM [Workspace].[WorkbenchAssignment] existing
                        WHERE existing.TemplateVersionId = versionRow.Id
                    );

                    INSERT INTO [Workspace].[WorkbenchCard]
                        (Id, TemplateVersionId, CardType, Title, Description, SortOrder,
                         Width, DataSourceType, FilterJson, RequiredResource, RequiredAction,
                         IsEnabled, CapabilityState, CreatedAt, UpdatedAt)
                    SELECT NEWID(), versionRow.Id, cardSeed.cardType, cardSeed.title,
                           cardSeed.description, cardSeed.sortOrder, cardSeed.width,
                           N'ExistingApi', cardSeed.filterJson, cardSeed.requiredResource, N'Read',
                           1, N'Connected', @Now, @Now
                    FROM (VALUES
                        (N'WS-RND', N'work_items', N'我的研发事项', N'真实流程与项目事项', 10, N'Half', N'{"top":"5"}', N'dashboard'),
                        (N'WS-RND', N'business_objects', N'研发测试业务对象', N'最近更新的研发对象', 20, N'Half', N'{"top":"5"}', N'business-object'),
                        (N'WS-RND', N'project_portfolio', N'研发项目', N'当前可见研发项目', 30, N'Third', N'{"top":"5"}', N'project'),
                        (N'WS-RND', N'minutes', N'最近听记', N'会议行动来源', 40, N'Third', N'{"top":"5"}', N'minutes'),
                        (N'WS-RND', N'recent_files', N'研发文件', N'部门最近证据', 50, N'Third', N'{"top":"5"}', N'file'),
                        (N'WS-RND', N'overdue_risks', N'逾期任务', N'逾期和高风险事项', 60, N'Full', N'{"top":"8"}', N'dashboard'),

                        (N'WS-QA', N'workflow_tasks', N'质量待办', N'与质量相关的流程节点', 10, N'Half', N'{"top":"5","keyword":"质量"}', N'workflow-task'),
                        (N'WS-QA', N'business_objects', N'批次质量偏差', N'最近业务对象', 20, N'Half', N'{"top":"5"}', N'business-object'),
                        (N'WS-QA', N'workflow_tasks', N'待复核和 CAPA', N'复核与改进事项', 30, N'Third', N'{"top":"5","keyword":"CAPA"}', N'workflow-task'),
                        (N'WS-QA', N'recent_files', N'最近质量文件', N'部门证据与报告', 40, N'Third', N'{"top":"5"}', N'file'),
                        (N'WS-QA', N'overdue_risks', N'逾期与高风险', N'需要质量判断的风险', 50, N'Third', N'{"top":"6"}', N'dashboard'),

                        (N'WS-SCM', N'business_objects', N'采购请购', N'采购相关业务对象', 10, N'Half', N'{"top":"5","keyword":"采购"}', N'business-object'),
                        (N'WS-SCM', N'workflow_tasks', N'采购流程待办', N'采购节点与到期事项', 20, N'Half', N'{"top":"5","keyword":"采购"}', N'workflow-task'),
                        (N'WS-SCM', N'recent_files', N'合同/供应商文件', N'部门合同与证据', 30, N'Third', N'{"top":"5"}', N'file'),
                        (N'WS-SCM', N'work_items', N'到货与异常事项', N'交付协同事项', 40, N'Third', N'{"top":"5","keyword":"到货"}', N'dashboard'),
                        (N'WS-SCM', N'overdue_risks', N'逾期风险', N'供应链逾期事项', 50, N'Third', N'{"top":"6"}', N'dashboard'),

                        (N'WS-FIN', N'workflow_tasks', N'采购付款审批', N'付款审批的真实流程任务', 10, N'Half', N'{"top":"5","keyword":"付款"}', N'workflow-task'),
                        (N'WS-FIN', N'business_objects', N'财务相关业务对象', N'当前部门业务对象', 20, N'Half', N'{"top":"5"}', N'business-object'),
                        (N'WS-FIN', N'recent_files', N'合同和发票证据', N'只读证据，不触发付款', 30, N'Third', N'{"top":"5"}', N'file'),
                        (N'WS-FIN', N'work_items', N'异常和待确认', N'真实异常与人工确认', 40, N'Third', N'{"top":"5"}', N'dashboard'),
                        (N'WS-FIN', N'overdue_risks', N'财务逾期与边界', N'高风险需要人工判断', 50, N'Third', N'{"top":"6"}', N'dashboard'),

                        (N'WS-MGR', N'team_pending', N'部门成员待办', N'团队当前待处理事项', 10, N'Half', N'{"top":"8"}', N'workflow-task'),
                        (N'WS-MGR', N'overdue_risks', N'部门逾期风险', N'团队需要升级的卡点', 20, N'Half', N'{"top":"8"}', N'dashboard'),
                        (N'WS-MGR', N'project_portfolio', N'项目组合', N'部门可见项目', 30, N'Third', N'{"top":"6"}', N'project'),
                        (N'WS-MGR', N'work_items', N'跨部门待协调事项', N'需要协调的业务事项', 40, N'Third', N'{"top":"6","keyword":"协调"}', N'dashboard'),
                        (N'WS-MGR', N'recent_files', N'最近文件', N'部门近期证据', 50, N'Third', N'{"top":"5"}', N'file'),

                        (N'WS-PM', N'project_portfolio', N'我负责的项目', N'当前项目经理可见项目', 10, N'Half', N'{"top":"8","projectScope":"owned"}', N'project'),
                        (N'WS-PM', N'project_tasks', N'里程碑和任务风险', N'项目任务与风险', 20, N'Half', N'{"top":"8"}', N'project-task'),
                        (N'WS-PM', N'recent_files', N'项目证据', N'部门文件证据', 30, N'Third', N'{"top":"5"}', N'file'),
                        (N'WS-PM', N'minutes', N'事件和听记来源', N'项目协同来源', 40, N'Third', N'{"top":"5"}', N'minutes'),
                        (N'WS-PM', N'workflow_tasks', N'流程 SLA', N'流程待办与 SLA', 50, N'Third', N'{"top":"8","keyword":"SLA"}', N'workflow-task')
                    ) cardSeed(templateCode, cardType, title, description, sortOrder, width, filterJson, requiredResource)
                    JOIN [Workspace].[WorkbenchTemplate] templateRow
                        ON templateRow.OrganizationId = @OrganizationId
                       AND templateRow.Code = cardSeed.templateCode
                    JOIN [Workspace].[WorkbenchTemplateVersion] versionRow
                        ON versionRow.TemplateId = templateRow.Id
                       AND versionRow.Status = N'Published'
                    WHERE NOT EXISTS (
                        SELECT 1 FROM [Workspace].[WorkbenchCard] existing
                        WHERE existing.TemplateVersionId = versionRow.Id
                    );
                END;

                INSERT INTO [Identity].[Permission] (Id, Code, Resource, Action)
                SELECT permissionSeed.Id, permissionSeed.Code, N'workbench-template', permissionSeed.Action
                FROM (VALUES
                    (CONVERT(uniqueidentifier, '31100000-0000-0000-0000-000000000001'), N'workbench-template-read', N'Read'),
                    (CONVERT(uniqueidentifier, '31100000-0000-0000-0000-000000000002'), N'workbench-template-manage', N'Manage'),
                    (CONVERT(uniqueidentifier, '31100000-0000-0000-0000-000000000003'), N'workbench-template-approve', N'Approve')
                ) permissionSeed(Id, Code, Action)
                WHERE NOT EXISTS (
                    SELECT 1 FROM [Identity].[Permission] existing WHERE existing.Code = permissionSeed.Code
                );

                INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
                SELECT roleRow.Id, permissionRow.Id
                FROM [Identity].[Role] roleRow
                JOIN [Identity].[Permission] permissionRow
                    ON permissionRow.Code = N'workbench-template-read'
                WHERE roleRow.IsActive = 1
                  AND NOT EXISTS (
                      SELECT 1 FROM [Identity].[RolePermission] existing
                      WHERE existing.RoleId = roleRow.Id AND existing.PermissionId = permissionRow.Id
                  );

                INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
                SELECT roleRow.Id, permissionRow.Id
                FROM [Identity].[Role] roleRow
                JOIN [Identity].[Permission] permissionRow
                    ON permissionRow.Code IN (N'workbench-template-manage', N'workbench-template-approve')
                WHERE roleRow.IsActive = 1
                  AND roleRow.Code IN (
                      N'platform-admin', N'project-manager', N'quality-engineer',
                      N'supply-specialist', N'finance-approver'
                  )
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
                """
                DELETE rolePermission
                FROM [Identity].[RolePermission] rolePermission
                JOIN [Identity].[Permission] permissionRow ON permissionRow.Id = rolePermission.PermissionId
                WHERE permissionRow.Code IN (
                    N'workbench-template-read',
                    N'workbench-template-manage',
                    N'workbench-template-approve'
                );

                DELETE FROM [Identity].[Permission]
                WHERE Code IN (
                    N'workbench-template-read',
                    N'workbench-template-manage',
                    N'workbench-template-approve'
                );
                """);

            migrationBuilder.DropTable(
                name: "WorkbenchAssignment",
                schema: "Workspace");

            migrationBuilder.DropTable(
                name: "WorkbenchCard",
                schema: "Workspace");

            migrationBuilder.DropTable(
                name: "WorkbenchTemplateVersion",
                schema: "Workspace");

            migrationBuilder.DropTable(
                name: "WorkbenchTemplate",
                schema: "Workspace");
        }
    }
}
