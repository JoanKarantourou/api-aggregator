namespace ApiAggregator.Models.Weather;

/// <summary>
/// Represents simplified weather information extracted from OpenWeatherMap.
/// </summary>
public class WeatherInfo
{
    public string City { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public DateTime RetrievedAt { get; set; }
}
