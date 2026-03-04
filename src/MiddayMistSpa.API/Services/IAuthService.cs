using MiddayMistSpa.API.DTOs.Auth;

namespace MiddayMistSpa.API.Services;

public interface IAuthService
{
    /// <summary>
    /// Authenticate user and generate JWT token
    /// </summary>
    Task<LoginResponse> LoginAsync(LoginRequest request);

    /// <summary>
    /// Complete login after successful 2FA validation
    /// </summary>
    Task<LoginResponse> CompleteTwoFactorLoginAsync(int userId);

    /// <summary>
    /// Logout user and invalidate session
    /// </summary>
    Task<bool> LogoutAsync(int userId, LogoutRequest? request = null);

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request);

    /// <summary>
    /// Change user password with policy validation
    /// </summary>
    Task<ChangePasswordResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request);

    /// <summary>
    /// Validate password against policy rules
    /// </summary>
    Task<(bool IsValid, List<string> Errors)> ValidatePasswordPolicyAsync(string password, int userId);

    /// <summary>
    /// Check if password exists in user's password history
    /// </summary>
    Task<bool> IsPasswordInHistoryAsync(int userId, string password);

    /// <summary>
    /// Check if user's password is expired
    /// </summary>
    Task<bool> IsPasswordExpiredAsync(int userId);

    /// <summary>
    /// Get active sessions for a user
    /// </summary>
    Task<IEnumerable<UserSessionDto>> GetActiveSessionsAsync(int userId);

    /// <summary>
    /// Terminate a specific session
    /// </summary>
    Task<bool> TerminateSessionAsync(int sessionId, int requestingUserId);
}

public class UserSessionDto
{
    public int SessionId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; }
    public DateTime LastActivity { get; set; }
    public bool IsCurrentSession { get; set; }
}
