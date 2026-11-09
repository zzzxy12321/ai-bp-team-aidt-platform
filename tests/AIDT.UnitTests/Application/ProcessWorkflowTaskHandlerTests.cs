using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Application.UseCases;
using AIDT.Domain.BusinessObjects;
using AIDT.Domain.Identity;
using AIDT.Domain.Workflows;
using NSubstitute;
using Shouldly;
using AuditEvent = AIDT.Domain.Audit.AuditEvent;
using OutboxMessage = AIDT.Domain.Integrations.OutboxMessage;

namespace AIDT.UnitTests.Application;

public sealed class ProcessWorkflowTaskHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 2, 0, 0, TimeSpan.Zero);
    private static readonly Guid DepartmentId = Guid.Parse("51000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task HandleAsync_Approve_CreatesNextTaskAndCommits()
    {
        var userId = Guid.NewGuid();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetRequiredUser().Returns(
            TestUsers.Authorized(
                userId,
                permissions:
                [
                    new PermissionGrant("workflow-task", AccessAction.Approve)
                ]));
        var instance = WorkflowInstance.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now,
            departmentId: DepartmentId,
            environmentCode: "test");
        var task = WorkflowTask.Create(
            instance.Id,
            "REVIEW",
            userId,
            Now,
            DepartmentId,
            "test");
        var workflows = Substitute.For<IWorkflowRepository>();
        workflows.GetTaskAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        workflows.GetInstanceAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);
        var businessObjects = Substitute.For<IBusinessObjectRepository>();
        businessObjects.GetByIdAsync(instance.BusinessObjectId, Arg.Any<CancellationToken>())
            .Returns(BusinessObject.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "SAMPLE-001",
                new Dictionary<string, string?>(),
                Now,
                DepartmentId,
                "test"));
        var departments = Substitute.For<IDepartmentRepository>();
        departments.ResolveAssigneeDepartmentAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(DepartmentId);
        var engine = Substitute.For<IWorkflowEngine>();
        var nextAssigneeId = Guid.NewGuid();
        var assignment = new ResolvedWorkflowAssignment(
            nextAssigneeId,
            [nextAssigneeId],
            WorkflowAssignmentStrategy.SpecificUser,
            null,
            null,
            null,
            null,
            """{"strategy":"SpecificUser"}""");
        engine.AdvanceAsync(instance, task, WorkflowDecision.Approve, Arg.Any<CancellationToken>())
            .Returns(new WorkflowAdvanceResult(
                "QA_REVIEW",
                nextAssigneeId,
                WorkflowInstanceStatus.Waiting,
                assignment));
        var transaction = Substitute.For<IApplicationTransaction>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(transaction);
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(Now);
        var audits = Substitute.For<IAuditRepository>();
        var outbox = Substitute.For<IOutboxRepository>();
        var handler = new ProcessWorkflowTaskHandler(
            currentUser,
            workflows,
            businessObjects,
            departments,
            engine,
            audits,
            outbox,
            Substitute.For<IIdempotencyStore>(),
            CreateFormValidator(),
            unitOfWork,
            clock);

        var result = await handler.HandleAsync(
            task.Id,
            new TaskDecisionRequest(
                WorkflowDecision.Approve,
                1,
                "同意",
                "idempotency-task-001"),
            "trace-task-001",
            CancellationToken.None);

        result.Status.ShouldBe(WorkflowTaskStatus.Approved);
        await workflows.Received(1).AddTaskAsync(
            Arg.Is<WorkflowTask>(next =>
                next.NodeCode == "QA_REVIEW"
                && next.DepartmentId == DepartmentId),
            Arg.Any<CancellationToken>());
        await audits.Received(2).AddAsync(
            Arg.Any<AuditEvent>(),
            Arg.Any<CancellationToken>());
        await outbox.Received(1).AddAsync(
            Arg.Any<OutboxMessage>(),
            Arg.Any<CancellationToken>());
        await transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Return_ClosesInstanceWithoutCallingWorkflowEngine()
    {
        var userId = Guid.NewGuid();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetRequiredUser().Returns(
            TestUsers.Authorized(
                userId,
                permissions:
                [
                    new PermissionGrant("workflow-task", AccessAction.Return)
                ]));
        var instance = WorkflowInstance.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now,
            departmentId: DepartmentId,
            environmentCode: "test");
        var task = WorkflowTask.Create(
            instance.Id,
            "REVIEW",
            userId,
            Now,
            DepartmentId,
            "test");
        var workflows = Substitute.For<IWorkflowRepository>();
        workflows.GetTaskAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        workflows.GetInstanceAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);
        var businessObjects = Substitute.For<IBusinessObjectRepository>();
        businessObjects.GetByIdAsync(instance.BusinessObjectId, Arg.Any<CancellationToken>())
            .Returns(BusinessObject.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "SAMPLE-002",
                new Dictionary<string, string?>(),
                Now,
                DepartmentId,
                "test"));
        var transaction = Substitute.For<IApplicationTransaction>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(transaction);
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(Now);
        var engine = Substitute.For<IWorkflowEngine>();
        var handler = new ProcessWorkflowTaskHandler(
            currentUser,
            workflows,
            businessObjects,
            Substitute.For<IDepartmentRepository>(),
            engine,
            Substitute.For<IAuditRepository>(),
            Substitute.For<IOutboxRepository>(),
            Substitute.For<IIdempotencyStore>(),
            CreateFormValidator(),
            unitOfWork,
            clock);

        var result = await handler.HandleAsync(
            task.Id,
            new TaskDecisionRequest(
                WorkflowDecision.Return,
                1,
                "资料不完整",
                "idempotency-task-002"),
            "trace-task-002",
            CancellationToken.None);

        result.Status.ShouldBe(WorkflowTaskStatus.Returned);
        instance.Status.ShouldBe(WorkflowInstanceStatus.Returned);
        await engine.DidNotReceive().AdvanceAsync(
            Arg.Any<WorkflowInstance>(),
            Arg.Any<WorkflowTask>(),
            Arg.Any<WorkflowDecision>(),
            Arg.Any<CancellationToken>());
    }

    private static IWorkflowTaskFormValidator CreateFormValidator()
    {
        var validator = Substitute.For<IWorkflowTaskFormValidator>();
        validator.ValidateAsync(
                Arg.Any<UserPrincipal>(),
                Arg.Any<WorkflowTask>(),
                Arg.Any<string?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(new WorkflowTaskFormValidationDto(
                true,
                string.Empty,
                [],
                "{}",
                []));
        return validator;
    }
}
