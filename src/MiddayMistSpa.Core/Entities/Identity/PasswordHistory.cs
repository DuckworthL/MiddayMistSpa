namespace MiddayMistSpa.Core.Entities.Identity;

/// <summary>
/// Tracks password history to prevent reuse of last 5 passwords
/// </summary>
public class PasswordHistory
{
    public int PasswordHistoryId { get; set; }
    public int UserId { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;
}
