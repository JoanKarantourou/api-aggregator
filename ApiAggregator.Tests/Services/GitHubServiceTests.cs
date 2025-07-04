using ApiAggregator.Models.GitHub;
using ApiAggregator.Services;
using ApiAggregator.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Xunit;

namespace ApiAggregator.Tests.Services
{
    public class GitHubServiceTests
    {
        private readonly GitHubService _gitHubService;
        private readonly Mock<IHttpClientFactory> _httpFactoryMock = new();
        private readonly Mock<HttpMessageHandler> _handlerMock = new();
        private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private readonly IOptions<ExternalApiOptions> _options;

        public GitHubServiceTests()
        {
            // 1) configure ExternalApiOptions
            var opts = new ExternalApiOptions
            {
                GitHub = new ExternalApiOptions.GitHubApi
                {
                    BaseUrl = "https://api.github.com/",
                    ApiKey = "fake-token",
                    UserAgent = "ApiAggregator-Tests"
                }
            };
            _options = Options.Create(opts);

            // 2) prepare fake HTTP response
            var repoObj = new
            {
                name = "myrepo",
                full_name = "octocat/myrepo",
                description = "Test repo",
                html_url = "https://github.com/octocat/myrepo",
                stargazers_count = 42,
                forks_count = 7,
                open_issues_count = 1,
                default_branch = "main",
                updated_at = "2025-07-04T00:00:00Z"
            };
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(repoObj))
            };
            fakeResponse.Content.Headers.ContentType =
                new MediaTypeHeaderValue("application/json");

            // 3) setup mock handler
            _handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(fakeResponse);

            // 4) create HttpClient with mock handler
            var client = new HttpClient(_handlerMock.Object)
            {
                BaseAddress = new Uri(opts.GitHub.BaseUrl)
            };
            _httpFactoryMock
                .Setup(f => f.CreateClient("GitHub"))
                .Returns(client);

            // 5) construct service under test
            var logger = new Mock<ILogger<GitHubService>>().Object;
            var statsService = new StatsService();

            _gitHubService = new GitHubService(
                _httpFactoryMock.Object,
                _cache,
                statsService,
                logger,
                _options);
        }

        [Fact]
        public async Task GetRepositoryAsync_ReturnsRepoInfo()
        {
            // Act
            var result = await _gitHubService.GetRepositoryAsync("octocat", "myrepo");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("octocat/myrepo", result!.FullName);
            Assert.Equal(42, result.Stars);
        }

        [Fact]
        public async Task GetRepositoryAsync_CachesResult_OnlyOneHttpCall()
        {
            // Act – call twice
            var first = await _gitHubService.GetRepositoryAsync("octocat", "myrepo");
            var second = await _gitHubService.GetRepositoryAsync("octocat", "myrepo");

            // Assert – same object & one HTTP hit
            Assert.Equal(first!.Stars, second!.Stars);

            _handlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }
    }
}
