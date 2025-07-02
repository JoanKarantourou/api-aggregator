namespace ApiAggregator.Models.OpenAI;

/// <summary>
/// Represents a request sent to the OpenAI completion endpoint.
/// </summary>
public class OpenAICompletionRequest
{
    public string Model { get; set; } = "text-davinci-003";
    public string Prompt { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 100;
    public double Temperature { get; set; } = 0.7;
}
