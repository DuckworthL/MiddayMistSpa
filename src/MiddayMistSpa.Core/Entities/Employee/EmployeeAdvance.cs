using MiddayMistSpa.Core.Entities.Identity;

namespace MiddayMistSpa.Core.Entities.Employee;

/// <summary>
/// Cash advances and loans (SSS, Pag-IBIG) with installment tracking
/// </summary>
public class EmployeeAdvance
{
    public int AdvanceId { get; set; }
    public int EmployeeId { get; set; }
    public string AdvanceType { get; set; } = string.Empty; // Cash Advance, SSS Loan, Pag-IBIG Loan
    public decimal Amount { get; set; }
    public decimal Balance { get; set; }
    public decimal MonthlyDeduction { get; set; }
    public DateTime StartDate { get; set; }
    public int NumberOfInstallments { get; set; }
    public int InstallmentsPaid { get; set; } = 0;
    public string Status { get; set; } = "Active"; // Active, Fully Paid
    public int? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Computed properties
    public int InstallmentsRemaining => NumberOfInstallments - InstallmentsPaid;
    public bool IsFullyPaid => Balance <= 0 || InstallmentsPaid >= NumberOfInstallments;

    // Navigation properties
    public virtual Employee Employee { get; set; } = null!;
    public virtual User? ApprovedByUser { get; set; }
}
