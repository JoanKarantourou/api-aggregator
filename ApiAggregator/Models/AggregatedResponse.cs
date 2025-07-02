namespace ApiAggregator.Models;

/// <summary>
/// Represents the full aggregated response returned by the aggregator endpoint.
/// </summary>
public class AggregatedResponse
{
    public Weather.WeatherInfo? Weather { get; set; }
    public List<News.NewsArticle> News { get; set; } = new();
    public string? OpenAIResult { get; set; }

    public string WeatherStatus { get; set; } = string.Empty;
    public string NewsStatus { get; set; } = string.Empty;
    public string OpenAIStatus { get; set; } = string.Empty;
}
