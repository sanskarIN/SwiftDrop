using System.Security.Cryptography;
using System.Text;

namespace SwiftDrop.Core.Security;

public sealed class OneTimePairingCodeManager
{
    private readonly object _gate = new();
    private readonly TimeSpan _lifetime;
    private string? _code;
    private DateTimeOffset _expiresUtc;

    public OneTimePairingCodeManager(TimeSpan? lifetime = null)
    {
        _lifetime = lifetime ?? TimeSpan.FromMinutes(2);
        if (_lifetime <= TimeSpan.Zero || _lifetime > TimeSpan.FromMinutes(10))
            throw new ArgumentOutOfRangeException(nameof(lifetime));
    }

    public PairingCodeSnapshot Create(DateTimeOffset nowUtc)
    {
        var value = RandomNumberGenerator.GetInt32(0, 100_000_000);
        var code = value.ToString("D8", System.Globalization.CultureInfo.InvariantCulture);
        var expires = nowUtc.Add(_lifetime);

        lock (_gate)
        {
            _code = code;
            _expiresUtc = expires;
        }

        return new PairingCodeSnapshot(code, expires);
    }

    public bool TryConsume(string? candidate, DateTimeOffset nowUtc)
    {
        if (candidate is null || candidate.Length != 8 || candidate.Any(ch => ch is < '0' or > '9'))
            return false;

        string? expected;
        DateTimeOffset expires;
        lock (_gate)
        {
            expected = _code;
            expires = _expiresUtc;
            if (expected is null || nowUtc > expires) return false;

            var left = Encoding.ASCII.GetBytes(expected);
            var right = Encoding.ASCII.GetBytes(candidate);
            var matches = CryptographicOperations.FixedTimeEquals(left, right);
            CryptographicOperations.ZeroMemory(left);
            CryptographicOperations.ZeroMemory(right);
            if (!matches) return false;

            _code = null;
            _expiresUtc = default;
            return true;
        }
    }

    public void Invalidate()
    {
        lock (_gate)
        {
            _code = null;
            _expiresUtc = default;
        }
    }
}

public sealed record PairingCodeSnapshot(string Code, DateTimeOffset ExpiresUtc);
