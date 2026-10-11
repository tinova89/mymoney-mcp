using System.Net;
using Microsoft.Extensions.Logging;
using MymoneyMcp.Api.Tests.Infrastructure;

namespace MymoneyMcp.Api.Tests.Auth;

/// <summary>Spec US2 scenario 5, FR-013, FR-014. The "header still required" case is in RequireAccountGroupTests.</summary>
[Collection(PostgresCollection.Name)]
public sealed class AuthDisabledTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Disabled_outside_production_accepts_calls_without_a_token_and_warns()
    {
        await using var factory = new ApiFactory(postgres.NewDatabaseConnectionString());
        factory.Settings["Auth:Enabled"] = "false";
        var client = factory.CreateClient();

        var response = await client.SendAsync(
            TestRequests.McpInitialize(token: null, TestRequests.Group),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(
            factory.Logs.GetSnapshot(),
            r => r.Level == LogLevel.Warning && r.Message.Contains("Token checking is DISABLED"));
    }

    [Fact]
    public async Task Disabled_in_production_stops_startup()
    {
        await using var factory = new ApiFactory(postgres.NewDatabaseConnectionString()) { Environment = "Production" };
        factory.Settings["Auth:Enabled"] = "false";

        var error = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("Production", Flatten(error));
    }

    // "" (not null) so the test overrides any key from the developer's user-secrets.
    [Theory]
    [InlineData("")]
    [InlineData("too-short")]
    public async Task Missing_or_short_signing_key_stops_startup_when_enabled(string signingKey)
    {
        await using var factory = new ApiFactory(postgres.NewDatabaseConnectionString());
        factory.Settings["Jwt:SigningKey"] = signingKey;

        var error = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("Jwt:SigningKey", Flatten(error));
    }

    private static string Flatten(Exception error) =>
        string.Join(" | ", Enumerable.Repeat(error, 1).Concat(InnerExceptions(error)).Select(e => e.Message));

    private static IEnumerable<Exception> InnerExceptions(Exception error)
    {
        for (var inner = error.InnerException; inner is not null; inner = inner.InnerException)
        {
            yield return inner;
        }
    }
}
