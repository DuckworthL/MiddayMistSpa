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

namespace MiddayMistSpa.Core.Interfaces;

/// <summary>
/// Unit of Work pattern - manages repositories and transactions
/// </summary>
public interface IUnitOfWork : IDisposable
{
    // Identity & Security
    IRepository<Role> Roles { get; }
    IRepository<User> Users { get; }
    IRepository<PasswordHistory> PasswordHistories { get; }
    IRepository<UserSession> UserSessions { get; }
    IRepository<AuditLog> AuditLogs { get; }

    // Employee & HR
    IRepository<Employee> Employees { get; }
    IRepository<EmployeeSchedule> EmployeeSchedules { get; }
    IRepository<TimeOffRequest> TimeOffRequests { get; }
    IRepository<EmployeeLeaveBalance> EmployeeLeaveBalances { get; }
    IRepository<EmployeeAdvance> EmployeeAdvances { get; }
    IRepository<AttendanceRecord> AttendanceRecords { get; }

    // Payroll
    IRepository<PhilippineHoliday> PhilippineHolidays { get; }
    IRepository<SSSContributionRate> SSSContributionRates { get; }
    IRepository<PhilHealthContributionRate> PhilHealthContributionRates { get; }
    IRepository<PagIBIGContributionRate> PagIBIGContributionRates { get; }
    IRepository<WithholdingTaxBracket> WithholdingTaxBrackets { get; }
    IRepository<PayrollPeriod> PayrollPeriods { get; }
    IRepository<PayrollRecord> PayrollRecords { get; }

    // Customer
    IRepository<Customer> Customers { get; }
    IRepository<CustomerSegment> CustomerSegments { get; }

    // Services
    IRepository<ServiceCategory> ServiceCategories { get; }
    IRepository<Service> Services { get; }
    IRepository<ServiceProductRequirement> ServiceProductRequirements { get; }

    // Inventory
    IRepository<ProductCategory> ProductCategories { get; }
    IRepository<Product> Products { get; }
    IRepository<Supplier> Suppliers { get; }
    IRepository<PurchaseOrder> PurchaseOrders { get; }
    IRepository<PurchaseOrderItem> PurchaseOrderItems { get; }
    IRepository<StockAdjustment> StockAdjustments { get; }

    // Appointments
    IRepository<Appointment> Appointments { get; }

    // Transactions
    IRepository<Transaction> Transactions { get; }
    IRepository<TransactionServiceItem> TransactionServiceItems { get; }
    IRepository<TransactionProductItem> TransactionProductItems { get; }
    IRepository<Refund> Refunds { get; }

    // Accounting
    IRepository<ChartOfAccount> ChartOfAccounts { get; }
    IRepository<JournalEntry> JournalEntries { get; }
    IRepository<JournalEntryLine> JournalEntryLines { get; }

    // Configuration
    IRepository<SystemSetting> SystemSettings { get; }

    // Transaction management
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
