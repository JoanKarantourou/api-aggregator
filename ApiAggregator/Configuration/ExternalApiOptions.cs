using System.ComponentModel.DataAnnotations;

public record ExternalApiOptions
{
    public OpenWeatherApi OpenWeather { get; init; } = default!;
    public Api NewsApi { get; init; } = default!;
    public OpenAiApi OpenAI { get; init; } = default!;

    public record Api
    {
        [Required]
        public string BaseUrl { get; init; } = "";

        [Required]
        public string ApiKey { get; init; } = "";   // or AuthToken
    }

    public record OpenWeatherApi : Api
    {
        [Range(1, 1440)]
        public int CacheMinutes { get; init; } = 5;
    }

    public record OpenAiApi : Api
    {
        [Required]
        public string Model { get; init; } = "gpt-4";

        [Range(0.0, 2.0)]
        public double Temperature { get; init; } = 0.7;

        [Range(1, 4096)]
        public int MaxTokens { get; init; } = 1024;
    }
}