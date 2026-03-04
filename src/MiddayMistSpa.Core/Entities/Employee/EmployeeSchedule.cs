namespace MiddayMistSpa.Core.Entities.Employee;

/// <summary>
/// Weekly schedule for employees with shift times and breaks
/// </summary>
public class EmployeeSchedule
{
    public int ScheduleId { get; set; }
    public int EmployeeId { get; set; }
    public int DayOfWeek { get; set; } // 0=Sunday, 1=Monday, ..., 6=Saturday
    public TimeSpan ShiftStartTime { get; set; }
    public TimeSpan ShiftEndTime { get; set; }
    public TimeSpan? BreakStartTime { get; set; }
    public TimeSpan? BreakEndTime { get; set; }
    public bool IsRestDay { get; set; } = false;
    public DateTime EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Employee Employee { get; set; } = null!;
}
