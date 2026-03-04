using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiddayMistSpa.Core.Entities.Customer;

namespace MiddayMistSpa.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.CustomerId);

        builder.Property(c => c.CustomerCode).HasMaxLength(50).IsRequired();
        builder.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.LastName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(256);
        builder.Property(c => c.PhoneNumber).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Gender).HasMaxLength(10);
        builder.Property(c => c.Address).HasMaxLength(500);
        builder.Property(c => c.City).HasMaxLength(100);
        builder.Property(c => c.Province).HasMaxLength(100);
        builder.Property(c => c.PostalCode).HasMaxLength(20);

        builder.Property(c => c.MembershipType).HasMaxLength(50);
        builder.Property(c => c.PressurePreference).HasMaxLength(50);
        builder.Property(c => c.TemperaturePreference).HasMaxLength(50);
        builder.Property(c => c.MusicPreference).HasMaxLength(50);
        builder.Property(c => c.Allergies).HasMaxLength(500);
        builder.Property(c => c.MedicalNotes).HasMaxLength(1000);
        builder.Property(c => c.SpecialRequests).HasMaxLength(500);
        builder.Property(c => c.ReferralSource).HasMaxLength(100);
        builder.Property(c => c.CustomerSegment).HasMaxLength(50);
        builder.Property(c => c.EmergencyContactName).HasMaxLength(100);
        builder.Property(c => c.EmergencyContactPhone).HasMaxLength(50);
        builder.Property(c => c.EmergencyContactRelationship).HasMaxLength(50);
        builder.Property(c => c.PreferredCommunicationChannel).HasMaxLength(20).HasDefaultValue("Email");

        builder.HasIndex(c => c.CustomerCode).IsUnique();
        builder.HasIndex(c => c.Email);
        builder.HasIndex(c => c.PhoneNumber);
        builder.HasIndex(c => c.CustomerSegment);
        builder.HasIndex(c => c.LastVisitDate);

        builder.HasOne(c => c.PreferredTherapist)
            .WithMany()
            .HasForeignKey(c => c.PreferredTherapistId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Ignore(c => c.FullName);
    }
}

public class CustomerSegmentConfiguration : IEntityTypeConfiguration<CustomerSegment>
{
    public void Configure(EntityTypeBuilder<CustomerSegment> builder)
    {
        builder.ToTable("CustomerSegments");
        builder.HasKey(s => s.SegmentId);

        builder.Property(s => s.SegmentName).HasMaxLength(100).IsRequired();
        builder.Property(s => s.SegmentCode).HasMaxLength(50).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(500);
        builder.Property(s => s.RecommendedAction).HasMaxLength(500);

        builder.HasIndex(s => s.SegmentCode).IsUnique();
    }
}

public class LoyaltyPointTransactionConfiguration : IEntityTypeConfiguration<LoyaltyPointTransaction>
{
    public void Configure(EntityTypeBuilder<LoyaltyPointTransaction> builder)
    {
        builder.ToTable("LoyaltyPointTransactions");
        builder.HasKey(l => l.LoyaltyPointTransactionId);

        builder.Property(l => l.TransactionType).HasMaxLength(20).IsRequired();
        builder.Property(l => l.Description).HasMaxLength(500);

        builder.HasIndex(l => l.CustomerId);
        builder.HasIndex(l => l.ExpiryDate);
        builder.HasIndex(l => l.TransactionType);

        builder.HasOne(l => l.Customer)
            .WithMany(c => c.LoyaltyPointTransactions)
            .HasForeignKey(l => l.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Transaction)
            .WithMany()
            .HasForeignKey(l => l.TransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Ignore(l => l.IsExpired);
    }
}
