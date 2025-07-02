using ApiAggregator.Models.Weather;
using ApiAggregator.Services;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Text.Json;

/// <summary>
/// Service for fetching and caching weather data from OpenWeatherMap API.
/// </summary>
public class WeatherService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly StatsService _stats;
    private readonly string _apiKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherService"/> class.
    /// </summary>
    public WeatherService(IHttpClientFactory httpClientFactory, IMemoryCache cache, StatsService stats, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _stats = stats;
        _apiKey = config["ExternalApis:OpenWeather:ApiKey"];
    }

    /// <summary>
    /// Retrieves current weather data for the specified city.
    /// Uses caching to reduce API calls and records performance metrics.
    /// </summary>
    /// <param name="city">The city to fetch weather for.</param>
    /// <returns>A <see cref="WeatherInfo"/> object, or null if the API call fails.</returns>
    public async Task<WeatherInfo?> GetWeatherAsync(string city)
    {
        if (string.IsNullOrWhiteSpace(city)) return null;

        string cacheKey = $"weather:{city.ToLower()}";
        if (_cache.TryGetValue(cacheKey, out WeatherInfo cached)) return cached;

        var client = _httpClientFactory.CreateClient("OpenWeather");
        var sw = Stopwatch.StartNew();

        try
        {
            string url = $"weather?q={Uri.EscapeDataString(city)}&appid={_apiKey}&units=metric";
            var response = await client.GetAsync(url);
            sw.Stop();
            _stats.Record("OpenWeatherMap", sw.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode) return null;

            string json = await response.Content.ReadAsStringAsync();
            var dto = JsonSerializer.Deserialize<OpenWeatherResponse>(json);

            var result = new WeatherInfo
            {
                City = city,
                Description = dto?.Weather.FirstOrDefault()?.Description ?? "No description",
                Temperature = dto?.Main.Temp ?? 0,
                Humidity = dto?.Main.Humidity ?? 0,
                RetrievedAt = DateTime.UtcNow
            };

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            return result;
        }
        catch
        {
            sw.Stop();
            _stats.Record("OpenWeatherMap", sw.ElapsedMilliseconds);
            return null;
        }
    }
}
