namespace MiddayMistSpa.Core.Entities.Identity;

/// <summary>
/// Tracks user sessions for concurrent session management (max 2 per user)
/// </summary>
public class UserSession
{
    public int SessionId { get; set; }
    public int UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;
}
