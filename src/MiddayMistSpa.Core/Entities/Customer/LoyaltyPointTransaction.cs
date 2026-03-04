namespace MiddayMistSpa.Core.Entities.Customer;

/// <summary>
/// Individual loyalty point earn/redeem/expire records for batch-level tracking with expiry
/// </summary>
public class LoyaltyPointTransaction
{
    public int LoyaltyPointTransactionId { get; set; }
    public int CustomerId { get; set; }

    /// <summary>
    /// Earn, Redeem, Expire, Adjust
    /// </summary>
    public string TransactionType { get; set; } = string.Empty;

    /// <summary>
    /// Positive for earning/adjustment, negative for redeem/expire
    /// </summary>
    public int Points { get; set; }

    /// <summary>
    /// Remaining points from this batch (for expiry tracking). Only relevant for "Earn" type.
    /// </summary>
    public int BalanceRemaining { get; set; }

    /// <summary>
    /// Date the points were earned
    /// </summary>
    public DateTime EarnedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Expiry date for earned points (12 months from earn date). Null for non-earn types.
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// The POS transaction that triggered these points (null for manual adjustments/expiry)
    /// </summary>
    public int? TransactionId { get; set; }

    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Computed
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;

    // Navigation properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual Transaction.Transaction? Transaction { get; set; }
}
