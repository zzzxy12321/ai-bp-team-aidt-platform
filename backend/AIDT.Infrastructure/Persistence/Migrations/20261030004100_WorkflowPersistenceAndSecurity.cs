using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIDT.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WorkflowPersistenceAndSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                schema: "BusinessObject",
                table: "BusinessObject",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "IdempotencyRecord",
                schema: "Integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ResponseType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ResponseId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecord", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessObject_Organization_UpdatedAt_Id",
                schema: "BusinessObject",
                table: "BusinessObject",
                columns: new[] { "OrganizationId", "UpdatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecord_ExpiresAt",
                schema: "Integration",
                table: "IdempotencyRecord",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "UX_IdempotencyRecord_Scope_Key",
                schema: "Integration",
                table: "IdempotencyRecord",
                columns: new[] { "Scope", "IdempotencyKey" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessObject_Organization",
                schema: "BusinessObject",
                table: "BusinessObject",
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
                name: "FK_BusinessObject_Organization",
                schema: "BusinessObject",
                table: "BusinessObject");

            migrationBuilder.DropTable(
                name: "IdempotencyRecord",
                schema: "Integration");

            migrationBuilder.DropIndex(
                name: "IX_BusinessObject_Organization_UpdatedAt_Id",
                schema: "BusinessObject",
                table: "BusinessObject");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                schema: "BusinessObject",
                table: "BusinessObject");
        }
    }
}
