using ApiAggregator.Models.GitHub;
using System.Threading;
using System.Threading.Tasks;

namespace ApiAggregator.Services.Interfaces;

/// <summary>
/// Contract for GitHub API integration.
/// </summary>
public interface IGitHubService
{
    Task<GitHubRepoInfo?> GetRepositoryAsync(
        string owner,
        string repo,
        CancellationToken cancellationToken = default);
}
