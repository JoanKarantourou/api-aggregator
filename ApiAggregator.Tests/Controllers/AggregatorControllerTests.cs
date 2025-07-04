using ApiAggregator.Models;
using ApiAggregator.Models.GitHub;
using ApiAggregator.Models.News;
using ApiAggregator.Models.OpenAI;
using ApiAggregator.Services;
using ApiAggregator.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregator.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]                     // flip to [Authorize] when JWT ready
public class AggregatorController : ControllerBase
{
    private readonly IGitHubService _gitHubService;
    private readonly INewsService _newsService;
    private readonly IOpenAIService _openAiService;

    public AggregatorController(
        IGitHubService gitHubService,
        INewsService newsService,
        IOpenAIService openAiService)
    {
        _gitHubService = gitHubService;
        _newsService = newsService;
        _openAiService = openAiService;
    }

    /// <summary>
    /// Aggregates GitHub repo info, news, and an OpenAI completion in parallel.
    /// Defaults: repo = dotnet/runtime, news → category=general.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAggregatedData(
        [FromQuery] string? gitHubOwner,
        [FromQuery] string? gitHubRepo,
        [FromQuery] string? newsQuery,
        [FromQuery] string? newsCategory,
        [FromQuery] string? sortBy,
        [FromQuery] string? openAiPrompt)
    {
        // 1) defaults
        gitHubOwner ??= "dotnet";
        gitHubRepo ??= "runtime";
        if (string.IsNullOrWhiteSpace(newsQuery) && string.IsNullOrWhiteSpace(newsCategory))
            newsCategory = "general";

        var ct = HttpContext.RequestAborted;

        // 2) kick off in parallel
        var ghTask = _gitHubService.GetRepositoryAsync(gitHubOwner, gitHubRepo, ct);
        var newsTask = _newsService.GetNewsAsync(newsQuery ?? "", newsCategory!, sortBy ?? "", ct);
        var aiTask = _openAiService.GetCompletionAsync(openAiPrompt ?? "", ct);

        GitHubRepoInfo? gh = null;
        List<NewsArticle>? ns = null;
        OpenAICompletion? ai = null;

        try { gh = await ghTask; } catch { }
        try { ns = await newsTask; } catch { }
        try { ai = await aiTask; } catch { }

        // 3) payload
        var response = new AggregatedResponse
        {
            GitHub = gh,
            News = ns ?? new List<NewsArticle>(),
            OpenAI = ai,
            GitHubStatus = gh != null ? "OK" : "Unavailable",
            NewsStatus = ns?.Count > 0 ? "OK" : "Unavailable or No results",
            OpenAIStatus = ai != null ? "OK" : "Unavailable",
            GeneratedAtUtc = DateTime.UtcNow
        };

        return Ok(response);
    }

    [HttpGet("stats")]
    public IActionResult GetStats([FromServices] StatsService stats) =>
        Ok(stats.GetStatisticsReport());
}
