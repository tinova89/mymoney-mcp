using System.Net;
using MymoneyMcp.Api.Tests.Infrastructure;
using Npgsql;
using Testcontainers.PostgreSql;

namespace MymoneyMcp.Api.Tests.Health;

/// <summary>Spec FR-012 and the "database lost after startup" edge case; research R10–R11.</summary>
[Collection(PostgresCollection.Name)]
public sealed class HealthTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Health_is_healthy_in_production_too()
    {
        await using var factory = new ApiFactory(postgres.NewDatabaseConnectionString()) { Environment = "Production" };

        var response = await factory.CreateClient().GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Database_lost_after_startup_makes_health_unhealthy_while_the_api_keeps_running()
    {
        var ct = TestContext.Current.CancellationToken;

        // A dedicated container, so stopping it never affects the shared one.
        await using var database = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await database.StartAsync(ct);
        await using var factory = new ApiFactory(database.GetConnectionString());
        var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health", ct)).StatusCode);

        await database.StopAsync(ct);
        NpgsqlConnection.ClearAllPools();

        var health = await client.GetAsync("/health", ct);
        var alive = await client.GetAsync("/alive", ct);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, health.StatusCode);
        Assert.Equal("Unhealthy", await health.Content.ReadAsStringAsync(ct));
        Assert.Equal(HttpStatusCode.OK, alive.StatusCode);
    }

    [Fact]
    public async Task Database_unreachable_at_startup_stops_the_host()
    {
        await using var factory = new ApiFactory("Host=127.0.0.1;Port=1;Database=unreachable;Username=x;Password=x;Timeout=3");

        Assert.ThrowsAny<Exception>(() => factory.CreateClient());
    }
}
