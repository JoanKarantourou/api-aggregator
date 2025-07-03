using ApiAggregator.Models.Weather;
using System.Threading.Tasks;

namespace ApiAggregator.Services.Interfaces
{
    /// <summary>
    /// Service for fetching current weather information.
    /// </summary>
    public interface IWeatherService
    {
        Task<WeatherInfo?> GetWeatherAsync(string city, CancellationToken cancellationToken);
    }
}
