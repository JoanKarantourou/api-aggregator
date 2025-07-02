namespace ApiAggregator.Services
{
    /// <summary>
    /// Tracks usage and response performance of external APIs.
    /// Thread-safe singleton used for statistics aggregation.
    /// </summary>
    public class StatsService
    {
        private class ApiStats
        {
            public long TotalRequests;
            public long TotalDurationMs;
            public long FastCount;
            public long MediumCount;
            public long SlowCount;
        }

        private readonly object _lock = new();
        private readonly Dictionary<string, ApiStats> _stats = new();

        /// <summary>
        /// Records a completed request's duration for a specified external API.
        /// Categorizes response time as Fast (<100ms), Medium (100–199ms), or Slow (200ms+).
        /// </summary>
        /// <param name="apiName">The name of the external API (e.g., "OpenWeatherMap").</param>
        /// <param name="durationMs">The response time in milliseconds.</param>
        public void Record(string apiName, long durationMs)
        {
            lock (_lock)
            {
                if (!_stats.ContainsKey(apiName))
                {
                    _stats[apiName] = new ApiStats();
                }

                var stat = _stats[apiName];
                stat.TotalRequests++;
                stat.TotalDurationMs += durationMs;

                if (durationMs < 100)
                    stat.FastCount++;
                else if (durationMs < 200)
                    stat.MediumCount++;
                else
                    stat.SlowCount++;
            }
        }

        /// <summary>
        /// Retrieves aggregated statistics for all tracked APIs,
        /// including request counts and average response times.
        /// </summary>
        /// <returns>
        /// A dictionary where each key is the API name and the value is an object
        /// with statistics such as total requests, average response time, and
        /// counts in Fast/Medium/Slow performance buckets.
        /// </returns>
        public Dictionary<string, object> GetStatisticsReport()
        {
            lock (_lock)
            {
                var report = new Dictionary<string, object>();

                foreach (var entry in _stats)
                {
                    var apiName = entry.Key;
                    var stat = entry.Value;

                    if (stat.TotalRequests == 0) continue;

                    double average = (double)stat.TotalDurationMs / stat.TotalRequests;

                    report[apiName] = new
                    {
                        stat.TotalRequests,
                        AverageResponseTimeMs = Math.Round(average, 2),
                        stat.FastCount,
                        stat.MediumCount,
                        stat.SlowCount
                    };
                }

                return report;
            }
        }
    }
}
