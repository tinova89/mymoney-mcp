using Microsoft.AspNetCore.Mvc;
using MymoneyMcp.Api.Auth;

namespace MymoneyMcp.Api.Common;

/// <summary>
/// Rejects every MCP and REST call without a usable account group with <c>400</c>, before any
/// handler runs (001 FR-004, 002 FR-004/FR-006, Constitution VII). The group comes from the
/// <c>X-Account-Group</c> header or the MCP URL (<see cref="AccountGroupResolver"/>). Runs after
/// authorization, so an anonymous caller gets <c>401</c> first (001 research R6). Problem details
/// never echo the header or URL value.
/// </summary>
public sealed class RequireAccountGroupMiddleware(RequestDelegate next)
{
    public const string MissingHeaderTitle = "Missing X-Account-Group header";
    public const string BlankUrlGroupTitle = "Blank account group in MCP URL";
    public const string MismatchTitle = "Account group mismatch";

    // "/.well-known": public OAuth metadata clients read before they can sign in (002 FR-011).
    private static readonly string[] ExemptPrefixes = ["/health", "/alive", "/openapi", "/.well-known"];
    private static readonly string[] ExemptRoutes = [AuthEndpoints.TokenPath, AuthEndpoints.RefreshPath];

    public async Task InvokeAsync(HttpContext context, IProblemDetailsService problemDetails)
    {
        var error = IsExempt(context.Request.Path)
            ? AccountGroupError.None
            : AccountGroupResolver.GetOrResolve(context).Error;

        if (error == AccountGroupError.None)
        {
            await next(context);
            return;
        }

        var (title, detail) = error switch
        {
            AccountGroupError.BlankUrlGroup => (BlankUrlGroupTitle, "The account group in the MCP URL must not be blank."),
            AccountGroupError.Mismatch => (MismatchTitle, $"The MCP URL and the {RequestAccountGroupContext.HeaderName} header name different account groups."),
            _ => (MissingHeaderTitle, $"Every call must carry the {RequestAccountGroupContext.HeaderName} header."),
        };

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = title,
                Detail = detail,
            },
        });
    }

    private static bool IsExempt(PathString path) =>
        ExemptPrefixes.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
        || ExemptRoutes.Any(route => path.Equals(route, StringComparison.OrdinalIgnoreCase));
}
