using MediatR;
using MymoneyMcp.Application.Common.Exceptions;
using MymoneyMcp.Application.Common.Interfaces;
using DomainRefreshToken = MymoneyMcp.Domain.Auth.RefreshToken;

namespace MymoneyMcp.Application.Auth.RefreshToken;

/// <summary>
/// Exchanges an active refresh token for a new pair and revokes the used one (rotation). Unknown,
/// used and expired tokens all fail the same way.
/// </summary>
public sealed class RefreshTokenCommandHandler(
    ITokenService tokens,
    IRefreshTokenStore refreshTokens,
    TimeProvider time)
    : IRequestHandler<RefreshTokenCommand, TokenResponse>
{
    public async Task<TokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var used = await refreshTokens.FindByHashAsync(tokens.HashRefreshToken(request.RefreshToken), cancellationToken);
        if (used is null || !used.IsActive(now))
        {
            throw new UnauthorizedException();
        }

        var refresh = tokens.CreateRefreshToken();
        var replacement = DomainRefreshToken.Create(used.ClientId, refresh.Hash, refresh.ExpiresAt, now);

        if (!await refreshTokens.RotateAsync(used.Id, now, replacement, cancellationToken))
        {
            throw new UnauthorizedException();
        }

        var access = tokens.CreateAccessToken(used.ClientId);
        return new TokenResponse(access.Token, "Bearer", access.ExpiresInSeconds, refresh.Token);
    }
}
