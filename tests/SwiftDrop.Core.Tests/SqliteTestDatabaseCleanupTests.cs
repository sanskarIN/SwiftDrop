using Microsoft.Data.Sqlite;

namespace SwiftDrop.Core.Tests;

public sealed class SqliteTestDatabaseCleanupTests
{
    [Fact]
    public async Task Delete_RemovesDatabaseAfterPooledConnectionWasDisposed()
    {
        var path = Path.Combine(Path.GetTempPath(), $"swiftdrop-cleanup-{Guid.NewGuid():N}.db");
        try
        {
            await using (var connection = new SqliteConnection($"Data Source={path}"))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = "CREATE TABLE cleanup_probe(id INTEGER PRIMARY KEY);";
                await command.ExecuteNonQueryAsync();
            }

            Assert.True(File.Exists(path));

            SqliteTestDatabaseCleanup.Delete(path);

            Assert.False(File.Exists(path));
            Assert.False(File.Exists(path + "-shm"));
            Assert.False(File.Exists(path + "-wal"));
        }
        finally
        {
            SqliteTestDatabaseCleanup.Delete(path);
        }
    }
}
