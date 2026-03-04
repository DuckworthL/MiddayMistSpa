namespace MiddayMistSpa.API.DTOs.Employee;

#region EmployeeShift DTOs

/// <summary>
/// Create a recurring weekly shift for an employee
/// </summary>
public record CreateShiftRequest
{
    public int EmployeeId { get; init; }
    public int DayOfWeek { get; init; } // 0=Sunday, 6=Saturday
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public bool IsRecurring { get; init; } = true;
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
}

/// <summary>
/// Update an existing shift
/// </summary>
public record UpdateShiftRequest
{
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public bool IsRecurring { get; init; } = true;
    public DateTime? EffectiveTo { get; init; }
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Set a full weekly shift schedule for an employee in one operation
/// </summary>
public record BulkShiftRequest
{
    public int EmployeeId { get; init; }
    public DateTime EffectiveFrom { get; init; }
    public List<DayShift> Shifts { get; init; } = new();
}

/// <summary>
/// A single day's shift within a bulk schedule
/// </summary>
public record DayShift
{
    public int DayOfWeek { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
}

/// <summary>
/// Response for a single employee shift
/// </summary>
public record ShiftResponse
{
    public int ShiftId { get; init; }
    public int EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public int DayOfWeek { get; init; }
    public string DayName { get; init; } = string.Empty;
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public string Duration { get; init; } = string.Empty;
    public bool IsRecurring { get; init; }
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Weekly schedule summary for an employee
/// </summary>
public record WeeklyShiftScheduleResponse
{
    public int EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public List<ShiftResponse> Shifts { get; init; } = new();
    public decimal TotalWeeklyHours { get; init; }
}

#endregion

#region ShiftException DTOs

/// <summary>
/// Create a shift exception (time off, sick leave, custom hours)
/// </summary>
public record CreateShiftExceptionRequest
{
    public int EmployeeId { get; init; }
    public DateTime ExceptionDate { get; init; }
    public string ExceptionType { get; init; } = string.Empty; // TimeOff, SickLeave, Emergency, CustomHours
    public TimeSpan? StartTime { get; init; } // null = full day off
    public TimeSpan? EndTime { get; init; }   // null = full day off
    public string? Reason { get; init; }
}

/// <summary>
/// Update an existing shift exception
/// </summary>
public record UpdateShiftExceptionRequest
{
    public string ExceptionType { get; init; } = string.Empty;
    public TimeSpan? StartTime { get; init; }
    public TimeSpan? EndTime { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Response for a shift exception
/// </summary>
public record ShiftExceptionResponse
{
    public int ExceptionId { get; init; }
    public int EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public DateTime ExceptionDate { get; init; }
    public string ExceptionType { get; init; } = string.Empty;
    public TimeSpan? StartTime { get; init; }
    public TimeSpan? EndTime { get; init; }
    public string? Reason { get; init; }
    public bool IsFullDayOff { get; init; }
    public DateTime CreatedAt { get; init; }
}

#endregion

#region Availability DTOs

/// <summary>
/// Employee availability for a specific date, combining shifts and exceptions
/// </summary>
public record EmployeeAvailabilityResponse
{
    public int EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public string DayName { get; init; } = string.Empty;
    public bool IsAvailable { get; init; }
    public string Status { get; init; } = string.Empty; // Working, DayOff, TimeOff, SickLeave, Emergency, CustomHours
    public TimeSpan? ShiftStart { get; init; }
    public TimeSpan? ShiftEnd { get; init; }
    public string? ExceptionReason { get; init; }
}

/// <summary>
/// Staff availability for a given date range
/// </summary>
public record StaffAvailabilityRequest
{
    public DateTime Date { get; init; }
    public TimeSpan? StartTime { get; init; }
    public TimeSpan? EndTime { get; init; }
    public bool TherapistsOnly { get; init; }
}

#endregion
