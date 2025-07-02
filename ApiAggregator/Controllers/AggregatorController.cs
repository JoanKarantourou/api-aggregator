using ApiAggregator.Models;
using ApiAggregator.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AggregatorController : ControllerBase
    {
        private readonly WeatherService _weatherService;
        private readonly NewsService _newsService;
        private readonly OpenAIService _openAIService;

        public AggregatorController(
            WeatherService weatherService,
            NewsService newsService,
            OpenAIService openAIService)
        {
            _weatherService = weatherService;
            _newsService = newsService;
            _openAIService = openAIService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAggregatedData(
            [FromQuery] string? city,
            [FromQuery] string? newsQuery,
            [FromQuery] string? newsCategory,
            [FromQuery] string? sortBy,
            [FromQuery] string? openAiPrompt)
        {
            city ??= "London";
            if (string.IsNullOrEmpty(newsQuery) && string.IsNullOrEmpty(newsCategory))
                newsCategory = "general";

            var weatherTask = _weatherService.GetWeatherAsync(city);
            var newsTask = _newsService.GetNewsAsync(newsQuery ?? "", newsCategory ?? "", sortBy ?? "");
            var openAITask = _openAIService.GetCompletionAsync(openAiPrompt ?? "");

            await Task.WhenAll(weatherTask, newsTask, openAITask);

            var weather = weatherTask.Result;
            var news = newsTask.Result;
            var openAI = openAITask.Result;

            var response = new AggregatedResponse
            {
                Weather = weather,
                News = news,
                OpenAI = openAI,
                WeatherStatus = weather != null ? "OK" : "Unavailable",
                NewsStatus = news?.Any() == true ? "OK" : "Unavailable or No results",
                OpenAIStatus = openAI != null ? "OK" : "Unavailable"
            };

            return Ok(response);
        }

        [HttpGet("stats")]
        public IActionResult GetStats([FromServices] StatsService statsService)
        {
            var report = statsService.GetStatisticsReport();
            return Ok(report);
        }
    }
}
