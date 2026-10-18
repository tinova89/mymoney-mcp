namespace MymoneyMcp.Application.Common.Interfaces;

/// <summary>Who is calling, as far as handlers need to know. Never exposes the token itself.</summary>
public interface ICallerContext
{
    TokenIssuer Issuer { get; }
}
