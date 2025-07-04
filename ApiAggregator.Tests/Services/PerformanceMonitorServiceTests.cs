using ApiAggregator.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiAggregator.HostedServices
{
    /// <summary>
    /// Background service that monitors API response times and logs anomalies.
    /// </summary>
    public class PerformanceMonitorService : BackgroundService
    {
        private readonly ILogger<PerformanceMonitorService> _logger;
        private readonly StatsService _stats;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
        private readonly Dictionary<string, double> _previousAverages = new();

        public PerformanceMonitorService(
            ILogger<PerformanceMonitorService> logger,
            StatsService stats)
        {
            _logger = logger;
            _stats = stats;
        }

        /// <summary>
        /// Exposed for testing: runs a single check & logs warnings if needed.
        /// </summary>
        public Task CheckPerformanceAsync()
        {
            CheckPerformance();
            return Task.CompletedTask;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PerformanceMonitorService starting; checking every {Minutes} minutes", _interval.TotalMinutes);

            using var timer = new PeriodicTimer(_interval);
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        CheckPerformance();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during performance check");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when the host is shutting down
            }

            _logger.LogInformation("PerformanceMonitorService stopping");
        }

        /// <summary>
        /// Synchronously evaluates the latest averages against the previous run
        /// and logs a warning if any have degraded by more than 50%.
        /// </summary>
        private void CheckPerformance()
        {
            var report = _stats.GetStatisticsReport();
            foreach (var (apiName, stats) in report)
            {
                var avgNow = stats.AverageResponseTimeMs;
                if (_previousAverages.TryGetValue(apiName, out var prevAvg)
                    && prevAvg > 0
                    && avgNow > prevAvg * 1.5)
                {
                    _logger.LogWarning(
                        "[PerformanceMonitor] Performance anomaly detected for {Api}: current avg {Current}ms >150% of previous {Previous}ms",
                        apiName, avgNow, prevAvg);
                }
                _previousAverages[apiName] = avgNow;
            }
        }
    }
}
