namespace MiddayMistSpa.Core.Entities.Employee;

/// <summary>
/// Exceptions to an employee's recurring shift schedule.
/// Covers time off, sick leave, emergency, or custom working hours for a specific date.
/// </summary>
public class ShiftException
{
    public int ExceptionId { get; set; }
    public int EmployeeId { get; set; }
    public DateTime ExceptionDate { get; set; }
    public string ExceptionType { get; set; } = string.Empty; // TimeOff, SickLeave, Emergency, CustomHours
    public TimeSpan? StartTime { get; set; } // NULL if full day off
    public TimeSpan? EndTime { get; set; } // NULL if full day off
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Computed properties
    public bool IsFullDayOff => StartTime == null && EndTime == null;

    // Navigation properties
    public virtual Employee Employee { get; set; } = null!;
}
