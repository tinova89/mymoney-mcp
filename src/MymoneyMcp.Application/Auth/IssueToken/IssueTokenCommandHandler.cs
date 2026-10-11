using MediatR;
using MymoneyMcp.Application.Common.Exceptions;
using MymoneyMcp.Application.Common.Interfaces;
using DomainRefreshToken = MymoneyMcp.Domain.Auth.RefreshToken;

namespace MymoneyMcp.Application.Auth.IssueToken;

/// <summary>
/// Issues an access token and a refresh token for valid client credentials. Never logs the
/// secret or any issued token (Constitution VIII).
/// </summary>
public sealed class IssueTokenCommandHandler(
    IClientCredentialStore clients,
    ITokenService tokens,
    IRefreshTokenStore refreshTokens,
    TimeProvider time)
    : IRequestHandler<IssueTokenCommand, TokenResponse>
{
    public async Task<TokenResponse> Handle(IssueTokenCommand request, CancellationToken cancellationToken)
    {
        if (!clients.IsValid(request.ClientId, request.ClientSecret))
        {
            throw new UnauthorizedException();
        }

        var access = tokens.CreateAccessToken(request.ClientId);
        var refresh = tokens.CreateRefreshToken();

        await refreshTokens.AddAsync(
            DomainRefreshToken.Create(request.ClientId, refresh.Hash, refresh.ExpiresAt, time.GetUtcNow()),
            cancellationToken);

        return new TokenResponse(access.Token, "Bearer", access.ExpiresInSeconds, refresh.Token);
    }
}
