using System.Collections.Generic;

namespace ApiAggregator.Models
{
    /// <summary>
    /// Represents the aggregated response from multiple external services.
    /// </summary>
    public class AggregatedResponse
    {
        /// <summary>
        /// Weather information returned by the weather service.
        /// </summary>
        public WeatherInfo Weather { get; set; }

        /// <summary>
        /// List of news articles returned by the news service.
        /// </summary>
        public List<NewsArticle> News { get; set; }

        /// <summary>
        /// OpenAI-generated completion returned by the OpenAI service.
        /// </summary>
        public OpenAICompletion OpenAI { get; set; }

        /// <summary>
        /// Indicates whether the weather API call succeeded.
        /// </summary>
        public bool IsWeatherSuccess { get; set; }

        /// <summary>
        /// Indicates whether the news API call succeeded.
        /// </summary>
        public bool IsNewsSuccess { get; set; }

        /// <summary>
        /// Indicates whether the OpenAI API call succeeded.
        /// </summary>
        public bool IsOpenAISuccess { get; set; }
    }
}
