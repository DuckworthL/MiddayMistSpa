namespace MiddayMistSpa.Core.Entities.Identity;

/// <summary>
/// User entity - ASP.NET Identity compatible
/// </summary>
public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; } = false;
    public string PasswordHash { get; set; } = string.Empty;
    public string? SecurityStamp { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; } = false;
    public bool TwoFactorEnabled { get; set; } = false;
    public string? TotpSecretKey { get; set; }
    public DateTime? TwoFactorConfirmedAt { get; set; }
    public string? RecoveryCodes { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public bool LockoutEnabled { get; set; } = true;
    public int AccessFailedCount { get; set; } = 0;

    // Role relationship
    public int RoleId { get; set; }

    // Personal info
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    // Status
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public DateTime PasswordExpiryDate { get; set; }
    public bool MustChangePassword { get; set; } = true;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Computed property
    public string FullName => $"{FirstName} {LastName}";

    // Navigation properties
    public virtual Role Role { get; set; } = null!;
    public virtual ICollection<PasswordHistory> PasswordHistories { get; set; } = new List<PasswordHistory>();
    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
