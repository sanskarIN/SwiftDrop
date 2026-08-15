using Microsoft.Data.Sqlite;
using SwiftDrop.Core.Storage;

namespace SwiftDrop.Core.Tests;

public sealed class DatabaseSchemaManagerTests
{
    [Fact]
    public async Task EnsureCurrentAsync_MigratesVersionZeroDatabase()
    {
        var path = TempDatabasePath();
        try
        {
            await using (var connection = new SqliteConnection($"Data Source={path}"))
            {
                await connection.OpenAsync();

                Assert.Equal(0, await DatabaseSchemaManager.GetVersionAsync(connection));
                await DatabaseSchemaManager.EnsureCurrentAsync(connection);
                Assert.Equal(DatabaseSchemaManager.CurrentVersion, await DatabaseSchemaManager.GetVersionAsync(connection));

                var tables = await ReadTablesAsync(connection);
                Assert.Contains("trusted_peers", tables);
                Assert.Contains("transfer_history", tables);
                Assert.Contains("diagnostic_events", tables);
                Assert.Contains("transfer_queue_metadata", tables);
                Assert.Contains("completed_batch_items", tables);

                var queueColumns = await ReadColumnsAsync(connection, "transfer_queue_metadata");
                Assert.Contains("operation_kind", queueColumns);
                Assert.Contains("updated_utc", queueColumns);
                Assert.Contains("progress_basis_points", queueColumns);
                Assert.Contains("item_count", queueColumns);
                Assert.Contains("completed_item_count", queueColumns);
                Assert.Contains("duration_ms", await ReadColumnsAsync(connection, "transfer_history"));
            }
        }
        finally
        {
            SqliteTestDatabaseCleanup.Delete(path);
        }
    }

    [Fact]
    public async Task EnsureCurrentAsync_MigratesVersionOneToCurrentSchema()
    {
        var path = TempDatabasePath();
        try
        {
            await using (var connection = new SqliteConnection($"Data Source={path}"))
            {
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA user_version = 1;";
                await command.ExecuteNonQueryAsync();

                await DatabaseSchemaManager.EnsureCurrentAsync(connection);

                Assert.Equal(DatabaseSchemaManager.CurrentVersion, await DatabaseSchemaManager.GetVersionAsync(connection));
                var tables = await ReadTablesAsync(connection);
                Assert.Contains("transfer_queue_metadata", tables);
                Assert.Contains("completed_batch_items", tables);
                Assert.Contains("transfer_history", tables);
                Assert.Contains("duration_ms", await ReadColumnsAsync(connection, "transfer_history"));
            }
        }
        finally
        {
            SqliteTestDatabaseCleanup.Delete(path);
        }
    }

    [Fact]
    public async Task EnsureCurrentAsync_MigratesVersionTwoToCurrentSchema()
    {
        var path = TempDatabasePath();
        try
        {
            await using (var connection = new SqliteConnection($"Data Source={path}"))
            {
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA user_version = 2;";
                await command.ExecuteNonQueryAsync();

                await DatabaseSchemaManager.EnsureCurrentAsync(connection);

                Assert.Equal(DatabaseSchemaManager.CurrentVersion, await DatabaseSchemaManager.GetVersionAsync(connection));
                Assert.Contains("completed_batch_items", await ReadTablesAsync(connection));
                Assert.Contains("progress_basis_points", await ReadColumnsAsync(connection, "transfer_queue_metadata"));
                Assert.Contains("duration_ms", await ReadColumnsAsync(connection, "transfer_history"));
            }
        }
        finally
        {
            SqliteTestDatabaseCleanup.Delete(path);
        }
    }

    [Fact]
    public async Task EnsureCurrentAsync_MigratesLegacyVersionThreeQueueRowsWithoutDataLoss()
    {
        var path = TempDatabasePath();
        try
        {
            await using (var connection = new SqliteConnection($"Data Source={path}"))
            {
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = """
                    CREATE TABLE transfer_queue_metadata (
                        id TEXT PRIMARY KEY,
                        label TEXT NOT NULL,
                        state TEXT NOT NULL,
                        created_utc TEXT NOT NULL,
                        started_utc TEXT NULL,
                        finished_utc TEXT NULL,
                        error_code TEXT NULL
                    );
                    INSERT INTO transfer_queue_metadata
                        (id, label, state, created_utc)
                    VALUES
                        ('legacy', 'Transfer', 'Queued', '2026-08-15T00:00:00.0000000Z');
                    PRAGMA user_version = 3;
                    """;
                await command.ExecuteNonQueryAsync();

                await DatabaseSchemaManager.EnsureCurrentAsync(connection);

                Assert.Equal(DatabaseSchemaManager.CurrentVersion, await DatabaseSchemaManager.GetVersionAsync(connection));
                using var read = connection.CreateCommand();
                read.CommandText = """
                    SELECT operation_kind, progress_basis_points, item_count, completed_item_count
                    FROM transfer_queue_metadata
                    WHERE id='legacy';
                    """;
                await using var reader = await read.ExecuteReaderAsync();
                Assert.True(await reader.ReadAsync());
                Assert.Equal("Transfer", reader.GetString(0));
                Assert.Equal(0, reader.GetInt32(1));
                Assert.True(reader.IsDBNull(2));
                Assert.True(reader.IsDBNull(3));
            }
        }
        finally
        {
            SqliteTestDatabaseCleanup.Delete(path);
        }
    }

    [Fact]
    public async Task EnsureCurrentAsync_MigratesVersionFourHistoryRowsWithNullDuration()
    {
        var path = TempDatabasePath();
        try
        {
            await using (var connection = new SqliteConnection($"Data Source={path}"))
            {
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = """
                    CREATE TABLE transfer_history (
                        id TEXT PRIMARY KEY,
                        direction TEXT NOT NULL,
                        peer_device_name TEXT NOT NULL,
                        file_name TEXT NOT NULL,
                        size_bytes INTEGER NOT NULL CHECK(size_bytes >= 0),
                        timestamp_utc TEXT NOT NULL,
                        status TEXT NOT NULL,
                        integrity_verified INTEGER NOT NULL CHECK(integrity_verified IN (0, 1))
                    );
                    INSERT INTO transfer_history
                        (id, direction, peer_device_name, file_name, size_bytes, timestamp_utc, status, integrity_verified)
                    VALUES
                        ('legacy-history', 'sent', 'Phone', 'file.bin', 42, '2026-08-15T00:00:00.0000000Z', 'completed', 1);
                    PRAGMA user_version = 4;
                    """;
                await command.ExecuteNonQueryAsync();

                await DatabaseSchemaManager.EnsureCurrentAsync(connection);

                Assert.Equal(5, await DatabaseSchemaManager.GetVersionAsync(connection));
                Assert.Contains("duration_ms", await ReadColumnsAsync(connection, "transfer_history"));
                using var read = connection.CreateCommand();
                read.CommandText = "SELECT duration_ms FROM transfer_history WHERE id='legacy-history';";
                Assert.Equal(DBNull.Value, await read.ExecuteScalarAsync());
            }
        }
        finally
        {
            SqliteTestDatabaseCleanup.Delete(path);
        }
    }

    [Fact]
    public async Task EnsureCurrentAsync_IsIdempotentAtCurrentVersion()
    {
        var path = TempDatabasePath();
        try
        {
            await using (var connection = new SqliteConnection($"Data Source={path}"))
            {
                await connection.OpenAsync();
                await DatabaseSchemaManager.EnsureCurrentAsync(connection);
                await DatabaseSchemaManager.EnsureCurrentAsync(connection);
                Assert.Equal(DatabaseSchemaManager.CurrentVersion, await DatabaseSchemaManager.GetVersionAsync(connection));
            }
        }
        finally
        {
            SqliteTestDatabaseCleanup.Delete(path);
        }
    }

    [Fact]
    public async Task EnsureCurrentAsync_RejectsUnknownFutureSchema()
    {
        var path = TempDatabasePath();
        try
        {
            await using (var connection = new SqliteConnection($"Data Source={path}"))
            {
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA user_version = 999;";
                await command.ExecuteNonQueryAsync();

                await Assert.ThrowsAsync<InvalidDataException>(() => DatabaseSchemaManager.EnsureCurrentAsync(connection));
            }
        }
        finally
        {
            SqliteTestDatabaseCleanup.Delete(path);
        }
    }

    private static async Task<HashSet<string>> ReadTablesAsync(SqliteConnection connection)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table';";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) result.Add(reader.GetString(0));
        return result;
    }

    private static async Task<HashSet<string>> ReadColumnsAsync(SqliteConnection connection, string table)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({table});";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) result.Add(reader.GetString(1));
        return result;
    }

    private static string TempDatabasePath()
        => Path.Combine(Path.GetTempPath(), "swiftdrop-schema-" + Guid.NewGuid().ToString("N") + ".db");
}
