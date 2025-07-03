using ApiAggregator.Models.Stats;
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

        public PerformanceMonitorService(ILogger<PerformanceMonitorService> logger, StatsService stats)
        {
            _logger = logger;
            _stats = stats;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PerformanceMonitorService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var report = _stats.GetStatisticsReport();

                    foreach (var entry in report)
                    {
                        string apiName = entry.Key;
                        double currentAvg = entry.Value.AverageResponseTimeMs;

                        if (_lastAverages.TryGetValue(apiName, out var prevAvg))
                        {
                            if (prevAvg > 0 && currentAvg > 1.5 * prevAvg)
                            {
                                _logger.LogWarning(
                                    "[{Timestamp}] Performance anomaly detected for {Api}: avg response time {Current}ms is >50% higher than previous {Previous}ms",
                                    DateTime.UtcNow.ToString("u"), apiName, currentAvg, prevAvg);
                            }
                        }

                        _lastAverages[apiName] = currentAvg;
                    }
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