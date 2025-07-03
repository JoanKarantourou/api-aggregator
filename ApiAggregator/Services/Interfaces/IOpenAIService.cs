using ApiAggregator.Models.OpenAI;

namespace ApiAggregator.Services.Interfaces
{
    /// <summary>
    /// Service for generating text completions via the OpenAI API.
    /// </summary>
    public interface IOpenAIService
    {
        Task<OpenAICompletion?> GetCompletionAsync(string prompt, CancellationToken cancellationToken);
    }
}
