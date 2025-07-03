using System.Text.Json.Serialization;

namespace ApiAggregator.Models.News;

/// <summary>
/// DTO matching NewsAPI's JSON structure.
/// </summary>
public class NewsApiResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("totalResults")]
    public int TotalResults { get; set; }

    [JsonPropertyName("articles")]
    public List<ArticleDto> Articles { get; set; } = new();

    public class ArticleDto
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        [JsonPropertyName("source")]
        public SourceInfo Source { get; set; } = new();
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
        [JsonPropertyName("publishedAt")]
        public DateTime PublishedAt { get; set; }
    }

    public class SourceInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
