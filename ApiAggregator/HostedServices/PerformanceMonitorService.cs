using ApiAggregator.Services;

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
        private readonly Dictionary<string, double> _lastAverages = new();

        public PerformanceMonitorService(
            ILogger<PerformanceMonitorService> logger,
            StatsService stats)
        {
            _logger = logger;
            _stats = stats;
        }

        /// <summary>
        /// Checks the current statistics and logs a warning if any API's
        /// average response time exceeds 150% of its previous average.
        /// </summary>
        public async Task CheckPerformanceAsync()
        {
            var report = _stats.GetStatisticsReport();
            foreach (var kvp in report)
            {
                var apiName = kvp.Key;
                var avgNow = kvp.Value.AverageResponseTimeMs;
                if (_lastAverages.TryGetValue(apiName, out var prevAvg) && prevAvg > 0)
                {
                    if (avgNow > prevAvg * 1.5)
                    {
                        _logger.LogWarning(
                            "[PerformanceMonitor] Performance anomaly detected for {Api}: current average {Current}ms is >150% of previous {Previous}ms",
                            apiName, avgNow, prevAvg);
                    }
                }
                _lastAverages[apiName] = avgNow;
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Runs the background loop, invoking CheckPerformanceAsync at each interval.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PerformanceMonitorService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckPerformanceAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in PerformanceMonitorService");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}
