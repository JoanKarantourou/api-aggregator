using ApiAggregator.Models.News;
using ApiAggregator.Models.OpenAI;
using ApiAggregator.Models.Weather;
using ApiAggregator.Services;

namespace ApiAggregator.Models;

/// <summary>
/// Represents the full aggregated response returned by the aggregator endpoint.
/// </summary>

public class AggregatedResponse
{
    public WeatherInfo? Weather { get; set; }
    public List<NewsArticle> News { get; set; }
    public OpenAICompletion? OpenAI { get; set; }

    public string WeatherStatus { get; set; }
    public string NewsStatus { get; set; }
    public string OpenAIStatus { get; set; }
}
