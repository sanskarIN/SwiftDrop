using System.Security.Cryptography;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class TransferEngineResumeTests
{
    [Fact]
    public async Task SendFileAsync_Starts_At_Requested_Offset()
    {
        var root = Path.Combine(Path.GetTempPath(), $"swiftdrop-send-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "source.bin");
        var content = Enumerable.Range(0, 4096).Select(i => (byte)(i % 251)).ToArray();
        await File.WriteAllBytesAsync(path, content);
        try
        {
            await using var network = new MemoryStream();
            await new TransferEngine().SendFileAsync(network, path, 1024, null, CancellationToken.None);
            Assert.Equal(content[1024..], network.ToArray());
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task ReceiveFileAsync_Resumes_Existing_Partial_And_Verifies_Final_File()
    {
        var root = Path.Combine(Path.GetTempPath(), $"swiftdrop-receive-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var final = Path.Combine(root, "file.bin");
        var partial = final + ".swiftdrop.part";
        var content = RandomNumberGenerator.GetBytes(8192);
        await File.WriteAllBytesAsync(partial, content[..2048]);
        var hash = Convert.ToHexString(SHA256.HashData(content));
        var entry = new FileManifestEntry("file.bin", content.Length, hash, DateTimeOffset.UtcNow);

        try
        {
            await using var network = new MemoryStream(content[2048..]);
            await new TransferEngine().ReceiveFileAsync(network, root, entry, 2048, null, CancellationToken.None);
            Assert.Equal(content, await File.ReadAllBytesAsync(final));
            Assert.False(File.Exists(partial));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
