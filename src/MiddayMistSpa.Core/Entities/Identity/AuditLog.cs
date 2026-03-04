namespace MiddayMistSpa.Core.Entities.Identity;

/// <summary>
/// Immutable audit trail for all system activities
/// </summary>
public class AuditLog
{
    public long AuditLogId { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty; // Login, Logout, Create, Update, Delete, etc.
    public string? TableName { get; set; }
    public string? RecordId { get; set; }
    public string? OldValues { get; set; } // JSON format
    public string? NewValues { get; set; } // JSON format
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User? User { get; set; }
}
