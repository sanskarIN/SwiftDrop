using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class TransferEngineSourceSafetyTests
{
    [Fact]
    public async Task SendFileAsync_StreamsRegularFileExactly()
    {
        var root = CreateTempDirectory();
        try
        {
            var path = Path.Combine(root, "payload.bin");
            var bytes = Enumerable.Range(0, 257).Select(index => (byte)(index % 251)).ToArray();
            await File.WriteAllBytesAsync(path, bytes);
            await using var network = new MemoryStream();

            await new TransferEngine().SendFileAsync(network, path, 0, bytes.Length, null, CancellationToken.None);

            Assert.Equal(bytes, network.ToArray());
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public async Task SendFileAsync_RejectsSymlinkedSourceAtStreamBoundaryWhenSupported()
    {
        var root = CreateTempDirectory();
        var outside = CreateTempDirectory();
        try
        {
            var target = Path.Combine(outside, "target.bin");
            await File.WriteAllBytesAsync(target, [1, 2, 3]);
            var link = Path.Combine(root, "link.bin");
            try
            {
                File.CreateSymbolicLink(link, target);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or PlatformNotSupportedException or IOException)
            {
                return;
            }

            await using var network = new MemoryStream();
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                new TransferEngine().SendFileAsync(network, link, 0, 3, null, CancellationToken.None));
            Assert.Empty(network.ToArray());
        }
        finally
        {
            DeleteBestEffort(root);
            DeleteBestEffort(outside);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"swiftdrop-engine-source-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteBestEffort(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }
        catch
        {
        }
    }
}
