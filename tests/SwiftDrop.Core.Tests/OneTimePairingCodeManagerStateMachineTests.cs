using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class OneTimePairingCodeManagerStateMachineTests
{
    [Fact]
    public void PairingCodeManager_MatchesSeededLifecycleModel()
    {
        const int operationCount = 4_000;
        var lifetime = TimeSpan.FromSeconds(45);
        var manager = new OneTimePairingCodeManager(lifetime);
        var random = new Random(0xC0DE51);
        var now = new DateTimeOffset(2026, 8, 19, 0, 0, 0, TimeSpan.Zero);
        PairingCodeSnapshot? current = null;

        for (var operation = 0; operation < operationCount; operation++)
        {
            switch (random.Next(6))
            {
                case 0:
                    current = manager.Create(now);
                    Assert.Equal(now + lifetime, current.ExpiresUtc);
                    Assert.Equal(8, current.Code.Length);
                    Assert.All(current.Code, ch => Assert.InRange(ch, '0', '9'));
                    break;

                case 1:
                {
                    var expected = current is not null && now < current.ExpiresUtc;
                    var candidate = current?.Code ?? "00000000";
                    Assert.Equal(expected, manager.TryConsume(candidate, now));
                    if (expected) current = null;
                    break;
                }

                case 2:
                {
                    var wrong = current is null ? "99999999" : DifferentCode(current.Code);
                    Assert.False(manager.TryConsume(wrong, now));
                    break;
                }

                case 3:
                    manager.Invalidate();
                    current = null;
                    break;

                case 4:
                    now = now.AddSeconds(random.Next(1, 30));
                    break;

                case 5:
                    Assert.False(manager.TryConsume("12x45678", now));
                    Assert.False(manager.TryConsume("1234567", now));
                    break;
            }

            if (current is not null && now >= current.ExpiresUtc)
            {
                Assert.False(manager.TryConsume(current.Code, now));
                current = null;
            }
        }
    }

    private static string DifferentCode(string code)
    {
        var chars = code.ToCharArray();
        chars[^1] = chars[^1] == '9' ? '0' : (char)(chars[^1] + 1);
        return new string(chars);
    }
}
