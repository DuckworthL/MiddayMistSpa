using MiddayMistSpa.Core.Entities.Inventory;

namespace MiddayMistSpa.Core.Entities.Service;

/// <summary>
/// Links services to required inventory items (auto-deduct on service completion)
/// </summary>
public class ServiceProductRequirement
{
    public int RequirementId { get; set; }
    public int ServiceId { get; set; }
    public int ProductId { get; set; }
    public decimal QuantityRequired { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Service Service { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
