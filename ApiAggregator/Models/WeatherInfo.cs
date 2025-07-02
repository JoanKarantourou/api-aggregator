namespace ApiAggregator.Models
{
    /// <summary>
    /// Represents weather data for a specific location.
    /// </summary>
    public class WeatherInfo
    {
        /// <summary>
        /// The name of the location (city or region).
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Temperature in Celsius.
        /// </summary>
        public double TemperatureCelsius { get; set; }

        /// <summary>
        /// Weather condition description (e.g., "Clear", "Rainy").
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Humidity percentage (0–100).
        /// </summary>
        public double Humidity { get; set; }
    }
}
