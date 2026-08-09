using Microsoft.Data.Sqlite;
using SwiftDrop.Core.Models;

namespace SwiftDrop.Core.Storage;

public sealed class TransferHistoryStore
{
    private readonly string _connectionString;

    public TransferHistoryStore(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(databasePath))!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS transfer_history (
                id TEXT PRIMARY KEY,
                direction TEXT NOT NULL,
                peer_device_name TEXT NOT NULL,
                file_name TEXT NOT NULL,
                size_bytes INTEGER NOT NULL CHECK(size_bytes >= 0),
                timestamp_utc TEXT NOT NULL,
                status TEXT NOT NULL,
                integrity_verified INTEGER NOT NULL CHECK(integrity_verified IN (0, 1))
            );
            CREATE INDEX IF NOT EXISTS ix_transfer_history_timestamp
                ON transfer_history(timestamp_utc DESC);
            """;
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task AddAsync(TransferHistoryEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR REPLACE INTO transfer_history
            (id, direction, peer_device_name, file_name, size_bytes, timestamp_utc, status, integrity_verified)
            VALUES ($id, $direction, $peer, $file, $size, $timestamp, $status, $verified);
            """;
        command.Parameters.AddWithValue("$id", entry.Id);
        command.Parameters.AddWithValue("$direction", entry.Direction);
        command.Parameters.AddWithValue("$peer", entry.PeerDeviceName);
        command.Parameters.AddWithValue("$file", entry.FileName);
        command.Parameters.AddWithValue("$size", entry.SizeBytes);
        command.Parameters.AddWithValue("$timestamp", entry.TimestampUtc.UtcDateTime.ToString("O"));
        command.Parameters.AddWithValue("$status", entry.Status);
        command.Parameters.AddWithValue("$verified", entry.IntegrityVerified ? 1 : 0);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<TransferHistoryEntry>> GetRecentAsync(int limit = 100, CancellationToken ct = default)
    {
        if (limit is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(limit));
        var results = new List<TransferHistoryEntry>();
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, direction, peer_device_name, file_name, size_bytes, timestamp_utc, status, integrity_verified
            FROM transfer_history
            ORDER BY timestamp_utc DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$limit", limit);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new TransferHistoryEntry(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt64(4),
                DateTimeOffset.Parse(reader.GetString(5), null, System.Globalization.DateTimeStyles.RoundtripKind),
                reader.GetString(6),
                reader.GetInt64(7) == 1));
        }
        return results;
    }

    public async Task<int> PruneBeforeAsync(DateTimeOffset cutoffUtc, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM transfer_history WHERE timestamp_utc < $cutoff;";
        command.Parameters.AddWithValue("$cutoff", cutoffUtc.UtcDateTime.ToString("O"));
        return await command.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM transfer_history WHERE id=$id;";
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM transfer_history;";
        await command.ExecuteNonQueryAsync(ct);
    }
}
