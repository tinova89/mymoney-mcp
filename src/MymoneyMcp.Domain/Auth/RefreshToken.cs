namespace MymoneyMcp.Domain.Auth;

/// <summary>
/// An opaque refresh token, stored only as a hash. Not account-owned: authentication happens before
/// any account group is known. Data rules: specs/001-system-foundation/data-model.md.
/// </summary>
public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Required.</summary>
    public string ClientId { get; private set; } = string.Empty;

    /// <summary>SHA-256 hex of the opaque token. Unique index.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>Required.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>Required.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Set when used or revoked.</summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    public static RefreshToken Create(string clientId, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            ClientId = clientId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt.ToUniversalTime(),
            CreatedAt = createdAt.ToUniversalTime(),
        };

    /// <summary>Valid when <c>RevokedAt is null and ExpiresAt &gt; now</c>.</summary>
    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;

    public void Revoke(DateTimeOffset now) => RevokedAt ??= now.ToUniversalTime();
}
