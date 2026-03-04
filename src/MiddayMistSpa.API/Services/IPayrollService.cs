using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.API.DTOs.Payroll;

namespace MiddayMistSpa.API.Services;

public interface IPayrollService
{
    // ============================================================================
    // Payroll Period Management
    // ============================================================================

    Task<PayrollPeriodResponse> CreatePayrollPeriodAsync(CreatePayrollPeriodRequest request);
    Task<PayrollPeriodResponse?> GetPayrollPeriodByIdAsync(int payrollPeriodId);
    Task<PagedResponse<PayrollPeriodListResponse>> SearchPayrollPeriodsAsync(PayrollPeriodSearchRequest request);
    Task<PayrollPeriodResponse> UpdatePayrollPeriodAsync(int payrollPeriodId, UpdatePayrollPeriodRequest request);
    Task<PayrollPeriodResponse> FinalizePayrollPeriodAsync(int payrollPeriodId, int finalizedByUserId);
    Task<bool> DeletePayrollPeriodAsync(int payrollPeriodId);

    // ============================================================================
    // Payroll Record Management
    // ============================================================================

    Task<PayrollRecordResponse> CreatePayrollRecordAsync(CreatePayrollRecordRequest request);
    Task<PayrollRecordResponse?> GetPayrollRecordByIdAsync(int payrollRecordId);
    Task<PagedResponse<PayrollRecordResponse>> GetPayrollRecordsByPeriodAsync(int periodId, int page, int pageSize);
    Task<PagedResponse<PayrollRecordListResponse>> SearchPayrollRecordsAsync(PayrollRecordSearchRequest request);
    Task<PayrollRecordResponse> UpdatePayrollRecordAsync(int payrollRecordId, UpdatePayrollRecordRequest request);
    Task<bool> DeletePayrollRecordAsync(int payrollRecordId);

    // ============================================================================
    // Payroll Generation & Processing
    // ============================================================================

    Task<List<PayrollRecordResponse>> GeneratePayrollAsync(GeneratePayrollRequest request);
    Task<PayrollRecordResponse> RecalculatePayrollRecordAsync(int payrollRecordId);
    Task<PayrollRecordResponse> ProcessPaymentAsync(int payrollRecordId, ProcessPayrollPaymentRequest request);
    Task<int> ProcessBulkPaymentAsync(BulkPaymentRequest request);

    // ============================================================================
    // Contribution Calculations
    // ============================================================================

    Task<ContributionCalculationResponse> CalculateContributionsAsync(decimal monthlySalary);
    Task<TaxCalculationResponse> CalculateWithholdingTaxAsync(decimal taxableIncome);

    // ============================================================================
    // Reports & Summaries
    // ============================================================================

    Task<PayrollSummaryResponse> GetPayrollSummaryAsync(int payrollPeriodId);
    Task<EmployeePayrollHistoryResponse> GetEmployeePayrollHistoryAsync(int employeeId, int year);
    Task<MonthlyPayrollReportResponse> GetMonthlyPayrollReportAsync(int year, int month);
    Task<ContributionReportResponse> GetContributionReportAsync(int year, int month);

    // ============================================================================
    // Payslip
    // ============================================================================

    Task<PayslipResponse> GeneratePayslipAsync(int payrollRecordId);

    // ============================================================================
    // 13th Month Pay
    // ============================================================================

    Task<ThirteenthMonthPayResponse> CalculateThirteenthMonthPayAsync(int employeeId, int year);
    Task<List<ThirteenthMonthPayResponse>> CalculateThirteenthMonthPayAllAsync(int year);
}
