using System.Text;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class HashingTests
{
    [Fact]
    public async Task Sha256FileAsync_ReturnsKnownDigest()
    {
        var path = Path.Combine(Path.GetTempPath(), $"swiftdrop-hash-{Guid.NewGuid():N}.bin");
        try
        {
            await File.WriteAllBytesAsync(path, Encoding.ASCII.GetBytes("abc"));

            var digest = await Hashing.Sha256FileAsync(path);

            Assert.Equal("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD", digest);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Sha256FileAsync_ReturnsKnownEmptyDigest()
    {
        var path = Path.Combine(Path.GetTempPath(), $"swiftdrop-hash-{Guid.NewGuid():N}.bin");
        try
        {
            await File.WriteAllBytesAsync(path, Array.Empty<byte>());

            var digest = await Hashing.Sha256FileAsync(path);

            Assert.Equal("E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855", digest);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Sha256FileAsync_RejectsMissingFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"swiftdrop-missing-{Guid.NewGuid():N}.bin");

        await Assert.ThrowsAsync<FileNotFoundException>(() => Hashing.Sha256FileAsync(path));
    }

    [Fact]
    public async Task Sha256FileAsync_HonorsPreCancelledToken()
    {
        var path = Path.Combine(Path.GetTempPath(), $"swiftdrop-hash-{Guid.NewGuid():N}.bin");
        try
        {
            await File.WriteAllBytesAsync(path, new byte[1024 * 1024]);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                Hashing.Sha256FileAsync(path, cancellation.Token));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
