namespace MymoneyMcp.Infrastructure.Auth;

/// <summary>Authentication settings (section <c>Auth</c>), research R7–R9.</summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Token checking; may be turned off only outside production (spec FR-014).</summary>
    public bool Enabled { get; set; } = true;

    public List<ClientCredential> Clients { get; set; } = [];

    public ExternalIssuerOptions External { get; set; } = new();
}

public sealed class ClientCredential
{
    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;
}

/// <summary>A trusted external token issuer (spec FR-007). Off unless <see cref="Authority"/> is set.</summary>
public sealed class ExternalIssuerOptions
{
    /// <summary>Base URL used for OIDC discovery of the signing keys.</summary>
    public string? Authority { get; set; }

    public string? Audience { get; set; }

    /// <summary>The <c>iss</c> value of external tokens; defaults to <see cref="Authority"/>.</summary>
    public string? Issuer { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Authority);

    public string? EffectiveIssuer => string.IsNullOrWhiteSpace(Issuer) ? Authority : Issuer;
}
