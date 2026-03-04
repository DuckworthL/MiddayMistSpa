namespace MiddayMistSpa.Core;

/// <summary>
/// Domain constants and allowed values for string-typed fields.
/// These serve as validation references across the application.
/// </summary>
public static class DomainConstants
{
    // ========================================================================
    // Account Types (Chart of Accounts)
    // ========================================================================
    public static class AccountTypes
    {
        public const string Asset = "Asset";
        public const string Liability = "Liability";
        public const string Equity = "Equity";
        public const string Revenue = "Revenue";
        public const string Expense = "Expense";

        public static readonly string[] All = { Asset, Liability, Equity, Revenue, Expense };

        public static bool IsValid(string? value) =>
            !string.IsNullOrWhiteSpace(value) && All.Contains(value, StringComparer.OrdinalIgnoreCase);
    }

    // ========================================================================
    // Payment Methods (Transaction)
    // ========================================================================
    public static class PaymentMethods
    {
        public const string Cash = "Cash";
        public const string Card = "Card";
        public const string GCash = "GCash";
        public const string Maya = "Maya";
        public const string BankTransfer = "Bank Transfer";
        public const string Split = "Split";

        public static readonly string[] All = { Cash, Card, GCash, Maya, BankTransfer, Split };

        public static bool IsValid(string? value) =>
            !string.IsNullOrWhiteSpace(value) && All.Contains(value, StringComparer.OrdinalIgnoreCase);
    }

    // ========================================================================
    // Payment Statuses (Transaction)
    // ========================================================================
    public static class PaymentStatuses
    {
        public const string Pending = "Pending";
        public const string Paid = "Paid";
        public const string Refunded = "Refunded";
        public const string Voided = "Voided";

        public static readonly string[] All = { Pending, Paid, Refunded, Voided };
    }

    // ========================================================================
    // Stock Adjustment Types (Inventory)
    // ========================================================================
    public static class StockAdjustmentTypes
    {
        public const string Received = "Received";
        public const string Sold = "Sold";
        public const string ServiceUsage = "Service Usage";
        public const string Damaged = "Damaged";
        public const string Expired = "Expired";
        public const string Spoilage = "Spoilage";
        public const string Shrinkage = "Shrinkage";
        public const string Audit = "Audit";
        public const string Increase = "Increase";
        public const string Decrease = "Decrease";
        public const string ReturnToStock = "Return to Stock";

        public static readonly string[] All =
        {
            Received, Sold, ServiceUsage, Damaged, Expired,
            Spoilage, Shrinkage, Audit, Increase, Decrease, ReturnToStock
        };

        public static bool IsValid(string? value) =>
            !string.IsNullOrWhiteSpace(value) && All.Contains(value, StringComparer.OrdinalIgnoreCase);
    }

    // ========================================================================
    // Product Types (Inventory)
    // ========================================================================
    public static class ProductTypes
    {
        public const string Retail = "Retail";
        public const string Supply = "Supply";
        public const string Consumable = "Consumable";

        public static readonly string[] All = { Retail, Supply, Consumable };
    }

    // ========================================================================
    // Purchase Order Statuses
    // ========================================================================
    public static class PurchaseOrderStatuses
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string PartiallyReceived = "Partially Received";
        public const string Delivered = "Delivered";
        public const string Cancelled = "Cancelled";

        public static readonly string[] All = { Pending, Approved, PartiallyReceived, Delivered, Cancelled };
    }

    // ========================================================================
    // Appointment Statuses
    // ========================================================================
    public static class AppointmentStatuses
    {
        public const string Scheduled = "Scheduled";
        public const string Confirmed = "Confirmed";
        public const string CheckedIn = "Checked In";
        public const string InProgress = "In Progress";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
        public const string NoShow = "No Show";

        public static readonly string[] All = { Scheduled, Confirmed, CheckedIn, InProgress, Completed, Cancelled, NoShow };
    }

    // ========================================================================
    // Payroll Statuses
    // ========================================================================
    public static class PayrollPeriodStatuses
    {
        public const string Draft = "Draft";
        public const string Processing = "Processing";
        public const string Finalized = "Finalized";
        public const string Paid = "Paid";

        public static readonly string[] All = { Draft, Processing, Finalized, Paid };
    }

    // ========================================================================
    // Journal Entry Statuses
    // ========================================================================
    public static class JournalEntryStatuses
    {
        public const string Draft = "Draft";
        public const string Posted = "Posted";
        public const string Voided = "Voided";

        public static readonly string[] All = { Draft, Posted, Voided };
    }

    // ========================================================================
    // Loyalty Point Transaction Types
    // ========================================================================
    public static class LoyaltyTransactionTypes
    {
        public const string Earn = "Earn";
        public const string Redeem = "Redeem";
        public const string Expire = "Expire";
        public const string Adjust = "Adjust";

        public static readonly string[] All = { Earn, Redeem, Expire, Adjust };
    }

    // ========================================================================
    // Customer Membership Tiers
    // ========================================================================
    public static class MembershipTiers
    {
        public const string Regular = "Regular";
        public const string Bronze = "Bronze";
        public const string Silver = "Silver";
        public const string Gold = "Gold";
        public const string Platinum = "Platinum";

        public static readonly string[] All = { Regular, Bronze, Silver, Gold, Platinum };

        // Tier thresholds (total non-expired loyalty points)
        public const int BronzeThreshold = 100;
        public const int SilverThreshold = 500;
        public const int GoldThreshold = 1000;
        public const int PlatinumThreshold = 2000;

        // Tier discount percentages
        public static decimal GetTierDiscount(string tier) => tier switch
        {
            Bronze => 0.00m,
            Silver => 0.05m,
            Gold => 0.10m,
            Platinum => 0.15m,
            _ => 0.00m
        };

        public static string GetTierForPoints(int totalPoints) => totalPoints switch
        {
            >= PlatinumThreshold => Platinum,
            >= GoldThreshold => Gold,
            >= SilverThreshold => Silver,
            >= BronzeThreshold => Bronze,
            _ => Regular
        };
    }

    // ========================================================================
    // Communication Channels
    // ========================================================================
    public static class CommunicationChannels
    {
        public const string Email = "Email";
        public const string SMS = "SMS";
        public const string Both = "Both";
        public const string None = "None";

        public static readonly string[] All = { Email, SMS, Both, None };
    }

    // ========================================================================
    // Loyalty Configuration Defaults
    // ========================================================================
    public static class LoyaltyConfig
    {
        /// <summary>Points earned per ₱100 spent (default: 1)</summary>
        public const int DefaultPointsPerHundredPesos = 1;

        /// <summary>Months before earned points expire (default: 12)</summary>
        public const int DefaultExpiryMonths = 12;
    }
}
