namespace MiddayMistSpa.Core.Entities.Accounting;

/// <summary>
/// Chart of accounts with hierarchical structure
/// </summary>
public class ChartOfAccount
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty; // Asset, Liability, Equity, Revenue, Expense
    public string NormalBalance { get; set; } = "Debit"; // Debit or Credit — determines how balance is calculated
    public string? AccountCategory { get; set; } // Cash, Accounts Receivable, Sales Revenue, etc.
    public int? ParentAccountId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ChartOfAccount? ParentAccount { get; set; }
    public virtual ICollection<ChartOfAccount> ChildAccounts { get; set; } = new List<ChartOfAccount>();
    public virtual ICollection<JournalEntryLine> JournalEntryLines { get; set; } = new List<JournalEntryLine>();
}
