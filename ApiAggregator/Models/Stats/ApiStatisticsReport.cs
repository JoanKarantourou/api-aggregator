using System.Text.Json.Serialization;

namespace ApiAggregator.Models.Stats;
public class ApiStatisticsReport
{
    [JsonPropertyName("totalRequests")]
    public long TotalRequests { get; set; }

    [JsonPropertyName("averageResponseTimeMs")]
    public double AverageResponseTimeMs { get; set; }

    [JsonPropertyName("fastCount")]
    public long FastCount { get; set; }

    [JsonPropertyName("mediumCount")]
    public long MediumCount { get; set; }

    [JsonPropertyName("slowCount")]
    public long SlowCount { get; set; }
}
