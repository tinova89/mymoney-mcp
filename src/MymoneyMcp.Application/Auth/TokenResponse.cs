namespace MymoneyMcp.Application.Auth;

/// <summary>Response of <c>/auth/token</c> and <c>/auth/refresh</c> (contracts/auth-api.md).</summary>
public sealed record TokenResponse(string AccessToken, string TokenType, int ExpiresIn, string RefreshToken)
{
    public override string ToString() => $"{nameof(TokenResponse)} {{ TokenType = {TokenType}, ExpiresIn = {ExpiresIn} }}";
}
