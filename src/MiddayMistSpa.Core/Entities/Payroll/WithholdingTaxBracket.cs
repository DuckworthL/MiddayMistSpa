namespace MiddayMistSpa.Core.Entities.Payroll;

/// <summary>
/// BIR withholding tax brackets (progressive rates)
/// </summary>
public class WithholdingTaxBracket
{
    public int TaxBracketId { get; set; }
    public decimal MinIncome { get; set; }
    public decimal? MaxIncome { get; set; }
    public decimal BaseTax { get; set; }
    public decimal TaxRate { get; set; } // e.g., 0.15 for 15%
    public decimal ExcessOver { get; set; }
    public int EffectiveYear { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
