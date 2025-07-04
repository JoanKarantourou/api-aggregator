using ApiAggregator.Models.OpenAI;
using ApiAggregator.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ApiAggregator.Services
{
    /// <summary>
    /// Handles interaction with the OpenAI Completions API (gpt-3.5-turbo-instruct).
    /// Responsible for requesting completions, caching results, and recording performance statistics.
    /// </summary>
    public class OpenAIService : IOpenAIService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly StatsService _stats;
        private readonly ILogger<OpenAIService> _logger;
        private readonly ExternalApiOptions.OpenAiApi _options;

        public OpenAIService(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            StatsService stats,
            ILogger<OpenAIService> logger,
            IOptions<ExternalApiOptions> apiOptions)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _stats = stats;
            _logger = logger;
            _options = apiOptions.Value.OpenAI;
        }

        public async Task<OpenAICompletion?> GetCompletionAsync(string prompt, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return null;

            string cacheKey = $"openai:completion:{prompt.ToLower()}";
            if (_cache.TryGetValue(cacheKey, out var obj) && obj is OpenAICompletion cached)
            {
                _stats.Record("OpenAI", 0);          // count cache hit (0 ms)
                return cached;
            }

            var client = _httpClientFactory.CreateClient("OpenAI");

            // Fallback to default if config / options left the model blank
            var modelName = string.IsNullOrWhiteSpace(_options.Model)
                ? "gpt-3.5-turbo-instruct"
                : _options.Model;

            var requestData = new
            {
                model = modelName,
                prompt = prompt,
                max_tokens = _options.MaxTokens,
                temperature = _options.Temperature
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var sw = Stopwatch.StartNew();

            try
            {
                // BaseAddress is "https://api.openai.com/v1/" so appending "completions" yields the correct path
                var response = await client.PostAsync("completions", content, cancellationToken);
                sw.Stop();
                _stats.Record("OpenAI", sw.ElapsedMilliseconds);

                _logger.LogInformation("OpenAI call took {Elapsed} ms for prompt='{Prompt}'", sw.ElapsedMilliseconds, prompt);

                string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = TryParseOpenAiError(responseBody);
                    _logger.LogError("OpenAI API error: {StatusCode} – {Error}", response.StatusCode, errorDetails);
                    return null;
                }

                var result = JsonSerializer.Deserialize<OpenAIResponse>(responseBody);

                if (result?.Choices is not { Count: > 0 })
                {
                    _logger.LogWarning("OpenAI returned no choices for prompt='{Prompt}'", prompt);
                    return null;
                }

                var completion = new OpenAICompletion
                {
                    Prompt = prompt,
                    CompletionText = result.Choices.First().Text?.Trim() ?? string.Empty,
                    RetrievedAt = DateTime.UtcNow
                };

                _cache.Set(cacheKey, completion, TimeSpan.FromMinutes(30));
                return completion;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _stats.Record("OpenAI", sw.ElapsedMilliseconds);
                _logger.LogError(ex, "Exception occurred while calling OpenAI for prompt='{Prompt}'", prompt);
                return null;
            }
        }

        private static string TryParseOpenAiError(string responseBody)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                return doc.RootElement.GetProperty("error")
                                      .GetProperty("message")
                                      .GetString()
                       ?? "Unknown error";
            }
            catch
            {
                return "Could not parse OpenAI error message.";
            }
        }
    }
}
