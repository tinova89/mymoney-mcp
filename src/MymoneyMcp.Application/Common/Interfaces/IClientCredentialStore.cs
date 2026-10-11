namespace MymoneyMcp.Application.Common.Interfaces;

/// <summary>Client credentials configured on the server (research R7).</summary>
public interface IClientCredentialStore
{
    /// <summary>
    /// True when the pair matches a configured client. Implementations compare in constant time.
    /// </summary>
    bool IsValid(string clientId, string clientSecret);
}
