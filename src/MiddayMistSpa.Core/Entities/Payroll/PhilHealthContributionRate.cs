namespace MiddayMistSpa.Core.Entities.Payroll;

/// <summary>
/// PhilHealth contribution rates (5% premium, split 50/50)
/// </summary>
public class PhilHealthContributionRate
{
    public int PhilHealthRateId { get; set; }
    public decimal MinSalary { get; set; }
    public decimal MaxSalary { get; set; }
    public decimal PremiumRate { get; set; } // e.g., 0.05 for 5%
    public decimal EmployeeShare { get; set; } // e.g., 0.025 for 2.5%
    public decimal EmployerShare { get; set; } // e.g., 0.025 for 2.5%
    public int EffectiveYear { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
