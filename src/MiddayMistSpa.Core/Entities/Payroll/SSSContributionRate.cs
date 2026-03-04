namespace MiddayMistSpa.Core.Entities.Payroll;

/// <summary>
/// SSS contribution rates by salary bracket
/// </summary>
public class SSSContributionRate
{
    public int SSSRateId { get; set; }
    public decimal MinSalary { get; set; }
    public decimal MaxSalary { get; set; }
    public decimal SalaryCredit { get; set; }
    public decimal EmployeeShare { get; set; }
    public decimal EmployerShare { get; set; }
    public decimal TotalContribution { get; set; }
    public int EffectiveYear { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
