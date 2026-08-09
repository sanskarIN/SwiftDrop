using System.Security.Cryptography;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class TransferEngineIntegrityTests
{
    [Fact]
    public async Task ReceiveFileAsync_Rejects_Digest_Mismatch_And_Removes_Partial()
    {
        var root = Path.Combine(Path.GetTempPath(), $"swiftdrop-integrity-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var content = RandomNumberGenerator.GetBytes(4096);
        var wrongHash = Convert.ToHexString(SHA256.HashData(RandomNumberGenerator.GetBytes(4096)));
        var entry = new FileManifestEntry("payload.bin", content.Length, wrongHash, DateTimeOffset.UtcNow);
        var final = Path.Combine(root, "payload.bin");
        var partial = final + ".swiftdrop.part";

        try
        {
            await using var network = new MemoryStream(content);
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                new TransferEngine().ReceiveFileAsync(network, root, entry, 0, null, CancellationToken.None));
            Assert.False(File.Exists(final));
            Assert.False(File.Exists(partial));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task ReceiveFileAsync_Rejects_Truncated_Stream()
    {
        var root = Path.Combine(Path.GetTempPath(), $"swiftdrop-truncated-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var content = RandomNumberGenerator.GetBytes(1024);
        var expected = RandomNumberGenerator.GetBytes(2048);
        var hash = Convert.ToHexString(SHA256.HashData(expected));
        var entry = new FileManifestEntry("payload.bin", expected.Length, hash, DateTimeOffset.UtcNow);

        try
        {
            await using var network = new MemoryStream(content);
            await Assert.ThrowsAsync<EndOfStreamException>(() =>
                new TransferEngine().ReceiveFileAsync(network, root, entry, 0, null, CancellationToken.None));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
