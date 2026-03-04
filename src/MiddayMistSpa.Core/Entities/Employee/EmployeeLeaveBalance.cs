namespace MiddayMistSpa.Core.Entities.Employee;

/// <summary>
/// Annual leave balance tracking (SIL = 5 days/year per Philippine Labor Code)
/// </summary>
public class EmployeeLeaveBalance
{
    public int LeaveBalanceId { get; set; }
    public int EmployeeId { get; set; }
    public int Year { get; set; }
    public decimal SILDays { get; set; } = 5.0m; // Service Incentive Leave (5 days/year in PH)
    public decimal SILUsed { get; set; } = 0;
    public decimal SickLeaveDays { get; set; } = 0;
    public decimal SickLeaveUsed { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Computed properties
    public decimal SILRemaining => SILDays - SILUsed;
    public decimal SickLeaveRemaining => SickLeaveDays - SickLeaveUsed;

    // Navigation properties
    public virtual Employee Employee { get; set; } = null!;
}
