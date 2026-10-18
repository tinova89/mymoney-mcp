using MymoneyMcp.Application.Common;
using MymoneyMcp.Application.Common.Interfaces;

namespace MymoneyMcp.Api.Common;

/// <summary>
/// The account group of the current request, from the MCP URL or the <c>X-Account-Group</c> header
/// (<see cref="AccountGroupResolver"/>). Empty when there is no request or the group was rejected.
/// Nothing is kept between requests (002 FR-007).
/// </summary>
public sealed class RequestAccountGroupContext(IHttpContextAccessor httpContextAccessor) : IAccountGroupContext
{
    public const string HeaderName = "X-Account-Group";

    public string Group => Resolution?.Group ?? string.Empty;

    public AccountGroupSource Source => Resolution?.Source ?? AccountGroupSource.None;

    private AccountGroupResolution? Resolution =>
        httpContextAccessor.HttpContext is { } context ? AccountGroupResolver.GetOrResolve(context) : null;
}
