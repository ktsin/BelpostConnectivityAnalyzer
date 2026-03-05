namespace BelpostConnectivityAnalyzer.Models;

public sealed class CountryStatus
{
    public long Id { get; set; }
    public string NameCyrillic { get; set; } = string.Empty;
    public string NameEnglish { get; set; } = string.Empty;
    public string EmsStatus { get; set; } = string.Empty;
    public string CorrespondenceStatus { get; set; } = string.Empty;
    public DateTime StatusChangedAt { get; set; }
    public DateTime LastSeenAt { get; set; }
}