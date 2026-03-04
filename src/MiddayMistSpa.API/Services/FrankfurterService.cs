using System.Text.Json;
using Microsoft.Extensions.Options;
using MiddayMistSpa.API.Settings;

namespace MiddayMistSpa.API.Services;

/// <summary>
/// Calls Frankfurter API (ECB exchange rates, no API key required).
/// Fetches exchange rates with PHP as the base currency.
/// API docs: https://frankfurter.dev
/// </summary>
public interface IFrankfurterService
{
    /// <summary>
    /// Get latest exchange rates for all supported currencies from PHP
    /// </summary>
    Task<Dictionary<string, decimal>?> GetLatestRatesAsync();

    /// <summary>
    /// Get exchange rate for a specific currency pair
    /// </summary>
    Task<decimal?> GetRateAsync(string fromCurrency, string toCurrency);
}

public class FrankfurterService : IFrankfurterService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FrankfurterService> _logger;
    private readonly CurrencySettings _settings;

    public FrankfurterService(HttpClient httpClient, ILogger<FrankfurterService> logger, IOptions<CurrencySettings> settings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<Dictionary<string, decimal>?> GetLatestRatesAsync()
    {
        try
        {
            // Frankfurter API: GET /latest?base=PHP&symbols=USD,EUR,JPY,...
            var symbols = string.Join(",", _settings.SupportedCurrencies);
            var url = $"/v1/latest?base={_settings.BaseCurrency}&symbols={symbols}";

            _logger.LogInformation("Fetching exchange rates from Frankfurter: {Url}", url);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Frankfurter API returned {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<FrankfurterResponse>(json);

            if (data?.rates == null || data.rates.Count == 0)
            {
                _logger.LogWarning("Frankfurter API returned no rates");
                return null;
            }

            _logger.LogInformation("Fetched {Count} exchange rates from Frankfurter (base: {Base}, date: {Date})",
                data.rates.Count, data.@base, data.date);

            return data.rates;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Frankfurter API request timed out");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching exchange rates from Frankfurter API");
            return null;
        }
    }

    public async Task<decimal?> GetRateAsync(string fromCurrency, string toCurrency)
    {
        try
        {
            var url = $"/v1/latest?base={fromCurrency}&symbols={toCurrency}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Frankfurter API returned {StatusCode} for {From}->{To}",
                    response.StatusCode, fromCurrency, toCurrency);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<FrankfurterResponse>(json);

            if (data?.rates != null && data.rates.TryGetValue(toCurrency, out var rate))
            {
                return rate;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching rate {From}->{To} from Frankfurter", fromCurrency, toCurrency);
            return null;
        }
    }

    /// <summary>
    /// Internal model matching Frankfurter API JSON response
    /// </summary>
    private class FrankfurterResponse
    {
        public decimal amount { get; set; }
        public string? @base { get; set; }
        public string? date { get; set; }
        public Dictionary<string, decimal>? rates { get; set; }
    }
}
