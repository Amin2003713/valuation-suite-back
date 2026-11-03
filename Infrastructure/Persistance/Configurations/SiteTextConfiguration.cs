using Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

/// <summary>
///     Table shape for admin-managed site texts. The key index is intentionally
///     NOT unique at the database level: rows are soft-deleted, and a filtered
///     unique index is not portable across the SQL Server and SQLite profiles.
///     Uniqueness among active rows is enforced in the write handler instead.
/// </summary>
public sealed class SiteTextConfiguration : IEntityTypeConfiguration<SiteText>
{
    public void Configure(EntityTypeBuilder<SiteText> builder)
    {
        builder.Property(t => t.TextKey).IsRequired().HasMaxLength(120);
        builder.Property(t => t.GroupKey).IsRequired().HasMaxLength(60);
        builder.Property(t => t.Label).HasMaxLength(200);
        builder.Property(t => t.TextFa).HasMaxLength(2000);
        builder.Property(t => t.TextEn).HasMaxLength(2000);

        builder.HasIndex(t => t.TextKey);
        builder.HasIndex(t => new { t.GroupKey, t.SortOrder });
    }
}
