using Microsoft.AspNetCore.Mvc;
using MymoneyMcp.Api.Auth;

namespace MymoneyMcp.Api.Common;

/// <summary>
/// Rejects every MCP and REST call without a non-blank <c>X-Account-Group</c> header with
/// <c>400</c>, before any handler runs (spec FR-004, Constitution VII). Runs after authorization,
/// so an anonymous caller gets <c>401</c> first (research R6).
/// </summary>
public sealed class RequireAccountGroupMiddleware(RequestDelegate next)
{
    public const string MissingHeaderTitle = "Missing X-Account-Group header";

    private static readonly string[] ExemptPrefixes = ["/health", "/alive", "/openapi"];
    private static readonly string[] ExemptRoutes = [AuthEndpoints.TokenPath, AuthEndpoints.RefreshPath];

    public async Task InvokeAsync(HttpContext context, IProblemDetailsService problemDetails)
    {
        if (IsExempt(context.Request.Path)
            || !string.IsNullOrWhiteSpace(context.Request.Headers[HeaderAccountGroupContext.HeaderName]))
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = MissingHeaderTitle,
                Detail = $"Every call must carry the {HeaderAccountGroupContext.HeaderName} header.",
            },
        });
    }

    private static bool IsExempt(PathString path) =>
        ExemptPrefixes.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
        || ExemptRoutes.Any(route => path.Equals(route, StringComparison.OrdinalIgnoreCase));
}
