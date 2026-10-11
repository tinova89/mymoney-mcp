using MymoneyMcp.Application.Auth.RefreshToken;
using MymoneyMcp.Application.Common.Exceptions;
using DomainRefreshToken = MymoneyMcp.Domain.Auth.RefreshToken;

namespace MymoneyMcp.Application.Tests.Auth;

public sealed class RefreshTokenCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeRefreshTokenStore store = new();
    private readonly FakeTokenService tokens;
    private readonly RefreshTokenCommandHandler handler;

    public RefreshTokenCommandHandlerTests()
    {
        var time = new FixedTimeProvider(Now);
        tokens = new FakeTokenService(time);
        handler = new RefreshTokenCommandHandler(tokens, store, time);
    }

    [Fact]
    public async Task Valid_token_is_revoked_and_a_new_pair_is_returned()
    {
        var old = StoreToken("old", expiresAt: Now.AddDays(1));

        var response = await handler.Handle(new RefreshTokenCommand("old"), CancellationToken.None);

        Assert.Equal(Now, old.RevokedAt);
        Assert.NotEqual("old", response.RefreshToken);
        Assert.StartsWith("access-client-", response.AccessToken);
        Assert.Contains(store.Tokens, t => t.TokenHash == tokens.HashRefreshToken(response.RefreshToken));
    }

    [Fact]
    public async Task Reused_token_is_rejected()
    {
        StoreToken("old", expiresAt: Now.AddDays(1));
        await handler.Handle(new RefreshTokenCommand("old"), CancellationToken.None);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(new RefreshTokenCommand("old"), CancellationToken.None));
    }

    [Fact]
    public async Task Expired_token_is_rejected()
    {
        StoreToken("old", expiresAt: Now.AddSeconds(-1));

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(new RefreshTokenCommand("old"), CancellationToken.None));
    }

    [Fact]
    public async Task Unknown_token_is_rejected()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(new RefreshTokenCommand("never-issued"), CancellationToken.None));
    }

    private DomainRefreshToken StoreToken(string token, DateTimeOffset expiresAt)
    {
        var entity = DomainRefreshToken.Create("client", tokens.HashRefreshToken(token), expiresAt, Now.AddDays(-1));
        store.Tokens.Add(entity);
        return entity;
    }
}
