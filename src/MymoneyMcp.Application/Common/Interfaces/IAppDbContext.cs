using Microsoft.EntityFrameworkCore;
using MymoneyMcp.Domain.Accounts;

namespace MymoneyMcp.Application.Common.Interfaces;

/// <summary>
/// Persistence seen by handlers. Every account-owned set is filtered by
/// <see cref="IAccountGroupContext.Group"/> in one central place (Constitution II).
/// </summary>
public interface IAppDbContext
{
    DbSet<Account> Accounts { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
