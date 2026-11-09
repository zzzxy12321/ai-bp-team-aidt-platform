using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Application.UseCases;
using AIDT.Domain.BusinessObjects;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;
using AIDT.Domain.Integrations;
using NSubstitute;
using Shouldly;
using AuditEvent = AIDT.Domain.Audit.AuditEvent;
using OutboxMessage = AIDT.Domain.Integrations.OutboxMessage;

namespace AIDT.UnitTests.Application;

public sealed class CreateBusinessObjectHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 2, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_WithAuthorizedUser_WritesObjectAuditAndOutboxInOneTransaction()
    {
        var objectType = ObjectType.Create(
            "CUSTOMER_SAMPLE",
            "客户样品",
            [new ObjectFieldDefinition("Customer", "客户", "string", true, 100, FieldSensitivity.Sensitive)]);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetRequiredUser().Returns(
            TestUsers.Authorized(permissions: new PermissionGrant("business-object", AccessAction.Create)));
        var objectTypes = Substitute.For<IObjectTypeRepository>();
        objectTypes.GetPublishedByIdAsync(objectType.Id, Arg.Any<CancellationToken>()).Returns(objectType);
        var departmentId = Guid.NewGuid();
        var departments = Substitute.For<IDepartmentRepository>();
        departments.ResolveIdAsync(
                Arg.Any<UserPrincipal>(),
                Arg.Any<Guid?>(),
                objectType.Code,
                Arg.Any<CancellationToken>())
            .Returns(departmentId);
        var objects = Substitute.For<IBusinessObjectRepository>();
        objects.ExistsCodeAsync(objectType.Id, "SAMPLE-001", Arg.Any<CancellationToken>()).Returns(false);
        var audits = Substitute.For<IAuditRepository>();
        var outbox = Substitute.For<IOutboxRepository>();
        var idempotency = Substitute.For<IIdempotencyStore>();
        var transaction = Substitute.For<IApplicationTransaction>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(transaction);
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(Now);

        var handler = new CreateBusinessObjectHandler(
            currentUser,
            objectTypes,
            departments,
            objects,
            audits,
            outbox,
            idempotency,
            unitOfWork,
            new DataEnvironmentOptions(),
            clock);

        var result = await handler.HandleAsync(
            new CreateBusinessObjectRequest(
                objectType.Id,
                "SAMPLE-001",
                new Dictionary<string, string?> { ["Customer"] = "测试客户" }),
            "trace-001",
            "idempotency-create-001",
            CancellationToken.None);

        result.ObjectCode.ShouldBe("SAMPLE-001");
        result.Version.ShouldBe(1);
        await objects.Received(1).AddAsync(
            Arg.Any<BusinessObject>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
        await audits.Received(1).AddAsync(Arg.Any<AuditEvent>(), Arg.Any<CancellationToken>());
        await outbox.Received(1).AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        await transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenPermissionMissing_DoesNotReadOrWritePersistence()
    {
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetRequiredUser().Returns(TestUsers.WithoutPermissions());
        var objectTypes = Substitute.For<IObjectTypeRepository>();
        var departments = Substitute.For<IDepartmentRepository>();
        var objects = Substitute.For<IBusinessObjectRepository>();
        var handler = new CreateBusinessObjectHandler(
            currentUser,
            objectTypes,
            departments,
            objects,
            Substitute.For<IAuditRepository>(),
            Substitute.For<IOutboxRepository>(),
            Substitute.For<IIdempotencyStore>(),
            Substitute.For<IUnitOfWork>(),
            new DataEnvironmentOptions(),
            Substitute.For<IClock>());

        var exception = await Should.ThrowAsync<PermissionException>(
            () => handler.HandleAsync(
                new CreateBusinessObjectRequest(
                    Guid.NewGuid(),
                    "SAMPLE-001",
                    new Dictionary<string, string?>()),
                "trace-002",
                "idempotency-create-002",
                CancellationToken.None));

        exception.ErrorCode.ShouldBe(ErrorCodes.PermissionDenied);
        await objectTypes.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await objects.DidNotReceive().AddAsync(
            Arg.Any<BusinessObject>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenObjectCodeExists_ReturnsDuplicateConflict()
    {
        var objectType = ObjectType.Create(
            "CUSTOMER_SAMPLE",
            "客户样品",
            [new ObjectFieldDefinition("Customer", "客户", "string", true, 100, FieldSensitivity.Sensitive)]);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetRequiredUser().Returns(
            TestUsers.Authorized(permissions: new PermissionGrant("business-object", AccessAction.Create)));
        var objectTypes = Substitute.For<IObjectTypeRepository>();
        objectTypes.GetPublishedByIdAsync(objectType.Id, Arg.Any<CancellationToken>()).Returns(objectType);
        var departments = Substitute.For<IDepartmentRepository>();
        departments.ResolveIdAsync(
                Arg.Any<UserPrincipal>(),
                Arg.Any<Guid?>(),
                objectType.Code,
                Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());
        var objects = Substitute.For<IBusinessObjectRepository>();
        objects.ExistsCodeAsync(objectType.Id, "SAMPLE-001", Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CreateBusinessObjectHandler(
            currentUser,
            objectTypes,
            departments,
            objects,
            Substitute.For<IAuditRepository>(),
            Substitute.For<IOutboxRepository>(),
            Substitute.For<IIdempotencyStore>(),
            Substitute.For<IUnitOfWork>(),
            new DataEnvironmentOptions(),
            Substitute.For<IClock>());

        var exception = await Should.ThrowAsync<ConflictException>(
            () => handler.HandleAsync(
                new CreateBusinessObjectRequest(
                    objectType.Id,
                    "SAMPLE-001",
                    new Dictionary<string, string?>()),
                "trace-003",
                "idempotency-create-003",
                CancellationToken.None));

        exception.ErrorCode.ShouldBe(ErrorCodes.DuplicateObjectCode);
    }
}
