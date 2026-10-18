using ModelContextProtocol.Client;
using MymoneyMcp.Api.Tests.Infrastructure;

namespace MymoneyMcp.Api.Tests.Mcp;

/// <summary>
/// 001 contracts/mcp-endpoint.md — spec US4 / FR-010, through a real MCP client. 002 adds the
/// diagnostic tool <c>whogroupami</c>; there are still no business tools.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class McpEndpointTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Client_connects_and_the_server_lists_only_the_diagnostic_tool()
    {
        await using var factory = new ApiFactory(postgres.NewDatabaseConnectionString());
        var ct = TestContext.Current.CancellationToken;

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(factory.Server.BaseAddress, "/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp,
                AdditionalHeaders = new Dictionary<string, string>
                {
                    ["Authorization"] = $"Bearer {TestRequests.LocalToken()}",
                    ["X-Account-Group"] = TestRequests.Group,
                },
            },
            factory.CreateClient(),
            ownsHttpClient: true);

        await using var client = await McpClient.CreateAsync(transport, cancellationToken: ct);
        var tools = await client.ListToolsAsync(cancellationToken: ct);

        Assert.Equal("mymoney-mcp", client.ServerInfo.Name);
        Assert.Equal("whogroupami", Assert.Single(tools).Name);
    }
}
