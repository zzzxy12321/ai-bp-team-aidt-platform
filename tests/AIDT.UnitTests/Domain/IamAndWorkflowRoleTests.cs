using AIDT.Domain.Common;
using AIDT.Domain.Identity;
using AIDT.Domain.Workflows;
using Shouldly;

namespace AIDT.UnitTests.Domain;

public sealed class IamAndWorkflowRoleTests
{
    [Fact]
    public void RoleAssignment_IsQualifiedOnlyInsideConfiguredScope()
    {
        var organizationId = Guid.NewGuid();
        var researchDepartmentId = Guid.NewGuid();
        var qualityDepartmentId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var user = new UserPrincipal(
            Guid.NewGuid(),
            organizationId,
            false,
            new HashSet<string>(["business-manager-reviewer"], StringComparer.Ordinal),
            [],
            [
                DataScope.Organization(organizationId),
                DataScope.Department(researchDepartmentId),
                DataScope.Department(qualityDepartmentId)
            ])
        {
            RoleAssignments =
            [
                new RoleAssignment(
                    roleId,
                    "business-manager-reviewer",
                    "department",
                    researchDepartmentId.ToString("D"))
            ]
        };

        user.HasRoleInScope("business-manager-reviewer", researchDepartmentId).ShouldBeTrue();
        user.HasRoleInScope("business-manager-reviewer", qualityDepartmentId).ShouldBeFalse();
    }

    [Fact]
    public void WorkflowTask_TransferPreservesOriginalAssigneeAndFrozenCandidates()
    {
        var now = DateTimeOffset.UtcNow;
        var original = Guid.NewGuid();
        var backup = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var task = WorkflowTask.Create(
            Guid.NewGuid(),
            "MANAGER_REVIEW",
            original,
            now,
            Guid.NewGuid(),
            "demo",
            true,
            assigneeStrategy: WorkflowAssignmentStrategy.BusinessRole,
            businessRoleId: Guid.NewGuid(),
            businessRoleCode: "business-manager-reviewer",
            businessRoleVersion: 3,
            candidateUserIds: [original, backup],
            assigneeSnapshotJson: """{"strategy":"BusinessRole"}""",
            backupAssigneeId: backup);

        task.Transfer(actor, backup, backup, "项目经理请假", task.Version, now.AddMinutes(1));

        task.AssigneeId.ShouldBe(backup);
        task.OriginalAssigneeId.ShouldBe(original);
        task.TransferredBy.ShouldBe(actor);
        task.TransferReason.ShouldBe("项目经理请假");
        task.CandidateUserIds.ShouldBe([original, backup]);
        task.Version.ShouldBe(2);
    }

    [Fact]
    public void WorkflowTask_TransferRequiresCurrentVersion()
    {
        var now = DateTimeOffset.UtcNow;
        var task = WorkflowTask.Create(
            Guid.NewGuid(),
            "MANAGER_REVIEW",
            Guid.NewGuid(),
            now,
            Guid.NewGuid());

        var exception = Should.Throw<ConflictException>(() =>
            task.Transfer(
                task.AssigneeId,
                Guid.NewGuid(),
                null,
                "转交",
                99,
                now));

        exception.ErrorCode.ShouldBe(ErrorCodes.WorkflowVersionConflict);
    }
}
