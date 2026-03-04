namespace MiddayMistSpa.API.Settings;

public class TwoFactorSettings
{
    public const string SectionName = "TwoFactorSettings";

    public string Issuer { get; set; } = "MiddayMistSpa";
    public int Digits { get; set; } = 6;
    public int Period { get; set; } = 30;
    public int TokenExpirationMinutes { get; set; } = 5;
}
