using System.Text;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;

namespace SwiftDrop.App.Services;

public static class ExternalInputInbox
{
    private static readonly object Gate = new();
    private static string? _pairingLink;
    private static string? _sharedText;
    private static readonly List<string> SharedPaths = new();

    public static event EventHandler? Changed;

    public static void SetPairingLink(string link)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(link);
        if (link.Length > 16_384 || !link.StartsWith("swiftdrop://pair", StringComparison.OrdinalIgnoreCase))
            return;
        lock (Gate) _pairingLink = link;
        RaiseChanged();
    }

    public static void SetSharedText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        AddSharedBatch(text, Array.Empty<string>());
    }

    public static void AddSharedFile(string localPath) => AddSharedPath(localPath);

    public static void AddSharedPath(string localPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localPath);
        AddSharedBatch(null, [localPath]);
    }

    public static void AddSharedBatch(string? text, IEnumerable<string> localPaths)
    {
        ArgumentNullException.ThrowIfNull(localPaths);
        var validated = new List<string>();
        foreach (var localPath in localPaths)
        {
            if (validated.Count >= ProtocolConstants.MaxBatchFiles) break;
            if (string.IsNullOrWhiteSpace(localPath)) continue;

            string full;
            try
            {
                full = Path.GetFullPath(localPath);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                continue;
            }

            if (!File.Exists(full) && !Directory.Exists(full)) continue;
            if (!validated.Contains(full, PathComparisonPolicy.Comparer)) validated.Add(full);
        }

        var changed = false;
        lock (Gate)
        {
            if (text is not null)
            {
                _sharedText = TruncateUtf8(text, ProtocolConstants.MaxTextSnippetBytes);
                changed = true;
            }

            foreach (var full in validated)
            {
                if (SharedPaths.Count >= ProtocolConstants.MaxBatchFiles) break;
                if (SharedPaths.Contains(full, PathComparisonPolicy.Comparer)) continue;
                SharedPaths.Add(full);
                changed = true;
            }
        }

        if (changed) RaiseChanged();
    }

    public static ExternalInputBatch Drain()
    {
        lock (Gate)
        {
            var result = new ExternalInputBatch(_pairingLink, _sharedText, SharedPaths.ToArray());
            _pairingLink = null;
            _sharedText = null;
            SharedPaths.Clear();
            return result;
        }
    }

    public static void PruneStagedCache(TimeSpan maximumAge)
    {
        if (maximumAge <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(maximumAge));
        var directory = Path.Combine(FileSystem.CacheDirectory, "shared-input");
        if (!Directory.Exists(directory)) return;
        var cutoff = DateTimeOffset.UtcNow - maximumAge;

        foreach (var path in Directory.EnumerateFileSystemEntries(directory, "*", SearchOption.TopDirectoryOnly))
        {
            try
            {
                if (File.Exists(path))
                {
                    if (File.GetLastWriteTimeUtc(path) < cutoff.UtcDateTime) File.Delete(path);
                    continue;
                }

                if (!Directory.Exists(path)) continue;
                var newestUtc = Directory.GetLastWriteTimeUtc(path);
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    var lastWrite = File.GetLastWriteTimeUtc(file);
                    if (lastWrite > newestUtc) newestUtc = lastWrite;
                }
                if (newestUtc < cutoff.UtcDateTime) Directory.Delete(path, recursive: true);
            }
            catch
            {
            }
        }
    }

    private static string TruncateUtf8(string value, int maximumBytes)
    {
        if (Encoding.UTF8.GetByteCount(value) <= maximumBytes) return value;

        var low = 0;
        var high = value.Length;
        while (low < high)
        {
            var mid = low + (high - low + 1) / 2;
            var end = mid;
            if (end > 0 && end < value.Length && char.IsHighSurrogate(value[end - 1]) && char.IsLowSurrogate(value[end]))
                end--;
            if (Encoding.UTF8.GetByteCount(value.AsSpan(0, end)) <= maximumBytes) low = end;
            else high = mid - 1;
        }

        if (low > 0 && low < value.Length && char.IsHighSurrogate(value[low - 1]) && char.IsLowSurrogate(value[low]))
            low--;
        return value[..low];
    }

    private static void RaiseChanged()
    {
        var handler = Changed;
        if (handler is null) return;
        if (MainThread.IsMainThread) handler(null, EventArgs.Empty);
        else MainThread.BeginInvokeOnMainThread(() => handler(null, EventArgs.Empty));
    }
}

public sealed record ExternalInputBatch(
    string? PairingLink,
    string? SharedText,
    IReadOnlyList<string> SharedFiles)
{
    public bool HasAny => !string.IsNullOrWhiteSpace(PairingLink) ||
                          !string.IsNullOrWhiteSpace(SharedText) ||
                          SharedFiles.Count > 0;
}
