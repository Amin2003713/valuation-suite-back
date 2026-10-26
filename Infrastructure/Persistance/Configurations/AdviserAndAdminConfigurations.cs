using Domain.Adviser;
using Domain.Admin;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class AdviserMessageConfiguration : IEntityTypeConfiguration<AdviserMessage>
{
    public void Configure(EntityTypeBuilder<AdviserMessage> builder)
    {
        builder.Property(m => m.ToolCode).IsRequired().HasMaxLength(50);
        builder.Property(m => m.Text).HasMaxLength(8000);
        builder.Property(m => m.ContextJson);
        builder.HasIndex(m => new { m.UserId, m.ToolCode, m.CreatedAt });
        builder.HasIndex(m => m.UserId);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AdminSectionSeenConfiguration : IEntityTypeConfiguration<AdminSectionSeen>
{
    public void Configure(EntityTypeBuilder<AdminSectionSeen> builder)
    {
        builder.Property(s => s.SectionKey).IsRequired().HasMaxLength(50);
        // One row per (admin user, section) — new acks upsert it.
        builder.HasIndex(s => new { s.UserId, s.SectionKey }).IsUnique();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
