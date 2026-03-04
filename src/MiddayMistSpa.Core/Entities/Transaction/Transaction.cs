using MiddayMistSpa.Core.Entities.Identity;

namespace MiddayMistSpa.Core.Entities.Transaction;

/// <summary>
/// POS transaction header with payment info and multi-currency support
/// </summary>
public class Transaction
{
    public int TransactionId { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public int? AppointmentId { get; set; } // Links to appointment if checkout after service

    // Amounts (in PHP)
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    public decimal DiscountPercentage { get; set; } = 0;
    public decimal TaxAmount { get; set; } = 0; // 12% VAT
    public decimal TipAmount { get; set; } = 0;
    public decimal TotalAmount { get; set; }

    // Payment
    public string PaymentMethod { get; set; } = string.Empty; // Cash, Card, Split
    public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid, Refunded, Voided
    public decimal? AmountTendered { get; set; } // Cash amount given by customer
    public decimal? ChangeAmount { get; set; } // Change returned to customer

    // Loyalty
    public int LoyaltyPointsEarned { get; set; } = 0;

    // Multi-Currency Support
    public string ClientCurrency { get; set; } = "PHP"; // Client's preferred currency
    public string? ClientCountryCode { get; set; } // From IpWhoIs (PH, US, JP, etc.)
    public string? ClientIPAddress { get; set; } // Client IP for geolocation
    public decimal ExchangeRate { get; set; } = 1.0m; // 1 PHP = X target currency
    public decimal? TotalInClientCurrency { get; set; } // Total converted to client currency

    // Tracking
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public DateTime? VoidedAt { get; set; }
    public int? VoidedBy { get; set; }
    public string? VoidReason { get; set; }

    public int CashierId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Concurrency
    public byte[] RowVersion { get; set; } = null!;

    // Computed properties
    public bool IsPaid => PaymentStatus == "Paid";
    public bool IsVoided => PaymentStatus == "Voided";
    public bool IsRefunded => PaymentStatus == "Refunded";

    // Navigation properties
    public virtual Customer.Customer Customer { get; set; } = null!;
    public virtual Appointment.Appointment? Appointment { get; set; }
    public virtual User Cashier { get; set; } = null!;
    public virtual User? VoidedByUser { get; set; }
    public virtual ICollection<TransactionServiceItem> ServiceItems { get; set; } = new List<TransactionServiceItem>();
    public virtual ICollection<TransactionProductItem> ProductItems { get; set; } = new List<TransactionProductItem>();
    public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();
}
