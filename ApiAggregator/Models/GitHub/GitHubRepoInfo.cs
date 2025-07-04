using System.Text.Json.Serialization;

namespace ApiAggregator.Models.GitHub;

/// <summary>
/// Sub-set of fields returned by GET /repos/{owner}/{repo}.
/// Enough for UI / API aggregation; extend anytime.
/// </summary>
public class GitHubRepoInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = default!;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = default!;

    [JsonPropertyName("stargazers_count")]
    public int Stars { get; set; }

    [JsonPropertyName("forks_count")]
    public int Forks { get; set; }

    [JsonPropertyName("open_issues_count")]
    public int OpenIssues { get; set; }

    [JsonPropertyName("default_branch")]
    public string DefaultBranch { get; set; } = default!;

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
