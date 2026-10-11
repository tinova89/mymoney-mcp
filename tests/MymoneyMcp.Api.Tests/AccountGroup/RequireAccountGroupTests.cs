using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MymoneyMcp.Api.Tests.Infrastructure;

namespace MymoneyMcp.Api.Tests.AccountGroup;

/// <summary>Spec US3 / FR-004, contracts/http-conventions.md, and the header part of FR-014.</summary>
[Collection(PostgresCollection.Name)]
public sealed class RequireAccountGroupTests(PostgresFixture postgres) : IAsyncLifetime
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
    public async Task Missing_header_is_400_with_the_contract_title()
    {
        var response = await client.SendAsync(
            TestRequests.McpInitialize(TestRequests.LocalToken(), group: null),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("Missing X-Account-Group header", problem.GetProperty("title").GetString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Blank_header_is_400(string group)
    {
        var response = await client.SendAsync(
            TestRequests.McpInitialize(TestRequests.LocalToken(), group),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Token_is_checked_before_the_header()
    {
        var response = await client.SendAsync(
            TestRequests.McpInitialize(token: null, group: null),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    [InlineData("/openapi/v1.json")]
    public async Task Infrastructure_routes_do_not_need_the_header(string path)
    {
        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Auth_routes_do_not_need_the_header()
    {
        var response = await client.PostAsJsonAsync(
            "/auth/token",
            new { clientId = ApiFactory.ClientId, clientSecret = ApiFactory.ClientSecret },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Header_is_still_required_when_token_checking_is_off()
    {
        await using var authOff = new ApiFactory(postgres.NewDatabaseConnectionString());
        authOff.Settings["Auth:Enabled"] = "false";

        var response = await authOff.CreateClient().SendAsync(
            TestRequests.McpInitialize(token: null, group: null),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
