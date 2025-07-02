namespace ApiAggregator.Configuration
{
    public record ExternalApiOptions
    {
        public Api OpenWeather { get; init; } = default!;
        public Api NewsApi { get; init; } = default!;
        public Api OpenAI { get; init; } = default!;
        public record Api
        {
            public string BaseUrl { get; init; } = "";
            public string ApiKey { get; init; } = "";   // or AuthToken
        }
    }

}
