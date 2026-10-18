using Microsoft.AspNetCore.Http.Features;
using MymoneyMcp.Application.Common;

namespace MymoneyMcp.Api.Common;

public enum AccountGroupError
{
    None,
    Missing,
    BlankUrlGroup,
    Mismatch,
}

/// <summary>The account group of one request; <see cref="Group"/> is empty when <see cref="Error"/> is set.</summary>
public sealed record AccountGroupResolution(string Group, AccountGroupSource Source, AccountGroupError Error)
{
    public static AccountGroupResolution Rejected(AccountGroupError error) => new(string.Empty, AccountGroupSource.None, error);
}

/// <summary>
/// Resolves the account group from the MCP URL (<c>/mcp/&lt;group&gt;</c>) and the
/// <c>X-Account-Group</c> header (002 research R1, data-model.md). The URL segment is read from the
/// raw request target and percent-decoded once, so <c>%2F</c> becomes <c>/</c> and <c>%252F</c>
/// becomes <c>%2F</c>. Both values are trimmed and compared exactly (case-sensitive).
/// </summary>
public static class AccountGroupResolver
{
    private const string McpPrefix = "/mcp/";
    private static readonly object ItemsKey = new();

    /// <summary>Resolves once per request; the result lives in <see cref="HttpContext.Items"/> only.</summary>
    public static AccountGroupResolution GetOrResolve(HttpContext context)
    {
        if (context.Items.TryGetValue(ItemsKey, out var cached) && cached is AccountGroupResolution resolution)
        {
            return resolution;
        }

        resolution = Resolve(context.Request);
        context.Items[ItemsKey] = resolution;
        return resolution;
    }

    public static AccountGroupResolution Resolve(HttpRequest request) =>
        Resolve(RawTarget(request), request.Headers[RequestAccountGroupContext.HeaderName].ToString());

    public static AccountGroupResolution Resolve(string rawTarget, string? header)
    {
        var headerGroup = header?.Trim() ?? string.Empty;
        var urlSegment = UrlSegment(rawTarget);

        if (urlSegment is null)
        {
            return headerGroup.Length > 0
                ? new AccountGroupResolution(headerGroup, AccountGroupSource.Header, AccountGroupError.None)
                : AccountGroupResolution.Rejected(AccountGroupError.Missing);
        }

        var urlGroup = Uri.UnescapeDataString(urlSegment).Trim();
        if (urlGroup.Length == 0)
        {
            return AccountGroupResolution.Rejected(AccountGroupError.BlankUrlGroup);
        }

        if (headerGroup.Length == 0)
        {
            return new AccountGroupResolution(urlGroup, AccountGroupSource.Url, AccountGroupError.None);
        }

        return string.Equals(urlGroup, headerGroup, StringComparison.Ordinal)
            ? new AccountGroupResolution(urlGroup, AccountGroupSource.Both, AccountGroupError.None)
            : AccountGroupResolution.Rejected(AccountGroupError.Mismatch);
    }

    /// <summary>
    /// The still-encoded group segment of <c>/mcp/&lt;group&gt;</c>, or <c>null</c> when the path is
    /// not exactly one segment under <c>/mcp</c> (<c>/mcp</c>, <c>/mcp/</c>, REST routes, <c>/mcp/A/B</c>).
    /// </summary>
    public static string? UrlSegment(string rawTarget)
    {
        var path = StripQuery(rawTarget);
        if (!path.StartsWith(McpPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var segment = path[McpPrefix.Length..];
        if (segment.EndsWith('/'))
        {
            segment = segment[..^1];
        }

        return segment.Length == 0 || segment.Contains('/') ? null : segment;
    }

    /// <summary>The request target as the client sent it, still percent-encoded, without the query.</summary>
    public static string RawPath(HttpRequest request) => StripQuery(RawTarget(request));

    private static string RawTarget(HttpRequest request)
    {
        var rawTarget = request.HttpContext.Features.Get<IHttpRequestFeature>()?.RawTarget;
        return string.IsNullOrEmpty(rawTarget) ? (request.PathBase + request.Path).ToUriComponent() : rawTarget;
    }

    private static string StripQuery(string target)
    {
        var query = target.IndexOf('?');
        return query < 0 ? target : target[..query];
    }
}
