using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using MymoneyMcp.Application.Common.Interfaces;

namespace MymoneyMcp.Infrastructure.Auth;

/// <summary>Reads clients from <c>Auth:Clients</c>; secrets are compared in constant time.</summary>
internal sealed class ClientCredentialStore(IOptionsMonitor<AuthOptions> options) : IClientCredentialStore
{
    public bool IsValid(string clientId, string clientSecret)
    {
        var candidate = SHA256.HashData(Encoding.UTF8.GetBytes(clientSecret));
        var valid = false;

        foreach (var client in options.CurrentValue.Clients)
        {
            var expected = SHA256.HashData(Encoding.UTF8.GetBytes(client.ClientSecret));
            var secretMatches = CryptographicOperations.FixedTimeEquals(candidate, expected);
            valid |= secretMatches && !string.IsNullOrEmpty(client.ClientSecret) && client.ClientId == clientId;
        }

        return valid;
    }
}
