namespace ApiAggregator.Models.News;

/// <summary>
/// DTO matching NewsAPI's JSON structure.
/// </summary>
public class NewsApiResponse
{
    public string Status { get; set; } = string.Empty;
    public int TotalResults { get; set; }
    public List<ArticleDto> Articles { get; set; } = new();

    public class ArticleDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public SourceInfo Source { get; set; } = new();
        public string Url { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
    }

    public class SourceInfo
    {
        public string Name { get; set; } = string.Empty;
    }
}
