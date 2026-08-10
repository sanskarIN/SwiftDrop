using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class PortablePathFuzzTests
{
    [Fact]
    public void RandomRelativePaths_NeverResolveOutsideRoot()
    {
        var root = CreateTempDirectory();
        try
        {
            var canonicalRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var random = new Random(0x51F7D0);
            const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789._-/\\ :";

            for (var i = 0; i < 5_000; i++)
            {
                var length = random.Next(1, 120);
                var chars = new char[length];
                for (var j = 0; j < chars.Length; j++) chars[j] = alphabet[random.Next(alphabet.Length)];
                var candidate = new string(chars);

                try
                {
                    var resolved = PathGuard.ResolveUnderRoot(root, candidate);
                    Assert.StartsWith(canonicalRoot, resolved, PathComparisonPolicy.Comparison);
                }
                catch (Exception ex) when (ex is InvalidDataException or ArgumentException or IOException or NotSupportedException)
                {
                }
            }
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Theory]
    [InlineData("C:/Windows/System32/config")]
    [InlineData("c:\\windows\\system32\\config")]
    [InlineData("Z:relative")]
    [InlineData("//server/share/file")]
    [InlineData("\\\\server\\share\\file")]
    [InlineData("\\\\?\\UNC\\server\\share\\file")]
    [InlineData("\\\\.\\PhysicalDrive0")]
    public void KnownPortableRootForms_AreRejectedOnEveryHost(string value)
    {
        var root = CreateTempDirectory();
        try
        {
            Assert.Throws<InvalidDataException>(() => PathGuard.ResolveUnderRoot(root, value));
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "swiftdrop-path-fuzz-" + Guid.NewGuid().ToString("N"));
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
