using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MymoneyMcp.Api.Common;
using MymoneyMcp.Api.Tests.Infrastructure;

namespace MymoneyMcp.Api.Tests.AccountGroup;

/// <summary>002 US1 / FR-001, FR-004, FR-006, edge cases: the group in the MCP URL, through the real host.</summary>
[Collection(PostgresCollection.Name)]
public sealed class UrlAccountGroupTests(PostgresFixture postgres) : IAsyncLifetime
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

    [Theory]
    [InlineData("/mcp/HOME")]
    [InlineData("/mcp/a%2Fb")]
    [InlineData("/mcp/Fam%C3%ADlia")]
    public async Task Group_url_without_the_header_is_accepted(string path)
    {
        var response = await SendAsync(TestRequests.LocalToken(), group: null, path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Blank_url_group_is_400_and_does_not_echo_the_value()
    {
        var response = await SendAsync(TestRequests.LocalToken(), group: null, "/mcp/%20%20");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ProblemAsync(response);
        Assert.Equal(RequireAccountGroupMiddleware.BlankUrlGroupTitle, problem.GetProperty("title").GetString());
        Assert.DoesNotContain("%20", problem.GetRawText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Trailing_slash_is_the_plain_url_so_the_header_decides()
    {
        var withHeader = await SendAsync(TestRequests.LocalToken(), TestRequests.Group, "/mcp/");
        var withoutHeader = await SendAsync(TestRequests.LocalToken(), group: null, "/mcp/");

        Assert.Equal(HttpStatusCode.OK, withHeader.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, withoutHeader.StatusCode);
        Assert.Equal(
            RequireAccountGroupMiddleware.MissingHeaderTitle,
            (await ProblemAsync(withoutHeader)).GetProperty("title").GetString());
    }

    [Fact]
    public async Task More_than_one_segment_is_not_an_mcp_route()
    {
        var response = await SendAsync(TestRequests.LocalToken(), TestRequests.Group, "/mcp/A/B");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Token_is_checked_before_the_url_group()
    {
        var response = await SendAsync(token: null, group: null, "/mcp/%20");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private Task<HttpResponseMessage> SendAsync(string? token, string? group, string path) =>
        client.SendAsync(TestRequests.McpInitialize(token, group, path), TestContext.Current.CancellationToken);

    private static async Task<JsonElement> ProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        return await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
    }
}
