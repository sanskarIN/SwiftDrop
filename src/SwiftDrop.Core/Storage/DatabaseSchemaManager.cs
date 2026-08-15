using Microsoft.Data.Sqlite;

namespace SwiftDrop.Core.Storage;

public static class DatabaseSchemaManager
{
    public const int CurrentVersion = 6;

    public static async Task EnsureCurrentAsync(SqliteConnection connection, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (connection.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("SQLite connection must be open before schema migration.");

        var version = await GetVersionAsync(connection, ct);
        if (version > CurrentVersion)
            throw new InvalidDataException($"SwiftDrop database schema {version} is newer than supported schema {CurrentVersion}.");

        if (version < 1)
        {
            await ApplyMigrationAsync(connection, """
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
                """, ct);
            version = 1;
        }

        if (version < 2)
        {
            await ApplyMigrationAsync(connection, """
                CREATE TABLE IF NOT EXISTS transfer_queue_metadata (
                    id TEXT PRIMARY KEY,
                    label TEXT NOT NULL,
                    state TEXT NOT NULL,
                    created_utc TEXT NOT NULL,
                    started_utc TEXT NULL,
                    finished_utc TEXT NULL,
                    error_code TEXT NULL
                );
                CREATE INDEX IF NOT EXISTS ix_transfer_queue_metadata_created ON transfer_queue_metadata(created_utc DESC);
                CREATE INDEX IF NOT EXISTS ix_transfer_queue_metadata_state ON transfer_queue_metadata(state);

                PRAGMA user_version = 2;
                """, ct);
            version = 2;
        }

        if (version < 3)
        {
            await ApplyMigrationAsync(connection, """
                CREATE TABLE IF NOT EXISTS completed_batch_items (
                    transfer_id TEXT NOT NULL,
                    source_relative_path TEXT NOT NULL,
                    receive_root_key TEXT NOT NULL,
                    destination_relative_path TEXT NOT NULL,
                    length INTEGER NOT NULL CHECK(length >= 0),
                    sha256 TEXT NOT NULL,
                    completed_utc TEXT NOT NULL,
                    PRIMARY KEY(transfer_id, source_relative_path, receive_root_key)
                );
                CREATE INDEX IF NOT EXISTS ix_completed_batch_items_completed
                    ON completed_batch_items(completed_utc DESC);

                PRAGMA user_version = 3;
                """, ct);
            version = 3;
        }

        if (version < 4)
        {
            await ApplyMigrationAsync(connection, """
                CREATE TABLE IF NOT EXISTS transfer_queue_metadata (
                    id TEXT PRIMARY KEY,
                    label TEXT NOT NULL,
                    state TEXT NOT NULL,
                    created_utc TEXT NOT NULL,
                    started_utc TEXT NULL,
                    finished_utc TEXT NULL,
                    error_code TEXT NULL
                );
                CREATE INDEX IF NOT EXISTS ix_transfer_queue_metadata_created ON transfer_queue_metadata(created_utc DESC);
                CREATE INDEX IF NOT EXISTS ix_transfer_queue_metadata_state ON transfer_queue_metadata(state);

                ALTER TABLE transfer_queue_metadata
                    ADD COLUMN operation_kind TEXT NOT NULL DEFAULT 'Transfer';
                ALTER TABLE transfer_queue_metadata
                    ADD COLUMN updated_utc TEXT NULL;
                ALTER TABLE transfer_queue_metadata
                    ADD COLUMN progress_basis_points INTEGER NOT NULL DEFAULT 0
                        CHECK(progress_basis_points >= 0 AND progress_basis_points <= 10000);
                ALTER TABLE transfer_queue_metadata
                    ADD COLUMN item_count INTEGER NULL
                        CHECK(item_count IS NULL OR item_count >= 0);
                ALTER TABLE transfer_queue_metadata
                    ADD COLUMN completed_item_count INTEGER NULL
                        CHECK(completed_item_count IS NULL OR completed_item_count >= 0);
                CREATE INDEX IF NOT EXISTS ix_transfer_queue_metadata_updated
                    ON transfer_queue_metadata(updated_utc DESC);

                PRAGMA user_version = 4;
                """, ct);
            version = 4;
        }

        if (version < 5)
        {
            await ApplyMigrationAsync(connection, """
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

                ALTER TABLE transfer_history
                    ADD COLUMN duration_ms INTEGER NULL
                        CHECK(duration_ms IS NULL OR (duration_ms >= 0 AND duration_ms <= 604800000));

                PRAGMA user_version = 5;
                """, ct);
            version = 5;
        }

        if (version < 6)
        {
            await ApplyMigrationAsync(connection, """
                ALTER TABLE transfer_history
                    ADD COLUMN measured_bytes INTEGER NULL
                        CHECK(measured_bytes IS NULL OR measured_bytes >= 0);

                PRAGMA user_version = 6;
                """, ct);
        }
    }

    public static async Task<int> GetVersionAsync(SqliteConnection connection, CancellationToken ct = default)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        var value = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task ApplyMigrationAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken ct)
    {
        await using var transaction = await connection.BeginTransactionAsync(ct);
        using var command = connection.CreateCommand();
        command.Transaction = (SqliteTransaction)transaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
