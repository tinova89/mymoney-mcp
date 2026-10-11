using MediatR;
using MymoneyMcp.Application.Auth.IssueToken;
using MymoneyMcp.Application.Auth.RefreshToken;

namespace MymoneyMcp.Api.Auth;

/// <summary>
/// <c>/auth/token</c> and <c>/auth/refresh</c> (contracts/auth-api.md). Thin: build the command and
/// send it (Constitution III). Anonymous and exempt from <c>X-Account-Group</c>.
/// </summary>
public static class AuthEndpoints
{
    public const string TokenPath = "/auth/token";
    public const string RefreshPath = "/auth/refresh";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(TokenPath, async (TokenRequest body, IMediator mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new IssueTokenCommand(body.ClientId ?? string.Empty, body.ClientSecret ?? string.Empty), ct)))
            .AllowAnonymous();

        app.MapPost(RefreshPath, async (RefreshRequest body, IMediator mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new RefreshTokenCommand(body.RefreshToken ?? string.Empty), ct)))
            .AllowAnonymous();

        return app;
    }

    public sealed record TokenRequest(string? ClientId, string? ClientSecret)
    {
        public override string ToString() => $"{nameof(TokenRequest)} {{ ClientId = {ClientId} }}";
    }

    public sealed record RefreshRequest(string? RefreshToken)
    {
        public override string ToString() => nameof(RefreshRequest);
    }
}
