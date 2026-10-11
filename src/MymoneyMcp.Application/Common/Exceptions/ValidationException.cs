namespace MymoneyMcp.Application.Common.Exceptions;

/// <summary>
/// One or more request fields failed validation. Holds messages only, never the attempted values,
/// so secrets sent in a request can never reach a response (Constitution VIII).
/// </summary>
public sealed class ValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("One or more validation errors occurred.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
