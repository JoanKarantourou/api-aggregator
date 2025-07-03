using System.Text.Json.Serialization;

namespace ApiAggregator.Models.Weather;

/// <summary>
/// DTO matching the OpenWeatherMap API JSON structure.
/// </summary>
public class OpenWeatherResponse
{
    [JsonPropertyName("main")]
    public MainData Main { get; set; } = new();

    [JsonPropertyName("weather")]
    public List<WeatherDescription> Weather { get; set; } = new();

    public class MainData
    {
        [JsonPropertyName("temp")]
        public double Temp { get; set; }

        [JsonPropertyName("humidity")]
        public double Humidity { get; set; }
    }

    public class WeatherDescription
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }
}
