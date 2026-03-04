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
using MiddayMistSpa.Core.Interfaces;
using MiddayMistSpa.Infrastructure.Data;

namespace MiddayMistSpa.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly SpaDbContext _context;
    private bool _disposed;

    // Identity
    private IRepository<User>? _users;
    private IRepository<Role>? _roles;
    private IRepository<PasswordHistory>? _passwordHistories;
    private IRepository<UserSession>? _userSessions;
    private IRepository<AuditLog>? _auditLogs;

    // Employee
    private IRepository<Core.Entities.Employee.Employee>? _employees;
    private IRepository<EmployeeSchedule>? _employeeSchedules;
    private IRepository<TimeOffRequest>? _timeOffRequests;
    private IRepository<EmployeeLeaveBalance>? _employeeLeaveBalances;
    private IRepository<EmployeeAdvance>? _employeeAdvances;
    private IRepository<AttendanceRecord>? _attendanceRecords;

    // Payroll
    private IRepository<PhilippineHoliday>? _philippineHolidays;
    private IRepository<SSSContributionRate>? _sssContributionRates;
    private IRepository<PhilHealthContributionRate>? _philHealthContributionRates;
    private IRepository<PagIBIGContributionRate>? _pagIBIGContributionRates;
    private IRepository<WithholdingTaxBracket>? _withholdingTaxBrackets;
    private IRepository<PayrollPeriod>? _payrollPeriods;
    private IRepository<PayrollRecord>? _payrollRecords;

    // Customer
    private IRepository<Core.Entities.Customer.Customer>? _customers;
    private IRepository<CustomerSegment>? _customerSegments;

    // Service
    private IRepository<ServiceCategory>? _serviceCategories;
    private IRepository<Core.Entities.Service.Service>? _services;
    private IRepository<ServiceProductRequirement>? _serviceProductRequirements;

    // Inventory
    private IRepository<ProductCategory>? _productCategories;
    private IRepository<Product>? _products;
    private IRepository<Supplier>? _suppliers;
    private IRepository<PurchaseOrder>? _purchaseOrders;
    private IRepository<PurchaseOrderItem>? _purchaseOrderItems;
    private IRepository<StockAdjustment>? _stockAdjustments;

    // Appointment
    private IRepository<Core.Entities.Appointment.Appointment>? _appointments;

    // Transaction
    private IRepository<Core.Entities.Transaction.Transaction>? _transactions;
    private IRepository<TransactionServiceItem>? _transactionServiceItems;
    private IRepository<TransactionProductItem>? _transactionProductItems;
    private IRepository<Refund>? _refunds;

    // Accounting
    private IRepository<ChartOfAccount>? _chartOfAccounts;
    private IRepository<JournalEntry>? _journalEntries;
    private IRepository<JournalEntryLine>? _journalEntryLines;

    // Configuration
    private IRepository<SystemSetting>? _systemSettings;

    public UnitOfWork(SpaDbContext context)
    {
        _context = context;
    }

    // Identity Repositories
    public IRepository<User> Users => _users ??= new Repository<User>(_context);
    public IRepository<Role> Roles => _roles ??= new Repository<Role>(_context);
    public IRepository<PasswordHistory> PasswordHistories => _passwordHistories ??= new Repository<PasswordHistory>(_context);
    public IRepository<UserSession> UserSessions => _userSessions ??= new Repository<UserSession>(_context);
    public IRepository<AuditLog> AuditLogs => _auditLogs ??= new Repository<AuditLog>(_context);

    // Employee Repositories
    public IRepository<Core.Entities.Employee.Employee> Employees => _employees ??= new Repository<Core.Entities.Employee.Employee>(_context);
    public IRepository<EmployeeSchedule> EmployeeSchedules => _employeeSchedules ??= new Repository<EmployeeSchedule>(_context);
    public IRepository<TimeOffRequest> TimeOffRequests => _timeOffRequests ??= new Repository<TimeOffRequest>(_context);
    public IRepository<EmployeeLeaveBalance> EmployeeLeaveBalances => _employeeLeaveBalances ??= new Repository<EmployeeLeaveBalance>(_context);
    public IRepository<EmployeeAdvance> EmployeeAdvances => _employeeAdvances ??= new Repository<EmployeeAdvance>(_context);
    public IRepository<AttendanceRecord> AttendanceRecords => _attendanceRecords ??= new Repository<AttendanceRecord>(_context);

    // Payroll Repositories
    public IRepository<PhilippineHoliday> PhilippineHolidays => _philippineHolidays ??= new Repository<PhilippineHoliday>(_context);
    public IRepository<SSSContributionRate> SSSContributionRates => _sssContributionRates ??= new Repository<SSSContributionRate>(_context);
    public IRepository<PhilHealthContributionRate> PhilHealthContributionRates => _philHealthContributionRates ??= new Repository<PhilHealthContributionRate>(_context);
    public IRepository<PagIBIGContributionRate> PagIBIGContributionRates => _pagIBIGContributionRates ??= new Repository<PagIBIGContributionRate>(_context);
    public IRepository<WithholdingTaxBracket> WithholdingTaxBrackets => _withholdingTaxBrackets ??= new Repository<WithholdingTaxBracket>(_context);
    public IRepository<PayrollPeriod> PayrollPeriods => _payrollPeriods ??= new Repository<PayrollPeriod>(_context);
    public IRepository<PayrollRecord> PayrollRecords => _payrollRecords ??= new Repository<PayrollRecord>(_context);

    // Customer Repositories
    public IRepository<Core.Entities.Customer.Customer> Customers => _customers ??= new Repository<Core.Entities.Customer.Customer>(_context);
    public IRepository<CustomerSegment> CustomerSegments => _customerSegments ??= new Repository<CustomerSegment>(_context);

    // Service Repositories
    public IRepository<ServiceCategory> ServiceCategories => _serviceCategories ??= new Repository<ServiceCategory>(_context);
    public IRepository<Core.Entities.Service.Service> Services => _services ??= new Repository<Core.Entities.Service.Service>(_context);
    public IRepository<ServiceProductRequirement> ServiceProductRequirements => _serviceProductRequirements ??= new Repository<ServiceProductRequirement>(_context);

    // Inventory Repositories
    public IRepository<ProductCategory> ProductCategories => _productCategories ??= new Repository<ProductCategory>(_context);
    public IRepository<Product> Products => _products ??= new Repository<Product>(_context);
    public IRepository<Supplier> Suppliers => _suppliers ??= new Repository<Supplier>(_context);
    public IRepository<PurchaseOrder> PurchaseOrders => _purchaseOrders ??= new Repository<PurchaseOrder>(_context);
    public IRepository<PurchaseOrderItem> PurchaseOrderItems => _purchaseOrderItems ??= new Repository<PurchaseOrderItem>(_context);
    public IRepository<StockAdjustment> StockAdjustments => _stockAdjustments ??= new Repository<StockAdjustment>(_context);

    // Appointment Repositories
    public IRepository<Core.Entities.Appointment.Appointment> Appointments => _appointments ??= new Repository<Core.Entities.Appointment.Appointment>(_context);

    // Transaction Repositories
    public IRepository<Core.Entities.Transaction.Transaction> Transactions => _transactions ??= new Repository<Core.Entities.Transaction.Transaction>(_context);
    public IRepository<TransactionServiceItem> TransactionServiceItems => _transactionServiceItems ??= new Repository<TransactionServiceItem>(_context);
    public IRepository<TransactionProductItem> TransactionProductItems => _transactionProductItems ??= new Repository<TransactionProductItem>(_context);
    public IRepository<Refund> Refunds => _refunds ??= new Repository<Refund>(_context);

    // Accounting Repositories
    public IRepository<ChartOfAccount> ChartOfAccounts => _chartOfAccounts ??= new Repository<ChartOfAccount>(_context);
    public IRepository<JournalEntry> JournalEntries => _journalEntries ??= new Repository<JournalEntry>(_context);
    public IRepository<JournalEntryLine> JournalEntryLines => _journalEntryLines ??= new Repository<JournalEntryLine>(_context);

    // Configuration Repositories
    public IRepository<SystemSetting> SystemSettings => _systemSettings ??= new Repository<SystemSetting>(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.CommitTransactionAsync();
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.RollbackTransactionAsync();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
