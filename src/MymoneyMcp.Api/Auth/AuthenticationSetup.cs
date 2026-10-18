using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;
using MymoneyMcp.Api.Common;
using MymoneyMcp.Infrastructure.Auth;

namespace MymoneyMcp.Api.Auth;

/// <summary>
/// Token validation (research R6–R9): a <c>Local</c> scheme for tokens this system issues, an
/// optional <c>External</c> scheme for a trusted issuer, and a policy scheme that picks one by the
/// token's <c>iss</c>. When token checking is on, every endpoint without explicit
/// <c>AllowAnonymous</c> requires an authenticated caller. With the external issuer configured,
/// the MCP scheme publishes RFC 9728 metadata naming it, and MCP <c>401</c>s point there so
/// claude.ai can sign in (002 research R4).
/// </summary>
public static class AuthenticationSetup
{
    public const string PolicyScheme = "Bearer";
    public const string LocalScheme = "Local";
    public const string ExternalScheme = "External";
    public const string ChallengeScheme = "Challenge";

    private const string McpPath = "/mcp";
    private const string MetadataPathPrefix = "/.well-known/oauth-protected-resource";

    public static IServiceCollection AddTokenAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var auth = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
        var external = auth.External;

        var authentication = services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = PolicyScheme;
                options.DefaultChallengeScheme = PolicyScheme;
            })
            .AddPolicyScheme(PolicyScheme, PolicyScheme, options =>
                options.ForwardDefaultSelector = context => SelectScheme(context, external))
            .AddJwtBearer(LocalScheme, options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Read back by HttpCallerContext to report the issuer (002 research R6).
                    AuthenticationType = LocalScheme,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    IssuerSigningKey = string.IsNullOrEmpty(jwt.SigningKey)
                        ? null
                        : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                };
            });

        if (external.IsConfigured)
        {
            authentication.AddJwtBearer(ExternalScheme, options =>
            {
                options.MapInboundClaims = false;
                options.Authority = external.Authority;
                options.Audience = external.Audience;
                options.TokenValidationParameters.ValidIssuer = external.EffectiveIssuer;
                options.TokenValidationParameters.AuthenticationType = ExternalScheme;
            });

            AddResourceMetadata(services, authentication, external);
        }

        var authorization = services.AddAuthorizationBuilder();
        if (auth.Enabled)
        {
            authorization.SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        }

        return services;
    }

    /// <summary>
    /// Serves <c>/.well-known/oauth-protected-resource/&lt;mcp path&gt;</c> and sends MCP challenges
    /// through the MCP scheme; REST challenges stay plain <c>Bearer</c> (001 behavior).
    /// </summary>
    private static void AddResourceMetadata(
        IServiceCollection services,
        AuthenticationBuilder authentication,
        ExternalIssuerOptions external)
    {
        authentication
            .AddMcp(options =>
            {
                options.ResourceMetadata = Metadata(external, resource: null);

                // A new document per request, so the shared options are never mutated.
                options.Events.OnResourceMetadataRequest = context =>
                {
                    context.ResourceMetadata = Metadata(external, ResourceUrl(context.HttpContext.Request));
                    return Task.CompletedTask;
                };
            })
            .AddPolicyScheme(ChallengeScheme, ChallengeScheme, options =>
                options.ForwardDefaultSelector = context =>
                    context.Request.Path.StartsWithSegments(McpPath, StringComparison.OrdinalIgnoreCase)
                        ? McpAuthenticationDefaults.AuthenticationScheme
                        : PolicyScheme);

        services.Configure<AuthenticationOptions>(options => options.DefaultChallengeScheme = ChallengeScheme);
    }

    private static ProtectedResourceMetadata Metadata(ExternalIssuerOptions external, string? resource) =>
        new()
        {
            Resource = resource,
            AuthorizationServers = [external.Authority!],
            BearerMethodsSupported = ["header"],
        };

    /// <summary>
    /// The MCP URL the metadata describes, from the raw request target so the group segment stays
    /// percent-encoded exactly as the client sent it (claude.ai compares it as a string).
    /// </summary>
    private static string ResourceUrl(HttpRequest request)
    {
        var rawPath = AccountGroupResolver.RawPath(request);
        var mcpPath = rawPath.StartsWith(MetadataPathPrefix, StringComparison.OrdinalIgnoreCase)
            ? rawPath[MetadataPathPrefix.Length..]
            : McpPath;
        return $"{request.Scheme}://{request.Host.ToUriComponent()}{request.PathBase.ToUriComponent()}{mcpPath}";
    }

    private static string SelectScheme(HttpContext context, ExternalIssuerOptions external)
    {
        if (!external.IsConfigured)
        {
            return LocalScheme;
        }

        var header = context.Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return LocalScheme;
        }

        var token = header["Bearer ".Length..].Trim();
        var handler = new JsonWebTokenHandler();
        return handler.CanReadToken(token) && handler.ReadJsonWebToken(token).Issuer == external.EffectiveIssuer
            ? ExternalScheme
            : LocalScheme;
    }
}
