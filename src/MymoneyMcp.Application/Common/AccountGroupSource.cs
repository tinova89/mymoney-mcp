namespace MymoneyMcp.Application.Common;

/// <summary>Where the current request's account group came from (002 data-model.md).</summary>
public enum AccountGroupSource
{
    /// <summary>No group: no request, or the group was rejected.</summary>
    None,

    /// <summary>The MCP URL (<c>/mcp/&lt;group&gt;</c>).</summary>
    Url,

    /// <summary>The <c>X-Account-Group</c> header.</summary>
    Header,

    /// <summary>The MCP URL and the header, with the same value.</summary>
    Both,
}
