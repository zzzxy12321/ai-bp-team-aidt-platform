using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIDT.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WorkflowTaskDepartmentBackfillCorrection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE workflowTask
                SET DepartmentId = assigneeDepartment.Id
                FROM [Workflow].[WorkflowTask] workflowTask
                OUTER APPLY (
                    SELECT TOP (1) departmentRow.Id
                    FROM [Identity].[UserDepartmentMembership] membershipRow
                    JOIN [Identity].[Department] departmentRow
                        ON departmentRow.Id = membershipRow.DepartmentId
                    WHERE membershipRow.UserId = workflowTask.AssigneeId
                      AND membershipRow.IsActive = 1
                      AND departmentRow.IsActive = 1
                    ORDER BY membershipRow.IsPrimary DESC, membershipRow.JoinedAt
                ) assigneeDepartment
                WHERE assigneeDepartment.Id IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This data correction is intentionally not reversed.
        }
    }
}
