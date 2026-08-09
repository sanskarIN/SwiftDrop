using System.Security.Cryptography;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Diagnostics;

public sealed class TransferSelfTestService
{
    public async Task<SelfTestResult> RunChecksumMismatchAsync(CancellationToken ct = default)
    {
        var root = CreateRoot();
        try
        {
            var expected = RandomNumberGenerator.GetBytes(64 * 1024);
            var corrupted = expected.ToArray();
            corrupted[^1] ^= 0x5A;
            var hash = Convert.ToHexString(SHA256.HashData(expected));
            var entry = new FileManifestEntry("checksum-test.bin", corrupted.Length, hash, DateTimeOffset.UtcNow);
            await using var network = new MemoryStream(corrupted, writable: false);
            try
            {
                await new TransferEngine().ReceiveFileAsync(network, root, entry, 0, null, ct);
                return new SelfTestResult("checksum-mismatch", false, "Corrupted bytes were unexpectedly accepted.");
            }
            catch (InvalidDataException ex) when (ex.Message.Contains("integrity", StringComparison.OrdinalIgnoreCase))
            {
                var final = Path.Combine(root, entry.RelativePath);
                var partial = final + ".swiftdrop.part";
                var passed = !File.Exists(final) && !File.Exists(partial);
                return new SelfTestResult(
                    "checksum-mismatch",
                    passed,
                    passed ? "Checksum mismatch was rejected and the invalid partial file was removed." : "Checksum mismatch was rejected but cleanup was incomplete.");
            }
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    public async Task<SelfTestResult> RunInterruptedReceiveAsync(CancellationToken ct = default)
    {
        var root = CreateRoot();
        try
        {
            var expected = RandomNumberGenerator.GetBytes(128 * 1024);
            var hash = Convert.ToHexString(SHA256.HashData(expected));
            var entry = new FileManifestEntry("interruption-test.bin", expected.Length, hash, DateTimeOffset.UtcNow);
            await using var network = new MemoryStream(expected.AsMemory(0, expected.Length / 4).ToArray(), writable: false);
            try
            {
                await new TransferEngine().ReceiveFileAsync(network, root, entry, 0, null, ct);
                return new SelfTestResult("interrupted-receive", false, "An incomplete stream was unexpectedly accepted.");
            }
            catch (EndOfStreamException)
            {
                var final = Path.Combine(root, entry.RelativePath);
                var partial = final + ".swiftdrop.part";
                var passed = !File.Exists(final) && File.Exists(partial) && new FileInfo(partial).Length == expected.Length / 4;
                return new SelfTestResult(
                    "interrupted-receive",
                    passed,
                    passed ? "Interrupted receive remained staged as a resumable partial file." : "Interrupted receive staging did not match the expected resumable state.");
            }
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    public async Task<SelfTestResult> RunSuccessfulRoundTripAsync(CancellationToken ct = default)
    {
        var root = CreateRoot();
        try
        {
            var bytes = RandomNumberGenerator.GetBytes(96 * 1024);
            var hash = Convert.ToHexString(SHA256.HashData(bytes));
            var entry = new FileManifestEntry("roundtrip-test.bin", bytes.Length, hash, DateTimeOffset.UtcNow);
            await using var network = new MemoryStream(bytes, writable: false);
            await new TransferEngine().ReceiveFileAsync(network, root, entry, 0, null, ct);
            var final = Path.Combine(root, entry.RelativePath);
            var actual = File.Exists(final) ? await Hashing.Sha256FileAsync(final, ct) : string.Empty;
            var passed = string.Equals(actual, hash, StringComparison.OrdinalIgnoreCase);
            return new SelfTestResult(
                "successful-roundtrip",
                passed,
                passed ? "Known-good bytes completed and matched their SHA-256 digest." : "Known-good round-trip verification failed.");
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    private static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "swiftdrop-selftest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void DeleteRoot(string root)
    {
        try { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); } catch { }
    }
}

public sealed record SelfTestResult(string Code, bool Passed, string Message);
