using ApiAggregator.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

public class OpenAIServiceTests
{
    private OpenAIService _openAIService;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly IMemoryCache _cache;
    private readonly Mock<IConfiguration> _configMock;

    public OpenAIServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _cache = new MemoryCache(new MemoryCacheOptions());

        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["ExternalApis:OpenAI:ApiKey"]).Returns("fake-api-key");
    }

    [Fact]
    public async Task GetCompletionAsync_ReturnsCompletionText()
    {
        // Arrange
        var responseObj = new
        {
            choices = new[]
            {
                new {
                    text = "This is a mock OpenAI completion."
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
            BaseAddress = new Uri("https://api.openai.com/v1/")
        };

        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

        _openAIService = new OpenAIService(
            _httpClientFactoryMock.Object,
            _cache,
            new StatsService()
        );

        // Act
        var result = await _openAIService.GetCompletionAsync("tell me a joke");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("mock OpenAI", result.CompletionText);
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
