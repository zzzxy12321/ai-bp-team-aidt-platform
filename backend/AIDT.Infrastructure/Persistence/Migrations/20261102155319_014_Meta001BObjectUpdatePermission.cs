using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIDT.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Meta001BObjectUpdateGrant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
                SELECT roleRow.Id, permissionRow.Id
                FROM [Identity].[Role] roleRow
                CROSS JOIN [Identity].[Permission] permissionRow
                WHERE roleRow.Code IN (
                    N'rd-engineer',
                    N'quality-engineer',
                    N'supply-specialist',
                    N'finance-approver',
                    N'production-team',
                    N'project-manager')
                  AND permissionRow.Code = N'business-object-update'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM [Identity].[RolePermission] existing
                      WHERE existing.RoleId = roleRow.Id
                        AND existing.PermissionId = permissionRow.Id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE rolePermission
                FROM [Identity].[RolePermission] rolePermission
                INNER JOIN [Identity].[Role] roleRow
                    ON roleRow.Id = rolePermission.RoleId
                INNER JOIN [Identity].[Permission] permissionRow
                    ON permissionRow.Id = rolePermission.PermissionId
                WHERE roleRow.Code IN (
                    N'rd-engineer',
                    N'quality-engineer',
                    N'supply-specialist',
                    N'finance-approver',
                    N'production-team',
                    N'project-manager')
                  AND permissionRow.Code = N'business-object-update';
                """);
        }
    }
}
