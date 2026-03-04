namespace MiddayMistSpa.Core.Entities.Transaction;

/// <summary>
/// Service line items in a transaction with commission tracking
/// </summary>
public class TransactionServiceItem
{
    public int TransactionServiceItemId { get; set; }
    public int TransactionId { get; set; }
    public int ServiceId { get; set; }
    public int? TherapistId { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Transaction Transaction { get; set; } = null!;
    public virtual Service.Service Service { get; set; } = null!;
    public virtual Employee.Employee? Therapist { get; set; }
}
