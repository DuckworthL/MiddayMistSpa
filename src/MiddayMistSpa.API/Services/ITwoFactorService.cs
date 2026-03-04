using MiddayMistSpa.API.DTOs.Auth;

namespace MiddayMistSpa.API.Services;

public interface ITwoFactorService
{
    /// <summary>
    /// Generate TOTP secret and QR code for setup
    /// </summary>
    Task<TwoFactorSetupResponse> SetupAsync(int userId);

    /// <summary>
    /// Verify the TOTP code during setup and enable 2FA
    /// </summary>
    Task<TwoFactorVerifySetupResponse> VerifySetupAsync(int userId, string code);

    /// <summary>
    /// Validate a TOTP code during login
    /// </summary>
    Task<bool> ValidateCodeAsync(int userId, string code);

    /// <summary>
    /// Validate a recovery code during login
    /// </summary>
    Task<bool> ValidateRecoveryCodeAsync(int userId, string recoveryCode);

    /// <summary>
    /// Disable 2FA for a user
    /// </summary>
    Task<(bool Success, string Message)> DisableAsync(int userId, string code);

    /// <summary>
    /// Get 2FA status for a user
    /// </summary>
    Task<TwoFactorStatusResponse> GetStatusAsync(int userId);

    /// <summary>
    /// Store a temporary 2FA token and return the token string
    /// </summary>
    string CreateTwoFactorToken(int userId);

    /// <summary>
    /// Validate and retrieve the userId from a temporary 2FA token
    /// </summary>
    int? ValidateTwoFactorToken(string token);
}
