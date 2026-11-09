using System.IO.Compression;
using System.Text;
using AIDT.Application.Documents;
using AIDT.Domain.Common;
using Shouldly;

namespace AIDT.UnitTests.Application;

public sealed class MinutesDocumentParserTests
{
    [Fact]
    public void ParseDocx_ExtractsNonEmptyParagraphsInOrder()
    {
        using var stream = CreateDocx(
            "<w:p><w:r><w:t>第一段</w:t></w:r></w:p>"
            + "<w:p><w:r><w:t> </w:t></w:r></w:p>"
            + "<w:p><w:r><w:t>第二段</w:t></w:r><w:r><w:t>补充</w:t></w:r></w:p>");

        var result = MinutesDocumentParser.ParseDocx(stream);

        result.ShouldBe($"第一段{Environment.NewLine}第二段补充");
    }

    [Fact]
    public void ParseDocx_RejectsEmptyDocument()
    {
        using var stream = CreateDocx("<w:p><w:r><w:t> </w:t></w:r></w:p>");

        var exception = Should.Throw<MinutesDocumentException>(() =>
            MinutesDocumentParser.ParseDocx(stream));

        exception.ErrorCode.ShouldBe(ErrorCodes.DocxParseFailed);
    }

    [Fact]
    public void ParseDocx_RejectsMissingDocumentXml()
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("word/styles.xml");
            using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
            writer.Write("<styles />");
        }
        stream.Position = 0;

        Should.Throw<MinutesDocumentException>(() => MinutesDocumentParser.ParseDocx(stream));
    }

    [Fact]
    public void ParseDocx_RejectsCorruptZip()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not-a-zip"));

        Should.Throw<MinutesDocumentException>(() => MinutesDocumentParser.ParseDocx(stream));
    }

    private static MemoryStream CreateDocx(string paragraphs)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("word/document.xml");
            using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
            writer.Write(
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
                + "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">"
                + $"<w:body>{paragraphs}</w:body></w:document>");
        }
        stream.Position = 0;
        return stream;
    }
}
