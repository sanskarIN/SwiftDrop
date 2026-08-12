using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class TransferSourceEnumeratorTests
{
    [Fact]
    public void EnumerateFiles_ReturnsDeterministicRelativeOrder()
    {
        var root = CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "nested"));
            File.WriteAllText(Path.Combine(root, "z.txt"), "z");
            File.WriteAllText(Path.Combine(root, "a.txt"), "a");
            File.WriteAllText(Path.Combine(root, "nested", "b.txt"), "b");

            var files = TransferSourceEnumerator.EnumerateFiles(root, 10, 10)
                .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
                .ToArray();

            Assert.Equal(["a.txt", "nested/b.txt", "z.txt"], files);
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public void EnumerateFiles_RejectsFileCountOverflow()
    {
        var root = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(root, "a.txt"), "a");
            File.WriteAllText(Path.Combine(root, "b.txt"), "b");

            Assert.Throws<InvalidDataException>(() => TransferSourceEnumerator.EnumerateFiles(root, 1, 10));
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public void EnumerateFiles_RejectsDirectoryCountOverflow()
    {
        var root = CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "one"));
            Directory.CreateDirectory(Path.Combine(root, "two"));

            Assert.Throws<InvalidDataException>(() => TransferSourceEnumerator.EnumerateFiles(root, 10, 1));
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public void EnumerateFiles_RejectsSymlinkedFileWhenSupported()
    {
        var root = CreateTempDirectory();
        var outside = CreateTempDirectory();
        try
        {
            var target = Path.Combine(outside, "outside.txt");
            File.WriteAllText(target, "outside");
            var link = Path.Combine(root, "link.txt");
            try
            {
                File.CreateSymbolicLink(link, target);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or PlatformNotSupportedException or IOException)
            {
                return;
            }

            Assert.Throws<InvalidDataException>(() => TransferSourceEnumerator.EnumerateFiles(root, 10, 10));
        }
        finally
        {
            DeleteBestEffort(root);
            DeleteBestEffort(outside);
        }
    }

    [Fact]
    public void EnumerateFiles_RejectsSymlinkedDirectoryWhenSupported()
    {
        var root = CreateTempDirectory();
        var outside = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(outside, "outside.txt"), "outside");
            var link = Path.Combine(root, "linked-folder");
            try
            {
                Directory.CreateSymbolicLink(link, outside);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or PlatformNotSupportedException or IOException)
            {
                return;
            }

            Assert.Throws<InvalidDataException>(() => TransferSourceEnumerator.EnumerateFiles(root, 10, 10));
        }
        finally
        {
            DeleteBestEffort(root);
            DeleteBestEffort(outside);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"swiftdrop-enumerator-{Guid.NewGuid():N}");
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
