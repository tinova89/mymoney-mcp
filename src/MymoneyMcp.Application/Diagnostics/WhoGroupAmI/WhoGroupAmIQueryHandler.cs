using MediatR;
using Microsoft.EntityFrameworkCore;
using MymoneyMcp.Application.Common;
using MymoneyMcp.Application.Common.Interfaces;

namespace MymoneyMcp.Application.Diagnostics.WhoGroupAmI;

/// <summary>
/// Reports the resolved account group, where it came from, the token issuer, and how many accounts
/// the group can see. The count relies on the central group filter (BR-03); it never filters by
/// group itself and never touches the token.
/// </summary>
public sealed class WhoGroupAmIQueryHandler(IAppDbContext db, IAccountGroupContext group, ICallerContext caller)
    : IRequestHandler<WhoGroupAmIQuery, WhoGroupAmIResponse>
{
    public async Task<WhoGroupAmIResponse> Handle(WhoGroupAmIQuery request, CancellationToken cancellationToken) =>
        new(
            group.Group,
            ToContractValue(group.Source),
            ToContractValue(caller.Issuer),
            await db.Accounts.CountAsync(cancellationToken));

    private static string ToContractValue(AccountGroupSource source) => source switch
    {
        AccountGroupSource.Url => "url",
        AccountGroupSource.Header => "header",
        AccountGroupSource.Both => "both",
        _ => "none",
    };

    private static string ToContractValue(TokenIssuer issuer) => issuer switch
    {
        TokenIssuer.Local => "local",
        TokenIssuer.External => "external",
        _ => "none",
    };
}
