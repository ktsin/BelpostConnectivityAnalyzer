using System.Text;
using BelpostConnectivityAnalyzer.Models;

namespace BelpostConnectivityAnalyzer.Notifications;

public static class StatusChangeReport
{
    public static string BuildSubject(List<CountryStatusChange> changes)
        => $"Belpost mail status changed: {changes.Count} countr{(changes.Count == 1 ? "y" : "ies")} affected";

    public static string BuildHtmlBody(List<CountryStatusChange> changes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<html><body>");
        sb.AppendLine($"<h2>International mail status update — {DateTime.UtcNow:dd MMM yyyy} UTC</h2>");
        sb.AppendLine($"<p>{changes.Count} change(s) detected:</p>");
        sb.AppendLine("<table border='1' cellpadding='4' cellspacing='0' style='border-collapse:collapse'>");
        sb.AppendLine("<tr><th>Country</th><th>EMS (prev → new)</th><th>Correspondence (prev → new)</th></tr>");

        foreach (var c in changes)
        {
            var label = c.IsNew ? $"{c.NameEnglish} <em>(new)</em>" : $"{c.NameEnglish}";
            var ems = FormatChange(c.PreviousEmsStatus, c.NewEmsStatus);
            var corr = FormatChange(c.PreviousCorrespondenceStatus, c.NewCorrespondenceStatus);
            sb.AppendLine($"<tr><td>{label}</td><td>{ems}</td><td>{corr}</td></tr>");
        }

        sb.AppendLine("</table>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }
    
    public static string BuildPlainTextBody(List<CountryStatusChange> changes)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"International mail status update — {DateTime.UtcNow:dd MMM yyyy} UTC");
        sb.AppendLine($"{changes.Count} change(s) detected:");

        var bodyBuilder = new StringBuilder();
        
        var labelMaxLength = 0;
        var emsMaxLength = 0;
        var corrMaxLength = 0;
        
        foreach (var c in changes)
        {
            var label = c.IsNew ? $"{c.NameEnglish} (new)" : c.NameEnglish;
            var ems = FormatChange(c.PreviousEmsStatus, c.NewEmsStatus, useHtml: false);
            var corr = FormatChange(c.PreviousCorrespondenceStatus, c.NewCorrespondenceStatus, useHtml: false);
            
            labelMaxLength = Math.Max(labelMaxLength, label.Length);
            emsMaxLength = Math.Max(emsMaxLength, ems.Length);
            corrMaxLength = Math.Max(corrMaxLength, corr.Length);
            
            bodyBuilder.AppendLine($"{label} | {ems} | {corr}");
        }
        
        AppendResizedHeader(sb, labelMaxLength, emsMaxLength, corrMaxLength);
        
        sb.AppendLine(bodyBuilder.ToString());

        return sb.ToString();
    }

    private static void AppendResizedHeader(
        StringBuilder sb,
        int labelMaxLength,
        int emsMaxLength,
        int corrMaxLength)
    {
        var labelHeader = "Country".PadRight(labelMaxLength);
        var emsHeader = "EMS (prev → new)".PadRight(emsMaxLength);
        var corrHeader = "Correspondence (prev → new)".PadRight(corrMaxLength);
        sb.AppendLine($"{labelHeader} | {emsHeader} | {corrHeader}");
        sb.AppendLine($"{new string('-', labelMaxLength)}-|-{new string('-', emsMaxLength)}-|-{new string('-', corrMaxLength)}");
    }

    private static string FormatChange(string? prev, string next, bool useHtml = true)
    {
        if (prev is null)
            return useHtml ? $"<strong>{next}</strong>" : next;
        if (string.Equals(prev, next, StringComparison.Ordinal))
            return next;
        return useHtml ? $"<s>{prev}</s> → <strong>{next}</strong>" : $"{prev} → {next}";
    }
}