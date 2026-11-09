using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Application.UseCases;
using AIDT.Domain.Agents;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;
using NSubstitute;
using Shouldly;

namespace AIDT.UnitTests.Application;

public sealed class ConfirmAgentRunHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCallerIsServiceIdentity_IsRejectedBeforeRepositoryRead()
    {
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetRequiredUser().Returns(
            TestUsers.Authorized(
                isServiceIdentity: true,
                permissions: new PermissionGrant("agent-run", AccessAction.Approve)));
        var agents = Substitute.For<IAgentRepository>();
        var handler = new ConfirmAgentRunHandler(
            currentUser,
            agents,
            Substitute.For<IAuditRepository>(),
            Substitute.For<IUnitOfWork>(),
            Substitute.For<IClock>());

        var exception = await Should.ThrowAsync<PermissionException>(
            () => handler.HandleAsync(
                Guid.NewGuid(),
                new ConfirmAgentRunRequest(Guid.NewGuid()),
                "trace-agent-001",
                CancellationToken.None));

        exception.ErrorCode.ShouldBe(ErrorCodes.PermissionDenied);
        await agents.DidNotReceive().GetRunAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
