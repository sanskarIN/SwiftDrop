using Microsoft.Data.Sqlite;

namespace SwiftDrop.Core.Storage;

public static class DatabaseSchemaManager
{
    public const int CurrentVersion = 1;

    public static async Task EnsureCurrentAsync(SqliteConnection connection, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (connection.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("SQLite connection must be open before schema migration.");

        var version = await GetVersionAsync(connection, ct);
        if (version > CurrentVersion)
            throw new InvalidDataException($"SwiftDrop database schema {version} is newer than supported schema {CurrentVersion}.");
        if (version == CurrentVersion) return;

        await using var transaction = await connection.BeginTransactionAsync(ct);
        var command = connection.CreateCommand();
        command.Transaction = (SqliteTransaction)transaction;
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS trusted_peers(
                device_id TEXT PRIMARY KEY,
                device_name TEXT NOT NULL,
                fingerprint TEXT NOT NULL,
                trusted_utc TEXT NOT NULL,
                last_seen_utc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_trusted_peers_last_seen ON trusted_peers(last_seen_utc DESC);

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
            CREATE INDEX IF NOT EXISTS ix_transfer_history_timestamp ON transfer_history(timestamp_utc DESC);

            CREATE TABLE IF NOT EXISTS diagnostic_events (
                id TEXT PRIMARY KEY,
                timestamp_utc TEXT NOT NULL,
                level TEXT NOT NULL,
                code TEXT NOT NULL,
                message TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_diagnostic_events_timestamp ON diagnostic_events(timestamp_utc DESC);

            PRAGMA user_version = 1;
            """;
        await command.ExecuteNonQueryAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public static async Task<int> GetVersionAsync(SqliteConnection connection, CancellationToken ct = default)
    {
        var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        var value = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
    }
}
