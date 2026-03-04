namespace MiddayMistSpa.Core.Entities.Inventory;

/// <summary>
/// Purchase order line items with receiving tracking
/// </summary>
public class PurchaseOrderItem
{
    public int POItemId { get; set; }
    public int PurchaseOrderId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal QuantityReceived { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Computed property
    public decimal QuantityPending => Quantity - QuantityReceived;
    public bool IsFullyReceived => QuantityReceived >= Quantity;

    // Navigation properties
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
