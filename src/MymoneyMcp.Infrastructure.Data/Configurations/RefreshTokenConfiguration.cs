using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MymoneyMcp.Domain.Auth;

namespace MymoneyMcp.Infrastructure.Data.Configurations;

/// <summary>No account-group filter: refresh tokens are not account-owned (data-model.md).</summary>
internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(t => t.ClientId).HasColumnName("client_id").HasColumnType("varchar(100)").IsRequired();
        builder.Property(t => t.TokenHash).HasColumnName("token_hash").HasColumnType("varchar(64)").IsRequired();
        builder.Property(t => t.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(t => t.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamptz");

        builder.HasIndex(t => t.TokenHash).IsUnique().HasDatabaseName("ux_refresh_tokens_token_hash");
    }
}
