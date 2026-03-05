using System.Text.Json;
using BelpostConnectivityAnalyzer.Json;

namespace BelpostConnectivityAnalyzer.Api;

public sealed class BelpostApiClient(IHttpClientFactory httpClientFactory, ILogger<BelpostApiClient> logger)
{
    private const string ClientName = "Belpost";
    private const int MaxPages = 3;

    /// <summary>
    /// Searches up to <see cref="MaxPages"/> pages of blog posts for a post whose title matches
    /// the given predicate. Returns the matching post with full content (fetching by slug if needed),
    /// or null if not found.
    /// </summary>
    public async Task<BlogPostDetail?> FindPostAsync(Func<string, bool> titleFilter, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(ClientName);

        for (var page = 1; page <= MaxPages; page++)
        {
            logger.LogDebug("Fetching blog page {Page}", page);
            BlogListResponse? listResponse;

            try
            {
                var response = await client.GetAsync($"api/v1/blog?page={page}", ct);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                listResponse = await JsonSerializer.DeserializeAsync(stream, AppJsonContext.Default.BlogListResponse, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to fetch blog page {Page}", page);
                return null;
            }

            if (listResponse is null || listResponse.Data.Length == 0)
                break;

            foreach (var item in listResponse.Data)
            {
                if (!titleFilter(item.Title))
                    continue;

                logger.LogInformation("Found matching post: {Title} (slug: {Slug})", item.Title, item.PrettyUrl);

                // If content is already present, use it
                if (!string.IsNullOrWhiteSpace(item.Content))
                {
                    return new BlogPostDetail
                    {
                        Id = item.Id,
                        PrettyUrl = item.PrettyUrl,
                        Title = item.Title,
                        Content = item.Content,
                        PublishedAt = item.PublishedAt
                    };
                }

                // Otherwise fetch the full post by slug
                return await GetPostBySlugAsync(GetItemSlugFromFrontendLink(item.PrettyUrl), ct);
            }
        }

        return null;
    }

    private async Task<BlogPostDetail?> GetPostBySlugAsync(string slug, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(ClientName);
        logger.LogDebug("Fetching full post by slug: {Slug}", slug);

        try
        {
            var response = await client.GetAsync($"api/v1/blog/{slug}", ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var detailResponse = await JsonSerializer.DeserializeAsync(stream, AppJsonContext.Default.BlogPostDetailResponse, ct);

            return detailResponse?.Data;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to fetch post by slug: {Slug}", slug);
            return null;
        }
    }

    private static string GetItemSlugFromFrontendLink(string itemPrettyUrl)
    {
        var parts = itemPrettyUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[^1] : itemPrettyUrl;
    }
}