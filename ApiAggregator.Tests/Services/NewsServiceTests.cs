using ApiAggregator.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

public class NewsServiceTests
{
    private NewsService _newsService;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly IMemoryCache _cache;
    private readonly Mock<IConfiguration> _configMock;

    public NewsServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _cache = new MemoryCache(new MemoryCacheOptions());

        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["ExternalApis:NewsApi:ApiKey"]).Returns("fake-api-key");
    }

    [Fact]
    public async Task GetNewsAsync_ReturnsNewsArticles()
    {
        // Arrange
        var responseObj = new
        {
            articles = new[]
            {
                new {
                    title = "Test Headline",
                    description = "Some summary",
                    url = "https://example.com/news",
                    source = new { name = "Test Source" }
                }
            }
        };

        var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(responseObj))
        };
        fakeResponse.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var handler = new FakeHttpMessageHandler(fakeResponse);
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://newsapi.org/v2/")
        };

        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

        _newsService = new NewsService(
            _httpClientFactoryMock.Object,
            _cache,
            new StatsService(),
            _configMock.Object
        );

        // Act
        var result = await _newsService.GetNewsAsync("ai", "technology", "relevancy");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Test Headline", result[0].Title);
        Assert.Equal("Test Source", result[0].Source);
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
