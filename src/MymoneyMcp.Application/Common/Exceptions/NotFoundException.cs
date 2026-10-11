namespace MymoneyMcp.Application.Common.Exceptions;

/// <summary>
/// The resource does not exist or belongs to another account group. Both cases use this same
/// exception so callers cannot tell them apart (spec FR-003).
/// </summary>
public sealed class NotFoundException(string resource) : Exception($"{resource} was not found.");
