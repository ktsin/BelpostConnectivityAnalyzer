using System.Globalization;
using BelpostConnectivityAnalyzer.Models;
using Microsoft.Data.Sqlite;

namespace BelpostConnectivityAnalyzer.Data;

public sealed class CountryRepository(DbConnectionFactory connectionFactory)
{
    /// <summary>Returns all countries keyed by name_english.</summary>
    public async Task<Dictionary<string, CountryStatus>> LoadAll(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await using var command = new SqliteCommand(
            """
            SELECT 
                id, 
                name_cyrillic, 
                name_english, 
                ems_status, 
                correspondence_status, 
                status_changed_at, 
                last_seen_at 
            FROM country_status
            """,
            connection);

        await using var reader = await command.ExecuteReaderAsync();
        var result = new Dictionary<string, CountryStatus>(StringComparer.OrdinalIgnoreCase);

        while (await reader.ReadAsync(cancellationToken))
        {
            var country = Map(reader);
            result[country.NameEnglish] = country;
        }

        return result;
    }

    /// <summary>Upserts a list of countries. Updates status_changed_at only when a status actually changes.</summary>
    public async Task UpsertAll(IEnumerable<CountryStatus> countries, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await using var transaction = connection.BeginTransaction();

        const string sql = """
            INSERT INTO country_status (name_cyrillic, name_english, ems_status, correspondence_status, status_changed_at, last_seen_at)
            VALUES (@nameCyrillic, @nameEnglish, @emsStatus, @correspondenceStatus, @now, @now)
            ON CONFLICT(name_english) DO UPDATE SET
                name_cyrillic = excluded.name_cyrillic,
                status_changed_at = CASE
                    WHEN ems_status != excluded.ems_status OR correspondence_status != excluded.correspondence_status
                    THEN excluded.status_changed_at
                    ELSE status_changed_at
                END,
                ems_status = excluded.ems_status,
                correspondence_status = excluded.correspondence_status,
                last_seen_at = excluded.last_seen_at
            """;

        await using var command = new SqliteCommand(sql, connection, transaction);
        var pNameCyrillic = command.Parameters.Add("@nameCyrillic", SqliteType.Text);
        var pNameEnglish = command.Parameters.Add("@nameEnglish", SqliteType.Text);
        var pEmsStatus = command.Parameters.Add("@emsStatus", SqliteType.Text);
        var pCorrespondenceStatus = command.Parameters.Add("@correspondenceStatus", SqliteType.Text);
        var pNow = command.Parameters.Add("@now", SqliteType.Text);

        var now = DateTime.UtcNow.ToString("O");

        foreach (var country in countries)
        {
            pNameCyrillic.Value = country.NameCyrillic;
            pNameEnglish.Value = country.NameEnglish;
            pEmsStatus.Value = country.EmsStatus;
            pCorrespondenceStatus.Value = country.CorrespondenceStatus;
            pNow.Value = now;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static CountryStatus Map(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        NameCyrillic = reader.GetString(1),
        NameEnglish = reader.GetString(2),
        EmsStatus = reader.GetString(3),
        CorrespondenceStatus = reader.GetString(4),
        StatusChangedAt = DateTime.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
        LastSeenAt = DateTime.Parse(reader.GetString(6), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal)
    };
}