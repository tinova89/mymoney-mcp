using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MymoneyMcp.Infrastructure.Data;

public static class DatabaseMigrator
{
    /// <summary>
    /// Applies pending migrations at startup and fails fast (research R10): if the database is
    /// unreachable or a migration fails, the host stops. PostgreSQL rolls a failed migration back,
    /// leaving the schema on the previous version.
    /// </summary>
    /// <remarks>
    /// The error is wrapped with context and not logged here, so whoever catches it logs it once
    /// (Sonar S2139). At startup nobody catches it: the runtime prints it and the process exits.
    /// </remarks>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            await db.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                "Database migration failed at startup; the host is stopping. Check that the database is reachable and the migrations are valid.",
                ex);
        }
    }
}
