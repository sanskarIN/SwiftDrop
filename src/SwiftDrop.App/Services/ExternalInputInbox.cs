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
        if (text.Length > 262_144) text = text[..262_144];
        lock (Gate) _sharedText = text;
        RaiseChanged();
    }

    public static void AddSharedFile(string localPath) => AddSharedPath(localPath);

    public static void AddSharedPath(string localPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localPath);
        string full;
        try
        {
            full = Path.GetFullPath(localPath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return;
        }

        if (!File.Exists(full) && !Directory.Exists(full)) return;
        lock (Gate)
        {
            if (SharedPaths.Count >= 2048) return;
            if (!SharedPaths.Contains(full, PathComparer)) SharedPaths.Add(full);
        }
        RaiseChanged();
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
        foreach (var path in Directory.EnumerateFiles(directory))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(path) < cutoff.UtcDateTime) File.Delete(path);
            }
            catch
            {
            }
        }
    }

    private static StringComparer PathComparer
        => OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

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
