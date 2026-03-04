using System.ComponentModel.DataAnnotations;

namespace MiddayMistSpa.API.DTOs.TimeAttendance;

// ============================================================================
// Extended DTOs that supplement the Employee DTOs
// ============================================================================

public class ScheduleSearchRequest
{
    public int? EmployeeId { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public bool? ActiveOnly { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class WeeklyScheduleResponse
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public List<ScheduleDay> Schedules { get; set; } = [];
    public decimal TotalWeeklyHours { get; set; }
    public int WorkDays { get; set; }
    public int RestDays { get; set; }
}

public class ScheduleDay
{
    public int ScheduleId { get; set; }
    public int DayOfWeek { get; set; }
    public string DayOfWeekName { get; set; } = string.Empty;
    public TimeSpan ShiftStartTime { get; set; }
    public TimeSpan ShiftEndTime { get; set; }
    public TimeSpan? BreakStartTime { get; set; }
    public TimeSpan? BreakEndTime { get; set; }
    public bool IsRestDay { get; set; }
    public decimal WorkingHours { get; set; }
}

public class TimeOffSearchRequest
{
    public int? EmployeeId { get; set; }
    public string? LeaveType { get; set; }
    public string? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class UpdateTimeOffRequest
{
    [StringLength(50)]
    public string? LeaveType { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }
}

public class RejectTimeOffRequest
{
    [Required, StringLength(500)]
    public string RejectionReason { get; set; } = string.Empty;
}

public class CreateLeaveBalanceRequest
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int Year { get; set; }

    [Range(0, 30)]
    public decimal SILDays { get; set; } = 5.0m;

    [Range(0, 30)]
    public decimal SickLeaveDays { get; set; } = 0;
}

public class LeaveBalanceSummary
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal TotalLeaveEntitlement { get; set; }
    public decimal TotalLeaveUsed { get; set; }
    public decimal TotalLeaveRemaining { get; set; }
}

public class UpdateAdvanceRequest
{
    [Range(0.01, 100000)]
    public decimal? MonthlyDeduction { get; set; }

    [Range(1, 120)]
    public int? NumberOfInstallments { get; set; }
}

public class AdvanceSearchRequest
{
    public int? EmployeeId { get; set; }
    public string? AdvanceType { get; set; }
    public string? Status { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class RecordPaymentRequest
{
    [Required, Range(0.01, 100000)]
    public decimal Amount { get; set; }

    public DateTime? PaymentDate { get; set; }

    [StringLength(200)]
    public string? Notes { get; set; }
}

// ============================================================================
// Attendance Summary DTOs
// ============================================================================

public class AttendanceSummaryRequest
{
    public int? EmployeeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class AttendanceSummaryResponse
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalWorkDays { get; set; }
    public int ScheduledWorkDays { get; set; }
    public decimal TotalScheduledHours { get; set; }
    public int LeaveDays { get; set; }
    public int AbsentDays { get; set; }
    public decimal AttendanceRate { get; set; }
}

public class TeamAttendanceResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalEmployees { get; set; }
    public decimal AverageAttendanceRate { get; set; }
    public int TotalLeaveRequests { get; set; }
    public int ApprovedLeaves { get; set; }
    public int PendingLeaves { get; set; }
    public List<AttendanceSummaryResponse> EmployeeSummaries { get; set; } = [];
}

// ============================================================================
// Calendar/Schedule View DTOs
// ============================================================================

public class ScheduleCalendarRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? DepartmentId { get; set; }
}

public class ScheduleCalendarDayResponse
{
    public DateTime Date { get; set; }
    public string DayName { get; set; } = string.Empty;
    public bool IsHoliday { get; set; }
    public string? HolidayName { get; set; }
    public List<EmployeeDaySchedule> Schedules { get; set; } = [];
}

public class EmployeeDaySchedule
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public TimeSpan? ShiftStart { get; set; }
    public TimeSpan? ShiftEnd { get; set; }
    public bool IsRestDay { get; set; }
    public bool IsOnLeave { get; set; }
    public string? LeaveType { get; set; }
    public string Status { get; set; } = "Scheduled";
}

// ============================================================================
// Clock In/Out & Attendance Record DTOs
// ============================================================================

public class AttendanceRecordDto
{
    public int AttendanceId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public DateTime Date { get; set; }
    public DateTime? ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
    public DateTime? BreakStart { get; set; }
    public DateTime? BreakEnd { get; set; }
    public decimal TotalHours { get; set; }
    public decimal BreakMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public int? ClockedInByUserId { get; set; }
    public string? Notes { get; set; }
}

public class LiveAttendanceStatusDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public bool IsClockedIn { get; set; }
    public DateTime? ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
    public bool OnBreak { get; set; }
    public DateTime? BreakStart { get; set; }
    public decimal TodayHours { get; set; }
    /// <summary>
    /// NotYetIn | ClockedIn | OnBreak | ClockedOut
    /// </summary>
    public string Status { get; set; } = "NotYetIn";
}

public class ManualAttendanceRequest
{
    [Required]
    public int EmployeeId { get; set; }
    [Required]
    public DateTime Date { get; set; }
    [Required]
    public DateTime ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
    public DateTime? BreakStart { get; set; }
    public DateTime? BreakEnd { get; set; }
    public int BreakMinutes { get; set; }
    [StringLength(500)]
    public string? Notes { get; set; }
}
