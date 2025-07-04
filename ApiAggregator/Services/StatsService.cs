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
        private const int FastThresholdMs = 100;
        private const int MediumThresholdMs = 200;

        private readonly ConcurrentDictionary<string, ApiStats> _stats = new();

        /// <summary>
        /// Records a completed request's duration for a specified external API.
        /// Categorizes response time as Fast (<100ms), Medium (100–199ms), or Slow (200ms+).
        /// </summary>
        /// <param name="apiName">The name of the external API.</param>
        /// <param name="durationMs">The response time in milliseconds.</param>
        public void Record(string apiName, long durationMs)
        {
            var stat = _stats.GetOrAdd(apiName, _ => new ApiStats());

            Interlocked.Increment(ref stat.TotalRequests);
            Interlocked.Add(ref stat.TotalDurationMs, durationMs);

            if (durationMs < FastThresholdMs)
                Interlocked.Increment(ref stat.FastCount);
            else if (durationMs < MediumThresholdMs)
                Interlocked.Increment(ref stat.MediumCount);
            else
                Interlocked.Increment(ref stat.SlowCount);
        }

        /// <summary>
        /// Retrieves aggregated statistics for all tracked APIs.
        /// </summary>
        /// <returns>
        /// A dictionary where each key is the API name and the value is an
        /// <see cref="ApiStatisticsReport"/> with request counts, average time,
        /// and Fast/Medium/Slow response buckets.
        /// </returns>
        public Dictionary<string, ApiStatisticsReport> GetStatisticsReport()
        {
            var report = new Dictionary<string, ApiStatisticsReport>();

            foreach (var (apiName, stat) in _stats)
            {
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

        /// <summary>
        /// Internal record to track cumulative API stats with thread-safe counters.
        /// </summary>
        private class ApiStats
        {
            public long TotalRequests;
            public long TotalDurationMs;
            public long FastCount;
            public long MediumCount;
            public long SlowCount;
        }
    }
}
