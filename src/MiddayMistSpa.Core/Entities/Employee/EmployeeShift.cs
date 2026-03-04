namespace MiddayMistSpa.Core.Entities.Employee;

/// <summary>
/// Recurring weekly shift schedule for employees/therapists.
/// Defines what days and hours an employee is scheduled to work.
/// </summary>
public class EmployeeShift
{
    public int ShiftId { get; set; }
    public int EmployeeId { get; set; }
    public int DayOfWeek { get; set; } // 0=Sunday, 1=Monday, ..., 6=Saturday
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsRecurring { get; set; } = true;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Computed properties
    public TimeSpan Duration => EndTime - StartTime;

    // Navigation properties
    public virtual Employee Employee { get; set; } = null!;
}
