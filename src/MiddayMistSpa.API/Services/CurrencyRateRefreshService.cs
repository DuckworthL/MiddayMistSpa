using Microsoft.Extensions.Options;
using MiddayMistSpa.API.Settings;

namespace MiddayMistSpa.API.Services;

/// <summary>
/// Background hosted service that periodically refreshes exchange rates
/// from the Frankfurter API based on the configured interval.
/// Runs an initial refresh on startup, then repeats every N minutes.
/// </summary>
public class CurrencyRateRefreshService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CurrencyRateRefreshService> _logger;
    private readonly CurrencySettings _settings;

    public CurrencyRateRefreshService(
        IServiceProvider serviceProvider,
        ILogger<CurrencyRateRefreshService> logger,
        IOptions<CurrencySettings> settings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Currency rate refresh service started. Interval: {Minutes} minutes",
            _settings.RefreshIntervalMinutes);

        // Initial delay to let the app fully start up
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshRatesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scheduled currency rate refresh");
            }

            // Wait for the configured interval before next refresh
            await Task.Delay(TimeSpan.FromMinutes(_settings.RefreshIntervalMinutes), stoppingToken);
        }

        _logger.LogInformation("Currency rate refresh service stopped");
    }

    private async Task RefreshRatesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var currencyService = scope.ServiceProvider.GetRequiredService<ICurrencyService>();

        _logger.LogInformation("Scheduled currency rate refresh starting...");
        var count = await currencyService.RefreshRatesAsync();
        _logger.LogInformation("Scheduled currency rate refresh completed: {Count} rates updated", count);
    }
}
