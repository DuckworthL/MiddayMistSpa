namespace MiddayMistSpa.Core;

/// <summary>
/// Utility for Philippine Standard Time (UTC+8) conversions.
/// Use for all display dates, scheduling logic, and business-hour checks.
/// Database timestamps should remain in UTC.
/// </summary>
public static class PhilippineTime
{
    private static readonly TimeZoneInfo PhtZone =
        TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows()
            ? "Singapore Standard Time"   // Windows TZ id for UTC+8
            : "Asia/Manila");              // IANA id for Linux/macOS

    /// <summary>Current Philippine date+time.</summary>
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PhtZone);

    /// <summary>Current Philippine date (midnight).</summary>
    public static DateTime Today => Now.Date;

    /// <summary>Convert a UTC DateTime to Philippine time.</summary>
    public static DateTime FromUtc(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), PhtZone);

    /// <summary>Convert a Philippine local DateTime to UTC.</summary>
    public static DateTime ToUtc(DateTime pht) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(pht, DateTimeKind.Unspecified), PhtZone);
}
