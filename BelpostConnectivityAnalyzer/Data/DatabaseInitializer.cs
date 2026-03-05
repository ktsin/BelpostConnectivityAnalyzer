using Microsoft.Data.Sqlite;

namespace BelpostConnectivityAnalyzer.Data;

public sealed class DatabaseInitializer(DbConnectionFactory connectionFactory)
{
    private const string Schema = """
        CREATE TABLE IF NOT EXISTS country_status (
            id                    INTEGER PRIMARY KEY AUTOINCREMENT,
            name_cyrillic         TEXT NOT NULL,
            name_english          TEXT NOT NULL,
            ems_status            TEXT NOT NULL,
            correspondence_status TEXT NOT NULL,
            status_changed_at     TEXT NOT NULL,
            last_seen_at          TEXT NOT NULL,
            UNIQUE(name_english)
        );

        CREATE TABLE IF NOT EXISTS sync_log (
            id               INTEGER PRIMARY KEY AUTOINCREMENT,
            started_at       TEXT NOT NULL,
            finished_at      TEXT,
            status           TEXT NOT NULL DEFAULT 'running',
            countries_found  INTEGER,
            changes_detected INTEGER,
            error_message    TEXT,
            blog_post_date   TEXT
        );

        CREATE TABLE IF NOT EXISTS notification_log (
            id            INTEGER PRIMARY KEY AUTOINCREMENT,
            sync_log_id   INTEGER NOT NULL REFERENCES sync_log(id),
            report_type   TEXT NOT NULL,
            sent_at       TEXT NOT NULL,
            recipients    TEXT NOT NULL,
            status        TEXT NOT NULL,
            error_message TEXT
        );
        """;

    public void Initialize()
    {
        using var connection = connectionFactory.CreateConnection();
        using var command = new SqliteCommand(Schema, connection);
        command.ExecuteNonQuery();
    }
}
