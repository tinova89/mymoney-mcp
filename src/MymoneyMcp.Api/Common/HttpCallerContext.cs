using MymoneyMcp.Api.Auth;
using MymoneyMcp.Application.Common;
using MymoneyMcp.Application.Common.Interfaces;

namespace MymoneyMcp.Api.Common;

/// <summary>
/// The issuer of the current request's token, from the authentication type that each bearer scheme
/// stamps on its identity (002 research R6). Never reads the token itself.
/// </summary>
public sealed class HttpCallerContext(IHttpContextAccessor httpContextAccessor) : ICallerContext
{
    public TokenIssuer Issuer => httpContextAccessor.HttpContext?.User.Identity switch
    {
        { IsAuthenticated: true, AuthenticationType: AuthenticationSetup.LocalScheme } => TokenIssuer.Local,
        { IsAuthenticated: true, AuthenticationType: AuthenticationSetup.ExternalScheme } => TokenIssuer.External,
        _ => TokenIssuer.None,
    };
}
