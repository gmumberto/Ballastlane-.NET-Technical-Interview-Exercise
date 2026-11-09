using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using User = TaskManagement.Domain.Entities.User;

namespace TaskManagement.Infrastructure.Authentication;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public AuthResponseDto GenerateToken(User user)
    {
        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT Issuer is missing.");

        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "JWT Audience is missing.");

        var keyBase64 = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT Key is missing.");

        var keyBytes = Convert.FromBase64String(keyBase64);

        if (keyBytes.Length < 32)
            throw new InvalidOperationException(
                "JWT Key must contain at least 32 bytes.");

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(keyBytes),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),

            new Claim(JwtRegisteredClaimNames.Email,
                user.Email),

            new Claim("name", user.UserName),

            new Claim(JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        var expiresIn = 3600;
        var expires = DateTime.UtcNow.AddSeconds(expiresIn);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new AuthResponseDto
        {
            AccessToken =
                new JwtSecurityTokenHandler()
                    .WriteToken(token),

            TokenType = "Bearer",
            ExpiresIn = expiresIn
        };
    }
}