using System.Security.Cryptography;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Transfer;

public sealed class TransferEngine
{
    public async Task SendFileAsync(Stream network, string sourcePath, long offset, IProgress<long>? progress, CancellationToken ct)
    {
        await using var file = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, ProtocolConstants.ChunkSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (offset < 0 || offset > file.Length) throw new ArgumentOutOfRangeException(nameof(offset));
        file.Position = offset;
        var buffer = new byte[ProtocolConstants.ChunkSize];
        long sent = offset;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var read = await file.ReadAsync(buffer, ct);
            if (read == 0) break;
            await network.WriteAsync(buffer.AsMemory(0, read), ct);
            sent += read;
            progress?.Report(sent);
        }
        await network.FlushAsync(ct);
    }

    public async Task ReceiveFileAsync(Stream network, string destinationRoot, FileManifestEntry entry, long offset, IProgress<long>? progress, CancellationToken ct)
    {
        var finalPath = PathGuard.ResolveUnderRoot(destinationRoot, entry.RelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
        var partial = finalPath + ".swiftdrop.part";
        var mode = offset == 0 ? FileMode.Create : FileMode.OpenOrCreate;
        await using (var file = new FileStream(partial, mode, FileAccess.Write, FileShare.None, ProtocolConstants.ChunkSize, FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            if (offset < 0 || offset > entry.Length) throw new ArgumentOutOfRangeException(nameof(offset));
            file.Position = offset;
            var remaining = entry.Length - offset;
            var buffer = new byte[ProtocolConstants.ChunkSize];
            long received = offset;
            while (remaining > 0)
            {
                ct.ThrowIfCancellationRequested();
                var requested = (int)Math.Min(buffer.Length, remaining);
                var read = await network.ReadAsync(buffer.AsMemory(0, requested), ct);
                if (read == 0) throw new EndOfStreamException();
                await file.WriteAsync(buffer.AsMemory(0, read), ct);
                remaining -= read;
                received += read;
                progress?.Report(received);
            }
            await file.FlushAsync(ct);
        }

        var actual = await Hashing.Sha256FileAsync(partial, ct);
        if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(actual), Convert.FromHexString(entry.Sha256)))
        {
            File.Delete(partial);
            throw new InvalidDataException("SHA-256 integrity check failed.");
        }
        File.Move(partial, finalPath, true);
        File.SetLastWriteTimeUtc(finalPath, entry.LastWriteUtc.UtcDateTime);
    }
}
