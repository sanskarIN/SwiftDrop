using System.Security.Cryptography;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class TransferPartialMutationTests
{
    [Fact]
    public async Task ReceiveFileAsync_RejectsPartialTruncatedAfterOffsetWasNegotiated()
    {
        var root = CreateTempDirectory();
        try
        {
            var expected = CreatePayload(64 * 1024);
            var entry = CreateEntry("resume.bin", expected);
            var partial = Path.Combine(root, "resume.bin.swiftdrop.part");
            await File.WriteAllBytesAsync(partial, expected[..32_000]);
            const int negotiatedOffset = 32_000;

            await using (var truncate = new FileStream(partial, FileMode.Open, FileAccess.Write, FileShare.None))
                truncate.SetLength(negotiatedOffset - 1);

            await using var remaining = new MemoryStream(expected[negotiatedOffset..], writable: false);
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                new TransferEngine().ReceiveFileAsync(
                    remaining,
                    root,
                    entry,
                    negotiatedOffset,
                    null,
                    CancellationToken.None));

            Assert.False(File.Exists(Path.Combine(root, "resume.bin")));
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public async Task ReceiveFileAsync_DetectsSameLengthPartialContentMutationAfterNegotiation()
    {
        var root = CreateTempDirectory();
        try
        {
            var expected = CreatePayload(96 * 1024);
            var entry = CreateEntry("mutated.bin", expected);
            var partial = Path.Combine(root, "mutated.bin.swiftdrop.part");
            const int negotiatedOffset = 48 * 1024;
            await File.WriteAllBytesAsync(partial, expected[..negotiatedOffset]);

            var staged = await File.ReadAllBytesAsync(partial);
            staged[123] ^= 0x5A;
            await File.WriteAllBytesAsync(partial, staged);
            Assert.Equal(negotiatedOffset, new FileInfo(partial).Length);

            await using var remaining = new MemoryStream(expected[negotiatedOffset..], writable: false);
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                new TransferEngine().ReceiveFileAsync(
                    remaining,
                    root,
                    entry,
                    negotiatedOffset,
                    null,
                    CancellationToken.None));

            Assert.False(File.Exists(Path.Combine(root, "mutated.bin")));
            Assert.False(File.Exists(partial));
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public async Task ReceiveFileAsync_TruncatesUnexpectedTailAndCompletesFromNegotiatedOffset()
    {
        var root = CreateTempDirectory();
        try
        {
            var expected = CreatePayload(80 * 1024);
            var entry = CreateEntry("tail.bin", expected);
            var partial = Path.Combine(root, "tail.bin.swiftdrop.part");
            const int negotiatedOffset = 40 * 1024;
            var stagedWithTail = expected[..(negotiatedOffset + 1024)].ToArray();
            Array.Fill(stagedWithTail, (byte)0xEE, negotiatedOffset, 1024);
            await File.WriteAllBytesAsync(partial, stagedWithTail);

            await using var remaining = new MemoryStream(expected[negotiatedOffset..], writable: false);
            await new TransferEngine().ReceiveFileAsync(
                remaining,
                root,
                entry,
                negotiatedOffset,
                null,
                CancellationToken.None);

            var completed = await File.ReadAllBytesAsync(Path.Combine(root, "tail.bin"));
            Assert.Equal(expected, completed);
            Assert.False(File.Exists(partial));
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    private static FileManifestEntry CreateEntry(string relativePath, byte[] bytes)
        => new(
            relativePath,
            bytes.LongLength,
            Convert.ToHexString(SHA256.HashData(bytes)),
            DateTimeOffset.UtcNow.AddMinutes(-1));

    private static byte[] CreatePayload(int length)
    {
        var payload = new byte[length];
        for (var i = 0; i < payload.Length; i++) payload[i] = (byte)((i * 37 + 11) % 251);
        return payload;
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "swiftdrop-partial-mutation-" + Guid.NewGuid().ToString("N"));
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
