using Domain.Tools;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class UserToolAccessConfiguration : IEntityTypeConfiguration<UserToolAccess>
{
    public void Configure(EntityTypeBuilder<UserToolAccess> builder)
    {
        builder.Property(a => a.ToolCode).IsRequired().HasMaxLength(50);
        builder.HasIndex(a => new { a.UserId, a.ToolCode });

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AccessPackageConfiguration : IEntityTypeConfiguration<AccessPackage>
{
    public void Configure(EntityTypeBuilder<AccessPackage> builder)
    {
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.ToolCodesJson).IsRequired();
    }
}

public class SubmissionNoteConfiguration : IEntityTypeConfiguration<SubmissionNote>
{
    public void Configure(EntityTypeBuilder<SubmissionNote> builder)
    {
        builder.Property(n => n.Text).HasMaxLength(20000);
        builder.Property(n => n.AudioMimeType).HasMaxLength(100);
        builder.HasIndex(n => n.SubmissionId);

        /* Submission FK intentionally removed with the move to per-tool submission
         * tables (one table per tool): a submission id now lives in exactly one of
         * the 23 tables, so a single FK to the legacy ToolSubmissions table can no
         * longer cover it. The SubmissionId column + index stay; the relationship is
         * logical only. The legacy table itself is kept (archive + old rows' FK target
         * is dropped in the same migration). */
    }
}
