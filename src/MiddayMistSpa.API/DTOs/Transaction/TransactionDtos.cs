using System.ComponentModel.DataAnnotations;
using MiddayMistSpa.API.DTOs.Employee;

namespace MiddayMistSpa.API.DTOs.Transaction;

// ============================================================================
// Transaction Request DTOs
// ============================================================================

public class CreateTransactionRequest
{
    [Required]
    public int CustomerId { get; set; }

    public int? AppointmentId { get; set; }

    public List<CreateTransactionServiceItemRequest> ServiceItems { get; set; } = new();

    public List<CreateTransactionProductItemRequest> ProductItems { get; set; } = new();

    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; } = 0;

    [Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; } = 0;

    [Range(0, double.MaxValue)]
    public decimal TipAmount { get; set; } = 0;

    [Required, StringLength(20)]
    public string PaymentMethod { get; set; } = "Cash"; // Cash, Card, Split

    [Range(0, double.MaxValue)]
    public decimal? AmountTendered { get; set; } // For cash payments

    // Multi-Currency support
    /// <summary>
    /// Client's preferred display currency (e.g. "USD", "EUR"). Defaults to "PHP" if omitted.
    /// The transaction is always processed in PHP; this triggers a conversion display.
    /// </summary>
    [StringLength(3)]
    public string? ClientCurrency { get; set; }

    /// <summary>
    /// Client IP address for geo-location currency detection (optional).
    /// </summary>
    public string? ClientIPAddress { get; set; }
}

public class CreateTransactionServiceItemRequest
{
    [Required]
    public int ServiceId { get; set; }

    public int? TherapistId { get; set; }

    [Range(1, 100)]
    public int Quantity { get; set; } = 1;

    public decimal? UnitPrice { get; set; } // Override price if needed
}

public class CreateTransactionProductItemRequest
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, 1000)]
    public int Quantity { get; set; } = 1;

    public decimal? UnitPrice { get; set; } // Override price if needed
}

public class ProcessPaymentRequest
{
    [Required, StringLength(20)]
    public string PaymentMethod { get; set; } = "Cash";

    [Range(0, double.MaxValue)]
    public decimal? AmountTendered { get; set; } // For cash payments
}

public class FinalizePendingTransactionRequest
{
    public List<CreateTransactionProductItemRequest> ProductItems { get; set; } = new();

    [Required, StringLength(20)]
    public string PaymentMethod { get; set; } = "Cash";

    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; } = 0;

    [Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; } = 0;

    [Range(0, double.MaxValue)]
    public decimal TipAmount { get; set; } = 0;

    [Range(0, double.MaxValue)]
    public decimal? AmountTendered { get; set; }

    public string? ClientCurrency { get; set; }
    public string? ClientIPAddress { get; set; }
}

public class VoidTransactionRequest
{
    [Required, StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class CreateRefundRequest
{
    [Required, Range(0.01, double.MaxValue)]
    public decimal RefundAmount { get; set; }

    [Required, StringLength(20)]
    public string RefundMethod { get; set; } = "Cash"; // Cash, Card Reversal

    [Required, StringLength(20)]
    public string RefundType { get; set; } = "Partial"; // Full, Partial

    [Required, StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Optional: product items being refunded (for stock restoration).
    /// If omitted on Full refund, all product stock is restored.
    /// </summary>
    public List<RefundProductItemRequest>? ProductItems { get; set; }
}

public class RefundProductItemRequest
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, 1000)]
    public int Quantity { get; set; } = 1;
}

// ============================================================================
// Transaction Response DTOs
// ============================================================================

public class TransactionResponse
{
    public int TransactionId { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;

    // Customer Info
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string MembershipType { get; set; } = string.Empty;

    public int? AppointmentId { get; set; }

    // Amounts
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TipAmount { get; set; }
    public decimal TotalAmount { get; set; }

    // Payment
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal? AmountTendered { get; set; }
    public decimal? ChangeAmount { get; set; }

    // Loyalty
    public int LoyaltyPointsEarned { get; set; }

    // Tracking
    public DateTime TransactionDate { get; set; }
    public int CashierId { get; set; }
    public string CashierName { get; set; } = string.Empty;

    // Voiding
    public DateTime? VoidedAt { get; set; }
    public string? VoidedByName { get; set; }
    public string? VoidReason { get; set; }

    // Items
    public List<TransactionServiceItemResponse> ServiceItems { get; set; } = new();
    public List<TransactionProductItemResponse> ProductItems { get; set; } = new();
    public List<RefundResponse> Refunds { get; set; } = new();

    // Multi-Currency
    public string ClientCurrency { get; set; } = "PHP";
    public string? ClientCountryCode { get; set; }
    public string? ClientIPAddress { get; set; }
    public decimal ExchangeRate { get; set; } = 1.0m;
    public decimal? TotalInClientCurrency { get; set; }

    // Computed
    public decimal TotalRefunded => Refunds.Sum(r => r.RefundAmount);
    public decimal NetAmount => TotalAmount - TotalRefunded;

    public DateTime CreatedAt { get; set; }
}

public class TransactionListResponse
{
    public int TransactionId { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public int ServiceItemCount { get; set; }
    public int ProductItemCount { get; set; }
    public DateTime TransactionDate { get; set; }

    // Multi-Currency
    public string ClientCurrency { get; set; } = "PHP";
    public decimal? TotalInClientCurrency { get; set; }
}

public class TransactionServiceItemResponse
{
    public int TransactionServiceItemId { get; set; }
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceCode { get; set; } = string.Empty;
    public int? TherapistId { get; set; }
    public string? TherapistName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
}

public class TransactionProductItemResponse
{
    public int TransactionProductItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
}

public class RefundResponse
{
    public int RefundId { get; set; }
    public int TransactionId { get; set; }
    public decimal RefundAmount { get; set; }
    public string RefundMethod { get; set; } = string.Empty;
    public string RefundType { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string ApprovedByName { get; set; } = string.Empty;
    public string ProcessedByName { get; set; } = string.Empty;
    public DateTime RefundDate { get; set; }
}

// ============================================================================
// Search & Filter DTOs
// ============================================================================

public class TransactionSearchRequest
{
    public string? SearchTerm { get; set; }
    public int? CustomerId { get; set; }
    public int? CashierId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? PaymentStatus { get; set; }
    public string? PaymentMethod { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public string? SortBy { get; set; } = "date";
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// ============================================================================
// POS Dashboard & Reports DTOs
// ============================================================================

public class POSDashboardResponse
{
    public DateTime Date { get; set; }
    public int TotalTransactions { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalTips { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal TotalRefunds { get; set; }
    public decimal NetSales { get; set; }
    public Dictionary<string, int> ByPaymentMethod { get; set; } = new();
    public Dictionary<string, decimal> AmountByPaymentMethod { get; set; } = new();
    public int VoidedCount { get; set; }
    public int RefundedCount { get; set; }
}

public class DailySalesReportResponse
{
    public DateTime Date { get; set; }
    public int TransactionCount { get; set; }
    public decimal GrossSales { get; set; }
    public decimal DiscountsGiven { get; set; }
    public decimal TaxCollected { get; set; }
    public decimal TipsReceived { get; set; }
    public decimal RefundsProcessed { get; set; }
    public decimal NetSales { get; set; }
    public decimal ServiceRevenue { get; set; }
    public decimal ProductRevenue { get; set; }
    public decimal TotalCommissions { get; set; }
}

public class TransactionSalesReportResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalTransactions { get; set; }
    public decimal GrossSales { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalTips { get; set; }
    public decimal TotalRefunds { get; set; }
    public decimal NetSales { get; set; }
    public decimal ServiceRevenue { get; set; }
    public decimal ProductRevenue { get; set; }
    public decimal TotalCommissions { get; set; }
    public decimal AverageTransactionValue { get; set; }
    public Dictionary<string, decimal> ByPaymentMethod { get; set; } = new();
    public List<TopServiceResponse> TopServices { get; set; } = new();
    public List<TopProductResponse> TopProducts { get; set; } = new();
}

public class TopServiceResponse
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class TopProductResponse
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class CashierShiftReportResponse
{
    public int CashierId { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public DateTime ShiftDate { get; set; }
    public int TransactionCount { get; set; }
    public decimal TotalSales { get; set; }
    public decimal CashSales { get; set; }
    public decimal CardSales { get; set; }
    public decimal TipsCollected { get; set; }
    public int VoidsCount { get; set; }
    public int RefundsCount { get; set; }
    public decimal RefundsAmount { get; set; }
}

// ============================================================================
// Receipt DTOs
// ============================================================================

public class ReceiptResponse
{
    public string TransactionNumber { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }

    // Business Info
    public string BusinessName { get; set; } = "MiddayMist Spa";
    public string? BusinessAddress { get; set; }
    public string? BusinessPhone { get; set; }
    public string? BusinessTIN { get; set; }

    // Customer
    public string CustomerName { get; set; } = string.Empty;
    public string MembershipType { get; set; } = string.Empty;

    // Items
    public List<ReceiptItemResponse> Items { get; set; } = new();

    // Totals
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TipAmount { get; set; }
    public decimal TotalAmount { get; set; }

    // Payment
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal? AmountTendered { get; set; }
    public decimal? ChangeAmount { get; set; }

    public string CashierName { get; set; } = string.Empty;
    public int LoyaltyPointsEarned { get; set; }
    public int LoyaltyPointsBalance { get; set; }
    public string? ThankYouMessage { get; set; }
}

public class ReceiptItemResponse
{
    public string ItemType { get; set; } = string.Empty; // Service, Product
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? TherapistName { get; set; }
}
