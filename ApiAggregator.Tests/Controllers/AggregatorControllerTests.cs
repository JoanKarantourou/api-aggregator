using ApiAggregator.Models;
using ApiAggregator.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ApiAggregator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class AggregatorController : ControllerBase
    {
        private readonly IWeatherService _weatherService;
        private readonly INewsService _newsService;
        private readonly IOpenAIService _openAiService;

        public AggregatorController(
            IWeatherService weatherService,
            INewsService newsService,
            IOpenAIService openAiService)
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
            [FromQuery] string? openAiPrompt)
        {
            // 1) apply defaults
            city = string.IsNullOrWhiteSpace(city) ? "London" : city;
            if (string.IsNullOrWhiteSpace(newsQuery)
             && string.IsNullOrWhiteSpace(newsCategory))
            {
                newsCategory = "general";
            }

            var token = HttpContext.RequestAborted;

            // 2) call each service
            var weather = await _weatherService.GetWeatherAsync(
                                  city, token);
            var news = await _newsService.GetNewsAsync(
                                  newsQuery ?? string.Empty,
                                  newsCategory!,
                                  sortBy ?? string.Empty,
                                  token);
            var openAiResult = await _openAiService.GetCompletionAsync(
                                  openAiPrompt ?? string.Empty,
                                  token);

            // 3) determine statuses
            var weatherStatus = weather != null ? "OK" : "Unavailable";
            var newsStatus = (news != null && news.Count > 0) ? "OK" : "Unavailable or No results";
            var openAiStatus = openAiResult != null ? "OK" : "Unavailable";

            // 4) shape payload
            var response = new AggregatedResponse
            {
                Weather = weather,
                News = news!,
                OpenAI = openAiResult,
                WeatherStatus = weatherStatus,
                NewsStatus = newsStatus,
                OpenAIStatus = openAiStatus,
                GeneratedAtUtc = DateTime.UtcNow
            };

            return Ok(response);
        }
    }
}
