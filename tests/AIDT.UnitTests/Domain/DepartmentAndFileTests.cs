using AIDT.Domain.Common;
using AIDT.Domain.Documents;
using AIDT.Domain.Identity;
using Shouldly;

namespace AIDT.UnitTests.Domain;

public sealed class DepartmentAndFileTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 31, 2, 0, 0, TimeSpan.Zero);
    private static readonly Guid OrganizationId =
        Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid DepartmentId =
        Guid.Parse("51000000-0000-0000-0000-000000000001");

    [Fact]
    public void Department_Create_NormalizesStableCode()
    {
        var department = Department.Create(OrganizationId, "RND", "研发", null);

        department.Code.ShouldBe("RND");
        department.Name.ShouldBe("研发");
        department.IsActive.ShouldBeTrue();
        Should.Throw<ValidationException>(() => Department.Create(OrganizationId, "rnd", "研发", null))
            .ErrorCode.ShouldBe(ErrorCodes.ValidationFailed);
    }

    [Theory]
    [InlineData("demo")]
    [InlineData("TEST")]
    [InlineData("prod")]
    public void DataEnvironment_NormalizesSupportedCodes(string value)
    {
        DataEnvironment.Normalize(value).ShouldBe(value.ToLowerInvariant());
    }

    [Fact]
    public void DataEnvironment_RejectsUnknownCode()
    {
        Should.Throw<ValidationException>(() => DataEnvironment.Normalize("staging"))
            .ErrorCode.ShouldBe(ErrorCodes.ValidationFailed);
    }

    [Fact]
    public void FileAttachment_DeletionRequestPreservesRecordAndBlocksNewVersions()
    {
        var attachment = CreateAttachment(FileSensitivityLevel.Sensitive);

        attachment.RequestDeletion(Guid.NewGuid(), "客户要求撤回错误附件", Now.AddMinutes(1));

        attachment.Status.ShouldBe(FileRecordStatus.DeletionRequested);
        attachment.ScanStatus.ShouldBe(FileScanStatus.PendingManualReview);
        attachment.DeletionReason.ShouldBe("客户要求撤回错误附件");
        attachment.CurrentVersionNumber.ShouldBe(1);
        Should.Throw<ConflictException>(() => attachment.AddCurrentVersion(
                "new.pdf",
                "application/pdf",
                10,
                new string('B', 64),
                "test/new.pdf",
                FileScanStatus.NotConfigured,
                FileSensitivityLevel.Internal,
                Now.AddMinutes(2)))
            .ErrorCode.ShouldBe(ErrorCodes.Conflict);
    }

    [Fact]
    public void FileAttachment_AddVersion_IncrementsAndKeepsCurrentMetadata()
    {
        var attachment = CreateAttachment(FileSensitivityLevel.Internal);

        attachment.AddCurrentVersion(
            "spec-v2.pdf",
            "application/pdf",
            2048,
            new string('C', 64),
            "demo/department/2026/10/guid.pdf",
            FileScanStatus.NotConfigured,
            FileSensitivityLevel.Internal,
            Now.AddMinutes(1));

        attachment.CurrentVersionNumber.ShouldBe(2);
        attachment.OriginalFileName.ShouldBe("spec-v2.pdf");
        attachment.SizeBytes.ShouldBe(2048);
    }

    [Fact]
    public void FileAttachment_InfectedScanStatus_IsRejected()
    {
        Should.Throw<ValidationException>(() => FileAttachment.Create(
                OrganizationId,
                DepartmentId,
                null,
                "test",
                isTestData: true,
                sourceSystem: "AIDT",
                originalFileName: "infected.pdf",
                mimeType: "application/pdf",
                sizeBytes: 10,
                sha256: new string('D', 64),
                storagePath: "test/infected.pdf",
                scanStatus: FileScanStatus.Infected,
                sensitivity: FileSensitivityLevel.Core,
                createdBy: Guid.NewGuid(),
                now: Now))
            .ErrorCode.ShouldBe(ErrorCodes.ValidationFailed);
    }

    private static FileAttachment CreateAttachment(FileSensitivityLevel sensitivity) =>
        FileAttachment.Create(
            OrganizationId,
            DepartmentId,
            null,
            "demo",
            isTestData: true,
            sourceSystem: "AIDT",
            originalFileName: "spec.pdf",
            mimeType: "application/pdf",
            sizeBytes: 1024,
            sha256: new string('A', 64),
            storagePath: "demo/department/2026/10/guid.pdf",
            scanStatus: sensitivity == FileSensitivityLevel.Internal
                ? FileScanStatus.NotConfigured
                : FileScanStatus.PendingManualReview,
            sensitivity,
            Guid.NewGuid(),
            Now);
}
