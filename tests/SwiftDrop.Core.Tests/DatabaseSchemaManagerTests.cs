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
            await using var connection = new SqliteConnection($"Data Source={path}");
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
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task EnsureCurrentAsync_MigratesVersionOneToCurrentSchema()
    {
        var path = TempDatabasePath();
        try
        {
            await using var connection = new SqliteConnection($"Data Source={path}");
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version = 1;";
            await command.ExecuteNonQueryAsync();

            await DatabaseSchemaManager.EnsureCurrentAsync(connection);

            Assert.Equal(DatabaseSchemaManager.CurrentVersion, await DatabaseSchemaManager.GetVersionAsync(connection));
            var tables = await ReadTablesAsync(connection);
            Assert.Contains("transfer_queue_metadata", tables);
            Assert.Contains("completed_batch_items", tables);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task EnsureCurrentAsync_MigratesVersionTwoToBatchCompletionSchema()
    {
        var path = TempDatabasePath();
        try
        {
            await using var connection = new SqliteConnection($"Data Source={path}");
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version = 2;";
            await command.ExecuteNonQueryAsync();

            await DatabaseSchemaManager.EnsureCurrentAsync(connection);

            Assert.Equal(3, await DatabaseSchemaManager.GetVersionAsync(connection));
            Assert.Contains("completed_batch_items", await ReadTablesAsync(connection));
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task EnsureCurrentAsync_IsIdempotentAtCurrentVersion()
    {
        var path = TempDatabasePath();
        try
        {
            await using var connection = new SqliteConnection($"Data Source={path}");
            await connection.OpenAsync();
            await DatabaseSchemaManager.EnsureCurrentAsync(connection);
            await DatabaseSchemaManager.EnsureCurrentAsync(connection);
            Assert.Equal(DatabaseSchemaManager.CurrentVersion, await DatabaseSchemaManager.GetVersionAsync(connection));
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task EnsureCurrentAsync_RejectsUnknownFutureSchema()
    {
        var path = TempDatabasePath();
        try
        {
            await using var connection = new SqliteConnection($"Data Source={path}");
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version = 999;";
            await command.ExecuteNonQueryAsync();

            await Assert.ThrowsAsync<InvalidDataException>(() => DatabaseSchemaManager.EnsureCurrentAsync(connection));
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    private static async Task<HashSet<string>> ReadTablesAsync(SqliteConnection connection)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table';";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) result.Add(reader.GetString(0));
        return result;
    }

    private static string TempDatabasePath()
        => Path.Combine(Path.GetTempPath(), "swiftdrop-schema-" + Guid.NewGuid().ToString("N") + ".db");

    private static void DeleteDatabase(string path)
    {
        foreach (var candidate in new[] { path, path + "-shm", path + "-wal" })
            if (File.Exists(candidate)) File.Delete(candidate);
    }
}
