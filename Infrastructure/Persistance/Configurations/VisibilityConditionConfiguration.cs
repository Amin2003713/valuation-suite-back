using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Common;

namespace Persistence.Configurations;

public class VisibilityConditionConfiguration : IEntityTypeConfiguration<VisibilityCondition>
{
    public void Configure(EntityTypeBuilder<VisibilityCondition> builder)
    {
        builder.HasKey(vc => new { vc.TargetQuestionId });
        builder.Property(vc => vc.TargetQuestionId).IsRequired();
        builder.Property(vc => vc.Type).HasConversion<int>().HasDefaultValue(VisibilityConditionType.Equals);
        builder.Property(vc => vc.Value).HasMaxLength(500);
    }
}
