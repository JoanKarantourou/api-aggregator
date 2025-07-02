namespace ApiAggregator.Models
{
    /// <summary>
    /// Represents the result of an OpenAI completion request.
    /// </summary>
    public class OpenAICompletion
    {
        /// <summary>
        /// The prompt submitted to OpenAI's API.
        /// </summary>
        public string Prompt { get; set; }

        /// <summary>
        /// The generated completion response from OpenAI.
        /// </summary>
        public string Completion { get; set; }

        /// <summary>
        /// The sampling temperature used in generation (optional).
        /// </summary>
        public float? Temperature { get; set; }

        /// <summary>
        /// The maximum number of tokens for the response (optional).
        /// </summary>
        public int? MaxTokens { get; set; }
    }
}