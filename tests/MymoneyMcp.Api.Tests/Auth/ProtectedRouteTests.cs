using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using MymoneyMcp.Api.Tests.Infrastructure;

namespace MymoneyMcp.Api.Tests.Auth;

/// <summary>Spec US2 scenarios 1 and 4, FR-005 exemptions, FR-007 external issuer.</summary>
[Collection(PostgresCollection.Name)]
public sealed class ProtectedRouteTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string ExternalIssuer = "https://external.test/";
    private const string ExternalAudience = "mymoney-mcp-external";

    private readonly RsaSecurityKey externalKey = new(RSA.Create(2048)) { KeyId = "external-test-key" };
    private ApiFactory factory = null!;
    private HttpClient client = null!;

    public ValueTask InitializeAsync()
    {
        factory = new ApiFactory(postgres.NewDatabaseConnectionString())
        {
            ConfigureServices = services => services.PostConfigure<JwtBearerOptions>("External", options =>
            {
                // Stand-in for OIDC discovery: the external issuer's metadata and key are fixed.
                var configuration = new OpenIdConnectConfiguration { Issuer = ExternalIssuer };
                configuration.SigningKeys.Add(externalKey);
                options.Configuration = configuration;
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            }),
        };
        factory.Settings["Auth:External:Authority"] = ExternalIssuer;
        factory.Settings["Auth:External:Audience"] = ExternalAudience;
        client = factory.CreateClient();
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync() => await factory.DisposeAsync();

    [Fact]
    public async Task No_token_is_401()
    {
        var response = await client.SendAsync(TestRequests.McpInitialize(null, TestRequests.Group), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, h => h.Scheme == "Bearer");
    }

    [Fact]
    public async Task Expired_token_is_401()
    {
        var expired = TestRequests.LocalToken(expires: DateTime.UtcNow.AddMinutes(-10));

        var response = await client.SendAsync(TestRequests.McpInitialize(expired, TestRequests.Group), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Valid_own_token_is_accepted()
    {
        var response = await client.SendAsync(
            TestRequests.McpInitialize(TestRequests.LocalToken(), TestRequests.Group),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Token_from_the_trusted_external_issuer_is_accepted()
    {
        var response = await client.SendAsync(
            TestRequests.McpInitialize(ExternalToken(), TestRequests.Group),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    [InlineData("/openapi/v1.json")]
    public async Task Exempt_routes_do_not_require_a_token(string path)
    {
        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private string ExternalToken() =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = ExternalIssuer,
            Audience = ExternalAudience,
            Subject = new ClaimsIdentity([new Claim("sub", "external-caller")]),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(externalKey, SecurityAlgorithms.RsaSha256),
        });
}
