using ApiAggregator.Models.Stats;
using ApiAggregator.Services;
using Xunit;

namespace ApiAggregator.Tests.Services
{
    public class StatsServiceTests
    {
        [Fact]
        public void Record_BucketsAndAverageAreCorrect()
        {
            // Arrange
            var stats = new StatsService();

            // Act: one in each bucket + boundary values
            stats.Record("API1", 99);  // Fast
            stats.Record("API1", 100);  // Medium (boundary)
            stats.Record("API1", 150);  // Medium
            stats.Record("API1", 200);  // Slow (boundary)
            stats.Record("API1", 250);  // Slow

            var report = stats.GetStatisticsReport();

            // Assert it's there
            Assert.True(report.ContainsKey("API1"));
            ApiStatisticsReport apiStats = report["API1"];

            // Total requests = 5
            Assert.Equal(5, apiStats.TotalRequests);

            // Average = (99+100+150+200+250)/5 = 159.8
            double expectedAvg = (99 + 100 + 150 + 200 + 250) / 5.0;
            Assert.InRange(apiStats.AverageResponseTimeMs, expectedAvg - 0.1, expectedAvg + 0.1);

            // Buckets
            Assert.Equal(1, apiStats.FastCount);
            Assert.Equal(2, apiStats.MediumCount);
            Assert.Equal(2, apiStats.SlowCount);
        }
    }
}