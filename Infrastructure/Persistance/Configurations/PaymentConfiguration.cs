using Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Gateway).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Authority).HasMaxLength(100);
        builder.Property(p => p.RefId).HasMaxLength(100);
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.ErrorMessage).HasMaxLength(1000);
        builder.Property(p => p.Status).HasConversion<int>().HasDefaultValue(PaymentStatus.Pending);
        builder.Property(p => p.Amount).HasConversion<long>();
        builder.Property(p => p.Months);

        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.Authority);
        builder.HasIndex(p => p.Status);
    }
}
