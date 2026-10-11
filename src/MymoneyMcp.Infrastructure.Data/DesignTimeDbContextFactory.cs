using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MymoneyMcp.Application.Common.Interfaces;

namespace MymoneyMcp.Infrastructure.Data;

/// <summary>
/// Used only by <c>dotnet ef</c>, so adding a migration never starts the API host. No connection
/// is opened when a migration is scaffolded.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=mymoneymcp-db")
            .Options;

        return new AppDbContext(options, new NoAccountGroup());
    }

    private sealed class NoAccountGroup : IAccountGroupContext
    {
        public string Group => string.Empty;
    }
}
