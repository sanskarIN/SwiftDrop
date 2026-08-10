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
        await DatabaseSchemaManager.EnsureCurrentAsync(connection, ct);
    }

    public async Task AddAsync(TransferHistoryEntry entry, CancellationToken ct = default)
    {
        ValidateEntry(entry);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        await DatabaseSchemaManager.EnsureCurrentAsync(connection, ct);
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
        await DatabaseSchemaManager.EnsureCurrentAsync(connection, ct);
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
            var entry = TryReadEntry(reader);
            if (entry is not null) results.Add(entry);
        }
        return results;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        ValidateToken(id, nameof(id), 128);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        await DatabaseSchemaManager.EnsureCurrentAsync(connection, ct);
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM transfer_history WHERE id=$id;";
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task PruneOlderThanAsync(DateTimeOffset cutoffUtc, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        await DatabaseSchemaManager.EnsureCurrentAsync(connection, ct);
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM transfer_history WHERE timestamp_utc < $cutoff;";
        command.Parameters.AddWithValue("$cutoff", cutoffUtc.UtcDateTime.ToString("O"));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        await DatabaseSchemaManager.EnsureCurrentAsync(connection, ct);
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM transfer_history;";
        await command.ExecuteNonQueryAsync(ct);
    }

    private static TransferHistoryEntry? TryReadEntry(SqliteDataReader reader)
    {
        try
        {
            var entry = new TransferHistoryEntry(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt64(4),
                DateTimeOffset.Parse(reader.GetString(5), null, System.Globalization.DateTimeStyles.RoundtripKind),
                reader.GetString(6),
                reader.GetInt64(7) == 1);
            ValidateEntry(entry);
            return entry;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or ArgumentException or OverflowException)
        {
            return null;
        }
    }

    private static void ValidateEntry(TransferHistoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ValidateToken(entry.Id, nameof(entry.Id), 128);
        ValidateToken(entry.Direction, nameof(entry.Direction), 32);
        ValidateText(entry.PeerDeviceName, nameof(entry.PeerDeviceName), 256);
        ValidateText(entry.FileName, nameof(entry.FileName), 1024);
        ValidateToken(entry.Status, nameof(entry.Status), 64);
        if (entry.SizeBytes < 0) throw new ArgumentOutOfRangeException(nameof(entry.SizeBytes));
        if (entry.TimestampUtc < DateTimeOffset.UnixEpoch || entry.TimestampUtc > DateTimeOffset.UtcNow.AddDays(2))
            throw new ArgumentOutOfRangeException(nameof(entry.TimestampUtc));
    }

    private static void ValidateToken(string? value, string parameter, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength || value.Any(char.IsControl))
            throw new ArgumentException("History metadata token is invalid.", parameter);
    }

    private static void ValidateText(string? value, string parameter, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength || value.Any(ch => ch is '\r' or '\n'))
            throw new ArgumentException("History metadata text is invalid.", parameter);
    }
}
