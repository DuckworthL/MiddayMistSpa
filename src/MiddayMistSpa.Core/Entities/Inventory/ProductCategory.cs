namespace MiddayMistSpa.Core.Entities.Inventory;

/// <summary>
/// Product categories (Retail, Supply, Consumable)
/// </summary>
public class ProductCategory
{
    public int ProductCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
