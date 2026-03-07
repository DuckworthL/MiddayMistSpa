using System.ComponentModel.DataAnnotations.Schema;
using MiddayMistSpa.Core.Entities.Customer;
using MiddayMistSpa.Core.Entities.Identity;

namespace MiddayMistSpa.Core.Entities.Accounting;

public class Invoice
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public int CustomerId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Sent, Partial, Paid, Overdue, Cancelled
    public string? Notes { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual MiddayMistSpa.Core.Entities.Customer.Customer Customer { get; set; } = null!;
    [ForeignKey("CreatedBy")]
    public virtual User CreatedByUser { get; set; } = null!;
    public virtual ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
}
