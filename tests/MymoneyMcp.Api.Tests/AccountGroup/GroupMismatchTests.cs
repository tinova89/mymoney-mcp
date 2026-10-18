using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MymoneyMcp.Api.Common;
using MymoneyMcp.Api.Tests.Infrastructure;

namespace MymoneyMcp.Api.Tests.AccountGroup;

/// <summary>002 US2 / FR-005: a URL group and a different header are rejected before any handler.</summary>
[Collection(PostgresCollection.Name)]
public sealed class GroupMismatchTests(PostgresFixture postgres) : IAsyncLifetime
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
    public async Task Different_url_group_and_header_is_400_and_echoes_neither()
    {
        var response = await SendAsync(TestRequests.LocalToken(), "BRAVO", "/mcp/ALPHA");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(RequireAccountGroupMiddleware.MismatchTitle, problem.GetProperty("title").GetString());
        Assert.DoesNotContain("ALPHA", problem.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain("BRAVO", problem.GetRawText(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ALPHA")]
    [InlineData(" ALPHA ")]
    public async Task Same_url_group_and_header_is_accepted(string header)
    {
        var response = await SendAsync(TestRequests.LocalToken(), header, "/mcp/ALPHA");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Matching_is_case_sensitive()
    {
        var response = await SendAsync(TestRequests.LocalToken(), "alpha", "/mcp/ALPHA");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Token_is_checked_before_the_mismatch()
    {
        var response = await SendAsync(token: null, "BRAVO", "/mcp/ALPHA");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Plain_url_with_the_header_works_as_in_001()
    {
        var response = await SendAsync(TestRequests.LocalToken(), TestRequests.Group, "/mcp");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private Task<HttpResponseMessage> SendAsync(string? token, string? group, string path) =>
        client.SendAsync(TestRequests.McpInitialize(token, group, path), TestContext.Current.CancellationToken);
}
