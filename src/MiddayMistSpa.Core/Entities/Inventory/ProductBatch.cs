namespace MiddayMistSpa.Core.Entities.Inventory;

/// <summary>
/// Batch/lot tracking for inventory with individual cost prices and expiry dates (enables FIFO costing)
/// </summary>
public class ProductBatch
{
    public int ProductBatchId { get; set; }
    public int ProductId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;

    /// <summary>
    /// Cost price for this specific batch (supports FIFO costing)
    /// </summary>
    public decimal CostPrice { get; set; }

    /// <summary>
    /// Original quantity received in this batch
    /// </summary>
    public decimal QuantityReceived { get; set; }

    /// <summary>
    /// Remaining quantity in this batch (decremented on usage/sale)
    /// </summary>
    public decimal QuantityRemaining { get; set; }

    /// <summary>
    /// Expiry date for this specific batch
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Date this batch was received into inventory
    /// </summary>
    public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Link to the PO item that brought this batch in (null for manual adjustments)
    /// </summary>
    public int? PurchaseOrderItemId { get; set; }

    public int? SupplierId { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Computed
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;
    public bool IsExpiringSoon => ExpiryDate.HasValue && ExpiryDate.Value <= DateTime.UtcNow.AddDays(30);
    public bool IsFullyConsumed => QuantityRemaining <= 0;

    // Navigation properties
    public virtual Product Product { get; set; } = null!;
    public virtual PurchaseOrderItem? PurchaseOrderItem { get; set; }
    public virtual Supplier? Supplier { get; set; }
}
