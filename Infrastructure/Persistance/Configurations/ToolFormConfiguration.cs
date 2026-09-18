using Domain.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class ToolFormConfiguration : IEntityTypeConfiguration<ToolForm>
{
    public void Configure(EntityTypeBuilder<ToolForm> builder)
    {
        builder.Property(t => t.ToolCode).IsRequired().HasMaxLength(50);
        builder.HasIndex(t => t.ToolCode).IsUnique();
        builder.Property(t => t.Title).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Description).HasMaxLength(1000);
        builder.Property(t => t.Kind).HasConversion<int>();
        builder.Property(t => t.SchemaJson).IsRequired();
        builder.Property(t => t.Route).HasMaxLength(100);
    }
}

public class ToolSubmissionConfiguration : IEntityTypeConfiguration<ToolSubmission>
{
    public void Configure(EntityTypeBuilder<ToolSubmission> builder)
    {
        builder.Property(s => s.ToolCode).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Name).HasMaxLength(200);
        builder.Property(s => s.OverallScore).HasConversion<double?>();

        builder.HasIndex(s => new { s.UserId, s.ToolCode });
        builder.HasIndex(s => s.UserId);

        builder.HasOne<Domain.Users.ApplicationUser>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
