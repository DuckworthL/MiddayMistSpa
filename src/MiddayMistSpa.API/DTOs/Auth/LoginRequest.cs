using System.ComponentModel.DataAnnotations;

namespace MiddayMistSpa.API.DTOs.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Username must be between 2 and 50 characters")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Client IP address for session tracking
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent for session tracking
    /// </summary>
    public string? UserAgent { get; set; }
}
