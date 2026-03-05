namespace BelpostConnectivityAnalyzer.Api;

public sealed class BlogListResponse
{
    public BlogPostItem[] Data { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; }
    public int PerPage { get; set; }
}
