using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MymoneyMcp.Domain.Accounts;

namespace MymoneyMcp.Infrastructure.Data.Configurations;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(a => a.DisplayName).HasColumnName("display_name").HasColumnType("varchar(100)").IsRequired();
        builder.Property(a => a.Group).HasColumnName("group").HasColumnType("varchar(100)").IsRequired();
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(a => a.Group).HasDatabaseName("ix_accounts_group");
    }
}
