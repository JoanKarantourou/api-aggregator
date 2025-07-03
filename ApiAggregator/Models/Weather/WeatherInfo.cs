using System.Text.Json.Serialization;

namespace ApiAggregator.Models.Weather;

/// <summary>
/// Represents simplified weather information extracted from OpenWeatherMap.
/// </summary>
public class WeatherInfo
{
    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("description")] 
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }

    [JsonPropertyName("humidity")]
    public double Humidity { get; set; }

    [JsonPropertyName("retrievedAt")]
    public DateTime RetrievedAt { get; set; }
}