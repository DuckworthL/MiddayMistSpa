using System.Text.Json;
using MiddayMistSpa.API.DTOs.Currency;

namespace MiddayMistSpa.API.Services;

/// <summary>
/// Calls IpWhoIs.io API for IP geolocation (no API key required).
/// Returns country code and local currency for a given IP address.
/// </summary>
public interface IIpGeoLocationService
{
    /// <summary>
    /// Look up geolocation data for an IP address
    /// </summary>
    Task<GeoLocationResponse> GetGeoLocationAsync(string ipAddress);
}

public class IpWhoIsService : IIpGeoLocationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IpWhoIsService> _logger;

    public IpWhoIsService(HttpClient httpClient, ILogger<IpWhoIsService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<GeoLocationResponse> GetGeoLocationAsync(string ipAddress)
    {
        try
        {
            // IpWhoIs.io free API — no key needed
            // GET https://ipwhois.app/json/{ip}?objects=country_code,country,currency_code,currency
            var url = $"/json/{ipAddress}?objects=success,country_code,country,currency_code,currency";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("IpWhoIs API returned {StatusCode} for IP {IP}", response.StatusCode, ipAddress);
                return new GeoLocationResponse
                {
                    IpAddress = ipAddress,
                    Success = false,
                    ErrorMessage = $"API returned {response.StatusCode}"
                };
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<IpWhoIsResponse>(json);

            if (data == null || !data.success)
            {
                _logger.LogWarning("IpWhoIs API returned unsuccessful response for IP {IP}", ipAddress);
                return new GeoLocationResponse
                {
                    IpAddress = ipAddress,
                    Success = false,
                    ErrorMessage = "IP lookup returned no results"
                };
            }

            _logger.LogInformation("IP {IP} resolved to country {Country} ({Code}), currency {Currency}",
                ipAddress, data.country, data.country_code, data.currency_code);

            return new GeoLocationResponse
            {
                IpAddress = ipAddress,
                CountryCode = data.country_code ?? string.Empty,
                CountryName = data.country ?? string.Empty,
                CurrencyCode = data.currency_code ?? string.Empty,
                CurrencyName = data.currency ?? string.Empty,
                Success = true
            };
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("IpWhoIs API request timed out for IP {IP}", ipAddress);
            return new GeoLocationResponse
            {
                IpAddress = ipAddress,
                Success = false,
                ErrorMessage = "Request timed out"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling IpWhoIs API for IP {IP}", ipAddress);
            return new GeoLocationResponse
            {
                IpAddress = ipAddress,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Internal model matching IpWhoIs.io JSON response
    /// </summary>
    private class IpWhoIsResponse
    {
        public bool success { get; set; }
        public string? country_code { get; set; }
        public string? country { get; set; }
        public string? currency_code { get; set; }
        public string? currency { get; set; }
    }
}
