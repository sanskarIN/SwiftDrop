using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class PathGuardReparseTests
{
    [Fact]
    public void EnsureNoReparsePointsUnderRoot_AllowsNormalMissingAndExistingChildren()
    {
        var root = TempDirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "folder"));
            var resolved = PathGuard.EnsureNoReparsePointsUnderRoot(root, "folder/file.txt");
            Assert.Equal(Path.Combine(root, "folder", "file.txt"), resolved);
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public void EnsureNoReparsePointsUnderRoot_RejectsDirectorySymlinkComponentWhenSupported()
    {
        var root = TempDirectory();
        var outside = TempDirectory();
        try
        {
            var link = Path.Combine(root, "linked");
            try
            {
                Directory.CreateSymbolicLink(link, outside);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or PlatformNotSupportedException or IOException)
            {
                return;
            }

            Assert.Throws<InvalidDataException>(() =>
                PathGuard.EnsureNoReparsePointsUnderRoot(root, "linked/escape.bin"));
        }
        finally
        {
            DeleteBestEffort(root);
            DeleteBestEffort(outside);
        }
    }

    [Fact]
    public void EnsureNoReparsePointsUnderRoot_RejectsFinalFileSymlinkWhenSupported()
    {
        var root = TempDirectory();
        var outside = Path.Combine(Path.GetTempPath(), $"swiftdrop-outside-{Guid.NewGuid():N}.bin");
        try
        {
            File.WriteAllBytes(outside, [1, 2, 3]);
            var link = Path.Combine(root, "linked.bin");
            try
            {
                File.CreateSymbolicLink(link, outside);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or PlatformNotSupportedException or IOException)
            {
                return;
            }

            Assert.Throws<InvalidDataException>(() =>
                PathGuard.EnsureNoReparsePointsUnderRoot(root, "linked.bin"));
        }
        finally
        {
            DeleteBestEffort(root);
            try { if (File.Exists(outside)) File.Delete(outside); } catch { }
        }
    }

    private static string TempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"swiftdrop-reparse-{Guid.NewGuid():N}");
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
