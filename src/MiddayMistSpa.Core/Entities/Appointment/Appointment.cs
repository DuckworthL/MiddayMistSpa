using MiddayMistSpa.Core.Entities.Identity;

namespace MiddayMistSpa.Core.Entities.Appointment;

/// <summary>
/// Appointment/booking with status workflow — receptionist-driven internal booking
/// </summary>
public class Appointment
{
    public int AppointmentId { get; set; }
    public string AppointmentNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public int ServiceId { get; set; }
    public int? TherapistId { get; set; }
    public int? RoomId { get; set; }

    // Scheduling
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    // Status Tracking
    // Scheduled → Confirmed → Checked-In → In Progress → Completed
    //                      ↘ Cancelled
    //                      ↘ No-Show
    public string Status { get; set; } = "Scheduled";

    // Booking Source
    public string BookingSource { get; set; } = "Direct"; // Direct, Walk-In, Phone, Email

    // Service Notes
    public string? CustomerNotes { get; set; }
    public string? TherapistNotes { get; set; }

    // Timestamps for each status change
    public DateTime? CheckedInAt { get; set; }
    public DateTime? ServiceStartedAt { get; set; }
    public DateTime? ServiceCompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    // Archive flag — hides the appointment from default list views
    public bool IsArchived { get; set; } = false;
    public DateTime? ArchivedAt { get; set; }

    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Computed properties
    public bool IsCompleted => Status == "Completed";
    public bool IsCancelled => Status == "Cancelled";
    public bool IsNoShow => Status == "No-Show";

    // Navigation properties
    public virtual Customer.Customer Customer { get; set; } = null!;
    public virtual Service.Service Service { get; set; } = null!;
    public virtual Employee.Employee? Therapist { get; set; }
    public virtual Room? Room { get; set; }
    public virtual User? CreatedByUser { get; set; }

    /// <summary>Additional services added to this appointment (multi-service support)</summary>
    public virtual ICollection<AppointmentServiceItem> ServiceItems { get; set; } = new List<AppointmentServiceItem>();
}
