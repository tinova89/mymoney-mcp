using MediatR;

namespace MymoneyMcp.Application.Auth.IssueToken;

public sealed record IssueTokenCommand(string ClientId, string ClientSecret) : IRequest<TokenResponse>
{
    /// <summary>Never prints the secret (Constitution VIII).</summary>
    public override string ToString() => $"{nameof(IssueTokenCommand)} {{ ClientId = {ClientId} }}";
}
