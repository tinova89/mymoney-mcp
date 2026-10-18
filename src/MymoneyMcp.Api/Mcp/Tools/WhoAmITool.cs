using System.ComponentModel;
using MediatR;
using ModelContextProtocol.Server;
using MymoneyMcp.Application.Diagnostics.WhoGroupAmI;

namespace MymoneyMcp.Api.Mcp.Tools;

/// <summary>"Who am I" diagnostics (002 US3). Thin: each method only sends a query (Constitution III).</summary>
[McpServerToolType]
public sealed class WhoAmITool
{
    [McpServerTool(Name = "whogroupami", ReadOnly = true)]
    [Description("Shows the account group this call resolved to, where it came from, which issuer accepted the token, and how many accounts the group can see.")]
    public static Task<WhoGroupAmIResponse> WhoGroupAmI(IMediator mediator, CancellationToken cancellationToken) =>
        mediator.Send(new WhoGroupAmIQuery(), cancellationToken);
}
