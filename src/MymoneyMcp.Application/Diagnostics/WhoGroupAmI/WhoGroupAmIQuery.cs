using MediatR;

namespace MymoneyMcp.Application.Diagnostics.WhoGroupAmI;

/// <summary>
/// Which account group this call resolved to (002 US3). No parameters, so no validator
/// (002 research R7).
/// </summary>
public sealed record WhoGroupAmIQuery : IRequest<WhoGroupAmIResponse>;
