using ApiAggregator.Models.OpenAI;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ApiAggregator.Services
{
    /// <summary>
    /// Handles interaction with the OpenAI completions API.
    /// Responsible for requesting completions, caching results, and recording performance statistics.
    /// </summary>
    public class OpenAIService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly StatsService _stats;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAIService"/> class.
        /// </summary>
        /// <param name="httpClientFactory">Factory to create named HttpClients.</param>
        /// <param name="cache">In-memory cache instance.</param>
        /// <param name="stats">Stats tracker service for monitoring API usage.</param>
        public OpenAIService(IHttpClientFactory httpClientFactory, IMemoryCache cache, StatsService stats)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _stats = stats;
        }

        /// <summary>
        /// Requests a text completion from OpenAI for a given prompt.
        /// Returns cached result if available.
        /// </summary>
        /// <param name="prompt">The user prompt for which a completion is requested.</param>
        /// <returns>An <see cref="OpenAICompletion"/> object with the result, or null if the request failed.</returns>
        public async Task<OpenAICompletion?> GetCompletionAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return null;

            string cacheKey = $"openai:completion:{prompt.ToLower()}";

            if (_cache.TryGetValue(cacheKey, out OpenAICompletion cached)) return cached;

            var client = _httpClientFactory.CreateClient("OpenAI");

            var requestData = new
            {
                model = "text-davinci-003",
                prompt = prompt,
                max_tokens = 100,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var sw = Stopwatch.StartNew();
            try
            {
                var response = await client.PostAsync("completions", content);
                sw.Stop();
                _stats.Record("OpenAI", sw.ElapsedMilliseconds);

                if (!response.IsSuccessStatusCode) return null;

                var responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OpenAIResponse>(responseBody);

                var completion = new OpenAICompletion
                {
                    Prompt = prompt,
                    CompletionText = result?.Choices?.FirstOrDefault()?.Text?.Trim() ?? "",
                    RetrievedAt = DateTime.UtcNow
                };

                _cache.Set(cacheKey, completion, TimeSpan.FromMinutes(30));
                return completion;
            }
            catch
            {
                sw.Stop();
                _stats.Record("OpenAI", sw.ElapsedMilliseconds);
                return null;
            }
        }
    }
}
