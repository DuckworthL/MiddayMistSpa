using MiddayMistSpa.Core.Entities.Identity;

namespace MiddayMistSpa.Core.Entities.Employee;

/// <summary>
/// Employee entity with Philippine government ID fields and therapist-specific data
/// </summary>
public class Employee
{
    public int EmployeeId { get; set; }
    public int? UserId { get; set; } // Links to Users table if employee has login access
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty; // Male, Female, Other
    public string? CivilStatus { get; set; } // Single, Married, Widowed, Divorced
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }

    // Philippine Government IDs
    public string? SSSNumber { get; set; }
    public string? PhilHealthNumber { get; set; }
    public string? PagIBIGNumber { get; set; }
    public string? TINNumber { get; set; }

    // Employment Details
    public string Position { get; set; } = string.Empty;
    public string? Department { get; set; }
    public DateTime HireDate { get; set; }
    public string EmploymentType { get; set; } = string.Empty; // Regular, Contractual, Part-Time, Probationary
    public string EmploymentStatus { get; set; } = "Active"; // Active, Resigned, Terminated, On Leave

    // Salary Information
    public decimal DailyRate { get; set; }
    public decimal MonthlyBasicSalary { get; set; }
    public string PayrollType { get; set; } = "Semi-Monthly"; // Semi-Monthly, Monthly, Daily

    // Therapist Specific
    public bool IsTherapist { get; set; } = false;
    public string? Specialization { get; set; } // Swedish Massage, Deep Tissue, Reflexology, etc.
    public string? LicenseNumber { get; set; }
    public DateTime? LicenseExpiryDate { get; set; }

    // Bank Account (for payroll disbursement)
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Computed property
    public string FullName => string.IsNullOrEmpty(MiddleName)
        ? $"{FirstName} {LastName}"
        : $"{FirstName} {MiddleName} {LastName}";

    // Navigation properties
    public virtual User? User { get; set; }
    public virtual ICollection<EmployeeSchedule> Schedules { get; set; } = new List<EmployeeSchedule>();
    public virtual ICollection<EmployeeShift> EmployeeShifts { get; set; } = new List<EmployeeShift>();
    public virtual ICollection<ShiftException> ShiftExceptions { get; set; } = new List<ShiftException>();
    public virtual ICollection<TimeOffRequest> TimeOffRequests { get; set; } = new List<TimeOffRequest>();
    public virtual ICollection<EmployeeLeaveBalance> LeaveBalances { get; set; } = new List<EmployeeLeaveBalance>();
    public virtual ICollection<EmployeeAdvance> Advances { get; set; } = new List<EmployeeAdvance>();
    public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}
