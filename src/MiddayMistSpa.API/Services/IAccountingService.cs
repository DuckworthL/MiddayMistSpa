using MiddayMistSpa.API.DTOs.Accounting;
using MiddayMistSpa.API.DTOs.Employee;

namespace MiddayMistSpa.API.Services;

public interface IAccountingService
{
    // Chart of Accounts
    Task<ChartOfAccountResponse?> GetAccountByIdAsync(int accountId);
    Task<PagedResponse<ChartOfAccountResponse>> SearchAccountsAsync(AccountSearchRequest request);
    Task<List<ChartOfAccountResponse>> GetAccountsHierarchyAsync();
    Task<ChartOfAccountResponse> CreateAccountAsync(CreateAccountRequest request);
    Task<ChartOfAccountResponse> UpdateAccountAsync(int accountId, UpdateAccountRequest request);
    Task<bool> DeleteAccountAsync(int accountId);

    // Journal Entries
    Task<JournalEntryResponse?> GetJournalEntryByIdAsync(int journalEntryId);
    Task<PagedResponse<JournalEntryResponse>> SearchJournalEntriesAsync(JournalEntrySearchRequest request);
    Task<JournalEntryResponse> CreateJournalEntryAsync(CreateJournalEntryRequest request, int userId);
    Task<JournalEntryResponse> PostJournalEntryAsync(int journalEntryId, int userId);
    Task<JournalEntryResponse> VoidJournalEntryAsync(int journalEntryId, int userId, string? reason = null);

    // Auto Journal Entries (from operations)
    Task CreateTransactionJournalEntryAsync(int transactionId, int userId);
    Task CreateRefundJournalEntryAsync(int transactionId, int refundId, decimal refundAmount, string refundMethod, string reason, int userId);
    Task CreatePayrollJournalEntryAsync(int payrollPeriodId, int userId);
    Task CreatePurchaseOrderJournalEntryAsync(int purchaseOrderId, int userId);
    Task CreateInvoicePaymentIncomeAsync(int invoiceId, decimal paymentAmount, int userId);

    // Financial Reports
    Task<TrialBalanceResponse> GetTrialBalanceAsync(DateTime asOfDate);
    Task<IncomeStatementResponse> GetIncomeStatementAsync(DateTime startDate, DateTime endDate);
    Task<BalanceSheetResponse> GetBalanceSheetAsync(DateTime asOfDate);
    Task<AccountLedgerResponse> GetAccountLedgerAsync(int accountId, DateTime startDate, DateTime endDate);

    // Expense/Income Tracking
    Task<PagedResponse<ExpenseResponse>> GetExpensesAsync(DateTime? startDate, DateTime? endDate, int pageNumber, int pageSize);
    Task<ExpenseResponse> CreateExpenseAsync(CreateExpenseRequest request, int userId);
    Task<PagedResponse<IncomeRecordResponse>> GetIncomeRecordsAsync(DateTime? startDate, DateTime? endDate, int pageNumber, int pageSize);
    Task<IncomeRecordResponse> CreateIncomeRecordAsync(CreateIncomeRequest request, int userId);

    // Dashboard/Summary
    Task<AccountingSummaryResponse> GetAccountingSummaryAsync(DateTime startDate, DateTime endDate);

    // Sub-page Summaries
    Task<ExpenseSummaryResponse> GetExpenseSummaryAsync();
    Task<IncomeSummaryResponse> GetIncomeSummaryAsync();
    Task<JournalSummaryResponse> GetJournalSummaryAsync();
}
