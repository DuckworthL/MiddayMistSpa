namespace MiddayMistSpa.Core.Entities.Appointment;

/// <summary>
/// Represents a service line item within an appointment.
/// Supports multiple services per appointment (e.g., client adds more mid-session).
/// </summary>
public class AppointmentServiceItem
{
    public int AppointmentServiceItemId { get; set; }
    public int AppointmentId { get; set; }
    public int ServiceId { get; set; }

    /// <summary>Price at the time of booking (snapshot from Service.RegularPrice)</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Duration in minutes (snapshot from Service.DurationMinutes)</summary>
    public int DurationMinutes { get; set; }

    public int Quantity { get; set; } = 1;

    /// <summary>When this service line was added (UTC)</summary>
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual Appointment Appointment { get; set; } = null!;
    public virtual Service.Service Service { get; set; } = null!;
}
