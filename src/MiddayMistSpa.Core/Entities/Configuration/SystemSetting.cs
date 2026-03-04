using MiddayMistSpa.Core.Entities.Identity;

namespace MiddayMistSpa.Core.Entities.Configuration;

/// <summary>
/// Key-value system settings with categories
/// </summary>
public class SystemSetting
{
    public int SettingId { get; set; }
    public string SettingKey { get; set; } = string.Empty;
    public string? SettingValue { get; set; }
    public string SettingType { get; set; } = string.Empty; // String, Number, Boolean, JSON
    public string? Description { get; set; }
    public string? Category { get; set; } // General, Payroll, Stripe, Calendly, etc.
    public bool IsEditable { get; set; } = true;
    public int? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User? UpdatedByUser { get; set; }
}
