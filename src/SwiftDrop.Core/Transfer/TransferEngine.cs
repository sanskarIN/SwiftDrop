using System.Security.Cryptography;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Transfer;

public sealed class TransferEngine
{
    public async Task SendFileAsync(Stream network, string sourcePath, long offset, IProgress<long>? progress, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        var expectedLength = new FileInfo(sourcePath).Length;
        await SendFileAsync(network, sourcePath, offset, expectedLength, progress, ct);
    }

    public async Task SendFileAsync(
        Stream network,
        string sourcePath,
        long offset,
        long expectedLength,
        IProgress<long>? progress,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(network);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        if (expectedLength < 0 || expectedLength > ProtocolConstants.MaxSingleFileBytes)
            throw new ArgumentOutOfRangeException(nameof(expectedLength));

        await using var file = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            ProtocolConstants.ChunkSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (file.Length != expectedLength)
            throw new IOException("Source file size changed after its transfer manifest was created.");
        if (offset < 0 || offset > expectedLength)
            throw new ArgumentOutOfRangeException(nameof(offset));

        file.Position = offset;
        var remaining = expectedLength - offset;
        var buffer = new byte[ProtocolConstants.ChunkSize];
        long sent = offset;
        while (remaining > 0)
        {
            ct.ThrowIfCancellationRequested();
            var requested = (int)Math.Min(buffer.Length, remaining);
            var read = await file.ReadAsync(buffer.AsMemory(0, requested), ct);
            if (read == 0)
                throw new EndOfStreamException("Source file became shorter during transfer.");
            await WriteNetworkAsync(network, buffer.AsMemory(0, read), ct);
            sent += read;
            remaining -= read;
            progress?.Report(sent);
        }

        if (file.Length != expectedLength)
            throw new IOException("Source file size changed during transfer.");
        await FlushNetworkAsync(network, ct);
    }

    public async Task ReceiveFileAsync(Stream network, string destinationRoot, FileManifestEntry entry, long offset, IProgress<long>? progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(network);
        ManifestValidator.ValidateEntry(entry);
        var finalPath = PathGuard.ResolveUnderRoot(destinationRoot, entry.RelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
        var partial = finalPath + ".swiftdrop.part";
        var mode = offset == 0 ? FileMode.Create : FileMode.OpenOrCreate;
        await using (var file = new FileStream(partial, mode, FileAccess.Write, FileShare.None, ProtocolConstants.ChunkSize, FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            if (offset < 0 || offset > entry.Length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (offset > 0 && file.Length < offset)
                throw new InvalidDataException("Resume offset is beyond the available staged partial file.");
            if (file.Length > offset)
                file.SetLength(offset);
            file.Position = offset;
            var remaining = entry.Length - offset;
            var buffer = new byte[ProtocolConstants.ChunkSize];
            long received = offset;
            while (remaining > 0)
            {
                ct.ThrowIfCancellationRequested();
                var requested = (int)Math.Min(buffer.Length, remaining);
                var read = await ReadNetworkAsync(network, buffer.AsMemory(0, requested), ct);
                if (read == 0) throw new EndOfStreamException();
                await file.WriteAsync(buffer.AsMemory(0, read), ct);
                remaining -= read;
                received += read;
                progress?.Report(received);
            }
            await file.FlushAsync(ct);
        }

        var actual = await Hashing.Sha256FileAsync(partial, ct);
        var actualBytes = Convert.FromHexString(actual);
        var expectedBytes = Convert.FromHexString(entry.Sha256);
        try
        {
            if (!CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes))
            {
                File.Delete(partial);
                throw new InvalidDataException("SHA-256 integrity check failed.");
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(actualBytes);
            CryptographicOperations.ZeroMemory(expectedBytes);
        }

        File.Move(partial, finalPath, true);
        File.SetLastWriteTimeUtc(finalPath, entry.LastWriteUtc.UtcDateTime);
    }

    private static async ValueTask<int> ReadNetworkAsync(Stream stream, Memory<byte> buffer, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(ProtocolConstants.IdleTimeout);
        try
        {
            return await stream.ReadAsync(buffer, timeout.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException("Peer stopped sending data before the transfer idle timeout.");
        }
    }

    private static async ValueTask WriteNetworkAsync(Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(ProtocolConstants.IdleTimeout);
        try
        {
            await stream.WriteAsync(buffer, timeout.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException("Peer stopped accepting data before the transfer idle timeout.");
        }
    }

    private static async ValueTask FlushNetworkAsync(Stream stream, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(ProtocolConstants.IdleTimeout);
        try
        {
            await stream.FlushAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException("Peer stopped accepting data before the transfer idle timeout.");
        }
    }
}
