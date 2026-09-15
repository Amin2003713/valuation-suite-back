using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Common;

namespace Infrastructure.Persistence.Configurations;

public class MathExpressionConfiguration : IEntityTypeConfiguration<MathExpression>
{
    public void Configure(EntityTypeBuilder<MathExpression> builder)
    {
        builder.HasKey(me => me.Expression);
        builder.Property(me => me.Expression).IsRequired().HasMaxLength(1000);
        builder.Property(me => me.PostfixNotation).HasMaxLength(1000);

        builder.HasMany(me => me.Variables).WithOne().HasForeignKey("MathExpressionId").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(me => me.Operations).WithOne().HasForeignKey("MathExpressionId").OnDelete(DeleteBehavior.Cascade);
    }
}
