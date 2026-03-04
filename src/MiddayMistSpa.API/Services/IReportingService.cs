using MiddayMistSpa.API.DTOs.Report;

namespace MiddayMistSpa.API.Services;

public interface IReportingService
{
    // ============================================================================
    // Dashboard
    // ============================================================================
    Task<DashboardResponse> GetDashboardAsync(DashboardRequest request);
    Task<QuickStatsResponse> GetQuickStatsAsync();

    // ============================================================================
    // Sales Reports
    // ============================================================================
    Task<SalesReportResponse> GetSalesReportAsync(SalesReportRequest request);

    // ============================================================================
    // Service Performance
    // ============================================================================
    Task<ServicePerformanceResponse> GetServicePerformanceReportAsync(ServicePerformanceRequest request);

    // ============================================================================
    // Employee Performance
    // ============================================================================
    Task<EmployeePerformanceResponse> GetEmployeePerformanceReportAsync(EmployeePerformanceRequest request);

    // ============================================================================
    // Customer Analytics
    // ============================================================================
    Task<CustomerAnalyticsResponse> GetCustomerAnalyticsReportAsync(CustomerAnalyticsRequest request);

    // ============================================================================
    // Inventory Reports
    // ============================================================================
    Task<InventoryReportResponse> GetInventoryReportAsync(InventoryReportRequest request);

    // ============================================================================
    // Payroll Summary
    // ============================================================================
    Task<PayrollSummaryReportResponse> GetPayrollSummaryReportAsync(PayrollSummaryRequest request);

    // ============================================================================
    // Financial Summary
    // ============================================================================
    Task<FinancialSummaryResponse> GetFinancialSummaryReportAsync(FinancialSummaryRequest request);

    // ============================================================================
    // Appointment Analytics
    // ============================================================================
    Task<AppointmentAnalyticsResponse> GetAppointmentAnalyticsReportAsync(AppointmentAnalyticsRequest request);

    // ============================================================================
    // Export
    // ============================================================================
    Task<ExportResponse> ExportReportAsync(ExportRequest request);
}
