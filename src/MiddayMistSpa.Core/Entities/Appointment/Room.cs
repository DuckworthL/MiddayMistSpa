namespace MiddayMistSpa.Core.Entities.Appointment;

/// <summary>
/// Treatment room or service area in the spa
/// </summary>
public class Room
{
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string RoomCode { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// e.g., Treatment, Massage, Facial, Nail, Multi-Purpose
    /// </summary>
    public string RoomType { get; set; } = "Treatment";

    /// <summary>
    /// Maximum number of clients the room can accommodate simultaneously
    /// </summary>
    public int Capacity { get; set; } = 1;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
