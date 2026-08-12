namespace SwiftDrop.Core.Security;

public sealed class DestinationReservationSet
{
    private readonly object _gate = new();
    private readonly HashSet<string> _reserved = new(PathComparisonPolicy.Comparer);

    public DestinationReservation Reserve(string requestedPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedPath);
        var fullRequested = Path.GetFullPath(requestedPath);

        lock (_gate)
        {
            var candidate = FindAvailablePathLocked(fullRequested);
            if (!_reserved.Add(candidate))
                throw new IOException("Could not reserve a unique receive destination.");
            return new DestinationReservation(candidate, this);
        }
    }

    public bool IsReserved(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var full = Path.GetFullPath(path);
        lock (_gate) return _reserved.Contains(full);
    }

    private string FindAvailablePathLocked(string requestedPath)
    {
        if (IsAvailableLocked(requestedPath)) return requestedPath;

        var directory = Path.GetDirectoryName(requestedPath) ?? string.Empty;
        var requestedName = Path.GetFileName(requestedPath);
        for (var i = 1; i < 10_000; i++)
        {
            var collisionName = FileNameSanitizer.CreateCollisionSegment(requestedName, i);
            var candidate = Path.Combine(directory, collisionName);
            if (IsAvailableLocked(candidate)) return candidate;
        }

        throw new IOException("Could not resolve destination filename collision.");
    }

    private bool IsAvailableLocked(string path)
        => !_reserved.Contains(path) && !File.Exists(path) && !Directory.Exists(path);

    private void Release(string path)
    {
        lock (_gate) _reserved.Remove(path);
    }

    public sealed class DestinationReservation : IDisposable
    {
        private DestinationReservationSet? _owner;

        internal DestinationReservation(string path, DestinationReservationSet owner)
        {
            Path = path;
            _owner = owner;
        }

        public string Path { get; }

        public void Dispose()
        {
            Interlocked.Exchange(ref _owner, null)?.Release(Path);
        }
    }
}
