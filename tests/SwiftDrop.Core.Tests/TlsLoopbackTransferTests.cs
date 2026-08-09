using System.Net;
using System.Net.Sockets;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Networking;
using SwiftDrop.Core.Security;
using SwiftDrop.Core.Transfer;
using Xunit;

namespace SwiftDrop.Core.Tests;

public sealed class TlsLoopbackTransferTests
{
    [Fact]
    public async Task MutualTls_TransfersAndVerifiesFileEndToEnd()
    {
        var temp = CreateTemporaryDirectory();
        try
        {
            var sourcePath = Path.Combine(temp, "source.bin");
            var receiveRoot = Path.Combine(temp, "received");
            Directory.CreateDirectory(receiveRoot);
            var bytes = new byte[384 * 1024 + 137];
            Random.Shared.NextBytes(bytes);
            await File.WriteAllBytesAsync(sourcePath, bytes);

            using var serverCertificate = new CertificateService().CreateSelfSigned("loopback-server");
            using var clientCertificate = new CertificateService().CreateSelfSigned("loopback-client");
            var expectedServerFingerprint = Fingerprint.FromCertificate(serverCertificate);
            var port = GetAvailablePort();

            await using var server = new TlsPeerServer(serverCertificate, port);
            server.Start();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            var acceptTask = server.AcceptAsync(timeout.Token);
            await using var clientStream = await new TlsPeerClient().ConnectAsync(
                IPAddress.Loopback.ToString(),
                port,
                expectedServerFingerprint,
                clientCertificate,
                timeout.Token);
            await using var serverStream = await acceptTask;

            var sourceInfo = new FileInfo(sourcePath);
            var entry = new FileManifestEntry(
                "payload/source.bin",
                sourceInfo.Length,
                await Hashing.Sha256FileAsync(sourcePath, timeout.Token),
                sourceInfo.LastWriteTimeUtc);

            var receiveTask = new TransferEngine().ReceiveFileAsync(
                serverStream,
                receiveRoot,
                entry,
                0,
                null,
                timeout.Token);
            var sendTask = new TransferEngine().SendFileAsync(
                clientStream,
                sourcePath,
                0,
                null,
                timeout.Token);

            await Task.WhenAll(sendTask, receiveTask);

            var receivedPath = Path.Combine(receiveRoot, "payload", "source.bin");
            Assert.True(File.Exists(receivedPath));
            Assert.Equal(bytes, await File.ReadAllBytesAsync(receivedPath, timeout.Token));
            Assert.Equal(
                await Hashing.Sha256FileAsync(sourcePath, timeout.Token),
                await Hashing.Sha256FileAsync(receivedPath, timeout.Token));
        }
        finally
        {
            DeleteDirectoryBestEffort(temp);
        }
    }

    [Fact]
    public async Task MutualTls_ResumeUsesExistingPartialBytes()
    {
        var temp = CreateTemporaryDirectory();
        try
        {
            var sourcePath = Path.Combine(temp, "resume-source.bin");
            var receiveRoot = Path.Combine(temp, "received");
            Directory.CreateDirectory(receiveRoot);
            var bytes = new byte[700 * 1024 + 29];
            Random.Shared.NextBytes(bytes);
            await File.WriteAllBytesAsync(sourcePath, bytes);

            var entry = new FileManifestEntry(
                "resume.bin",
                bytes.LongLength,
                await Hashing.Sha256FileAsync(sourcePath),
                File.GetLastWriteTimeUtc(sourcePath));
            var partialPath = Path.Combine(receiveRoot, "resume.bin.swiftdrop.part");
            const int offset = 211_337;
            await File.WriteAllBytesAsync(partialPath, bytes[..offset]);

            using var serverCertificate = new CertificateService().CreateSelfSigned("resume-server");
            using var clientCertificate = new CertificateService().CreateSelfSigned("resume-client");
            var port = GetAvailablePort();
            await using var server = new TlsPeerServer(serverCertificate, port);
            server.Start();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            var acceptTask = server.AcceptAsync(timeout.Token);
            await using var clientStream = await new TlsPeerClient().ConnectAsync(
                IPAddress.Loopback.ToString(),
                port,
                Fingerprint.FromCertificate(serverCertificate),
                clientCertificate,
                timeout.Token);
            await using var serverStream = await acceptTask;

            await Task.WhenAll(
                new TransferEngine().SendFileAsync(clientStream, sourcePath, offset, null, timeout.Token),
                new TransferEngine().ReceiveFileAsync(serverStream, receiveRoot, entry, offset, null, timeout.Token));

            var finalPath = Path.Combine(receiveRoot, "resume.bin");
            Assert.True(File.Exists(finalPath));
            Assert.False(File.Exists(partialPath));
            Assert.Equal(bytes, await File.ReadAllBytesAsync(finalPath, timeout.Token));
        }
        finally
        {
            DeleteDirectoryBestEffort(temp);
        }
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "SwiftDrop.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectoryBestEffort(string path)
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
