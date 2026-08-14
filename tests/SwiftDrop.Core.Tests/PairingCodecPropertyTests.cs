using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class PairingCodecPropertyTests
{
    [Fact]
    public void EncodeDecodeEncode_IsStableAcrossDeterministicValidPayloads()
    {
        var random = new Random(0x50414952);
        var now = new DateTimeOffset(2026, 8, 14, 7, 0, 0, TimeSpan.Zero);

        for (var i = 0; i < 2_000; i++)
        {
            var payload = new PairingPayload(
                ProtocolConstants.CurrentVersion,
                $"device-{i}-{RandomToken(random, random.Next(4, 28))}",
                $"Device {i} {RandomToken(random, random.Next(1, 18))}",
                RandomPrivateIpv4(random),
                random.Next(1, 65_536),
                RandomFingerprint(random),
                RandomToken(random, random.Next(16, 65)),
                now.AddSeconds(random.Next(1, (int)ProtocolConstants.PairingLifetime.TotalSeconds + 1)).ToUnixTimeSeconds());

            var encoded = PairingCodec.Encode(payload);
            var decoded = PairingCodec.Decode(encoded, now);
            var reencoded = PairingCodec.Encode(decoded);

            Assert.Equal(encoded, reencoded);
            Assert.Equal(payload, decoded);
            Assert.StartsWith("swiftdrop://pair?p=", encoded, StringComparison.Ordinal);
            Assert.DoesNotContain('=', encoded[(encoded.IndexOf("?p=", StringComparison.Ordinal) + 3)..]);
        }
    }

    [Fact]
    public void CanonicalLinks_RejectDeterministicOuterAndQueryAliases()
    {
        var random = new Random(0x43414E4F);
        var now = new DateTimeOffset(2026, 8, 14, 7, 0, 0, TimeSpan.Zero);

        for (var i = 0; i < 250; i++)
        {
            var payload = new PairingPayload(
                ProtocolConstants.CurrentVersion,
                $"device-{i}",
                $"Device-{i}",
                RandomPrivateIpv4(random),
                ProtocolConstants.DefaultPort,
                RandomFingerprint(random),
                RandomToken(random, 24),
                now.AddMinutes(2).ToUnixTimeSeconds());
            var canonical = PairingCodec.Encode(payload);
            var query = canonical[canonical.IndexOf('?')..];
            var encoded = canonical[(canonical.IndexOf("?p=", StringComparison.Ordinal) + 3)..];

            var aliases = new[]
            {
                " " + canonical,
                canonical + "\n",
                canonical + "#fragment",
                canonical + "&debug=true",
                canonical + "&p=" + encoded,
                canonical.Replace("?p=", "?P=", StringComparison.Ordinal),
                canonical.Replace("?p=", "?p==", StringComparison.Ordinal),
                canonical.Replace("?p=", "?&p=", StringComparison.Ordinal),
                $"swiftdrop://pair/extra{query}",
                $"swiftdrop://pair:443/{query}",
            };

            foreach (var alias in aliases)
                Assert.Throws<FormatException>(() => PairingCodec.Decode(alias, now));
        }
    }

    private static string RandomPrivateIpv4(Random random)
    {
        return random.Next(3) switch
        {
            0 => $"10.{random.Next(256)}.{random.Next(256)}.{random.Next(1, 255)}",
            1 => $"172.{random.Next(16, 32)}.{random.Next(256)}.{random.Next(1, 255)}",
            _ => $"192.168.{random.Next(256)}.{random.Next(1, 255)}",
        };
    }

    private static string RandomFingerprint(Random random)
    {
        Span<char> chars = stackalloc char[64];
        const string hex = "0123456789ABCDEF";
        for (var i = 0; i < chars.Length; i++) chars[i] = hex[random.Next(hex.Length)];
        return new string(chars);
    }

    private static string RandomToken(Random random, int length)
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";
        var chars = new char[length];
        for (var i = 0; i < chars.Length; i++) chars[i] = alphabet[random.Next(alphabet.Length)];
        return new string(chars);
    }
}
