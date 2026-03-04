using MiddayMistSpa.Core.Entities.Identity;

namespace MiddayMistSpa.Core.Entities.Transaction;

/// <summary>
/// Refund tracking with approval
/// </summary>
public class Refund
{
    public int RefundId { get; set; }
    public int TransactionId { get; set; }
    public decimal RefundAmount { get; set; }
    public string RefundMethod { get; set; } = string.Empty; // Cash, Card Reversal
    public string? Reason { get; set; }
    public string RefundType { get; set; } = string.Empty; // Full, Partial
    public int ApprovedBy { get; set; }
    public int ProcessedBy { get; set; }
    public DateTime RefundDate { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Transaction Transaction { get; set; } = null!;
    public virtual User ApprovedByUser { get; set; } = null!;
    public virtual User ProcessedByUser { get; set; } = null!;
}
