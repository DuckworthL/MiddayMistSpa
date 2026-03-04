namespace MiddayMistSpa.Core.Entities.Service;

/// <summary>
/// Service categories (Massage, Facial, Body Treatment, Packages, Add-Ons)
/// </summary>
public class ServiceCategory
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Service> Services { get; set; } = new List<Service>();
}
