namespace MiddayMistSpa.Core.Entities.Payroll;

/// <summary>
/// Individual employee payroll record with all earnings and deductions
/// </summary>
public class PayrollRecord
{
    public int PayrollRecordId { get; set; }
    public int PayrollPeriodId { get; set; }
    public int EmployeeId { get; set; }

    // Work Hours
    public decimal DaysWorked { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal OvertimeHours { get; set; } = 0;
    public decimal NightDifferentialHours { get; set; } = 0;

    // Earnings
    public decimal BasicSalary { get; set; }
    public decimal OvertimePay { get; set; } = 0;
    public decimal NightDifferentialPay { get; set; } = 0;
    public decimal HolidayPay { get; set; } = 0;
    public decimal RestDayPay { get; set; } = 0;
    public decimal Commissions { get; set; } = 0;
    public decimal Tips { get; set; } = 0;
    public decimal RiceAllowance { get; set; } = 0;
    public decimal LaundryAllowance { get; set; } = 0;
    public decimal OtherAllowances { get; set; } = 0;
    public decimal GrossPay { get; set; }

    // Mandatory Deductions (Philippine) - Employee Share
    public decimal SSSContribution { get; set; } = 0;
    public decimal PhilHealthContribution { get; set; } = 0;
    public decimal PagIBIGContribution { get; set; } = 0;
    public decimal WithholdingTax { get; set; } = 0;

    // Mandatory Deductions - Employer Share (for remittance reports)
    public decimal SSSEmployerContribution { get; set; } = 0;
    public decimal PhilHealthEmployerContribution { get; set; } = 0;
    public decimal PagIBIGEmployerContribution { get; set; } = 0;
    public decimal ECContribution { get; set; } = 0; // Employees' Compensation

    // Other Deductions
    public decimal Tardiness { get; set; } = 0;
    public decimal Absences { get; set; } = 0;
    public decimal CashAdvances { get; set; } = 0;
    public decimal LoanDeductions { get; set; } = 0;
    public decimal OtherDeductions { get; set; } = 0;
    public decimal TotalDeductions { get; set; }

    // Net Pay
    public decimal NetPay { get; set; }

    // Payment Details
    public string? PaymentMethod { get; set; } // Cash, Bank Transfer
    public DateTime? PaymentDate { get; set; }
    public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual PayrollPeriod PayrollPeriod { get; set; } = null!;
    public virtual Employee.Employee Employee { get; set; } = null!;
}
