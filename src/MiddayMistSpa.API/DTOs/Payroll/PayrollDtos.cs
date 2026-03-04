using System.ComponentModel.DataAnnotations;
using MiddayMistSpa.API.DTOs.Employee;

namespace MiddayMistSpa.API.DTOs.Payroll;

// ============================================================================
// Payroll Period DTOs
// ============================================================================

public class CreatePayrollPeriodRequest
{
    [Required, StringLength(100)]
    public string PeriodName { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required, StringLength(20)]
    public string PayrollType { get; set; } = "Semi-Monthly"; // Semi-Monthly, Monthly

    [Required]
    public DateTime CutoffDate { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }
}

public class UpdatePayrollPeriodRequest
{
    [StringLength(100)]
    public string? PeriodName { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? CutoffDate { get; set; }
    public DateTime? PaymentDate { get; set; }
}

public class PayrollPeriodResponse
{
    public int PayrollPeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string PayrollType { get; set; } = string.Empty;
    public DateTime CutoffDate { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? FinalizedBy { get; set; }
    public string? FinalizedByName { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public int RecordCount { get; set; }
    public decimal TotalGrossPay { get; set; }
    public decimal TotalNetPay { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PayrollPeriodListResponse
{
    public int PayrollPeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string PayrollType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public int RecordCount { get; set; }
    public decimal TotalNetPay { get; set; }
    public string? FinalizedByName { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ============================================================================
// Payroll Record DTOs
// ============================================================================

public class CreatePayrollRecordRequest
{
    [Required]
    public int PayrollPeriodId { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Range(0, 31)]
    public decimal DaysWorked { get; set; }

    [Range(0, 500)]
    public decimal HoursWorked { get; set; }

    [Range(0, 200)]
    public decimal OvertimeHours { get; set; } = 0;

    [Range(0, 200)]
    public decimal NightDifferentialHours { get; set; } = 0;

    // Manual overrides (optional - calculated automatically if not provided)
    public decimal? BasicSalary { get; set; }
    public decimal? Commissions { get; set; }
    public decimal? Tips { get; set; }
    public decimal? RiceAllowance { get; set; }
    public decimal? LaundryAllowance { get; set; }
    public decimal? OtherAllowances { get; set; }
}

public class UpdatePayrollRecordRequest
{
    [Range(0, 31)]
    public decimal? DaysWorked { get; set; }

    [Range(0, 500)]
    public decimal? HoursWorked { get; set; }

    [Range(0, 200)]
    public decimal? OvertimeHours { get; set; }

    [Range(0, 200)]
    public decimal? NightDifferentialHours { get; set; }

    public decimal? BasicSalary { get; set; }
    public decimal? OvertimePay { get; set; }
    public decimal? NightDifferentialPay { get; set; }
    public decimal? HolidayPay { get; set; }
    public decimal? RestDayPay { get; set; }
    public decimal? Commissions { get; set; }
    public decimal? Tips { get; set; }
    public decimal? RiceAllowance { get; set; }
    public decimal? LaundryAllowance { get; set; }
    public decimal? OtherAllowances { get; set; }

    // Deduction overrides
    public decimal? Tardiness { get; set; }
    public decimal? Absences { get; set; }
    public decimal? CashAdvances { get; set; }
    public decimal? LoanDeductions { get; set; }
    public decimal? OtherDeductions { get; set; }
}

public class PayrollRecordResponse
{
    public int PayrollRecordId { get; set; }
    public int PayrollPeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;

    // Work Hours
    public decimal DaysWorked { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal NightDifferentialHours { get; set; }

    // Earnings
    public decimal BasicSalary { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal NightDifferentialPay { get; set; }
    public decimal HolidayPay { get; set; }
    public decimal RestDayPay { get; set; }
    public decimal Commissions { get; set; }
    public decimal Tips { get; set; }
    public decimal RiceAllowance { get; set; }
    public decimal LaundryAllowance { get; set; }
    public decimal OtherAllowances { get; set; }
    public decimal GrossPay { get; set; }

    // Deductions
    public decimal SSSContribution { get; set; }
    public decimal PhilHealthContribution { get; set; }
    public decimal PagIBIGContribution { get; set; }
    public decimal WithholdingTax { get; set; }
    public decimal Tardiness { get; set; }
    public decimal Absences { get; set; }
    public decimal CashAdvances { get; set; }
    public decimal LoanDeductions { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal TotalDeductions { get; set; }

    // Employer Contributions
    public decimal SSSEmployerContribution { get; set; }
    public decimal PhilHealthEmployerContribution { get; set; }
    public decimal PagIBIGEmployerContribution { get; set; }
    public decimal ECContribution { get; set; }

    // Net Pay
    public decimal NetPay { get; set; }

    // Payment
    public string? PaymentMethod { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class PayrollRecordListResponse
{
    public int PayrollRecordId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public decimal DaysWorked { get; set; }
    public decimal GrossPay { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
}

// ============================================================================
// Payroll Processing DTOs
// ============================================================================

public class GeneratePayrollRequest
{
    [Required]
    public int PayrollPeriodId { get; set; }

    public List<int>? EmployeeIds { get; set; } // If null, generates for all active employees
}

public class ProcessPayrollPaymentRequest
{
    [Required, StringLength(20)]
    public string PaymentMethod { get; set; } = "Cash"; // Cash, Bank Transfer

    public DateTime? PaymentDate { get; set; }
}

public class BulkPaymentRequest
{
    [Required]
    public int PayrollPeriodId { get; set; }

    [Required, StringLength(20)]
    public string PaymentMethod { get; set; } = "Cash";

    public DateTime? PaymentDate { get; set; }

    public List<int>? RecordIds { get; set; } // If null, pays all pending records
}

// ============================================================================
// Search & Filter DTOs
// ============================================================================

public class PayrollPeriodSearchRequest
{
    public string? SearchTerm { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? Status { get; set; }
    public string? PayrollType { get; set; }
    public string? SortBy { get; set; } = "date";
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PayrollRecordSearchRequest
{
    public int? PayrollPeriodId { get; set; }
    public int? EmployeeId { get; set; }
    public string? PaymentStatus { get; set; }
    public string? SortBy { get; set; } = "name";
    public bool SortDescending { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

// ============================================================================
// Contribution & Tax DTOs
// ============================================================================

public class ContributionCalculationResponse
{
    public decimal MonthlySalary { get; set; }
    public decimal SSSContribution { get; set; }
    public decimal SSSEmployerShare { get; set; }
    public decimal PhilHealthContribution { get; set; }
    public decimal PhilHealthEmployerShare { get; set; }
    public decimal PagIBIGContribution { get; set; }
    public decimal PagIBIGEmployerShare { get; set; }
    public decimal TotalEmployeeContributions { get; set; }
    public decimal TotalEmployerContributions { get; set; }
}

public class TaxCalculationResponse
{
    public decimal TaxableIncome { get; set; }
    public string TaxBracket { get; set; } = string.Empty;
    public decimal TaxRate { get; set; }
    public decimal WithholdingTax { get; set; }
    public decimal NetAfterTax { get; set; }
}

// ============================================================================
// Payroll Summary & Reports DTOs
// ============================================================================

public class PayrollSummaryResponse
{
    public int PayrollPeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;

    public int TotalEmployees { get; set; }
    public int PaidEmployees { get; set; }
    public int PendingEmployees { get; set; }

    // Totals
    public decimal TotalBasicSalary { get; set; }
    public decimal TotalOvertimePay { get; set; }
    public decimal TotalCommissions { get; set; }
    public decimal TotalTips { get; set; }
    public decimal TotalAllowances { get; set; }
    public decimal TotalGrossPay { get; set; }

    // Deduction Totals
    public decimal TotalSSS { get; set; }
    public decimal TotalPhilHealth { get; set; }
    public decimal TotalPagIBIG { get; set; }
    public decimal TotalWithholdingTax { get; set; }
    public decimal TotalOtherDeductions { get; set; }
    public decimal TotalDeductions { get; set; }

    public decimal TotalNetPay { get; set; }
}

public class EmployeePayrollHistoryResponse
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public List<PayrollRecordListResponse> Records { get; set; } = new();
    public decimal TotalEarnings { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNetPay { get; set; }
    public int TotalPeriods { get; set; }
}

public class MonthlyPayrollReportResponse
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public decimal TotalGrossPay { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNetPay { get; set; }
    public List<PayrollPeriodListResponse> Periods { get; set; } = new();
}

public class ContributionReportResponse
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalSSS { get; set; }
    public decimal TotalSSSEmployer { get; set; }
    public decimal TotalPhilHealth { get; set; }
    public decimal TotalPhilHealthEmployer { get; set; }
    public decimal TotalPagIBIG { get; set; }
    public decimal TotalPagIBIGEmployer { get; set; }
    public decimal TotalWithholdingTax { get; set; }
    public List<EmployeeContributionDetail> EmployeeDetails { get; set; } = new();
}

public class EmployeeContributionDetail
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public decimal MonthlySalary { get; set; }
    public decimal SSS { get; set; }
    public decimal PhilHealth { get; set; }
    public decimal PagIBIG { get; set; }
    public decimal WithholdingTax { get; set; }
}

// ============================================================================
// Payslip DTOs
// ============================================================================

public class PayslipResponse
{
    // Header
    public string BusinessName { get; set; } = "MiddayMist Spa";
    public string PayrollPeriod { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }

    // Employee Info
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;

    // Attendance
    public decimal DaysWorked { get; set; }
    public decimal OvertimeHours { get; set; }

    // Earnings
    public List<PayslipEarningItem> Earnings { get; set; } = new();
    public decimal TotalEarnings { get; set; }

    // Deductions
    public List<PayslipDeductionItem> Deductions { get; set; } = new();
    public decimal TotalDeductions { get; set; }

    // Net Pay
    public decimal NetPay { get; set; }

    // Government IDs (for remittance reference)
    public string? SSSNumber { get; set; }
    public string? PhilHealthNumber { get; set; }
    public string? PagIBIGNumber { get; set; }
    public string? TINNumber { get; set; }
}

public class PayslipEarningItem
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class PayslipDeductionItem
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

// ============================================================================
// 13th Month Pay DTOs
// ============================================================================

public class ThirteenthMonthPayResponse
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal TotalBasicSalaryEarned { get; set; }
    public int MonthsCovered { get; set; }
    public decimal ThirteenthMonthPay { get; set; }
    public List<MonthlyBasicSalaryDetail> MonthlyBreakdown { get; set; } = new();
}

public class MonthlyBasicSalaryDetail
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal BasicSalary { get; set; }
}
