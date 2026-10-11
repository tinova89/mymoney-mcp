using Microsoft.EntityFrameworkCore;
using MymoneyMcp.Application.Common.Interfaces;
using MymoneyMcp.Domain.Accounts;
using MymoneyMcp.Domain.Auth;

namespace MymoneyMcp.Infrastructure.Data;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IAccountGroupContext accountGroupContext)
    : DbContext(options), IAppDbContext
{
    /// <summary>Name of the query filter that hides other groups' data (research R5).</summary>
    public const string AccountGroupFilter = "AccountGroup";

    public DbSet<Account> Accounts => Set<Account>();

    /// <summary>Not account-owned, so not group-filtered.</summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>
    /// Read by the account-group query filter. EF Core evaluates it per context instance, so each
    /// request sees only its own group.
    /// </summary>
    internal string CurrentGroup => accountGroupContext.Group;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // The single place where account isolation is enforced (Constitution II, spec FR-002).
        modelBuilder.Entity<Account>().HasQueryFilter(AccountGroupFilter, a => a.Group == CurrentGroup);
    }
}
