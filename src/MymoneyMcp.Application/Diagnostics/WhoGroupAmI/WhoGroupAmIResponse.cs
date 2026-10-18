namespace MymoneyMcp.Application.Diagnostics.WhoGroupAmI;

/// <summary>
/// Response of the <c>whogroupami</c> tool (002 contracts/mcp-endpoint.md). <see cref="GroupSource"/>
/// is <c>url</c>, <c>header</c> or <c>both</c>; <see cref="Issuer"/> is <c>local</c>, <c>external</c>
/// or <c>none</c>.
/// </summary>
public sealed record WhoGroupAmIResponse(string AccountGroup, string GroupSource, string Issuer, int VisibleAccounts);
