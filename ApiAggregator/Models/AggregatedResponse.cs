using ApiAggregator.Models.News;
using ApiAggregator.Models.OpenAI;
using ApiAggregator.Models.GitHub;
using System.Text.Json.Serialization;

namespace ApiAggregator.Models
{
    /// <summary>
    /// Represents the full aggregated response returned by the aggregator endpoint.
    /// </summary>
    public class AggregatedResponse
    {
        [JsonPropertyName("gitHub")]
        public GitHubRepoInfo? GitHub { get; set; }

        [JsonPropertyName("news")]
        public List<NewsArticle> News { get; set; } = new();

        [JsonPropertyName("openAi")]
        public OpenAICompletion? OpenAI { get; set; }

        [JsonPropertyName("gitHubStatus")]
        public string GitHubStatus { get; set; } = string.Empty;

        [JsonPropertyName("newsStatus")]
        public string NewsStatus { get; set; } = string.Empty;

        [JsonPropertyName("openAiStatus")]
        public string OpenAIStatus { get; set; } = string.Empty;

        [JsonPropertyName("generatedAtUtc")]
        public DateTime GeneratedAtUtc { get; init; }
    }
}
