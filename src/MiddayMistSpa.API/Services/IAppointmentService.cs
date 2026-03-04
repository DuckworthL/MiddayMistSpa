using MiddayMistSpa.API.DTOs.Appointment;
using MiddayMistSpa.API.DTOs.Employee;

namespace MiddayMistSpa.API.Services;

public interface IAppointmentService
{
    // ========================================================================
    // Appointment CRUD
    // ========================================================================
    Task<AppointmentResponse> CreateAppointmentAsync(CreateAppointmentRequest request, int? createdBy = null);
    Task<AppointmentResponse?> GetAppointmentByIdAsync(int appointmentId);
    Task<AppointmentResponse?> GetAppointmentByNumberAsync(string appointmentNumber);
    Task<PagedResponse<AppointmentListResponse>> SearchAppointmentsAsync(AppointmentSearchRequest request);
    Task<AppointmentResponse> UpdateAppointmentAsync(int appointmentId, UpdateAppointmentRequest request);
    Task<bool> DeleteAppointmentAsync(int appointmentId);

    // ========================================================================
    // Status Workflow
    // ========================================================================
    Task<AppointmentResponse> ConfirmAppointmentAsync(int appointmentId);
    Task<AppointmentResponse> CheckInAppointmentAsync(int appointmentId);
    Task<AppointmentResponse> StartServiceAsync(int appointmentId);
    Task<AppointmentResponse> CompleteServiceAsync(int appointmentId);
    Task<AppointmentResponse> CancelAppointmentAsync(int appointmentId, CancelAppointmentRequest request);
    Task<AppointmentResponse> MarkAsNoShowAsync(int appointmentId);

    // ========================================================================
    // Scheduling & Rescheduling
    // ========================================================================
    Task<AppointmentResponse> RescheduleAppointmentAsync(int appointmentId, RescheduleAppointmentRequest request);
    Task<AppointmentResponse> AssignTherapistAsync(int appointmentId, int therapistId);

    // ========================================================================
    // Notes
    // ========================================================================
    Task<AppointmentResponse> AddTherapistNotesAsync(int appointmentId, string notes);

    // ========================================================================
    // Availability & Scheduling
    // ========================================================================
    Task<AvailabilityResponse> GetAvailabilityAsync(AvailabilityRequest request);
    Task<List<AvailableSlotResponse>> GetAvailableSlotsAsync(int serviceId, DateTime date, int? therapistId = null);
    Task<TherapistScheduleResponse> GetTherapistScheduleAsync(int therapistId, DateTime date);
    Task<DailyScheduleResponse> GetDailyScheduleAsync(DateTime date);

    // ========================================================================
    // Customer & Therapist Views
    // ========================================================================
    Task<List<AppointmentListResponse>> GetCustomerAppointmentsAsync(int customerId, bool includeCompleted = false);
    Task<List<AppointmentListResponse>> GetTherapistAppointmentsAsync(int therapistId, DateTime? date = null);
    Task<List<AppointmentListResponse>> GetTodaysAppointmentsAsync();
    Task<List<AppointmentListResponse>> GetUpcomingAppointmentsAsync(int days = 7);

    // ========================================================================
    // Dashboard & Statistics
    // ========================================================================
    Task<AppointmentDashboardResponse> GetDashboardAsync(DateTime date);
    Task<AppointmentStatsResponse> GetStatisticsAsync(DateTime startDate, DateTime endDate);

    // ========================================================================
    // Multi-Service Management
    // ========================================================================
    Task<AppointmentResponse> AddServiceToAppointmentAsync(int appointmentId, AddServiceToAppointmentRequest request);
    Task<AppointmentResponse> RemoveServiceFromAppointmentAsync(int appointmentId, int appointmentServiceItemId);

    // ========================================================================
    // Waitlist Management
    // ========================================================================
    Task<WaitlistEntryResponse> AddToWaitlistAsync(AddToWaitlistRequest request);
    Task<List<WaitlistEntryResponse>> GetWaitlistAsync(int? serviceId = null);
    Task<WaitlistEntryResponse?> GetNextWaitlistEntryAsync(int serviceId, DateTime date, TimeSpan time, int? therapistId = null);
    Task<bool> RemoveFromWaitlistAsync(int waitlistId);
    Task<AppointmentResponse?> ConvertWaitlistToAppointmentAsync(int waitlistId, DateTime date, TimeSpan startTime, int? therapistId = null);

    // ========================================================================
    // Validation & Helpers
    // ========================================================================
    Task<bool> IsSlotAvailableAsync(int serviceId, DateTime date, TimeSpan startTime, int? therapistId = null, int? excludeAppointmentId = null);
    Task<bool> HasConflictingAppointmentAsync(int customerId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeAppointmentId = null);

    // ========================================================================
    // Room Management
    // ========================================================================
    Task<List<RoomResponse>> GetActiveRoomsAsync();

    // ========================================================================
    // Archive
    // ========================================================================
    Task<bool> ArchiveAppointmentAsync(int appointmentId);
}
