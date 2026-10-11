using MymoneyMcp.Domain.Auth;

namespace MymoneyMcp.Application.Common.Interfaces;

/// <summary>Persistence for refresh tokens (data-model.md, <c>refresh_tokens</c>).</summary>
public interface IRefreshTokenStore
{
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken);

    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes the used token only if it is still active and stores its replacement, in one
    /// database transaction. Returns false when another call already used the token, so a refresh
    /// token can succeed at most once even under concurrent calls.
    /// </summary>
    Task<bool> RotateAsync(Guid usedTokenId, DateTimeOffset now, RefreshToken replacement, CancellationToken cancellationToken);
}
