namespace BelpostConnectivityAnalyzer.Configuration;

public sealed class SyncSettings
{
    /// <summary>Time of day to run sync in UTC, format "HH:mm".</summary>
    public string SyncTimeUtc { get; set; } = "08:00";

    public string DatabasePath { get; set; } = "/app/data/belpost.db";
}