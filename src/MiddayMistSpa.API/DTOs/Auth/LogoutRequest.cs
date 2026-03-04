using System.ComponentModel.DataAnnotations;

namespace MiddayMistSpa.API.DTOs.Auth;

public class LogoutRequest
{
    /// <summary>
    /// Optional: Specific session ID to logout. If not provided, logs out current session.
    /// </summary>
    public int? SessionId { get; set; }

    /// <summary>
    /// If true, logs out all sessions for the user
    /// </summary>
    public bool LogoutAllSessions { get; set; } = false;
}
