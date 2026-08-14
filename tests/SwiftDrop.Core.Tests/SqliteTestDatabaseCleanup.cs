using Microsoft.Data.Sqlite;

namespace SwiftDrop.Core.Tests;

internal static class SqliteTestDatabaseCleanup
{
    public static void Delete(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // Microsoft.Data.Sqlite pools native connections by default. A disposed
        // connection can therefore keep the database file open on Windows until
        // its pool is cleared. Tests create isolated temporary databases, so
        // clearing pools before cleanup is safe and makes teardown portable.
        SqliteConnection.ClearAllPools();

        foreach (var suffix in new[] { string.Empty, "-shm", "-wal" })
        {
            var candidate = path + suffix;
            if (File.Exists(candidate))
                File.Delete(candidate);
        }
    }
}
