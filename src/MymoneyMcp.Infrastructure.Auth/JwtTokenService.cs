using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MymoneyMcp.Application.Common.Interfaces;

namespace MymoneyMcp.Infrastructure.Auth;

/// <summary>
/// Issues HMAC-SHA256 JWTs (claims <c>sub</c>, <c>iss</c>, <c>aud</c>, <c>iat</c>, <c>exp</c>; no group)
/// and opaque refresh tokens stored as SHA-256 hashes (research R7).
/// </summary>
internal sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider time) : ITokenService
{
    private readonly JsonWebTokenHandler handler = new();

    public AccessToken CreateAccessToken(string clientId)
    {
        var jwt = options.Value;
        if (string.IsNullOrEmpty(jwt.SigningKey))
        {
            throw new InvalidOperationException("Jwt:SigningKey is not configured.");
        }

        var now = time.GetUtcNow().UtcDateTime;
        var lifetime = TimeSpan.FromMinutes(jwt.AccessTokenMinutes);

        var token = handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, clientId)]),
            IssuedAt = now,
            NotBefore = now,
            Expires = now.Add(lifetime),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                SecurityAlgorithms.HmacSha256),
        });

        return new AccessToken(token, (int)lifetime.TotalSeconds);
    }

    public IssuedRefreshToken CreateRefreshToken()
    {
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        var expiresAt = time.GetUtcNow().AddDays(options.Value.RefreshTokenDays);
        return new IssuedRefreshToken(token, HashRefreshToken(token), expiresAt);
    }

    public string HashRefreshToken(string refreshToken) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
