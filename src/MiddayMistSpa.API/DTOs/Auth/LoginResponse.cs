namespace MiddayMistSpa.API.DTOs.Auth;

public class LoginResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public TokenResponse? Token { get; set; }
    public UserInfo? User { get; set; }
    public bool RequiresPasswordChange { get; set; }
    public bool RequiresTwoFactor { get; set; }
    public string? TwoFactorToken { get; set; }
    public int? RemainingAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }
}

public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string TokenType { get; set; } = "Bearer";
}

public class UserInfo
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string RoleName { get; set; } = string.Empty;
    public int? EmployeeId { get; set; }
}
