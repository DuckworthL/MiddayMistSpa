namespace MiddayMistSpa.Core.Entities.Accounting;

/// <summary>
/// Journal entry line items (debits and credits)
/// </summary>
public class JournalEntryLine
{
    public int JournalLineId { get; set; }
    public int JournalEntryId { get; set; }
    public int AccountId { get; set; }
    public decimal DebitAmount { get; set; } = 0;
    public decimal CreditAmount { get; set; } = 0;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Computed property
    public bool IsDebit => DebitAmount > 0;
    public bool IsCredit => CreditAmount > 0;

    // Navigation properties
    public virtual JournalEntry JournalEntry { get; set; } = null!;
    public virtual ChartOfAccount Account { get; set; } = null!;
}
