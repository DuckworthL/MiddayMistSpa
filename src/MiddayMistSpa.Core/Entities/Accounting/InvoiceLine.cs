namespace MiddayMistSpa.Core.Entities.Accounting;

public class InvoiceLine
{
    public int InvoiceLineId { get; set; }
    public int InvoiceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }

    // Navigation
    public virtual Invoice Invoice { get; set; } = null!;
}
