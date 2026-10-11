using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MymoneyMcp.Infrastructure.Auth;

namespace MymoneyMcp.Api.Auth;

/// <summary>
/// Token validation (research R6–R9): a <c>Local</c> scheme for tokens this system issues, an
/// optional <c>External</c> scheme for a trusted issuer, and a policy scheme that picks one by the
/// token's <c>iss</c>. When token checking is on, every endpoint without explicit
/// <c>AllowAnonymous</c> requires an authenticated caller.
/// </summary>
public static class AuthenticationSetup
{
    public const string PolicyScheme = "Bearer";
    public const string LocalScheme = "Local";
    public const string ExternalScheme = "External";

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
            });
        }

        var authorization = services.AddAuthorizationBuilder();
        if (auth.Enabled)
        {
            authorization.SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        }

        return services;
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
