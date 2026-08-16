using IdentityService.Data.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace IdentityService.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public JwtResult GenerateToken(User user, IEnumerable<string> roles)
    {
        var settings = GetJwtSettings();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.UserName ?? string.Empty)
        };

        if (roles != null)
        {
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(settings.DurationMinutes);

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = GenerateRefreshTokenValue();
        var refreshExpiresAt = DateTime.UtcNow.AddDays(settings.RefreshTokenDurationDays);

        return new JwtResult
        {
            Token = tokenString,
            ExpiresAt = expiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt
        };
    }

    internal static string GenerateRefreshTokenValue()
    {
        var refreshBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(refreshBytes);
        return Convert.ToBase64String(refreshBytes);
    }

    private JwtSettings GetJwtSettings()
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var key = jwtSection.GetValue<string>("Key");
        var issuer = jwtSection.GetValue<string>("Issuer");
        var audience = jwtSection.GetValue<string>("Audience");

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Jwt:Key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException("Jwt:Issuer is not configured.");
        }

        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("Jwt:Audience is not configured.");
        }

        return new JwtSettings
        {
            Key = key,
            Issuer = issuer,
            Audience = audience,
            DurationMinutes = jwtSection.GetValue<int>("DurationMinutes"),
            RefreshTokenDurationDays = jwtSection.GetValue<int?>("RefreshTokenDurationDays") ?? 7
        };
    }

    private sealed class JwtSettings
    {
        public required string Key { get; init; }
        public required string Issuer { get; init; }
        public required string Audience { get; init; }
        public int DurationMinutes { get; init; }
        public int RefreshTokenDurationDays { get; init; }
    }
}
