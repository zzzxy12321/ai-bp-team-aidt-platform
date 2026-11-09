using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using AIDT.Application.Contracts;
using AIDT.Domain.Common;

namespace AIDT.Application.Documents;

public sealed class FileContentValidator(long maxBytes)
{
    private static readonly Dictionary<string, FileTypeRule> Rules =
        new Dictionary<string, FileTypeRule>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = new(["application/pdf"], "pdf", "pdf"),
            [".docx"] = new(["application/vnd.openxmlformats-officedocument.wordprocessingml.document"], "zip", "unsupported", "word/"),
            [".xlsx"] = new(["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"], "zip", "unsupported", "xl/"),
            [".pptx"] = new(["application/vnd.openxmlformats-officedocument.presentationml.presentation"], "zip", "unsupported", "ppt/"),
            [".png"] = new(["image/png"], "png", "image"),
            [".jpg"] = new(["image/jpeg"], "jpeg", "image"),
            [".jpeg"] = new(["image/jpeg"], "jpeg", "image"),
            [".txt"] = new(["text/plain"], "text", "text"),
            [".md"] = new(["text/markdown", "text/plain"], "text", "text"),
            [".json"] = new(["application/json", "text/json"], "text", "text"),
            [".csv"] = new(["text/csv", "application/csv", "application/vnd.ms-excel", "text/plain"], "text", "text")
        };

    public async Task<ValidatedFile> ValidateAsync(
        Stream stream,
        string originalFileName,
        string declaredContentType,
        CancellationToken cancellationToken)
    {
        if (!stream.CanRead || !stream.CanSeek)
        {
            throw new ValidationException("上传内容不可读取或不可校验。");
        }

        if (stream.Length > maxBytes)
        {
            throw new ValidationException($"文件超过允许大小上限 {maxBytes} 字节。");
        }

        if (stream.Length == 0)
        {
            throw new ValidationException("上传文件不能为空。");
        }

        var extension = Path.GetExtension(Path.GetFileName(originalFileName)).ToLowerInvariant();
        if (!Rules.TryGetValue(extension, out var rule))
        {
            throw new ValidationException("不支持该文件类型。");
        }

        var normalizedMime = string.IsNullOrWhiteSpace(declaredContentType)
            ? rule.MimeTypes[0]
            : declaredContentType.Split(';', 2)[0].Trim().ToLowerInvariant();
        if (!rule.MimeTypes.Contains(normalizedMime, StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationException("声明 MIME 与扩展名不匹配。");
        }

        await using var hashScope = new MemoryStream();
        await stream.CopyToAsync(hashScope, cancellationToken);
        var content = hashScope.ToArray();
        if (content.LongLength != stream.Length)
        {
            throw new ValidationException("上传内容读取不完整。");
        }

        ValidateSignature(rule, content, extension);
        var sha256 = Convert.ToHexString(SHA256.HashData(content));
        return new ValidatedFile(extension, normalizedMime, content.LongLength, sha256, rule.PreviewKind);
    }

    private static void ValidateSignature(FileTypeRule rule, byte[] content, string extension)
    {
        switch (rule.Signature)
        {
            case "pdf":
                if (content.Length < 5 || Encoding.ASCII.GetString(content, 0, 5) != "%PDF-")
                {
                    throw new ValidationException("PDF 文件签名无效。");
                }
                break;
            case "png":
                if (!content.AsSpan().StartsWith(Convert.FromHexString("89504E470D0A1A0A")))
                {
                    throw new ValidationException("PNG 文件签名无效。");
                }
                break;
            case "jpeg":
                if (content.Length < 3 || content[0] != 0xFF || content[1] != 0xD8 || content[2] != 0xFF)
                {
                    throw new ValidationException("JPEG 文件签名无效。");
                }
                break;
            case "zip":
                if (!content.AsSpan().StartsWith(Convert.FromHexString("504B0304")))
                {
                    throw new ValidationException("Office 文件签名无效。");
                }
                using (var archive = new ZipArchive(new MemoryStream(content, writable: false), ZipArchiveMode.Read))
                {
                    var marker = extension switch
                    {
                        ".docx" => "word/",
                        ".xlsx" => "xl/",
                        ".pptx" => "ppt/",
                        _ => string.Empty
                    };
                    if (!archive.Entries.Any(entry => entry.FullName.StartsWith(marker, StringComparison.Ordinal)))
                    {
                        throw new ValidationException($"文件内容与 {extension} 扩展名不匹配。");
                    }
                }
                break;
            case "text":
                ValidateText(content);
                break;
        }
    }

    private static void ValidateText(byte[] content)
    {
        if (content.Take(4096).Any(value => value == 0))
        {
            throw new ValidationException("文本文件包含二进制内容。");
        }

        try
        {
            _ = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                .GetString(content);
        }
        catch (DecoderFallbackException)
        {
            throw new ValidationException("文本文件必须使用 UTF-8 编码。");
        }
    }

    private sealed record FileTypeRule(
        IReadOnlyList<string> MimeTypes,
        string Signature,
        string PreviewKind,
        string? ZipMarker = null);
}
