namespace MiddayMistSpa.API.Settings;

public class SessionPolicySettings
{
    public const string SectionName = "SessionPolicy";

    /// <summary>
    /// Maximum number of concurrent sessions per user
    /// </summary>
    public int MaxConcurrentSessions { get; set; } = 2;

    /// <summary>
    /// Session timeout in minutes (inactivity)
    /// </summary>
    public int SessionTimeoutMinutes { get; set; } = 30;
}
