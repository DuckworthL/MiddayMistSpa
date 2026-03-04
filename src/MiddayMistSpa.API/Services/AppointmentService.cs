using Microsoft.EntityFrameworkCore;
using MiddayMistSpa.API.DTOs.Appointment;
using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.API.DTOs.Notification;
using MiddayMistSpa.Core;
using MiddayMistSpa.Core.Entities.Appointment;
using MiddayMistSpa.Infrastructure.Data;

namespace MiddayMistSpa.API.Services;

public class AppointmentService : IAppointmentService
{
    private readonly SpaDbContext _context;
    private readonly ILogger<AppointmentService> _logger;
    private readonly INotificationService _notificationService;

    // Business hours (configurable in real app via SystemSettings)
    private static readonly TimeSpan OpeningTime = new(9, 0, 0);   // 9:00 AM
    private static readonly TimeSpan ClosingTime = new(21, 0, 0);  // 9:00 PM
    private const int SlotIntervalMinutes = 30;

    public AppointmentService(SpaDbContext context, ILogger<AppointmentService> logger, INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
    }

    #region Appointment CRUD

    public async Task<AppointmentResponse> CreateAppointmentAsync(CreateAppointmentRequest request, int? createdBy = null)
    {
        // Validate customer
        var customer = await _context.Customers.FindAsync(request.CustomerId)
            ?? throw new InvalidOperationException($"Customer with ID {request.CustomerId} not found");

        // Validate service
        var service = await _context.Services.FindAsync(request.ServiceId)
            ?? throw new InvalidOperationException($"Service with ID {request.ServiceId} not found");

        // Calculate end time
        var endTime = request.StartTime.Add(TimeSpan.FromMinutes(service.DurationMinutes));

        // Validate therapist if specified
        if (request.TherapistId.HasValue)
        {
            var therapist = await _context.Employees.FindAsync(request.TherapistId.Value)
                ?? throw new InvalidOperationException($"Therapist with ID {request.TherapistId.Value} not found");

            if (!therapist.IsTherapist)
                throw new InvalidOperationException("Assigned employee is not a therapist");

            // For today's appointments, verify therapist is clocked in and not on break
            if (request.AppointmentDate.Date == PhilippineTime.Today)
            {
                var todayRecord = await _context.AttendanceRecords
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.EmployeeId == request.TherapistId.Value
                        && a.Date == PhilippineTime.Today);

                if (todayRecord == null)
                    throw new InvalidOperationException($"Therapist {therapist.FullName} has not clocked in today and cannot be assigned to appointments");

                if (todayRecord.Status == "OnBreak")
                    throw new InvalidOperationException($"Therapist {therapist.FullName} is currently on break");

                if (todayRecord.ClockOut != null)
                    throw new InvalidOperationException($"Therapist {therapist.FullName} has already clocked out for today");
            }
        }

        // Check for slot availability with detailed reason
        var unavailabilityReason = await GetSlotUnavailabilityReasonAsync(
            request.ServiceId, request.AppointmentDate, request.StartTime, request.TherapistId);

        if (unavailabilityReason != null)
            throw new InvalidOperationException(unavailabilityReason);

        // Check customer conflict
        var hasConflict = await HasConflictingAppointmentAsync(
            request.CustomerId, request.AppointmentDate, request.StartTime, endTime);

        if (hasConflict)
            throw new InvalidOperationException($"Customer {customer.FirstName} {customer.LastName} already has an appointment on {request.AppointmentDate:MMM dd, yyyy} at {DateTime.Today.Add(request.StartTime):h:mm tt}");

        // Generate appointment number
        var appointmentNumber = await GenerateAppointmentNumberAsync(request.AppointmentDate);

        var appointment = new Appointment
        {
            AppointmentNumber = appointmentNumber,
            CustomerId = request.CustomerId,
            ServiceId = request.ServiceId,
            TherapistId = request.TherapistId,
            RoomId = request.RoomId,
            AppointmentDate = request.AppointmentDate.Date,
            StartTime = request.StartTime,
            EndTime = endTime,
            Status = "Scheduled",
            BookingSource = request.BookingSource,
            CustomerNotes = request.CustomerNotes,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created appointment {AppointmentNumber} for customer {CustomerId}",
            appointmentNumber, request.CustomerId);

        // Create in-app notification for appointment booked
        try
        {
            await _notificationService.CreateNotificationAsync(new CreateNotificationRequest
            {
                UserId = createdBy,
                Type = "InApp",
                Category = "Appointment",
                Title = "New Appointment Booked",
                Message = $"Appointment {appointmentNumber} booked for {customer.FirstName} {customer.LastName} — {service.ServiceName} on {request.AppointmentDate:MMM dd, yyyy} at {request.StartTime:hh\\:mm tt}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create notification for appointment {AppointmentNumber}", appointmentNumber);
        }

        return await GetAppointmentByIdAsync(appointment.AppointmentId)
            ?? throw new InvalidOperationException("Failed to retrieve created appointment");
    }

    public async Task<AppointmentResponse?> GetAppointmentByIdAsync(int appointmentId)
    {
        var appointment = await _context.Appointments
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.Service)
            .Include(a => a.ServiceItems).ThenInclude(si => si.Service)
            .Include(a => a.Therapist)
            .Include(a => a.Room)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

        return appointment == null ? null : MapToAppointmentResponse(appointment);
    }

    public async Task<AppointmentResponse?> GetAppointmentByNumberAsync(string appointmentNumber)
    {
        var appointment = await _context.Appointments
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.Service)
            .Include(a => a.ServiceItems).ThenInclude(si => si.Service)
            .Include(a => a.Therapist)
            .Include(a => a.Room)
            .FirstOrDefaultAsync(a => a.AppointmentNumber == appointmentNumber);

        return appointment == null ? null : MapToAppointmentResponse(appointment);
    }

    public async Task<PagedResponse<AppointmentListResponse>> SearchAppointmentsAsync(AppointmentSearchRequest request)
    {
        var query = _context.Appointments
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.Service)
            .Include(a => a.ServiceItems).ThenInclude(si => si.Service)
            .Include(a => a.Therapist)
            .Include(a => a.Room)
            .AsQueryable();

        // When IncludeArchived is true show ONLY archived; otherwise hide all archived
        if (request.IncludeArchived)
            query = query.Where(a => a.IsArchived);
        else
            query = query.Where(a => !a.IsArchived);

        // Search term
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(a =>
                a.AppointmentNumber.ToLower().Contains(term) ||
                a.Customer.FirstName.ToLower().Contains(term) ||
                a.Customer.LastName.ToLower().Contains(term) ||
                a.Service.ServiceName.ToLower().Contains(term));
        }

        // Filters
        if (request.CustomerId.HasValue)
            query = query.Where(a => a.CustomerId == request.CustomerId.Value);

        if (request.TherapistId.HasValue)
            query = query.Where(a => a.TherapistId == request.TherapistId.Value);

        if (request.ServiceId.HasValue)
            query = query.Where(a => a.ServiceId == request.ServiceId.Value);

        if (request.DateFrom.HasValue)
            query = query.Where(a => a.AppointmentDate >= request.DateFrom.Value.Date);

        if (request.DateTo.HasValue)
            query = query.Where(a => a.AppointmentDate <= request.DateTo.Value.Date);

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(a => a.Status == request.Status);

        if (!string.IsNullOrWhiteSpace(request.BookingSource))
            query = query.Where(a => a.BookingSource == request.BookingSource);

        // Sort
        query = request.SortBy?.ToLower() switch
        {
            "customer" => request.SortDescending
                ? query.OrderByDescending(a => a.Customer.LastName)
                : query.OrderBy(a => a.Customer.LastName),
            "service" => request.SortDescending
                ? query.OrderByDescending(a => a.Service.ServiceName)
                : query.OrderBy(a => a.Service.ServiceName),
            "status" => request.SortDescending
                ? query.OrderByDescending(a => a.Status)
                : query.OrderBy(a => a.Status),
            _ => request.SortDescending
                ? query.OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.StartTime)
                : query.OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => MapToListResponse(a))
            .ToListAsync();

        return new PagedResponse<AppointmentListResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<AppointmentResponse> UpdateAppointmentAsync(int appointmentId, UpdateAppointmentRequest request)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Service)
            .Include(a => a.ServiceItems).ThenInclude(si => si.Service)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId)
            ?? throw new InvalidOperationException($"Appointment with ID {appointmentId} not found");

        if (appointment.Status is "Completed" or "Cancelled" or "No Show" or "In Progress" or "Checked In")
            throw new InvalidOperationException($"Cannot update appointment with status: {appointment.Status}");

        if (request.CustomerId.HasValue)
        {
            var customer = await _context.Customers.FindAsync(request.CustomerId.Value)
                ?? throw new InvalidOperationException($"Customer with ID {request.CustomerId.Value} not found");
            appointment.CustomerId = request.CustomerId.Value;
        }

        if (request.ServiceId.HasValue)
        {
            var service = await _context.Services.FindAsync(request.ServiceId.Value)
                ?? throw new InvalidOperationException($"Service with ID {request.ServiceId.Value} not found");
            appointment.ServiceId = request.ServiceId.Value;
            appointment.Service = service;
        }

        if (request.TherapistId.HasValue)
            appointment.TherapistId = request.TherapistId.Value;

        if (request.RoomId.HasValue)
            appointment.RoomId = request.RoomId.Value;

        if (request.AppointmentDate.HasValue || request.StartTime.HasValue || request.TherapistId.HasValue)
        {
            var newDate = request.AppointmentDate ?? appointment.AppointmentDate;
            var newStartTime = request.StartTime ?? appointment.StartTime;
            var newEndTime = newStartTime.Add(TimeSpan.FromMinutes(appointment.Service.DurationMinutes));

            // Validate slot availability with detailed reason
            var unavailabilityReason = await GetSlotUnavailabilityReasonAsync(
                appointment.ServiceId, newDate, newStartTime, appointment.TherapistId, appointmentId);

            if (unavailabilityReason != null)
                throw new InvalidOperationException(unavailabilityReason);

            // Check customer conflict
            var customerId = request.CustomerId ?? appointment.CustomerId;
            var hasConflict = await HasConflictingAppointmentAsync(
                customerId, newDate, newStartTime, newEndTime, appointmentId);

            if (hasConflict)
                throw new InvalidOperationException("Customer already has an appointment at this time");

            appointment.AppointmentDate = newDate.Date;
            appointment.StartTime = newStartTime;
            appointment.EndTime = newEndTime;
        }

        if (request.CustomerNotes != null)
            appointment.CustomerNotes = request.CustomerNotes;

        appointment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetAppointmentByIdAsync(appointmentId)
            ?? throw new InvalidOperationException("Failed to retrieve updated appointment");
    }

    public async Task<bool> DeleteAppointmentAsync(int appointmentId)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment == null) return false;

        // Only allow deletion of Scheduled or Cancelled appointments
        if (appointment.Status is not "Scheduled" and not "Cancelled")
            throw new InvalidOperationException($"Cannot delete appointment with status: {appointment.Status}. Only Scheduled or Cancelled appointments can be deleted.");

        _context.Appointments.Remove(appointment);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ArchiveAppointmentAsync(int appointmentId)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment == null) return false;

        if (appointment.Status is not "Completed" and not "Cancelled" and not "No Show")
            throw new InvalidOperationException($"Cannot archive appointment with status: {appointment.Status}. Only Completed, Cancelled, or No Show appointments can be archived.");

        if (appointment.IsArchived)
            return true; // already archived

        appointment.IsArchived = true;
        appointment.ArchivedAt = DateTime.UtcNow;
        appointment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Status Workflow

    public async Task<AppointmentResponse> ConfirmAppointmentAsync(int appointmentId)
    {
        var appointment = await GetAppointmentEntityAsync(appointmentId);

        if (appointment.Status != "Scheduled")
            throw new InvalidOperationException($"Cannot confirm appointment with status: {appointment.Status}");

        appointment.Status = "Confirmed";
        appointment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Confirmed appointment {AppointmentNumber}", appointment.AppointmentNumber);
        return await GetAppointmentByIdAsync(appointmentId) ?? throw new InvalidOperationException("Appointment not found");
    }

    public async Task<AppointmentResponse> CheckInAppointmentAsync(int appointmentId)
    {
        var appointment = await GetAppointmentEntityAsync(appointmentId);

        if (appointment.Status is not "Scheduled" and not "Confirmed")
            throw new InvalidOperationException($"Cannot check in appointment with status: {appointment.Status}");

        appointment.Status = "Checked In";
        appointment.CheckedInAt = DateTime.UtcNow;
        appointment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Checked in appointment {AppointmentNumber}", appointment.AppointmentNumber);
        return await GetAppointmentByIdAsync(appointmentId) ?? throw new InvalidOperationException("Appointment not found");
    }

    public async Task<AppointmentResponse> StartServiceAsync(int appointmentId)
    {
        var appointment = await GetAppointmentEntityAsync(appointmentId);

        if (appointment.Status != "Checked In")
            throw new InvalidOperationException($"Cannot start service for appointment with status: {appointment.Status}");

        if (!appointment.TherapistId.HasValue)
            throw new InvalidOperationException("Cannot start service without an assigned therapist");

        appointment.Status = "In Progress";
        appointment.ServiceStartedAt = DateTime.UtcNow;
        appointment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Started service for appointment {AppointmentNumber}", appointment.AppointmentNumber);
        return await GetAppointmentByIdAsync(appointmentId) ?? throw new InvalidOperationException("Appointment not found");
    }

    public async Task<AppointmentResponse> CompleteServiceAsync(int appointmentId)
    {
        var appointment = await GetAppointmentEntityAsync(appointmentId);

        if (appointment.Status != "In Progress")
            throw new InvalidOperationException($"Cannot complete appointment with status: {appointment.Status}");

        appointment.Status = "Completed";
        appointment.ServiceCompletedAt = DateTime.UtcNow;
        appointment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Update customer visit count
        var customer = await _context.Customers.FindAsync(appointment.CustomerId);
        if (customer != null)
        {
            customer.TotalVisits++;
            customer.LastVisitDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        // Create in-app notification for appointment completed
        try
        {
            await _notificationService.CreateNotificationAsync(new CreateNotificationRequest
            {
                Type = "InApp",
                Category = "Appointment",
                Title = "Appointment Completed",
                Message = $"Appointment {appointment.AppointmentNumber} has been completed successfully."
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create notification for completed appointment {AppointmentNumber}", appointment.AppointmentNumber);
        }

        _logger.LogInformation("Completed appointment {AppointmentNumber}", appointment.AppointmentNumber);
        return await GetAppointmentByIdAsync(appointmentId) ?? throw new InvalidOperationException("Appointment not found");
    }

    public async Task<AppointmentResponse> CancelAppointmentAsync(int appointmentId, CancelAppointmentRequest request)
    {
        var appointment = await GetAppointmentEntityAsync(appointmentId);

        if (appointment.Status is "Completed" or "Cancelled" or "No Show")
            throw new InvalidOperationException($"Cannot cancel appointment with status: {appointment.Status}");

        appointment.Status = "Cancelled";
        appointment.CancelledAt = DateTime.UtcNow;
        appointment.CancellationReason = request.Reason;
        appointment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Create in-app notification for appointment cancelled
        try
        {
            await _notificationService.CreateNotificationAsync(new CreateNotificationRequest
            {
                Type = "InApp",
                Category = "Appointment",
                Title = "Appointment Cancelled",
                Message = $"Appointment {appointment.AppointmentNumber} has been cancelled.{(string.IsNullOrEmpty(request.Reason) ? "" : $" Reason: {request.Reason}")}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create notification for cancelled appointment {AppointmentNumber}", appointment.AppointmentNumber);
        }

        _logger.LogInformation("Cancelled appointment {AppointmentNumber}: {Reason}",
            appointment.AppointmentNumber, request.Reason);

        return await GetAppointmentByIdAsync(appointmentId) ?? throw new InvalidOperationException("Appointment not found");
    }

    public async Task<AppointmentResponse> MarkAsNoShowAsync(int appointmentId)
    {
        var appointment = await GetAppointmentEntityAsync(appointmentId);

        if (appointment.Status is "Completed" or "Cancelled" or "No Show" or "In Progress")
            throw new InvalidOperationException($"Cannot mark as no-show for appointment with status: {appointment.Status}");

        appointment.Status = "No Show";
        appointment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Marked appointment {AppointmentNumber} as no-show", appointment.AppointmentNumber);
        return await GetAppointmentByIdAsync(appointmentId) ?? throw new InvalidOperationException("Appointment not found");
    }

    #endregion

    #region Multi-Service Management

    public async Task<AppointmentResponse> AddServiceToAppointmentAsync(int appointmentId, AddServiceToAppointmentRequest request)
    {
        var appointment = await GetAppointmentEntityAsync(appointmentId);

        if (appointment.Status is "Completed" or "Cancelled" or "No Show")
            throw new InvalidOperationException($"Cannot add services to appointment with status: {appointment.Status}");

        var service = await _context.Services.FindAsync(request.ServiceId)
            ?? throw new InvalidOperationException($"Service with ID {request.ServiceId} not found");

        if (!service.IsActive)
            throw new InvalidOperationException($"Service '{service.ServiceName}' is not active");

        var item = new AppointmentServiceItem
        {
            AppointmentId = appointmentId,
            ServiceId = request.ServiceId,
            UnitPrice = service.RegularPrice,
            DurationMinutes = service.DurationMinutes,
            Quantity = request.Quantity,
            AddedAt = DateTime.UtcNow
        };

        _context.AppointmentServiceItems.Add(item);

        // Update the appointment end time to accommodate the extra service duration
        var extraDuration = TimeSpan.FromMinutes(service.DurationMinutes * request.Quantity);
        appointment.EndTime = appointment.EndTime.Add(extraDuration);
        appointment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Added service {ServiceName} to appointment {AppointmentNumber}",
            service.ServiceName, appointment.AppointmentNumber);

        return await GetAppointmentByIdAsync(appointmentId)
            ?? throw new InvalidOperationException("Failed to retrieve updated appointment");
    }

    public async Task<AppointmentResponse> RemoveServiceFromAppointmentAsync(int appointmentId, int appointmentServiceItemId)
    {
        var appointment = await GetAppointmentEntityAsync(appointmentId);

        if (appointment.Status is "Completed" or "Cancelled" or "No Show")
            throw new InvalidOperationException($"Cannot remove services from appointment with status: {appointment.Status}");

        var item = await _context.AppointmentServiceItems
            .FirstOrDefaultAsync(si => si.AppointmentServiceItemId == appointmentServiceItemId && si.AppointmentId == appointmentId)
            ?? throw new InvalidOperationException("Service item not found on this appointment");

        // Reduce the end time
        var removedDuration = TimeSpan.FromMinutes(item.DurationMinutes * item.Quantity);
        appointment.EndTime = appointment.EndTime.Subtract(removedDuration);
        appointment.UpdatedAt = DateTime.UtcNow;

        _context.AppointmentServiceItems.Remove(item);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Removed service item {ItemId} from appointment {AppointmentNumber}",
            appointmentServiceItemId, appointment.AppointmentNumber);

        return await GetAppointmentByIdAsync(appointmentId)
            ?? throw new InvalidOperationException("Failed to retrieve updated appointment");
    }

    #endregion

    #region Scheduling & Rescheduling

    public async Task<AppointmentResponse> RescheduleAppointmentAsync(int appointmentId, RescheduleAppointmentRequest request)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Service)
            .Include(a => a.ServiceItems).ThenInclude(si => si.Service)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId)
            ?? throw new InvalidOperationException($"Appointment with ID {appointmentId} not found");

        if (appointment.Status is "Completed" or "Cancelled" or "No Show")
            throw new InvalidOperationException($"Cannot reschedule appointment with status: {appointment.Status}");

        var newEndTime = request.NewStartTime.Add(TimeSpan.FromMinutes(appointment.Service.DurationMinutes));
        var therapistId = request.NewTherapistId ?? appointment.TherapistId;

        // Check slot availability with detailed reason
        var unavailabilityReason = await GetSlotUnavailabilityReasonAsync(
            appointment.ServiceId, request.NewDate, request.NewStartTime, therapistId, appointmentId);

        if (unavailabilityReason != null)
            throw new InvalidOperationException(unavailabilityReason);

        // Check customer conflict
        var hasConflict = await HasConflictingAppointmentAsync(
            appointment.CustomerId, request.NewDate, request.NewStartTime, newEndTime, appointmentId);

        if (hasConflict)
            throw new InvalidOperationException("Customer already has an appointment at the new time");

        appointment.AppointmentDate = request.NewDate.Date;
        appointment.StartTime = request.NewStartTime;
        appointment.EndTime = newEndTime;
        if (request.NewTherapistId.HasValue)
            appointment.TherapistId = request.NewTherapistId.Value;

        // Reset status back to Scheduled when rescheduling
        if (appointment.Status is "Confirmed" or "Checked In")
        {
            appointment.Status = "Scheduled";
            appointment.CheckedInAt = null;
        }

        appointment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Create in-app notification for appointment rescheduled
        try
        {
            await _notificationService.CreateNotificationAsync(new CreateNotificationRequest
            {
                Type = "InApp",
                Category = "Appointment",
                Title = "Appointment Rescheduled",
                Message = $"Appointment {appointment.AppointmentNumber} rescheduled to {request.NewDate:MMM dd, yyyy} at {request.NewStartTime:hh\\:mm tt}."
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create notification for rescheduled appointment {AppointmentNumber}", appointment.AppointmentNumber);
        }

        _logger.LogInformation("Rescheduled appointment {AppointmentNumber} to {Date} {Time}",
            appointment.AppointmentNumber, request.NewDate.ToShortDateString(), request.NewStartTime);

        return await GetAppointmentByIdAsync(appointmentId) ?? throw new InvalidOperationException("Appointment not found");
    }

    public async Task<AppointmentResponse> AssignTherapistAsync(int appointmentId, int therapistId)
    {
        var appointment = await GetAppointmentEntityAsync(appointmentId);

        var therapist = await _context.Employees.FindAsync(therapistId)
            ?? throw new InvalidOperationException($"Therapist with ID {therapistId} not found");

        if (!therapist.IsTherapist)
            throw new InvalidOperationException("Assigned employee is not a therapist");

        appointment.TherapistId = therapistId;
        appointment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Assigned therapist {TherapistId} to appointment {AppointmentNumber}",
            therapistId, appointment.AppointmentNumber);

        return await GetAppointmentByIdAsync(appointmentId) ?? throw new InvalidOperationException("Appointment not found");
    }

    public async Task<AppointmentResponse> AddTherapistNotesAsync(int appointmentId, string notes)
    {
        var appointment = await GetAppointmentEntityAsync(appointmentId);

        appointment.TherapistNotes = string.IsNullOrWhiteSpace(appointment.TherapistNotes)
            ? notes
            : $"{appointment.TherapistNotes}\n---\n{notes}";
        appointment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetAppointmentByIdAsync(appointmentId) ?? throw new InvalidOperationException("Appointment not found");
    }

    #endregion

    #region Availability & Scheduling

    public async Task<AvailabilityResponse> GetAvailabilityAsync(AvailabilityRequest request)
    {
        var service = await _context.Services.FindAsync(request.ServiceId)
            ?? throw new InvalidOperationException($"Service with ID {request.ServiceId} not found");

        var slots = await GetAvailableSlotsAsync(request.ServiceId, request.Date, request.PreferredTherapistId);

        return new AvailabilityResponse
        {
            Date = request.Date.Date,
            ServiceId = request.ServiceId,
            ServiceName = service.ServiceName,
            DurationMinutes = service.DurationMinutes,
            AvailableSlots = slots
        };
    }

    public async Task<List<AvailableSlotResponse>> GetAvailableSlotsAsync(int serviceId, DateTime date, int? therapistId = null)
    {
        var service = await _context.Services.FindAsync(serviceId)
            ?? throw new InvalidOperationException($"Service with ID {serviceId} not found");

        var availableSlots = new List<AvailableSlotResponse>();
        var dayOfWeek = (int)date.DayOfWeek;

        // Get therapists scheduled to work on this day (check EmployeeShifts)
        var scheduledTherapistIds = await _context.EmployeeShifts
            .AsNoTracking()
            .Where(s =>
                s.Employee.IsTherapist &&
                s.Employee.IsActive &&
                s.IsActive &&
                s.DayOfWeek == dayOfWeek &&
                s.EffectiveFrom <= date &&
                (s.EffectiveTo == null || s.EffectiveTo >= date))
            .Select(s => s.EmployeeId)
            .Distinct()
            .ToListAsync();

        // Exclude therapists on approved leave
        var therapistsOnLeave = await _context.TimeOffRequests
            .AsNoTracking()
            .Where(t =>
                scheduledTherapistIds.Contains(t.EmployeeId) &&
                t.Status == "Approved" &&
                t.StartDate <= date &&
                t.EndDate >= date)
            .Select(t => t.EmployeeId)
            .ToListAsync();

        var availableTherapistIds = scheduledTherapistIds.Except(therapistsOnLeave).ToHashSet();

        // Get the shift details for shift time checks
        var shifts = await _context.EmployeeShifts
            .AsNoTracking()
            .Where(s =>
                availableTherapistIds.Contains(s.EmployeeId) &&
                s.IsActive &&
                s.DayOfWeek == dayOfWeek &&
                s.EffectiveFrom <= date &&
                (s.EffectiveTo == null || s.EffectiveTo >= date))
            .ToListAsync();

        // Build a lookup: therapistId -> their shift for this day
        var scheduleByTherapist = shifts
            .GroupBy(s => s.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.EffectiveFrom).First());

        // Get therapist details
        // Get ALL therapists (including unavailable) for detailed reporting
        var allTherapistQuery = _context.Employees
            .AsNoTracking()
            .Where(e => e.IsTherapist && e.IsActive);

        if (therapistId.HasValue)
            allTherapistQuery = allTherapistQuery.Where(e => e.EmployeeId == therapistId.Value);

        var allTherapists = await allTherapistQuery.ToListAsync();

        // Get existing appointments for the day
        var existingAppointments = await _context.Appointments
            .AsNoTracking()
            .Where(a => a.AppointmentDate.Date == date.Date &&
                       a.Status != "Cancelled" && a.Status != "No Show")
            .ToListAsync();

        // Generate time slots
        var currentSlot = OpeningTime;
        var isToday = date.Date == PhilippineTime.Today;
        var nowTimeOfDay = PhilippineTime.Now.TimeOfDay;

        // For today's appointments, load attendance records to check clock-in status
        Dictionary<int, string> todayAttendanceStatus = new();
        if (isToday)
        {
            var todayRecords = await _context.AttendanceRecords
                .AsNoTracking()
                .Where(a => a.Date == PhilippineTime.Today)
                .ToListAsync();

            foreach (var rec in todayRecords)
            {
                if (rec.ClockOut != null)
                    todayAttendanceStatus[rec.EmployeeId] = "ClockedOut";
                else if (rec.Status == "OnBreak")
                    todayAttendanceStatus[rec.EmployeeId] = "OnBreak";
                else
                    todayAttendanceStatus[rec.EmployeeId] = "ClockedIn";
            }
        }

        while (currentSlot.Add(TimeSpan.FromMinutes(service.DurationMinutes)) <= ClosingTime)
        {
            var slotEnd = currentSlot.Add(TimeSpan.FromMinutes(service.DurationMinutes));

            // Skip past slots for today
            if (isToday && currentSlot <= nowTimeOfDay)
            {
                currentSlot = currentSlot.Add(TimeSpan.FromMinutes(SlotIntervalMinutes));
                continue;
            }
            var slotAvailableTherapists = new List<AvailableTherapistResponse>();
            var slotUnavailableTherapists = new List<UnavailableTherapistResponse>();

            foreach (var therapist in allTherapists)
            {
                var name = $"{therapist.FirstName} {therapist.LastName}";

                // Check if on leave
                if (therapistsOnLeave.Contains(therapist.EmployeeId))
                {
                    slotUnavailableTherapists.Add(new UnavailableTherapistResponse
                    {
                        TherapistId = therapist.EmployeeId,
                        TherapistName = name,
                        Reason = "On Leave"
                    });
                    continue;
                }

                // For today: check if therapist is clocked in (not on break, not clocked out, not absent)
                if (isToday)
                {
                    if (!todayAttendanceStatus.TryGetValue(therapist.EmployeeId, out var attStatus))
                    {
                        slotUnavailableTherapists.Add(new UnavailableTherapistResponse
                        {
                            TherapistId = therapist.EmployeeId,
                            TherapistName = name,
                            Reason = "Not Clocked In"
                        });
                        continue;
                    }
                    if (attStatus == "OnBreak")
                    {
                        slotUnavailableTherapists.Add(new UnavailableTherapistResponse
                        {
                            TherapistId = therapist.EmployeeId,
                            TherapistName = name,
                            Reason = "Currently On Break"
                        });
                        continue;
                    }
                    if (attStatus == "ClockedOut")
                    {
                        slotUnavailableTherapists.Add(new UnavailableTherapistResponse
                        {
                            TherapistId = therapist.EmployeeId,
                            TherapistName = name,
                            Reason = "Already Clocked Out"
                        });
                        continue;
                    }
                }

                // Check if scheduled to work
                if (!scheduleByTherapist.TryGetValue(therapist.EmployeeId, out var sched))
                {
                    slotUnavailableTherapists.Add(new UnavailableTherapistResponse
                    {
                        TherapistId = therapist.EmployeeId,
                        TherapistName = name,
                        Reason = "Not Scheduled"
                    });
                    continue;
                }

                // Check if outside shift hours
                if (currentSlot < sched.StartTime || slotEnd > sched.EndTime)
                {
                    slotUnavailableTherapists.Add(new UnavailableTherapistResponse
                    {
                        TherapistId = therapist.EmployeeId,
                        TherapistName = name,
                        Reason = "Outside Shift"
                    });
                    continue;
                }

                // Check if has conflicting appointment
                var hasConflict = existingAppointments.Any(a =>
                    a.TherapistId == therapist.EmployeeId &&
                    DoTimesOverlap(a.StartTime, a.EndTime, currentSlot, slotEnd));

                if (hasConflict)
                {
                    slotUnavailableTherapists.Add(new UnavailableTherapistResponse
                    {
                        TherapistId = therapist.EmployeeId,
                        TherapistName = name,
                        Reason = "Already Booked"
                    });
                    continue;
                }

                slotAvailableTherapists.Add(new AvailableTherapistResponse
                {
                    TherapistId = therapist.EmployeeId,
                    TherapistName = name
                });
            }

            // Include slot even if no therapists are available (for visibility)
            availableSlots.Add(new AvailableSlotResponse
            {
                StartTime = currentSlot,
                EndTime = slotEnd,
                AvailableTherapists = slotAvailableTherapists,
                UnavailableTherapists = slotUnavailableTherapists
            });

            currentSlot = currentSlot.Add(TimeSpan.FromMinutes(SlotIntervalMinutes));
        }

        return availableSlots;
    }

    public async Task<TherapistScheduleResponse> GetTherapistScheduleAsync(int therapistId, DateTime date)
    {
        var therapist = await _context.Employees.FindAsync(therapistId)
            ?? throw new InvalidOperationException($"Therapist with ID {therapistId} not found");

        var appointments = await _context.Appointments
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.Service)
            .Include(a => a.ServiceItems).ThenInclude(si => si.Service)
            .Where(a => a.TherapistId == therapistId &&
                       a.AppointmentDate.Date == date.Date &&
                       a.Status != "Cancelled")
            .OrderBy(a => a.StartTime)
            .ToListAsync();

        var timeSlots = new List<TimeSlotResponse>();
        var currentSlot = OpeningTime;

        while (currentSlot < ClosingTime)
        {
            var slotEnd = currentSlot.Add(TimeSpan.FromMinutes(SlotIntervalMinutes));
            var appointment = appointments.FirstOrDefault(a =>
                DoTimesOverlap(a.StartTime, a.EndTime, currentSlot, slotEnd));

            timeSlots.Add(new TimeSlotResponse
            {
                StartTime = currentSlot,
                EndTime = slotEnd,
                IsAvailable = appointment == null,
                AppointmentId = appointment?.AppointmentId,
                CustomerName = appointment != null
                    ? $"{appointment.Customer.FirstName} {appointment.Customer.LastName}"
                    : null,
                ServiceName = appointment?.Service.ServiceName
            });

            currentSlot = slotEnd;
        }

        return new TherapistScheduleResponse
        {
            TherapistId = therapistId,
            TherapistName = $"{therapist.FirstName} {therapist.LastName}",
            Date = date.Date,
            TimeSlots = timeSlots,
            Appointments = appointments.Select(a => MapToListResponse(a)).ToList()
        };
    }

    public async Task<DailyScheduleResponse> GetDailyScheduleAsync(DateTime date)
    {
        var appointments = await _context.Appointments
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.Service)
            .Include(a => a.ServiceItems).ThenInclude(si => si.Service)
            .Include(a => a.Therapist)
            .Include(a => a.Room)
            .Where(a => a.AppointmentDate.Date == date.Date)
            .OrderBy(a => a.StartTime)
            .ToListAsync();

        return new DailyScheduleResponse
        {
            Date = date.Date,
            Appointments = appointments.Select(a => MapToListResponse(a)).ToList(),
            TotalAppointments = appointments.Count,
            CompletedCount = appointments.Count(a => a.Status == "Completed"),
            PendingCount = appointments.Count(a => a.Status is "Scheduled" or "Confirmed" or "Checked In" or "In Progress"),
            CancelledCount = appointments.Count(a => a.Status == "Cancelled")
        };
    }

    #endregion

    #region Customer & Therapist Views

    public async Task<List<AppointmentListResponse>> GetCustomerAppointmentsAsync(int customerId, bool includeCompleted = false)
    {
        var query = _context.Appointments
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.Service)
            .Include(a => a.ServiceItems).ThenInclude(si => si.Service)
            .Include(a => a.Therapist)
            .Include(a => a.Room)
            .Where(a => a.CustomerId == customerId);

        if (!includeCompleted)
            query = query.Where(a => a.Status != "Completed" && a.Status != "Cancelled" && a.Status != "No Show");

        return await query
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.StartTime)
            .Select(a => MapToListResponse(a))
            .ToListAsync();
    }

    public async Task<List<AppointmentListResponse>> GetTherapistAppointmentsAsync(int therapistId, DateTime? date = null)
    {
        var query = _context.Appointments
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.Service)
            .Include(a => a.ServiceItems).ThenInclude(si => si.Service)
            .Include(a => a.Therapist)
            .Include(a => a.Room)
            .Where(a => a.TherapistId == therapistId && a.Status != "Cancelled");

        if (date.HasValue)
            query = query.Where(a => a.AppointmentDate.Date == date.Value.Date);

        return await query
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.StartTime)
            .Select(a => MapToListResponse(a))
            .ToListAsync();
    }

    public async Task<List<AppointmentListResponse>> GetTodaysAppointmentsAsync()
    {
        var today = DateTime.Today;
        return await _context.Appointments
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.Service)
            .Include(a => a.ServiceItems).ThenInclude(si => si.Service)
            .Include(a => a.Therapist)
            .Include(a => a.Room)
            .Where(a => a.AppointmentDate.Date == today && a.Status != "Cancelled")
            .OrderBy(a => a.StartTime)
            .Select(a => MapToListResponse(a))
            .ToListAsync();
    }

    public async Task<List<AppointmentListResponse>> GetUpcomingAppointmentsAsync(int days = 7)
    {
        var today = DateTime.Today;
        var endDate = today.AddDays(days);

        return await _context.Appointments
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.Service)
            .Include(a => a.ServiceItems).ThenInclude(si => si.Service)
            .Include(a => a.Therapist)
            .Include(a => a.Room)
            .Where(a => a.AppointmentDate.Date >= today &&
                       a.AppointmentDate.Date <= endDate &&
                       a.Status != "Cancelled" && a.Status != "Completed" && a.Status != "No Show")
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.StartTime)
            .Select(a => MapToListResponse(a))
            .ToListAsync();
    }

    #endregion

    #region Dashboard & Statistics

    public async Task<AppointmentDashboardResponse> GetDashboardAsync(DateTime date)
    {
        var appointments = await _context.Appointments
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.Service)
            .Include(a => a.ServiceItems).ThenInclude(si => si.Service)
            .Include(a => a.Therapist)
            .Include(a => a.Room)
            .Where(a => a.AppointmentDate.Date == date.Date)
            .ToListAsync();

        var now = PhilippineTime.Now.TimeOfDay;
        var upcomingToday = appointments
            .Where(a => a.StartTime > now && a.Status is "Scheduled" or "Confirmed")
            .OrderBy(a => a.StartTime)
            .Take(10)
            .Select(a => MapToListResponse(a))
            .ToList();

        return new AppointmentDashboardResponse
        {
            Date = date.Date,
            TotalAppointments = appointments.Count,
            ScheduledCount = appointments.Count(a => a.Status == "Scheduled"),
            ConfirmedCount = appointments.Count(a => a.Status == "Confirmed"),
            CheckedInCount = appointments.Count(a => a.Status == "Checked In"),
            InProgressCount = appointments.Count(a => a.Status == "In Progress"),
            CompletedCount = appointments.Count(a => a.Status == "Completed"),
            CancelledCount = appointments.Count(a => a.Status == "Cancelled"),
            NoShowCount = appointments.Count(a => a.Status == "No Show"),
            UpcomingToday = upcomingToday
        };
    }

    public async Task<AppointmentStatsResponse> GetStatisticsAsync(DateTime startDate, DateTime endDate)
    {
        var appointments = await _context.Appointments
            .AsNoTracking()
            .Include(a => a.Service)
            .Include(a => a.ServiceItems).ThenInclude(si => si.Service)
            .Where(a => a.AppointmentDate.Date >= startDate.Date && a.AppointmentDate.Date <= endDate.Date)
            .ToListAsync();

        var totalCount = appointments.Count;
        var completedCount = appointments.Count(a => a.Status == "Completed");
        var cancelledCount = appointments.Count(a => a.Status == "Cancelled");
        var noShowCount = appointments.Count(a => a.Status == "No Show");

        return new AppointmentStatsResponse
        {
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            TotalAppointments = totalCount,
            CompletedCount = completedCount,
            CancelledCount = cancelledCount,
            NoShowCount = noShowCount,
            CompletionRate = totalCount > 0 ? Math.Round((decimal)completedCount / totalCount * 100, 2) : 0,
            CancellationRate = totalCount > 0 ? Math.Round((decimal)cancelledCount / totalCount * 100, 2) : 0,
            NoShowRate = totalCount > 0 ? Math.Round((decimal)noShowCount / totalCount * 100, 2) : 0,
            ByStatus = appointments.GroupBy(a => a.Status).ToDictionary(g => g.Key, g => g.Count()),
            ByBookingSource = appointments.GroupBy(a => a.BookingSource).ToDictionary(g => g.Key, g => g.Count()),
            ByService = appointments.GroupBy(a => a.Service.ServiceName).ToDictionary(g => g.Key, g => g.Count())
        };
    }

    #endregion

    #region Waitlist Management

    public async Task<WaitlistEntryResponse> AddToWaitlistAsync(AddToWaitlistRequest request)
    {
        var customer = await _context.Customers.FindAsync(request.CustomerId)
            ?? throw new InvalidOperationException($"Customer with ID {request.CustomerId} not found");

        var service = await _context.Services.FindAsync(request.ServiceId)
            ?? throw new InvalidOperationException($"Service with ID {request.ServiceId} not found");

        var waitlist = new Waitlist
        {
            CustomerId = request.CustomerId,
            ServiceId = request.ServiceId,
            PreferredTherapistId = request.PreferredTherapistId,
            PreferredDate = request.PreferredDate?.Date,
            PreferredTimeFrom = request.PreferredTimeFrom,
            PreferredTimeTo = request.PreferredTimeTo,
            Notes = request.Notes,
            Status = "Waiting",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Waitlists.Add(waitlist);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added customer {CustomerId} to waitlist for service {ServiceId}",
            request.CustomerId, request.ServiceId);

        return await MapToWaitlistResponseAsync(waitlist);
    }

    public async Task<List<WaitlistEntryResponse>> GetWaitlistAsync(int? serviceId = null)
    {
        var query = _context.Waitlists
            .AsNoTracking()
            .Include(w => w.Customer)
            .Include(w => w.Service)
            .Include(w => w.PreferredTherapist)
            .Where(w => w.Status == "Waiting");

        if (serviceId.HasValue)
            query = query.Where(w => w.ServiceId == serviceId.Value);

        var waitlistEntries = await query
            .OrderBy(w => w.CreatedAt)
            .ToListAsync();

        var responses = new List<WaitlistEntryResponse>();
        foreach (var entry in waitlistEntries)
        {
            responses.Add(await MapToWaitlistResponseAsync(entry));
        }
        return responses;
    }

    public async Task<WaitlistEntryResponse?> GetNextWaitlistEntryAsync(int serviceId, DateTime date, TimeSpan time, int? therapistId = null)
    {
        var query = _context.Waitlists
            .AsNoTracking()
            .Include(w => w.Customer)
            .Include(w => w.Service)
            .Include(w => w.PreferredTherapist)
            .Where(w => w.ServiceId == serviceId && w.Status == "Waiting");

        // Filter by preferred date if specified
        query = query.Where(w => !w.PreferredDate.HasValue || w.PreferredDate == date.Date);

        // Filter by preferred therapist if specified
        if (therapistId.HasValue)
            query = query.Where(w => !w.PreferredTherapistId.HasValue || w.PreferredTherapistId == therapistId);

        // Filter by preferred time range
        query = query.Where(w =>
            (!w.PreferredTimeFrom.HasValue || time >= w.PreferredTimeFrom.Value) &&
            (!w.PreferredTimeTo.HasValue || time <= w.PreferredTimeTo.Value));

        var entry = await query
            .OrderBy(w => w.CreatedAt)
            .FirstOrDefaultAsync();

        return entry == null ? null : await MapToWaitlistResponseAsync(entry);
    }

    public async Task<bool> RemoveFromWaitlistAsync(int waitlistId)
    {
        var waitlist = await _context.Waitlists.FindAsync(waitlistId);
        if (waitlist == null) return false;

        waitlist.Status = "Cancelled";
        waitlist.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<AppointmentResponse?> ConvertWaitlistToAppointmentAsync(int waitlistId, DateTime date, TimeSpan startTime, int? therapistId = null)
    {
        var waitlist = await _context.Waitlists.FindAsync(waitlistId)
            ?? throw new InvalidOperationException($"Waitlist entry with ID {waitlistId} not found");

        if (waitlist.Status != "Waiting")
            throw new InvalidOperationException($"Waitlist entry is not in 'Waiting' status");

        var request = new CreateAppointmentRequest
        {
            CustomerId = waitlist.CustomerId,
            ServiceId = waitlist.ServiceId,
            TherapistId = therapistId ?? waitlist.PreferredTherapistId,
            AppointmentDate = date,
            StartTime = startTime,
            BookingSource = "Waitlist"
        };

        var appointment = await CreateAppointmentAsync(request);

        waitlist.Status = "Booked";
        waitlist.ConvertedAppointmentId = appointment.AppointmentId;
        waitlist.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Converted waitlist {WaitlistId} to appointment {AppointmentId}",
            waitlistId, appointment.AppointmentId);

        return appointment;
    }

    #endregion

    #region Validation & Helpers

    public async Task<bool> IsSlotAvailableAsync(int serviceId, DateTime date, TimeSpan startTime, int? therapistId = null, int? excludeAppointmentId = null)
    {
        var service = await _context.Services.FindAsync(serviceId);
        if (service == null) return false;

        var endTime = startTime.Add(TimeSpan.FromMinutes(service.DurationMinutes));

        // Check business hours
        if (startTime < OpeningTime || endTime > ClosingTime)
            return false;

        var query = _context.Appointments
            .Where(a => a.AppointmentDate.Date == date.Date &&
                       a.Status != "Cancelled" && a.Status != "No Show");

        if (excludeAppointmentId.HasValue)
            query = query.Where(a => a.AppointmentId != excludeAppointmentId.Value);

        if (therapistId.HasValue)
        {
            // For today, verify therapist is clocked in and available
            if (date.Date == PhilippineTime.Today)
            {
                var todayRecord = await _context.AttendanceRecords
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.EmployeeId == therapistId.Value
                        && a.Date == PhilippineTime.Today);

                // Not clocked in, on break, or already clocked out = not available
                if (todayRecord == null || todayRecord.Status == "OnBreak" || todayRecord.ClockOut != null)
                    return false;
            }

            // Verify therapist is scheduled to work on this day and time
            if (!await IsTherapistScheduledAsync(therapistId.Value, date, startTime, endTime))
                return false;

            // Check if therapist has conflicting appointments
            var therapistConflict = await query
                .AnyAsync(a => a.TherapistId == therapistId.Value &&
                              ((startTime >= a.StartTime && startTime < a.EndTime) ||
                               (endTime > a.StartTime && endTime <= a.EndTime) ||
                               (startTime <= a.StartTime && endTime >= a.EndTime)));

            return !therapistConflict;
        }

        // If no specific therapist, check if any scheduled therapist is available
        var dayOfWeek = (int)date.DayOfWeek;

        // Get therapists scheduled for this day/time
        var scheduledTherapistIds = await _context.EmployeeShifts
            .AsNoTracking()
            .Where(s =>
                s.Employee.IsTherapist &&
                s.Employee.IsActive &&
                s.IsActive &&
                s.DayOfWeek == dayOfWeek &&
                s.EffectiveFrom <= date &&
                (s.EffectiveTo == null || s.EffectiveTo >= date) &&
                s.StartTime <= startTime &&
                s.EndTime >= endTime)
            .Select(s => s.EmployeeId)
            .Distinct()
            .ToListAsync();

        // If no schedule records exist at all, fall back to all active therapists
        if (!scheduledTherapistIds.Any())
        {
            var hasAnySchedules = await _context.EmployeeShifts.AsNoTracking().AnyAsync();
            if (!hasAnySchedules)
            {
                scheduledTherapistIds = await _context.Employees
                    .AsNoTracking()
                    .Where(e => e.IsTherapist && e.IsActive)
                    .Select(e => e.EmployeeId)
                    .ToListAsync();
            }
        }

        // Exclude therapists on approved leave
        var therapistsOnLeave = await _context.TimeOffRequests
            .AsNoTracking()
            .Where(t =>
                scheduledTherapistIds.Contains(t.EmployeeId) &&
                t.Status == "Approved" &&
                t.StartDate <= date &&
                t.EndDate >= date)
            .Select(t => t.EmployeeId)
            .ToListAsync();

        var availableTherapistIds = scheduledTherapistIds.Except(therapistsOnLeave).ToList();

        // Now check which of these are not busy
        var busyTherapists = await query
            .Where(a => (startTime >= a.StartTime && startTime < a.EndTime) ||
                       (endTime > a.StartTime && endTime <= a.EndTime) ||
                       (startTime <= a.StartTime && endTime >= a.EndTime))
            .Select(a => a.TherapistId)
            .Where(t => t.HasValue)
            .Select(t => t!.Value)
            .ToListAsync();

        return availableTherapistIds.Except(busyTherapists).Any();
    }

    public async Task<bool> HasConflictingAppointmentAsync(int customerId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeAppointmentId = null)
    {
        var query = _context.Appointments
            .Where(a => a.CustomerId == customerId &&
                       a.AppointmentDate.Date == date.Date &&
                       a.Status != "Cancelled" && a.Status != "No Show");

        if (excludeAppointmentId.HasValue)
            query = query.Where(a => a.AppointmentId != excludeAppointmentId.Value);

        return await query.AnyAsync(a =>
            (startTime >= a.StartTime && startTime < a.EndTime) ||
            (endTime > a.StartTime && endTime <= a.EndTime) ||
            (startTime <= a.StartTime && endTime >= a.EndTime));
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Returns a user-friendly reason why the slot is unavailable, or null if available.
    /// Provides specific reasons like schedule conflicts, therapist availability, etc.
    /// </summary>
    private async Task<string?> GetSlotUnavailabilityReasonAsync(int serviceId, DateTime date, TimeSpan startTime, int? therapistId = null, int? excludeAppointmentId = null)
    {
        var service = await _context.Services.FindAsync(serviceId);
        if (service == null) return "The selected service was not found.";

        var endTime = startTime.Add(TimeSpan.FromMinutes(service.DurationMinutes));
        var dayName = date.ToString("dddd");
        var timeFormatted = DateTime.Today.Add(startTime).ToString("h:mm tt");

        // Check business hours
        if (startTime < OpeningTime || endTime > ClosingTime)
            return $"The time {timeFormatted} is outside business hours (9:00 AM – 9:00 PM).";

        var query = _context.Appointments
            .Where(a => a.AppointmentDate.Date == date.Date &&
                       a.Status != "Cancelled" && a.Status != "No Show");

        if (excludeAppointmentId.HasValue)
            query = query.Where(a => a.AppointmentId != excludeAppointmentId.Value);

        if (therapistId.HasValue)
        {
            var therapist = await _context.Employees.FindAsync(therapistId.Value);
            var therapistName = therapist?.FullName ?? "Selected therapist";

            // For today, verify therapist is clocked in
            if (date.Date == PhilippineTime.Today)
            {
                var todayRecord = await _context.AttendanceRecords
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.EmployeeId == therapistId.Value
                        && a.Date == PhilippineTime.Today);

                if (todayRecord == null)
                    return $"{therapistName} has not clocked in today and cannot be assigned.";
                if (todayRecord.Status == "OnBreak")
                    return $"{therapistName} is currently on break.";
                if (todayRecord.ClockOut != null)
                    return $"{therapistName} has already clocked out for today.";
            }

            // Check if therapist has schedule records at all
            var hasAnySchedule = await _context.EmployeeShifts
                .AsNoTracking()
                .AnyAsync(s => s.EmployeeId == therapistId.Value && s.IsActive);

            if (hasAnySchedule)
            {
                // Check if scheduled on this day of week
                var dayOfWeek = (int)date.DayOfWeek;
                var hasShiftOnDay = await _context.EmployeeShifts
                    .AsNoTracking()
                    .AnyAsync(s =>
                        s.EmployeeId == therapistId.Value &&
                        s.IsActive &&
                        s.DayOfWeek == dayOfWeek &&
                        s.EffectiveFrom <= date &&
                        (s.EffectiveTo == null || s.EffectiveTo >= date));

                if (!hasShiftOnDay)
                {
                    // Get their actual scheduled days for helpful message
                    var scheduledDays = await _context.EmployeeShifts
                        .AsNoTracking()
                        .Where(s => s.EmployeeId == therapistId.Value && s.IsActive &&
                            s.EffectiveFrom <= date && (s.EffectiveTo == null || s.EffectiveTo >= date))
                        .Select(s => s.DayOfWeek)
                        .Distinct()
                        .ToListAsync();

                    var dayNames = scheduledDays.Order()
                        .Select(d => ((DayOfWeek)d).ToString())
                        .ToList();

                    return dayNames.Any()
                        ? $"{therapistName} is not scheduled to work on {dayName}s. Their work days are: {string.Join(", ", dayNames)}."
                        : $"{therapistName} has no active schedule configured.";
                }

                // Check if the time is within their shift hours
                var isWithinShift = await _context.EmployeeShifts
                    .AsNoTracking()
                    .AnyAsync(s =>
                        s.EmployeeId == therapistId.Value &&
                        s.IsActive &&
                        s.DayOfWeek == dayOfWeek &&
                        s.EffectiveFrom <= date &&
                        (s.EffectiveTo == null || s.EffectiveTo >= date) &&
                        s.StartTime <= startTime &&
                        s.EndTime >= endTime);

                if (!isWithinShift)
                {
                    var shift = await _context.EmployeeShifts
                        .AsNoTracking()
                        .Where(s => s.EmployeeId == therapistId.Value && s.IsActive &&
                            s.DayOfWeek == dayOfWeek && s.EffectiveFrom <= date &&
                            (s.EffectiveTo == null || s.EffectiveTo >= date))
                        .OrderByDescending(s => s.EffectiveFrom)
                        .FirstOrDefaultAsync();

                    if (shift != null)
                    {
                        var shiftStart = DateTime.Today.Add(shift.StartTime).ToString("h:mm tt");
                        var shiftEnd = DateTime.Today.Add(shift.EndTime).ToString("h:mm tt");
                        return $"{therapistName}'s shift on {dayName}s is {shiftStart} – {shiftEnd}. The selected time ({timeFormatted}) is outside their shift.";
                    }
                }
            }

            // Check for approved time-off
            var leaveRequest = await _context.TimeOffRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.EmployeeId == therapistId.Value &&
                    t.Status == "Approved" &&
                    t.StartDate <= date &&
                    t.EndDate >= date);

            if (leaveRequest != null)
                return $"{therapistName} is on approved leave on {date:MMM dd, yyyy}.";

            // Check for conflicting appointments
            var conflictingAppt = await query
                .Where(a => a.TherapistId == therapistId.Value &&
                           ((startTime >= a.StartTime && startTime < a.EndTime) ||
                            (endTime > a.StartTime && endTime <= a.EndTime) ||
                            (startTime <= a.StartTime && endTime >= a.EndTime)))
                .Select(a => new { a.AppointmentNumber, a.StartTime, a.EndTime })
                .FirstOrDefaultAsync();

            if (conflictingAppt != null)
            {
                var conflictStart = DateTime.Today.Add(conflictingAppt.StartTime).ToString("h:mm tt");
                var conflictEnd = DateTime.Today.Add(conflictingAppt.EndTime).ToString("h:mm tt");
                return $"{therapistName} already has appointment {conflictingAppt.AppointmentNumber} from {conflictStart} to {conflictEnd}.";
            }

            return null; // slot is available
        }

        // No specific therapist — check if any therapist is available
        var dayOfWeekNoTherapist = (int)date.DayOfWeek;

        var scheduledTherapistIds = await _context.EmployeeShifts
            .AsNoTracking()
            .Where(s =>
                s.Employee.IsTherapist &&
                s.Employee.IsActive &&
                s.IsActive &&
                s.DayOfWeek == dayOfWeekNoTherapist &&
                s.EffectiveFrom <= date &&
                (s.EffectiveTo == null || s.EffectiveTo >= date) &&
                s.StartTime <= startTime &&
                s.EndTime >= endTime)
            .Select(s => s.EmployeeId)
            .Distinct()
            .ToListAsync();

        if (!scheduledTherapistIds.Any())
        {
            var hasAnySchedules = await _context.EmployeeShifts.AsNoTracking().AnyAsync();
            if (!hasAnySchedules)
            {
                scheduledTherapistIds = await _context.Employees
                    .AsNoTracking()
                    .Where(e => e.IsTherapist && e.IsActive)
                    .Select(e => e.EmployeeId)
                    .ToListAsync();
            }
            else
            {
                return $"No therapists are scheduled to work on {dayName}s at {timeFormatted}. Please choose a different day or time.";
            }
        }

        var therapistsOnLeave = await _context.TimeOffRequests
            .AsNoTracking()
            .Where(t =>
                scheduledTherapistIds.Contains(t.EmployeeId) &&
                t.Status == "Approved" &&
                t.StartDate <= date &&
                t.EndDate >= date)
            .Select(t => t.EmployeeId)
            .ToListAsync();

        var availableTherapistIds = scheduledTherapistIds.Except(therapistsOnLeave).ToList();

        if (!availableTherapistIds.Any())
            return $"All scheduled therapists are on leave on {date:MMM dd, yyyy}. Please choose a different date.";

        var busyTherapists = await query
            .Where(a => (startTime >= a.StartTime && startTime < a.EndTime) ||
                       (endTime > a.StartTime && endTime <= a.EndTime) ||
                       (startTime <= a.StartTime && endTime >= a.EndTime))
            .Select(a => a.TherapistId)
            .Where(t => t.HasValue)
            .Select(t => t!.Value)
            .ToListAsync();

        if (!availableTherapistIds.Except(busyTherapists).Any())
            return $"All available therapists are fully booked at {timeFormatted} on {date:MMM dd, yyyy}. Please choose a different time.";

        return null; // slot is available
    }

    /// <summary>
    /// Checks if a therapist is scheduled to work on the given date/time,
    /// accounting for their weekly schedule, effective dates, and approved time-off.
    /// If no schedule records exist for the therapist at all, they are treated as available
    /// (allows booking when schedules haven't been configured yet).
    /// </summary>
    private async Task<bool> IsTherapistScheduledAsync(int therapistId, DateTime date, TimeSpan startTime, TimeSpan endTime)
    {
        var dayOfWeek = (int)date.DayOfWeek;

        // First check if any shift records exist at all for this therapist
        var hasAnySchedule = await _context.EmployeeShifts
            .AsNoTracking()
            .AnyAsync(s => s.EmployeeId == therapistId && s.IsActive);

        if (!hasAnySchedule)
        {
            // No schedule configured — treat therapist as available (don't block booking)
            // Still check for approved time-off below
            var isOnLeave = await _context.TimeOffRequests
                .AsNoTracking()
                .AnyAsync(t =>
                    t.EmployeeId == therapistId &&
                    t.Status == "Approved" &&
                    t.StartDate <= date &&
                    t.EndDate >= date);

            return !isOnLeave;
        }

        var isScheduled = await _context.EmployeeShifts
            .AsNoTracking()
            .AnyAsync(s =>
                s.EmployeeId == therapistId &&
                s.IsActive &&
                s.DayOfWeek == dayOfWeek &&
                s.EffectiveFrom <= date &&
                (s.EffectiveTo == null || s.EffectiveTo >= date) &&
                s.StartTime <= startTime &&
                s.EndTime >= endTime);

        if (!isScheduled)
            return false;

        // Check for approved time-off
        var hasApprovedLeave = await _context.TimeOffRequests
            .AsNoTracking()
            .AnyAsync(t =>
                t.EmployeeId == therapistId &&
                t.Status == "Approved" &&
                t.StartDate <= date &&
                t.EndDate >= date);

        return !hasApprovedLeave;
    }

    private async Task<Appointment> GetAppointmentEntityAsync(int appointmentId)
    {
        return await _context.Appointments.FindAsync(appointmentId)
            ?? throw new InvalidOperationException($"Appointment with ID {appointmentId} not found");
    }

    private async Task<string> GenerateAppointmentNumberAsync(DateTime date)
    {
        var prefix = date.ToString("yyyyMMdd");

        for (int attempt = 0; attempt < 5; attempt++)
        {
            var todayCount = await _context.Appointments
                .CountAsync(a => a.AppointmentNumber.StartsWith(prefix));

            var candidate = $"{prefix}-{(todayCount + 1):D4}";
            var exists = await _context.Appointments.AnyAsync(a => a.AppointmentNumber == candidate);
            if (!exists) return candidate;

            await Task.Delay(50 * (attempt + 1));
        }

        return $"{prefix}-{PhilippineTime.Now:HHmmssfff}";
    }

    private static bool DoTimesOverlap(TimeSpan start1, TimeSpan end1, TimeSpan start2, TimeSpan end2)
    {
        return start1 < end2 && start2 < end1;
    }

    private static List<AppointmentServiceItemResponse> MapServiceItems(Appointment appointment)
    {
        if (appointment.ServiceItems == null || !appointment.ServiceItems.Any())
        {
            // Backward compat: build a single-item list from the original ServiceId
            return new List<AppointmentServiceItemResponse>
            {
                new()
                {
                    AppointmentServiceItemId = 0,
                    ServiceId = appointment.ServiceId,
                    ServiceName = appointment.Service.ServiceName,
                    ServiceCode = appointment.Service.ServiceCode,
                    UnitPrice = appointment.Service.RegularPrice,
                    DurationMinutes = appointment.Service.DurationMinutes,
                    Quantity = 1,
                    AddedAt = appointment.CreatedAt
                }
            };
        }

        return appointment.ServiceItems.Select(si => new AppointmentServiceItemResponse
        {
            AppointmentServiceItemId = si.AppointmentServiceItemId,
            ServiceId = si.ServiceId,
            ServiceName = si.Service.ServiceName,
            ServiceCode = si.Service.ServiceCode,
            UnitPrice = si.UnitPrice,
            DurationMinutes = si.DurationMinutes,
            Quantity = si.Quantity,
            AddedAt = si.AddedAt
        }).ToList();
    }

    private static AppointmentResponse MapToAppointmentResponse(Appointment appointment)
    {
        return new AppointmentResponse
        {
            AppointmentId = appointment.AppointmentId,
            AppointmentNumber = appointment.AppointmentNumber,
            CustomerId = appointment.CustomerId,
            CustomerName = $"{appointment.Customer.FirstName} {appointment.Customer.LastName}",
            CustomerPhone = appointment.Customer.PhoneNumber,
            CustomerEmail = appointment.Customer.Email,
            MembershipType = appointment.Customer.MembershipType,
            ServiceId = appointment.ServiceId,
            ServiceName = appointment.Service.ServiceName,
            ServiceCode = appointment.Service.ServiceCode,
            DurationMinutes = appointment.Service.DurationMinutes,
            TherapistId = appointment.TherapistId,
            TherapistName = appointment.Therapist != null
                ? $"{appointment.Therapist.FirstName} {appointment.Therapist.LastName}"
                : null,
            RoomId = appointment.RoomId,
            RoomName = appointment.Room?.RoomName,
            AppointmentDate = appointment.AppointmentDate,
            StartTime = appointment.StartTime,
            EndTime = appointment.EndTime,
            Status = appointment.Status,
            BookingSource = appointment.BookingSource,
            CustomerNotes = appointment.CustomerNotes,
            TherapistNotes = appointment.TherapistNotes,
            CheckedInAt = appointment.CheckedInAt,
            ServiceStartedAt = appointment.ServiceStartedAt,
            ServiceCompletedAt = appointment.ServiceCompletedAt,
            CancelledAt = appointment.CancelledAt,
            CancellationReason = appointment.CancellationReason,
            CreatedAt = appointment.CreatedAt,
            UpdatedAt = appointment.UpdatedAt,
            ServiceItems = MapServiceItems(appointment)
        };
    }

    private static AppointmentListResponse MapToListResponse(Appointment appointment)
    {
        return new AppointmentListResponse
        {
            AppointmentId = appointment.AppointmentId,
            AppointmentNumber = appointment.AppointmentNumber,
            CustomerId = appointment.CustomerId,
            CustomerName = $"{appointment.Customer.FirstName} {appointment.Customer.LastName}",
            ServiceName = appointment.Service.ServiceName,
            TherapistName = appointment.Therapist != null
                ? $"{appointment.Therapist.FirstName} {appointment.Therapist.LastName}"
                : null,
            RoomId = appointment.RoomId,
            RoomName = appointment.Room?.RoomName,
            AppointmentDate = appointment.AppointmentDate,
            StartTime = appointment.StartTime,
            EndTime = appointment.EndTime,
            Status = appointment.Status,
            BookingSource = appointment.BookingSource,
            ServicePrice = appointment.Service.RegularPrice,
            DurationMinutes = appointment.Service.DurationMinutes,
            ServiceItems = MapServiceItems(appointment),
            IsArchived = appointment.IsArchived
        };
    }

    private async Task<WaitlistEntryResponse> MapToWaitlistResponseAsync(Waitlist waitlist)
    {
        var customer = waitlist.Customer ?? await _context.Customers.FindAsync(waitlist.CustomerId);
        var service = waitlist.Service ?? await _context.Services.FindAsync(waitlist.ServiceId);
        var therapist = waitlist.PreferredTherapist ??
            (waitlist.PreferredTherapistId.HasValue
                ? await _context.Employees.FindAsync(waitlist.PreferredTherapistId.Value)
                : null);

        return new WaitlistEntryResponse
        {
            WaitlistId = waitlist.WaitlistId,
            CustomerId = waitlist.CustomerId,
            CustomerName = customer != null ? $"{customer.FirstName} {customer.LastName}" : "Unknown",
            CustomerPhone = customer?.PhoneNumber,
            ServiceId = waitlist.ServiceId,
            ServiceName = service?.ServiceName ?? "Unknown",
            PreferredTherapistId = waitlist.PreferredTherapistId,
            PreferredTherapistName = therapist != null ? $"{therapist.FirstName} {therapist.LastName}" : null,
            PreferredDate = waitlist.PreferredDate,
            PreferredTimeFrom = waitlist.PreferredTimeFrom,
            PreferredTimeTo = waitlist.PreferredTimeTo,
            Status = waitlist.Status,
            Notes = waitlist.Notes,
            CreatedAt = waitlist.CreatedAt
        };
    }

    #endregion

    #region Room Management

    public async Task<List<RoomResponse>> GetActiveRoomsAsync()
    {
        return await _context.Rooms
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.RoomName)
            .Select(r => new RoomResponse
            {
                RoomId = r.RoomId,
                RoomName = r.RoomName,
                RoomCode = r.RoomCode,
                Description = r.Description,
                RoomType = r.RoomType,
                Capacity = r.Capacity,
                IsActive = r.IsActive
            })
            .ToListAsync();
    }

    #endregion
}
