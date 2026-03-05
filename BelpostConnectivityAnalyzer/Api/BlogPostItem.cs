namespace BelpostConnectivityAnalyzer.Api;

public sealed class BlogPostItem
{
    public int Id { get; set; }
    public string PrettyUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string PublishedAt { get; set; } = string.Empty;
}
