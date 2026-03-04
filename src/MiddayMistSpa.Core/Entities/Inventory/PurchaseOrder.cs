using MiddayMistSpa.Core.Entities.Identity;

namespace MiddayMistSpa.Core.Entities.Inventory;

/// <summary>
/// Purchase order header with approval workflow
/// </summary>
public class PurchaseOrder
{
    public int PurchaseOrderId { get; set; }
    public string PONumber { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Approved, Delivered, Cancelled
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public string? Notes { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Supplier Supplier { get; set; } = null!;
    public virtual User? ApprovedByUser { get; set; }
    public virtual User CreatedByUser { get; set; } = null!;
    public virtual ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}
