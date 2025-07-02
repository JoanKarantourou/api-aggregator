namespace ApiAggregator.Models.OpenAI;

/// <summary>
/// Represents the final OpenAI completion result returned by the service.
/// </summary>
public class OpenAICompletion
{
    public string Prompt { get; set; }
    public string CompletionText { get; set; }
    public DateTime RetrievedAt { get; set; }
}
