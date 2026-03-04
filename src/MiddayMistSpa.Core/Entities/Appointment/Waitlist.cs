namespace MiddayMistSpa.Core.Entities.Appointment;

/// <summary>
/// Waitlist entry for customers waiting for appointment slots
/// </summary>
public class Waitlist
{
    public int WaitlistId { get; set; }
    public int CustomerId { get; set; }
    public int ServiceId { get; set; }
    public int? PreferredTherapistId { get; set; }

    // Preferred timing
    public DateTime? PreferredDate { get; set; }
    public TimeSpan? PreferredTimeFrom { get; set; }
    public TimeSpan? PreferredTimeTo { get; set; }

    // Status: Waiting, Notified, Booked, Expired, Cancelled
    public string Status { get; set; } = "Waiting";

    public string? Notes { get; set; }
    public DateTime? NotifiedAt { get; set; }
    public int? ConvertedAppointmentId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Customer.Customer Customer { get; set; } = null!;
    public virtual Service.Service Service { get; set; } = null!;
    public virtual Employee.Employee? PreferredTherapist { get; set; }
    public virtual Appointment? ConvertedAppointment { get; set; }
}
