using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiddayMistSpa.Core.Entities.Accounting;

namespace MiddayMistSpa.Infrastructure.Data.Configurations;

public class ChartOfAccountConfiguration : IEntityTypeConfiguration<ChartOfAccount>
{
    public void Configure(EntityTypeBuilder<ChartOfAccount> builder)
    {
        builder.ToTable("ChartOfAccounts");
        builder.HasKey(a => a.AccountId);

        builder.Property(a => a.AccountCode).HasMaxLength(50).IsRequired();
        builder.Property(a => a.AccountName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.AccountType).HasMaxLength(50).IsRequired();
        builder.Property(a => a.NormalBalance).HasMaxLength(10).IsRequired().HasDefaultValue("Debit");
        builder.Property(a => a.AccountCategory).HasMaxLength(100);

        builder.HasIndex(a => a.AccountCode).IsUnique();

        builder.HasOne(a => a.ParentAccount)
            .WithMany(a => a.ChildAccounts)
            .HasForeignKey(a => a.ParentAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("JournalEntries");
        builder.HasKey(j => j.JournalEntryId);

        builder.Property(j => j.EntryNumber).HasMaxLength(50).IsRequired();
        builder.Property(j => j.Description).HasMaxLength(500);
        builder.Property(j => j.ReferenceType).HasMaxLength(50);
        builder.Property(j => j.ReferenceId).HasMaxLength(50);
        builder.Property(j => j.Status).HasMaxLength(50).IsRequired();

        builder.HasIndex(j => j.EntryNumber).IsUnique();

        builder.Property(j => j.VoidReason).HasMaxLength(500);

        builder.HasOne(j => j.CreatedByUser)
            .WithMany()
            .HasForeignKey(j => j.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(j => j.VoidedByUser)
            .WithMany()
            .HasForeignKey(j => j.VoidedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(j => j.ReversalOfEntry)
            .WithMany()
            .HasForeignKey(j => j.ReversalOfEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(j => j.IsBalanced);
    }
}

public class JournalEntryLineConfiguration : IEntityTypeConfiguration<JournalEntryLine>
{
    public void Configure(EntityTypeBuilder<JournalEntryLine> builder)
    {
        builder.ToTable("JournalEntryLines");
        builder.HasKey(l => l.JournalLineId);

        builder.Property(l => l.Description).HasMaxLength(500);

        builder.HasOne(l => l.JournalEntry)
            .WithMany(j => j.Lines)
            .HasForeignKey(l => l.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Account)
            .WithMany(a => a.JournalEntryLines)
            .HasForeignKey(l => l.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(l => l.IsDebit);
        builder.Ignore(l => l.IsCredit);
    }
}
