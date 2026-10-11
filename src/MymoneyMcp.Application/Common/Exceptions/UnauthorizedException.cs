namespace MymoneyMcp.Application.Common.Exceptions;

/// <summary>
/// Credentials or a refresh token were rejected. The message is the same for every cause so
/// callers cannot probe which part was wrong.
/// </summary>
public sealed class UnauthorizedException() : Exception("Invalid credentials.");
