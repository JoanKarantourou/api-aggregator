namespace ApiAggregator.Models.OpenAI;

/// <summary>
/// Represents the full response returned from the OpenAI completions endpoint.
/// </summary>
public class OpenAIResponse
{
    public List<OpenAIChoice> Choices { get; set; }
}

/// <summary>
/// Represents a single text completion choice returned by OpenAI.
/// </summary>
public class OpenAIChoice
{
    public string Text { get; set; }
}