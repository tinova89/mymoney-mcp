using MymoneyMcp.Application.Common;
using MymoneyMcp.Application.Common.Interfaces;

namespace MymoneyMcp.Api.Tests.Infrastructure;

/// <summary>A fixed account group, for building <c>AppDbContext</c> or handlers without a request.</summary>
public sealed class FixedAccountGroup(string group, AccountGroupSource source = AccountGroupSource.Header) : IAccountGroupContext
{
    public string Group => group;

    public AccountGroupSource Source => source;
}

/// <summary>A fixed caller, for handlers that report the token issuer.</summary>
public sealed class FixedCaller(TokenIssuer issuer) : ICallerContext
{
    public TokenIssuer Issuer => issuer;
}
