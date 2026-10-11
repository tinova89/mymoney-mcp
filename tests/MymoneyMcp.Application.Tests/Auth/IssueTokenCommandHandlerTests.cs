using MymoneyMcp.Application.Auth.IssueToken;
using MymoneyMcp.Application.Common.Exceptions;

namespace MymoneyMcp.Application.Tests.Auth;

public sealed class IssueTokenCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeRefreshTokenStore store = new();
    private readonly IssueTokenCommandHandler handler;

    public IssueTokenCommandHandlerTests()
    {
        var time = new FixedTimeProvider(Now);
        handler = new IssueTokenCommandHandler(
            new FakeClientCredentialStore("client", "secret"),
            new FakeTokenService(time),
            store,
            time);
    }

    [Fact]
    public async Task Valid_credentials_return_a_bearer_token_pair_and_store_the_refresh_hash()
    {
        var response = await handler.Handle(new IssueTokenCommand("client", "secret"), CancellationToken.None);

        Assert.Equal("Bearer", response.TokenType);
        Assert.Equal(3600, response.ExpiresIn);
        Assert.StartsWith("access-client-", response.AccessToken);

        var stored = Assert.Single(store.Tokens);
        Assert.Equal("client", stored.ClientId);
        Assert.Equal($"hash:{response.RefreshToken}", stored.TokenHash);
        Assert.NotEqual(response.RefreshToken, stored.TokenHash);
    }

    [Fact]
    public async Task Unknown_client_and_wrong_secret_fail_the_same_way()
    {
        var unknownClient = await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(new IssueTokenCommand("other", "secret"), CancellationToken.None));
        var wrongSecret = await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(new IssueTokenCommand("client", "wrong"), CancellationToken.None));

        Assert.Equal(unknownClient.Message, wrongSecret.Message);
        Assert.Empty(store.Tokens);
    }
}
