using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FCG.BuildingBlocks.Auth;
using FCG.Users.Api.Domain.Entities;
using FCG.Users.Api.Domain.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FCG.Users.Api.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public TokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
        if (string.IsNullOrEmpty(_settings.Secret))
            throw new InvalidOperationException("JwtSettings:Secret is not configured.");
    }

    public string GenerateToken(Usuario u)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_settings.Secret!);
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()),
                new Claim(ClaimTypes.Name, u.Name ?? string.Empty),
                new Claim(ClaimTypes.Email, u.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, u.Role ?? "User")
            }),
            Expires = GetExpiration(),
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
    }

    public DateTime GetExpiration() => DateTime.UtcNow.AddHours(_settings.ExpirationHours);
}
