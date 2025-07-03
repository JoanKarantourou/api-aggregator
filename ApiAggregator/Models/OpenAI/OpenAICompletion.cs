using System.Text.Json.Serialization;

namespace ApiAggregator.Models.OpenAI;

/// <summary>
/// Represents the final OpenAI completion result returned by the service.
/// </summary>
public class OpenAICompletion
{
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = default!;

    [JsonPropertyName("completionText")]
    public string CompletionText { get; set; } = default!;

    [JsonPropertyName("retrievedAt")]
    public DateTime RetrievedAt { get; set; }
}
