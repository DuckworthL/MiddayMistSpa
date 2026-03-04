namespace MiddayMistSpa.API.DTOs.Currency;

#region Currency Rate DTOs

/// <summary>
/// Response for a cached currency exchange rate
/// </summary>
public record CurrencyRateResponse
{
    public int RateId { get; init; }
    public string BaseCurrency { get; init; } = "PHP";
    public string TargetCurrency { get; init; } = string.Empty;
    public decimal ExchangeRate { get; init; }
    public DateTime LastUpdated { get; init; }
    public string Source { get; init; } = string.Empty;
    public bool IsStale { get; init; }
}

/// <summary>
/// All supported rates at a glance
/// </summary>
public record AllRatesResponse
{
    public string BaseCurrency { get; init; } = "PHP";
    public DateTime LastUpdated { get; init; }
    public List<CurrencyRateResponse> Rates { get; init; } = new();
}

#endregion

#region Currency Conversion DTOs

/// <summary>
/// Request to convert an amount between currencies
/// </summary>
public record ConvertCurrencyRequest
{
    public decimal Amount { get; init; }
    public string FromCurrency { get; init; } = "PHP";
    public string ToCurrency { get; init; } = "USD";
}

/// <summary>
/// Response with the converted amount
/// </summary>
public record ConvertCurrencyResponse
{
    public decimal OriginalAmount { get; init; }
    public string FromCurrency { get; init; } = string.Empty;
    public decimal ConvertedAmount { get; init; }
    public string ToCurrency { get; init; } = string.Empty;
    public decimal ExchangeRate { get; init; }
    public DateTime RateTimestamp { get; init; }
    public bool RateIsStale { get; init; }
}

#endregion

#region IP Geolocation DTOs

/// <summary>
/// Result of IP geolocation lookup via IpWhoIs.io
/// </summary>
public record GeoLocationResponse
{
    public string IpAddress { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public string CountryName { get; init; } = string.Empty;
    public string CurrencyCode { get; init; } = string.Empty;
    public string CurrencyName { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Detected client info for a POS session
/// </summary>
public record DetectedClientInfoResponse
{
    public string IpAddress { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public string CountryName { get; init; } = string.Empty;
    public string DetectedCurrency { get; init; } = string.Empty;
    public bool IsSupported { get; init; }
    public string SuggestedCurrency { get; init; } = "PHP";
    public decimal? ExchangeRate { get; init; }
}

#endregion

#region Supported Currencies

/// <summary>
/// Info about a supported currency
/// </summary>
public record SupportedCurrencyInfo
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Symbol { get; init; } = string.Empty;
    public decimal? CurrentRate { get; init; }
}

/// <summary>
/// List of all supported currencies with current rates
/// </summary>
public record SupportedCurrenciesResponse
{
    public string BaseCurrency { get; init; } = "PHP";
    public List<SupportedCurrencyInfo> Currencies { get; init; } = new();
}

#endregion
