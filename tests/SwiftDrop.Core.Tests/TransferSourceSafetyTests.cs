using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class TransferSourceSafetyTests
{
    [Fact]
    public void GetRegularFile_ReturnsExistingNonLinkFile()
    {
        var root = CreateTempDirectory();
        try
        {
            var path = Path.Combine(root, "payload.bin");
            File.WriteAllBytes(path, [1, 2, 3]);

            var info = TransferSourceSafety.GetRegularFile(path);

            Assert.True(info.Exists);
            Assert.Equal(3, info.Length);
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public void GetRegularDirectory_ReturnsExistingNonLinkDirectory()
    {
        var root = CreateTempDirectory();
        try
        {
            var folder = Path.Combine(root, "folder");
            Directory.CreateDirectory(folder);

            var info = TransferSourceSafety.GetRegularDirectory(folder);

            Assert.True(info.Exists);
            Assert.Equal(Path.GetFullPath(folder), info.FullName);
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public void GetRegularFile_RejectsSymlinkWhenSupported()
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

            Assert.Throws<InvalidDataException>(() => TransferSourceSafety.GetRegularFile(link));
        }
        finally
        {
            DeleteBestEffort(root);
            DeleteBestEffort(outside);
        }
    }

    [Fact]
    public void GetRegularDirectory_RejectsSymlinkWhenSupported()
    {
        var root = CreateTempDirectory();
        var outside = CreateTempDirectory();
        try
        {
            var link = Path.Combine(root, "linked-folder");
            try
            {
                Directory.CreateSymbolicLink(link, outside);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or PlatformNotSupportedException or IOException)
            {
                return;
            }

            Assert.Throws<InvalidDataException>(() => TransferSourceSafety.GetRegularDirectory(link));
        }
        finally
        {
            DeleteBestEffort(root);
            DeleteBestEffort(outside);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"swiftdrop-source-safety-{Guid.NewGuid():N}");
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
