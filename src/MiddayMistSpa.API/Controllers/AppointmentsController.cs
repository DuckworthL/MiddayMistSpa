using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiddayMistSpa.API.DTOs.Appointment;
using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.API.Services;
using System.Security.Claims;

namespace MiddayMistSpa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(IAppointmentService appointmentService, ILogger<AppointmentsController> logger)
    {
        _appointmentService = appointmentService;
        _logger = logger;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    #region Appointment CRUD

    [HttpPost]
    [Authorize(Policy = "Permission:appointments.create")]
    public async Task<ActionResult<AppointmentResponse>> CreateAppointment([FromBody] CreateAppointmentRequest request)
    {
        try
        {
            var appointment = await _appointmentService.CreateAppointmentAsync(request, GetCurrentUserId());
            return CreatedAtAction(nameof(GetAppointmentById), new { id = appointment.AppointmentId }, appointment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appointment");
            return BadRequest(new { error = $"[{ex.GetType().Name}] {ex.Message}" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AppointmentResponse>> GetAppointmentById(int id)
    {
        var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
        if (appointment == null) return NotFound();
        return Ok(appointment);
    }

    [HttpGet("by-number/{number}")]
    public async Task<ActionResult<AppointmentResponse>> GetAppointmentByNumber(string number)
    {
        var appointment = await _appointmentService.GetAppointmentByNumberAsync(number);
        if (appointment == null) return NotFound();
        return Ok(appointment);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<AppointmentListResponse>>> SearchAppointments([FromQuery] AppointmentSearchRequest request)
    {
        var result = await _appointmentService.SearchAppointmentsAsync(request);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "Permission:appointments.edit")]
    public async Task<ActionResult<AppointmentResponse>> UpdateAppointment(int id, [FromBody] UpdateAppointmentRequest request)
    {
        try
        {
            var appointment = await _appointmentService.UpdateAppointmentAsync(id, request);
            return Ok(appointment);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating appointment {AppointmentId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "Permission:appointments.delete")]
    public async Task<IActionResult> DeleteAppointment(int id)
    {
        var deleted = await _appointmentService.DeleteAppointmentAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    #endregion

    #region Status Workflow

    [HttpPost("{id}/confirm")]
    [Authorize(Policy = "Permission:appointments.edit")]
    public async Task<ActionResult<AppointmentResponse>> ConfirmAppointment(int id)
    {
        try
        {
            var appointment = await _appointmentService.ConfirmAppointmentAsync(id);
            return Ok(appointment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/check-in")]
    [Authorize(Policy = "Permission:appointments.edit")]
    public async Task<ActionResult<AppointmentResponse>> CheckInAppointment(int id)
    {
        try
        {
            var appointment = await _appointmentService.CheckInAppointmentAsync(id);
            return Ok(appointment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/start")]
    [Authorize(Policy = "Permission:appointments.edit")]
    public async Task<ActionResult<AppointmentResponse>> StartService(int id)
    {
        try
        {
            var appointment = await _appointmentService.StartServiceAsync(id);
            return Ok(appointment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/complete")]
    [Authorize(Policy = "Permission:appointments.edit")]
    public async Task<ActionResult<AppointmentResponse>> CompleteService(int id)
    {
        try
        {
            var appointment = await _appointmentService.CompleteServiceAsync(id);
            return Ok(appointment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Policy = "Permission:appointments.edit")]
    public async Task<ActionResult<AppointmentResponse>> CancelAppointment(int id, [FromBody] CancelAppointmentRequest request)
    {
        try
        {
            var appointment = await _appointmentService.CancelAppointmentAsync(id, request);
            return Ok(appointment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/no-show")]
    [Authorize(Policy = "Permission:appointments.edit")]
    public async Task<ActionResult<AppointmentResponse>> MarkAsNoShow(int id)
    {
        try
        {
            var appointment = await _appointmentService.MarkAsNoShowAsync(id);
            return Ok(appointment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/archive")]
    public async Task<IActionResult> ArchiveAppointment(int id)
    {
        try
        {
            var archived = await _appointmentService.ArchiveAppointmentAsync(id);
            if (!archived) return NotFound();
            return Ok(new { message = "Appointment archived." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Scheduling & Rescheduling

    [HttpPost("{id}/reschedule")]
    [Authorize(Policy = "Permission:appointments.edit")]
    public async Task<ActionResult<AppointmentResponse>> RescheduleAppointment(int id, [FromBody] RescheduleAppointmentRequest request)
    {
        try
        {
            var appointment = await _appointmentService.RescheduleAppointmentAsync(id, request);
            return Ok(appointment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/assign-therapist")]
    [Authorize(Policy = "Permission:appointments.edit")]
    public async Task<ActionResult<AppointmentResponse>> AssignTherapist(int id, [FromBody] AssignTherapistRequest request)
    {
        try
        {
            var appointment = await _appointmentService.AssignTherapistAsync(id, request.TherapistId);
            return Ok(appointment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/therapist-notes")]
    [Authorize(Policy = "Permission:appointments.edit")]
    public async Task<ActionResult<AppointmentResponse>> AddTherapistNotes(int id, [FromBody] AddTherapistNotesRequest request)
    {
        try
        {
            var appointment = await _appointmentService.AddTherapistNotesAsync(id, request.Notes);
            return Ok(appointment);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    #endregion

    #region Multi-Service Management

    [HttpPost("{id}/services")]
    [Authorize(Policy = "Permission:appointments.edit")]
    public async Task<ActionResult<AppointmentResponse>> AddServiceToAppointment(int id, [FromBody] AddServiceToAppointmentRequest request)
    {
        try
        {
            var appointment = await _appointmentService.AddServiceToAppointmentAsync(id, request);
            return Ok(appointment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}/services/{serviceItemId}")]
    [Authorize(Policy = "Permission:appointments.edit")]
    public async Task<ActionResult<AppointmentResponse>> RemoveServiceFromAppointment(int id, int serviceItemId)
    {
        try
        {
            var appointment = await _appointmentService.RemoveServiceFromAppointmentAsync(id, serviceItemId);
            return Ok(appointment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Availability & Scheduling

    [HttpPost("availability")]
    [AllowAnonymous]
    public async Task<ActionResult<AvailabilityResponse>> GetAvailability([FromBody] AvailabilityRequest request)
    {
        try
        {
            var availability = await _appointmentService.GetAvailabilityAsync(request);
            return Ok(availability);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("available-slots")]
    [AllowAnonymous]
    public async Task<ActionResult<List<AvailableSlotResponse>>> GetAvailableSlots(
        [FromQuery] int serviceId,
        [FromQuery] DateTime date,
        [FromQuery] int? therapistId = null)
    {
        try
        {
            var slots = await _appointmentService.GetAvailableSlotsAsync(serviceId, date, therapistId);
            return Ok(slots);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("therapist/{therapistId}/schedule")]
    public async Task<ActionResult<TherapistScheduleResponse>> GetTherapistSchedule(int therapistId, [FromQuery] DateTime date)
    {
        try
        {
            var schedule = await _appointmentService.GetTherapistScheduleAsync(therapistId, date);
            return Ok(schedule);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("daily-schedule")]
    public async Task<ActionResult<DailyScheduleResponse>> GetDailySchedule([FromQuery] DateTime date)
    {
        var schedule = await _appointmentService.GetDailyScheduleAsync(date);
        return Ok(schedule);
    }

    #endregion

    #region Customer & Therapist Views

    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<List<AppointmentListResponse>>> GetCustomerAppointments(
        int customerId,
        [FromQuery] bool includeCompleted = false)
    {
        var appointments = await _appointmentService.GetCustomerAppointmentsAsync(customerId, includeCompleted);
        return Ok(appointments);
    }

    [HttpGet("therapist/{therapistId}")]
    public async Task<ActionResult<List<AppointmentListResponse>>> GetTherapistAppointments(
        int therapistId,
        [FromQuery] DateTime? date = null)
    {
        var appointments = await _appointmentService.GetTherapistAppointmentsAsync(therapistId, date);
        return Ok(appointments);
    }

    [HttpGet("today")]
    public async Task<ActionResult<List<AppointmentListResponse>>> GetTodaysAppointments()
    {
        var appointments = await _appointmentService.GetTodaysAppointmentsAsync();
        return Ok(appointments);
    }

    [HttpGet("upcoming")]
    public async Task<ActionResult<List<AppointmentListResponse>>> GetUpcomingAppointments([FromQuery] int days = 7)
    {
        var appointments = await _appointmentService.GetUpcomingAppointmentsAsync(days);
        return Ok(appointments);
    }

    #endregion

    #region Dashboard & Statistics

    [HttpGet("dashboard")]
    public async Task<ActionResult<AppointmentDashboardResponse>> GetDashboard([FromQuery] DateTime? date = null)
    {
        var dashboard = await _appointmentService.GetDashboardAsync(date ?? DateTime.Today);
        return Ok(dashboard);
    }

    [HttpGet("statistics")]
    [Authorize(Policy = "Permission:reports.view")]
    public async Task<ActionResult<AppointmentStatsResponse>> GetStatistics(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var stats = await _appointmentService.GetStatisticsAsync(startDate, endDate);
        return Ok(stats);
    }

    #endregion

    #region Waitlist Management

    [HttpPost("waitlist")]
    [Authorize(Policy = "Permission:appointments.create")]
    public async Task<ActionResult<WaitlistEntryResponse>> AddToWaitlist([FromBody] AddToWaitlistRequest request)
    {
        try
        {
            var entry = await _appointmentService.AddToWaitlistAsync(request);
            return CreatedAtAction(nameof(GetWaitlist), null, entry);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("waitlist")]
    public async Task<ActionResult<List<WaitlistEntryResponse>>> GetWaitlist([FromQuery] int? serviceId = null)
    {
        var waitlist = await _appointmentService.GetWaitlistAsync(serviceId);
        return Ok(waitlist);
    }

    [HttpDelete("waitlist/{waitlistId}")]
    [Authorize(Policy = "Permission:appointments.delete")]
    public async Task<IActionResult> RemoveFromWaitlist(int waitlistId)
    {
        var removed = await _appointmentService.RemoveFromWaitlistAsync(waitlistId);
        if (!removed) return NotFound();
        return NoContent();
    }

    [HttpPost("waitlist/{waitlistId}/convert")]
    [Authorize(Policy = "Permission:appointments.create")]
    public async Task<ActionResult<AppointmentResponse>> ConvertWaitlistToAppointment(
        int waitlistId,
        [FromQuery] DateTime date,
        [FromQuery] TimeSpan startTime,
        [FromQuery] int? therapistId = null)
    {
        try
        {
            var appointment = await _appointmentService.ConvertWaitlistToAppointmentAsync(waitlistId, date, startTime, therapistId);
            if (appointment == null) return NotFound();
            return Ok(appointment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Validation

    [HttpGet("check-slot")]
    public async Task<ActionResult<object>> CheckSlotAvailability(
        [FromQuery] int serviceId,
        [FromQuery] DateTime date,
        [FromQuery] TimeSpan startTime,
        [FromQuery] int? therapistId = null)
    {
        var isAvailable = await _appointmentService.IsSlotAvailableAsync(serviceId, date, startTime, therapistId);
        return Ok(new { isAvailable });
    }

    #endregion

    #region Room Management

    /// <summary>
    /// Get all active rooms for appointment booking
    /// </summary>
    [HttpGet("rooms")]
    public async Task<ActionResult<List<RoomResponse>>> GetActiveRooms()
    {
        var rooms = await _appointmentService.GetActiveRoomsAsync();
        return Ok(rooms);
    }

    #endregion
}
