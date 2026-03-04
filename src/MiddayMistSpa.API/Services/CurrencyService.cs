using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiddayMistSpa.API.DTOs.Currency;
using MiddayMistSpa.API.Settings;
using MiddayMistSpa.Core.Entities.Configuration;
using MiddayMistSpa.Infrastructure.Data;

namespace MiddayMistSpa.API.Services;

/// <summary>
/// Central currency service that combines IP geolocation, exchange rate lookup,
/// currency conversion, and cached rate management.
/// </summary>
public interface ICurrencyService
{
    /// <summary>
    /// Get all cached exchange rates
    /// </summary>
    Task<AllRatesResponse> GetAllRatesAsync();

    /// <summary>
    /// Get a specific cached rate
    /// </summary>
    Task<CurrencyRateResponse?> GetRateAsync(string targetCurrency);

    /// <summary>
    /// Convert an amount between currencies using cached rates
    /// </summary>
    Task<ConvertCurrencyResponse> ConvertAsync(ConvertCurrencyRequest request);

    /// <summary>
    /// Detect client info from IP address (country, currency)
    /// </summary>
    Task<DetectedClientInfoResponse> DetectClientInfoAsync(string? ipAddress);

    /// <summary>
    /// Get list of supported currencies with current rates
    /// </summary>
    Task<SupportedCurrenciesResponse> GetSupportedCurrenciesAsync();

    /// <summary>
    /// Refresh all exchange rates from Frankfurter API and cache in DB
    /// </summary>
    Task<int> RefreshRatesAsync();
}

public class CurrencyService : ICurrencyService
{
    private readonly SpaDbContext _context;
    private readonly IFrankfurterService _frankfurter;
    private readonly IIpGeoLocationService _ipGeoLocation;
    private readonly CurrencySettings _settings;
    private readonly ILogger<CurrencyService> _logger;

    // Currency metadata for display purposes
    private static readonly Dictionary<string, (string Name, string Symbol)> CurrencyMeta = new()
    {
        ["PHP"] = ("Philippine Peso", "₱"),
        ["USD"] = ("US Dollar", "$"),
        ["EUR"] = ("Euro", "€"),
        ["JPY"] = ("Japanese Yen", "¥"),
        ["GBP"] = ("British Pound", "£"),
        ["SGD"] = ("Singapore Dollar", "S$"),
        ["AUD"] = ("Australian Dollar", "A$"),
        ["KRW"] = ("South Korean Won", "₩"),
        ["CNY"] = ("Chinese Yuan", "¥"),
    };

    public CurrencyService(
        SpaDbContext context,
        IFrankfurterService frankfurter,
        IIpGeoLocationService ipGeoLocation,
        IOptions<CurrencySettings> settings,
        ILogger<CurrencyService> logger)
    {
        _context = context;
        _frankfurter = frankfurter;
        _ipGeoLocation = ipGeoLocation;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<AllRatesResponse> GetAllRatesAsync()
    {
        var rates = await _context.CurrencyRates
            .AsNoTracking()
            .Where(r => r.BaseCurrency == _settings.BaseCurrency)
            .OrderBy(r => r.TargetCurrency)
            .ToListAsync();

        return new AllRatesResponse
        {
            BaseCurrency = _settings.BaseCurrency,
            LastUpdated = rates.Any() ? rates.Max(r => r.LastUpdated) : DateTime.MinValue,
            Rates = rates.Select(MapToResponse).ToList()
        };
    }

    public async Task<CurrencyRateResponse?> GetRateAsync(string targetCurrency)
    {
        var rate = await _context.CurrencyRates
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.BaseCurrency == _settings.BaseCurrency
                                   && r.TargetCurrency == targetCurrency.ToUpper());

        return rate == null ? null : MapToResponse(rate);
    }

    public async Task<ConvertCurrencyResponse> ConvertAsync(ConvertCurrencyRequest request)
    {
        var from = request.FromCurrency.ToUpper();
        var to = request.ToCurrency.ToUpper();

        if (from == to)
        {
            return new ConvertCurrencyResponse
            {
                OriginalAmount = request.Amount,
                FromCurrency = from,
                ConvertedAmount = request.Amount,
                ToCurrency = to,
                ExchangeRate = 1.0m,
                RateTimestamp = DateTime.UtcNow,
                RateIsStale = false
            };
        }

        // Determine which rate we need from the cache (always stored as PHP -> X)
        decimal exchangeRate;
        DateTime rateTimestamp;
        bool isStale;

        if (from == _settings.BaseCurrency)
        {
            // PHP -> target: use the cached rate directly
            var rate = await _context.CurrencyRates
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.BaseCurrency == _settings.BaseCurrency && r.TargetCurrency == to)
                ?? throw new InvalidOperationException($"No exchange rate found for {from} -> {to}. Run rate refresh first.");

            exchangeRate = rate.ExchangeRate;
            rateTimestamp = rate.LastUpdated;
            isStale = rate.IsStale;
        }
        else if (to == _settings.BaseCurrency)
        {
            // Foreign -> PHP: invert the cached PHP -> foreign rate
            var rate = await _context.CurrencyRates
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.BaseCurrency == _settings.BaseCurrency && r.TargetCurrency == from)
                ?? throw new InvalidOperationException($"No exchange rate found for {from} -> {to}. Run rate refresh first.");

            exchangeRate = 1.0m / rate.ExchangeRate;
            rateTimestamp = rate.LastUpdated;
            isStale = rate.IsStale;
        }
        else
        {
            // Cross-rate: foreign1 -> PHP -> foreign2
            var fromRate = await _context.CurrencyRates
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.BaseCurrency == _settings.BaseCurrency && r.TargetCurrency == from)
                ?? throw new InvalidOperationException($"No exchange rate found for {_settings.BaseCurrency} -> {from}.");

            var toRate = await _context.CurrencyRates
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.BaseCurrency == _settings.BaseCurrency && r.TargetCurrency == to)
                ?? throw new InvalidOperationException($"No exchange rate found for {_settings.BaseCurrency} -> {to}.");

            // from -> PHP: 1/fromRate, then PHP -> to: toRate
            exchangeRate = toRate.ExchangeRate / fromRate.ExchangeRate;
            rateTimestamp = fromRate.LastUpdated < toRate.LastUpdated ? fromRate.LastUpdated : toRate.LastUpdated;
            isStale = fromRate.IsStale || toRate.IsStale;
        }

        var convertedAmount = Math.Round(request.Amount * exchangeRate, 2);

        return new ConvertCurrencyResponse
        {
            OriginalAmount = request.Amount,
            FromCurrency = from,
            ConvertedAmount = convertedAmount,
            ToCurrency = to,
            ExchangeRate = Math.Round(exchangeRate, 6),
            RateTimestamp = rateTimestamp,
            RateIsStale = isStale
        };
    }

    public async Task<DetectedClientInfoResponse> DetectClientInfoAsync(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || ipAddress == "::1" || ipAddress == "127.0.0.1")
        {
            // Localhost — default to PHP
            return new DetectedClientInfoResponse
            {
                IpAddress = ipAddress ?? "unknown",
                CountryCode = "PH",
                CountryName = "Philippines",
                DetectedCurrency = "PHP",
                IsSupported = true,
                SuggestedCurrency = "PHP",
                ExchangeRate = 1.0m
            };
        }

        var geoResult = await _ipGeoLocation.GetGeoLocationAsync(ipAddress);

        if (!geoResult.Success)
        {
            _logger.LogWarning("IP geolocation failed for {IP}, defaulting to PHP", ipAddress);
            return new DetectedClientInfoResponse
            {
                IpAddress = ipAddress,
                CountryCode = "PH",
                CountryName = "Unknown",
                DetectedCurrency = "PHP",
                IsSupported = true,
                SuggestedCurrency = "PHP",
                ExchangeRate = 1.0m
            };
        }

        var detectedCurrency = geoResult.CurrencyCode;
        var isSupported = detectedCurrency == _settings.BaseCurrency
                       || _settings.SupportedCurrencies.Contains(detectedCurrency);

        // If the detected currency is supported, get the exchange rate
        decimal? exchangeRate = null;
        var suggestedCurrency = isSupported ? detectedCurrency : _settings.BaseCurrency;

        if (suggestedCurrency != _settings.BaseCurrency)
        {
            var rate = await _context.CurrencyRates
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.BaseCurrency == _settings.BaseCurrency
                                       && r.TargetCurrency == suggestedCurrency);
            exchangeRate = rate?.ExchangeRate;
        }
        else
        {
            exchangeRate = 1.0m;
        }

        return new DetectedClientInfoResponse
        {
            IpAddress = ipAddress,
            CountryCode = geoResult.CountryCode,
            CountryName = geoResult.CountryName,
            DetectedCurrency = detectedCurrency,
            IsSupported = isSupported,
            SuggestedCurrency = suggestedCurrency,
            ExchangeRate = exchangeRate
        };
    }

    public async Task<SupportedCurrenciesResponse> GetSupportedCurrenciesAsync()
    {
        var rates = await _context.CurrencyRates
            .AsNoTracking()
            .Where(r => r.BaseCurrency == _settings.BaseCurrency)
            .ToDictionaryAsync(r => r.TargetCurrency, r => r.ExchangeRate);

        var currencies = new List<SupportedCurrencyInfo>
        {
            // Always include base currency
            new()
            {
                Code = _settings.BaseCurrency,
                Name = GetCurrencyName(_settings.BaseCurrency),
                Symbol = GetCurrencySymbol(_settings.BaseCurrency),
                CurrentRate = 1.0m
            }
        };

        foreach (var code in _settings.SupportedCurrencies.Distinct().OrderBy(c => c))
        {
            currencies.Add(new SupportedCurrencyInfo
            {
                Code = code,
                Name = GetCurrencyName(code),
                Symbol = GetCurrencySymbol(code),
                CurrentRate = rates.TryGetValue(code, out var rate) ? rate : null
            });
        }

        return new SupportedCurrenciesResponse
        {
            BaseCurrency = _settings.BaseCurrency,
            Currencies = currencies
        };
    }

    public async Task<int> RefreshRatesAsync()
    {
        _logger.LogInformation("Refreshing exchange rates from Frankfurter API...");

        var latestRates = await _frankfurter.GetLatestRatesAsync();
        if (latestRates == null || latestRates.Count == 0)
        {
            _logger.LogWarning("No rates received from Frankfurter API");
            return 0;
        }

        var now = DateTime.UtcNow;
        var updated = 0;

        foreach (var (currency, rate) in latestRates)
        {
            var existing = await _context.CurrencyRates
                .FirstOrDefaultAsync(r => r.BaseCurrency == _settings.BaseCurrency
                                       && r.TargetCurrency == currency);

            if (existing != null)
            {
                existing.ExchangeRate = rate;
                existing.LastUpdated = now;
                existing.Source = "Frankfurter";
            }
            else
            {
                _context.CurrencyRates.Add(new CurrencyRate
                {
                    BaseCurrency = _settings.BaseCurrency,
                    TargetCurrency = currency,
                    ExchangeRate = rate,
                    LastUpdated = now,
                    Source = "Frankfurter"
                });
            }

            updated++;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Refreshed {Count} exchange rates (base: {Base})", updated, _settings.BaseCurrency);
        return updated;
    }

    #region Private Helpers

    private static CurrencyRateResponse MapToResponse(CurrencyRate rate) => new()
    {
        RateId = rate.RateId,
        BaseCurrency = rate.BaseCurrency,
        TargetCurrency = rate.TargetCurrency,
        ExchangeRate = rate.ExchangeRate,
        LastUpdated = rate.LastUpdated,
        Source = rate.Source,
        IsStale = rate.IsStale
    };

    private static string GetCurrencyName(string code) =>
        CurrencyMeta.TryGetValue(code, out var meta) ? meta.Name : code;

    private static string GetCurrencySymbol(string code) =>
        CurrencyMeta.TryGetValue(code, out var meta) ? meta.Symbol : code;

    #endregion
}
