using Microsoft.Data.Sqlite;
using SwiftDrop.Core.Models;

namespace SwiftDrop.Core.Storage;

public sealed class TransferQueueMetadataStore
{
    private static readonly HashSet<string> AllowedStates = new(StringComparer.Ordinal)
    {
        "Queued", "Running", "Completed", "Failed", "Cancelled", "Interrupted"
    };

    private static readonly HashSet<string> AllowedOperationKinds = new(StringComparer.Ordinal)
    {
        "Transfer", "File", "Batch", "Text", "Receive"
    };

    private readonly string _connectionString;

    public TransferQueueMetadataStore(string databasePath)
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

    public async Task UpsertAsync(TransferQueueMetadataEntry entry, CancellationToken ct = default)
    {
        Validate(entry);
        var updatedUtc = entry.UpdatedUtc ?? entry.CreatedUtc;
        await using var connection = await OpenAsync(ct);
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO transfer_queue_metadata
            (id, label, state, created_utc, started_utc, finished_utc, error_code,
             operation_kind, updated_utc, progress_basis_points, item_count, completed_item_count)
            VALUES ($id, $label, $state, $created, $started, $finished, $error,
                    $operation, $updated, $progress, $itemCount, $completedItemCount)
            ON CONFLICT(id) DO UPDATE SET
                label=excluded.label,
                state=excluded.state,
                created_utc=excluded.created_utc,
                started_utc=excluded.started_utc,
                finished_utc=excluded.finished_utc,
                error_code=excluded.error_code,
                operation_kind=excluded.operation_kind,
                updated_utc=excluded.updated_utc,
                progress_basis_points=excluded.progress_basis_points,
                item_count=excluded.item_count,
                completed_item_count=excluded.completed_item_count;
            """;
        command.Parameters.AddWithValue("$id", entry.Id);
        command.Parameters.AddWithValue("$label", entry.Label);
        command.Parameters.AddWithValue("$state", entry.State);
        command.Parameters.AddWithValue("$created", Format(entry.CreatedUtc));
        command.Parameters.AddWithValue("$started", DbValue(entry.StartedUtc));
        command.Parameters.AddWithValue("$finished", DbValue(entry.FinishedUtc));
        command.Parameters.AddWithValue("$error", (object?)entry.ErrorCode ?? DBNull.Value);
        command.Parameters.AddWithValue("$operation", entry.OperationKind);
        command.Parameters.AddWithValue("$updated", Format(updatedUtc));
        command.Parameters.AddWithValue("$progress", entry.ProgressBasisPoints);
        command.Parameters.AddWithValue("$itemCount", (object?)entry.ItemCount ?? DBNull.Value);
        command.Parameters.AddWithValue("$completedItemCount", (object?)entry.CompletedItemCount ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<TransferQueueMetadataEntry>> GetRecentAsync(
        int limit = 100,
        CancellationToken ct = default)
    {
        if (limit is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(limit));
        var results = new List<TransferQueueMetadataEntry>();
        await using var connection = await OpenAsync(ct);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, label, state, created_utc, started_utc, finished_utc, error_code,
                   operation_kind, updated_utc, progress_basis_points, item_count, completed_item_count
            FROM transfer_queue_metadata
            ORDER BY created_utc DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$limit", limit);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var createdUtc = Parse(reader.GetString(3));
            results.Add(new TransferQueueMetadataEntry(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                createdUtc,
                reader.IsDBNull(4) ? null : Parse(reader.GetString(4)),
                reader.IsDBNull(5) ? null : Parse(reader.GetString(5)),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.GetString(7),
                reader.IsDBNull(8) ? createdUtc : Parse(reader.GetString(8)),
                reader.GetInt32(9),
                reader.IsDBNull(10) ? null : reader.GetInt32(10),
                reader.IsDBNull(11) ? null : reader.GetInt32(11)));
        }
        return results;
    }

    public async Task MarkInFlightInterruptedAsync(DateTimeOffset finishedUtc, CancellationToken ct = default)
    {
        await using var connection = await OpenAsync(ct);
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE transfer_queue_metadata
            SET state='Interrupted', finished_utc=$finished, updated_utc=$finished, error_code='app-restarted'
            WHERE state IN ('Queued', 'Running');
            """;
        command.Parameters.AddWithValue("$finished", Format(finishedUtc));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteFinishedAsync(CancellationToken ct = default)
    {
        await using var connection = await OpenAsync(ct);
        using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM transfer_queue_metadata
            WHERE state IN ('Completed', 'Failed', 'Cancelled', 'Interrupted');
            """;
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task TrimAsync(int keepLatest = 100, CancellationToken ct = default)
    {
        if (keepLatest is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(keepLatest));
        await using var connection = await OpenAsync(ct);
        using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM transfer_queue_metadata
            WHERE id IN (
                SELECT id FROM transfer_queue_metadata
                ORDER BY created_utc DESC
                LIMIT -1 OFFSET $keep
            );
            """;
        command.Parameters.AddWithValue("$keep", keepLatest);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        await using var connection = await OpenAsync(ct);
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM transfer_queue_metadata;";
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken ct)
    {
        var connection = new SqliteConnection(_connectionString);
        try
        {
            await connection.OpenAsync(ct);
            await DatabaseSchemaManager.EnsureCurrentAsync(connection, ct);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private static void Validate(TransferQueueMetadataEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (string.IsNullOrWhiteSpace(entry.Id) || entry.Id.Length > 64 || entry.Id.Any(char.IsControl))
            throw new InvalidDataException("Invalid queue metadata ID.");
        if (string.IsNullOrWhiteSpace(entry.Label) || entry.Label.Length > 64 || entry.Label.Any(char.IsControl))
            throw new InvalidDataException("Invalid queue metadata label.");
        if (!AllowedStates.Contains(entry.State))
            throw new InvalidDataException("Invalid queue metadata state.");
        if (!AllowedOperationKinds.Contains(entry.OperationKind))
            throw new InvalidDataException("Invalid queue metadata operation kind.");
        if (entry.CreatedUtc < DateTimeOffset.UnixEpoch || entry.CreatedUtc > DateTimeOffset.UtcNow.AddDays(2))
            throw new InvalidDataException("Invalid queue metadata timestamp.");

        var updatedUtc = entry.UpdatedUtc ?? entry.CreatedUtc;
        if (updatedUtc < entry.CreatedUtc || updatedUtc > DateTimeOffset.UtcNow.AddDays(2))
            throw new InvalidDataException("Invalid queue metadata update timestamp.");
        if (entry.StartedUtc is { } startedUtc && startedUtc < entry.CreatedUtc)
            throw new InvalidDataException("Queue metadata start timestamp predates creation.");
        if (entry.FinishedUtc is { } finishedUtc && finishedUtc < entry.CreatedUtc)
            throw new InvalidDataException("Queue metadata finish timestamp predates creation.");
        if (entry.ProgressBasisPoints is < 0 or > 10_000)
            throw new InvalidDataException("Invalid queue metadata progress.");
        if (entry.ItemCount is < 0 || entry.CompletedItemCount is < 0)
            throw new InvalidDataException("Invalid queue metadata item count.");
        if (entry.ItemCount is { } itemCount && entry.CompletedItemCount is { } completed && completed > itemCount)
            throw new InvalidDataException("Completed queue item count exceeds total item count.");
        if (entry.ErrorCode is { Length: > 64 } ||
            entry.ErrorCode is not null && entry.ErrorCode.Any(ch => !char.IsAsciiLetterOrDigit(ch) && ch is not '-' and not '_' and not '.'))
            throw new InvalidDataException("Invalid queue metadata error code.");
    }

    private static string Format(DateTimeOffset value) => value.UtcDateTime.ToString("O");
    private static object DbValue(DateTimeOffset? value) => value is null ? DBNull.Value : Format(value.Value);
    private static DateTimeOffset Parse(string value)
        => DateTimeOffset.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);
}
