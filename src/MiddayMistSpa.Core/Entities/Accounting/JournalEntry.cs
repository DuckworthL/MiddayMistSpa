using MiddayMistSpa.Core.Entities.Identity;

namespace MiddayMistSpa.Core.Entities.Accounting;

/// <summary>
/// Journal entry header with double-entry validation
/// </summary>
public class JournalEntry
{
    public int JournalEntryId { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string? Description { get; set; }
    public string? ReferenceType { get; set; } // Transaction, Payroll, PO, Adjustment
    public string? ReferenceId { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Posted, Voided
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? VoidedBy { get; set; }
    public DateTime? VoidedAt { get; set; }
    public string? VoidReason { get; set; }
    public int? ReversalOfEntryId { get; set; } // For reversing entries, points to the original

    // Computed property - validates double-entry
    public bool IsBalanced => TotalDebit == TotalCredit;

    // Navigation properties
    public virtual User CreatedByUser { get; set; } = null!;
    public virtual User? VoidedByUser { get; set; }
    public virtual JournalEntry? ReversalOfEntry { get; set; }
    public virtual ICollection<JournalEntryLine> Lines { get; set; } = new List<JournalEntryLine>();
}
