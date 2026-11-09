using AIDT.Domain.BusinessObjects;
using AIDT.Domain.Common;
using Shouldly;

namespace AIDT.UnitTests.Domain;

public sealed class BusinessObjectTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 1, 0, 0, TimeSpan.Zero);
    private static readonly Guid DepartmentId = Guid.Parse("51000000-0000-0000-0000-000000000001");

    [Fact]
    public void UpdateFields_WithCurrentVersion_IncrementsVersionAndKeepsPrimaryIdentity()
    {
        var businessObject = BusinessObject.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "LOT-2026-001",
            new Dictionary<string, string?> { ["Customer"] = "A" },
            Now,
            DepartmentId,
            "test");

        businessObject.UpdateFields(
            new Dictionary<string, string?> { ["Customer"] = "B" },
            1,
            Now.AddMinutes(1));

        businessObject.Id.ShouldNotBe(Guid.Empty);
        businessObject.Version.ShouldBe(2);
        businessObject.Fields["Customer"].ShouldBe("B");
    }

    [Fact]
    public void UpdateFields_WithStaleVersion_ThrowsConflict()
    {
        var businessObject = BusinessObject.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "LOT-2026-001",
            new Dictionary<string, string?>(),
            Now,
            DepartmentId,
            "test");
        businessObject.UpdateFields(new Dictionary<string, string?>(), 1, Now.AddMinutes(1));

        var exception = Should.Throw<ConflictException>(
            () => businessObject.UpdateFields(new Dictionary<string, string?>(), 1, Now.AddMinutes(2)));

        exception.ErrorCode.ShouldBe(ErrorCodes.Conflict);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_IsIdempotent()
    {
        var businessObject = BusinessObject.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "LOT-2026-001",
            new Dictionary<string, string?>(),
            Now,
            DepartmentId,
            "test");
        businessObject.Archive(1, Now);

        businessObject.Archive(2, Now.AddMinutes(1));

        businessObject.Status.ShouldBe(BusinessObjectStatus.Archived);
        businessObject.Version.ShouldBe(2);
    }
}
