using Microsoft.Data.Sqlite;
using SwiftDrop.Core.Models;

namespace SwiftDrop.Core.Storage;

public sealed class DiagnosticEventStore
{
    private readonly string _connectionString;

    public DiagnosticEventStore(string databasePath)
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
            CREATE TABLE IF NOT EXISTS diagnostic_events (
                id TEXT PRIMARY KEY,
                timestamp_utc TEXT NOT NULL,
                level TEXT NOT NULL,
                code TEXT NOT NULL,
                message TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_diagnostic_events_timestamp
                ON diagnostic_events(timestamp_utc DESC);
            """;
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task AddAsync(DiagnosticEvent entry, CancellationToken ct = default)
    {
        Validate(entry);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO diagnostic_events(id,timestamp_utc,level,code,message)
            VALUES($id,$timestamp,$level,$code,$message);
            """;
        command.Parameters.AddWithValue("$id", entry.Id);
        command.Parameters.AddWithValue("$timestamp", entry.TimestampUtc.UtcDateTime.ToString("O"));
        command.Parameters.AddWithValue("$level", entry.Level);
        command.Parameters.AddWithValue("$code", entry.Code);
        command.Parameters.AddWithValue("$message", entry.Message);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<DiagnosticEvent>> GetRecentAsync(int limit = 200, CancellationToken ct = default)
    {
        if (limit is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(limit));
        var entries = new List<DiagnosticEvent>();
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id,timestamp_utc,level,code,message
            FROM diagnostic_events
            ORDER BY timestamp_utc DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$limit", limit);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var entry = TryRead(reader);
            if (entry is not null) entries.Add(entry);
        }
        return entries;
    }

    public async Task PruneAsync(DateTimeOffset olderThanUtc, int maximumRows = 1000, CancellationToken ct = default)
    {
        if (maximumRows is < 10 or > 10000) throw new ArgumentOutOfRangeException(nameof(maximumRows));
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM diagnostic_events WHERE timestamp_utc < $cutoff;
            DELETE FROM diagnostic_events
            WHERE id NOT IN (
                SELECT id FROM diagnostic_events ORDER BY timestamp_utc DESC LIMIT $maxRows
            );
            """;
        command.Parameters.AddWithValue("$cutoff", olderThanUtc.UtcDateTime.ToString("O"));
        command.Parameters.AddWithValue("$maxRows", maximumRows);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM diagnostic_events;";
        await command.ExecuteNonQueryAsync(ct);
    }

    private static DiagnosticEvent? TryRead(SqliteDataReader reader)
    {
        try
        {
            var entry = new DiagnosticEvent(
                reader.GetString(0),
                DateTimeOffset.Parse(reader.GetString(1), null, System.Globalization.DateTimeStyles.RoundtripKind),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4));
            Validate(entry);
            return entry;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or ArgumentException or OverflowException)
        {
            return null;
        }
    }

    private static void Validate(DiagnosticEvent entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (string.IsNullOrWhiteSpace(entry.Id) || entry.Id.Length > 64 || entry.Id.Any(char.IsControl))
            throw new ArgumentException("Invalid diagnostic event id.", nameof(entry));
        if (entry.TimestampUtc < DateTimeOffset.UnixEpoch || entry.TimestampUtc > DateTimeOffset.UtcNow.AddDays(2))
            throw new ArgumentException("Invalid diagnostic timestamp.", nameof(entry));
        if (string.IsNullOrWhiteSpace(entry.Level) || entry.Level.Length > 16 || entry.Level.Any(char.IsControl))
            throw new ArgumentException("Invalid diagnostic level.", nameof(entry));
        if (string.IsNullOrWhiteSpace(entry.Code) || entry.Code.Length > 96 || entry.Code.Any(char.IsControl))
            throw new ArgumentException("Invalid diagnostic code.", nameof(entry));
        if (entry.Message.Length > 512 || entry.Message.Any(c => c is '\r' or '\n'))
            throw new ArgumentException("Diagnostic message is too long or multiline.", nameof(entry));
    }
}
