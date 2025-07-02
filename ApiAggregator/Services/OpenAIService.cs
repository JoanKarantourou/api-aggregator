using ApiAggregator.Models;
using ApiAggregator.Models.External;
using ApiAggregator.Services;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class OpenAIService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly StatsService _stats;
    private readonly string _apiKey;

    public OpenAIService(IHttpClientFactory httpClientFactory, IMemoryCache cache, StatsService stats, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _stats = stats;
        _apiKey = config["ExternalApis:OpenAI:ApiKey"];
    }

    public async Task<OpenAICompletion?> GetCompletionAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt)) return null;

        string cacheKey = $"openai:completion:{prompt.ToLower()}";
        if (_cache.TryGetValue(cacheKey, out OpenAICompletion cached)) return cached;

        var client = _httpClientFactory.CreateClient("OpenAI");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var request = new
        {
            model = "text-davinci-003",
            prompt = prompt,
            max_tokens = 100,
            temperature = 0.7
        };

        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await client.PostAsync("completions", content);
            sw.Stop();
            _stats.Record("OpenAI", sw.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode) return null;

            string json = await response.Content.ReadAsStringAsync();
            var dto = JsonSerializer.Deserialize<OpenAIResponse>(json);

            var result = new OpenAICompletion
            {
                Prompt = prompt,
                Response = dto?.Choices?.FirstOrDefault()?.Text?.Trim() ?? "",
                CreatedAt = DateTime.UtcNow
            };

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(30));
            return result;
        }
        catch
        {
            sw.Stop();
            _stats.Record("OpenAI", sw.ElapsedMilliseconds);
            return null;
        }
    }
}
