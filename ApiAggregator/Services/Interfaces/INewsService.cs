using ApiAggregator.Models.News;

namespace ApiAggregator.Services.Interfaces
{
    public interface INewsService
    {
        /// <summary>
        /// Retrieves news articles based on query/category and allows cancellation.
        /// </summary>
        Task<List<NewsArticle>> GetNewsAsync(
            string query,
            string category,
            string sortBy,
            CancellationToken cancellationToken);
    }
}
