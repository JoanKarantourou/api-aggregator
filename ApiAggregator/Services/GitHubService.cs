using ApiAggregator.Models.GitHub;
using ApiAggregator.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ApiAggregator.Services;

/// <summary>
/// Lightweight wrapper around GitHub’s REST API.
/// Records timing stats via <see cref="StatsService"/>.
/// </summary>
public class GitHubService : IGitHubService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IMemoryCache _cache;
    private readonly StatsService _stats;
    private readonly ILogger<GitHubService> _log;
    private readonly ExternalApiOptions.GitHubApi _opt;

    public GitHubService(
        IHttpClientFactory httpFactory,
        IMemoryCache cache,
        StatsService stats,
        ILogger<GitHubService> log,
        IOptions<ExternalApiOptions> opts)
    {
        _httpFactory = httpFactory;
        _cache = cache;
        _stats = stats;
        _log = log;
        _opt = opts.Value.GitHub;
    }

    public async Task<GitHubRepoInfo?> GetRepositoryAsync(
        string owner,
        string repo,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
            return null;

        var cacheKey = $"github:repo:{owner}/{repo}".ToLowerInvariant();
        if (_cache.TryGetValue(cacheKey, out GitHubRepoInfo? cached) && cached is not null)
            return cached;

        var client = _httpFactory.CreateClient("GitHub");
        client.BaseAddress ??= new Uri(_opt.BaseUrl);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(_opt.UserAgent);
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        if (!string.IsNullOrEmpty(_opt.ApiKey))
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _opt.ApiKey);

        var sw = Stopwatch.StartNew();
        var resp = await client.GetAsync($"repos/{owner}/{repo}", ct);
        sw.Stop();

        _stats.Record("GitHub", sw.ElapsedMilliseconds);

        if (!resp.IsSuccessStatusCode)
        {
            _log.LogError("GitHub API error {Status} for {Owner}/{Repo}", resp.StatusCode, owner, repo);
            return null;
        }

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        var info = await JsonSerializer.DeserializeAsync<GitHubRepoInfo>(
            stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);

        if (info != null)
            _cache.Set(cacheKey, info, TimeSpan.FromMinutes(10));

        return info;
    }
}
