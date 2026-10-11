using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using MymoneyMcp.Api.Tests.Infrastructure;

namespace MymoneyMcp.Api.Tests.Auth;

/// <summary>contracts/auth-api.md — spec US2 scenarios 2 and 3, FR-006.</summary>
[Collection(PostgresCollection.Name)]
public sealed class AuthApiTests(PostgresFixture postgres) : IAsyncLifetime
{
    private ApiFactory factory = null!;
    private HttpClient client = null!;

    public ValueTask InitializeAsync()
    {
        factory = new ApiFactory(postgres.NewDatabaseConnectionString());
        client = factory.CreateClient();
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync() => await factory.DisposeAsync();

    [Fact]
    public async Task Token_is_issued_without_the_group_header()
    {
        var response = await client.PostAsJsonAsync(
            "/auth/token",
            new { clientId = ApiFactory.ClientId, clientSecret = ApiFactory.ClientSecret },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadTokenResponse(response);
        Assert.Equal("Bearer", body.GetProperty("tokenType").GetString());
        Assert.True(body.GetProperty("expiresIn").GetInt32() > 0);
        Assert.False(string.IsNullOrEmpty(body.GetProperty("refreshToken").GetString()));
    }

    [Fact]
    public async Task Issued_jwt_has_no_group_claim()
    {
        var body = await IssueTokenAsync();

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(body.GetProperty("accessToken").GetString());

        Assert.Equal(ApiFactory.ClientId, jwt.Subject);
        Assert.DoesNotContain(jwt.Claims, c => c.Type.Contains("group", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Wrong_secret_is_401()
    {
        var response = await client.PostAsJsonAsync(
            "/auth/token",
            new { clientId = ApiFactory.ClientId, clientSecret = "wrong-secret-value" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Blank_field_is_400()
    {
        var response = await client.PostAsJsonAsync(
            "/auth/token",
            new { clientId = ApiFactory.ClientId, clientSecret = "" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_works_once_and_reuse_is_401()
    {
        var refreshToken = (await IssueTokenAsync()).GetProperty("refreshToken").GetString();
        var ct = TestContext.Current.CancellationToken;

        var first = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken }, ct);
        var second = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken }, ct);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.NotEqual(refreshToken, (await ReadTokenResponse(first)).GetProperty("refreshToken").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
    }

    [Fact]
    public async Task Concurrent_refreshes_with_the_same_token_succeed_exactly_once()
    {
        var refreshToken = (await IssueTokenAsync()).GetProperty("refreshToken").GetString();
        var ct = TestContext.Current.CancellationToken;

        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(
            _ => client.PostAsJsonAsync("/auth/refresh", new { refreshToken }, ct)));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.All(
            responses.Where(r => r.StatusCode != HttpStatusCode.OK),
            r => Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode));
    }

    [Fact]
    public async Task Secrets_never_appear_in_error_responses_or_logs()
    {
        const string sentSecret = "never-echo-this-secret";
        const string sentRefresh = "never-echo-this-refresh";
        var ct = TestContext.Current.CancellationToken;

        var badSecret = await client.PostAsJsonAsync(
            "/auth/token", new { clientId = ApiFactory.ClientId, clientSecret = sentSecret }, ct);
        var tooLong = await client.PostAsJsonAsync(
            "/auth/token", new { clientId = new string('c', 101), clientSecret = sentSecret }, ct);
        var badRefresh = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = sentRefresh }, ct);

        foreach (var response in new[] { badSecret, tooLong, badRefresh })
        {
            var text = await response.Content.ReadAsStringAsync(ct);
            Assert.DoesNotContain(sentSecret, text);
            Assert.DoesNotContain(sentRefresh, text);
        }

        var logs = factory.Logs.GetSnapshot().Select(r => r.Message + r.Exception);
        Assert.DoesNotContain(logs, l => l.Contains(sentSecret) || l.Contains(sentRefresh) || l.Contains(ApiFactory.ClientSecret));
    }

    private async Task<JsonElement> IssueTokenAsync()
    {
        var response = await client.PostAsJsonAsync(
            "/auth/token",
            new { clientId = ApiFactory.ClientId, clientSecret = ApiFactory.ClientSecret },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return await ReadTokenResponse(response);
    }

    private static async Task<JsonElement> ReadTokenResponse(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
}
