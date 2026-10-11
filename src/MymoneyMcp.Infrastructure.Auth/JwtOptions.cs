namespace MymoneyMcp.Infrastructure.Auth;

/// <summary>
/// Settings for tokens this system issues (section <c>Jwt</c>). <see cref="SigningKey"/> comes from
/// user-secrets or the environment, never from the repository (spec FR-013).
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Minimum signing key size for HMAC-SHA256 (research R7).</summary>
    public const int MinimumSigningKeyBytes = 32;

    public string? SigningKey { get; set; }

    public string Issuer { get; set; } = "mymoney-mcp";

    public string Audience { get; set; } = "mymoney-mcp";

    public int AccessTokenMinutes { get; set; } = 60;

    public int RefreshTokenDays { get; set; } = 14;
}
