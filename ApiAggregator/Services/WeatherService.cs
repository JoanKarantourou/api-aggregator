using ApiAggregator.Models;
using ApiAggregator.Models.External;
using ApiAggregator.Services;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Text.Json;

public class WeatherService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly StatsService _stats;
    private readonly string _apiKey;

    public WeatherService(IHttpClientFactory httpClientFactory, IMemoryCache cache, StatsService stats, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _stats = stats;
        _apiKey = config["ExternalApis:OpenWeather:ApiKey"];
    }

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
