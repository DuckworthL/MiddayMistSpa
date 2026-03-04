using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiddayMistSpa.Core.Entities.Payroll;

namespace MiddayMistSpa.Infrastructure.Data.Configurations;

public class PhilippineHolidayConfiguration : IEntityTypeConfiguration<PhilippineHoliday>
{
    public void Configure(EntityTypeBuilder<PhilippineHoliday> builder)
    {
        builder.ToTable("PhilippineHolidays");
        builder.HasKey(h => h.HolidayId);

        builder.Property(h => h.HolidayName).HasMaxLength(200).IsRequired();
        builder.Property(h => h.HolidayType).HasMaxLength(50).IsRequired();
    }
}

public class SSSContributionRateConfiguration : IEntityTypeConfiguration<SSSContributionRate>
{
    public void Configure(EntityTypeBuilder<SSSContributionRate> builder)
    {
        builder.ToTable("SSSContributionRates");
        builder.HasKey(s => s.SSSRateId);
    }
}

public class PhilHealthContributionRateConfiguration : IEntityTypeConfiguration<PhilHealthContributionRate>
{
    public void Configure(EntityTypeBuilder<PhilHealthContributionRate> builder)
    {
        builder.ToTable("PhilHealthContributionRates");
        builder.HasKey(p => p.PhilHealthRateId);

        // Different precision for rates
        builder.Property(p => p.PremiumRate).HasPrecision(5, 4);
        builder.Property(p => p.EmployeeShare).HasPrecision(5, 4);
        builder.Property(p => p.EmployerShare).HasPrecision(5, 4);
    }
}

public class PagIBIGContributionRateConfiguration : IEntityTypeConfiguration<PagIBIGContributionRate>
{
    public void Configure(EntityTypeBuilder<PagIBIGContributionRate> builder)
    {
        builder.ToTable("PagIBIGContributionRates");
        builder.HasKey(p => p.PagIBIGRateId);

        builder.Property(p => p.EmployeeRate).HasPrecision(5, 4);
        builder.Property(p => p.EmployerRate).HasPrecision(5, 4);
    }
}

public class WithholdingTaxBracketConfiguration : IEntityTypeConfiguration<WithholdingTaxBracket>
{
    public void Configure(EntityTypeBuilder<WithholdingTaxBracket> builder)
    {
        builder.ToTable("WithholdingTaxBrackets");
        builder.HasKey(t => t.TaxBracketId);

        builder.Property(t => t.TaxRate).HasPrecision(5, 4);
    }
}

public class PayrollPeriodConfiguration : IEntityTypeConfiguration<PayrollPeriod>
{
    public void Configure(EntityTypeBuilder<PayrollPeriod> builder)
    {
        builder.ToTable("PayrollPeriods");
        builder.HasKey(p => p.PayrollPeriodId);

        builder.Property(p => p.PeriodName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.PayrollType).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Status).HasMaxLength(50).IsRequired();

        builder.HasOne(p => p.FinalizedByUser)
            .WithMany()
            .HasForeignKey(p => p.FinalizedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class PayrollRecordConfiguration : IEntityTypeConfiguration<PayrollRecord>
{
    public void Configure(EntityTypeBuilder<PayrollRecord> builder)
    {
        builder.ToTable("PayrollRecords");
        builder.HasKey(p => p.PayrollRecordId);

        builder.Property(p => p.PaymentMethod).HasMaxLength(50);
        builder.Property(p => p.PaymentStatus).HasMaxLength(50).IsRequired();

        builder.HasIndex(p => p.EmployeeId);
        builder.HasIndex(p => p.PayrollPeriodId);

        builder.HasOne(p => p.PayrollPeriod)
            .WithMany(pp => pp.PayrollRecords)
            .HasForeignKey(p => p.PayrollPeriodId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Employee)
            .WithMany()
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
