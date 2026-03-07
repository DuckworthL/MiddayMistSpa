using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.API.DTOs.Transaction;
using MiddayMistSpa.API.Services;

namespace MiddayMistSpa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var id))
            throw new UnauthorizedAccessException("User identity not found");
        return id;
    }

    // ============================================================================
    // Transaction CRUD
    // ============================================================================

    [HttpPost]
    [Authorize(Policy = "Permission:pos.access")]
    public async Task<ActionResult<TransactionResponse>> CreateTransaction([FromBody] CreateTransactionRequest request)
    {
        try
        {
            var cashierId = GetCurrentUserId();
            var result = await _transactionService.CreateTransactionAsync(request, cashierId);
            return CreatedAtAction(nameof(GetTransaction), new { id = result.TransactionId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{appointmentId}/pending")]
    [Authorize(Policy = "Permission:pos.access")]
    public async Task<ActionResult<TransactionResponse>> CreatePendingTransaction(int appointmentId)
    {
        try
        {
            var cashierId = GetCurrentUserId();
            var result = await _transactionService.CreatePendingTransactionForAppointmentAsync(appointmentId, cashierId);
            return CreatedAtAction(nameof(GetTransaction), new { id = result.TransactionId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/finalize")]
    [Authorize(Policy = "Permission:pos.access")]
    public async Task<ActionResult<TransactionResponse>> FinalizePendingTransaction(int id, [FromBody] FinalizePendingTransactionRequest request)
    {
        try
        {
            var cashierId = GetCurrentUserId();
            var result = await _transactionService.FinalizePendingTransactionAsync(id, request, cashierId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("by-appointment/{appointmentId}/pending")]
    public async Task<ActionResult<TransactionResponse>> GetPendingByAppointment(int appointmentId)
    {
        var result = await _transactionService.GetPendingTransactionByAppointmentAsync(appointmentId);
        if (result == null)
            return NotFound(new { error = "No pending transaction found for this appointment" });
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionResponse>> GetTransaction(int id)
    {
        var result = await _transactionService.GetTransactionByIdAsync(id);
        if (result == null)
            return NotFound(new { error = "Transaction not found" });

        return Ok(result);
    }

    [HttpGet("by-number/{transactionNumber}")]
    public async Task<ActionResult<TransactionResponse>> GetTransactionByNumber(string transactionNumber)
    {
        var result = await _transactionService.GetTransactionByNumberAsync(transactionNumber);
        if (result == null)
            return NotFound(new { error = "Transaction not found" });

        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<TransactionListResponse>>> SearchTransactions([FromQuery] TransactionSearchRequest request)
    {
        var result = await _transactionService.SearchTransactionsAsync(request);
        return Ok(result);
    }

    // ============================================================================
    // Payment Processing
    // ============================================================================

    [HttpPost("{id}/payment")]
    [Authorize(Policy = "Permission:pos.access")]
    public async Task<ActionResult<TransactionResponse>> ProcessPayment(int id, [FromBody] ProcessPaymentRequest request)
    {
        try
        {
            var result = await _transactionService.ProcessPaymentAsync(id, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/void")]
    [Authorize(Policy = "Permission:pos.refund")]
    public async Task<ActionResult<TransactionResponse>> VoidTransaction(int id, [FromBody] VoidTransactionRequest request)
    {
        try
        {
            var voidedById = GetCurrentUserId();
            var result = await _transactionService.VoidTransactionAsync(id, request, voidedById);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ============================================================================
    // Refunds
    // ============================================================================

    [HttpPost("{id}/refund")]
    [Authorize(Policy = "Permission:pos.refund")]
    public async Task<ActionResult<RefundResponse>> ProcessRefund(int id, [FromBody] CreateRefundRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _transactionService.ProcessRefundAsync(id, request, currentUserId, currentUserId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}/refunds")]
    public async Task<ActionResult<List<RefundResponse>>> GetRefunds(int id)
    {
        var result = await _transactionService.GetRefundsByTransactionAsync(id);
        return Ok(result);
    }

    // ============================================================================
    // Customer History
    // ============================================================================

    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<PagedResponse<TransactionListResponse>>> GetCustomerTransactions(
        int customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _transactionService.GetCustomerTransactionsAsync(customerId, page, pageSize);
        return Ok(result);
    }

    // ============================================================================
    // Reports & Dashboard
    // ============================================================================

    [HttpGet("stats")]
    [Authorize(Policy = "Permission:pos.access")]
    public async Task<ActionResult<TransactionStatsResponse>> GetTransactionStats()
    {
        var result = await _transactionService.GetTransactionStatsAsync();
        return Ok(result);
    }

    [HttpGet("dashboard")]
    [Authorize(Policy = "Permission:pos.access")]
    public async Task<ActionResult<POSDashboardResponse>> GetPOSDashboard([FromQuery] DateTime? date)
    {
        var targetDate = date ?? DateTime.UtcNow;
        var result = await _transactionService.GetPOSDashboardAsync(targetDate);
        return Ok(result);
    }

    [HttpGet("reports/daily")]
    [Authorize(Policy = "Permission:reports.view")]
    public async Task<ActionResult<DailySalesReportResponse>> GetDailySalesReport([FromQuery] DateTime? date)
    {
        var targetDate = date ?? DateTime.UtcNow;
        var result = await _transactionService.GetDailySalesReportAsync(targetDate);
        return Ok(result);
    }

    [HttpGet("reports/sales")]
    [Authorize(Policy = "Permission:reports.view")]
    public async Task<ActionResult<TransactionSalesReportResponse>> GetSalesReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var result = await _transactionService.GetSalesReportAsync(startDate, endDate);
        return Ok(result);
    }

    [HttpGet("reports/cashier/{cashierId}")]
    [Authorize(Policy = "Permission:reports.view")]
    public async Task<ActionResult<CashierShiftReportResponse>> GetCashierShiftReport(int cashierId, [FromQuery] DateTime? date)
    {
        try
        {
            var targetDate = date ?? DateTime.UtcNow;
            var result = await _transactionService.GetCashierShiftReportAsync(cashierId, targetDate);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    // ============================================================================
    // Receipt
    // ============================================================================

    [HttpGet("{id}/receipt")]
    public async Task<ActionResult<ReceiptResponse>> GetReceipt(int id)
    {
        try
        {
            var result = await _transactionService.GenerateReceiptAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
