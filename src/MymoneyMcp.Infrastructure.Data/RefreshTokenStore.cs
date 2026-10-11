using Microsoft.EntityFrameworkCore;
using MymoneyMcp.Application.Common.Interfaces;
using MymoneyMcp.Domain.Auth;

namespace MymoneyMcp.Infrastructure.Data;

internal sealed class RefreshTokenStore(AppDbContext db) : IRefreshTokenStore
{
    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        db.RefreshTokens.AsNoTracking().SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public Task<bool> RotateAsync(
        Guid usedTokenId,
        DateTimeOffset now,
        RefreshToken replacement,
        CancellationToken cancellationToken)
    {
        // The conditional update is the guard: under concurrent calls only one transaction finds the
        // row still unrevoked; the others update 0 rows and fail (PostgreSQL row lock, READ COMMITTED).
        var strategy = db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async ct =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            var revoked = await db.RefreshTokens
                .Where(t => t.Id == usedTokenId && t.RevokedAt == null && t.ExpiresAt > now)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);

            if (revoked == 0)
            {
                await transaction.RollbackAsync(ct);
                return false;
            }

            db.RefreshTokens.Add(replacement);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return true;
        }, cancellationToken);
    }
}
