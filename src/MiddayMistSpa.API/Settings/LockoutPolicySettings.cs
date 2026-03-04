namespace MiddayMistSpa.API.Settings;

public class LockoutPolicySettings
{
    public const string SectionName = "LockoutPolicy";

    /// <summary>
    /// Number of failed attempts before temporary lockout
    /// </summary>
    public int MaxFailedAttempts { get; set; } = 3;

    /// <summary>
    /// Duration of temporary lockout in minutes
    /// </summary>
    public int TempLockoutMinutes { get; set; } = 5;

    /// <summary>
    /// Total failed attempts within window before permanent lockout
    /// </summary>
    public int MaxTotalFailedAttempts { get; set; } = 5;

    /// <summary>
    /// Window in minutes for counting total failed attempts
    /// </summary>
    public int LockoutWindowMinutes { get; set; } = 15;
}
