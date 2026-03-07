using System.ComponentModel.DataAnnotations;

namespace MiddayMistSpa.API.DTOs.Auth;

public class ForgotPasswordRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class PasswordResetResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
