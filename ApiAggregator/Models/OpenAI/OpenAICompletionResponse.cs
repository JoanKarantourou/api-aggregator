namespace ApiAggregator.Models.OpenAI;

/// <summary>
/// Represents the response from the OpenAI completion endpoint.
/// </summary>
public class OpenAICompletionResponse
{
    public List<OpenAIChoice> Choices { get; set; } = new();
}
