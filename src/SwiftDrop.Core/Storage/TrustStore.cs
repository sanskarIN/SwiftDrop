using Microsoft.Data.Sqlite;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Storage;

public sealed class TrustStore
{
    private readonly string _connectionString;

    public TrustStore(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(databasePath))!);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var db = new SqliteConnection(_connectionString);
        await db.OpenAsync(ct);
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS trusted_peers(
                device_id TEXT PRIMARY KEY,
                device_name TEXT NOT NULL,
                fingerprint TEXT NOT NULL,
                trusted_utc TEXT NOT NULL,
                last_seen_utc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_trusted_peers_last_seen ON trusted_peers(last_seen_utc DESC);
            """;
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpsertAsync(TrustedPeer peer, CancellationToken ct = default)
    {
        var normalized = NormalizePeer(peer);
        await using var db = new SqliteConnection(_connectionString);
        await db.OpenAsync(ct);
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            INSERT INTO trusted_peers(device_id,device_name,fingerprint,trusted_utc,last_seen_utc)
            VALUES($id,$name,$fp,$trusted,$seen)
            ON CONFLICT(device_id) DO UPDATE SET
              device_name=excluded.device_name,
              fingerprint=excluded.fingerprint,
              last_seen_utc=excluded.last_seen_utc;
            """;
        cmd.Parameters.AddWithValue("$id", normalized.DeviceId);
        cmd.Parameters.AddWithValue("$name", normalized.DeviceName);
        cmd.Parameters.AddWithValue("$fp", normalized.CertificateFingerprint);
        cmd.Parameters.AddWithValue("$trusted", normalized.TrustedAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$seen", normalized.LastSeenUtc.ToString("O"));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<TrustedPeer?> GetAsync(string deviceId, CancellationToken ct = default)
    {
        ValidateDeviceId(deviceId);
        await using var db = new SqliteConnection(_connectionString);
        await db.OpenAsync(ct);
        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT device_name,fingerprint,trusted_utc,last_seen_utc FROM trusted_peers WHERE device_id=$id";
        cmd.Parameters.AddWithValue("$id", deviceId.Trim());
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        return TryReadPeer(deviceId.Trim(), r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3));
    }

    public async Task<IReadOnlyList<TrustedPeer>> GetAllAsync(CancellationToken ct = default)
    {
        var peers = new List<TrustedPeer>();
        await using var db = new SqliteConnection(_connectionString);
        await db.OpenAsync(ct);
        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT device_id,device_name,fingerprint,trusted_utc,last_seen_utc FROM trusted_peers ORDER BY last_seen_utc DESC";
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            var peer = TryReadPeer(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4));
            if (peer is not null) peers.Add(peer);
        }
        return peers;
    }

    public async Task RemoveAsync(string deviceId, CancellationToken ct = default)
    {
        ValidateDeviceId(deviceId);
        await using var db = new SqliteConnection(_connectionString);
        await db.OpenAsync(ct);
        using var cmd = db.CreateCommand();
        cmd.CommandText = "DELETE FROM trusted_peers WHERE device_id=$id";
        cmd.Parameters.AddWithValue("$id", deviceId.Trim());
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        await using var db = new SqliteConnection(_connectionString);
        await db.OpenAsync(ct);
        using var cmd = db.CreateCommand();
        cmd.CommandText = "DELETE FROM trusted_peers";
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static TrustedPeer NormalizePeer(TrustedPeer peer)
    {
        ArgumentNullException.ThrowIfNull(peer);
        ValidateDeviceId(peer.DeviceId);
        var name = peer.DeviceName?.Trim() ?? string.Empty;
        if (name.Length is 0 or > 128 || name.Any(char.IsControl))
            throw new ArgumentException("Trusted device name is invalid.", nameof(peer));
        var fingerprint = Fingerprint.NormalizeSha256(peer.CertificateFingerprint);
        return peer with
        {
            DeviceId = peer.DeviceId.Trim(),
            DeviceName = name,
            CertificateFingerprint = fingerprint
        };
    }

    private static TrustedPeer? TryReadPeer(
        string deviceId,
        string deviceName,
        string fingerprint,
        string trustedUtc,
        string lastSeenUtc)
    {
        try
        {
            var peer = new TrustedPeer(
                deviceId,
                deviceName,
                fingerprint,
                DateTimeOffset.Parse(trustedUtc, null, System.Globalization.DateTimeStyles.RoundtripKind),
                DateTimeOffset.Parse(lastSeenUtc, null, System.Globalization.DateTimeStyles.RoundtripKind));
            return NormalizePeer(peer);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or OverflowException)
        {
            return null;
        }
    }

    private static void ValidateDeviceId(string? deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId) || deviceId.Length > 128 || deviceId.Any(char.IsControl))
            throw new ArgumentException("Trusted device ID is invalid.", nameof(deviceId));
    }
}
