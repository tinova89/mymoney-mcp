using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MymoneyMcp.Infrastructure.Data;

public static class DatabaseMigrator
{
    /// <summary>
    /// Applies pending migrations at startup and fails fast (research R10): if the database is
    /// unreachable or a migration fails, the error is logged and rethrown so the host stops.
    /// PostgreSQL rolls a failed migration back, leaving the schema on the previous version.
    /// </summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseMigrator));

        try
        {
            await db.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database migration failed at startup; stopping the host.");
        }
    }
}
