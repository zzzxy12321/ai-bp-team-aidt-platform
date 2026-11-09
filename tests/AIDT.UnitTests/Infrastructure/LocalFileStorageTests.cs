using System.Text;
using AIDT.Domain.Common;
using AIDT.Infrastructure.Configuration;
using AIDT.Infrastructure.Storage;
using Shouldly;

namespace AIDT.UnitTests.Infrastructure;

public sealed class LocalFileStorageTests
{
    [Fact]
    public async Task SaveAsync_UsesGuidPhysicalNameAndRejectsPathTraversal()
    {
        var root = Path.Combine(Path.GetTempPath(), $"aidt-storage-{Guid.NewGuid():N}");
        try
        {
            var storage = new LocalFileStorage(new FileStorageOptions
            {
                RootPath = root,
                MaxSizeBytes = 1024
            });

            var result = await storage.SaveAsync(
                new MemoryStream(Encoding.UTF8.GetBytes("AIDT")),
                ".txt",
                Guid.Parse("51000000-0000-0000-0000-000000000001"),
                "test",
                CancellationToken.None);

            result.RelativePath.ShouldStartWith("test/");
            result.RelativePath.ShouldNotContain("original.txt");
            Path.GetFileName(result.RelativePath).ShouldMatch("^[a-f0-9]{32}\\.txt$");
            await using var saved = await storage.OpenReadAsync(result.RelativePath, CancellationToken.None);
            using var reader = new StreamReader(saved, Encoding.UTF8);
            (await reader.ReadToEndAsync()).ShouldBe("AIDT");

            await Should.ThrowAsync<ValidationException>(() =>
                storage.OpenReadAsync("../outside.txt", CancellationToken.None));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
