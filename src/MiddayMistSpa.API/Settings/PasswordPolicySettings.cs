namespace MiddayMistSpa.API.Settings;

public class PasswordPolicySettings
{
    public const string SectionName = "PasswordPolicy";

    public int MinimumLength { get; set; } = 8;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecialCharacter { get; set; } = true;
    public int PasswordHistoryCount { get; set; } = 5;
    public int PasswordExpirationDays { get; set; } = 90;
}
