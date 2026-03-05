using Microsoft.Data.Sqlite;

namespace BelpostConnectivityAnalyzer.Data;

public sealed class NotificationLogRepository(DbConnectionFactory connectionFactory)
{
    public void Insert(long syncLogId, string reportType, string recipients, string status, string? errorMessage)
    {
        using var connection = connectionFactory.CreateConnection();
        using var command = new SqliteCommand(
            """
            INSERT INTO notification_log (sync_log_id, report_type, sent_at, recipients, status, error_message)
            VALUES (@syncLogId, @reportType, @sentAt, @recipients, @status, @errorMessage)
            """,
            connection);
        command.Parameters.AddWithValue("@syncLogId", syncLogId);
        command.Parameters.AddWithValue("@reportType", reportType);
        command.Parameters.AddWithValue("@sentAt", DateTime.UtcNow.ToString("o"));
        command.Parameters.AddWithValue("@recipients", recipients);
        command.Parameters.AddWithValue("@status", status);
        command.Parameters.AddWithValue("@errorMessage", (object?)errorMessage ?? DBNull.Value);
        command.ExecuteNonQuery();
    }
}
