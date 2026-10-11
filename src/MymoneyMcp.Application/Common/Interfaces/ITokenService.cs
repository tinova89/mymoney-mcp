namespace MymoneyMcp.Application.Common.Interfaces;

public sealed record AccessToken(string Token, int ExpiresInSeconds);

/// <summary>A new opaque refresh token. Only <see cref="Hash"/> is ever stored.</summary>
public sealed record IssuedRefreshToken(string Token, string Hash, DateTimeOffset ExpiresAt)
{
    public override string ToString() => $"{nameof(IssuedRefreshToken)} {{ ExpiresAt = {ExpiresAt:O} }}";
}

/// <summary>Creates the tokens this system issues (research R7).</summary>
public interface ITokenService
{
    AccessToken CreateAccessToken(string clientId);

    IssuedRefreshToken CreateRefreshToken();

    string HashRefreshToken(string refreshToken);
}
