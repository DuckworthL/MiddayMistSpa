namespace MiddayMistSpa.Core.Entities.Payroll;

/// <summary>
/// Pag-IBIG contribution rates (2% each, capped at ₱200/₱300)
/// </summary>
public class PagIBIGContributionRate
{
    public int PagIBIGRateId { get; set; }
    public decimal MinSalary { get; set; }
    public decimal MaxSalary { get; set; }
    public decimal EmployeeRate { get; set; } // e.g., 0.02 for 2%
    public decimal EmployerRate { get; set; } // e.g., 0.02 for 2%
    public decimal EmployeeMaxContribution { get; set; } // Capped at 200
    public decimal EmployerMaxContribution { get; set; } // Capped at 300
    public int EffectiveYear { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
