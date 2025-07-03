using ApiAggregator.Models.News;
using ApiAggregator.Services;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Text.Json;

/// <summary>
/// Service for fetching and caching news articles from NewsAPI.org.
/// </summary>
public class NewsService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly StatsService _stats;
    private readonly string _apiKey;

    // Allowed sortBy values to avoid API 400 errors
    private static readonly HashSet<string> ValidSortByOptions = new(StringComparer.OrdinalIgnoreCase)
    {
        "relevancy", "popularity", "publishedAt"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="NewsService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating named HttpClient instances.</param>
    /// <param name="cache">In-memory cache for storing news results.</param>
    /// <param name="stats">Service for recording performance metrics.</param>
    /// <param name="config">Application configuration to retrieve API key.</param>
    public NewsService(IHttpClientFactory httpClientFactory, IMemoryCache cache, StatsService stats, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _stats = stats;

        _apiKey = config["ExternalApis:NewsApi:ApiKey"]
            ?? throw new InvalidOperationException("NewsApi:ApiKey is missing from configuration.");
    }

    /// <summary>
    /// Retrieves news articles based on search query or category.
    /// Caches results and tracks request statistics.
    /// </summary>
    /// <param name="query">Search keyword (optional).</param>
    /// <param name="category">News category (used if query is not specified).</param>
    /// <param name="sortBy">Sorting option (relevancy, popularity, or publishedAt).</param>
    /// <returns>A list of <see cref="NewsArticle"/> objects, or an empty list if failed or no data found.</returns>
    public async Task<List<NewsArticle>> GetNewsAsync(string query, string category, string sortBy)
    {
        string cacheKey, url;

        if (!string.IsNullOrEmpty(query))
        {
            if (string.IsNullOrEmpty(sortBy) || !ValidSortByOptions.Contains(sortBy))
                sortBy = "publishedAt";

            cacheKey = $"news:search:{query.ToLower()}:{sortBy}";

            if (_cache.TryGetValue(cacheKey, out var obj) && obj is List<NewsArticle> cachedSearch)
                return cachedSearch;

            url = $"everything?q={Uri.EscapeDataString(query)}&sortBy={sortBy}&apiKey={_apiKey}";
        }
        else if (!string.IsNullOrEmpty(category))
        {
            cacheKey = $"news:headlines:{category.ToLower()}";

            if (_cache.TryGetValue(cacheKey, out var obj2) && obj2 is List<NewsArticle> cachedCategory)
                return cachedCategory;

            url = $"top-headlines?country=us&category={Uri.EscapeDataString(category)}&apiKey={_apiKey}";
        }
        else
        {
            return new List<NewsArticle>();
        }

        var client = _httpClientFactory.CreateClient("NewsApi");
        var sw = Stopwatch.StartNew();

        try
        {
            var response = await client.GetAsync(url);
            sw.Stop();
            _stats.Record("NewsAPI", sw.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
                return new List<NewsArticle>();

            string json = await response.Content.ReadAsStringAsync();
            var dto = JsonSerializer.Deserialize<NewsApiResponse>(json);

            var articles = dto?.Articles?.Select(a => new NewsArticle
            {
                Title = a.Title ?? "",
                Description = a.Description ?? "",
                Source = a.Source?.Name ?? "",
                Url = a.Url ?? "",
                PublishedAt = a.PublishedAt
            }).ToList() ?? new List<NewsArticle>();

            _cache.Set(cacheKey, articles, TimeSpan.FromMinutes(10));
            return articles;
        }
        catch
        {
            sw.Stop();
            _stats.Record("NewsAPI", sw.ElapsedMilliseconds);
            return new List<NewsArticle>();
        }
    }
}
