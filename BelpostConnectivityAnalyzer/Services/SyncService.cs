using System.Text.RegularExpressions;
using BelpostConnectivityAnalyzer.Api;
using BelpostConnectivityAnalyzer.Data;
using BelpostConnectivityAnalyzer.Models;
using BelpostConnectivityAnalyzer.Notifications;
using BelpostConnectivityAnalyzer.Parsing;

namespace BelpostConnectivityAnalyzer.Services;

public sealed partial class SyncService(
    BelpostApiClient apiClient,
    PostalServiceAnnouncementHtmlParser htmlParser,
    CountryRepository countryRepository,
    SyncLogRepository syncLogRepository,
    NotificationLogRepository notificationLogRepository,
    INotificationSender notificationSender,
    ILogger<SyncService> logger)
{
    // Matches: "ВАЖНО | Прием международной почты, актуальный список стран с 01.01.2026"
    [GeneratedRegex(
        @"Прием международной почты,\s+актуальный список стран с \d{2}\.\d{2}\.\d{4}",
        RegexOptions.IgnoreCase,
        matchTimeoutMilliseconds: 500)]
    private static partial Regex ArticleTitleRegex();

    public async Task RunAsync(CancellationToken ct)
    {
        var startedAt = DateTime.UtcNow;
        var syncLogId = syncLogRepository.Insert(startedAt);
        logger.LogInformation("Sync started (log id={SyncLogId})", syncLogId);

        try
        {
            // 1. Find the relevant blog post
            var post = await apiClient.FindPostAsync(title => ArticleTitleRegex().IsMatch(title), ct);

            if (post is null)
            {
                logger.LogWarning("No matching blog post found");
                syncLogRepository.Complete(syncLogId, 0, 0, null);
                return;
            }

            // 2. Parse the HTML table
            var parsed = htmlParser.Parse(post.Content);
            if (parsed.Count == 0)
            {
                logger.LogWarning("Parser returned 0 countries — skipping upsert");
                syncLogRepository.Complete(syncLogId, 0, 0, post.PublishedAt);
                return;
            }

            // 3. Load existing data and diff
            var existing = await countryRepository.LoadAll(ct);
            var changes = DetectChanges(existing, parsed);

            logger.LogInformation(
                "Sync: {Found} countries found, {Changes} changes detected",
                parsed.Count,
                changes.Count);

            // 4. Upsert all countries
            await countryRepository.UpsertAll(parsed, ct);

            // 5. Send notifications if there are changes
            if (changes.Count > 0)
            {
                await notificationSender.SendStatusChangeAsync(changes, syncLogId, notificationLogRepository, ct);
            }

            syncLogRepository.Complete(syncLogId, parsed.Count, changes.Count, post.PublishedAt);
        }
        catch (OperationCanceledException)
        {
            syncLogRepository.Fail(syncLogId, "Cancelled");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Sync failed");
            syncLogRepository.Fail(syncLogId, ex.Message);
        }
    }

    private static List<CountryStatusChange> DetectChanges(
        Dictionary<string, CountryStatus> existing,
        List<CountryStatus> parsed)
    {
        var changes = new List<CountryStatusChange>();

        foreach (var country in parsed)
        {
            if (!existing.TryGetValue(country.NameEnglish, out var prev))
            {
                // New country — always report as a change
                changes.Add(new CountryStatusChange
                {
                    NameCyrillic = country.NameCyrillic,
                    NameEnglish = country.NameEnglish,
                    PreviousEmsStatus = null,
                    NewEmsStatus = country.EmsStatus,
                    PreviousCorrespondenceStatus = null,
                    NewCorrespondenceStatus = country.CorrespondenceStatus,
                    IsNew = true
                });
                continue;
            }

            var emsChanged = !string.Equals(prev.EmsStatus, country.EmsStatus, StringComparison.Ordinal);
            var corrChanged = !string.Equals(prev.CorrespondenceStatus, country.CorrespondenceStatus, StringComparison.Ordinal);

            if (emsChanged || corrChanged)
            {
                changes.Add(new CountryStatusChange
                {
                    NameCyrillic = country.NameCyrillic,
                    NameEnglish = country.NameEnglish,
                    PreviousEmsStatus = prev.EmsStatus,
                    NewEmsStatus = country.EmsStatus,
                    PreviousCorrespondenceStatus = prev.CorrespondenceStatus,
                    NewCorrespondenceStatus = country.CorrespondenceStatus,
                    IsNew = false
                });
            }
        }

        return changes;
    }
}