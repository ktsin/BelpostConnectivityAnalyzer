namespace BelpostConnectivityAnalyzer.Models;

public sealed class CountryStatusChange
{
    public string NameCyrillic { get; set; } = string.Empty;
    public string NameEnglish { get; set; } = string.Empty;

    public string? PreviousEmsStatus { get; set; }
    public string NewEmsStatus { get; set; } = string.Empty;

    public string? PreviousCorrespondenceStatus { get; set; }
    public string NewCorrespondenceStatus { get; set; } = string.Empty;

    /// <summary>True when this country was not present in the DB before this sync.</summary>
    public bool IsNew { get; set; }
}