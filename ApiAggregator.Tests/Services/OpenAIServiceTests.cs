using ApiAggregator.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ApiAggregator.Tests.Services
{
    public class OpenAIServiceTests
    {
        private readonly OpenAIService _openAIService;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
        private readonly Mock<HttpMessageHandler> _handlerMock = new();
        private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private readonly IOptions<ExternalApiOptions> _options;

        public OpenAIServiceTests()
        {
            // 1) Configure ExternalApiOptions for OpenAI
            var optionsModel = new ExternalApiOptions
            {
                OpenAI = new ExternalApiOptions.OpenAiApi
                {
                    BaseUrl = "https://api.openai.com/v1/",
                    ApiKey = "fake-api-key",
                    Model = "test-model",
                    MaxTokens = 16,
                    Temperature = 0.7
                }
            };
            _options = Options.Create(optionsModel);

            // 2) Prepare fake HTTP response
            var responseObj = new
            {
                choices = new[]
                {
                    new { text = "This is a mock completion." }
                }
            };
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(responseObj))
            };
            fakeResponse.Content.Headers.ContentType =
                new MediaTypeHeaderValue("application/json");

            // 3) Setup mock handler
            _handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(fakeResponse);

            // 4) Create HttpClient with mock handler
            var client = new HttpClient(_handlerMock.Object)
            {
                BaseAddress = new Uri(optionsModel.OpenAI.BaseUrl)
            };
            _httpClientFactoryMock
                .Setup(f => f.CreateClient("OpenAI"))
                .Returns(client);

            // 5) Construct the service under test, now including a logger
            var logger = new Mock<ILogger<OpenAIService>>().Object;
            _openAIService = new OpenAIService(
                _httpClientFactoryMock.Object,
                _cache,
                new StatsService(),
                logger,
                _options
            );
        }

        [Theory]
        [InlineData("tell me a joke")]
        [InlineData("hello world")]
        public async Task GetCompletionAsync_ReturnsCompletionText(string prompt)
        {
            // Act
            var result = await _openAIService.GetCompletionAsync(prompt, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("mock completion", result.CompletionText, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("tell me a joke")]
        [InlineData("hello world")]
        public async Task GetCompletionAsync_CachesResult_OnlyOneHttpCall(string prompt)
        {
            // Act: call twice
            var first = await _openAIService.GetCompletionAsync(prompt, CancellationToken.None);
            var second = await _openAIService.GetCompletionAsync(prompt, CancellationToken.None);

            // Assert: same result
            Assert.Equal(first.CompletionText, second.CompletionText);

            // Verify only one HTTP call
            _handlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
        }
    }
}
