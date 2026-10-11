using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MymoneyMcp.Application.Common.Interfaces;

namespace MymoneyMcp.Infrastructure.Data;

public static class DependencyInjection
{
    public const string ConnectionName = "mymoneymcp-db";

    /// <summary>
    /// Registers <see cref="AppDbContext"/> against the Aspire-provided PostgreSQL connection.
    /// The context is not pooled because it depends on the scoped <see cref="IAccountGroupContext"/>;
    /// the Aspire enrichment adds retries, telemetry and the database health check.
    /// </summary>
    public static IHostApplicationBuilder AddDataInfrastructure(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString(ConnectionName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionName}' is missing. Run through the AppHost or set ConnectionStrings:{ConnectionName}.");

        builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        builder.EnrichNpgsqlDbContext<AppDbContext>();

        builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        builder.Services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();

        return builder;
    }
}
