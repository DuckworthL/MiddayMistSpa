namespace MiddayMistSpa.API.Services;

/// <summary>
/// Service for managing Google reCAPTCHA v2 verification and settings
/// </summary>
public interface ICaptchaService
{
    /// <summary>
    /// Check if captcha is enabled in system settings
    /// </summary>
    Task<bool> IsCaptchaEnabledAsync();

    /// <summary>
    /// Get captcha settings (enabled status + site key for frontend)
    /// </summary>
    Task<CaptchaSettingsDto> GetSettingsAsync();

    /// <summary>
    /// Verify a reCAPTCHA token with Google's API
    /// </summary>
    Task<bool> VerifyTokenAsync(string? captchaToken);

    /// <summary>
    /// Update captcha settings (enable/disable, change keys)
    /// </summary>
    Task<bool> UpdateSettingsAsync(UpdateCaptchaSettingsDto settings);
}

public class CaptchaSettingsDto
{
    public bool Enabled { get; set; }
    public string SiteKey { get; set; } = string.Empty;
}

public class UpdateCaptchaSettingsDto
{
    public bool Enabled { get; set; }
    public string SiteKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}
