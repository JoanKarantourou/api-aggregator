using System.ComponentModel.DataAnnotations;

public record ExternalApiOptions
{
    // Active external APIs
    public Api        NewsApi  { get; init; } = default!;
    public OpenAiApi  OpenAI   { get; init; } = default!;
    public GitHubApi  GitHub   { get; init; } = default!;

    // Common base
    public record Api
    {
        [Required] public string BaseUrl { get; init; } = "";
        [Required] public string ApiKey  { get; init; } = "";   // or AuthToken
    }

    // OpenAI
    public record OpenAiApi : Api
    {
        [Required] public string Model { get; init; } = "gpt-3.5-turbo-instruct";
        [Range(0.0, 2.0)] public double Temperature { get; init; } = 0.7;
        [Range(1, 4096)]  public int    MaxTokens    { get; init; } = 1024;
    }

    // GitHub
    public record GitHubApi : Api
    {
        // Optional UA header if GitHub API rate-limits anonymous calls.
        public string UserAgent { get; init; } = "ApiAggregator";
    }
}
