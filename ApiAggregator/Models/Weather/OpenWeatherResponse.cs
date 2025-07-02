namespace ApiAggregator.Models.Weather;

/// <summary>
/// DTO matching the OpenWeatherMap API JSON structure.
/// </summary>
public class OpenWeatherResponse
{
    public MainData Main { get; set; } = new();
    public List<WeatherDescription> Weather { get; set; } = new();

    public class MainData
    {
        public double Temp { get; set; }
        public double Humidity { get; set; }
    }

    public class WeatherDescription
    {
        public string Description { get; set; } = string.Empty;
    }
}
