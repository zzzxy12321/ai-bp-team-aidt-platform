using System.Globalization;
using System.Security.Cryptography;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Common;
using AIDT.Infrastructure.Configuration;

namespace AIDT.Infrastructure.Storage;

public sealed class LocalFileStorage(FileStorageOptions options) : IFileStorage
{
    private readonly string _rootPath = Path.GetFullPath(options.RootPath);

    public async Task<StoredFile> SaveAsync(
        Stream content,
        string extension,
        Guid departmentId,
        string environmentCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(extension)
            || !extension.StartsWith('.')
            || extension.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0)
        {
            throw new ValidationException("文件扩展名无效。");
        }

        var now = DateTimeOffset.UtcNow;
        var relativePath = Path.Combine(
            environmentCode,
            departmentId.ToString("N"),
            now.ToString("yyyy", CultureInfo.InvariantCulture),
            now.ToString("MM", CultureInfo.InvariantCulture),
            $"{Guid.NewGuid():N}{extension}");
        var absolutePath = ResolvePath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
        var temporaryPath = $"{absolutePath}.uploading";
        long totalBytes = 0;
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        try
        {
            await using (var output = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                64 * 1024,
                FileOptions.Asynchronous))
            {
                var buffer = new byte[64 * 1024];
                int read;
                while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    totalBytes += read;
                    if (totalBytes > options.MaxSizeBytes)
                    {
                        throw new ValidationException($"文件超过允许大小上限 {options.MaxSizeBytes} 字节。");
                    }

                    hash.AppendData(buffer, 0, read);
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
            }

            File.Move(temporaryPath, absolutePath);
            return new StoredFile(
                relativePath.Replace('\\', '/'),
                totalBytes,
                Convert.ToHexString(hash.GetHashAndReset()));
        }
        catch
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }

            throw;
        }
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var absolutePath = ResolvePath(relativePath);
        if (!File.Exists(absolutePath))
        {
            throw new NotFoundException("文件内容不存在。");
        }

        return Task.FromResult<Stream>(new FileStream(
            absolutePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan));
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var absolutePath = ResolvePath(relativePath);
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        return Task.CompletedTask;
    }

    private string ResolvePath(string relativePath)
    {
        if (Path.IsPathRooted(relativePath)
            || relativePath.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar])
                .Any(part => part == ".."))
        {
            throw new ValidationException("文件存储路径无效。");
        }

        var combined = Path.GetFullPath(Path.Combine(_rootPath, relativePath));
        if (!combined.StartsWith(_rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("文件存储路径越界。");
        }

        return combined;
    }
}
