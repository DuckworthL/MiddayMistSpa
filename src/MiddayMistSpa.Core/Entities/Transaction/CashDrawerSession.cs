using MiddayMistSpa.Core.Entities.Identity;

namespace MiddayMistSpa.Core.Entities.Transaction;

public class CashDrawerSession
{
    public int SessionId { get; set; }
    public int OpenedByUserId { get; set; }
    public int? ClosedByUserId { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal StartingFloat { get; set; }
    public decimal TotalCashIn { get; set; }
    public decimal TotalCashOut { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal? ActualCash { get; set; }
    public decimal? Discrepancy { get; set; }
    public string Status { get; set; } = "Open"; // Open, Closed
    public string? Notes { get; set; }

    // Navigation
    public virtual User OpenedByUser { get; set; } = null!;
    public virtual User? ClosedByUser { get; set; }
}
