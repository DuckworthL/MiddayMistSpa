using System.Text.Json;
using MiddayMistSpa.Core.Interfaces;

namespace MiddayMistSpa.API.Services;

/// <summary>
/// Google reCAPTCHA v2 verification service.
/// Reads configuration from SystemSettings (Captcha.Enabled, Captcha.SiteKey, Captcha.SecretKey).
/// </summary>
public class CaptchaService : ICaptchaService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CaptchaService> _logger;

    private const string GoogleVerifyUrl = "https://www.google.com/recaptcha/api/siteverify";

    public CaptchaService(IUnitOfWork unitOfWork, IHttpClientFactory httpClientFactory, ILogger<CaptchaService> logger)
    {
        _unitOfWork = unitOfWork;
        _httpClient = httpClientFactory.CreateClient("Captcha");
        _logger = logger;
    }

    public async Task<bool> IsCaptchaEnabledAsync()
    {
        var setting = (await _unitOfWork.SystemSettings.FindAsync(s => s.SettingKey == "Captcha.Enabled")).FirstOrDefault();
        return setting?.SettingValue?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
    }

    public async Task<CaptchaSettingsDto> GetSettingsAsync()
    {
        var settings = await _unitOfWork.SystemSettings.FindAsync(s => s.Category == "Captcha");
        var settingsList = settings.ToList();

        return new CaptchaSettingsDto
        {
            Enabled = settingsList.FirstOrDefault(s => s.SettingKey == "Captcha.Enabled")?.SettingValue?.Equals("true", StringComparison.OrdinalIgnoreCase) == true,
            SiteKey = settingsList.FirstOrDefault(s => s.SettingKey == "Captcha.SiteKey")?.SettingValue ?? string.Empty
        };
    }

    public async Task<bool> VerifyTokenAsync(string? captchaToken)
    {
        // If captcha is not enabled, always pass
        if (!await IsCaptchaEnabledAsync())
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(captchaToken))
        {
            _logger.LogWarning("Captcha token is empty but captcha is enabled");
            return false;
        }

        try
        {
            var secretSetting = (await _unitOfWork.SystemSettings.FindAsync(s => s.SettingKey == "Captcha.SecretKey")).FirstOrDefault();
            var secretKey = secretSetting?.SettingValue;

            if (string.IsNullOrEmpty(secretKey))
            {
                _logger.LogError("Captcha secret key not configured");
                return false;
            }

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", secretKey),
                new KeyValuePair<string, string>("response", captchaToken)
            });

            var response = await _httpClient.PostAsync(GoogleVerifyUrl, formContent);
            var json = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<GoogleCaptchaResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Success != true)
            {
                _logger.LogWarning("Captcha verification failed. Errors: {Errors}", string.Join(", ", result?.ErrorCodes ?? []));
            }

            return result?.Success == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying captcha token");
            // On error, fail open to avoid blocking all logins if Google is unreachable
            return true;
        }
    }

    public async Task<bool> UpdateSettingsAsync(UpdateCaptchaSettingsDto settings)
    {
        try
        {
            var captchaSettings = (await _unitOfWork.SystemSettings.FindAsync(s => s.Category == "Captcha")).ToList();

            var enabledSetting = captchaSettings.FirstOrDefault(s => s.SettingKey == "Captcha.Enabled");
            var siteKeySetting = captchaSettings.FirstOrDefault(s => s.SettingKey == "Captcha.SiteKey");
            var secretKeySetting = captchaSettings.FirstOrDefault(s => s.SettingKey == "Captcha.SecretKey");

            if (enabledSetting != null)
            {
                enabledSetting.SettingValue = settings.Enabled.ToString().ToLower();
                enabledSetting.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.SystemSettings.Update(enabledSetting);
            }

            if (siteKeySetting != null)
            {
                siteKeySetting.SettingValue = settings.SiteKey;
                siteKeySetting.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.SystemSettings.Update(siteKeySetting);
            }

            if (secretKeySetting != null && !string.IsNullOrWhiteSpace(settings.SecretKey))
            {
                secretKeySetting.SettingValue = settings.SecretKey;
                secretKeySetting.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.SystemSettings.Update(secretKeySetting);
            }

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Captcha settings updated. Enabled: {Enabled}", settings.Enabled);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating captcha settings");
            return false;
        }
    }

    /// <summary>
    /// Google reCAPTCHA siteverify response
    /// </summary>
    private class GoogleCaptchaResponse
    {
        public bool Success { get; set; }
        public DateTime ChallengeTs { get; set; }
        public string? Hostname { get; set; }
        public List<string>? ErrorCodes { get; set; }
    }
}
