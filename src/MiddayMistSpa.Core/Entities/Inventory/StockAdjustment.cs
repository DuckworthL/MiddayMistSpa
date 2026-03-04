using MiddayMistSpa.Core.Entities.Identity;

namespace MiddayMistSpa.Core.Entities.Inventory;

/// <summary>
/// All stock movements with before/after tracking
/// </summary>
public class StockAdjustment
{
    public int AdjustmentId { get; set; }
    public int ProductId { get; set; }
    public string AdjustmentType { get; set; } = string.Empty; // Received, Sold, Damaged, Expired, Service Usage, Audit
    public decimal QuantityBefore { get; set; }
    public decimal QuantityChange { get; set; } // Positive or negative
    public decimal QuantityAfter { get; set; }
    public string? Reason { get; set; }
    public string? ReferenceNumber { get; set; } // Links to PO, Invoice, Appointment, etc.
    public int AdjustedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Product Product { get; set; } = null!;
    public virtual User AdjustedByUser { get; set; } = null!;
}
