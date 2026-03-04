using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiddayMistSpa.API.DTOs.Accounting;
using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.API.Services;
using System.Security.Claims;

namespace MiddayMistSpa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountingController : ControllerBase
{
    private readonly IAccountingService _accountingService;
    private readonly ILogger<AccountingController> _logger;

    public AccountingController(IAccountingService accountingService, ILogger<AccountingController> logger)
    {
        _accountingService = accountingService;
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
    // Chart of Accounts Endpoints
    // ============================================================================

    [HttpGet("accounts")]
    [Authorize(Roles = "SuperAdmin,Admin,Accountant")]
    public async Task<ActionResult<PagedResponse<ChartOfAccountResponse>>> GetAccounts([FromQuery] AccountSearchRequest request)
    {
        var result = await _accountingService.SearchAccountsAsync(request);
        return Ok(result);
    }

    [HttpGet("accounts/hierarchy")]
    [Authorize(Roles = "SuperAdmin,Admin,Accountant")]
    public async Task<ActionResult<List<ChartOfAccountResponse>>> GetAccountsHierarchy()
    {
        var result = await _accountingService.GetAccountsHierarchyAsync();
        return Ok(result);
    }

    [HttpGet("accounts/{id}")]
    [Authorize(Roles = "SuperAdmin,Admin,Accountant")]
    public async Task<ActionResult<ChartOfAccountResponse>> GetAccount(int id)
    {
        var result = await _accountingService.GetAccountByIdAsync(id);
        if (result == null)
            return NotFound(new { error = "Account not found" });
        return Ok(result);
    }

    [HttpPost("accounts")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<ChartOfAccountResponse>> CreateAccount([FromBody] CreateAccountRequest request)
    {
        try
        {
            var result = await _accountingService.CreateAccountAsync(request);
            return CreatedAtAction(nameof(GetAccount), new { id = result.AccountId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account");
            return StatusCode(500, new { error = "An error occurred while creating the account" });
        }
    }

    [HttpPut("accounts/{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<ChartOfAccountResponse>> UpdateAccount(int id, [FromBody] UpdateAccountRequest request)
    {
        try
        {
            var result = await _accountingService.UpdateAccountAsync(id, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating account {AccountId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the account" });
        }
    }

    [HttpDelete("accounts/{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult> DeleteAccount(int id)
    {
        try
        {
            var result = await _accountingService.DeleteAccountAsync(id);
            if (!result)
                return NotFound(new { error = "Account not found" });
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ============================================================================
    // Journal Entry Endpoints
    // ============================================================================

    [HttpGet("journal-entries")]
    [Authorize(Roles = "SuperAdmin,Admin,Accountant")]
    public async Task<ActionResult<PagedResponse<JournalEntryResponse>>> GetJournalEntries([FromQuery] JournalEntrySearchRequest request)
    {
        var result = await _accountingService.SearchJournalEntriesAsync(request);
        return Ok(result);
    }

    [HttpGet("journal-entries/{id}")]
    [Authorize(Roles = "SuperAdmin,Admin,Accountant")]
    public async Task<ActionResult<JournalEntryResponse>> GetJournalEntry(int id)
    {
        var result = await _accountingService.GetJournalEntryByIdAsync(id);
        if (result == null)
            return NotFound(new { error = "Journal entry not found" });
        return Ok(result);
    }

    [HttpPost("journal-entries")]
    [Authorize(Roles = "SuperAdmin,Admin,Accountant")]
    public async Task<ActionResult<JournalEntryResponse>> CreateJournalEntry([FromBody] CreateJournalEntryRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _accountingService.CreateJournalEntryAsync(request, userId);
            return CreatedAtAction(nameof(GetJournalEntry), new { id = result.JournalEntryId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating journal entry");
            return StatusCode(500, new { error = "An error occurred while creating the journal entry" });
        }
    }

    [HttpPost("journal-entries/{id}/post")]
    [Authorize(Roles = "SuperAdmin,Admin,Accountant")]
    public async Task<ActionResult<JournalEntryResponse>> PostJournalEntry(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _accountingService.PostJournalEntryAsync(id, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting journal entry {JournalEntryId}", id);
            return StatusCode(500, new { error = "An error occurred while posting the journal entry" });
        }
    }

    [HttpPost("journal-entries/{id}/void")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<JournalEntryResponse>> VoidJournalEntry(int id, [FromBody] VoidJournalEntryRequest? request = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _accountingService.VoidJournalEntryAsync(id, userId, request?.Reason);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error voiding journal entry {JournalEntryId}", id);
            return StatusCode(500, new { error = "An error occurred while voiding the journal entry" });
        }
    }

    // ============================================================================
    // Financial Reports Endpoints
    // ============================================================================

    [HttpGet("reports/trial-balance")]
    [Authorize(Roles = "SuperAdmin,Admin,Accountant")]
    public async Task<ActionResult<TrialBalanceResponse>> GetTrialBalance([FromQuery] DateTime? asOfDate)
    {
        var date = asOfDate ?? DateTime.Today;
        var result = await _accountingService.GetTrialBalanceAsync(date);
        return Ok(result);
    }

    [HttpGet("reports/income-statement")]
    [Authorize(Roles = "SuperAdmin,Admin,Accountant")]
    public async Task<ActionResult<IncomeStatementResponse>> GetIncomeStatement([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var result = await _accountingService.GetIncomeStatementAsync(startDate, endDate);
        return Ok(result);
    }

    [HttpGet("reports/balance-sheet")]
    [Authorize(Roles = "SuperAdmin,Admin,Accountant")]
    public async Task<ActionResult<BalanceSheetResponse>> GetBalanceSheet([FromQuery] DateTime? asOfDate)
    {
        var date = asOfDate ?? DateTime.Today;
        var result = await _accountingService.GetBalanceSheetAsync(date);
        return Ok(result);
    }

    [HttpGet("accounts/{id}/ledger")]
    [Authorize(Roles = "SuperAdmin,Admin,Accountant")]
    public async Task<ActionResult<AccountLedgerResponse>> GetAccountLedger(int id, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            var result = await _accountingService.GetAccountLedgerAsync(id, startDate, endDate);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    // ============================================================================
    // Expense Endpoints
    // ============================================================================

    [HttpGet("expenses")]
    [Authorize(Roles = "SuperAdmin,Admin,Accountant")]
    public async Task<ActionResult<PagedResponse<ExpenseResponse>>> GetExpenses(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _accountingService.GetExpensesAsync(startDate, endDate, pageNumber, pageSize);
        return Ok(result);
    }

    [HttpPost("expenses")]
    [Authorize(Roles = "SuperAdmin,Admin,Accountant")]
    public async Task<ActionResult<ExpenseResponse>> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _accountingService.CreateExpenseAsync(request, userId);
            return CreatedAtAction(nameof(GetJournalEntry), new { id = result.JournalEntryId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            return StatusCode(500, new { error = "An error occurred while creating the expense" });
        }
    }

    // ============================================================================
    // Income Endpoints
    // ============================================================================

    [HttpGet("income")]
    [Authorize(Roles = "SuperAdmin,Admin,Accountant")]
    public async Task<ActionResult<PagedResponse<IncomeRecordResponse>>> GetIncomeRecords(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _accountingService.GetIncomeRecordsAsync(startDate, endDate, pageNumber, pageSize);
        return Ok(result);
    }

    [HttpPost("income")]
    [Authorize(Roles = "SuperAdmin,Admin,Accountant")]
    public async Task<ActionResult<IncomeRecordResponse>> CreateIncomeRecord([FromBody] CreateIncomeRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _accountingService.CreateIncomeRecordAsync(request, userId);
            return CreatedAtAction(nameof(GetJournalEntry), new { id = result.JournalEntryId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating income record");
            return StatusCode(500, new { error = "An error occurred while creating the income record" });
        }
    }

    // ============================================================================
    // Dashboard/Summary Endpoints
    // ============================================================================

    [HttpGet("summary")]
    [Authorize(Roles = "SuperAdmin,Admin,Accountant")]
    public async Task<ActionResult<AccountingSummaryResponse>> GetAccountingSummary([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var start = startDate ?? new DateTime(DateTime.Today.Year, 1, 1);
        var end = endDate ?? DateTime.Today;
        var result = await _accountingService.GetAccountingSummaryAsync(start, end);
        return Ok(result);
    }
}
