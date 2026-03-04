using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiddayMistSpa.Core.Entities.Configuration;

namespace MiddayMistSpa.Infrastructure.Data.Configurations;

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings");
        builder.HasKey(s => s.SettingId);

        builder.Property(s => s.SettingKey).HasMaxLength(100).IsRequired();
        builder.Property(s => s.SettingType).HasMaxLength(50).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(500);
        builder.Property(s => s.Category).HasMaxLength(100);

        builder.HasIndex(s => s.SettingKey).IsUnique();

        builder.HasOne(s => s.UpdatedByUser)
            .WithMany()
            .HasForeignKey(s => s.UpdatedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class CurrencyRateConfiguration : IEntityTypeConfiguration<CurrencyRate>
{
    public void Configure(EntityTypeBuilder<CurrencyRate> builder)
    {
        builder.ToTable("CurrencyRates");
        builder.HasKey(r => r.RateId);

        builder.Property(r => r.BaseCurrency).HasMaxLength(3).IsRequired();
        builder.Property(r => r.TargetCurrency).HasMaxLength(3).IsRequired();
        builder.Property(r => r.ExchangeRate).HasPrecision(18, 6).IsRequired();
        builder.Property(r => r.Source).HasMaxLength(50);

        builder.HasIndex(r => r.TargetCurrency);
        builder.HasIndex(r => new { r.BaseCurrency, r.TargetCurrency }).IsUnique();

        builder.Ignore(r => r.IsStale);
    }
}
