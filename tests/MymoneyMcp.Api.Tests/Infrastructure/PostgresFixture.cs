using Npgsql;
using Testcontainers.PostgreSql;

namespace MymoneyMcp.Api.Tests.Infrastructure;

/// <summary>
/// One real PostgreSQL container per test collection (Constitution V). Each test gets its own
/// database inside it, so tests never see each other's rows.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public async ValueTask InitializeAsync() => await container.StartAsync();

    public async ValueTask DisposeAsync() => await container.DisposeAsync();

    /// <summary>
    /// Connection string to a fresh, not-yet-created database; the API creates it on startup migration.
    /// </summary>
    public string NewDatabaseConnectionString() =>
        new NpgsqlConnectionStringBuilder(container.GetConnectionString())
        {
            Database = $"test_{Guid.NewGuid():N}",
        }.ConnectionString;

    public Task StopAsync() => container.StopAsync();

    public Task StartAsync() => container.StartAsync();
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
