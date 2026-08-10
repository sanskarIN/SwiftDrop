using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class PathGuardPortableRootTests
{
    [Theory]
    [InlineData("../escape.txt")]
    [InlineData("..\\escape.txt")]
    [InlineData("folder/../escape.txt")]
    [InlineData("folder\\..\\escape.txt")]
    [InlineData("./file.txt")]
    [InlineData("folder/./file.txt")]
    [InlineData("/absolute/file.txt")]
    [InlineData("\\absolute\\file.txt")]
    [InlineData("\\\\server\\share\\file.txt")]
    [InlineData("C:\\Windows\\file.txt")]
    [InlineData("C:relative-file.txt")]
    [InlineData("\\\\?\\C:\\Windows\\file.txt")]
    public void ResolveUnderRoot_RejectsPortableRootAndTraversalSyntax(string relativePath)
    {
        var root = CreateTempDirectory();
        try
        {
            Assert.Throws<InvalidDataException>(() => PathGuard.ResolveUnderRoot(root, relativePath));
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Theory]
    [InlineData("folder/sub/file.txt")]
    [InlineData("folder\\sub\\file.txt")]
    public void ResolveUnderRoot_NormalizesPortableSeparatorsInsideRoot(string relativePath)
    {
        var root = CreateTempDirectory();
        try
        {
            var result = PathGuard.ResolveUnderRoot(root, relativePath);
            var expected = Path.Combine(Path.GetFullPath(root), "folder", "sub", "file.txt");
            Assert.Equal(expected, result, PathComparisonPolicy.Comparer);
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public void ResolveUnderRoot_RejectsWhitespaceRoot()
        => Assert.Throws<ArgumentException>(() => PathGuard.ResolveUnderRoot(" ", "file.txt"));

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "swiftdrop-path-portable-" + Guid.NewGuid().ToString("N"));
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
