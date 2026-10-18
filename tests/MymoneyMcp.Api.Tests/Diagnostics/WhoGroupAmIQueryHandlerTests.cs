using Microsoft.EntityFrameworkCore;
using MymoneyMcp.Api.Tests.Infrastructure;
using MymoneyMcp.Application.Common;
using MymoneyMcp.Application.Diagnostics.WhoGroupAmI;
using MymoneyMcp.Domain.Accounts;
using MymoneyMcp.Infrastructure.Data;

namespace MymoneyMcp.Api.Tests.Diagnostics;

/// <summary>002 US3 / FR-009 / SC-002: the handler on real PostgreSQL, with fixed contexts.</summary>
[Collection(PostgresCollection.Name)]
public sealed class WhoGroupAmIQueryHandlerTests(PostgresFixture postgres) : IAsyncLifetime
{
    private readonly string connectionString = postgres.NewDatabaseConnectionString();

    public async ValueTask InitializeAsync()
    {
        await using var db = CreateContext(new FixedAccountGroup(string.Empty));
        await db.Database.MigrateAsync(TestContext.Current.CancellationToken);

        var now = DateTimeOffset.UtcNow;
        db.Accounts.AddRange(
            Account.Create("A1", "A", now),
            Account.Create("A2", "A", now),
            Account.Create("B1", "B", now),
            Account.Create("B2", "B", now),
            Account.Create("B3", "B", now));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Theory]
    [InlineData("A", 2)]
    [InlineData("B", 3)]
    [InlineData("UNKNOWN", 0)]
    public async Task Counts_only_the_accounts_of_the_current_group(string group, int expected)
    {
        var response = await HandleAsync(new FixedAccountGroup(group), TokenIssuer.Local);

        Assert.Equal(group, response.AccountGroup);
        Assert.Equal(expected, response.VisibleAccounts);
    }

    [Theory]
    [InlineData(AccountGroupSource.Url, "url")]
    [InlineData(AccountGroupSource.Header, "header")]
    [InlineData(AccountGroupSource.Both, "both")]
    public async Task Maps_the_group_source_to_its_contract_value(AccountGroupSource source, string expected)
    {
        var response = await HandleAsync(new FixedAccountGroup("A", source), TokenIssuer.Local);

        Assert.Equal(expected, response.GroupSource);
    }

    [Theory]
    [InlineData(TokenIssuer.Local, "local")]
    [InlineData(TokenIssuer.External, "external")]
    [InlineData(TokenIssuer.None, "none")]
    public async Task Maps_the_issuer_to_its_contract_value(TokenIssuer issuer, string expected)
    {
        var response = await HandleAsync(new FixedAccountGroup("A"), issuer);

        Assert.Equal(expected, response.Issuer);
    }

    private async Task<WhoGroupAmIResponse> HandleAsync(FixedAccountGroup group, TokenIssuer issuer)
    {
        await using var db = CreateContext(group);
        var handler = new WhoGroupAmIQueryHandler(db, group, new FixedCaller(issuer));
        return await handler.Handle(new WhoGroupAmIQuery(), TestContext.Current.CancellationToken);
    }

    private AppDbContext CreateContext(FixedAccountGroup group) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options, group);
}
