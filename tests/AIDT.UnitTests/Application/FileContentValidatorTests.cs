using System.Text;
using AIDT.Application.Documents;
using AIDT.Domain.Common;
using Shouldly;

namespace AIDT.UnitTests.Application;

public sealed class FileContentValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ValidPdf_ReturnsHashAndPreviewKind()
    {
        var content = Encoding.ASCII.GetBytes("%PDF-1.7\nAIDT test document");
        var validator = new FileContentValidator(1024 * 1024);

        var result = await validator.ValidateAsync(
            new MemoryStream(content),
            "evidence.pdf",
            "application/pdf",
            CancellationToken.None);

        result.Extension.ShouldBe(".pdf");
        result.PreviewKind.ShouldBe("pdf");
        result.Sha256.Length.ShouldBe(64);
        result.SizeBytes.ShouldBe(content.LongLength);
    }

    [Fact]
    public async Task ValidateAsync_MimeMismatch_IsRejected()
    {
        var validator = new FileContentValidator(1024 * 1024);

        await Should.ThrowAsync<ValidationException>(() => validator.ValidateAsync(
            new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.7")),
            "fake.pdf",
            "image/png",
            CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_FakePdfSignature_IsRejected()
    {
        var validator = new FileContentValidator(1024 * 1024);

        await Should.ThrowAsync<ValidationException>(() => validator.ValidateAsync(
            new MemoryStream(Encoding.ASCII.GetBytes("not a pdf")),
            "fake.pdf",
            "application/pdf",
            CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_UnknownExtension_IsRejected()
    {
        var validator = new FileContentValidator(1024 * 1024);

        await Should.ThrowAsync<ValidationException>(() => validator.ValidateAsync(
            new MemoryStream([1, 2, 3]),
            "script.exe",
            "application/octet-stream",
            CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_OversizedFile_IsRejected()
    {
        var validator = new FileContentValidator(4);

        await Should.ThrowAsync<ValidationException>(() => validator.ValidateAsync(
            new MemoryStream([1, 2, 3, 4, 5]),
            "note.txt",
            "text/plain",
            CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_EmptyTextFile_IsRejected()
    {
        var validator = new FileContentValidator(1024);

        await Should.ThrowAsync<ValidationException>(() => validator.ValidateAsync(
            new MemoryStream(),
            "empty.txt",
            "text/plain",
            CancellationToken.None));
    }

    [Theory]
    [InlineData("minutes.md", "text/markdown")]
    [InlineData("minutes.json", "application/json")]
    public async Task ValidateAsync_MinutesTextFormats_AreAccepted(
        string fileName,
        string mimeType)
    {
        var validator = new FileContentValidator(1024);
        var result = await validator.ValidateAsync(
            new MemoryStream(Encoding.UTF8.GetBytes("会议：测试\n[demo] 请完成行动项")),
            fileName,
            mimeType,
            CancellationToken.None);

        result.PreviewKind.ShouldBe("text");
        result.SizeBytes.ShouldBeGreaterThan(0);
    }
}
