using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiddayMistSpa.Core.Entities.Transaction;

namespace MiddayMistSpa.Infrastructure.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");
        builder.HasKey(t => t.TransactionId);

        builder.Property(t => t.TransactionNumber).HasMaxLength(50).IsRequired();
        builder.Property(t => t.PaymentMethod).HasMaxLength(50).IsRequired();
        builder.Property(t => t.PaymentStatus).HasMaxLength(50).IsRequired();

        // Multi-Currency
        builder.Property(t => t.ClientCurrency).HasMaxLength(3);
        builder.Property(t => t.ClientCountryCode).HasMaxLength(2);
        builder.Property(t => t.ClientIPAddress).HasMaxLength(45);
        builder.Property(t => t.ExchangeRate).HasPrecision(18, 6);

        builder.Property(t => t.VoidReason).HasMaxLength(500);

        // Concurrency token
        builder.Property(t => t.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(t => t.TransactionNumber).IsUnique();
        builder.HasIndex(t => t.CustomerId);
        builder.HasIndex(t => t.TransactionDate);
        builder.HasIndex(t => t.PaymentStatus);

        builder.HasOne(t => t.Customer)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Appointment)
            .WithMany()
            .HasForeignKey(t => t.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Cashier)
            .WithMany()
            .HasForeignKey(t => t.CashierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.VoidedByUser)
            .WithMany()
            .HasForeignKey(t => t.VoidedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Ignore(t => t.IsPaid);
        builder.Ignore(t => t.IsVoided);
        builder.Ignore(t => t.IsRefunded);
    }
}

public class TransactionServiceItemConfiguration : IEntityTypeConfiguration<TransactionServiceItem>
{
    public void Configure(EntityTypeBuilder<TransactionServiceItem> builder)
    {
        builder.ToTable("TransactionServiceItems");
        builder.HasKey(i => i.TransactionServiceItemId);

        builder.Property(i => i.CommissionRate).HasPrecision(5, 4);

        builder.HasOne(i => i.Transaction)
            .WithMany(t => t.ServiceItems)
            .HasForeignKey(i => i.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Service)
            .WithMany()
            .HasForeignKey(i => i.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Therapist)
            .WithMany()
            .HasForeignKey(i => i.TherapistId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class TransactionProductItemConfiguration : IEntityTypeConfiguration<TransactionProductItem>
{
    public void Configure(EntityTypeBuilder<TransactionProductItem> builder)
    {
        builder.ToTable("TransactionProductItems");
        builder.HasKey(i => i.TransactionProductItemId);

        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.Property(i => i.CommissionRate).HasPrecision(5, 4);

        builder.HasOne(i => i.Transaction)
            .WithMany(t => t.ProductItems)
            .HasForeignKey(i => i.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.SoldByUser)
            .WithMany()
            .HasForeignKey(i => i.SoldBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("Refunds");
        builder.HasKey(r => r.RefundId);

        builder.Property(r => r.RefundMethod).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Reason).HasMaxLength(500);
        builder.Property(r => r.RefundType).HasMaxLength(50).IsRequired();

        builder.HasOne(r => r.Transaction)
            .WithMany(t => t.Refunds)
            .HasForeignKey(r => r.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ApprovedByUser)
            .WithMany()
            .HasForeignKey(r => r.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ProcessedByUser)
            .WithMany()
            .HasForeignKey(r => r.ProcessedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
