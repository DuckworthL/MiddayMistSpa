namespace MiddayMistSpa.Core.Entities.Configuration;

/// <summary>
/// Cached exchange rates from Frankfurter API (ECB data).
/// Base currency is always PHP. Rates are refreshed every 30 minutes.
/// </summary>
public class CurrencyRate
{
    public int RateId { get; set; }
    public string BaseCurrency { get; set; } = "PHP"; // Always PHP
    public string TargetCurrency { get; set; } = string.Empty; // USD, EUR, JPY, etc.
    public decimal ExchangeRate { get; set; } // How much 1 PHP = in target currency
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = "Frankfurter"; // API source

    // Computed properties
    public bool IsStale => (DateTime.UtcNow - LastUpdated).TotalHours > 1;
}
