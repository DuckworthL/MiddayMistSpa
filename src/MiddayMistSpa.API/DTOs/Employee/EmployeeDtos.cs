using System.ComponentModel.DataAnnotations;

namespace MiddayMistSpa.API.DTOs.Employee;

#region Employee DTOs

public record CreateEmployeeRequest
{
    [Required, MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;
    [Required, MaxLength(100)]
    public string LastName { get; init; } = string.Empty;
    [MaxLength(100)]
    public string? MiddleName { get; init; }
    public DateTime DateOfBirth { get; init; }
    [Required, MaxLength(20)]
    public string Gender { get; init; } = string.Empty;
    [MaxLength(20)]
    public string? CivilStatus { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Province { get; init; }
    public string? PostalCode { get; init; }
    [Required, MaxLength(20)]
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? EmergencyContactName { get; init; }
    public string? EmergencyContactPhone { get; init; }

    // Philippine Government IDs
    public string? SSSNumber { get; init; }
    public string? PhilHealthNumber { get; init; }
    public string? PagIBIGNumber { get; init; }
    public string? TINNumber { get; init; }

    // Employment Details
    [Required, MaxLength(100)]
    public string Position { get; init; } = string.Empty;
    public string? Department { get; init; }
    public DateTime HireDate { get; init; }
    [MaxLength(20)]
    public string EmploymentType { get; init; } = "Regular";
    [Range(0, double.MaxValue)]
    public decimal DailyRate { get; init; }
    [Range(0, double.MaxValue)]
    public decimal MonthlyBasicSalary { get; init; }
    [MaxLength(20)]
    public string PayrollType { get; init; } = "Semi-Monthly";

    // Therapist Specific
    public bool IsTherapist { get; init; }
    public string? Specialization { get; init; }
    public string? LicenseNumber { get; init; }
    public DateTime? LicenseExpiryDate { get; init; }

    // User Account (optional - creates login access)
    public bool CreateUserAccount { get; init; }
    public int? RoleId { get; init; }
}

public record UpdateEmployeeRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public DateTime DateOfBirth { get; init; }
    public string Gender { get; init; } = string.Empty;
    public string? CivilStatus { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Province { get; init; }
    public string? PostalCode { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? EmergencyContactName { get; init; }
    public string? EmergencyContactPhone { get; init; }

    // Philippine Government IDs
    public string? SSSNumber { get; init; }
    public string? PhilHealthNumber { get; init; }
    public string? PagIBIGNumber { get; init; }
    public string? TINNumber { get; init; }

    // Employment Details
    public string Position { get; init; } = string.Empty;
    public string? Department { get; init; }
    public string EmploymentType { get; init; } = "Regular";
    public string EmploymentStatus { get; init; } = "Active";
    public decimal DailyRate { get; init; }
    public decimal MonthlyBasicSalary { get; init; }
    public string PayrollType { get; init; } = "Semi-Monthly";

    // Therapist Specific
    public bool IsTherapist { get; init; }
    public string? Specialization { get; init; }
    public string? LicenseNumber { get; init; }
    public DateTime? LicenseExpiryDate { get; init; }
}

public record EmployeeResponse
{
    public int EmployeeId { get; init; }
    public string EmployeeCode { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public DateTime DateOfBirth { get; init; }
    public string Gender { get; init; } = string.Empty;
    public string? CivilStatus { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Province { get; init; }
    public string? PostalCode { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? EmergencyContactName { get; init; }
    public string? EmergencyContactPhone { get; init; }

    // Philippine Government IDs
    public string? SSSNumber { get; init; }
    public string? PhilHealthNumber { get; init; }
    public string? PagIBIGNumber { get; init; }
    public string? TINNumber { get; init; }

    // Employment Details
    public string Position { get; init; } = string.Empty;
    public string? Department { get; init; }
    public DateTime HireDate { get; init; }
    public string EmploymentType { get; init; } = string.Empty;
    public string EmploymentStatus { get; init; } = string.Empty;
    public decimal DailyRate { get; init; }
    public decimal MonthlyBasicSalary { get; init; }
    public string PayrollType { get; init; } = string.Empty;

    // Therapist Specific
    public bool IsTherapist { get; init; }
    public string? Specialization { get; init; }
    public string? LicenseNumber { get; init; }
    public DateTime? LicenseExpiryDate { get; init; }

    public bool IsActive { get; init; }
    public int? UserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record EmployeeListResponse
{
    public int EmployeeId { get; init; }
    public string EmployeeCode { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Position { get; init; } = string.Empty;
    public string? Department { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public string EmploymentStatus { get; init; } = string.Empty;
    public bool IsTherapist { get; init; }
    public string? Specialization { get; init; }
    public bool IsActive { get; init; }
}

#endregion

#region Schedule DTOs

public record CreateScheduleRequest
{
    public int EmployeeId { get; init; }
    public int DayOfWeek { get; init; } // 0=Sunday, 6=Saturday
    public TimeSpan ShiftStartTime { get; init; }
    public TimeSpan ShiftEndTime { get; init; }
    public TimeSpan? BreakStartTime { get; init; }
    public TimeSpan? BreakEndTime { get; init; }
    public bool IsRestDay { get; init; }
    public DateTime EffectiveDate { get; init; }
    public DateTime? EndDate { get; init; }
}

public record UpdateScheduleRequest
{
    public TimeSpan ShiftStartTime { get; init; }
    public TimeSpan ShiftEndTime { get; init; }
    public TimeSpan? BreakStartTime { get; init; }
    public TimeSpan? BreakEndTime { get; init; }
    public bool IsRestDay { get; init; }
    public DateTime? EndDate { get; init; }
}

public record ScheduleResponse
{
    public int ScheduleId { get; init; }
    public int EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public int DayOfWeek { get; init; }
    public string DayName { get; init; } = string.Empty;
    public TimeSpan ShiftStartTime { get; init; }
    public TimeSpan ShiftEndTime { get; init; }
    public TimeSpan? BreakStartTime { get; init; }
    public TimeSpan? BreakEndTime { get; init; }
    public bool IsRestDay { get; init; }
    public DateTime EffectiveDate { get; init; }
    public DateTime? EndDate { get; init; }
}

public record BulkScheduleRequest
{
    public int EmployeeId { get; init; }
    public DateTime EffectiveDate { get; init; }
    public List<DaySchedule> Schedules { get; init; } = new();
}

public record DaySchedule
{
    public int DayOfWeek { get; init; }
    public TimeSpan ShiftStartTime { get; init; }
    public TimeSpan ShiftEndTime { get; init; }
    public TimeSpan? BreakStartTime { get; init; }
    public TimeSpan? BreakEndTime { get; init; }
    public bool IsRestDay { get; init; }
}

#endregion

#region Time Off Request DTOs

public record CreateTimeOffRequest
{
    public int EmployeeId { get; init; }
    public string LeaveType { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? Reason { get; init; }
}

public record ApproveTimeOffRequest
{
    public bool Approved { get; init; }
    public string? RejectionReason { get; init; }
}

public record TimeOffResponse
{
    public int TimeOffRequestId { get; init; }
    public int EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public string LeaveType { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public decimal TotalDays { get; init; }
    public string? Reason { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? ApprovedByName { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public string? RejectionReason { get; init; }
    public DateTime CreatedAt { get; init; }
}

#endregion

#region Leave Balance DTOs

public record LeaveBalanceResponse
{
    public int LeaveBalanceId { get; init; }
    public int EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public int Year { get; init; }
    public decimal SILDays { get; init; }
    public decimal SILUsed { get; init; }
    public decimal SILRemaining { get; init; }
    public decimal SickLeaveDays { get; init; }
    public decimal SickLeaveUsed { get; init; }
    public decimal SickLeaveRemaining { get; init; }
}

public record UpdateLeaveBalanceRequest
{
    public decimal SILDays { get; init; }
    public decimal SickLeaveDays { get; init; }
}

#endregion

#region Advance/Loan DTOs

public record CreateAdvanceRequest
{
    public int EmployeeId { get; init; }
    public string AdvanceType { get; init; } = string.Empty; // Cash Advance, SSS Loan, Pag-IBIG Loan
    public decimal Amount { get; init; }
    public decimal MonthlyDeduction { get; init; }
    public DateTime StartDate { get; init; }
    public int NumberOfInstallments { get; init; }
}

public record AdvanceResponse
{
    public int AdvanceId { get; init; }
    public int EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public string AdvanceType { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal Balance { get; init; }
    public decimal MonthlyDeduction { get; init; }
    public DateTime StartDate { get; init; }
    public int NumberOfInstallments { get; init; }
    public int InstallmentsPaid { get; init; }
    public int InstallmentsRemaining { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? ApprovedByName { get; init; }
    public DateTime CreatedAt { get; init; }
}

#endregion

#region Common DTOs

public record PagedRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SearchTerm { get; init; }
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
    public string? Department { get; init; }
    public bool? ActiveOnly { get; init; }
}

public record PagedResponse<T>
{
    public List<T> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

#endregion
