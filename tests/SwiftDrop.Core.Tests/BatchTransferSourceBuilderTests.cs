using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class BatchTransferSourceBuilderTests
{
    [Fact]
    public async Task BuildAsync_IncludesFilesAndFolderRelativePaths()
    {
        var root = CreateTempDirectory("batch");
        try
        {
            var single = Path.Combine(root, "single.txt");
            await File.WriteAllTextAsync(single, "one");
            var folder = Path.Combine(root, "folder");
            Directory.CreateDirectory(Path.Combine(folder, "nested"));
            await File.WriteAllTextAsync(Path.Combine(folder, "a.txt"), "two");
            await File.WriteAllTextAsync(Path.Combine(folder, "nested", "b.txt"), "three");

            var batch = await BatchTransferSourceBuilder.BuildAsync(new[] { single, folder });

            Assert.Equal(3, batch.FileCount);
            Assert.Contains(batch.Items, x => Normalize(x.Entry.RelativePath) == "single.txt");
            Assert.Contains(batch.Items, x => Normalize(x.Entry.RelativePath) == "folder/a.txt");
            Assert.Contains(batch.Items, x => Normalize(x.Entry.RelativePath) == "folder/nested/b.txt");
            Assert.Equal(batch.Items.Sum(x => x.Entry.Length), batch.TotalBytes);
            Assert.All(batch.Items, item => Assert.Equal(64, item.Entry.Sha256.Length));
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public async Task BuildAsync_PreservesCallerTransferIdForResume()
    {
        var root = CreateTempDirectory("stable-id");
        try
        {
            var path = Path.Combine(root, "payload.bin");
            await File.WriteAllBytesAsync(path, [1, 2, 3]);

            var first = await BatchTransferSourceBuilder.BuildAsync([path], "stable-batch-id");
            var retry = await BatchTransferSourceBuilder.BuildAsync([path], "stable-batch-id");

            Assert.Equal("stable-batch-id", first.TransferId);
            Assert.Equal(first.TransferId, retry.TransferId);
            Assert.Equal(first.Items[0].Entry, retry.Items[0].Entry);
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public async Task BuildAsync_RepeatedFolderBuildKeepsDeterministicManifestOrder()
    {
        var root = CreateTempDirectory("deterministic");
        try
        {
            var folder = Path.Combine(root, "folder");
            Directory.CreateDirectory(Path.Combine(folder, "nested"));
            await File.WriteAllTextAsync(Path.Combine(folder, "z.txt"), "z");
            await File.WriteAllTextAsync(Path.Combine(folder, "a.txt"), "a");
            await File.WriteAllTextAsync(Path.Combine(folder, "nested", "b.txt"), "b");

            var first = await BatchTransferSourceBuilder.BuildAsync([folder], "stable-folder-id");
            var retry = await BatchTransferSourceBuilder.BuildAsync([folder], "stable-folder-id");

            Assert.Equal(first.Items.Select(item => item.Entry).ToArray(), retry.Items.Select(item => item.Entry).ToArray());
            Assert.Equal(
                ["folder/a.txt", "folder/nested/b.txt", "folder/z.txt"],
                first.Items.Select(item => Normalize(item.Entry.RelativePath)).ToArray());
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public async Task BuildAsync_RejectsInvalidCallerTransferIdBeforeHashing()
    {
        var root = CreateTempDirectory("bad-id");
        try
        {
            var path = Path.Combine(root, "payload.bin");
            await File.WriteAllBytesAsync(path, [1, 2, 3]);
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                BatchTransferSourceBuilder.BuildAsync([path], "bad\nid"));
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public async Task BuildAsync_DeconflictsDuplicateTopLevelFileNames()
    {
        var root = CreateTempDirectory("duplicates");
        var left = Path.Combine(root, "left");
        var right = Path.Combine(root, "right");
        Directory.CreateDirectory(left);
        Directory.CreateDirectory(right);
        try
        {
            var first = Path.Combine(left, "photo.jpg");
            var second = Path.Combine(right, "photo.jpg");
            await File.WriteAllTextAsync(first, "first");
            await File.WriteAllTextAsync(second, "second");

            var batch = await BatchTransferSourceBuilder.BuildAsync(new[] { first, second });

            Assert.Equal(2, batch.FileCount);
            Assert.Equal(2, batch.Items.Select(x => Normalize(x.Entry.RelativePath)).Distinct(StringComparer.Ordinal).Count());
            Assert.Contains(batch.Items, x => Normalize(x.Entry.RelativePath) == "photo.jpg");
            Assert.Contains(batch.Items, x => Normalize(x.Entry.RelativePath) == "photo (2).jpg");
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public async Task BuildAsync_DeconflictsCaseOnlyPortableTopLevelNames()
    {
        var root = CreateTempDirectory("case-collision");
        var left = Path.Combine(root, "left");
        var right = Path.Combine(root, "right");
        Directory.CreateDirectory(left);
        Directory.CreateDirectory(right);
        try
        {
            var first = Path.Combine(left, "Report.txt");
            var second = Path.Combine(right, "report.TXT");
            await File.WriteAllTextAsync(first, "first");
            await File.WriteAllTextAsync(second, "second");

            var batch = await BatchTransferSourceBuilder.BuildAsync([first, second]);
            var paths = batch.Items.Select(item => Normalize(item.Entry.RelativePath)).ToArray();

            Assert.Equal(2, paths.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.Equal("Report.txt", paths[0]);
            Assert.Contains(" (2)", paths[1], StringComparison.Ordinal);
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public async Task BuildAsync_DeconflictsSanitizationEquivalentTopLevelNames()
    {
        var root = CreateTempDirectory("sanitized-collision");
        var left = Path.Combine(root, "left");
        var right = Path.Combine(root, "right");
        Directory.CreateDirectory(left);
        Directory.CreateDirectory(right);
        try
        {
            var first = Path.Combine(left, "report?.txt");
            var second = Path.Combine(right, "report*.txt");
            try
            {
                await File.WriteAllTextAsync(first, "first");
                await File.WriteAllTextAsync(second, "second");
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException)
            {
                return;
            }

            var batch = await BatchTransferSourceBuilder.BuildAsync([first, second]);
            var paths = batch.Items.Select(item => Normalize(item.Entry.RelativePath)).ToArray();

            Assert.Equal(2, paths.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.Contains(paths, path => path == "report.txt");
            Assert.Contains(paths, path => path == "report (2).txt");
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public async Task BuildAsync_RejectsTopLevelSymlinkedFileWhenSupported()
    {
        var root = CreateTempDirectory("link-file");
        var outside = CreateTempDirectory("link-file-target");
        try
        {
            var target = Path.Combine(outside, "target.bin");
            await File.WriteAllBytesAsync(target, [1]);
            var link = Path.Combine(root, "link.bin");
            try
            {
                File.CreateSymbolicLink(link, target);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or PlatformNotSupportedException or IOException)
            {
                return;
            }

            await Assert.ThrowsAsync<InvalidDataException>(() => BatchTransferSourceBuilder.BuildAsync([link]));
        }
        finally
        {
            DeleteBestEffort(root);
            DeleteBestEffort(outside);
        }
    }

    [Fact]
    public async Task BuildAsync_RejectsTopLevelSymlinkedDirectoryWhenSupported()
    {
        var root = CreateTempDirectory("link-directory");
        var outside = CreateTempDirectory("link-directory-target");
        try
        {
            await File.WriteAllTextAsync(Path.Combine(outside, "target.txt"), "target");
            var link = Path.Combine(root, "linked-folder");
            try
            {
                Directory.CreateSymbolicLink(link, outside);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or PlatformNotSupportedException or IOException)
            {
                return;
            }

            await Assert.ThrowsAsync<InvalidDataException>(() => BatchTransferSourceBuilder.BuildAsync([link]));
        }
        finally
        {
            DeleteBestEffort(root);
            DeleteBestEffort(outside);
        }
    }

    [Fact]
    public async Task BuildAsync_PreflightsCancellationBeforeHashing()
    {
        var root = CreateTempDirectory("cancelled");
        try
        {
            var path = Path.Combine(root, "payload.bin");
            await File.WriteAllBytesAsync(path, new byte[4096]);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                BatchTransferSourceBuilder.BuildAsync(new[] { path }, cts.Token));
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public async Task BuildAsync_RejectsMissingSource()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.bin");
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            BatchTransferSourceBuilder.BuildAsync(new[] { missing }));
    }

    [Fact]
    public async Task BuildAsync_RejectsEmptySelection()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BatchTransferSourceBuilder.BuildAsync(Array.Empty<string>()));
    }

    [Fact]
    public void MaxFileCount_UsesProtocolSourceOfTruth()
        => Assert.Equal(ProtocolConstants.MaxBatchFiles, BatchTransferSourceBuilder.MaxFilesPerBatch);

    private static string Normalize(string value) => value.Replace('\\', '/');

    private static string CreateTempDirectory(string suffix)
    {
        var path = Path.Combine(Path.GetTempPath(), $"swiftdrop-{suffix}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteBestEffort(string path)
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
