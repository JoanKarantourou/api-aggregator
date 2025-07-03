using ApiAggregator.Models.News;
using ApiAggregator.Services;
using ApiAggregator.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ApiAggregator.Tests.Services
{
    public class NewsServiceTests
    {
        private readonly NewsService _newsService;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        private readonly Mock<HttpMessageHandler> _handlerMock = new Mock<HttpMessageHandler>();
        private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private readonly IConfiguration _configuration;
        private readonly List<NewsArticle> _expectedArticles;

        public NewsServiceTests()
        {
            // Configuration for API key
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["ExternalApis:NewsApi:ApiKey"] = "fake-news-key"
                })
                .Build();

            // Prepare dummy API response and expected model
            var publishedAt = DateTime.UtcNow;
            var apiResponse = new
            {
                articles = new[]
                {
                    new
                    {
                        title = "Test Title",
                        description = "Test Desc",
                        source = new { name = "TestSource" },
                        url = "http://test",
                        publishedAt = publishedAt
                    }
                }
            };

            _expectedArticles = new List<NewsArticle>
            {
                new NewsArticle
                {
                    Title = "Test Title",
                    Description = "Test Desc",
                    Source = "TestSource",
                    Url = "http://test",
                    PublishedAt = publishedAt
                }
            };

            // Mock HTTP handler to return our fake response
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(apiResponse))
            };
            fakeResponse.Content.Headers.ContentType =
                new MediaTypeHeaderValue("application/json");

            _handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(fakeResponse);

            // Setup HttpClientFactory to use our handler
            var client = new HttpClient(_handlerMock.Object)
            {
                BaseAddress = new Uri("https://newsapi.org/v2/")
            };
            _httpClientFactoryMock
                .Setup(f => f.CreateClient("NewsApi"))
                .Returns(client);

            // Construct the service under test
            _newsService = new NewsService(
                _httpClientFactoryMock.Object,
                _cache,
                new StatsService(),
                _configuration
            );
        }

        [Theory]
        [InlineData("bitcoin", "", "relevancy")]
        [InlineData("bitcoin", "", "publishedAt")]
        public async Task GetNewsAsync_WithQuery_ReturnsArticles_AndCaches(
            string query, string category, string sortBy)
        {
            // Act: call twice
            var first = await _newsService.GetNewsAsync(query, category, sortBy, CancellationToken.None);
            var second = await _newsService.GetNewsAsync(query, category, sortBy, CancellationToken.None);

            // Assert: correct data
            Assert.Equal(_expectedArticles.Count, first.Count);
            Assert.Equal(_expectedArticles[0].Title, first[0].Title);
            Assert.Equal(_expectedArticles[0].Description, first[0].Description);
            Assert.Equal(_expectedArticles[0].Source, first[0].Source);
            Assert.Equal(_expectedArticles[0].Url, first[0].Url);
            Assert.Equal(_expectedArticles[0].PublishedAt, first[0].PublishedAt);

            // Cache-hit: handler invoked only once
            _handlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Theory]
        [InlineData("", "business", "")]
        [InlineData("", "sports", "")]
        public async Task GetNewsAsync_WithCategory_ReturnsArticles_AndCaches(
            string query, string category, string sortBy)
        {
            // Act: call twice
            var first = await _newsService.GetNewsAsync(query, category, sortBy, CancellationToken.None);
            var second = await _newsService.GetNewsAsync(query, category, sortBy, CancellationToken.None);

            // Assert: correct data
            Assert.Equal(_expectedArticles.Count, first.Count);
            Assert.Equal(_expectedArticles[0].Title, first[0].Title);
            Assert.Equal(_expectedArticles[0].Description, first[0].Description);
            Assert.Equal(_expectedArticles[0].Source, first[0].Source);
            Assert.Equal(_expectedArticles[0].Url, first[0].Url);
            Assert.Equal(_expectedArticles[0].PublishedAt, first[0].PublishedAt);

            // Cache-hit: handler invoked only once
            _handlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
        }
    }
}
