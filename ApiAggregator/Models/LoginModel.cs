using System.Text.Json.Serialization;

namespace ApiAggregator.Models;

/// <summary>
/// Represents the credentials supplied by a user when logging in.
/// </summary>
public class LoginModel
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = default!;

    [JsonPropertyName("password")]
    public string Password { get; set; } = default!;
}