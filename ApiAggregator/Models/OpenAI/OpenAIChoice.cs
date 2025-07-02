namespace ApiAggregator.Models.OpenAI;

/// <summary>
/// Represents an individual choice in the OpenAI completion response.
/// </summary>
public class OpenAIChoice
{
    public string Text { get; set; } = string.Empty;
}
