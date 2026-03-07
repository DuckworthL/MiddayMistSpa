using System.ComponentModel.DataAnnotations;

namespace MiddayMistSpa.API.DTOs.Auth;

public class UpdateProfileRequest
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
}

public class UpdateProfileResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}
