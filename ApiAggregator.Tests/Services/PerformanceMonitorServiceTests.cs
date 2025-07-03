using ApiAggregator.HostedServices;
using ApiAggregator.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace ApiAggregator.Tests.BackgroundTasks
{
    public class PerformanceMonitorServiceTests
    {
        [Fact]
        public async Task CheckPerformanceAsync_LogsWarning_WhenAverageOverThreshold()
        {
            // Arrange: stats yields two data points
            var stats = new StatsService();
            stats.Record("API1", 100);  // first avg = 100
            var loggerMock = new Mock<ILogger<PerformanceMonitorService>>();

            var svc = new PerformanceMonitorService(loggerMock.Object, stats);

            // Act #1: seed baseline (no warning)
            await svc.CheckPerformanceAsync();
            // Add a slow call → new avg = (100 + 300) /2 = 200 > 1.5×100
            stats.Record("API1", 300);
            await svc.CheckPerformanceAsync();

            // Assert: one warning was logged
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Performance anomaly detected for API1")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}
