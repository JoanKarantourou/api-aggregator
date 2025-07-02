namespace ApiAggregator.Models.News;

/// <summary>
/// Represents a simplified news article from NewsAPI.
/// </summary>
public class NewsArticle
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
}
