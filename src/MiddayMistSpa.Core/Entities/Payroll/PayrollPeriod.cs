using MiddayMistSpa.Core.Entities.Identity;

namespace MiddayMistSpa.Core.Entities.Payroll;

/// <summary>
/// Payroll period (Semi-Monthly or Monthly) with finalization workflow
/// </summary>
public class PayrollPeriod
{
    public int PayrollPeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty; // e.g., "January 1-15, 2026"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string PayrollType { get; set; } = string.Empty; // Semi-Monthly, Monthly
    public DateTime CutoffDate { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Finalized, Paid
    public int? FinalizedBy { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User? FinalizedByUser { get; set; }
    public virtual ICollection<PayrollRecord> PayrollRecords { get; set; } = new List<PayrollRecord>();
}
