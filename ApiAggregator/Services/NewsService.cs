using ApiAggregator.Models;
using ApiAggregator.Models.External;
using ApiAggregator.Services;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Text.Json;

public class NewsService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly StatsService _stats;
    private readonly string _apiKey;

    public NewsService(IHttpClientFactory httpClientFactory, IMemoryCache cache, StatsService stats, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _stats = stats;
        _apiKey = config["ExternalApis:NewsApi:ApiKey"];
    }

    public async Task<List<NewsArticle>> GetNewsAsync(string query, string category, string sortBy)
    {
        string cacheKey, url;
        if (!string.IsNullOrEmpty(query))
        {
            cacheKey = $"news:search:{query.ToLower()}:{sortBy}";
            if (_cache.TryGetValue(cacheKey, out List<NewsArticle> cached)) return cached;
            sortBy = string.IsNullOrEmpty(sortBy) ? "publishedAt" : sortBy;
            url = $"everything?q={Uri.EscapeDataString(query)}&sortBy={sortBy}&apiKey={_apiKey}";
        }
        else if (!string.IsNullOrEmpty(category))
        {
            cacheKey = $"news:headlines:{category.ToLower()}";
            if (_cache.TryGetValue(cacheKey, out List<NewsArticle> cached)) return cached;
            url = $"top-headlines?country=us&category={Uri.EscapeDataString(category)}&apiKey={_apiKey}";
        }
        else return new();

        var client = _httpClientFactory.CreateClient("NewsApi");
        var sw = Stopwatch.StartNew();

        try
        {
            var response = await client.GetAsync(url);
            sw.Stop();
            _stats.Record("NewsAPI", sw.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode) return new();

            string json = await response.Content.ReadAsStringAsync();
            var dto = JsonSerializer.Deserialize<NewsApiResponse>(json);

            var articles = dto?.Articles?.Select(a => new NewsArticle
            {
                Title = a.Title,
                Description = a.Description,
                Source = a.Source?.Name ?? "",
                Url = a.Url,
                PublishedAt = a.PublishedAt
            }).ToList() ?? new();

            _cache.Set(cacheKey, articles, TimeSpan.FromMinutes(10));
            return articles;
        }
        catch
        {
            sw.Stop();
            _stats.Record("NewsAPI", sw.ElapsedMilliseconds);
            return new();
        }
    }
}
