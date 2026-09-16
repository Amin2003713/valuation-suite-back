using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Common;

namespace Persistence.Configurations;

public class CalculationConfigConfiguration : IEntityTypeConfiguration<CalculationConfig>
{
    public void Configure(EntityTypeBuilder<CalculationConfig> builder)
    {
        builder.HasKey(cc => cc.OutputKey);
        builder.Property(cc => cc.Expression).IsRequired().HasMaxLength(1000);
        builder.Property(cc => cc.Formula).HasMaxLength(1000);
        builder.Property(cc => cc.OutputKey).IsRequired().HasMaxLength(200);
        builder.Property(cc => cc.ResultLabel).HasMaxLength(200);
        builder.Ignore(cc => cc.Weights);
    }
}
