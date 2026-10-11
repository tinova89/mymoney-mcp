using Microsoft.EntityFrameworkCore;
using MymoneyMcp.Api.Tests.Infrastructure;
using MymoneyMcp.Application.Common.Interfaces;
using MymoneyMcp.Domain.Accounts;
using MymoneyMcp.Infrastructure.Data;

namespace MymoneyMcp.Api.Tests.Accounts;

/// <summary>
/// Spec US1 / FR-002 / FR-003 / SC-001: the central account-group filter, proven on real PostgreSQL.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class AccountGroupIsolationTests(PostgresFixture postgres) : IAsyncLifetime
{
    private readonly string connectionString = postgres.NewDatabaseConnectionString();
    private Account accountA1 = null!;
    private Account accountA2 = null!;
    private Account accountB = null!;

    public async ValueTask InitializeAsync()
    {
        await using var db = CreateContext(group: string.Empty);
        await db.Database.MigrateAsync(TestContext.Current.CancellationToken);

        var now = DateTimeOffset.UtcNow;
        accountA1 = Account.Create("Checking A1", "A", now);
        accountA2 = Account.Create("Savings A2", "A", now);
        accountB = Account.Create("Checking B", "B", now);
        db.Accounts.AddRange(accountA1, accountA2, accountB);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Reading_as_group_A_returns_only_group_A_accounts()
    {
        await using var db = CreateContext("A");

        var ids = await db.Accounts.Select(a => a.Id).ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            new[] { accountA1.Id, accountA2.Id }.Order(),
            ids.Order());
    }

    [Fact]
    public async Task Looking_up_another_groups_account_behaves_like_an_unknown_id()
    {
        await using var db = CreateContext("A");
        var ct = TestContext.Current.CancellationToken;

        var otherGroups = await db.Accounts.FirstOrDefaultAsync(a => a.Id == accountB.Id, ct);
        var unknown = await db.Accounts.FirstOrDefaultAsync(a => a.Id == Guid.NewGuid(), ct);

        Assert.Null(otherGroups);
        Assert.Null(unknown);
    }

    [Fact]
    public async Task A_group_with_no_accounts_reads_empty_without_error()
    {
        await using var db = CreateContext("NO-ACCOUNTS");

        var accounts = await db.Accounts.ToListAsync(TestContext.Current.CancellationToken);

        Assert.Empty(accounts);
    }

    [Fact]
    public async Task Group_matching_is_case_sensitive()
    {
        await using var db = CreateContext("a");

        var accounts = await db.Accounts.ToListAsync(TestContext.Current.CancellationToken);

        Assert.Empty(accounts);
    }

    private AppDbContext CreateContext(string group) =>
        new(
            new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options,
            new FixedAccountGroup(group));

    private sealed class FixedAccountGroup(string group) : IAccountGroupContext
    {
        public string Group => group;
    }
}
