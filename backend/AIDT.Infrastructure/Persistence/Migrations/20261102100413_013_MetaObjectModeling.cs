using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIDT.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MetaObjectModeling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                schema: "Metadata",
                table: "ObjectType",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RetiredAt",
                schema: "Metadata",
                table: "ObjectType",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Revision",
                schema: "Metadata",
                table: "ObjectType",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "Metadata",
                table: "ObjectType",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: Array.Empty<byte>());

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "Metadata",
                table: "ObjectType",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Published");

            migrationBuilder.AddColumn<string>(
                name: "DefaultValue",
                schema: "Metadata",
                table: "FieldDefinition",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Filterable",
                schema: "Metadata",
                table: "FieldDefinition",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OptionsJson",
                schema: "Metadata",
                table: "FieldDefinition",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowInList",
                schema: "Metadata",
                table: "FieldDefinition",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                schema: "Metadata",
                table: "FieldDefinition",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ObjectTypeVersion",
                schema: "Metadata",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ObjectTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FieldsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValidatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectTypeVersion", x => x.Id);
                    table.CheckConstraint("CK_ObjectTypeVersion_FieldsJson", "ISJSON([FieldsJson]) = 1");
                    table.CheckConstraint("CK_ObjectTypeVersion_Status", "[Status] IN ('Draft','Validated','Published','Retired','Superseded')");
                    table.ForeignKey(
                        name: "FK_ObjectTypeVersion_ObjectType",
                        column: x => x.ObjectTypeId,
                        principalSchema: "Metadata",
                        principalTable: "ObjectType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ObjectType_Status_UpdatedAt",
                schema: "Metadata",
                table: "ObjectType",
                columns: new[] { "Status", "UpdatedAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ObjectType_Status",
                schema: "Metadata",
                table: "ObjectType",
                sql: "[Status] IN ('Draft','Validated','Published','Retired','Superseded')");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectTypeVersion_Type_Status_Version",
                schema: "Metadata",
                table: "ObjectTypeVersion",
                columns: new[] { "ObjectTypeId", "Status", "Version" });

            migrationBuilder.CreateIndex(
                name: "UX_ObjectTypeVersion_Type_Version",
                schema: "Metadata",
                table: "ObjectTypeVersion",
                columns: new[] { "ObjectTypeId", "Version" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO [Identity].[Permission] (Id, Code, Resource, Action)
                SELECT NEWID(), N'object-model-' + actionRow.[ActionCode],
                       N'object-model', actionRow.[ActionName]
                FROM (VALUES
                    (N'read', N'Read'),
                    (N'create', N'Create'),
                    (N'update', N'Update'),
                    (N'manage', N'Manage'),
                    (N'approve', N'Approve')
                ) AS actionRow([ActionCode], [ActionName])
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM [Identity].[Permission] existing
                    WHERE existing.Resource = N'object-model'
                      AND existing.Action = actionRow.[ActionName]);

                INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
                SELECT roleRow.Id, permissionRow.Id
                FROM [Identity].[Role] roleRow
                CROSS JOIN [Identity].[Permission] permissionRow
                WHERE roleRow.Code = N'platform-admin'
                  AND permissionRow.Resource = N'object-model'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM [Identity].[RolePermission] existing
                      WHERE existing.RoleId = roleRow.Id
                        AND existing.PermissionId = permissionRow.Id);

                INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
                SELECT roleRow.Id, permissionRow.Id
                FROM [Identity].[Role] roleRow
                CROSS JOIN [Identity].[Permission] permissionRow
                WHERE roleRow.Code = N'platform-admin'
                  AND permissionRow.Resource = N'business-object'
                  AND permissionRow.Action = N'Update'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM [Identity].[RolePermission] existing
                      WHERE existing.RoleId = roleRow.Id
                        AND existing.PermissionId = permissionRow.Id);

                UPDATE [Metadata].[ObjectType]
                SET [Status] = N'Published',
                    [Revision] = CASE WHEN [Revision] < 1 THEN 1 ELSE [Revision] END,
                    [PublishedAt] = COALESCE([PublishedAt], [UpdatedAt])
                WHERE [Status] = N'Published';

                INSERT INTO [Metadata].[ObjectTypeVersion]
                    ([ObjectTypeId], [Version], [Status], [Name], [FieldsJson],
                     [ValidatedAt], [PublishedAt], [RetiredAt], [CreatedBy], [UpdatedBy],
                     [CreatedAt], [UpdatedAt])
                SELECT objectType.[Id], objectType.[Version], N'Published', objectType.[Name],
                       COALESCE((
                           SELECT
                               field.[Key] AS [key],
                               field.[DisplayName] AS [displayName],
                               CASE field.[DataType]
                                   WHEN N'string' THEN N'Text'
                                   WHEN N'text' THEN N'LongText'
                                   WHEN N'decimal' THEN N'Number'
                                   ELSE field.[DataType]
                               END AS [dataType],
                               field.[IsRequired] AS [isRequired],
                               field.[MaxLength] AS [maxLength],
                               field.[Sensitivity] AS [sensitivity],
                               field.[DefaultValue] AS [defaultValue],
                               CASE
                                   WHEN field.[OptionsJson] IS NULL THEN JSON_QUERY(N'[]')
                                   ELSE JSON_QUERY(field.[OptionsJson])
                               END AS [options],
                               field.[ShowInList] AS [showInList],
                               field.[Filterable] AS [filterable],
                               field.[SortOrder] AS [sortOrder],
                               field.[Key] AS [code]
                           FROM [Metadata].[FieldDefinition] field
                           WHERE field.[ObjectTypeId] = objectType.[Id]
                           ORDER BY field.[SortOrder], field.[Key]
                           FOR JSON PATH
                       ), N'[]'),
                       objectType.[UpdatedAt], objectType.[UpdatedAt], NULL, NULL, NULL,
                       objectType.[CreatedAt], objectType.[UpdatedAt]
                FROM [Metadata].[ObjectType] objectType
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM [Metadata].[ObjectTypeVersion] version
                    WHERE version.[ObjectTypeId] = objectType.[Id]
                      AND version.[Version] = objectType.[Version]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE rolePermission
                FROM [Identity].[RolePermission] rolePermission
                INNER JOIN [Identity].[Permission] permissionRow
                    ON permissionRow.[Id] = rolePermission.[PermissionId]
                WHERE permissionRow.[Resource] = N'object-model';

                DELETE FROM [Identity].[Permission]
                WHERE [Resource] = N'object-model';
                """);

            migrationBuilder.DropTable(
                name: "ObjectTypeVersion",
                schema: "Metadata");

            migrationBuilder.DropIndex(
                name: "IX_ObjectType_Status_UpdatedAt",
                schema: "Metadata",
                table: "ObjectType");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ObjectType_Status",
                schema: "Metadata",
                table: "ObjectType");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                schema: "Metadata",
                table: "ObjectType");

            migrationBuilder.DropColumn(
                name: "RetiredAt",
                schema: "Metadata",
                table: "ObjectType");

            migrationBuilder.DropColumn(
                name: "Revision",
                schema: "Metadata",
                table: "ObjectType");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "Metadata",
                table: "ObjectType");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "Metadata",
                table: "ObjectType");

            migrationBuilder.DropColumn(
                name: "DefaultValue",
                schema: "Metadata",
                table: "FieldDefinition");

            migrationBuilder.DropColumn(
                name: "Filterable",
                schema: "Metadata",
                table: "FieldDefinition");

            migrationBuilder.DropColumn(
                name: "OptionsJson",
                schema: "Metadata",
                table: "FieldDefinition");

            migrationBuilder.DropColumn(
                name: "ShowInList",
                schema: "Metadata",
                table: "FieldDefinition");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                schema: "Metadata",
                table: "FieldDefinition");
        }
    }
}
