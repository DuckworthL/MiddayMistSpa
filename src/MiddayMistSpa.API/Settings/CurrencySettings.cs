namespace MiddayMistSpa.API.Settings;

public class CurrencySettings
{
    public const string SectionName = "CurrencySettings";

    /// <summary>
    /// Base currency for all transactions (PHP for Philippine spa)
    /// </summary>
    public string BaseCurrency { get; set; } = "PHP";

    /// <summary>
    /// Currencies supported for multi-currency display.
    /// Configured via appsettings.json CurrencySettings:SupportedCurrencies.
    /// </summary>
    public List<string> SupportedCurrencies { get; set; } = [];

    /// <summary>
    /// How often to refresh exchange rates from Frankfurter API (minutes)
    /// </summary>
    public int RefreshIntervalMinutes { get; set; } = 30;

    /// <summary>
    /// Frankfurter API base URL (ECB exchange rates, no key needed)
    /// </summary>
    public string FrankfurterBaseUrl { get; set; } = "https://api.frankfurter.dev";

    /// <summary>
    /// IpWhoIs API base URL (IP geolocation, no key needed)
    /// </summary>
    public string IpWhoIsBaseUrl { get; set; } = "https://ipwhois.app";

    /// <summary>
    /// Hours after which a cached rate is considered stale
    /// </summary>
    public int StaleThresholdHours { get; set; } = 1;
}
