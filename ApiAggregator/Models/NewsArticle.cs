using System;

namespace ApiAggregator.Models
{
    /// <summary>
    /// Represents a single news article.
    /// </summary>
    public class NewsArticle
    {
        /// <summary>
        /// The headline/title of the news article.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The direct URL to the full article.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The news source or publisher.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// The date and time the article was published.
        /// </summary>
        public DateTime PublishedAt { get; set; }
    }
}