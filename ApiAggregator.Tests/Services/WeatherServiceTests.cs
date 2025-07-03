using ApiAggregator.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ApiAggregator.Tests.Services
{
    public class WeatherServiceTests
    {
        private readonly WeatherService _weatherService;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock
            = new Mock<IHttpClientFactory>();
        private readonly Mock<HttpMessageHandler> _handlerMock
            = new Mock<HttpMessageHandler>();
        private readonly IMemoryCache _cache
            = new MemoryCache(new MemoryCacheOptions());
        private readonly IConfiguration _configuration;
        private readonly IOptions<ExternalApiOptions> _apiOptions;

        public WeatherServiceTests()
        {
            // Configuration
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["ExternalApis:OpenWeather:ApiKey"] = "fake-api-key"
                }).Build();

            // IOptions<ExternalApiOptions>
            var optsModel = new ExternalApiOptions
            {
                OpenWeather = new ExternalApiOptions.OpenWeatherApi
                {
                    BaseUrl = "https://api.openweathermap.org/data/2.5/",
                    ApiKey = "fake-api-key"
                }
            };
            _apiOptions = Options.Create(optsModel);

            // Prepare fake HTTP response
            var responseObj = new
            {
                main = new { temp = 25.0 },
                weather = new[] { new { description = "clear sky" } }
            };
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(responseObj))
            };
            fakeResponse.Content.Headers.ContentType
                = new MediaTypeHeaderValue("application/json");

            // Setup mock handler
            _handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(fakeResponse);

            // Create HttpClient with the mock handler
            var client = new HttpClient(_handlerMock.Object)
            {
                BaseAddress = new Uri(optsModel.OpenWeather.BaseUrl)
            };
            _httpClientFactoryMock
                .Setup(x => x.CreateClient("OpenWeather"))
                .Returns(client);

            // Construct the service under test
            _weatherService = new WeatherService(
                _httpClientFactoryMock.Object,
                _cache,
                new StatsService(),
                NullLogger<WeatherService>.Instance,
                _configuration,
                _apiOptions
            );
        }

        [Theory]
        [InlineData("london")]
        [InlineData("paris")]
        public async Task GetWeatherAsync_ReturnsCorrectWeatherInfo(string city)
        {
            var result = await _weatherService.GetWeatherAsync(city, CancellationToken.None);
            Assert.NotNull(result);
            Assert.Equal(25.0, result.Temperature);
            Assert.Equal("clear sky", result.Description);
        }

        [Theory]
        [InlineData("london")]
        [InlineData("paris")]
        public async Task GetWeatherAsync_CachesResult_OnlyOneHttpCall(string city)
        {
            // First & second call
            var first = await _weatherService.GetWeatherAsync(city, CancellationToken.None);
            var second = await _weatherService.GetWeatherAsync(city, CancellationToken.None);

            Assert.Equal(first.Temperature, second.Temperature);
            Assert.Equal(first.Description, second.Description);

            // Verify only one SendAsync invocation
            _handlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
        }
    }
}
