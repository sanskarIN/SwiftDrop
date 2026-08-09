using System.Security.Cryptography;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class TransferEngineResumeTests
{
    [Fact]
    public async Task SendFileAsync_Starts_At_Requested_Offset()
    {
        var root = CreateRoot("send");
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
            DeleteRoot(root);
        }
    }

    [Fact]
    public async Task SendFileAsync_Rejects_Source_Length_Changed_From_Manifest()
    {
        var root = CreateRoot("manifest-length");
        var path = Path.Combine(root, "source.bin");
        await File.WriteAllBytesAsync(path, new byte[4097]);
        try
        {
            await using var network = new MemoryStream();
            await Assert.ThrowsAsync<IOException>(() =>
                new TransferEngine().SendFileAsync(
                    network,
                    path,
                    0,
                    4096,
                    null,
                    CancellationToken.None));
            Assert.Equal(0, network.Length);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public async Task ReceiveFileAsync_Resumes_Existing_Partial_And_Verifies_Final_File()
    {
        var root = CreateRoot("receive");
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
            DeleteRoot(root);
        }
    }

    [Fact]
    public async Task ReceiveFileAsync_Truncates_Staged_Tail_To_Negotiated_Offset()
    {
        var root = CreateRoot("resume-tail");
        var final = Path.Combine(root, "file.bin");
        var partial = final + ".swiftdrop.part";
        var content = RandomNumberGenerator.GetBytes(12_000);
        var staged = content[..5000].Concat(RandomNumberGenerator.GetBytes(2000)).ToArray();
        await File.WriteAllBytesAsync(partial, staged);
        var entry = new FileManifestEntry(
            "file.bin",
            content.Length,
            Convert.ToHexString(SHA256.HashData(content)),
            DateTimeOffset.UtcNow);

        try
        {
            await using var network = new MemoryStream(content[5000..]);
            await new TransferEngine().ReceiveFileAsync(network, root, entry, 5000, null, CancellationToken.None);
            Assert.Equal(content, await File.ReadAllBytesAsync(final));
            Assert.False(File.Exists(partial));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public async Task ReceiveFileAsync_Rejects_Resume_Offset_Beyond_Partial_Length()
    {
        var root = CreateRoot("resume-invalid-offset");
        var final = Path.Combine(root, "file.bin");
        var partial = final + ".swiftdrop.part";
        var content = RandomNumberGenerator.GetBytes(4096);
        await File.WriteAllBytesAsync(partial, content[..1000]);
        var entry = new FileManifestEntry(
            "file.bin",
            content.Length,
            Convert.ToHexString(SHA256.HashData(content)),
            DateTimeOffset.UtcNow);

        try
        {
            await using var network = new MemoryStream(content[2000..]);
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                new TransferEngine().ReceiveFileAsync(network, root, entry, 2000, null, CancellationToken.None));
            Assert.False(File.Exists(final));
            Assert.True(File.Exists(partial));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    private static string CreateRoot(string suffix)
    {
        var root = Path.Combine(Path.GetTempPath(), $"swiftdrop-{suffix}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        return root;
    }

    private static void DeleteRoot(string root)
    {
        try
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
        catch
        {
        }
    }
}
