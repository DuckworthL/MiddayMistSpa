namespace MiddayMistSpa.API.DTOs.Auth;

public class ChangePasswordResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string>? ValidationErrors { get; set; }
}
