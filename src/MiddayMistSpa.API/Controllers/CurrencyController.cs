using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiddayMistSpa.API.DTOs.Currency;
using MiddayMistSpa.API.Services;

namespace MiddayMistSpa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CurrencyController : ControllerBase
{
    private readonly ICurrencyService _currencyService;
    private readonly ILogger<CurrencyController> _logger;

    public CurrencyController(ICurrencyService currencyService, ILogger<CurrencyController> logger)
    {
        _currencyService = currencyService;
        _logger = logger;
    }

    /// <summary>
    /// Get all cached exchange rates
    /// </summary>
    [HttpGet("rates")]
    [Authorize(Policy = "AllStaff")]
    public async Task<ActionResult<AllRatesResponse>> GetAllRates()
    {
        var rates = await _currencyService.GetAllRatesAsync();
        return Ok(rates);
    }

    /// <summary>
    /// Get exchange rate for a specific currency (e.g., /api/currency/rates/USD)
    /// </summary>
    [HttpGet("rates/{currency}")]
    [Authorize(Policy = "AllStaff")]
    public async Task<ActionResult<CurrencyRateResponse>> GetRate(string currency)
    {
        var rate = await _currencyService.GetRateAsync(currency);
        if (rate == null)
            return NotFound(new { message = $"No exchange rate found for {currency.ToUpper()}" });

        return Ok(rate);
    }

    /// <summary>
    /// Convert an amount between currencies
    /// </summary>
    [HttpPost("convert")]
    [Authorize(Policy = "AllStaff")]
    public async Task<ActionResult<ConvertCurrencyResponse>> Convert([FromBody] ConvertCurrencyRequest request)
    {
        try
        {
            if (request.Amount <= 0)
                return BadRequest(new { message = "Amount must be greater than zero" });

            var result = await _currencyService.ConvertAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting currency");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Detect client's country and currency from their IP address.
    /// Uses the request's remote IP if none specified.
    /// </summary>
    [HttpGet("detect")]
    [Authorize(Policy = "AllStaff")]
    public async Task<ActionResult<DetectedClientInfoResponse>> DetectClientInfo([FromQuery] string? ip = null)
    {
        var ipAddress = ip ?? HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _currencyService.DetectClientInfoAsync(ipAddress);
        return Ok(result);
    }

    /// <summary>
    /// Get list of all supported currencies with metadata and current rates
    /// </summary>
    [HttpGet("supported")]
    [Authorize(Policy = "AllStaff")]
    public async Task<ActionResult<SupportedCurrenciesResponse>> GetSupportedCurrencies()
    {
        var result = await _currencyService.GetSupportedCurrenciesAsync();
        return Ok(result);
    }

    /// <summary>
    /// Manually trigger a refresh of exchange rates from Frankfurter API
    /// </summary>
    [HttpPost("rates/refresh")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<ActionResult> RefreshRates()
    {
        try
        {
            var count = await _currencyService.RefreshRatesAsync();
            return Ok(new { message = $"Successfully refreshed {count} exchange rates", count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing exchange rates");
            return BadRequest(new { message = ex.Message });
        }
    }
}
