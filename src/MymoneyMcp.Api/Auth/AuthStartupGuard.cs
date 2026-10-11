using System.Text;
using MymoneyMcp.Infrastructure.Auth;

namespace MymoneyMcp.Api.Auth;

/// <summary>
/// Startup checks for authentication (spec FR-013, FR-014; research R9). ASP.NET Core treats an
/// unset environment as Production, so these checks fail safe.
/// </summary>
public static class AuthStartupGuard
{
    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        var auth = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

        if (!auth.Enabled)
        {
            if (environment.IsProduction())
            {
                throw new InvalidOperationException(
                    "Auth:Enabled=false is not allowed in Production. Token checking can be turned off only outside production.");
            }

            return;
        }

        var signingKey = configuration[$"{JwtOptions.SectionName}:{nameof(JwtOptions.SigningKey)}"];
        if (string.IsNullOrEmpty(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < JwtOptions.MinimumSigningKeyBytes)
        {
            throw new InvalidOperationException(
                $"Jwt:SigningKey must be set to at least {JwtOptions.MinimumSigningKeyBytes} bytes when token checking is on. Use user-secrets or an environment variable.");
        }
    }

    public static void WarnIfDisabled(IConfiguration configuration, ILogger logger)
    {
        if (!configuration.GetValue($"{AuthOptions.SectionName}:{nameof(AuthOptions.Enabled)}", true))
        {
            logger.LogWarning(
                "Token checking is DISABLED (Auth:Enabled=false). Every caller is accepted without a token; the X-Account-Group header is still required.");
        }
    }
}
