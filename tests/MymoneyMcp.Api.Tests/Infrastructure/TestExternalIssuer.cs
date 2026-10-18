using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace MymoneyMcp.Api.Tests.Infrastructure;

/// <summary>
/// A trusted external issuer with a fixed key, standing in for OIDC discovery (same setup as 001
/// <c>ProtectedRouteTests</c>).
/// </summary>
public sealed class TestExternalIssuer
{
    public const string Issuer = "https://external.test/";
    public const string Audience = "mymoney-mcp-external";

    private readonly RsaSecurityKey key = new(RSA.Create(2048)) { KeyId = "external-test-key" };

    /// <summary>A host that trusts this issuer.</summary>
    public ApiFactory CreateFactory(string connectionString)
    {
        var factory = new ApiFactory(connectionString)
        {
            ConfigureServices = services => services.PostConfigure<JwtBearerOptions>("External", options =>
            {
                var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                configuration.SigningKeys.Add(key);
                options.Configuration = configuration;
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            }),
        };
        factory.Settings["Auth:External:Authority"] = Issuer;
        factory.Settings["Auth:External:Audience"] = Audience;
        return factory;
    }

    public string Token() =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Subject = new ClaimsIdentity([new Claim("sub", "external-caller")]),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256),
        });
}
