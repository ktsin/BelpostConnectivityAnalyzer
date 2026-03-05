using Microsoft.Data.Sqlite;

namespace BelpostConnectivityAnalyzer.Data;

public sealed class SyncLogRepository(DbConnectionFactory connectionFactory)
{
    public long Insert(DateTime startedAt)
    {
        using var connection = connectionFactory.CreateConnection();
        using var command = new SqliteCommand(
            "INSERT INTO sync_log (started_at, status) VALUES (@startedAt, 'running'); SELECT last_insert_rowid();",
            connection);
        command.Parameters.AddWithValue("@startedAt", startedAt.ToString("o"));
        return (long)command.ExecuteScalar()!;
    }

    public void Complete(long id, int countriesFound, int changesDetected, string? blogPostDate)
    {
        using var connection = connectionFactory.CreateConnection();
        using var command = new SqliteCommand(
            """
            UPDATE sync_log SET
                finished_at      = @finishedAt,
                status           = 'completed',
                countries_found  = @countriesFound,
                changes_detected = @changesDetected,
                blog_post_date   = @blogPostDate
            WHERE id = @id
            """,
            connection);
        command.Parameters.AddWithValue("@finishedAt", DateTime.UtcNow.ToString("o"));
        command.Parameters.AddWithValue("@countriesFound", countriesFound);
        command.Parameters.AddWithValue("@changesDetected", changesDetected);
        command.Parameters.AddWithValue("@blogPostDate", (object?)blogPostDate ?? DBNull.Value);
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
    }

    public void Fail(long id, string errorMessage)
    {
        using var connection = connectionFactory.CreateConnection();
        using var command = new SqliteCommand(
            "UPDATE sync_log SET finished_at = @finishedAt, status = 'failed', error_message = @errorMessage WHERE id = @id",
            connection);
        command.Parameters.AddWithValue("@finishedAt", DateTime.UtcNow.ToString("o"));
        command.Parameters.AddWithValue("@errorMessage", errorMessage);
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
    }
}