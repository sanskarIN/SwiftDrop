using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class FileNameSanitizerPropertyTests
{
    [Fact]
    public void SanitizeSegment_IsIdempotentAcrossDeterministicRandomInputs()
    {
        var random = new Random(0x5A17D0);
        for (var i = 0; i < 2000; i++)
        {
            var input = RandomString(random, random.Next(0, 260));
            var once = FileNameSanitizer.SanitizeSegment(input);
            var twice = FileNameSanitizer.SanitizeSegment(once);

            Assert.Equal(once, twice);
            Assert.NotEmpty(once);
            Assert.True(once.Length <= 180);
            Assert.DoesNotContain(once, char.IsControl);
            Assert.DoesNotContain('/', once);
            Assert.DoesNotContain('\\', once);
        }
    }

    [Fact]
    public void PortableCollisionKey_IsStableAfterSanitation()
    {
        var random = new Random(0x711E);
        for (var i = 0; i < 1000; i++)
        {
            var raw = $"folder-{i % 17}/{RandomString(random, random.Next(1, 80))}.txt";
            string sanitized;
            try
            {
                sanitized = FileNameSanitizer.SanitizeRelativePath(raw);
            }
            catch (InvalidDataException)
            {
                continue;
            }

            Assert.Equal(
                FileNameSanitizer.GetPortableCollisionKey(raw),
                FileNameSanitizer.GetPortableCollisionKey(sanitized),
                StringComparer.Ordinal);
        }
    }

    [Fact]
    public void SanitizeRelativePath_NeverReturnsTraversalSegments()
    {
        var candidates = new[]
        {
            "a/../b.txt",
            "./a.txt",
            "a/./b.txt",
            "a/../../b.txt",
            "../b.txt",
            "a\\..\\b.txt",
            "a\\.\\b.txt"
        };

        foreach (var candidate in candidates)
        {
            Assert.Throws<InvalidDataException>(() => FileNameSanitizer.SanitizeRelativePath(candidate));
        }
    }

    private static string RandomString(Random random, int length)
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 ._-<>:\\/?*|é\u0301";
        var chars = new char[length];
        for (var i = 0; i < chars.Length; i++)
        {
            if (random.Next(30) == 0)
                chars[i] = (char)random.Next(0, 32);
            else
                chars[i] = alphabet[random.Next(alphabet.Length)];
        }
        return new string(chars);
    }
}
