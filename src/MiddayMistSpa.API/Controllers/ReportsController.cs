using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiddayMistSpa.API.DTOs.Report;
using MiddayMistSpa.API.Services;
using System.Security.Claims;

namespace MiddayMistSpa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportingService _reportingService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportingService reportingService, ILogger<ReportsController> logger)
    {
        _reportingService = reportingService;
        _logger = logger;
    }

    // ============================================================================
    // Dashboard Endpoints
    // ============================================================================

    [HttpGet("dashboard")]
    [Authorize(Policy = "Permission:reports.view")]
    public async Task<ActionResult<DashboardResponse>> GetDashboard([FromQuery] DashboardRequest request)
    {
        try
        {
            var result = await _reportingService.GetDashboardAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard");
            return StatusCode(500, new { error = "An error occurred while getting the dashboard" });
        }
    }

    [HttpGet("quick-stats")]
    [Authorize(Policy = "Permission:reports.view")]
    public async Task<ActionResult<QuickStatsResponse>> GetQuickStats()
    {
        try
        {
            var result = await _reportingService.GetQuickStatsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quick stats");
            return StatusCode(500, new { error = "An error occurred while getting quick stats" });
        }
    }

    // ============================================================================
    // Sales Report Endpoints
    // ============================================================================

    [HttpGet("sales")]
    [Authorize(Policy = "Permission:reports.view")]
    public async Task<ActionResult<SalesReportResponse>> GetSalesReport([FromQuery] SalesReportRequest request)
    {
        try
        {
            var result = await _reportingService.GetSalesReportAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales report");
            return StatusCode(500, new { error = "An error occurred while getting the sales report" });
        }
    }

    // ============================================================================
    // Service Performance Endpoints
    // ============================================================================

    [HttpGet("services/performance")]
    [Authorize(Policy = "Permission:reports.view")]
    public async Task<ActionResult<ServicePerformanceResponse>> GetServicePerformance([FromQuery] ServicePerformanceRequest request)
    {
        try
        {
            var result = await _reportingService.GetServicePerformanceReportAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service performance report");
            return StatusCode(500, new { error = "An error occurred while getting the service performance report" });
        }
    }

    // ============================================================================
    // Employee Performance Endpoints
    // ============================================================================

    [HttpGet("employees/performance")]
    [Authorize(Policy = "Permission:reports.view")]
    public async Task<ActionResult<EmployeePerformanceResponse>> GetEmployeePerformance([FromQuery] EmployeePerformanceRequest request)
    {
        try
        {
            var result = await _reportingService.GetEmployeePerformanceReportAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employee performance report");
            return StatusCode(500, new { error = "An error occurred while getting the employee performance report" });
        }
    }

    // ============================================================================
    // Customer Analytics Endpoints
    // ============================================================================

    [HttpGet("customers/analytics")]
    [Authorize(Policy = "Permission:reports.view")]
    public async Task<ActionResult<CustomerAnalyticsResponse>> GetCustomerAnalytics([FromQuery] CustomerAnalyticsRequest request)
    {
        try
        {
            var result = await _reportingService.GetCustomerAnalyticsReportAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer analytics report");
            return StatusCode(500, new { error = "An error occurred while getting the customer analytics report" });
        }
    }

    // ============================================================================
    // Inventory Report Endpoints
    // ============================================================================

    [HttpGet("inventory")]
    [Authorize(Policy = "Permission:reports.view")]
    public async Task<ActionResult<InventoryReportResponse>> GetInventoryReport([FromQuery] InventoryReportRequest request)
    {
        try
        {
            var result = await _reportingService.GetInventoryReportAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory report");
            return StatusCode(500, new { error = "An error occurred while getting the inventory report" });
        }
    }

    // ============================================================================
    // Payroll Summary Endpoints
    // ============================================================================

    [HttpGet("payroll")]
    [Authorize(Policy = "Permission:reports.view")]
    public async Task<ActionResult<PayrollSummaryReportResponse>> GetPayrollSummary([FromQuery] PayrollSummaryRequest request)
    {
        try
        {
            var result = await _reportingService.GetPayrollSummaryReportAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payroll summary report");
            return StatusCode(500, new { error = "An error occurred while getting the payroll summary report" });
        }
    }

    // ============================================================================
    // Financial Summary Endpoints
    // ============================================================================

    [HttpGet("financial")]
    [Authorize(Policy = "Permission:reports.view")]
    public async Task<ActionResult<FinancialSummaryResponse>> GetFinancialSummary([FromQuery] FinancialSummaryRequest request)
    {
        try
        {
            var result = await _reportingService.GetFinancialSummaryReportAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting financial summary report");
            return StatusCode(500, new { error = "An error occurred while getting the financial summary report" });
        }
    }

    // ============================================================================
    // Appointment Analytics Endpoints
    // ============================================================================

    [HttpGet("appointments/analytics")]
    [Authorize(Policy = "Permission:reports.view")]
    public async Task<ActionResult<AppointmentAnalyticsResponse>> GetAppointmentAnalytics([FromQuery] AppointmentAnalyticsRequest request)
    {
        try
        {
            var result = await _reportingService.GetAppointmentAnalyticsReportAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting appointment analytics report");
            return StatusCode(500, new { error = "An error occurred while getting the appointment analytics report" });
        }
    }

    // ============================================================================
    // Export Endpoints
    // ============================================================================

    [HttpPost("export")]
    [Authorize(Policy = "Permission:reports.export")]
    public async Task<IActionResult> ExportReport([FromBody] ExportRequest request)
    {
        try
        {
            // Populate user info from JWT claims
            var firstName = User.FindFirstValue(ClaimTypes.GivenName) ?? "";
            var lastName = User.FindFirstValue(ClaimTypes.Surname) ?? "";
            var fullName = $"{firstName} {lastName}".Trim();
            request.GeneratedByName = string.IsNullOrEmpty(fullName) ? (User.Identity?.Name ?? "Unknown") : fullName;
            request.GeneratedByRole = User.FindFirstValue(ClaimTypes.Role) ?? "Unknown";

            var result = await _reportingService.ExportReportAsync(request);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting report");
            return StatusCode(500, new { error = "An error occurred while exporting the report" });
        }
    }
}
