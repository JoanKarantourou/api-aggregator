using System.Text.Json.Serialization;

namespace ApiAggregator.Models.OpenAI;

/// <summary>
/// Represents the full response returned from the OpenAI completions endpoint.
/// </summary>
public class OpenAIResponse
{
    [JsonPropertyName("choices")]
    public List<OpenAIChoice> Choices { get; set; } = new();

    [JsonPropertyName("usage")]
    public Usage Usage { get; set; } = default!;
}

/// <summary>
/// Represents a single text completion choice returned by OpenAI.
/// </summary>
public class OpenAIChoice
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = default!;
}

/// <summary>
/// Represents usage statistics for an OpenAI completion request.
/// </summary>
public class Usage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}