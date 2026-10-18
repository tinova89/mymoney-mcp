using System.Net;
using MymoneyMcp.Api.Tests.Infrastructure;

namespace MymoneyMcp.Api.Tests.Auth;

/// <summary>002 spec edge case: without an external issuer, no metadata is published and 401s stay as in 001.</summary>
[Collection(PostgresCollection.Name)]
public sealed class NoExternalIssuerTests(PostgresFixture postgres) : IAsyncLifetime
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
    public async Task Metadata_is_not_published()
    {
        var response = await client.GetAsync(
            "/.well-known/oauth-protected-resource/mcp/A",
            TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Mcp_401_has_no_resource_metadata()
    {
        var response = await client.SendAsync(
            TestRequests.McpInitialize(token: null, group: null, "/mcp/A"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain(
            response.Headers.WwwAuthenticate,
            challenge => challenge.Parameter?.Contains("resource_metadata", StringComparison.Ordinal) == true);
    }
}
