using SwiftDrop.Core.Models;
using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Transfer;

public static class BatchCompletionVerifier
{
    public static async Task<string?> TryVerifyAsync(
        string receiveRoot,
        CompletedBatchItem completion,
        FileManifestEntry expected,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(receiveRoot);
        ArgumentNullException.ThrowIfNull(completion);
        ArgumentNullException.ThrowIfNull(expected);
        expected = ManifestValidator.ValidateEntry(expected);

        if (!string.Equals(completion.ReceiveRootKey, ReceiveRootKey.Create(receiveRoot), StringComparison.Ordinal) ||
            !string.Equals(completion.SourceRelativePath, expected.RelativePath, StringComparison.Ordinal) ||
            completion.Length != expected.Length ||
            !FixedTimeHexEquals(completion.Sha256, expected.Sha256))
            return null;

        string destination;
        try
        {
            destination = PathGuard.ResolveUnderRoot(receiveRoot, completion.DestinationRelativePath);
        }
        catch (Exception ex) when (ex is InvalidDataException or ArgumentException or NotSupportedException)
        {
            return null;
        }

        var info = new FileInfo(destination);
        if (!info.Exists || info.Length != expected.Length) return null;
        var actualHash = await Hashing.Sha256FileAsync(destination, ct);
        if (!FixedTimeHexEquals(actualHash, expected.Sha256)) return null;
        return completion.DestinationRelativePath;
    }

    private static bool FixedTimeHexEquals(string left, string right)
    {
        if (left.Length != 64 || right.Length != 64 || !left.All(Uri.IsHexDigit) || !right.All(Uri.IsHexDigit))
            return false;
        Span<byte> leftBytes = stackalloc byte[32];
        Span<byte> rightBytes = stackalloc byte[32];
        if (!Convert.TryFromHexString(left, leftBytes, out var leftWritten) || leftWritten != 32 ||
            !Convert.TryFromHexString(right, rightBytes, out var rightWritten) || rightWritten != 32)
            return false;
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
