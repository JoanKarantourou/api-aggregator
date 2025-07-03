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
    /// Handles interaction with the OpenAI completions API.
    /// Responsible for requesting completions, caching results, and recording performance statistics.
    /// </summary>
    public class OpenAIService : IOpenAIService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly StatsService _stats;
        private readonly ExternalApiOptions.OpenAiApi _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAIService"/> class.
        /// </summary>
        public OpenAIService(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            StatsService stats,
            IOptions<ExternalApiOptions> apiOptions)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _stats = stats;
            _options = apiOptions.Value.OpenAI;
        }

        /// <summary>
        /// Requests a text completion from OpenAI for a given prompt.
        /// Returns cached result if available.
        /// </summary>
        /// <param name="prompt">The user prompt for which a completion is requested.</param>
        /// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
        /// <returns>An <see cref="OpenAICompletion"/> object with the result, or null if the request failed.</returns>
        public async Task<OpenAICompletion?> GetCompletionAsync(string prompt, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return null;

            string cacheKey = $"openai:completion:{prompt.ToLower()}";
            if (_cache.TryGetValue(cacheKey, out var obj) && obj is OpenAICompletion cached)
                return cached;

            var client = _httpClientFactory.CreateClient("OpenAI");

            var requestData = new
            {
                model = _options.Model,
                prompt = prompt,
                max_tokens = _options.MaxTokens,
                temperature = _options.Temperature
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var sw = Stopwatch.StartNew();
            try
            {
                var response = await client.PostAsync("completions", content, cancellationToken);
                sw.Stop();
                _stats.Record("OpenAI", sw.ElapsedMilliseconds);

                string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = TryParseOpenAiError(responseBody);
                    Console.WriteLine($"OpenAI API Error: {response.StatusCode} - {errorDetails}");
                    return null;
                }

                var result = JsonSerializer.Deserialize<OpenAIResponse>(responseBody);
                var completion = new OpenAICompletion
                {
                    Prompt = prompt,
                    CompletionText = result?.Choices?.FirstOrDefault()?.Text?.Trim() ?? string.Empty,
                    RetrievedAt = DateTime.UtcNow
                };

                _cache.Set(cacheKey, completion, TimeSpan.FromMinutes(30));
                return completion;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _stats.Record("OpenAI", sw.ElapsedMilliseconds);
                Console.WriteLine($"OpenAIService exception: {ex.Message}");
                return null;
            }
        }

        private string TryParseOpenAiError(string responseBody)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                return doc.RootElement.GetProperty("error").GetProperty("message").GetString() ?? "Unknown error";
            }
            catch
            {
                return "Could not parse OpenAI error message.";
            }
        }
    }
}
