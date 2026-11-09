using AIDT.Domain.Common;
using AIDT.Domain.Workflows;
using Shouldly;

namespace AIDT.UnitTests.Domain;

public sealed class WorkflowTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 1, 0, 0, TimeSpan.Zero);
    private static readonly Guid DepartmentId = Guid.Parse("51000000-0000-0000-0000-000000000001");

    [Fact]
    public void WorkflowInstance_Start_FixesDefinitionAndVersionIdentifiers()
    {
        var definitionId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var objectId = Guid.NewGuid();

        var instance = WorkflowInstance.Start(
            definitionId,
            versionId,
            objectId,
            Now,
            departmentId: DepartmentId,
            environmentCode: "test");

        instance.WorkflowDefinitionId.ShouldBe(definitionId);
        instance.WorkflowVersionId.ShouldBe(versionId);
        instance.BusinessObjectId.ShouldBe(objectId);
        instance.Status.ShouldBe(WorkflowInstanceStatus.Running);
    }

    [Fact]
    public void ReturnTask_WithoutReason_IsRejected()
    {
        var assigneeId = Guid.NewGuid();
        var task = WorkflowTask.Create(
            Guid.NewGuid(),
            "REVIEW",
            assigneeId,
            Now,
            DepartmentId,
            "test");

        var exception = Should.Throw<ValidationException>(
            () => task.Decide(assigneeId, WorkflowDecision.Return, " ", 1, Now));

        exception.ErrorCode.ShouldBe(ErrorCodes.ValidationFailed);
        task.Status.ShouldBe(WorkflowTaskStatus.Pending);
    }

    [Fact]
    public void Decide_WhenTaskAlreadyProcessed_ReturnsStableConflictCode()
    {
        var assigneeId = Guid.NewGuid();
        var task = WorkflowTask.Create(
            Guid.NewGuid(),
            "REVIEW",
            assigneeId,
            Now,
            DepartmentId,
            "test");
        task.Decide(assigneeId, WorkflowDecision.Approve, "同意", 1, Now);

        var exception = Should.Throw<ConflictException>(
            () => task.Decide(assigneeId, WorkflowDecision.Approve, "重复", 2, Now));

        exception.ErrorCode.ShouldBe(ErrorCodes.TaskAlreadyProcessed);
    }

    [Fact]
    public void Complete_WithTerminate_EndsInstanceAndPreventsFurtherAdvance()
    {
        var instance = WorkflowInstance.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now,
            departmentId: DepartmentId,
            environmentCode: "test");
        instance.Complete(WorkflowDecision.Terminate, 1, Now.AddMinutes(1));

        instance.Status.ShouldBe(WorkflowInstanceStatus.Terminated);
        Should.Throw<ConflictException>(
                () => instance.Complete(WorkflowDecision.Approve, instance.Version, Now.AddMinutes(2)))
            .ErrorCode.ShouldBe(ErrorCodes.WorkflowStateInvalid);
    }
}
