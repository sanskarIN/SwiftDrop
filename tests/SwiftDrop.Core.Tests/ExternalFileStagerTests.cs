using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class ExternalFileStagerTests
{
    [Fact]
    public async Task StageFileAsync_CopiesExactBytesIntoRequestedRoot()
    {
        var root = CreateTempDirectory();
        try
        {
            var source = Path.Combine(root, "shared.txt");
            var data = Enumerable.Range(0, 4096).Select(i => (byte)(i % 251)).ToArray();
            await File.WriteAllBytesAsync(source, data);
            var staging = Path.Combine(root, "staging");

            var result = await ExternalFileStager.StageFileAsync(source, staging, 10_000);

            Assert.True(File.Exists(result));
            Assert.Equal(data, await File.ReadAllBytesAsync(result));
            Assert.StartsWith(Path.GetFullPath(staging), Path.GetFullPath(result), StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith("shared.txt", Path.GetFileName(result), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public async Task StageFileAsync_RejectsOversizedSourceWithoutOutput()
    {
        var root = CreateTempDirectory();
        try
        {
            var source = Path.Combine(root, "large.bin");
            await File.WriteAllBytesAsync(source, new byte[1024]);
            var staging = Path.Combine(root, "staging");

            await Assert.ThrowsAsync<InvalidDataException>(() =>
                ExternalFileStager.StageFileAsync(source, staging, 100));
            Assert.False(Directory.Exists(staging) && Directory.EnumerateFiles(staging).Any());
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public async Task StageFileAsync_CancelledCopyRemovesPartialOutput()
    {
        var root = CreateTempDirectory();
        try
        {
            var source = Path.Combine(root, "cancel.bin");
            await File.WriteAllBytesAsync(source, new byte[512 * 1024]);
            var staging = Path.Combine(root, "staging");
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                ExternalFileStager.StageFileAsync(source, staging, 1024 * 1024, cts.Token));
            Assert.Empty(Directory.Exists(staging) ? Directory.EnumerateFiles(staging) : Array.Empty<string>());
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public async Task StageFileAsync_RejectsMissingSource()
    {
        var root = CreateTempDirectory();
        try
        {
            await Assert.ThrowsAsync<FileNotFoundException>(() =>
                ExternalFileStager.StageFileAsync(
                    Path.Combine(root, "missing.bin"),
                    Path.Combine(root, "staging"),
                    1024));
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "swiftdrop-stage-" + Guid.NewGuid().ToString("N"));
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
