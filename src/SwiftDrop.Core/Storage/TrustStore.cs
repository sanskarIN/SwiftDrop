using Microsoft.Data.Sqlite;
using SwiftDrop.Core.Models;

namespace SwiftDrop.Core.Storage;

public sealed class TrustStore
{
    private readonly string _connectionString;
    public TrustStore(string databasePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadWriteCreate }.ToString();
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var db = new SqliteConnection(_connectionString);
        await db.OpenAsync(ct);
        var cmd = db.CreateCommand();
        cmd.CommandText = "CREATE TABLE IF NOT EXISTS trusted_peers(device_id TEXT PRIMARY KEY, device_name TEXT NOT NULL, fingerprint TEXT NOT NULL, trusted_utc TEXT NOT NULL, last_seen_utc TEXT NOT NULL);";
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpsertAsync(TrustedPeer peer, CancellationToken ct = default)
    {
        await using var db = new SqliteConnection(_connectionString);
        await db.OpenAsync(ct);
        var cmd = db.CreateCommand();
        cmd.CommandText = "INSERT INTO trusted_peers(device_id,device_name,fingerprint,trusted_utc,last_seen_utc) VALUES($id,$name,$fp,$trusted,$seen) ON CONFLICT(device_id) DO UPDATE SET device_name=excluded.device_name,fingerprint=excluded.fingerprint,last_seen_utc=excluded.last_seen_utc;";
        cmd.Parameters.AddWithValue("$id", peer.DeviceId); cmd.Parameters.AddWithValue("$name", peer.DeviceName); cmd.Parameters.AddWithValue("$fp", peer.CertificateFingerprint); cmd.Parameters.AddWithValue("$trusted", peer.TrustedAtUtc.ToString("O")); cmd.Parameters.AddWithValue("$seen", peer.LastSeenUtc.ToString("O"));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<TrustedPeer?> GetAsync(string deviceId, CancellationToken ct = default)
    {
        await using var db = new SqliteConnection(_connectionString); await db.OpenAsync(ct);
        var cmd = db.CreateCommand(); cmd.CommandText = "SELECT device_name,fingerprint,trusted_utc,last_seen_utc FROM trusted_peers WHERE device_id=$id"; cmd.Parameters.AddWithValue("$id", deviceId);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        return new(deviceId, r.GetString(0), r.GetString(1), DateTimeOffset.Parse(r.GetString(2)), DateTimeOffset.Parse(r.GetString(3)));
    }

    public async Task RemoveAsync(string deviceId, CancellationToken ct = default)
    {
        await using var db = new SqliteConnection(_connectionString); await db.OpenAsync(ct);
        var cmd = db.CreateCommand(); cmd.CommandText = "DELETE FROM trusted_peers WHERE device_id=$id"; cmd.Parameters.AddWithValue("$id", deviceId); await cmd.ExecuteNonQueryAsync(ct);
    }
}
