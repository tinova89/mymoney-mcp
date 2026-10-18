using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace MymoneyMcp.Api.Tests.Infrastructure;

/// <summary>Connects the MCP SDK client to the test host at any MCP path (still percent-encoded).</summary>
public static class McpTestClient
{
    public static Task<McpClient> ConnectAsync(HttpClient http, string path, string? token, string? group)
    {
        var headers = new Dictionary<string, string>();
        if (token is not null)
        {
            headers["Authorization"] = $"Bearer {token}";
        }

        if (group is not null)
        {
            headers["X-Account-Group"] = group;
        }

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(http.BaseAddress!, path),
                TransportMode = HttpTransportMode.StreamableHttp,
                AdditionalHeaders = headers,
            },
            http,
            ownsHttpClient: false);

        return McpClient.CreateAsync(transport, cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>Calls <c>whogroupami</c> and returns its JSON result.</summary>
    public static async Task<JsonElement> WhoGroupAmIAsync(HttpClient http, string path, string? token, string? group)
    {
        await using var client = await ConnectAsync(http, path, token, group);
        var result = await client.CallToolAsync("whogroupami", cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotEqual(true, result.IsError);
        var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content));
        return JsonDocument.Parse(text.Text).RootElement.Clone();
    }
}
