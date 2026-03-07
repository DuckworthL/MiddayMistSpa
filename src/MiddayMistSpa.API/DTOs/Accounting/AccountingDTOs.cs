using System.ComponentModel.DataAnnotations;

namespace MiddayMistSpa.API.DTOs.Accounting;

// ============================================================================
// Chart of Accounts DTOs
// ============================================================================

public class ChartOfAccountResponse
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string NormalBalance { get; set; } = string.Empty;
    public string? AccountCategory { get; set; }
    public int? ParentAccountId { get; set; }
    public string? ParentAccountName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal Balance { get; set; }
    public List<ChartOfAccountResponse> ChildAccounts { get; set; } = new();
}

public class CreateAccountRequest
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string? NormalBalance { get; set; } // Auto-derived from AccountType if not provided
    public string? AccountCategory { get; set; }
    public int? ParentAccountId { get; set; }
}

public class UpdateAccountRequest
{
    public string? AccountName { get; set; }
    public string? AccountCategory { get; set; }
    public bool? IsActive { get; set; }
}

public class AccountSearchRequest
{
    public string? SearchTerm { get; set; }
    public string? AccountType { get; set; }
    public bool? IsActive { get; set; }
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;
    [Range(1, 100)]
    public int PageSize { get; set; } = 50;
}

// ============================================================================
// Journal Entry DTOs
// ============================================================================

public class JournalEntryResponse
{
    public int JournalEntryId { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string? Description { get; set; }
    public string? ReferenceType { get; set; }
    public string? ReferenceId { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public bool IsBalanced { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? VoidedBy { get; set; }
    public string? VoidedByName { get; set; }
    public DateTime? VoidedAt { get; set; }
    public string? VoidReason { get; set; }
    public int? ReversalOfEntryId { get; set; }
    public List<JournalEntryLineResponse> Lines { get; set; } = new();
}

public class JournalEntryLineResponse
{
    public int JournalLineId { get; set; }
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string? Description { get; set; }
}

public class CreateJournalEntryRequest
{
    public DateTime EntryDate { get; set; }
    public string? Description { get; set; }
    public string? ReferenceType { get; set; }
    public string? ReferenceId { get; set; }
    public string Status { get; set; } = "Draft"; // Draft or Posted
    public List<CreateJournalLineRequest> Lines { get; set; } = new();
}

public class CreateJournalLineRequest
{
    public int AccountId { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string? Description { get; set; }
}

public class JournalEntrySearchRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ReferenceType { get; set; }
    public string? Status { get; set; }
    public int? AccountId { get; set; }
    public string? SearchTerm { get; set; }
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

public class VoidJournalEntryRequest
{
    public string? Reason { get; set; }
}

// ============================================================================
// Financial Reports DTOs
// ============================================================================

public class TrialBalanceResponse
{
    public DateTime AsOfDate { get; set; }
    public List<TrialBalanceLineItem> Items { get; set; } = new();
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public bool IsBalanced { get; set; }
}

public class TrialBalanceLineItem
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public decimal DebitBalance { get; set; }
    public decimal CreditBalance { get; set; }
}

public class IncomeStatementResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<IncomeStatementSection> Sections { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetIncome { get; set; }
}

public class IncomeStatementSection
{
    public string SectionName { get; set; } = string.Empty;
    public List<IncomeStatementLineItem> Items { get; set; } = new();
    public decimal Total { get; set; }
}

public class IncomeStatementLineItem
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class BalanceSheetResponse
{
    public DateTime AsOfDate { get; set; }
    public BalanceSheetSection Assets { get; set; } = new();
    public BalanceSheetSection Liabilities { get; set; } = new();
    public BalanceSheetSection Equity { get; set; } = new();
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }
    public bool IsBalanced { get; set; }
}

public class BalanceSheetSection
{
    public string SectionName { get; set; } = string.Empty;
    public List<BalanceSheetLineItem> Items { get; set; } = new();
    public decimal Total { get; set; }
}

public class BalanceSheetLineItem
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public decimal Amount { get; set; }
}

public class AccountLedgerResponse
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public List<LedgerEntry> Entries { get; set; } = new();
}

public class LedgerEntry
{
    public DateTime Date { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
}

// ============================================================================
// Expense/Income Tracking DTOs
// ============================================================================

public class ExpenseResponse
{
    public int JournalEntryId { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? Description { get; set; }
    public string? Vendor { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
}

public class CreateExpenseRequest
{
    public DateTime Date { get; set; }
    [Required, MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    [MaxLength(200)]
    public string? Vendor { get; set; }
    [Range(1, int.MaxValue)]
    public int ExpenseAccountId { get; set; }
    [Range(1, int.MaxValue)]
    public int PaymentAccountId { get; set; }
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
}

public class IncomeRecordResponse
{
    public int JournalEntryId { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? Description { get; set; }
    public string? Customer { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<IncomeLineItemResponse> LineItems { get; set; } = new();
}

public class IncomeLineItemResponse
{
    public string ItemName { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty; // "Service" or "Product"
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class CreateIncomeRequest
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public int RevenueAccountId { get; set; }
    public int DepositAccountId { get; set; }
    public decimal Amount { get; set; }
}

// ============================================================================
// Invoice DTOs
// ============================================================================

public class InvoiceResponse
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = string.Empty; // Draft, Sent, Partial, Paid, Overdue, Cancelled
    public List<InvoiceLineResponse> Lines { get; set; } = new();
}

public class InvoiceLineResponse
{
    public int LineId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}

public class CreateInvoiceRequest
{
    [Required, Range(1, int.MaxValue)]
    public int CustomerId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    [MaxLength(500)]
    public string? Notes { get; set; }
    [Required, MinLength(1)]
    public List<CreateInvoiceLineRequest> Lines { get; set; } = new();
}

public class CreateInvoiceLineRequest
{
    [Required, MaxLength(300)]
    public string Description { get; set; } = string.Empty;
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}

public class UpdateInvoiceStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}

public class RecordPaymentRequest
{
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
}

// ============================================================================
// Dashboard/Summary DTOs
// ============================================================================

public class AccountingSummaryResponse
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetIncome { get; set; }
    public decimal CashOnHand { get; set; }
    public decimal AccountsReceivable { get; set; }
    public decimal AccountsPayable { get; set; }
    public List<MonthlyTrendItem> MonthlyTrend { get; set; } = new();
    public List<ExpenseCategoryItem> ExpensesByCategory { get; set; } = new();
    public List<RevenueStreamItem> RevenueStreams { get; set; } = new();
}

public class MonthlyTrendItem
{
    public string Month { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
    public decimal NetIncome { get; set; }
}

public class ExpenseCategoryItem
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
}

public class RevenueStreamItem
{
    public string Stream { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
}

// ============================================================================
// Sub-page Summary DTOs
// ============================================================================

public class ExpenseSummaryResponse
{
    public decimal MonthTotal { get; set; }
    public decimal LastMonthTotal { get; set; }
    public decimal YtdTotal { get; set; }
    public int PendingCount { get; set; }
}

public class IncomeSummaryResponse
{
    public decimal MonthTotal { get; set; }
    public decimal ServicesRevenue { get; set; }
    public decimal ProductSales { get; set; }
    public decimal YtdTotal { get; set; }
}

public class JournalSummaryResponse
{
    public int TotalEntries { get; set; }
    public int MonthEntries { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
}
