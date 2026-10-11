using MymoneyMcp.Application.Common.Interfaces;

namespace MymoneyMcp.Api.Common;

/// <summary>
/// Reads the account group from the <c>X-Account-Group</c> header, trimmed. Matching is exact and
/// case-sensitive (<c>HOME</c> ≠ <c>home</c>). Empty when there is no request or no header.
/// </summary>
public sealed class HeaderAccountGroupContext(IHttpContextAccessor httpContextAccessor) : IAccountGroupContext
{
    public const string HeaderName = "X-Account-Group";

    public string Group =>
        httpContextAccessor.HttpContext?.Request.Headers[HeaderName].ToString().Trim() ?? string.Empty;
}
