namespace MiddayMistSpa.Core.Entities.Service;

/// <summary>
/// Spa service with pricing tiers and therapist commission rate
/// </summary>
public class Service
{
    public int ServiceId { get; set; }
    public int CategoryId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }

    // Pricing (in PHP)
    public decimal RegularPrice { get; set; }
    public decimal? MemberPrice { get; set; }
    public decimal? PromoPrice { get; set; }

    // Commission Rates
    public decimal TherapistCommissionRate { get; set; } // e.g., 0.40 for 40%

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ServiceCategory Category { get; set; } = null!;
    public virtual ICollection<ServiceProductRequirement> ProductRequirements { get; set; } = new List<ServiceProductRequirement>();
}
