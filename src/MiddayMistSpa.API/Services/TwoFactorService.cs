using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MiddayMistSpa.API.DTOs.Auth;
using MiddayMistSpa.API.Settings;
using MiddayMistSpa.Core.Interfaces;
using OtpNet;
using QRCoder;

namespace MiddayMistSpa.API.Services;

public class TwoFactorService : ITwoFactorService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly TwoFactorSettings _settings;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TwoFactorService> _logger;

    private const string TwoFactorTokenPrefix = "2fa_token_";

    public TwoFactorService(
        IUnitOfWork unitOfWork,
        IOptions<TwoFactorSettings> settings,
        IMemoryCache cache,
        ILogger<TwoFactorService> logger)
    {
        _unitOfWork = unitOfWork;
        _settings = settings.Value;
        _cache = cache;
        _logger = logger;
    }

    public async Task<TwoFactorSetupResponse> SetupAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return new TwoFactorSetupResponse { Success = false, Message = "User not found" };
        }

        if (user.TwoFactorEnabled)
        {
            return new TwoFactorSetupResponse { Success = false, Message = "Two-factor authentication is already enabled" };
        }

        // Generate a new secret key
        var secretKey = KeyGeneration.GenerateRandomKey(20);
        var base32Secret = Base32Encoding.ToString(secretKey);

        // Store the secret temporarily (not confirmed yet)
        user.TotpSecretKey = base32Secret;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        // Build the otpauth URI
        var issuer = Uri.EscapeDataString(_settings.Issuer);
        var account = Uri.EscapeDataString(user.Email ?? user.Username);
        var otpauthUri = $"otpauth://totp/{issuer}:{account}?secret={base32Secret}&issuer={issuer}&digits={_settings.Digits}&period={_settings.Period}";

        // Generate QR code as base64 PNG
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(otpauthUri, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(5);
        var qrCodeBase64 = $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";

        _logger.LogInformation("2FA setup initiated for user {UserId}", userId);

        return new TwoFactorSetupResponse
        {
            Success = true,
            Message = "Scan the QR code with your authenticator app",
            QrCodeBase64 = qrCodeBase64,
            ManualEntryKey = FormatBase32Key(base32Secret)
        };
    }

    public async Task<TwoFactorVerifySetupResponse> VerifySetupAsync(int userId, string code)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return new TwoFactorVerifySetupResponse { Success = false, Message = "User not found" };
        }

        if (string.IsNullOrEmpty(user.TotpSecretKey))
        {
            return new TwoFactorVerifySetupResponse { Success = false, Message = "Please initiate 2FA setup first" };
        }

        if (user.TwoFactorEnabled)
        {
            return new TwoFactorVerifySetupResponse { Success = false, Message = "Two-factor authentication is already enabled" };
        }

        // Validate the code
        if (!VerifyTotp(user.TotpSecretKey, code))
        {
            return new TwoFactorVerifySetupResponse { Success = false, Message = "Invalid verification code. Please try again." };
        }

        // Generate recovery codes
        var recoveryCodes = GenerateRecoveryCodes(8);
        var hashedCodes = recoveryCodes.Select(c => HashRecoveryCode(c)).ToList();

        // Enable 2FA
        user.TwoFactorEnabled = true;
        user.TwoFactorConfirmedAt = DateTime.UtcNow;
        user.RecoveryCodes = JsonSerializer.Serialize(hashedCodes);
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("2FA enabled for user {UserId}", userId);

        return new TwoFactorVerifySetupResponse
        {
            Success = true,
            Message = "Two-factor authentication has been enabled successfully",
            RecoveryCodes = recoveryCodes
        };
    }

    public async Task<bool> ValidateCodeAsync(int userId, string code)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || !user.TwoFactorEnabled || string.IsNullOrEmpty(user.TotpSecretKey))
        {
            return false;
        }

        return VerifyTotp(user.TotpSecretKey, code);
    }

    public async Task<bool> ValidateRecoveryCodeAsync(int userId, string recoveryCode)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || !user.TwoFactorEnabled || string.IsNullOrEmpty(user.RecoveryCodes))
        {
            return false;
        }

        var hashedCodes = JsonSerializer.Deserialize<List<string>>(user.RecoveryCodes) ?? new();
        var hashedInput = HashRecoveryCode(recoveryCode.Trim().ToUpperInvariant());

        var matchIndex = hashedCodes.FindIndex(c => c == hashedInput);
        if (matchIndex < 0)
        {
            return false;
        }

        // Remove used recovery code
        hashedCodes.RemoveAt(matchIndex);
        user.RecoveryCodes = JsonSerializer.Serialize(hashedCodes);
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogWarning("Recovery code used for user {UserId}. {Remaining} codes remaining.", userId, hashedCodes.Count);

        return true;
    }

    public async Task<(bool Success, string Message)> DisableAsync(int userId, string code)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return (false, "User not found");
        }

        if (!user.TwoFactorEnabled)
        {
            return (false, "Two-factor authentication is not enabled");
        }

        // Verify the TOTP code before disabling
        if (!VerifyTotp(user.TotpSecretKey!, code))
        {
            return (false, "Invalid verification code");
        }

        user.TwoFactorEnabled = false;
        user.TotpSecretKey = null;
        user.TwoFactorConfirmedAt = null;
        user.RecoveryCodes = null;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("2FA disabled for user {UserId}", userId);

        return (true, "Two-factor authentication has been disabled");
    }

    public async Task<TwoFactorStatusResponse> GetStatusAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return new TwoFactorStatusResponse { IsEnabled = false };
        }

        return new TwoFactorStatusResponse
        {
            IsEnabled = user.TwoFactorEnabled,
            EnabledAt = user.TwoFactorConfirmedAt
        };
    }

    public string CreateTwoFactorToken(int userId)
    {
        var token = Guid.NewGuid().ToString("N");
        var cacheKey = TwoFactorTokenPrefix + token;

        _cache.Set(cacheKey, userId, TimeSpan.FromMinutes(_settings.TokenExpirationMinutes));

        return token;
    }

    public int? ValidateTwoFactorToken(string token)
    {
        var cacheKey = TwoFactorTokenPrefix + token;

        if (_cache.TryGetValue(cacheKey, out int userId))
        {
            // Remove the token after use (one-time use)
            _cache.Remove(cacheKey);
            return userId;
        }

        return null;
    }

    #region Private Methods

    private bool VerifyTotp(string base32Secret, string code)
    {
        try
        {
            var secretBytes = Base32Encoding.ToBytes(base32Secret);
            var totp = new Totp(secretBytes, step: _settings.Period, totpSize: _settings.Digits);

            // Allow a window of ±1 step for clock drift
            return totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying TOTP code");
            return false;
        }
    }

    private static List<string> GenerateRecoveryCodes(int count)
    {
        var codes = new List<string>();
        for (int i = 0; i < count; i++)
        {
            // Generate format: XXXX-XXXX (8 characters with hyphen)
            var bytes = RandomNumberGenerator.GetBytes(5);
            var code = Convert.ToHexString(bytes).ToUpperInvariant();
            codes.Add($"{code[..4]}-{code[4..8]}");
        }
        return codes;
    }

    private static string HashRecoveryCode(string code)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(code.Trim().ToUpperInvariant()));
        return Convert.ToBase64String(bytes);
    }

    private static string FormatBase32Key(string key)
    {
        // Format in groups of 4 for readability: XXXX XXXX XXXX ...
        var formatted = string.Join(" ", Enumerable.Range(0, (key.Length + 3) / 4)
            .Select(i => key.Substring(i * 4, Math.Min(4, key.Length - i * 4))));
        return formatted;
    }

    #endregion
}
