namespace MiddayMistSpa.API.DTOs.Auth;

public class TwoFactorSetupResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? QrCodeBase64 { get; set; }
    public string? ManualEntryKey { get; set; }
}

public class TwoFactorVerifySetupRequest
{
    public string Code { get; set; } = string.Empty;
}

public class TwoFactorVerifySetupResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string> RecoveryCodes { get; set; } = new();
}

public class TwoFactorValidateRequest
{
    public string TwoFactorToken { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? RecoveryCode { get; set; }
}

public class TwoFactorDisableRequest
{
    public string Code { get; set; } = string.Empty;
}

public class TwoFactorStatusResponse
{
    public bool IsEnabled { get; set; }
    public DateTime? EnabledAt { get; set; }
}
