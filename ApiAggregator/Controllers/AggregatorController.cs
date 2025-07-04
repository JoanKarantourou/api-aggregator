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
[AllowAnonymous]                       // flip to [Authorize] when JWT is ready
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
    /// Defaults: repo = dotnet/runtime, category = general.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAggregatedData(
        [FromQuery] string? gitHubOwner,
        [FromQuery] string? gitHubRepo,
        [FromQuery] string? newsQuery,
        [FromQuery] string? newsCategory,
        [FromQuery] string? newsSortBy,
        [FromQuery] string? openAiPrompt)
    {
        // sensible fall-backs
        gitHubOwner ??= "dotnet";
        gitHubRepo ??= "runtime";
        if (string.IsNullOrWhiteSpace(newsQuery) && string.IsNullOrWhiteSpace(newsCategory))
            newsCategory = "general";

        var ct = HttpContext.RequestAborted;

        // kick off in parallel
        var gitHubTask = _gitHubService.GetRepositoryAsync(gitHubOwner, gitHubRepo, ct);
        var newsTask = _newsService.GetNewsAsync(newsQuery ?? "", newsCategory ?? "", newsSortBy ?? "", ct);
        var aiTask = _openAiService.GetCompletionAsync(openAiPrompt ?? "", ct);

        GitHubRepoInfo? gitHub = null;
        List<NewsArticle>? news = null;
        OpenAICompletion? aiRes = null;

        try { gitHub = await gitHubTask; } catch { /* swallow & mark as null */ }
        try { news = await newsTask; } catch { }
        try { aiRes = await aiTask; } catch { }

        var response = new AggregatedResponse
        {
            GitHub = gitHub,
            News = news ?? new List<NewsArticle>(),
            OpenAI = aiRes,
            GitHubStatus = gitHub != null ? "OK" : "Unavailable",
            NewsStatus = news?.Count > 0 ? "OK" : "Unavailable or No results",
            OpenAIStatus = aiRes != null ? "OK" : "Unavailable",
            GeneratedAtUtc = DateTime.UtcNow
        };

        return Ok(response);
    }

    [HttpGet("stats")]
    public IActionResult GetStats([FromServices] StatsService stats) =>
        Ok(stats.GetStatisticsReport());
}
