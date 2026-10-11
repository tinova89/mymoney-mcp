using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace MymoneyMcp.Api.Tests.Infrastructure;

/// <summary>Builds tokens and raw requests used across the integration tests.</summary>
public static class TestRequests
{
    public const string Group = "HOME";

    public static string LocalToken(DateTime? expires = null) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "mymoney-mcp",
            Audience = "mymoney-mcp",
            Subject = new ClaimsIdentity([new Claim("sub", ApiFactory.ClientId)]),
            IssuedAt = (expires ?? DateTime.UtcNow.AddHours(1)).AddHours(-2),
            NotBefore = (expires ?? DateTime.UtcNow.AddHours(1)).AddHours(-2),
            Expires = expires ?? DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiFactory.SigningKey)),
                SecurityAlgorithms.HmacSha256),
        });

    /// <summary>A minimal MCP <c>initialize</c> call to <c>/mcp</c>.</summary>
    public static HttpRequestMessage McpInitialize(string? token, string? group)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = JsonContent.Create(new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "initialize",
                @params = new
                {
                    protocolVersion = "2025-06-18",
                    capabilities = new { },
                    clientInfo = new { name = "tests", version = "1.0" },
                },
            }),
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (group is not null)
        {
            request.Headers.Add("X-Account-Group", group);
        }

        return request;
    }
}
