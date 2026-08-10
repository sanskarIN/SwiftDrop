using SwiftDrop.Core.Models;
using SwiftDrop.Core.Security;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class BatchCompletionVerifierTests
{
    [Fact]
    public async Task TryVerifyAsync_ReturnsDestinationForExactCompletedFile()
    {
        var root = TempDirectory();
        try
        {
            var destination = Path.Combine(root, "folder", "a.bin");
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            await File.WriteAllBytesAsync(destination, [1, 2, 3, 4]);
            var hash = await Hashing.Sha256FileAsync(destination, CancellationToken.None);
            var entry = new FileManifestEntry("source/a.bin", 4, hash, DateTimeOffset.UtcNow);
            var completion = new CompletedBatchItem(
                "batch",
                entry.RelativePath,
                ReceiveRootKey.Create(root),
                "folder/a.bin",
                entry.Length,
                entry.Sha256,
                DateTimeOffset.UtcNow);

            Assert.Equal(
                "folder/a.bin",
                await BatchCompletionVerifier.TryVerifyAsync(root, completion, entry));
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public async Task TryVerifyAsync_RejectsModifiedDestination()
    {
        var root = TempDirectory();
        try
        {
            var destination = Path.Combine(root, "a.bin");
            await File.WriteAllBytesAsync(destination, [1, 2, 3]);
            var hash = await Hashing.Sha256FileAsync(destination, CancellationToken.None);
            var entry = new FileManifestEntry("a.bin", 3, hash, DateTimeOffset.UtcNow);
            var completion = new CompletedBatchItem(
                "batch", "a.bin", ReceiveRootKey.Create(root), "a.bin", 3, hash, DateTimeOffset.UtcNow);
            await File.WriteAllBytesAsync(destination, [3, 2, 1]);

            Assert.Null(await BatchCompletionVerifier.TryVerifyAsync(root, completion, entry));
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public async Task TryVerifyAsync_RejectsDifferentRootSourceOrManifest()
    {
        var root = TempDirectory();
        var otherRoot = TempDirectory();
        try
        {
            var destination = Path.Combine(root, "a.bin");
            await File.WriteAllBytesAsync(destination, [1]);
            var hash = await Hashing.Sha256FileAsync(destination, CancellationToken.None);
            var entry = new FileManifestEntry("a.bin", 1, hash, DateTimeOffset.UtcNow);
            var completion = new CompletedBatchItem(
                "batch", "a.bin", ReceiveRootKey.Create(root), "a.bin", 1, hash, DateTimeOffset.UtcNow);

            Assert.Null(await BatchCompletionVerifier.TryVerifyAsync(otherRoot, completion, entry));
            Assert.Null(await BatchCompletionVerifier.TryVerifyAsync(root, completion, entry with { RelativePath = "b.bin" }));
            Assert.Null(await BatchCompletionVerifier.TryVerifyAsync(root, completion, entry with { Sha256 = new string('A', 64) }));
        }
        finally
        {
            DeleteBestEffort(root);
            DeleteBestEffort(otherRoot);
        }
    }

    [Fact]
    public async Task TryVerifyAsync_RejectsTraversalDestinationMetadata()
    {
        var root = TempDirectory();
        try
        {
            var entry = new FileManifestEntry("a.bin", 0, await HashEmptyAsync(), DateTimeOffset.UtcNow);
            var completion = new CompletedBatchItem(
                "batch", "a.bin", ReceiveRootKey.Create(root), "../escape.bin", 0, entry.Sha256, DateTimeOffset.UtcNow);

            Assert.Null(await BatchCompletionVerifier.TryVerifyAsync(root, completion, entry));
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    private static async Task<string> HashEmptyAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), $"swiftdrop-empty-{Guid.NewGuid():N}");
        try
        {
            await File.WriteAllBytesAsync(path, []);
            return await Hashing.Sha256FileAsync(path, CancellationToken.None);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static string TempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"swiftdrop-completion-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteBestEffort(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }
        catch
        {
        }
    }
}
