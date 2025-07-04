using ApiAggregator.Configuration;
using ApiAggregator.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ApiAggregator.Controllers
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthController> _logger;
        private readonly string _username;
        private readonly string _password;

        public AuthController(
            IConfiguration config,
            IOptions<JwtSettings> jwtOptions,
            ILogger<AuthController> logger)
        {
            _jwtSettings = jwtOptions.Value;
            _logger = logger;

            _username = config["DemoCredentials:Username"]
                ?? throw new InvalidOperationException("DemoCredentials:Username is missing.");
            _password = config["DemoCredentials:Password"]
                ?? throw new InvalidOperationException("DemoCredentials:Password is missing.");
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody][Required] LoginModel login)
        {
            if (login.Username == _username && login.Password == _password)
            {
                var token = GenerateJwtToken(login.Username);
                _logger.LogInformation("JWT issued for user '{Username}'", login.Username);
                return Ok(new { token });
            }

            _logger.LogWarning("Unauthorized login attempt for username '{Username}'", login.Username);
            return Unauthorized(new { Message = "Invalid credentials." });
        }

        [HttpPost("refresh-token")]
        public IActionResult RefreshToken([FromBody] RefreshTokenRequest request)
        {
            return StatusCode(StatusCodes.Status501NotImplemented,
                new { Message = "Refresh-token endpoint not implemented." });
        }

        private string GenerateJwtToken(string username)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
