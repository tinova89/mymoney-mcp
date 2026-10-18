using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MymoneyMcp.Api.Tests.Infrastructure;
using MymoneyMcp.Domain.Accounts;
using MymoneyMcp.Infrastructure.Data;

namespace MymoneyMcp.Api.Tests.Mcp;

/// <summary>
/// 002 US3 / FR-007, FR-009, FR-010, contracts/mcp-endpoint.md: <c>whogroupami</c> through the real
/// host, PostgreSQL and the MCP SDK client (Constitution V).
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class WhoAmIToolTests(PostgresFixture postgres) : IAsyncLifetime
{
    private ApiFactory factory = null!;

    public async ValueTask InitializeAsync()
    {
        factory = new ApiFactory(postgres.NewDatabaseConnectionString());
        await SeedAsync(factory);
    }

    public async ValueTask DisposeAsync() => await factory.DisposeAsync();

    [Fact]
    public async Task Url_group_is_decoded_trimmed_and_isolated()
    {
        var result = await WhoGroupAmIAsync(factory, "/mcp/%20A%20", TestRequests.LocalToken(), header: null);

        Assert.Equal("A", result.GetProperty("accountGroup").GetString());
        Assert.Equal("url", result.GetProperty("groupSource").GetString());
        Assert.Equal("local", result.GetProperty("issuer").GetString());
        Assert.Equal(2, result.GetProperty("visibleAccounts").GetInt32());
    }

    [Fact]
    public async Task Header_group_on_the_plain_url()
    {
        var result = await WhoGroupAmIAsync(factory, "/mcp", TestRequests.LocalToken(), header: "B");

        Assert.Equal("B", result.GetProperty("accountGroup").GetString());
        Assert.Equal("header", result.GetProperty("groupSource").GetString());
        Assert.Equal(3, result.GetProperty("visibleAccounts").GetInt32());
    }

    [Fact]
    public async Task Url_group_and_matching_header_is_both()
    {
        var result = await WhoGroupAmIAsync(factory, "/mcp/A", TestRequests.LocalToken(), header: "A");

        Assert.Equal("both", result.GetProperty("groupSource").GetString());
    }

    [Fact]
    public async Task Encoded_slash_is_part_of_the_group_name()
    {
        var result = await WhoGroupAmIAsync(factory, "/mcp/a%2Fb", TestRequests.LocalToken(), header: null);

        Assert.Equal("a/b", result.GetProperty("accountGroup").GetString());
        Assert.Equal(0, result.GetProperty("visibleAccounts").GetInt32());
    }

    [Fact]
    public async Task The_group_is_never_carried_over_to_the_next_call()
    {
        var token = TestRequests.LocalToken();
        using var http = factory.CreateClient();

        var first = await WhoGroupAmIAsync(http, "/mcp/A", token, header: null);
        var second = await WhoGroupAmIAsync(http, "/mcp/B", token, header: null);

        Assert.Equal("A", first.GetProperty("accountGroup").GetString());
        Assert.Equal("B", second.GetProperty("accountGroup").GetString());
        Assert.Equal(3, second.GetProperty("visibleAccounts").GetInt32());
    }

    [Fact]
    public async Task Issuer_is_none_when_token_checking_is_off()
    {
        await using var authOff = new ApiFactory(postgres.NewDatabaseConnectionString());
        authOff.Settings["Auth:Enabled"] = "false";

        var result = await WhoGroupAmIAsync(authOff, "/mcp/A", token: null, header: null);

        Assert.Equal("none", result.GetProperty("issuer").GetString());
    }

    [Fact]
    public async Task Tool_is_listed_as_read_only_and_never_returns_the_token()
    {
        var token = TestRequests.LocalToken();
        using var http = factory.CreateClient();
        await using var client = await McpTestClient.ConnectAsync(http, "/mcp/A", token, group: null);
        var ct = TestContext.Current.CancellationToken;

        var tools = await client.ListToolsAsync(cancellationToken: ct);
        var result = await client.CallToolAsync("whogroupami", cancellationToken: ct);

        var tool = Assert.Single(tools);
        Assert.Equal("whogroupami", tool.Name);
        Assert.True(tool.ProtocolTool.Annotations?.ReadOnlyHint);
        Assert.DoesNotContain(token, JsonSerializer.Serialize(result), StringComparison.Ordinal);
    }

    private static async Task SeedAsync(ApiFactory host)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        db.Accounts.AddRange(
            Account.Create("A1", "A", now),
            Account.Create("A2", "A", now),
            Account.Create("B1", "B", now),
            Account.Create("B2", "B", now),
            Account.Create("B3", "B", now));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<JsonElement> WhoGroupAmIAsync(ApiFactory host, string path, string? token, string? header)
    {
        using var http = host.CreateClient();
        return await WhoGroupAmIAsync(http, path, token, header);
    }

    private static Task<JsonElement> WhoGroupAmIAsync(HttpClient http, string path, string? token, string? header) =>
        McpTestClient.WhoGroupAmIAsync(http, path, token, header);
}
