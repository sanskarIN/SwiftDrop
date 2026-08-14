using System.Text;
using System.Text.Json;
using SwiftDrop.Core.Protocol;

namespace SwiftDrop.Core.Tests;

public sealed class StrictJsonGuardFuzzTests
{
    [Fact]
    public void RandomBoundedBytes_NeverEscapeExpectedValidationExceptions()
    {
        var random = new Random(0x4A534F4E);

        for (var i = 0; i < 5_000; i++)
        {
            var bytes = new byte[random.Next(0, 513)];
            random.NextBytes(bytes);

            try
            {
                StrictJsonGuard.Validate(bytes, 16);
            }
            catch (Exception ex) when (ex is JsonException or InvalidDataException)
            {
                continue;
            }
        }
    }

    [Fact]
    public void RandomCaseVariantDuplicateProperties_AreAlwaysRejected()
    {
        var random = new Random(0x44555045);

        for (var i = 0; i < 2_000; i++)
        {
            var name = RandomAsciiIdentifier(random, random.Next(1, 33));
            var duplicate = RandomizeAsciiCase(random, name);
            var json = $"{{\"outer\":{{\"{name}\":1,\"{duplicate}\":2}}}}";

            Assert.Throws<InvalidDataException>(
                () => StrictJsonGuard.Validate(Encoding.UTF8.GetBytes(json), 16));
        }
    }

    [Fact]
    public void RandomDistinctProperties_RoundTripThroughStrictValidation()
    {
        var random = new Random(0x554E4951);

        for (var i = 0; i < 1_000; i++)
        {
            var properties = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            while (properties.Count < 12)
                properties.TryAdd(RandomAsciiIdentifier(random, random.Next(1, 17)), random.Next());

            var json = JsonSerializer.Serialize(properties);
            StrictJsonGuard.Validate(Encoding.UTF8.GetBytes(json), 16);
        }
    }

    private static string RandomAsciiIdentifier(Random random, int length)
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyz";
        var chars = new char[length];
        for (var i = 0; i < chars.Length; i++) chars[i] = alphabet[random.Next(alphabet.Length)];
        return new string(chars);
    }

    private static string RandomizeAsciiCase(Random random, string value)
    {
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (random.Next(2) == 0)
                chars[i] = char.ToUpperInvariant(chars[i]);
        }
        return new string(chars);
    }
}
