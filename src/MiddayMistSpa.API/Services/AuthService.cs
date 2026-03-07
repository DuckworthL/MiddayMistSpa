using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MiddayMistSpa.API.DTOs.Auth;
using MiddayMistSpa.API.Settings;
using MiddayMistSpa.Core.Entities.Identity;
using MiddayMistSpa.Core.Interfaces;

namespace MiddayMistSpa.API.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtSettings _jwtSettings;
    private readonly PasswordPolicySettings _passwordPolicy;
    private readonly LockoutPolicySettings _lockoutPolicy;
    private readonly SessionPolicySettings _sessionPolicy;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITwoFactorService _twoFactorService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUnitOfWork unitOfWork,
        IOptions<JwtSettings> jwtSettings,
        IOptions<PasswordPolicySettings> passwordPolicy,
        IOptions<LockoutPolicySettings> lockoutPolicy,
        IOptions<SessionPolicySettings> sessionPolicy,
        IHttpContextAccessor httpContextAccessor,
        ITwoFactorService twoFactorService,
        ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _jwtSettings = jwtSettings.Value;
        _passwordPolicy = passwordPolicy.Value;
        _lockoutPolicy = lockoutPolicy.Value;
        _sessionPolicy = sessionPolicy.Value;
        _httpContextAccessor = httpContextAccessor;
        _twoFactorService = twoFactorService;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        // Fetch user with Role included for proper authorization
        var users = await _unitOfWork.Users.FindWithIncludesAsync(
            u => u.Username == request.Username,
            u => u.Role!);
        var user = users.FirstOrDefault();

        if (user == null)
        {
            _logger.LogWarning("Login attempt for non-existent user: {Username}", request.Username);
            return new LoginResponse
            {
                Success = false,
                Message = "Invalid username or password"
            };
        }

        // Check if account is locked out (permanent or temporary)
        if (user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
        {
            _logger.LogWarning("Login attempt for locked account: {Username}", request.Username);
            return new LoginResponse
            {
                Success = false,
                Message = "Account is locked. Please try again later or contact administrator.",
                LockoutEnd = user.LockoutEnd
            };
        }

        // Check if user is active
        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for inactive account: {Username}", request.Username);
            return new LoginResponse
            {
                Success = false,
                Message = "Account is inactive. Please contact administrator."
            };
        }

        // Verify password
        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            // Increment failed attempts
            user.AccessFailedCount++;

            // Check for temporary lockout
            if (user.AccessFailedCount >= _lockoutPolicy.MaxFailedAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(_lockoutPolicy.TempLockoutMinutes);
                _logger.LogWarning("User {Username} temporarily locked after {Attempts} failed attempts",
                    request.Username, user.AccessFailedCount);
            }

            // Check for permanent lockout
            if (user.AccessFailedCount >= _lockoutPolicy.MaxTotalFailedAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.AddYears(100); // Effectively permanent
                _logger.LogWarning("User {Username} permanently locked after {Attempts} failed attempts",
                    request.Username, user.AccessFailedCount);
            }

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            var remainingAttempts = _lockoutPolicy.MaxTotalFailedAttempts - user.AccessFailedCount;
            return new LoginResponse
            {
                Success = false,
                Message = "Invalid username or password",
                RemainingAttempts = remainingAttempts > 0 ? remainingAttempts : 0
            };
        }

        // Check concurrent sessions
        var activeSessions = await GetActiveSessionsCountAsync(user.UserId);
        if (activeSessions >= _sessionPolicy.MaxConcurrentSessions)
        {
            // Terminate oldest session
            await TerminateOldestSessionAsync(user.UserId);
        }

        // Reset failed attempts on successful login
        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        // Check if 2FA is enabled — if so, return a temporary token
        if (user.TwoFactorEnabled)
        {
            var twoFactorToken = _twoFactorService.CreateTwoFactorToken(user.UserId);
            _logger.LogInformation("2FA required for user {Username}", request.Username);
            return new LoginResponse
            {
                Success = false,
                RequiresTwoFactor = true,
                TwoFactorToken = twoFactorToken,
                Message = "Two-factor authentication required"
            };
        }

        // Check if password is expired
        var passwordExpired = await IsPasswordExpiredAsync(user.UserId);

        // Generate tokens
        var (accessToken, refreshToken, expiresAt) = GenerateTokens(user);

        // Create session
        var session = new UserSession
        {
            UserId = user.UserId,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            IpAddress = request.IpAddress ?? GetClientIpAddress(),
            UserAgent = request.UserAgent ?? GetUserAgent(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.UserSessions.AddAsync(session);
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        // Log audit
        await LogAuditAsync(user.UserId, "Login", "Users", user.UserId.ToString(), request.IpAddress);

        _logger.LogInformation("User {Username} logged in successfully", request.Username);

        // Look up linked employee record
        var employee = (await _unitOfWork.Employees.FindAsync(e => e.UserId == user.UserId)).FirstOrDefault();

        return new LoginResponse
        {
            Success = true,
            Message = "Login successful",
            Token = new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt
            },
            User = new UserInfo
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                RoleName = user.Role?.RoleName ?? "Unknown",
                EmployeeId = employee?.EmployeeId
            },
            RequiresPasswordChange = passwordExpired || user.MustChangePassword
        };
    }

    public async Task<LoginResponse> CompleteTwoFactorLoginAsync(int userId)
    {
        var users = await _unitOfWork.Users.FindWithIncludesAsync(
            u => u.UserId == userId,
            u => u.Role!);
        var user = users.FirstOrDefault();

        if (user == null)
        {
            return new LoginResponse { Success = false, Message = "User not found" };
        }

        // Check if password is expired
        var passwordExpired = await IsPasswordExpiredAsync(user.UserId);

        // Generate tokens
        var (accessToken, refreshToken, expiresAt) = GenerateTokens(user);

        // Create session
        var session = new UserSession
        {
            UserId = user.UserId,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            IpAddress = GetClientIpAddress(),
            UserAgent = GetUserAgent(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.UserSessions.AddAsync(session);
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        // Log audit
        await LogAuditAsync(user.UserId, "Login (2FA)", "Users", user.UserId.ToString(), GetClientIpAddress());

        _logger.LogInformation("User {Username} logged in successfully via 2FA", user.Username);

        // Look up linked employee record
        var employee = (await _unitOfWork.Employees.FindAsync(e => e.UserId == user.UserId)).FirstOrDefault();

        return new LoginResponse
        {
            Success = true,
            Message = "Login successful",
            Token = new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt
            },
            User = new UserInfo
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                RoleName = user.Role?.RoleName ?? "Unknown",
                EmployeeId = employee?.EmployeeId
            },
            RequiresPasswordChange = passwordExpired || user.MustChangePassword
        };
    }

    public async Task<bool> LogoutAsync(int userId, LogoutRequest? request = null)
    {
        if (request?.LogoutAllSessions == true)
        {
            // Logout all sessions
            var sessions = await _unitOfWork.UserSessions.FindAsync(s => s.UserId == userId && s.IsActive);
            foreach (var session in sessions)
            {
                session.IsActive = false;
                _unitOfWork.UserSessions.Update(session);
            }
        }
        else if (request?.SessionId.HasValue == true)
        {
            // Logout specific session
            var session = await _unitOfWork.UserSessions.GetByIdAsync(request.SessionId.Value);
            if (session != null && session.UserId == userId)
            {
                session.IsActive = false;
                _unitOfWork.UserSessions.Update(session);
            }
        }
        else
        {
            // Logout current session (based on current refresh token)
            var currentToken = GetCurrentRefreshToken();
            if (!string.IsNullOrEmpty(currentToken))
            {
                var session = await _unitOfWork.UserSessions.FirstOrDefaultAsync(
                    s => s.Token == currentToken && s.UserId == userId);
                if (session != null)
                {
                    session.IsActive = false;
                    _unitOfWork.UserSessions.Update(session);
                }
            }
        }

        await _unitOfWork.SaveChangesAsync();
        await LogAuditAsync(userId, "Logout", "Users", userId.ToString());

        return true;
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var session = await _unitOfWork.UserSessions.FirstOrDefaultAsync(
            s => s.Token == request.RefreshToken && s.IsActive);

        if (session == null)
        {
            return new LoginResponse
            {
                Success = false,
                Message = "Invalid refresh token"
            };
        }

        if (session.ExpiresAt < DateTime.UtcNow)
        {
            session.IsActive = false;
            _unitOfWork.UserSessions.Update(session);
            await _unitOfWork.SaveChangesAsync();

            return new LoginResponse
            {
                Success = false,
                Message = "Refresh token has expired"
            };
        }

        var user = await _unitOfWork.Users.GetByIdAsync(session.UserId);
        if (user == null || !user.IsActive)
        {
            return new LoginResponse
            {
                Success = false,
                Message = "User account is not available"
            };
        }

        // Check lockout
        if (user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
        {
            return new LoginResponse
            {
                Success = false,
                Message = "User account is locked"
            };
        }

        // Generate new tokens
        var (accessToken, refreshToken, expiresAt) = GenerateTokens(user);

        // Update session with new refresh token
        session.Token = refreshToken;
        session.ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        _unitOfWork.UserSessions.Update(session);
        await _unitOfWork.SaveChangesAsync();

        // Get role name
        var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId);

        return new LoginResponse
        {
            Success = true,
            Token = new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt
            },
            User = new UserInfo
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                RoleName = role?.RoleName ?? "Unknown",
                EmployeeId = (await _unitOfWork.Employees.FindAsync(e => e.UserId == user.UserId)).FirstOrDefault()?.EmployeeId
            }
        };
    }

    public async Task<ChangePasswordResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return new ChangePasswordResponse
            {
                Success = false,
                Message = "User not found"
            };
        }

        // Verify current password
        if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return new ChangePasswordResponse
            {
                Success = false,
                Message = "Current password is incorrect"
            };
        }

        // Validate new password policy
        var (isValid, errors) = await ValidatePasswordPolicyAsync(request.NewPassword, userId);
        if (!isValid)
        {
            return new ChangePasswordResponse
            {
                Success = false,
                Message = "Password does not meet policy requirements",
                ValidationErrors = errors
            };
        }

        // Check password history
        if (await IsPasswordInHistoryAsync(userId, request.NewPassword))
        {
            return new ChangePasswordResponse
            {
                Success = false,
                Message = $"Cannot reuse any of your last {_passwordPolicy.PasswordHistoryCount} passwords"
            };
        }

        // Store current password in history
        var passwordHistory = new PasswordHistory
        {
            UserId = userId,
            PasswordHash = user.PasswordHash,
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.PasswordHistories.AddAsync(passwordHistory);

        // Update password
        user.PasswordHash = HashPassword(request.NewPassword);
        user.PasswordExpiryDate = DateTime.UtcNow.AddDays(_passwordPolicy.PasswordExpirationDays);
        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await LogAuditAsync(userId, "PasswordChange", "Users", userId.ToString());

        return new ChangePasswordResponse
        {
            Success = true,
            Message = "Password changed successfully"
        };
    }

    public async Task<(bool IsValid, List<string> Errors)> ValidatePasswordPolicyAsync(string password, int userId)
    {
        var errors = new List<string>();

        if (password.Length < _passwordPolicy.MinimumLength)
        {
            errors.Add($"Password must be at least {_passwordPolicy.MinimumLength} characters");
        }

        if (_passwordPolicy.RequireUppercase && !Regex.IsMatch(password, "[A-Z]"))
        {
            errors.Add("Password must contain at least one uppercase letter");
        }

        if (_passwordPolicy.RequireLowercase && !Regex.IsMatch(password, "[a-z]"))
        {
            errors.Add("Password must contain at least one lowercase letter");
        }

        if (_passwordPolicy.RequireDigit && !Regex.IsMatch(password, "[0-9]"))
        {
            errors.Add("Password must contain at least one digit");
        }

        if (_passwordPolicy.RequireSpecialCharacter && !Regex.IsMatch(password, @"[!@#$%^&*(),.?""':{}|<>]"))
        {
            errors.Add("Password must contain at least one special character");
        }

        // Check if password contains username
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user != null && password.Contains(user.Username, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Password cannot contain your username");
        }

        return (errors.Count == 0, errors);
    }

    public async Task<bool> IsPasswordInHistoryAsync(int userId, string password)
    {
        var history = await _unitOfWork.PasswordHistories
            .FindAsync(h => h.UserId == userId);

        var recentHistory = history
            .OrderByDescending(h => h.CreatedAt)
            .Take(_passwordPolicy.PasswordHistoryCount);

        foreach (var record in recentHistory)
        {
            if (VerifyPassword(password, record.PasswordHash))
            {
                return true;
            }
        }

        return false;
    }

    public async Task<bool> IsPasswordExpiredAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return true;
        }

        return DateTime.UtcNow > user.PasswordExpiryDate;
    }

    public async Task<IEnumerable<UserSessionDto>> GetActiveSessionsAsync(int userId)
    {
        var sessions = await _unitOfWork.UserSessions.FindAsync(s => s.UserId == userId && s.IsActive);
        var currentToken = GetCurrentRefreshToken();

        return sessions.Select(s => new UserSessionDto
        {
            SessionId = s.SessionId,
            IpAddress = s.IpAddress ?? "Unknown",
            UserAgent = s.UserAgent ?? "Unknown",
            LoginTime = s.CreatedAt,
            LastActivity = s.CreatedAt, // UserSession doesn't have LastActivity, use CreatedAt
            IsCurrentSession = s.Token == currentToken
        });
    }

    public async Task<bool> TerminateSessionAsync(int sessionId, int requestingUserId)
    {
        var session = await _unitOfWork.UserSessions.GetByIdAsync(sessionId);

        if (session == null || session.UserId != requestingUserId)
        {
            return false;
        }

        session.IsActive = false;
        _unitOfWork.UserSessions.Update(session);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    #region Private Methods

    private (string AccessToken, string RefreshToken, DateTime ExpiresAt) GenerateTokens(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new(ClaimTypes.Role, user.Role?.RoleName ?? "User"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = GenerateRefreshToken();

        return (accessToken, refreshToken, expiresAt);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private static string HashPassword(string password)
    {
        var saltBytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            100000,
            HashAlgorithmName.SHA256,
            32);

        // Combine salt and hash for storage
        var combined = new byte[saltBytes.Length + hash.Length];
        Buffer.BlockCopy(saltBytes, 0, combined, 0, saltBytes.Length);
        Buffer.BlockCopy(hash, 0, combined, saltBytes.Length, hash.Length);

        return Convert.ToBase64String(combined);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var combined = Convert.FromBase64String(storedHash);
        var saltBytes = new byte[16];
        var storedHashBytes = new byte[32];

        Buffer.BlockCopy(combined, 0, saltBytes, 0, 16);
        Buffer.BlockCopy(combined, 16, storedHashBytes, 0, 32);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            100000,
            HashAlgorithmName.SHA256,
            32);

        return CryptographicOperations.FixedTimeEquals(hash, storedHashBytes);
    }

    private async Task<int> GetActiveSessionsCountAsync(int userId)
    {
        return await _unitOfWork.UserSessions.CountAsync(s => s.UserId == userId && s.IsActive);
    }

    private async Task TerminateOldestSessionAsync(int userId)
    {
        var sessions = await _unitOfWork.UserSessions.FindAsync(s => s.UserId == userId && s.IsActive);
        var oldest = sessions.OrderBy(s => s.CreatedAt).FirstOrDefault();

        if (oldest != null)
        {
            oldest.IsActive = false;
            _unitOfWork.UserSessions.Update(oldest);
        }
    }

    private string GetClientIpAddress()
    {
        return _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private string GetUserAgent()
    {
        return _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString() ?? "Unknown";
    }

    private string? GetCurrentRefreshToken()
    {
        return _httpContextAccessor.HttpContext?.Request?.Headers["X-Refresh-Token"].ToString();
    }

    private async Task LogAuditAsync(int userId, string action, string? tableName = null, string? recordId = null, string? ipAddress = null)
    {
        var audit = new AuditLog
        {
            UserId = userId,
            Action = action,
            TableName = tableName,
            RecordId = recordId,
            IpAddress = ipAddress ?? GetClientIpAddress(),
            UserAgent = GetUserAgent(),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.AuditLogs.AddAsync(audit);
        await _unitOfWork.SaveChangesAsync();
    }

    #endregion

    // =========================================================================
    // Password Reset
    // =========================================================================

    public async Task<DTOs.Auth.PasswordResetResponse> RequestPasswordResetAsync(string email)
    {
        // Always return success to avoid revealing whether the email exists
        var users = await _unitOfWork.Users.FindAsync(u => u.Email == email && u.IsActive);
        var user = users.FirstOrDefault();

        if (user != null)
        {
            // Generate a 6-digit reset code
            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            user.PasswordResetToken = code;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(30);
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Password reset requested for user {UserId}. Code generated.", user.UserId);
        }

        return new DTOs.Auth.PasswordResetResponse
        {
            Success = true,
            Message = "If the email address is registered, a reset code has been generated. Please contact your administrator to obtain the code."
        };
    }

    public async Task<DTOs.Auth.PasswordResetResponse> ResetPasswordAsync(DTOs.Auth.ResetPasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmPassword)
        {
            return new DTOs.Auth.PasswordResetResponse { Success = false, Message = "Passwords do not match" };
        }

        var users = await _unitOfWork.Users.FindAsync(u => u.Email == request.Email && u.IsActive);
        var user = users.FirstOrDefault();

        if (user == null || user.PasswordResetToken != request.Token
            || !user.PasswordResetTokenExpiry.HasValue
            || user.PasswordResetTokenExpiry.Value < DateTime.UtcNow)
        {
            return new DTOs.Auth.PasswordResetResponse { Success = false, Message = "Invalid or expired reset code" };
        }

        // Validate password policy
        var (isValid, errors) = await ValidatePasswordPolicyAsync(request.NewPassword, user.UserId);
        if (!isValid)
        {
            return new DTOs.Auth.PasswordResetResponse { Success = false, Message = string.Join("; ", errors) };
        }

        // Update password
        var oldHash = user.PasswordHash;
        user.PasswordHash = HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.MustChangePassword = false;
        user.PasswordExpiryDate = DateTime.UtcNow.AddDays(_passwordPolicy.PasswordExpirationDays);
        user.AccessFailedCount = 0;
        user.LockoutEnd = null;

        // Add old password to history
        var history = new PasswordHistory
        {
            UserId = user.UserId,
            PasswordHash = oldHash,
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.PasswordHistories.AddAsync(history);

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await LogAuditAsync(user.UserId, "PasswordReset", "Users", user.UserId.ToString());

        return new DTOs.Auth.PasswordResetResponse { Success = true, Message = "Password has been reset successfully" };
    }

    // =========================================================================
    // Profile Update
    // =========================================================================

    public async Task<DTOs.Auth.UpdateProfileResponse> UpdateProfileAsync(int userId, DTOs.Auth.UpdateProfileRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return new DTOs.Auth.UpdateProfileResponse { Success = false, Message = "User not found" };

        // Check for email uniqueness if changed
        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _unitOfWork.Users.FindAsync(u => u.Email == request.Email && u.UserId != userId);
            if (existing.Any())
                return new DTOs.Auth.UpdateProfileResponse { Success = false, Message = "Email is already in use" };
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Email = request.Email;
        user.PhoneNumber = request.PhoneNumber;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await LogAuditAsync(userId, "ProfileUpdate", "Users", userId.ToString());

        return new DTOs.Auth.UpdateProfileResponse { Success = true, Message = "Profile updated successfully" };
    }
}
