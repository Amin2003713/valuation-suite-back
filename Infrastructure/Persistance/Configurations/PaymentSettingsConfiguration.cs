using Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public sealed class PaymentSettingsConfiguration : IEntityTypeConfiguration<PaymentSettings>
{
    public void Configure(EntityTypeBuilder<PaymentSettings> builder)
    {
        builder.Property(s => s.MerchantId).IsRequired().HasMaxLength(100);
        builder.Property(s => s.FrontendRedirectUrl).HasMaxLength(500);

        // The settings row is read on every checkout: keep the "latest row" scan trivial.
        builder.HasIndex(s => s.ModifiedAt);
    }
}
