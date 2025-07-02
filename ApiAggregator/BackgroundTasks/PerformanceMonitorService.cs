using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ApiAggregator.Services;

namespace ApiAggregator.BackgroundTasks
{
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

                        if (entry.Value != null)
                        {
                            var stats = entry.Value;
                            var avgProperty = stats.GetType().GetProperty("AverageResponseTimeMs");
                            if (avgProperty != null)
                            {
                                double currentAvg = (double)avgProperty.GetValue(stats);

                                if (_lastAverages.TryGetValue(apiName, out double previousAvg))
                                {
                                    if (previousAvg > 0 && currentAvg > 1.5 * previousAvg)
                                    {
                                        _logger.LogWarning(
                                            "Performance anomaly detected for {Api}: avg response time {Current}ms is >50% higher than previous {Previous}ms",
                                            apiName, currentAvg, previousAvg);
                                    }
                                }

                                _lastAverages[apiName] = currentAvg;
                            }
                        }
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
