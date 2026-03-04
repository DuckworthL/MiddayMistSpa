using MiddayMistSpa.Core.Entities.Identity;

namespace MiddayMistSpa.Core.Entities.Employee;

/// <summary>
/// Leave/time-off requests with approval workflow
/// </summary>
public class TimeOffRequest
{
    public int TimeOffRequestId { get; set; }
    public int EmployeeId { get; set; }
    public string LeaveType { get; set; } = string.Empty; // SIL, Sick Leave, Maternity, Paternity, Solo Parent, Emergency
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Employee Employee { get; set; } = null!;
    public virtual User? ApprovedByUser { get; set; }
}
