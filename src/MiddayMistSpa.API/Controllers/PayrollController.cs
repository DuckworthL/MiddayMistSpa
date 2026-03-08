using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.API.DTOs.Payroll;
using MiddayMistSpa.API.Services;
using MiddayMistSpa.Infrastructure.Data;
using System.Security.Claims;

namespace MiddayMistSpa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PayrollController : ControllerBase
{
    private readonly IPayrollService _payrollService;
    private readonly SpaDbContext _context;
    private readonly ILogger<PayrollController> _logger;

    public PayrollController(IPayrollService payrollService, SpaDbContext context, ILogger<PayrollController> logger)
    {
        _payrollService = payrollService;
        _context = context;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("User identity not found");
        return userId;
    }

    // ============================================================================
    // Payroll Period Endpoints
    // ============================================================================

    [HttpPost("periods")]
    [Authorize(Policy = "Permission:payroll.manage")]
    public async Task<ActionResult<PayrollPeriodResponse>> CreatePayrollPeriod([FromBody] CreatePayrollPeriodRequest request)
    {
        try
        {
            var result = await _payrollService.CreatePayrollPeriodAsync(request);
            return CreatedAtAction(nameof(GetPayrollPeriod), new { id = result.PayrollPeriodId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payroll period");
            return StatusCode(500, new { error = "An error occurred while creating the payroll period" });
        }
    }

    [HttpGet("periods/{id}")]
    [Authorize(Policy = "Permission:payroll.view")]
    public async Task<ActionResult<PayrollPeriodResponse>> GetPayrollPeriod(int id)
    {
        var result = await _payrollService.GetPayrollPeriodByIdAsync(id);
        if (result == null)
            return NotFound(new { error = "Payroll period not found" });

        return Ok(result);
    }

    [HttpGet("periods")]
    [Authorize(Policy = "Permission:payroll.view")]
    public async Task<ActionResult<PagedResponse<PayrollPeriodListResponse>>> SearchPayrollPeriods([FromQuery] PayrollPeriodSearchRequest request)
    {
        var result = await _payrollService.SearchPayrollPeriodsAsync(request);
        return Ok(result);
    }

    [HttpPut("periods/{id}")]
    [Authorize(Policy = "Permission:payroll.manage")]
    public async Task<ActionResult<PayrollPeriodResponse>> UpdatePayrollPeriod(int id, [FromBody] UpdatePayrollPeriodRequest request)
    {
        try
        {
            var result = await _payrollService.UpdatePayrollPeriodAsync(id, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payroll period {PayrollPeriodId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the payroll period" });
        }
    }

    [HttpPost("periods/{id}/finalize")]
    [Authorize(Policy = "Permission:payroll.manage")]
    public async Task<ActionResult<PayrollPeriodResponse>> FinalizePayrollPeriod(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _payrollService.FinalizePayrollPeriodAsync(id, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing payroll period {PayrollPeriodId}", id);
            return StatusCode(500, new { error = "An error occurred while finalizing the payroll period" });
        }
    }

    [HttpPost("periods/{id}/reopen")]
    [Authorize(Policy = "Permission:payroll.manage")]
    public async Task<ActionResult<PayrollPeriodResponse>> ReopenPayrollPeriod(int id)
    {
        try
        {
            var result = await _payrollService.ReopenPayrollPeriodAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reopening payroll period {PayrollPeriodId}", id);
            return StatusCode(500, new { error = "An error occurred while reopening the payroll period" });
        }
    }

    [HttpDelete("periods/{id}")]
    [Authorize(Policy = "Permission:payroll.manage")]
    public async Task<ActionResult> DeletePayrollPeriod(int id)
    {
        try
        {
            var result = await _payrollService.DeletePayrollPeriodAsync(id);
            if (!result)
                return NotFound(new { error = "Payroll period not found" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payroll period {PayrollPeriodId}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the payroll period" });
        }
    }

    // ============================================================================
    // Payroll Record Endpoints
    // ============================================================================

    [HttpGet("periods/{id}/records")]
    [Authorize(Policy = "Permission:payroll.view")]
    public async Task<ActionResult<PagedResponse<PayrollRecordResponse>>> GetPeriodRecords(
        int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var result = await _payrollService.GetPayrollRecordsByPeriodAsync(id, page, pageSize);
        return Ok(result);
    }

    [HttpPost("records")]
    [Authorize(Policy = "Permission:payroll.manage")]
    public async Task<ActionResult<PayrollRecordResponse>> CreatePayrollRecord([FromBody] CreatePayrollRecordRequest request)
    {
        try
        {
            var result = await _payrollService.CreatePayrollRecordAsync(request);
            return CreatedAtAction(nameof(GetPayrollRecord), new { id = result.PayrollRecordId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payroll record");
            return StatusCode(500, new { error = "An error occurred while creating the payroll record" });
        }
    }

    [HttpGet("records/{id}")]
    [Authorize(Policy = "Permission:payroll.view")]
    public async Task<ActionResult<PayrollRecordResponse>> GetPayrollRecord(int id)
    {
        var result = await _payrollService.GetPayrollRecordByIdAsync(id);
        if (result == null)
            return NotFound(new { error = "Payroll record not found" });

        return Ok(result);
    }

    [HttpGet("records")]
    [Authorize(Policy = "Permission:payroll.view")]
    public async Task<ActionResult<PagedResponse<PayrollRecordListResponse>>> SearchPayrollRecords([FromQuery] PayrollRecordSearchRequest request)
    {
        var result = await _payrollService.SearchPayrollRecordsAsync(request);
        return Ok(result);
    }

    [HttpPut("records/{id}")]
    [Authorize(Policy = "Permission:payroll.manage")]
    public async Task<ActionResult<PayrollRecordResponse>> UpdatePayrollRecord(int id, [FromBody] UpdatePayrollRecordRequest request)
    {
        try
        {
            var result = await _payrollService.UpdatePayrollRecordAsync(id, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payroll record {PayrollRecordId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the payroll record" });
        }
    }

    [HttpDelete("records/{id}")]
    [Authorize(Policy = "Permission:payroll.manage")]
    public async Task<ActionResult> DeletePayrollRecord(int id)
    {
        try
        {
            var result = await _payrollService.DeletePayrollRecordAsync(id);
            if (!result)
                return NotFound(new { error = "Payroll record not found" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payroll record {PayrollRecordId}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the payroll record" });
        }
    }

    // ============================================================================
    // Generation & Processing Endpoints
    // ============================================================================

    [HttpPost("generate")]
    [Authorize(Policy = "Permission:payroll.manage")]
    public async Task<ActionResult<List<PayrollRecordResponse>>> GeneratePayroll([FromBody] GeneratePayrollRequest request)
    {
        try
        {
            var result = await _payrollService.GeneratePayrollAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payroll for period {PayrollPeriodId}", request.PayrollPeriodId);
            return StatusCode(500, new { error = "An error occurred while generating payroll" });
        }
    }

    [HttpPost("records/{id}/recalculate")]
    [Authorize(Policy = "Permission:payroll.manage")]
    public async Task<ActionResult<PayrollRecordResponse>> RecalculatePayrollRecord(int id)
    {
        try
        {
            var result = await _payrollService.RecalculatePayrollRecordAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating payroll record {PayrollRecordId}", id);
            return StatusCode(500, new { error = "An error occurred while recalculating the payroll record" });
        }
    }

    [HttpPost("records/{id}/pay")]
    [Authorize(Policy = "Permission:payroll.manage")]
    public async Task<ActionResult<PayrollRecordResponse>> ProcessPayment(int id, [FromBody] ProcessPayrollPaymentRequest request)
    {
        try
        {
            var result = await _payrollService.ProcessPaymentAsync(id, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for payroll record {PayrollRecordId}", id);
            return StatusCode(500, new { error = "An error occurred while processing the payment" });
        }
    }

    [HttpPost("periods/{id}/pay-all")]
    [Authorize(Policy = "Permission:payroll.manage")]
    public async Task<ActionResult> ProcessBulkPayment(int id, [FromBody] BulkPaymentRequest request)
    {
        try
        {
            request.PayrollPeriodId = id;
            var count = await _payrollService.ProcessBulkPaymentAsync(request);
            return Ok(new { message = $"Successfully processed payment for {count} records" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bulk payment for period {PayrollPeriodId}", id);
            return StatusCode(500, new { error = "An error occurred while processing bulk payment" });
        }
    }

    // ============================================================================
    // Payroll Preview (read-only estimate)
    // ============================================================================

    [HttpGet("preview")]
    [Authorize(Policy = "Permission:payroll.view")]
    public async Task<ActionResult<List<PayrollRecordResponse>>> PreviewPayroll(
        [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            var result = await _payrollService.PreviewPayrollAsync(startDate, endDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payroll preview");
            return StatusCode(500, new { error = "An error occurred while generating the payroll preview" });
        }
    }

    // ============================================================================
    // Calculation Endpoints
    // ============================================================================

    [HttpGet("calculate/contributions")]
    [Authorize(Policy = "Permission:payroll.view")]
    public async Task<ActionResult<ContributionCalculationResponse>> CalculateContributions([FromQuery] decimal monthlySalary)
    {
        var result = await _payrollService.CalculateContributionsAsync(monthlySalary);
        return Ok(result);
    }

    [HttpGet("calculate/tax")]
    [Authorize(Policy = "Permission:payroll.view")]
    public async Task<ActionResult<TaxCalculationResponse>> CalculateWithholdingTax([FromQuery] decimal taxableIncome)
    {
        var result = await _payrollService.CalculateWithholdingTaxAsync(taxableIncome);
        return Ok(result);
    }

    // ============================================================================
    // Report Endpoints
    // ============================================================================

    [HttpGet("periods/{id}/summary")]
    [Authorize(Policy = "Permission:payroll.view")]
    public async Task<ActionResult<PayrollSummaryResponse>> GetPayrollSummary(int id)
    {
        try
        {
            var result = await _payrollService.GetPayrollSummaryAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payroll summary for period {PayrollPeriodId}", id);
            return StatusCode(500, new { error = "An error occurred while getting the payroll summary" });
        }
    }

    [HttpGet("employees/{employeeId}/history")]
    [Authorize(Policy = "Permission:payroll.view")]
    public async Task<ActionResult<EmployeePayrollHistoryResponse>> GetEmployeePayrollHistory(int employeeId, [FromQuery] int year)
    {
        try
        {
            if (year == 0) year = DateTime.UtcNow.Year;
            var result = await _payrollService.GetEmployeePayrollHistoryAsync(employeeId, year);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payroll history for employee {EmployeeId}", employeeId);
            return StatusCode(500, new { error = "An error occurred while getting the payroll history" });
        }
    }

    [HttpGet("reports/monthly")]
    [Authorize(Policy = "Permission:payroll.view")]
    public async Task<ActionResult<MonthlyPayrollReportResponse>> GetMonthlyPayrollReport([FromQuery] int year, [FromQuery] int month)
    {
        if (year == 0) year = DateTime.UtcNow.Year;
        if (month == 0) month = DateTime.UtcNow.Month;

        var result = await _payrollService.GetMonthlyPayrollReportAsync(year, month);
        return Ok(result);
    }

    [HttpGet("reports/contributions")]
    [Authorize(Policy = "Permission:payroll.view")]
    public async Task<ActionResult<ContributionReportResponse>> GetContributionReport([FromQuery] int year, [FromQuery] int month)
    {
        if (year == 0) year = DateTime.UtcNow.Year;
        if (month == 0) month = DateTime.UtcNow.Month;

        var result = await _payrollService.GetContributionReportAsync(year, month);
        return Ok(result);
    }

    // ============================================================================
    // Payslip Endpoints
    // ============================================================================

    [HttpGet("records/{id}/payslip")]
    [Authorize(Policy = "Permission:payroll.view")]
    public async Task<ActionResult<PayslipResponse>> GetPayslip(int id)
    {
        try
        {
            var result = await _payrollService.GeneratePayslipAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payslip for payroll record {PayrollRecordId}", id);
            return StatusCode(500, new { error = "An error occurred while generating the payslip" });
        }
    }

    [HttpGet("my-payslips")]
    public async Task<ActionResult<PagedResponse<PayrollRecordListResponse>>> GetMyPayslips([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        // Look up the Employee linked to this UserId (they are separate IDs)
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employee == null)
            return NotFound(new { error = "No employee record linked to your user account" });

        var request = new PayrollRecordSearchRequest
        {
            EmployeeId = employee.EmployeeId,
            Page = page,
            PageSize = pageSize
        };

        var result = await _payrollService.SearchPayrollRecordsAsync(request);
        return Ok(result);
    }

    // ============================================================================
    // 13th Month Pay Endpoints
    // ============================================================================

    [HttpGet("thirteenth-month/{employeeId}/{year}")]
    [Authorize(Policy = "Permission:payroll.view")]
    public async Task<ActionResult<ThirteenthMonthPayResponse>> GetThirteenthMonthPay(int employeeId, int year)
    {
        try
        {
            var result = await _payrollService.CalculateThirteenthMonthPayAsync(employeeId, year);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating 13th month pay for employee {EmployeeId}, year {Year}", employeeId, year);
            return StatusCode(500, new { error = "An error occurred while calculating 13th month pay" });
        }
    }

    [HttpGet("thirteenth-month/{year}")]
    [Authorize(Policy = "Permission:payroll.view")]
    public async Task<ActionResult<List<ThirteenthMonthPayResponse>>> GetThirteenthMonthPayAll(int year)
    {
        try
        {
            var result = await _payrollService.CalculateThirteenthMonthPayAllAsync(year);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating 13th month pay for all employees, year {Year}", year);
            return StatusCode(500, new { error = "An error occurred while calculating 13th month pay" });
        }
    }

    // =========================================================================
    // Bank File Export
    // =========================================================================

    [HttpPost("bank-file/{periodId}")]
    [Authorize(Policy = "Permission:payroll.manage")]
    public async Task<IActionResult> DownloadBankFile(int periodId)
    {
        try
        {
            var fileContent = await _payrollService.GenerateBankFileAsync(periodId);
            return File(fileContent, "text/csv", $"payroll-bank-file-{periodId}.csv");
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating bank file for period {PeriodId}", periodId);
            return StatusCode(500, new { error = "An error occurred while generating the bank file" });
        }
    }
}
