namespace MymoneyMcp.Application.Common;

/// <summary>Which issuer's token authenticated the current request (002 data-model.md).</summary>
public enum TokenIssuer
{
    /// <summary>No token: token checking is off.</summary>
    None,

    /// <summary>This system's own issuer.</summary>
    Local,

    /// <summary>The trusted external issuer.</summary>
    External,
}
