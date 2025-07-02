namespace ApiAggregator.Configuration
{
    public record JwtSettings
    {
        public string SecretKey { get; init; } = "";
        public string Issuer { get; init; } = "";
        public string Audience { get; init; } = "";
    }
}
