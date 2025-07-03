using System.Text.Json.Serialization;

namespace ApiAggregator.Models.News;

/// <summary>
/// Represents a simplified news article from NewsAPI.
/// </summary>
public class NewsArticle
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("publishedAt")]
    public DateTime PublishedAt { get; set; }
}
