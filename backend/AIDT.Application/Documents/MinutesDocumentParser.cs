using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using AIDT.Domain.Common;

namespace AIDT.Application.Documents;

public sealed class MinutesDocumentParser
{
    public const long MaxBytes = 2 * 1024 * 1024;
    private static readonly XNamespace WordNamespace =
        "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    public static string ParseDocx(Stream content)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!content.CanRead)
        {
            throw new MinutesDocumentException("DOCX 内容不可读取。");
        }

        try
        {
            using var buffer = new MemoryStream();
            content.CopyTo(buffer);
            if (buffer.Length is 0 or > MaxBytes)
            {
                throw new MinutesDocumentException("DOCX 必须为非空文件且不超过 2 MiB。");
            }

            buffer.Position = 0;
            using var archive = new ZipArchive(buffer, ZipArchiveMode.Read, leaveOpen: false);
            var documentEntry = archive.GetEntry("word/document.xml")
                ?? throw new MinutesDocumentException("DOCX 缺少 word/document.xml 正文。");
            using var documentStream = documentEntry.Open();
            using var reader = XmlReader.Create(
                documentStream,
                new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    MaxCharactersInDocument = 4_000_000,
                    IgnoreComments = true,
                    IgnoreProcessingInstructions = true
                });
            var document = XDocument.Load(reader, LoadOptions.None);
            var paragraphs = document
                .Descendants(WordNamespace + "p")
                .Select(paragraph => string.Concat(
                    paragraph.Descendants(WordNamespace + "t").Select(run => run.Value)).Trim())
                .Where(paragraph => paragraph.Length > 0)
                .ToArray();
            if (paragraphs.Length == 0)
            {
                throw new MinutesDocumentException("DOCX 没有可读取的正文段落。");
            }

            return string.Join(Environment.NewLine, paragraphs);
        }
        catch (MinutesDocumentException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is InvalidDataException
                or IOException
                or XmlException
                or UnauthorizedAccessException)
        {
            throw new MinutesDocumentException("DOCX 文件损坏或正文格式无效，无法解析。");
        }
    }
}
