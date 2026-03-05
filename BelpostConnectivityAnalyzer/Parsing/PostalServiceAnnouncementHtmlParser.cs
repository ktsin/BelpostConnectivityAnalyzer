using AngleSharp;
using AngleSharp.Dom;
using BelpostConnectivityAnalyzer.Models;

namespace BelpostConnectivityAnalyzer.Parsing;

public sealed class PostalServiceAnnouncementHtmlParser(ILogger<PostalServiceAnnouncementHtmlParser> logger)
{
    /// <summary>
    /// Parses HTML content of a blog post and extracts postal service statuses from the first table found.
    /// Expected columns: 0="Country / Страна" (split on '/'), 1=EMS status, 2=Correspondence status.
    /// Header is skipped, including the column names.
    /// </summary>
    public List<CountryStatus> Parse(string html)
    {
        var context = BrowsingContext.New(AngleSharp.Configuration.Default);
        var document = context.OpenAsync(req => req.Content(html)).GetAwaiter().GetResult();

        var table = document.QuerySelector("table");
        if (table is null)
        {
            logger.LogWarning("No table found in blog post HTML");
            return [];
        }

        var rows = table.QuerySelectorAll("tr");
        var result = new List<CountryStatus>(rows.Length);

        foreach (var row in rows)
        {
            // Skip header rows
            if (row.QuerySelector("th") is not null)
                continue;

            var cells = row.QuerySelectorAll("td");
            if (cells.Length < 3)
            {
                logger.LogDebug("Skipping row with {CellCount} cells (expected 3+)", cells.Length);
                continue;
            }
            
            if (cells[0].TextContent.Trim().StartsWith("Страна", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogDebug("Skipping header-like row: {FirstCell}", cells[0].TextContent.Trim());
                continue;
            }

            var (emsStatus, correspondenceStatus, nameCyrillic, nameEnglish) = ParseLine(cells);

            if (string.IsNullOrEmpty(nameEnglish))
            {
                logger.LogDebug("Skipping row with empty name");
                continue;
            }

            result.Add(new CountryStatus
            {
                NameCyrillic = nameCyrillic,
                NameEnglish = nameEnglish,
                EmsStatus = emsStatus,
                CorrespondenceStatus = correspondenceStatus
            });
        }

        logger.LogInformation("Parsed {Count} countries from HTML table", result.Count);
        return result;
    }

    private static (string emsStatus, string correspondenceStatus, string nameCyrillic, string nameEnglish) ParseLine(
        IHtmlCollection<IElement> cells)
    {
        var countryCell = cells[0].TextContent.Trim();
        var emsStatus = cells[1].TextContent.Trim();
        var correspondenceStatus = cells[2].TextContent.Trim();

        // Split "Австрия / Austria" → ["Австрия", "Austria"]
        var slashIndex = countryCell.IndexOf('/');
        string nameCyrillic, nameEnglish;
        if (slashIndex >= 0)
        {
            nameCyrillic = countryCell[..slashIndex].Trim();
            nameEnglish = countryCell[(slashIndex + 1)..].Trim();
        }
        else
        {
            // Fallback: no slash, store the same value in both
            nameCyrillic = countryCell;
            nameEnglish = countryCell;
        }

        return (emsStatus, correspondenceStatus, nameCyrillic, nameEnglish);
    }
}