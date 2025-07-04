using ApiAggregator.Models.News;
using ApiAggregator.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Text.Json;

namespace ApiAggregator.Services
{
    /// <summary>
    /// Service for fetching and caching news articles from NewsAPI.org.
    /// </summary>
    public class NewsService : INewsService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly StatsService _stats;
        private readonly ILogger<NewsService> _logger;
        private readonly string _apiKey;

        private static readonly HashSet<string> ValidSortByOptions = new(StringComparer.OrdinalIgnoreCase)
        {
            "relevancy", "popularity", "publishedAt"
        };

        public NewsService(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            StatsService stats,
            ILogger<NewsService> logger,
            IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _stats = stats;
            _logger = logger;

            _apiKey = config["ExternalApis:NewsApi:ApiKey"]
                ?? throw new InvalidOperationException("NewsApi:ApiKey is missing from configuration.");
        }

        public async Task<List<NewsArticle>> GetNewsAsync(
            string query,
            string category,
            string sortBy,
            CancellationToken cancellationToken)
        {
            string cacheKey;
            string url;

            if (!string.IsNullOrEmpty(query))
            {
                if (string.IsNullOrEmpty(sortBy) || !ValidSortByOptions.Contains(sortBy))
                    sortBy = "publishedAt";

                cacheKey = $"news:search:{query.ToLower()}:{sortBy}";
                if (_cache.TryGetValue(cacheKey, out var cachedObj) && cachedObj is List<NewsArticle> cachedSearch)
                    return cachedSearch;

                url = $"everything?q={Uri.EscapeDataString(query)}&sortBy={sortBy}&apiKey={_apiKey}";
            }
            else if (!string.IsNullOrEmpty(category))
            {
                cacheKey = $"news:headlines:{category.ToLower()}";
                if (_cache.TryGetValue(cacheKey, out var cachedObj2) && cachedObj2 is List<NewsArticle> cachedCategory)
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
                var response = await client.GetAsync(url, cancellationToken);
                sw.Stop();

                _stats.Record("NewsAPI", sw.ElapsedMilliseconds);
                _logger.LogInformation("NewsAPI call took {Elapsed}ms for query='{Query}', category='{Category}'", sw.ElapsedMilliseconds, query, category);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("NewsAPI call failed with status {StatusCode}", response.StatusCode);
                    return new List<NewsArticle>();
                }

                string json = await response.Content.ReadAsStringAsync(cancellationToken);
                var dto = JsonSerializer.Deserialize<NewsApiResponse>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var articles = dto?.Articles?.Select(a => new NewsArticle
                {
                    Title = a.Title ?? string.Empty,
                    Description = a.Description ?? string.Empty,
                    Source = a.Source?.Name ?? string.Empty,
                    Url = a.Url ?? string.Empty,
                    PublishedAt = a.PublishedAt
                }).ToList() ?? new List<NewsArticle>();

                if (articles.Count > 0)
                    _cache.Set(cacheKey, articles, TimeSpan.FromMinutes(10));
                return articles;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _stats.Record("NewsAPI", sw.ElapsedMilliseconds);
                _logger.LogError(ex, "Exception occurred while fetching news for query='{Query}', category='{Category}'", query, category);
                return new List<NewsArticle>();
            }
        }
    }
}
