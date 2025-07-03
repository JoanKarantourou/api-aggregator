using ApiAggregator.Models.Stats;
using System.Collections.Concurrent;

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

        private readonly ConcurrentDictionary<string, ApiStats> _stats = new();

        /// <summary>
        /// Records a completed request's duration for a specified external API.
        /// Categorizes response time as Fast (<100ms), Medium (100–199ms), or Slow (200ms+).
        /// </summary>
        /// <param name="apiName">The name of the external API (e.g., "OpenWeatherMap").</param>
        /// <param name="durationMs">The response time in milliseconds.</param>
        public void Record(string apiName, long durationMs)
        {
            var stat = _stats.GetOrAdd(apiName, _ => new ApiStats());

            Interlocked.Increment(ref stat.TotalRequests);
            Interlocked.Add(ref stat.TotalDurationMs, durationMs);

            if (durationMs < 100)
                Interlocked.Increment(ref stat.FastCount);
            else if (durationMs < 200)
                Interlocked.Increment(ref stat.MediumCount);
            else
                Interlocked.Increment(ref stat.SlowCount);
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
        public Dictionary<string, ApiStatisticsReport> GetStatisticsReport()
        {
            var report = new Dictionary<string, ApiStatisticsReport>();

            foreach (var entry in _stats)
            {
                var apiName = entry.Key;
                var stat = entry.Value;

                if (stat.TotalRequests == 0) continue;

                double average = (double)stat.TotalDurationMs / stat.TotalRequests;

                report[apiName] = new ApiStatisticsReport
                {
                    TotalRequests = stat.TotalRequests,
                    AverageResponseTimeMs = Math.Round(average, 2),
                    FastCount = stat.FastCount,
                    MediumCount = stat.MediumCount,
                    SlowCount = stat.SlowCount
                };
            }

            return report;
        }
    }
}
