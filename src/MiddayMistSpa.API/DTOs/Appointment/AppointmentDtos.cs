using System.ComponentModel.DataAnnotations;

namespace MiddayMistSpa.API.DTOs.Appointment;

// ============================================================================
// Appointment Request DTOs
// ============================================================================

public class CreateAppointmentRequest
{
    [Required]
    public int CustomerId { get; set; }

    [Required]
    public int ServiceId { get; set; }

    public int? TherapistId { get; set; }
    public int? RoomId { get; set; }

    [Required]
    public DateTime AppointmentDate { get; set; }

    [Required]
    public TimeSpan StartTime { get; set; }

    public string? CustomerNotes { get; set; }

    public string BookingSource { get; set; } = "Direct"; // Direct, Calendly, Walk-In
}

public class UpdateAppointmentRequest
{
    public int? CustomerId { get; set; }
    public int? ServiceId { get; set; }
    public int? TherapistId { get; set; }
    public int? RoomId { get; set; }
    public DateTime? AppointmentDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public string? CustomerNotes { get; set; }
}

public class RescheduleAppointmentRequest
{
    [Required]
    public DateTime NewDate { get; set; }

    [Required]
    public TimeSpan NewStartTime { get; set; }

    public int? NewTherapistId { get; set; }
    public string? Reason { get; set; }
}

public class CancelAppointmentRequest
{
    [Required, StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    public bool NotifyCustomer { get; set; } = true;
}

public class AssignTherapistRequest
{
    [Required]
    public int TherapistId { get; set; }
}

public class AddTherapistNotesRequest
{
    [Required, StringLength(2000)]
    public string Notes { get; set; } = string.Empty;
}

// ============================================================================
// Appointment Response DTOs
// ============================================================================

public class AppointmentResponse
{
    public int AppointmentId { get; set; }
    public string AppointmentNumber { get; set; } = string.Empty;

    // Customer Info
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public string MembershipType { get; set; } = "Regular";

    // Service Info
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceCode { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }

    // Therapist Info
    public int? TherapistId { get; set; }
    public string? TherapistName { get; set; }

    // Room Info
    public int? RoomId { get; set; }
    public string? RoomName { get; set; }

    // Scheduling
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    // Status
    public string Status { get; set; } = string.Empty;
    public string BookingSource { get; set; } = string.Empty;

    // Notes
    public string? CustomerNotes { get; set; }
    public string? TherapistNotes { get; set; }

    // Status Timestamps
    public DateTime? CheckedInAt { get; set; }
    public DateTime? ServiceStartedAt { get; set; }
    public DateTime? ServiceCompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Multi-service support
    public List<AppointmentServiceItemResponse> ServiceItems { get; set; } = new();
    public decimal TotalPrice => ServiceItems.Any() ? ServiceItems.Sum(s => s.UnitPrice * s.Quantity) : 0;
    public int TotalDurationMinutes => ServiceItems.Any() ? ServiceItems.Sum(s => s.DurationMinutes * s.Quantity) : DurationMinutes;

    // Computed
    public bool IsToday => AppointmentDate.Date == DateTime.Today;
    public bool IsUpcoming => AppointmentDate.Date >= DateTime.Today && Status != "Cancelled" && Status != "Completed";
    public bool CanBeRescheduled => Status is "Scheduled" or "Confirmed";
    public bool CanBeCancelled => Status is "Scheduled" or "Confirmed";
}

public class AppointmentServiceItemResponse
{
    public int AppointmentServiceItemId { get; set; }
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceCode { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int DurationMinutes { get; set; }
    public int Quantity { get; set; } = 1;
    public DateTime AddedAt { get; set; }
}

public class AppointmentListResponse
{
    public int AppointmentId { get; set; }
    public string AppointmentNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string? TherapistName { get; set; }
    public int? RoomId { get; set; }
    public string? RoomName { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string BookingSource { get; set; } = string.Empty;
    public decimal ServicePrice { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsArchived { get; set; }
    public List<AppointmentServiceItemResponse> ServiceItems { get; set; } = new();
    public decimal TotalPrice => ServiceItems.Any() ? ServiceItems.Sum(s => s.UnitPrice * s.Quantity) : ServicePrice;
    public int TotalDurationMinutes => ServiceItems.Any() ? ServiceItems.Sum(s => s.DurationMinutes * s.Quantity) : DurationMinutes;
}

// ============================================================================
// Calendar & Scheduling DTOs
// ============================================================================

public class DailyScheduleResponse
{
    public DateTime Date { get; set; }
    public List<AppointmentListResponse> Appointments { get; set; } = new();
    public int TotalAppointments { get; set; }
    public int CompletedCount { get; set; }
    public int PendingCount { get; set; }
    public int CancelledCount { get; set; }
}

public class TherapistScheduleResponse
{
    public int TherapistId { get; set; }
    public string TherapistName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public List<TimeSlotResponse> TimeSlots { get; set; } = new();
    public List<AppointmentListResponse> Appointments { get; set; } = new();
}

public class TimeSlotResponse
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsAvailable { get; set; }
    public int? AppointmentId { get; set; }
    public string? CustomerName { get; set; }
    public string? ServiceName { get; set; }
}

public class AvailabilityRequest
{
    [Required]
    public int ServiceId { get; set; }

    [Required]
    public DateTime Date { get; set; }

    public int? PreferredTherapistId { get; set; }
}

public class AvailabilityResponse
{
    public DateTime Date { get; set; }
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public List<AvailableSlotResponse> AvailableSlots { get; set; } = new();
}

public class AvailableSlotResponse
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public List<AvailableTherapistResponse> AvailableTherapists { get; set; } = new();
    public List<UnavailableTherapistResponse> UnavailableTherapists { get; set; } = new();
}

public class AvailableTherapistResponse
{
    public int TherapistId { get; set; }
    public string TherapistName { get; set; } = string.Empty;
}

public class UnavailableTherapistResponse
{
    public int TherapistId { get; set; }
    public string TherapistName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty; // "On Leave", "On Break", "Already Booked", "Outside Shift", "Not Scheduled"
}

// ============================================================================
// Search & Filter DTOs
// ============================================================================

public class AppointmentSearchRequest
{
    public string? SearchTerm { get; set; }
    public int? CustomerId { get; set; }
    public int? TherapistId { get; set; }
    public int? ServiceId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? Status { get; set; }
    public string? BookingSource { get; set; }
    public string? SortBy { get; set; } = "date";
    public bool SortDescending { get; set; } = false;
    public bool IncludeArchived { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// ============================================================================
// Dashboard & Stats DTOs
// ============================================================================

public class AppointmentDashboardResponse
{
    public DateTime Date { get; set; }
    public int TotalAppointments { get; set; }
    public int ScheduledCount { get; set; }
    public int ConfirmedCount { get; set; }
    public int CheckedInCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
    public int NoShowCount { get; set; }
    public List<AppointmentListResponse> UpcomingToday { get; set; } = new();
}

public class AppointmentStatsResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalAppointments { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
    public int NoShowCount { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal CancellationRate { get; set; }
    public decimal NoShowRate { get; set; }
    public Dictionary<string, int> ByStatus { get; set; } = new();
    public Dictionary<string, int> ByBookingSource { get; set; } = new();
    public Dictionary<string, int> ByService { get; set; } = new();
}

// ============================================================================
// Waitlist DTOs
// ============================================================================

public class WaitlistEntryResponse
{
    public int WaitlistId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int? PreferredTherapistId { get; set; }
    public string? PreferredTherapistName { get; set; }
    public DateTime? PreferredDate { get; set; }
    public TimeSpan? PreferredTimeFrom { get; set; }
    public TimeSpan? PreferredTimeTo { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AddServiceToAppointmentRequest
{
    [Required]
    public int ServiceId { get; set; }

    public int Quantity { get; set; } = 1;
}

public class AddToWaitlistRequest
{
    [Required]
    public int CustomerId { get; set; }

    [Required]
    public int ServiceId { get; set; }

    public int? PreferredTherapistId { get; set; }
    public DateTime? PreferredDate { get; set; }
    public TimeSpan? PreferredTimeFrom { get; set; }
    public TimeSpan? PreferredTimeTo { get; set; }
    public string? Notes { get; set; }
}

// ============================================================================
// Room DTOs
// ============================================================================

public class RoomResponse
{
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string RoomCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string RoomType { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public bool IsActive { get; set; }
}
