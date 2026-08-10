using SwiftDrop.Core.Security;
using SwiftDrop.Core.Storage;

namespace SwiftDrop.Core.Transfer;

public static class ExternalFileStager
{
    private const int BufferSize = 128 * 1024;

    public static async Task<string> StageFileAsync(
        string sourcePath,
        string destinationRoot,
        long maximumBytes,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationRoot);
        if (maximumBytes < 0) throw new ArgumentOutOfRangeException(nameof(maximumBytes));

        var source = new FileInfo(Path.GetFullPath(sourcePath));
        if (!source.Exists) throw new FileNotFoundException("Shared source file is unavailable.", source.FullName);
        if (source.Length < 0 || source.Length > maximumBytes)
            throw new InvalidDataException("Shared source exceeds the staging size limit.");

        var root = Path.GetFullPath(destinationRoot);
        Directory.CreateDirectory(root);
        StorageCapacityGuard.EnsureCapacity(root, source.Length);

        var safeName = FileNameSanitizer.SanitizeSegment(source.Name);
        var destination = Path.Combine(root, $"{Guid.NewGuid():N}-{safeName}");
        var expectedLength = source.Length;

        try
        {
            await using var input = new FileStream(
                source.FullName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                BufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            if (input.Length != expectedLength)
                throw new IOException("Shared source size changed before staging began.");

            await using var output = new FileStream(
                destination,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                BufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            var remaining = expectedLength;
            var buffer = new byte[BufferSize];
            while (remaining > 0)
            {
                ct.ThrowIfCancellationRequested();
                var requested = (int)Math.Min(buffer.Length, remaining);
                var read = await input.ReadAsync(buffer.AsMemory(0, requested), ct);
                if (read == 0)
                    throw new EndOfStreamException("Shared source ended before its declared length.");
                await output.WriteAsync(buffer.AsMemory(0, read), ct);
                remaining -= read;
            }
            await output.FlushAsync(ct);

            if (input.Length != expectedLength || new FileInfo(source.FullName).Length != expectedLength)
                throw new IOException("Shared source size changed while staging.");

            return destination;
        }
        catch
        {
            TryDelete(destination);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
        }
    }
}
