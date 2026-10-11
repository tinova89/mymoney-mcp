using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace MymoneyMcp.Api.Tests.Infrastructure;

/// <summary>
/// Runs the real API host against a PostgreSQL database in <see cref="PostgresFixture"/>.
/// Settings are applied with <c>UseSetting</c> so they are visible while <c>Program</c> builds.
/// </summary>
public sealed class ApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    public const string SigningKey = "test-signing-key-with-at-least-32-bytes!!";
    public const string ClientId = "test-client";
    public const string ClientSecret = "test-client-secret";

    public string Environment { get; init; } = "Development";

    /// <summary>Extra settings; a <c>null</c> value removes a default.</summary>
    public Dictionary<string, string?> Settings { get; } = [];

    public Action<IServiceCollection>? ConfigureServices { get; init; }

    /// <summary>Every log record written by the host, for assertions on log content.</summary>
    public FakeLogCollector Logs => Services.GetRequiredService<FakeLogCollector>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environment);

        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:mymoneymcp-db"] = connectionString,
            ["Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:DisableRetry"] = "true",
            ["Jwt:SigningKey"] = SigningKey,
            ["Auth:Clients:0:ClientId"] = ClientId,
            ["Auth:Clients:0:ClientSecret"] = ClientSecret,
        };
        foreach (var (key, value) in Settings)
        {
            settings[key] = value;
        }

        foreach (var (key, value) in settings)
        {
            if (value is not null)
            {
                builder.UseSetting(key, value);
            }
        }

        builder.ConfigureServices(services =>
        {
            services.AddLogging(logging => logging.AddFakeLogging());
            ConfigureServices?.Invoke(services);
        });
    }
}
