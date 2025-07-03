namespace ApiAggregator.Models.Stats
{
    public class ApiStatisticsReport
    {
        public long TotalRequests { get; set; }
        public double AverageResponseTimeMs { get; set; }
        public long FastCount { get; set; }
        public long MediumCount { get; set; }
        public long SlowCount { get; set; }
    }
}
