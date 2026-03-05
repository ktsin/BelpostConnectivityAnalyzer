namespace BelpostConnectivityAnalyzer.Configuration;

public sealed class ReportConfigEntry
{
    public string Type { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string[] Recipients { get; set; } = [];
}