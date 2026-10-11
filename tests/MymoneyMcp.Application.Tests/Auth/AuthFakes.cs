using MymoneyMcp.Application.Common.Interfaces;
using DomainRefreshToken = MymoneyMcp.Domain.Auth.RefreshToken;

namespace MymoneyMcp.Application.Tests.Auth;

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

internal sealed class FakeClientCredentialStore(string clientId, string clientSecret) : IClientCredentialStore
{
    public bool IsValid(string candidateId, string candidateSecret) =>
        candidateId == clientId && candidateSecret == clientSecret;
}

internal sealed class FakeTokenService(TimeProvider time) : ITokenService
{
    private int counter;

    public AccessToken CreateAccessToken(string clientId) => new($"access-{clientId}-{++counter}", 3600);

    public IssuedRefreshToken CreateRefreshToken()
    {
        var token = $"refresh-{++counter}";
        return new IssuedRefreshToken(token, HashRefreshToken(token), time.GetUtcNow().AddDays(14));
    }

    public string HashRefreshToken(string refreshToken) => $"hash:{refreshToken}";
}

internal sealed class FakeRefreshTokenStore : IRefreshTokenStore
{
    public List<DomainRefreshToken> Tokens { get; } = [];

    public Task AddAsync(DomainRefreshToken token, CancellationToken cancellationToken)
    {
        Tokens.Add(token);
        return Task.CompletedTask;
    }

    public Task<DomainRefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(Tokens.SingleOrDefault(t => t.TokenHash == tokenHash));

    public Task<bool> RotateAsync(
        Guid usedTokenId,
        DateTimeOffset now,
        DomainRefreshToken replacement,
        CancellationToken cancellationToken)
    {
        var used = Tokens.Single(t => t.Id == usedTokenId);
        if (!used.IsActive(now))
        {
            return Task.FromResult(false);
        }

        used.Revoke(now);
        Tokens.Add(replacement);
        return Task.FromResult(true);
    }
}
