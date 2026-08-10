using System.Security.Cryptography;
using System.Text;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class TransferFinalPromotionRaceTests
{
    [Fact]
    public async Task ReceiveFileAsync_DoesNotOverwriteFileCreatedDuringTransfer()
    {
        var root = CreateTempDirectory();
        try
        {
            var payload = Enumerable.Range(0, 128 * 1024).Select(i => (byte)(i % 251)).ToArray();
            var finalPath = Path.Combine(root, "race.bin");
            var partialPath = finalPath + ".swiftdrop.part";
            var externalBytes = Encoding.UTF8.GetBytes("created-by-another-writer");
            var entry = new FileManifestEntry(
                "race.bin",
                payload.LongLength,
                Convert.ToHexString(SHA256.HashData(payload)),
                DateTimeOffset.UtcNow.AddMinutes(-1));

            await using var network = new CallbackReadStream(payload, () => File.WriteAllBytes(finalPath, externalBytes));

            await Assert.ThrowsAsync<IOException>(() =>
                new TransferEngine().ReceiveFileAsync(
                    network,
                    root,
                    entry,
                    0,
                    null,
                    CancellationToken.None));

            Assert.Equal(externalBytes, await File.ReadAllBytesAsync(finalPath));
            Assert.True(File.Exists(partialPath));
            Assert.Equal(payload, await File.ReadAllBytesAsync(partialPath));
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    private sealed class CallbackReadStream : MemoryStream
    {
        private readonly Action _onFirstRead;
        private int _called;

        public CallbackReadStream(byte[] buffer, Action onFirstRead)
            : base(buffer, writable: false)
        {
            _onFirstRead = onFirstRead;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _called, 1) == 0) _onFirstRead();
            return base.ReadAsync(buffer, cancellationToken);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "swiftdrop-final-race-" + Guid.NewGuid().ToString("N"));
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
