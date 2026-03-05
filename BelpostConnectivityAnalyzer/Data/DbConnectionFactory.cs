using Microsoft.Data.Sqlite;

namespace BelpostConnectivityAnalyzer.Data;

public sealed class DbConnectionFactory(string databasePath)
{
    public SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();
        return connection;
    }
}
