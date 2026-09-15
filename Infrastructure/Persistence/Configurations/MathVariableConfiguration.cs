using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Common;

namespace Infrastructure.Persistence.Configurations;

public class MathVariableConfiguration : IEntityTypeConfiguration<MathVariable>
{
    public void Configure(EntityTypeBuilder<MathVariable> builder)
    {
        builder.HasKey(mv => mv.Name);
        builder.Property(mv => mv.Name).IsRequired().HasMaxLength(200);
        builder.Property(mv => mv.Label).IsRequired().HasMaxLength(200);
        builder.Property(mv => mv.MinValue);
        builder.Property(mv => mv.MaxValue);
        builder.Property(mv => mv.DefaultValue);
        builder.Property(mv => mv.IsRequired).HasDefaultValue(true);

        builder.HasOne<MathExpression>()
            .WithMany(me => me.Variables)
            .HasForeignKey("MathExpressionId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
