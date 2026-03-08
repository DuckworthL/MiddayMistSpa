using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MiddayMistSpa.Core;
using MiddayMistSpa.Core.Entities.Accounting;
using MiddayMistSpa.Core.Entities.Appointment;
using MiddayMistSpa.Core.Entities.Configuration;
using MiddayMistSpa.Core.Entities.Customer;
using MiddayMistSpa.Core.Entities.Employee;
using MiddayMistSpa.Core.Entities.Identity;
using MiddayMistSpa.Core.Entities.Inventory;
using MiddayMistSpa.Core.Entities.Payroll;
using MiddayMistSpa.Core.Entities.Service;
using MiddayMistSpa.Core.Entities.Transaction;

namespace MiddayMistSpa.Infrastructure.Data;

/// <summary>
/// Entity Framework Core DbContext for MiddayMist Spa ERP
/// </summary>
public class SpaDbContext : DbContext
{
    public SpaDbContext(DbContextOptions<SpaDbContext> options) : base(options)
    {
    }

    // Identity & Security
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Employee & HR
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeSchedule> EmployeeSchedules => Set<EmployeeSchedule>();
    public DbSet<EmployeeShift> EmployeeShifts => Set<EmployeeShift>();
    public DbSet<ShiftException> ShiftExceptions => Set<ShiftException>();
    public DbSet<TimeOffRequest> TimeOffRequests => Set<TimeOffRequest>();
    public DbSet<EmployeeLeaveBalance> EmployeeLeaveBalances => Set<EmployeeLeaveBalance>();
    public DbSet<EmployeeAdvance> EmployeeAdvances => Set<EmployeeAdvance>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    // Payroll
    public DbSet<PhilippineHoliday> PhilippineHolidays => Set<PhilippineHoliday>();
    public DbSet<SSSContributionRate> SSSContributionRates => Set<SSSContributionRate>();
    public DbSet<PhilHealthContributionRate> PhilHealthContributionRates => Set<PhilHealthContributionRate>();
    public DbSet<PagIBIGContributionRate> PagIBIGContributionRates => Set<PagIBIGContributionRate>();
    public DbSet<WithholdingTaxBracket> WithholdingTaxBrackets => Set<WithholdingTaxBracket>();
    public DbSet<PayrollPeriod> PayrollPeriods => Set<PayrollPeriod>();
    public DbSet<PayrollRecord> PayrollRecords => Set<PayrollRecord>();

    // Customer
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerSegment> CustomerSegments => Set<CustomerSegment>();
    public DbSet<LoyaltyPointTransaction> LoyaltyPointTransactions => Set<LoyaltyPointTransaction>();

    // Services
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceProductRequirement> ServiceProductRequirements => Set<ServiceProductRequirement>();

    // Inventory
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<ProductBatch> ProductBatches => Set<ProductBatch>();

    // Appointments
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AppointmentServiceItem> AppointmentServiceItems => Set<AppointmentServiceItem>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Waitlist> Waitlists => Set<Waitlist>();

    // Transactions
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionServiceItem> TransactionServiceItems => Set<TransactionServiceItem>();
    public DbSet<TransactionProductItem> TransactionProductItems => Set<TransactionProductItem>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<CashDrawerSession> CashDrawerSessions => Set<CashDrawerSession>();

    // Accounting
    public DbSet<ChartOfAccount> ChartOfAccounts => Set<ChartOfAccount>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();

    // Configuration
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<CurrencyRate> CurrencyRates => Set<CurrencyRate>();

    /// <summary>
    /// Automatically stamps CreatedAt and UpdatedAt with Philippine Standard Time on every save.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = PhilippineTime.Now;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                TrySetProperty(entry, "CreatedAt", now);
                TrySetProperty(entry, "UpdatedAt", now);
            }
            else if (entry.State == EntityState.Modified)
            {
                TrySetProperty(entry, "UpdatedAt", now);
            }
        }
        return await base.SaveChangesAsync(cancellationToken);
    }

    private static void TrySetProperty(EntityEntry entry, string propertyName, DateTime value)
    {
        var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == propertyName);
        if (prop != null)
            prop.CurrentValue = value;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SpaDbContext).Assembly);

        // Unique constraints on business keys
        modelBuilder.Entity<MiddayMistSpa.Core.Entities.Accounting.Invoice>()
            .HasIndex(e => e.InvoiceNumber)
            .IsUnique();

        modelBuilder.Entity<MiddayMistSpa.Core.Entities.Transaction.Transaction>()
            .HasIndex(e => e.TransactionNumber)
            .IsUnique();

        // CashDrawerSession - configure non-conventional FK relationships
        modelBuilder.Entity<CashDrawerSession>(entity =>
        {
            entity.HasKey(e => e.SessionId);
            entity.HasOne(e => e.OpenedByUser).WithMany().HasForeignKey(e => e.OpenedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ClosedByUser).WithMany().HasForeignKey(e => e.ClosedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // Configure decimal precision for Philippine Peso (default 18,2)
        // Skip properties that already have explicit precision set by configurations (e.g., ExchangeRate at 18,6)
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            if (property.GetPrecision() == null)
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }
        }
    }
}
