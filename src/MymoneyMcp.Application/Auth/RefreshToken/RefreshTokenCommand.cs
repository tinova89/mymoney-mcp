using MediatR;

namespace MymoneyMcp.Application.Auth.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<TokenResponse>
{
    /// <summary>Never prints the token (Constitution VIII).</summary>
    public override string ToString() => nameof(RefreshTokenCommand);
}
