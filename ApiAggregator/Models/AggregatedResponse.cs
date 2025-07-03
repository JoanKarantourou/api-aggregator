using ApiAggregator.Models.News;
using ApiAggregator.Models.OpenAI;
using ApiAggregator.Models.Weather;
using System.Text.Json.Serialization;

namespace ApiAggregator.Models;

/// <summary>
/// Represents the full aggregated response returned by the aggregator endpoint.
/// </summary>

public class AggregatedResponse
{
    [JsonPropertyName("weather")]
    public WeatherInfo? Weather { get; set; }

    [JsonPropertyName("news")]
    public List<NewsArticle> News { get; set; } = default!;

    [JsonPropertyName("openAi")]
    public OpenAICompletion? OpenAI { get; set; }

    [JsonPropertyName("weatherStatus")]
    public string WeatherStatus { get; set; } = default!;
    
    [JsonPropertyName("newsStatus")]
    public string NewsStatus { get; set; } = default!;
    
    [JsonPropertyName("openAiStatus")]
    public string OpenAIStatus { get; set; } = default!;

    // Traceability: when this response was generated (UTC)

    [JsonPropertyName("generatedAtUtc")]
    public DateTime GeneratedAtUtc { get; init; }
}
