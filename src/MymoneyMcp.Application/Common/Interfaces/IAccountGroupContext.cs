namespace MymoneyMcp.Application.Common.Interfaces;

/// <summary>
/// The account group of the current request (Constitution II). Empty when no group applies,
/// e.g. at startup or on header-exempt routes.
/// </summary>
public interface IAccountGroupContext
{
    string Group { get; }

    /// <summary>Where <see cref="Group"/> came from; <see cref="AccountGroupSource.None"/> when empty.</summary>
    AccountGroupSource Source { get; }
}
