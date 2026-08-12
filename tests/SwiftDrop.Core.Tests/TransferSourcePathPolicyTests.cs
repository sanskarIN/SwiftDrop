using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class TransferSourcePathPolicyTests
{
    [Fact]
    public void ExistingDistinct_PreservesFilesAndDirectoriesAndDropsMissingEntries()
    {
        var root = CreateTempDirectory();
        try
        {
            var file = Path.Combine(root, "a.bin");
            File.WriteAllBytes(file, [1, 2, 3]);
            var folder = Path.Combine(root, "folder");
            Directory.CreateDirectory(folder);
            var missing = Path.Combine(root, "missing.bin");

            var result = TransferSourcePathPolicy.ExistingDistinct([file, folder, file, missing]);

            Assert.Equal(2, result.Length);
            Assert.Contains(Path.GetFullPath(file), result);
            Assert.Contains(Path.GetFullPath(folder), result);
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public void ExistingDistinct_DropsSymlinkedSourceWhenSupported()
    {
        var root = CreateTempDirectory();
        var outside = CreateTempDirectory();
        try
        {
            var target = Path.Combine(outside, "target.bin");
            File.WriteAllBytes(target, [1]);
            var link = Path.Combine(root, "link.bin");
            try
            {
                File.CreateSymbolicLink(link, target);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or PlatformNotSupportedException or IOException)
            {
                return;
            }

            Assert.Empty(TransferSourcePathPolicy.ExistingDistinct([link]));
            Assert.Throws<InvalidDataException>(() => TransferSourcePathPolicy.GetHistoryMetadata(link));
        }
        finally
        {
            DeleteBestEffort(root);
            DeleteBestEffort(outside);
        }
    }

    [Fact]
    public void GetHistoryMetadata_UsesFileLengthAndZeroForDirectorySource()
    {
        var root = CreateTempDirectory();
        try
        {
            var file = Path.Combine(root, "payload.bin");
            File.WriteAllBytes(file, [1, 2, 3, 4]);
            var folder = Path.Combine(root, "folder");
            Directory.CreateDirectory(folder);

            Assert.Equal(("payload.bin", 4L), TransferSourcePathPolicy.GetHistoryMetadata(file));
            Assert.Equal(("folder", 0L), TransferSourcePathPolicy.GetHistoryMetadata(folder));
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public void GetHistoryMetadata_RejectsMissingSource()
    {
        var path = Path.Combine(Path.GetTempPath(), $"swiftdrop-missing-{Guid.NewGuid():N}");
        Assert.Throws<FileNotFoundException>(() => TransferSourcePathPolicy.GetHistoryMetadata(path));
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"swiftdrop-source-policy-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteBestEffort(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }
        catch
        {
        }
    }
}
