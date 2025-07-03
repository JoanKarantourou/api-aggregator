using ApiAggregator.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using ApiAggregator.Services;
using Xunit;

public class WeatherServiceTests
{
    private WeatherService _weatherService;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly IMemoryCache _cache;
    private readonly Mock<IConfiguration> _configMock;

    public WeatherServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _cache = new MemoryCache(new MemoryCacheOptions());

        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["ExternalApis:OpenWeather:ApiKey"]).Returns("fake-api-key");
    }

    [Fact]
    public async Task GetWeatherAsync_ReturnsWeatherInfo()
    {
        // Arrange
        var responseObj = new
        {
            main = new { temp = 25.0 },
            weather = new[] { new { description = "clear sky" } }
        };

        var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(responseObj))
        };
        fakeResponse.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var clientHandler = new FakeHttpMessageHandler(fakeResponse);
        var client = new HttpClient(clientHandler)
        {
            BaseAddress = new Uri("https://api.openweathermap.org/data/2.5/")
        };

        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

        _weatherService = new WeatherService(
            _httpClientFactoryMock.Object,
            _cache,
            new StatsService(),
            _configMock.Object
        );

        // Act
        var result = await _weatherService.GetWeatherAsync("london");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(25.0, result.Temperature);
        Assert.Equal("clear sky", result.Description);
    }

    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public FakeHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }
}
