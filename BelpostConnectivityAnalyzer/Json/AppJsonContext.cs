using System.Text.Json;
using System.Text.Json.Serialization;
using BelpostConnectivityAnalyzer.Api;
using BelpostConnectivityAnalyzer.Configuration;

namespace BelpostConnectivityAnalyzer.Json;

[JsonSerializable(typeof(BlogListResponse))]
[JsonSerializable(typeof(BlogPostItem))]
[JsonSerializable(typeof(BlogPostItem[]))]
[JsonSerializable(typeof(BlogPostDetail))]
[JsonSerializable(typeof(BlogPostDetailResponse))]
[JsonSerializable(typeof(ReportConfigEntry))]
[JsonSerializable(typeof(ReportConfigEntry[]))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true)]
public partial class AppJsonContext : JsonSerializerContext;
