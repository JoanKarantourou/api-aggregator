using ApiAggregator.Models;
using ApiAggregator.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class AggregatorController : ControllerBase
    {
        private readonly WeatherService _weatherService;
        private readonly NewsService _newsService;
        private readonly OpenAIService _openAiService;

        public AggregatorController(
            WeatherService weatherService,
            NewsService newsService,
            OpenAIService openAiService)
        {
            _weatherService = weatherService;
            _newsService = newsService;
            _openAiService = openAiService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAggregatedData(
            [FromQuery] string? city,
            [FromQuery] string? newsQuery,
            [FromQuery] string? newsCategory,
            [FromQuery] string? sortBy,
            [FromQuery] string? openAiPrompt,
            [FromQuery] string? githubTopic,
            [FromQuery] string? githubLanguage)
        {
            // Set default values
            if (string.IsNullOrEmpty(city)) city = "London";
            if (string.IsNullOrEmpty(newsQuery) && string.IsNullOrEmpty(newsCategory)) newsCategory = "general";

            // Start tasks in parallel, passing the cancellation token
            var weatherTask = _weatherService.GetWeatherAsync(city, HttpContext.RequestAborted);
            var newsTask = _newsService.GetNewsAsync(newsQuery ?? string.Empty, newsCategory ?? string.Empty, sortBy ?? string.Empty, HttpContext.RequestAborted);
            var openAiTask = _openAiService.GetCompletionAsync(openAiPrompt ?? string.Empty, HttpContext.RequestAborted);

            // Wrap parallel execution in try/catch
            try
            {
                await Task.WhenAll(weatherTask, newsTask, openAiTask);
            }
            catch (Exception)
            {
                // Optionally log the exception here
            }

            // Retrieve results
            var weather = weatherTask.Result;
            var news = newsTask.Result;
            var openAiResult = openAiTask.Result;

            // Determine statuses
            string weatherStatus = weather != null ? "OK" : "Unavailable";
            string newsStatus = (news != null && news.Count > 0) ? "OK" : "Unavailable or No results";
            string openAiStatus = openAiResult != null ? "OK" : "Unavailable";

            var aggregatedResponse = new AggregatedResponse
            {
                Weather = weather,
                News = news!,
                OpenAI = openAiResult,
                WeatherStatus = weatherStatus,
                NewsStatus = newsStatus,
                OpenAIStatus = openAiStatus,
            };

            return Ok(aggregatedResponse);
        }

        [HttpGet("stats")]
        public IActionResult GetStats([FromServices] StatsService statsService)
        {
            var report = statsService.GetStatisticsReport();
            return Ok(report);
        }
    }
}
