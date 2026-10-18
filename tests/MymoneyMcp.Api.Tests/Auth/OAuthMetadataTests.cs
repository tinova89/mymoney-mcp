using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MymoneyMcp.Api.Tests.Infrastructure;

namespace MymoneyMcp.Api.Tests.Auth;

/// <summary>
/// 002 US4 / FR-011, FR-012, FR-016, contracts/oauth-metadata.md: RFC 9728 metadata naming the
/// external issuer, and MCP <c>401</c>s pointing to it.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class OAuthMetadataTests(PostgresFixture postgres) : IAsyncLifetime
{
    private readonly TestExternalIssuer external = new();
    private ApiFactory factory = null!;
    private HttpClient client = null!;

    public ValueTask InitializeAsync()
    {
        factory = external.CreateFactory(postgres.NewDatabaseConnectionString());
        client = factory.CreateClient();
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync() => await factory.DisposeAsync();

    [Fact]
    public async Task Mcp_call_without_a_token_points_to_the_metadata()
    {
        var response = await client.SendAsync(
            TestRequests.McpInitialize(token: null, group: null, "/mcp/A"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var challenge = Assert.Single(response.Headers.WwwAuthenticate);
        Assert.Equal("Bearer", challenge.Scheme);
        Assert.Contains(
            "resource_metadata=\"http://localhost/.well-known/oauth-protected-resource/mcp/A\"",
            challenge.Parameter,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/mcp/A")]
    [InlineData("/mcp")]
    [InlineData("/mcp/Fam%C3%ADlia")]
    public async Task Metadata_is_anonymous_group_free_and_names_the_external_issuer(string mcpPath)
    {
        var response = await client.GetAsync(
            new Uri("/.well-known/oauth-protected-resource" + mcpPath, UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var metadata = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("http://localhost" + mcpPath, metadata.GetProperty("resource").GetString());
        Assert.Equal(TestExternalIssuer.Issuer, Assert.Single(metadata.GetProperty("authorization_servers").EnumerateArray()).GetString());
        Assert.Equal("header", Assert.Single(metadata.GetProperty("bearer_methods_supported").EnumerateArray()).GetString());
    }

    [Fact]
    public async Task One_external_token_works_on_every_group_url()
    {
        var token = external.Token();

        var a = await McpTestClient.WhoGroupAmIAsync(client, "/mcp/A", token, group: null);
        var b = await McpTestClient.WhoGroupAmIAsync(client, "/mcp/B", token, group: null);

        Assert.Equal("A", a.GetProperty("accountGroup").GetString());
        Assert.Equal("external", a.GetProperty("issuer").GetString());
        Assert.Equal("B", b.GetProperty("accountGroup").GetString());
    }

    [Fact]
    public async Task Local_token_still_works_on_a_group_url()
    {
        var response = await client.SendAsync(
            TestRequests.McpInitialize(TestRequests.LocalToken(), group: null, "/mcp/A"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Metadata_is_served_when_token_checking_is_off()
    {
        await using var authOff = external.CreateFactory(postgres.NewDatabaseConnectionString());
        authOff.Settings["Auth:Enabled"] = "false";

        var response = await authOff.CreateClient().GetAsync(
            "/.well-known/oauth-protected-resource/mcp/A",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
