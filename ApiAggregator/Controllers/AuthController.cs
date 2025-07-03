using ApiAggregator.Models;
using Microsoft.AspNetCore.Mvc;
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
        private readonly IConfiguration _config;
        private readonly string _username;
        private readonly string _password;

        public AuthController(IConfiguration config)
        {
            _config = config;
            _username = _config["DemoCredentials:Username"]!;
            _password = _config["DemoCredentials:Password"]!;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody][Required] LoginModel login)
        {
            // Simple static validation (demo only)
            if (login.Username == _username && login.Password == _password)
            {
                var token = GenerateJwtToken(login.Username);
                return Ok(new { token });
            }

            return Unauthorized();
        }

        [HttpPost("refresh-token")]
        public IActionResult RefreshToken([FromBody] RefreshTokenRequest request)
        {
            return StatusCode(StatusCodes.Status501NotImplemented,
                new { Message = "Refresh-token endpoint not implemented." });
        }

        private string GenerateJwtToken(string username)
        {
            var rawKey = _config["JwtSettings:SecretKey"]
                ?? throw new InvalidOperationException("JwtSettings:SecretKey is missing in configuration.");

            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(rawKey));
            var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"],
                audience: _config["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
