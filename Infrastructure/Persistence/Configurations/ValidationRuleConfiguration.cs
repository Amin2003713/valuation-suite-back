using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Common;

namespace Infrastructure.Persistence.Configurations;

public class ValidationRuleConfiguration : IEntityTypeConfiguration<ValidationRule>
{
    public void Configure(EntityTypeBuilder<ValidationRule> builder)
    {
        builder.HasKey(v => v.Field);
        builder.Property(v => v.Field).IsRequired().HasMaxLength(200);
        builder.Property(v => v.Type).HasConversion<int>().HasDefaultValue(ValidationType.Required);
        builder.Property(v => v.MinValue);
        builder.Property(v => v.MaxValue);
        builder.Property(v => v.MinLength);
        builder.Property(v => v.MaxLength);
        builder.Property(v => v.Pattern).HasMaxLength(500);
        builder.Property(v => v.ErrorMessage).HasMaxLength(500);

        builder.HasIndex(v => new { v.Field, v.Type }).IsUnique();
    }
}
