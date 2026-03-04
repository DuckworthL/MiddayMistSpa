namespace MiddayMistSpa.Core.Entities.Inventory;

/// <summary>
/// Product/inventory item with stock tracking and expiry
/// </summary>
public class Product
{
    public int ProductId { get; set; }
    public int ProductCategoryId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ProductType { get; set; } = string.Empty; // Retail, Supply, Consumable

    // Inventory
    public decimal CurrentStock { get; set; } = 0;
    public decimal ReorderLevel { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty; // pcs, ml, g, bottles, etc.

    // Pricing
    public decimal CostPrice { get; set; }
    public decimal? SellingPrice { get; set; }
    public decimal RetailCommissionRate { get; set; } = 0.10m; // 10% for retail products

    // Tracking
    public DateTime? ExpiryDate { get; set; }
    public string? Supplier { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Computed properties
    public bool IsLowStock => CurrentStock <= ReorderLevel;
    public bool IsExpiringSoon => ExpiryDate.HasValue && ExpiryDate.Value <= DateTime.UtcNow.AddDays(30);
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;

    // Navigation properties
    public virtual ProductCategory Category { get; set; } = null!;
    public virtual ICollection<StockAdjustment> StockAdjustments { get; set; } = new List<StockAdjustment>();
    public virtual ICollection<Service.ServiceProductRequirement> ServiceRequirements { get; set; } = new List<Service.ServiceProductRequirement>();
    public virtual ICollection<ProductBatch> Batches { get; set; } = new List<ProductBatch>();
}
