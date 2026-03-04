namespace MiddayMistSpa.Core.Entities.Employee;

/// <summary>
/// Tracks daily clock-in/out and break times for employees.
/// One record per employee per day. Duplicate prevention enforced at the service layer.
/// </summary>
public class AttendanceRecord
{
    public int AttendanceId { get; set; }
    public int EmployeeId { get; set; }
    public DateTime Date { get; set; } // Date only (no time component)
    public DateTime? ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
    public DateTime? BreakStart { get; set; }
    public DateTime? BreakEnd { get; set; }
    public decimal TotalHours { get; set; }
    public decimal BreakMinutes { get; set; }
    public string Status { get; set; } = "ClockedIn"; // ClockedIn, OnBreak, ClockedOut, Absent
    public bool IsApproved { get; set; } = false;
    public int? ClockedInByUserId { get; set; } // Null = self, otherwise the admin/HR user who clocked them in
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual Employee Employee { get; set; } = null!;
}
