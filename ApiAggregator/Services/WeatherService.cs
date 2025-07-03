using ApiAggregator.Configuration;
using ApiAggregator.Models.Weather;
using ApiAggregator.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
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
    private readonly ILogger<WeatherService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _apiKey;
    private readonly int _cacheMinutes;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherService"/> class.
    /// </summary>
    public WeatherService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        StatsService stats,
        ILogger<WeatherService> logger,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration config,
        IOptions<ExternalApiOptions> apiOptions)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _stats = stats;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;

        _apiKey = config["ExternalApis:OpenWeather:ApiKey"]
            ?? throw new InvalidOperationException("OpenWeather:ApiKey is missing in configuration.");

        _cacheMinutes = apiOptions.Value.OpenWeather.CacheMinutes;
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
        if (_cache.TryGetValue(cacheKey, out var obj) && obj is WeatherInfo cached)
            return cached;

        var client = _httpClientFactory.CreateClient("OpenWeather");
        var sw = Stopwatch.StartNew();
        var cancellationToken = _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"weather?q={Uri.EscapeDataString(city)}&appid={_apiKey}&units=metric");

            var response = await client.SendAsync(request, cancellationToken);
            sw.Stop();
            _stats.Record("OpenWeatherMap", sw.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get weather for {City}. Status code: {StatusCode}", city, response.StatusCode);
                return null;
            }

            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            var dto = JsonSerializer.Deserialize<OpenWeatherResponse>(json);

            if (dto == null) return null;

            var result = new WeatherInfo
            {
                City = city,
                Description = dto.Weather.FirstOrDefault()?.Description ?? "No description",
                Temperature = dto.Main.Temp,
                Humidity = dto.Main.Humidity,
                RetrievedAt = DateTime.UtcNow
            };

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(_cacheMinutes));
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _stats.Record("OpenWeatherMap", sw.ElapsedMilliseconds);
            _logger.LogError(ex, "Exception occurred while fetching weather for {City}", city);
            return null;
        }
    }
}
