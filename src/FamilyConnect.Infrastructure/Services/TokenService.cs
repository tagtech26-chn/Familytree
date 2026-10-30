using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FamilyConnect.Application.Abstractions;
using FamilyConnect.Application.Contracts;
using FamilyConnect.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FamilyConnect.Infrastructure.Services;

public sealed class TokenService(IConfiguration configuration) : ITokenService
{
    public TokenResponse CreateTokens(AppUser user)
    {
        var issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT issuer is not configured.");
        var audience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT audience is not configured.");
        var key = configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("JWT signing key is not configured.");
        if (Encoding.UTF8.GetByteCount(key) < 32) throw new InvalidOperationException("JWT signing key must be at least 32 bytes.");
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(15);
        var claims = new[] {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("preferred_language", user.PreferredLanguage)
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims, now, expires, credentials);
        return new TokenResponse(new JwtSecurityTokenHandler().WriteToken(token),
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)), expires, user.UserId);
    }
}
