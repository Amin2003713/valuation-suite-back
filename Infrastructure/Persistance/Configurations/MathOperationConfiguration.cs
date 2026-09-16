using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Common;

namespace Persistence.Configurations;

public class MathOperationConfiguration : IEntityTypeConfiguration<MathOperation>
{
    public void Configure(EntityTypeBuilder<MathOperation> builder)
    {
        builder.HasKey(mo => mo.Type);
        builder.Property(mo => mo.Type).HasConversion<int>();
        builder.Property(mo => mo.Operand).HasMaxLength(200);
        builder.Property(mo => mo.Constant);

        builder.HasOne<MathExpression>()
            .WithMany(me => me.Operations)
            .HasForeignKey("MathExpressionId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
