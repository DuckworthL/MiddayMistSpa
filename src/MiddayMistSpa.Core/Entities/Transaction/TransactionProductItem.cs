using MiddayMistSpa.Core.Entities.Identity;

namespace MiddayMistSpa.Core.Entities.Transaction;

/// <summary>
/// Product/retail line items in a transaction with commission tracking
/// </summary>
public class TransactionProductItem
{
    public int TransactionProductItemId { get; set; }
    public int TransactionId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal CommissionRate { get; set; } = 0.10m; // 10% default for retail
    public decimal CommissionAmount { get; set; }
    public int? SoldBy { get; set; } // Employee who sold the product
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Transaction Transaction { get; set; } = null!;
    public virtual Inventory.Product Product { get; set; } = null!;
    public virtual User? SoldByUser { get; set; }
}
