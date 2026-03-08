using Microsoft.EntityFrameworkCore;
using System.Text;
using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.API.DTOs.Payroll;
using MiddayMistSpa.Core;
using MiddayMistSpa.Core.Entities.Employee;
using MiddayMistSpa.Core.Entities.Payroll;
using MiddayMistSpa.Infrastructure.Data;

namespace MiddayMistSpa.API.Services;

public class PayrollService : IPayrollService
{
    private readonly SpaDbContext _context;
    private readonly ILogger<PayrollService> _logger;
    private readonly IAccountingService _accountingService;

    public PayrollService(SpaDbContext context, ILogger<PayrollService> logger, IAccountingService accountingService)
    {
        _context = context;
        _logger = logger;
        _accountingService = accountingService;
    }

    // ============================================================================
    // Payroll Period Management
    // ============================================================================

    public async Task<PayrollPeriodResponse> CreatePayrollPeriodAsync(CreatePayrollPeriodRequest request)
    {
        if (request.StartDate > request.EndDate)
            throw new InvalidOperationException("Start date must be on or before end date");

        // Check for overlapping periods of the same type
        var overlapping = await _context.PayrollPeriods
            .AnyAsync(p => p.PayrollType == request.PayrollType &&
                          p.StartDate <= request.EndDate &&
                          p.EndDate >= request.StartDate);
        if (overlapping)
            throw new InvalidOperationException("A payroll period of this type already exists that overlaps with the specified dates");

        var period = new PayrollPeriod
        {
            PeriodName = request.PeriodName,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PayrollType = request.PayrollType,
            CutoffDate = request.CutoffDate,
            PaymentDate = request.PaymentDate,
            Status = "Draft",
            CreatedAt = PhilippineTime.Now,
            UpdatedAt = PhilippineTime.Now
        };

        _context.PayrollPeriods.Add(period);
        await _context.SaveChangesAsync();

        return MapToPeriodResponse(period);
    }

    public async Task<PayrollPeriodResponse?> GetPayrollPeriodByIdAsync(int payrollPeriodId)
    {
        var period = await _context.PayrollPeriods
            .Include(p => p.FinalizedByUser)
            .Include(p => p.PayrollRecords)
            .FirstOrDefaultAsync(p => p.PayrollPeriodId == payrollPeriodId);

        if (period == null)
            return null;

        return MapToPeriodResponse(period);
    }

    public async Task<PagedResponse<PayrollPeriodListResponse>> SearchPayrollPeriodsAsync(PayrollPeriodSearchRequest request)
    {
        var query = _context.PayrollPeriods
            .Include(p => p.PayrollRecords)
            .Include(p => p.FinalizedByUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(p => p.PeriodName.ToLower().Contains(term));
        }

        if (request.DateFrom.HasValue)
            query = query.Where(p => p.StartDate >= request.DateFrom.Value);

        if (request.DateTo.HasValue)
            query = query.Where(p => p.EndDate <= request.DateTo.Value);

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(p => p.Status == request.Status);

        if (!string.IsNullOrWhiteSpace(request.PayrollType))
            query = query.Where(p => p.PayrollType == request.PayrollType);

        // Sort
        query = request.SortBy?.ToLower() switch
        {
            "name" => request.SortDescending
                ? query.OrderByDescending(p => p.PeriodName)
                : query.OrderBy(p => p.PeriodName),
            "status" => request.SortDescending
                ? query.OrderByDescending(p => p.Status)
                : query.OrderBy(p => p.Status),
            _ => request.SortDescending
                ? query.OrderByDescending(p => p.StartDate)
                : query.OrderBy(p => p.StartDate)
        };

        var totalCount = await query.CountAsync();
        var periods = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new PayrollPeriodListResponse
            {
                PayrollPeriodId = p.PayrollPeriodId,
                PeriodName = p.PeriodName,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                PayrollType = p.PayrollType,
                Status = p.Status,
                PaymentDate = p.PaymentDate,
                RecordCount = p.PayrollRecords.Count,
                TotalNetPay = p.PayrollRecords.Sum(r => r.NetPay),
                FinalizedByName = p.FinalizedByUser != null
                    ? p.FinalizedByUser.FirstName + " " + p.FinalizedByUser.LastName
                    : null,
                FinalizedAt = p.FinalizedAt,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return new PagedResponse<PayrollPeriodListResponse>
        {
            Items = periods,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<PayrollPeriodResponse> UpdatePayrollPeriodAsync(int payrollPeriodId, UpdatePayrollPeriodRequest request)
    {
        var period = await _context.PayrollPeriods.FindAsync(payrollPeriodId);
        if (period == null)
            throw new InvalidOperationException("Payroll period not found");

        if (period.Status == "Finalized" || period.Status == "Paid")
            throw new InvalidOperationException("Cannot update finalized or paid payroll period");

        if (!string.IsNullOrWhiteSpace(request.PeriodName))
            period.PeriodName = request.PeriodName;
        if (request.StartDate.HasValue)
            period.StartDate = request.StartDate.Value;
        if (request.EndDate.HasValue)
            period.EndDate = request.EndDate.Value;
        if (request.CutoffDate.HasValue)
            period.CutoffDate = request.CutoffDate.Value;
        if (request.PaymentDate.HasValue)
            period.PaymentDate = request.PaymentDate.Value;

        period.UpdatedAt = PhilippineTime.Now;

        await _context.SaveChangesAsync();
        return (await GetPayrollPeriodByIdAsync(payrollPeriodId))!;
    }

    public async Task<PayrollPeriodResponse> ReopenPayrollPeriodAsync(int payrollPeriodId)
    {
        var period = await _context.PayrollPeriods
            .FirstOrDefaultAsync(p => p.PayrollPeriodId == payrollPeriodId)
            ?? throw new InvalidOperationException("Payroll period not found");

        if (period.Status == "Draft")
            throw new InvalidOperationException("Payroll period is already in Draft status");

        period.Status = "Draft";
        period.UpdatedAt = PhilippineTime.Now;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Payroll period {PeriodId} reopened to Draft status", payrollPeriodId);

        return (await GetPayrollPeriodByIdAsync(payrollPeriodId))!;
    }

    public async Task<PayrollPeriodResponse> FinalizePayrollPeriodAsync(int payrollPeriodId, int finalizedByUserId)
    {
        var period = await _context.PayrollPeriods
            .Include(p => p.PayrollRecords)
            .FirstOrDefaultAsync(p => p.PayrollPeriodId == payrollPeriodId);

        if (period == null)
            throw new InvalidOperationException("Payroll period not found");

        if (period.Status != "Draft")
            throw new InvalidOperationException("Only draft payroll periods can be finalized");

        if (!period.PayrollRecords.Any())
            throw new InvalidOperationException("Cannot finalize payroll period without records");

        period.Status = "Finalized";
        period.FinalizedBy = finalizedByUserId;
        period.FinalizedAt = PhilippineTime.Now;
        period.UpdatedAt = PhilippineTime.Now;

        // Mark all records as Paid when finalizing
        foreach (var record in period.PayrollRecords)
        {
            if (record.PaymentStatus is "Preview" or "Pending")
            {
                record.PaymentStatus = "Paid";
                record.PaymentDate ??= PhilippineTime.Now;
                record.UpdatedAt = PhilippineTime.Now;
            }
        }

        await _context.SaveChangesAsync();

        // Auto-create journal entry for the finalized payroll
        try
        {
            await _accountingService.CreatePayrollJournalEntryAsync(payrollPeriodId, finalizedByUserId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create auto-JE for payroll period {PeriodId}. GL may need manual entry.", payrollPeriodId);
        }

        return (await GetPayrollPeriodByIdAsync(payrollPeriodId))!;
    }

    public async Task<bool> DeletePayrollPeriodAsync(int payrollPeriodId)
    {
        var period = await _context.PayrollPeriods
            .Include(p => p.PayrollRecords)
            .FirstOrDefaultAsync(p => p.PayrollPeriodId == payrollPeriodId);

        if (period == null)
            return false;

        if (period.Status != "Draft")
            throw new InvalidOperationException("Only draft payroll periods can be deleted");

        _context.PayrollRecords.RemoveRange(period.PayrollRecords);
        _context.PayrollPeriods.Remove(period);
        await _context.SaveChangesAsync();

        return true;
    }

    // ============================================================================
    // Payroll Record Management
    // ============================================================================

    public async Task<PayrollRecordResponse> CreatePayrollRecordAsync(CreatePayrollRecordRequest request)
    {
        // Validate no negative numeric inputs
        if (request.DaysWorked < 0 || request.HoursWorked < 0 || request.OvertimeHours < 0 || request.NightDifferentialHours < 0)
            throw new InvalidOperationException("Days worked, hours, and overtime values cannot be negative");

        if ((request.BasicSalary.HasValue && request.BasicSalary < 0) ||
            (request.Commissions.HasValue && request.Commissions < 0) ||
            (request.Tips.HasValue && request.Tips < 0))
            throw new InvalidOperationException("Salary, commissions, and tips cannot be negative");

        var period = await _context.PayrollPeriods.FindAsync(request.PayrollPeriodId);
        if (period == null)
            throw new InvalidOperationException("Payroll period not found");

        if (period.Status != "Draft")
            throw new InvalidOperationException("Cannot add records to finalized payroll period");

        var employee = await _context.Employees.FindAsync(request.EmployeeId);
        if (employee == null)
            throw new InvalidOperationException("Employee not found");

        // Check if record already exists
        var existingRecord = await _context.PayrollRecords
            .FirstOrDefaultAsync(r => r.PayrollPeriodId == request.PayrollPeriodId && r.EmployeeId == request.EmployeeId);

        if (existingRecord != null)
            throw new InvalidOperationException("Payroll record already exists for this employee in this period");

        var record = new PayrollRecord
        {
            PayrollPeriodId = request.PayrollPeriodId,
            EmployeeId = request.EmployeeId,
            DaysWorked = request.DaysWorked,
            HoursWorked = request.HoursWorked,
            OvertimeHours = request.OvertimeHours,
            NightDifferentialHours = request.NightDifferentialHours,
            CreatedAt = PhilippineTime.Now,
            UpdatedAt = PhilippineTime.Now
        };

        // Calculate earnings
        record.BasicSalary = request.BasicSalary ?? CalculateBasicSalary(employee, request.DaysWorked, period.PayrollType);
        record.OvertimePay = CalculateOvertimePay(employee.DailyRate, request.OvertimeHours);
        record.NightDifferentialPay = CalculateNightDifferentialPay(employee.DailyRate, request.NightDifferentialHours);
        record.Commissions = request.Commissions ?? 0;
        record.Tips = request.Tips ?? 0;
        record.RiceAllowance = request.RiceAllowance ?? 0;
        record.LaundryAllowance = request.LaundryAllowance ?? 0;
        record.OtherAllowances = request.OtherAllowances ?? 0;

        record.GrossPay = record.BasicSalary + record.OvertimePay + record.NightDifferentialPay +
            record.HolidayPay + record.RestDayPay + record.Commissions + record.Tips +
            record.RiceAllowance + record.LaundryAllowance + record.OtherAllowances;

        // Calculate deductions
        await CalculateAndApplyDeductions(record, employee.MonthlyBasicSalary, period.PayrollType);

        _context.PayrollRecords.Add(record);
        await _context.SaveChangesAsync();

        return (await GetPayrollRecordByIdAsync(record.PayrollRecordId))!;
    }

    public async Task<PayrollRecordResponse?> GetPayrollRecordByIdAsync(int payrollRecordId)
    {
        var record = await _context.PayrollRecords
            .Include(r => r.PayrollPeriod)
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.PayrollRecordId == payrollRecordId);

        if (record == null)
            return null;

        return MapToRecordResponse(record);
    }

    public async Task<PagedResponse<PayrollRecordResponse>> GetPayrollRecordsByPeriodAsync(int periodId, int page, int pageSize)
    {
        var query = _context.PayrollRecords
            .Include(r => r.Employee)
            .Include(r => r.PayrollPeriod)
            .Where(r => r.PayrollPeriodId == periodId)
            .OrderBy(r => r.Employee.LastName);

        var totalCount = await query.CountAsync();
        var records = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<PayrollRecordResponse>
        {
            Items = records.Select(MapToRecordResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResponse<PayrollRecordListResponse>> SearchPayrollRecordsAsync(PayrollRecordSearchRequest request)
    {
        var query = _context.PayrollRecords
            .Include(r => r.Employee)
            .AsQueryable();

        if (request.PayrollPeriodId.HasValue)
            query = query.Where(r => r.PayrollPeriodId == request.PayrollPeriodId);

        if (request.EmployeeId.HasValue)
            query = query.Where(r => r.EmployeeId == request.EmployeeId);

        if (!string.IsNullOrWhiteSpace(request.PaymentStatus))
            query = query.Where(r => r.PaymentStatus == request.PaymentStatus);

        // Sort
        query = request.SortBy?.ToLower() switch
        {
            "netpay" => request.SortDescending
                ? query.OrderByDescending(r => r.NetPay)
                : query.OrderBy(r => r.NetPay),
            "grosspay" => request.SortDescending
                ? query.OrderByDescending(r => r.GrossPay)
                : query.OrderBy(r => r.GrossPay),
            _ => request.SortDescending
                ? query.OrderByDescending(r => r.Employee.LastName)
                : query.OrderBy(r => r.Employee.LastName)
        };

        var totalCount = await query.CountAsync();
        var records = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new PayrollRecordListResponse
            {
                PayrollRecordId = r.PayrollRecordId,
                EmployeeId = r.EmployeeId,
                EmployeeCode = r.Employee.EmployeeCode,
                EmployeeName = r.Employee.FirstName + " " + r.Employee.LastName,
                Position = r.Employee.Position,
                DaysWorked = r.DaysWorked,
                GrossPay = r.GrossPay,
                TotalDeductions = r.TotalDeductions,
                NetPay = r.NetPay,
                PaymentStatus = r.PaymentStatus
            })
            .ToListAsync();

        return new PagedResponse<PayrollRecordListResponse>
        {
            Items = records,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<PayrollRecordResponse> UpdatePayrollRecordAsync(int payrollRecordId, UpdatePayrollRecordRequest request)
    {
        var record = await _context.PayrollRecords
            .Include(r => r.PayrollPeriod)
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.PayrollRecordId == payrollRecordId);

        if (record == null)
            throw new InvalidOperationException("Payroll record not found");

        if (record.PayrollPeriod.Status == "Paid")
            throw new InvalidOperationException("Cannot update records in paid payroll period");

        // Update fields
        if (request.DaysWorked.HasValue) record.DaysWorked = request.DaysWorked.Value;
        if (request.HoursWorked.HasValue) record.HoursWorked = request.HoursWorked.Value;
        if (request.OvertimeHours.HasValue) record.OvertimeHours = request.OvertimeHours.Value;
        if (request.NightDifferentialHours.HasValue) record.NightDifferentialHours = request.NightDifferentialHours.Value;
        if (request.BasicSalary.HasValue) record.BasicSalary = request.BasicSalary.Value;
        if (request.OvertimePay.HasValue) record.OvertimePay = request.OvertimePay.Value;
        if (request.NightDifferentialPay.HasValue) record.NightDifferentialPay = request.NightDifferentialPay.Value;
        if (request.HolidayPay.HasValue) record.HolidayPay = request.HolidayPay.Value;
        if (request.RestDayPay.HasValue) record.RestDayPay = request.RestDayPay.Value;
        if (request.Commissions.HasValue) record.Commissions = request.Commissions.Value;
        if (request.Tips.HasValue) record.Tips = request.Tips.Value;
        if (request.RiceAllowance.HasValue) record.RiceAllowance = request.RiceAllowance.Value;
        if (request.LaundryAllowance.HasValue) record.LaundryAllowance = request.LaundryAllowance.Value;
        if (request.OtherAllowances.HasValue) record.OtherAllowances = request.OtherAllowances.Value;
        if (request.Tardiness.HasValue) record.Tardiness = request.Tardiness.Value;
        if (request.Absences.HasValue) record.Absences = request.Absences.Value;
        if (request.CashAdvances.HasValue) record.CashAdvances = request.CashAdvances.Value;
        if (request.LoanDeductions.HasValue) record.LoanDeductions = request.LoanDeductions.Value;
        if (request.OtherDeductions.HasValue) record.OtherDeductions = request.OtherDeductions.Value;

        // Recalculate totals
        record.GrossPay = record.BasicSalary + record.OvertimePay + record.NightDifferentialPay +
            record.HolidayPay + record.RestDayPay + record.Commissions + record.Tips +
            record.RiceAllowance + record.LaundryAllowance + record.OtherAllowances;

        // Recalculate mandatory deductions (SSS, PhilHealth, PagIBIG, tax) based on updated gross
        await CalculateAndApplyDeductions(record, record.Employee.MonthlyBasicSalary, record.PayrollPeriod.PayrollType);

        record.UpdatedAt = PhilippineTime.Now;

        await _context.SaveChangesAsync();
        return MapToRecordResponse(record);
    }

    public async Task<bool> DeletePayrollRecordAsync(int payrollRecordId)
    {
        var record = await _context.PayrollRecords
            .Include(r => r.PayrollPeriod)
            .FirstOrDefaultAsync(r => r.PayrollRecordId == payrollRecordId);

        if (record == null)
            return false;

        if (record.PayrollPeriod.Status != "Draft")
            throw new InvalidOperationException("Cannot delete records from finalized or paid payroll period");

        _context.PayrollRecords.Remove(record);
        await _context.SaveChangesAsync();

        return true;
    }

    // ============================================================================
    // Payroll Generation & Processing
    // ============================================================================

    public async Task<List<PayrollRecordResponse>> GeneratePayrollAsync(GeneratePayrollRequest request)
    {
        var period = await _context.PayrollPeriods.FindAsync(request.PayrollPeriodId);
        if (period == null)
            throw new InvalidOperationException("Payroll period not found");

        if (period.Status != "Draft")
            throw new InvalidOperationException("Cannot generate payroll for finalized period");

        // Get employees
        var employeesQuery = _context.Employees.Where(e => e.IsActive);

        if (request.EmployeeIds != null && request.EmployeeIds.Any())
            employeesQuery = employeesQuery.Where(e => request.EmployeeIds.Contains(e.EmployeeId));

        var employees = await employeesQuery.ToListAsync();

        // Get existing records to avoid duplicates
        var existingEmployeeIds = await _context.PayrollRecords
            .Where(r => r.PayrollPeriodId == request.PayrollPeriodId)
            .Select(r => r.EmployeeId)
            .ToListAsync();

        var results = new List<PayrollRecordResponse>();

        foreach (var employee in employees)
        {
            if (existingEmployeeIds.Contains(employee.EmployeeId))
                continue;

            // Get work hours from attendance (if available)
            var workDays = await CalculateWorkDaysAsync(employee.EmployeeId, period.StartDate, period.EndDate);

            var createRequest = new CreatePayrollRecordRequest
            {
                PayrollPeriodId = request.PayrollPeriodId,
                EmployeeId = employee.EmployeeId,
                DaysWorked = workDays.DaysWorked,
                HoursWorked = workDays.HoursWorked,
                OvertimeHours = workDays.OvertimeHours,
                NightDifferentialHours = workDays.NightDifferentialHours
            };

            // Get commissions and tips for the period
            var commissionAndTips = await CalculateCommissionsAndTipsAsync(employee.EmployeeId, period.StartDate, period.EndDate);
            createRequest.Commissions = commissionAndTips.Commissions;
            createRequest.Tips = commissionAndTips.Tips;

            var result = await CreatePayrollRecordAsync(createRequest);
            results.Add(result);
        }

        return results;
    }

    public async Task<List<PayrollRecordResponse>> PreviewPayrollAsync(DateTime startDate, DateTime endDate, List<int>? employeeIds = null)
    {
        // Get employees
        var employeesQuery = _context.Employees.Where(e => e.IsActive);
        if (employeeIds != null && employeeIds.Any())
            employeesQuery = employeesQuery.Where(e => employeeIds.Contains(e.EmployeeId));

        var employees = await employeesQuery.ToListAsync();
        var results = new List<PayrollRecordResponse>();
        var payrollType = "Semi-Monthly";

        foreach (var employee in employees)
        {
            var workDays = await CalculateWorkDaysAsync(employee.EmployeeId, startDate, endDate);
            var commissionAndTips = await CalculateCommissionsAndTipsAsync(employee.EmployeeId, startDate, endDate);

            // Build an in-memory record (NOT saved to DB)
            var record = new PayrollRecord
            {
                EmployeeId = employee.EmployeeId,
                DaysWorked = workDays.DaysWorked,
                HoursWorked = workDays.HoursWorked,
                OvertimeHours = workDays.OvertimeHours,
                NightDifferentialHours = workDays.NightDifferentialHours
            };

            record.BasicSalary = CalculateBasicSalary(employee, workDays.DaysWorked, payrollType);
            record.OvertimePay = CalculateOvertimePay(employee.DailyRate, workDays.OvertimeHours);
            record.NightDifferentialPay = CalculateNightDifferentialPay(employee.DailyRate, workDays.NightDifferentialHours);
            record.Commissions = commissionAndTips.Commissions;
            record.Tips = commissionAndTips.Tips;

            record.GrossPay = record.BasicSalary + record.OvertimePay + record.NightDifferentialPay +
                record.HolidayPay + record.RestDayPay + record.Commissions + record.Tips +
                record.RiceAllowance + record.LaundryAllowance + record.OtherAllowances;

            // Calculate deductions in-memory
            await CalculateAndApplyDeductions(record, employee.MonthlyBasicSalary, payrollType);

            results.Add(new PayrollRecordResponse
            {
                PayrollRecordId = 0,
                PayrollPeriodId = 0,
                PeriodName = $"Preview: {startDate:MMM dd} - {endDate:MMM dd, yyyy}",
                EmployeeId = employee.EmployeeId,
                EmployeeCode = employee.EmployeeCode,
                EmployeeName = $"{employee.FirstName} {employee.LastName}",
                Position = employee.Position,
                DaysWorked = record.DaysWorked,
                HoursWorked = record.HoursWorked,
                OvertimeHours = record.OvertimeHours,
                NightDifferentialHours = record.NightDifferentialHours,
                BasicSalary = record.BasicSalary,
                OvertimePay = record.OvertimePay,
                NightDifferentialPay = record.NightDifferentialPay,
                HolidayPay = record.HolidayPay,
                RestDayPay = record.RestDayPay,
                Commissions = record.Commissions,
                Tips = record.Tips,
                RiceAllowance = record.RiceAllowance,
                LaundryAllowance = record.LaundryAllowance,
                OtherAllowances = record.OtherAllowances,
                GrossPay = record.GrossPay,
                SSSContribution = record.SSSContribution,
                PhilHealthContribution = record.PhilHealthContribution,
                PagIBIGContribution = record.PagIBIGContribution,
                WithholdingTax = record.WithholdingTax,
                Tardiness = record.Tardiness,
                Absences = record.Absences,
                CashAdvances = record.CashAdvances,
                LoanDeductions = record.LoanDeductions,
                OtherDeductions = record.OtherDeductions,
                TotalDeductions = record.TotalDeductions,
                SSSEmployerContribution = record.SSSEmployerContribution,
                PhilHealthEmployerContribution = record.PhilHealthEmployerContribution,
                PagIBIGEmployerContribution = record.PagIBIGEmployerContribution,
                ECContribution = record.ECContribution,
                NetPay = record.NetPay,
                PaymentStatus = "Preview",
                CreatedAt = PhilippineTime.Now
            });
        }

        return results;
    }

    public async Task<PayrollRecordResponse> RecalculatePayrollRecordAsync(int payrollRecordId)
    {
        var record = await _context.PayrollRecords
            .Include(r => r.PayrollPeriod)
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.PayrollRecordId == payrollRecordId);

        if (record == null)
            throw new InvalidOperationException("Payroll record not found");

        if (record.PayrollPeriod.Status == "Paid")
            throw new InvalidOperationException("Cannot recalculate paid payroll record");

        // Recalculate earnings
        record.BasicSalary = CalculateBasicSalary(record.Employee, record.DaysWorked, record.PayrollPeriod.PayrollType);
        record.OvertimePay = CalculateOvertimePay(record.Employee.DailyRate, record.OvertimeHours);
        record.NightDifferentialPay = CalculateNightDifferentialPay(record.Employee.DailyRate, record.NightDifferentialHours);

        record.GrossPay = record.BasicSalary + record.OvertimePay + record.NightDifferentialPay +
            record.HolidayPay + record.RestDayPay + record.Commissions + record.Tips +
            record.RiceAllowance + record.LaundryAllowance + record.OtherAllowances;

        // Recalculate deductions
        await CalculateAndApplyDeductions(record, record.Employee.MonthlyBasicSalary, record.PayrollPeriod.PayrollType);

        record.UpdatedAt = PhilippineTime.Now;
        await _context.SaveChangesAsync();

        return MapToRecordResponse(record);
    }

    public async Task<PayrollRecordResponse> ProcessPaymentAsync(int payrollRecordId, ProcessPayrollPaymentRequest request)
    {
        var record = await _context.PayrollRecords
            .Include(r => r.PayrollPeriod)
            .FirstOrDefaultAsync(r => r.PayrollRecordId == payrollRecordId);

        if (record == null)
            throw new InvalidOperationException("Payroll record not found");

        if (record.PayrollPeriod.Status == "Draft")
            throw new InvalidOperationException("Payroll period must be finalized before processing payments");

        if (record.PaymentStatus == "Paid")
            throw new InvalidOperationException("Payment already processed");

        record.PaymentMethod = request.PaymentMethod;
        record.PaymentDate = request.PaymentDate ?? PhilippineTime.Now;
        record.PaymentStatus = "Paid";
        record.UpdatedAt = PhilippineTime.Now;

        await _context.SaveChangesAsync();
        return (await GetPayrollRecordByIdAsync(payrollRecordId))!;
    }

    public async Task<int> ProcessBulkPaymentAsync(BulkPaymentRequest request)
    {
        var period = await _context.PayrollPeriods.FindAsync(request.PayrollPeriodId);
        if (period == null)
            throw new InvalidOperationException("Payroll period not found");

        if (period.Status == "Draft")
            throw new InvalidOperationException("Payroll period must be finalized before processing payments");

        var recordsQuery = _context.PayrollRecords
            .Where(r => r.PayrollPeriodId == request.PayrollPeriodId)
            .Where(r => r.PaymentStatus == "Pending");

        if (request.RecordIds != null && request.RecordIds.Any())
            recordsQuery = recordsQuery.Where(r => request.RecordIds.Contains(r.PayrollRecordId));

        var records = await recordsQuery.ToListAsync();

        foreach (var record in records)
        {
            record.PaymentMethod = request.PaymentMethod;
            record.PaymentDate = request.PaymentDate ?? PhilippineTime.Now;
            record.PaymentStatus = "Paid";
            record.UpdatedAt = PhilippineTime.Now;
        }

        // Check if all records are paid, then update period status
        var allPaid = !await _context.PayrollRecords
            .Where(r => r.PayrollPeriodId == request.PayrollPeriodId)
            .AnyAsync(r => r.PaymentStatus == "Pending");

        if (allPaid)
        {
            period.Status = "Paid";
            period.UpdatedAt = PhilippineTime.Now;
        }

        await _context.SaveChangesAsync();
        return records.Count;
    }

    // ============================================================================
    // Contribution Calculations
    // ============================================================================

    public async Task<ContributionCalculationResponse> CalculateContributionsAsync(decimal monthlySalary)
    {
        var currentYear = PhilippineTime.Now.Year;

        // SSS Contribution
        var sssRate = await _context.SSSContributionRates
            .Where(r => r.EffectiveYear <= currentYear && monthlySalary >= r.MinSalary && monthlySalary <= r.MaxSalary)
            .OrderByDescending(r => r.EffectiveYear)
            .FirstOrDefaultAsync();

        var sssEmployee = sssRate?.EmployeeShare ?? 0;
        var sssEmployer = sssRate?.EmployerShare ?? 0;

        // PhilHealth Contribution
        var philHealthRate = await _context.PhilHealthContributionRates
            .Where(r => r.EffectiveYear <= currentYear)
            .OrderByDescending(r => r.EffectiveYear)
            .FirstOrDefaultAsync();

        var philHealthEmployee = Math.Min(monthlySalary * (philHealthRate?.EmployeeShare ?? 0.025m), 5000m);
        var philHealthEmployer = philHealthEmployee;

        // Pag-IBIG Contribution
        var pagibigRate = await _context.PagIBIGContributionRates
            .Where(r => r.EffectiveYear <= currentYear)
            .OrderByDescending(r => r.EffectiveYear)
            .FirstOrDefaultAsync();

        var pagibigEmployee = Math.Min(monthlySalary * (pagibigRate?.EmployeeRate ?? 0.02m), pagibigRate?.EmployeeMaxContribution ?? 200m);
        var pagibigEmployer = Math.Min(monthlySalary * (pagibigRate?.EmployerRate ?? 0.02m), pagibigRate?.EmployerMaxContribution ?? 200m);

        return new ContributionCalculationResponse
        {
            MonthlySalary = monthlySalary,
            SSSContribution = sssEmployee,
            SSSEmployerShare = sssEmployer,
            PhilHealthContribution = philHealthEmployee,
            PhilHealthEmployerShare = philHealthEmployer,
            PagIBIGContribution = pagibigEmployee,
            PagIBIGEmployerShare = pagibigEmployer,
            TotalEmployeeContributions = sssEmployee + philHealthEmployee + pagibigEmployee,
            TotalEmployerContributions = sssEmployer + philHealthEmployer + pagibigEmployer
        };
    }

    public async Task<TaxCalculationResponse> CalculateWithholdingTaxAsync(decimal taxableIncome)
    {
        var currentYear = PhilippineTime.Now.Year;

        var bracket = await _context.WithholdingTaxBrackets
            .Where(b => b.EffectiveYear <= currentYear && taxableIncome >= b.MinIncome && taxableIncome <= b.MaxIncome)
            .OrderByDescending(b => b.EffectiveYear)
            .FirstOrDefaultAsync();

        if (bracket == null)
        {
            return new TaxCalculationResponse
            {
                TaxableIncome = taxableIncome,
                TaxBracket = "Exempt",
                TaxRate = 0,
                WithholdingTax = 0,
                NetAfterTax = taxableIncome
            };
        }

        var excessIncome = taxableIncome - bracket.ExcessOver;
        var tax = bracket.BaseTax + (excessIncome * bracket.TaxRate);

        return new TaxCalculationResponse
        {
            TaxableIncome = taxableIncome,
            TaxBracket = $"₱{bracket.MinIncome:N0} - ₱{bracket.MaxIncome:N0}",
            TaxRate = bracket.TaxRate * 100,
            WithholdingTax = tax,
            NetAfterTax = taxableIncome - tax
        };
    }

    // ============================================================================
    // Reports & Summaries
    // ============================================================================

    public async Task<PayrollSummaryResponse> GetPayrollSummaryAsync(int payrollPeriodId)
    {
        var period = await _context.PayrollPeriods
            .Include(p => p.PayrollRecords)
            .FirstOrDefaultAsync(p => p.PayrollPeriodId == payrollPeriodId);

        if (period == null)
            throw new InvalidOperationException("Payroll period not found");

        var records = period.PayrollRecords.ToList();

        return new PayrollSummaryResponse
        {
            PayrollPeriodId = period.PayrollPeriodId,
            PeriodName = period.PeriodName,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            Status = period.Status,
            TotalEmployees = records.Count,
            PaidEmployees = records.Count(r => r.PaymentStatus == "Paid"),
            PendingEmployees = records.Count(r => r.PaymentStatus == "Pending"),
            TotalBasicSalary = records.Sum(r => r.BasicSalary),
            TotalOvertimePay = records.Sum(r => r.OvertimePay),
            TotalCommissions = records.Sum(r => r.Commissions),
            TotalTips = records.Sum(r => r.Tips),
            TotalAllowances = records.Sum(r => r.RiceAllowance + r.LaundryAllowance + r.OtherAllowances),
            TotalGrossPay = records.Sum(r => r.GrossPay),
            TotalSSS = records.Sum(r => r.SSSContribution),
            TotalPhilHealth = records.Sum(r => r.PhilHealthContribution),
            TotalPagIBIG = records.Sum(r => r.PagIBIGContribution),
            TotalWithholdingTax = records.Sum(r => r.WithholdingTax),
            TotalOtherDeductions = records.Sum(r => r.Tardiness + r.Absences + r.CashAdvances + r.LoanDeductions + r.OtherDeductions),
            TotalDeductions = records.Sum(r => r.TotalDeductions),
            TotalNetPay = records.Sum(r => r.NetPay)
        };
    }

    public async Task<EmployeePayrollHistoryResponse> GetEmployeePayrollHistoryAsync(int employeeId, int year)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
            throw new InvalidOperationException("Employee not found");

        var records = await _context.PayrollRecords
            .Include(r => r.PayrollPeriod)
            .Where(r => r.EmployeeId == employeeId && r.PayrollPeriod.StartDate.Year == year)
            .OrderByDescending(r => r.PayrollPeriod.StartDate)
            .Select(r => new PayrollRecordListResponse
            {
                PayrollRecordId = r.PayrollRecordId,
                EmployeeId = r.EmployeeId,
                EmployeeCode = r.Employee.EmployeeCode,
                EmployeeName = r.Employee.FirstName + " " + r.Employee.LastName,
                Position = r.Employee.Position,
                DaysWorked = r.DaysWorked,
                GrossPay = r.GrossPay,
                TotalDeductions = r.TotalDeductions,
                NetPay = r.NetPay,
                PaymentStatus = r.PaymentStatus
            })
            .ToListAsync();

        return new EmployeePayrollHistoryResponse
        {
            EmployeeId = employeeId,
            EmployeeCode = employee.EmployeeCode,
            EmployeeName = $"{employee.FirstName} {employee.LastName}",
            Records = records,
            TotalEarnings = records.Sum(r => r.GrossPay),
            TotalDeductions = records.Sum(r => r.TotalDeductions),
            TotalNetPay = records.Sum(r => r.NetPay),
            TotalPeriods = records.Count
        };
    }

    public async Task<MonthlyPayrollReportResponse> GetMonthlyPayrollReportAsync(int year, int month)
    {
        var startOfMonth = new DateTime(year, month, 1);
        var endOfMonth = startOfMonth.AddMonths(1);

        var periods = await _context.PayrollPeriods
            .Include(p => p.PayrollRecords)
            .Where(p => p.StartDate >= startOfMonth && p.StartDate < endOfMonth)
            .ToListAsync();

        var allRecords = periods.SelectMany(p => p.PayrollRecords).ToList();

        return new MonthlyPayrollReportResponse
        {
            Year = year,
            Month = month,
            MonthName = startOfMonth.ToString("MMMM"),
            EmployeeCount = allRecords.Select(r => r.EmployeeId).Distinct().Count(),
            TotalGrossPay = allRecords.Sum(r => r.GrossPay),
            TotalDeductions = allRecords.Sum(r => r.TotalDeductions),
            TotalNetPay = allRecords.Sum(r => r.NetPay),
            Periods = periods.Select(p => new PayrollPeriodListResponse
            {
                PayrollPeriodId = p.PayrollPeriodId,
                PeriodName = p.PeriodName,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                PayrollType = p.PayrollType,
                Status = p.Status,
                PaymentDate = p.PaymentDate,
                RecordCount = p.PayrollRecords.Count,
                TotalNetPay = p.PayrollRecords.Sum(r => r.NetPay)
            }).ToList()
        };
    }

    public async Task<ContributionReportResponse> GetContributionReportAsync(int year, int month)
    {
        var startOfMonth = new DateTime(year, month, 1);
        var endOfMonth = startOfMonth.AddMonths(1);

        var records = await _context.PayrollRecords
            .Include(r => r.PayrollPeriod)
            .Include(r => r.Employee)
            .Where(r => r.PayrollPeriod.StartDate >= startOfMonth && r.PayrollPeriod.StartDate < endOfMonth)
            .ToListAsync();

        // Group by employee to get monthly totals per employee
        var employeeDetails = records
            .GroupBy(r => r.EmployeeId)
            .Select(g => new EmployeeContributionDetail
            {
                EmployeeId = g.Key,
                EmployeeCode = g.First().Employee.EmployeeCode,
                EmployeeName = $"{g.First().Employee.FirstName} {g.First().Employee.LastName}",
                MonthlySalary = g.Sum(r => r.BasicSalary),
                SSS = g.Sum(r => r.SSSContribution),
                PhilHealth = g.Sum(r => r.PhilHealthContribution),
                PagIBIG = g.Sum(r => r.PagIBIGContribution),
                WithholdingTax = g.Sum(r => r.WithholdingTax)
            })
            .ToList();

        return new ContributionReportResponse
        {
            Year = year,
            Month = month,
            TotalSSS = records.Sum(r => r.SSSContribution),
            TotalSSSEmployer = records.Sum(r => r.SSSEmployerContribution),
            TotalPhilHealth = records.Sum(r => r.PhilHealthContribution),
            TotalPhilHealthEmployer = records.Sum(r => r.PhilHealthEmployerContribution),
            TotalPagIBIG = records.Sum(r => r.PagIBIGContribution),
            TotalPagIBIGEmployer = records.Sum(r => r.PagIBIGEmployerContribution),
            TotalWithholdingTax = records.Sum(r => r.WithholdingTax),
            EmployeeDetails = employeeDetails
        };
    }

    // ============================================================================
    // Payslip
    // ============================================================================

    public async Task<PayslipResponse> GeneratePayslipAsync(int payrollRecordId)
    {
        var record = await _context.PayrollRecords
            .Include(r => r.PayrollPeriod)
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.PayrollRecordId == payrollRecordId);

        if (record == null)
            throw new InvalidOperationException("Payroll record not found");

        var payslip = new PayslipResponse
        {
            PayrollPeriod = record.PayrollPeriod.PeriodName,
            PaymentDate = record.PaymentDate ?? record.PayrollPeriod.PaymentDate,
            EmployeeCode = record.Employee.EmployeeCode,
            EmployeeName = $"{record.Employee.FirstName} {record.Employee.LastName}",
            Position = record.Employee.Position,
            Department = record.Employee.Department ?? "",
            DaysWorked = record.DaysWorked,
            OvertimeHours = record.OvertimeHours,
            SSSNumber = record.Employee.SSSNumber,
            PhilHealthNumber = record.Employee.PhilHealthNumber,
            PagIBIGNumber = record.Employee.PagIBIGNumber,
            TINNumber = record.Employee.TINNumber
        };

        // Earnings
        payslip.Earnings.Add(new PayslipEarningItem { Description = "Basic Salary", Amount = record.BasicSalary });
        if (record.OvertimePay > 0)
            payslip.Earnings.Add(new PayslipEarningItem { Description = "Overtime Pay", Amount = record.OvertimePay });
        if (record.NightDifferentialPay > 0)
            payslip.Earnings.Add(new PayslipEarningItem { Description = "Night Differential", Amount = record.NightDifferentialPay });
        if (record.HolidayPay > 0)
            payslip.Earnings.Add(new PayslipEarningItem { Description = "Holiday Pay", Amount = record.HolidayPay });
        if (record.RestDayPay > 0)
            payslip.Earnings.Add(new PayslipEarningItem { Description = "Rest Day Pay", Amount = record.RestDayPay });
        if (record.Commissions > 0)
            payslip.Earnings.Add(new PayslipEarningItem { Description = "Commissions", Amount = record.Commissions });
        if (record.Tips > 0)
            payslip.Earnings.Add(new PayslipEarningItem { Description = "Tips", Amount = record.Tips });
        if (record.RiceAllowance > 0)
            payslip.Earnings.Add(new PayslipEarningItem { Description = "Rice Allowance", Amount = record.RiceAllowance });
        if (record.LaundryAllowance > 0)
            payslip.Earnings.Add(new PayslipEarningItem { Description = "Laundry Allowance", Amount = record.LaundryAllowance });
        if (record.OtherAllowances > 0)
            payslip.Earnings.Add(new PayslipEarningItem { Description = "Other Allowances", Amount = record.OtherAllowances });

        payslip.TotalEarnings = record.GrossPay;

        // Deductions
        if (record.SSSContribution > 0)
            payslip.Deductions.Add(new PayslipDeductionItem { Description = "SSS Contribution", Amount = record.SSSContribution });
        if (record.PhilHealthContribution > 0)
            payslip.Deductions.Add(new PayslipDeductionItem { Description = "PhilHealth", Amount = record.PhilHealthContribution });
        if (record.PagIBIGContribution > 0)
            payslip.Deductions.Add(new PayslipDeductionItem { Description = "Pag-IBIG", Amount = record.PagIBIGContribution });
        if (record.WithholdingTax > 0)
            payslip.Deductions.Add(new PayslipDeductionItem { Description = "Withholding Tax", Amount = record.WithholdingTax });
        if (record.Tardiness > 0)
            payslip.Deductions.Add(new PayslipDeductionItem { Description = "Tardiness", Amount = record.Tardiness });
        if (record.Absences > 0)
            payslip.Deductions.Add(new PayslipDeductionItem { Description = "Absences", Amount = record.Absences });
        if (record.CashAdvances > 0)
            payslip.Deductions.Add(new PayslipDeductionItem { Description = "Cash Advance", Amount = record.CashAdvances });
        if (record.LoanDeductions > 0)
            payslip.Deductions.Add(new PayslipDeductionItem { Description = "Loan Deduction", Amount = record.LoanDeductions });
        if (record.OtherDeductions > 0)
            payslip.Deductions.Add(new PayslipDeductionItem { Description = "Other Deductions", Amount = record.OtherDeductions });

        payslip.TotalDeductions = record.TotalDeductions;
        payslip.NetPay = record.NetPay;

        return payslip;
    }

    // ============================================================================
    // Helper Methods
    // ============================================================================

    private decimal CalculateBasicSalary(Core.Entities.Employee.Employee employee, decimal daysWorked, string payrollType)
    {
        // Use DailyRate × DaysWorked — the standard Philippine payroll calculation.
        // DailyRate is already derived from MonthlyBasicSalary based on the employee's schedule.
        return employee.DailyRate * daysWorked;
    }

    private decimal CalculateOvertimePay(decimal dailyRate, decimal overtimeHours)
    {
        // OT rate: 125% of hourly rate (regular OT)
        var hourlyRate = dailyRate / 8m;
        return hourlyRate * 1.25m * overtimeHours;
    }

    private decimal CalculateNightDifferentialPay(decimal dailyRate, decimal nightDiffHours)
    {
        // Night diff rate: 10% additional
        var hourlyRate = dailyRate / 8m;
        return hourlyRate * 0.10m * nightDiffHours;
    }

    private async Task CalculateAndApplyDeductions(PayrollRecord record, decimal monthlySalary, string payrollType)
    {
        // For semi-monthly, contributions are split across two periods
        var contributionFactor = payrollType == "Semi-Monthly" ? 0.5m : 1m;

        var contributions = await CalculateContributionsAsync(monthlySalary);

        // Employee share
        record.SSSContribution = contributions.SSSContribution * contributionFactor;
        record.PhilHealthContribution = contributions.PhilHealthContribution * contributionFactor;
        record.PagIBIGContribution = contributions.PagIBIGContribution * contributionFactor;

        // Employer share (stored for reporting/remittance)
        record.SSSEmployerContribution = contributions.SSSEmployerShare * contributionFactor;
        record.PhilHealthEmployerContribution = contributions.PhilHealthEmployerShare * contributionFactor;
        record.PagIBIGEmployerContribution = contributions.PagIBIGEmployerShare * contributionFactor;
        record.ECContribution = contributions.SSSEmployerShare * 0.10m * contributionFactor; // EC is ~10% of SSS employer share

        // Calculate withholding tax based on taxable income
        // BIR: Taxable = Gross - mandatory employee contributions
        var taxableIncome = record.GrossPay - record.SSSContribution - record.PhilHealthContribution - record.PagIBIGContribution;
        // For semi-monthly, project to monthly for correct bracket lookup, then halve result
        var monthlyTaxable = taxableIncome * (payrollType == "Semi-Monthly" ? 2m : 1m);
        var taxResult = await CalculateWithholdingTaxAsync(monthlyTaxable);
        record.WithholdingTax = taxResult.WithholdingTax * contributionFactor;

        // Calculate total deductions
        record.TotalDeductions = record.SSSContribution + record.PhilHealthContribution +
            record.PagIBIGContribution + record.WithholdingTax + record.Tardiness +
            record.Absences + record.CashAdvances + record.LoanDeductions + record.OtherDeductions;

        // Calculate net pay
        record.NetPay = record.GrossPay - record.TotalDeductions;
    }

    private async Task<(decimal DaysWorked, decimal HoursWorked, decimal OvertimeHours, decimal NightDifferentialHours)> CalculateWorkDaysAsync(
        int employeeId, DateTime startDate, DateTime endDate)
    {
        var records = await _context.AttendanceRecords
            .Where(r => r.EmployeeId == employeeId
                && r.Date >= startDate
                && r.Date <= endDate
                && r.Status == "ClockedOut")
            .ToListAsync();

        if (!records.Any())
        {
            // Fallback: approximate if no attendance data exists
            var totalDays = (endDate - startDate).Days + 1;
            var workDays = Math.Round((decimal)totalDays * 5m / 7m, 2);
            return (workDays, workDays * 8, 0, 0);
        }

        var daysWorked = (decimal)records.Count;
        var hoursWorked = records.Sum(r => r.TotalHours);
        var overtimeHours = records.Sum(r => r.TotalHours > 8m ? r.TotalHours - 8m : 0m);
        var nightDiffHours = records
            .Where(r => r.ClockIn.HasValue && r.ClockOut.HasValue)
            .Sum(r => CalculateNightDiffHours(r.ClockIn!.Value, r.ClockOut!.Value));

        return (daysWorked, hoursWorked, overtimeHours, nightDiffHours);
    }

    /// <summary>
    /// Calculates hours worked between 10 PM and 6 AM (DOLE night differential window).
    /// </summary>
    private static decimal CalculateNightDiffHours(DateTime clockIn, DateTime clockOut)
    {
        decimal nightMinutes = 0m;
        var day = clockIn.Date;
        var endDay = clockOut.Date.AddDays(1);

        while (day < endDay)
        {
            // 10 PM to midnight of this calendar day
            var seg1Start = day.AddHours(22);
            var seg1End = day.AddDays(1);
            // Midnight to 6 AM of this calendar day
            var seg2Start = day;
            var seg2End = day.AddHours(6);

            foreach (var (segStart, segEnd) in new[] { (seg1Start, seg1End), (seg2Start, seg2End) })
            {
                var overlapStart = clockIn > segStart ? clockIn : segStart;
                var overlapEnd = clockOut < segEnd ? clockOut : segEnd;
                if (overlapEnd > overlapStart)
                    nightMinutes += (decimal)(overlapEnd - overlapStart).TotalMinutes;
            }

            day = day.AddDays(1);
        }

        return Math.Round(nightMinutes / 60m, 2);
    }

    private async Task<(decimal Commissions, decimal Tips)> CalculateCommissionsAndTipsAsync(
        int employeeId, DateTime startDate, DateTime endDate)
    {
        // Get employee's transactions in the period
        var serviceItems = await _context.Set<Core.Entities.Transaction.TransactionServiceItem>()
            .Include(si => si.Transaction)
            .Where(si => si.TherapistId == employeeId)
            .Where(si => si.Transaction.TransactionDate >= startDate && si.Transaction.TransactionDate <= endDate)
            .Where(si => si.Transaction.PaymentStatus == "Paid")
            .ToListAsync();

        var commissions = serviceItems.Sum(si => si.CommissionAmount);

        // Tips are tracked at transaction level - load to memory first to avoid
        // nested aggregate SQL error (cannot SUM a subquery result in SQL Server)
        var tipTransactions = await _context.Transactions
            .Where(t => t.ServiceItems.Any(si => si.TherapistId == employeeId))
            .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
            .Where(t => t.PaymentStatus == "Paid")
            .Select(t => new { t.TipAmount, ServiceItemCount = t.ServiceItems.Count })
            .ToListAsync();

        var tips = tipTransactions.Sum(t => t.ServiceItemCount > 0 ? t.TipAmount / t.ServiceItemCount : 0m);

        return (commissions, tips);
    }

    private PayrollPeriodResponse MapToPeriodResponse(PayrollPeriod period)
    {
        return new PayrollPeriodResponse
        {
            PayrollPeriodId = period.PayrollPeriodId,
            PeriodName = period.PeriodName,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            PayrollType = period.PayrollType,
            CutoffDate = period.CutoffDate,
            PaymentDate = period.PaymentDate,
            Status = period.Status,
            FinalizedBy = period.FinalizedBy,
            FinalizedByName = period.FinalizedByUser != null
                ? $"{period.FinalizedByUser.FirstName} {period.FinalizedByUser.LastName}"
                : null,
            FinalizedAt = period.FinalizedAt,
            RecordCount = period.PayrollRecords.Count,
            TotalGrossPay = period.PayrollRecords.Sum(r => r.GrossPay),
            TotalNetPay = period.PayrollRecords.Sum(r => r.NetPay),
            CreatedAt = period.CreatedAt
        };
    }

    private PayrollRecordResponse MapToRecordResponse(PayrollRecord record)
    {
        return new PayrollRecordResponse
        {
            PayrollRecordId = record.PayrollRecordId,
            PayrollPeriodId = record.PayrollPeriodId,
            PeriodName = record.PayrollPeriod?.PeriodName ?? "",
            EmployeeId = record.EmployeeId,
            EmployeeCode = record.Employee?.EmployeeCode ?? "",
            EmployeeName = record.Employee != null
                ? $"{record.Employee.FirstName} {record.Employee.LastName}"
                : "",
            Position = record.Employee?.Position ?? "",
            DaysWorked = record.DaysWorked,
            HoursWorked = record.HoursWorked,
            OvertimeHours = record.OvertimeHours,
            NightDifferentialHours = record.NightDifferentialHours,
            BasicSalary = record.BasicSalary,
            OvertimePay = record.OvertimePay,
            NightDifferentialPay = record.NightDifferentialPay,
            HolidayPay = record.HolidayPay,
            RestDayPay = record.RestDayPay,
            Commissions = record.Commissions,
            Tips = record.Tips,
            RiceAllowance = record.RiceAllowance,
            LaundryAllowance = record.LaundryAllowance,
            OtherAllowances = record.OtherAllowances,
            GrossPay = record.GrossPay,
            SSSContribution = record.SSSContribution,
            PhilHealthContribution = record.PhilHealthContribution,
            PagIBIGContribution = record.PagIBIGContribution,
            WithholdingTax = record.WithholdingTax,
            Tardiness = record.Tardiness,
            Absences = record.Absences,
            CashAdvances = record.CashAdvances,
            LoanDeductions = record.LoanDeductions,
            OtherDeductions = record.OtherDeductions,
            TotalDeductions = record.TotalDeductions,
            SSSEmployerContribution = record.SSSEmployerContribution,
            PhilHealthEmployerContribution = record.PhilHealthEmployerContribution,
            PagIBIGEmployerContribution = record.PagIBIGEmployerContribution,
            ECContribution = record.ECContribution,
            NetPay = record.NetPay,
            PaymentMethod = record.PaymentMethod,
            PaymentDate = record.PaymentDate,
            PaymentStatus = record.PaymentStatus,
            CreatedAt = record.CreatedAt
        };
    }

    // ============================================================================
    // 13th Month Pay
    // ============================================================================

    /// <summary>
    /// Calculate 13th month pay for an employee.
    /// Per Philippine DOLE: 13th Month Pay = Total Basic Salary Earned During the Year / 12
    /// </summary>
    public async Task<ThirteenthMonthPayResponse> CalculateThirteenthMonthPayAsync(int employeeId, int year)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
            throw new InvalidOperationException("Employee not found");

        // Get all payroll records for this employee in the given year
        var records = await _context.PayrollRecords
            .Include(r => r.PayrollPeriod)
            .Where(r => r.EmployeeId == employeeId)
            .Where(r => r.PayrollPeriod.StartDate.Year == year)
            .Where(r => r.PayrollPeriod.Status != "Draft") // Only finalized/paid periods
            .OrderBy(r => r.PayrollPeriod.StartDate)
            .ToListAsync();

        // Group by month to get monthly basic salary totals
        var monthlyBreakdown = records
            .GroupBy(r => r.PayrollPeriod.StartDate.Month)
            .Select(g => new MonthlyBasicSalaryDetail
            {
                Month = g.Key,
                MonthName = new DateTime(year, g.Key, 1).ToString("MMMM"),
                BasicSalary = g.Sum(r => r.BasicSalary)
            })
            .OrderBy(m => m.Month)
            .ToList();

        var totalBasicSalary = monthlyBreakdown.Sum(m => m.BasicSalary);
        var thirteenthMonthPay = totalBasicSalary / 12m;

        return new ThirteenthMonthPayResponse
        {
            EmployeeId = employeeId,
            EmployeeCode = employee.EmployeeCode,
            EmployeeName = $"{employee.FirstName} {employee.LastName}",
            Year = year,
            TotalBasicSalaryEarned = totalBasicSalary,
            MonthsCovered = monthlyBreakdown.Count,
            ThirteenthMonthPay = Math.Round(thirteenthMonthPay, 2),
            MonthlyBreakdown = monthlyBreakdown
        };
    }

    /// <summary>
    /// Calculate 13th month pay for all active employees.
    /// </summary>
    public async Task<List<ThirteenthMonthPayResponse>> CalculateThirteenthMonthPayAllAsync(int year)
    {
        var employees = await _context.Employees
            .Where(e => e.IsActive)
            .ToListAsync();

        var results = new List<ThirteenthMonthPayResponse>();
        foreach (var employee in employees)
        {
            var result = await CalculateThirteenthMonthPayAsync(employee.EmployeeId, year);
            results.Add(result);
        }

        return results;
    }

    // =========================================================================
    // Bank File Export
    // =========================================================================

    public async Task<byte[]> GenerateBankFileAsync(int payrollPeriodId)
    {
        var period = await _context.PayrollPeriods.FindAsync(payrollPeriodId)
            ?? throw new ArgumentException("Payroll period not found");

        var records = await _context.PayrollRecords
            .Include(r => r.Employee)
            .Where(r => r.PayrollPeriodId == payrollPeriodId && r.PaymentStatus != "Pending")
            .OrderBy(r => r.Employee.LastName)
            .ThenBy(r => r.Employee.FirstName)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Employee Code,Employee Name,Bank Name,Account Number,Net Pay,Payment Method");
        foreach (var record in records)
        {
            var emp = record.Employee;
            var bankName = emp.BankName ?? "";
            var accountNumber = emp.BankAccountNumber ?? "";
            // CSV-safe escaping
            var empName = $"{emp.LastName}, {emp.FirstName}";
            sb.AppendLine($"{emp.EmployeeCode},\"{empName}\",\"{bankName}\",\"{accountNumber}\",{record.NetPay:F2},{record.PaymentMethod ?? "Cash"}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
