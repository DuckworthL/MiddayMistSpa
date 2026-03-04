using Microsoft.EntityFrameworkCore;
using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.Core.Entities.Employee;
using MiddayMistSpa.Infrastructure.Data;

namespace MiddayMistSpa.API.Services;

public class ShiftService : IShiftService
{
    private readonly SpaDbContext _context;
    private readonly ILogger<ShiftService> _logger;
    private static readonly string[] DayNames = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
    private static readonly string[] ValidExceptionTypes = { "TimeOff", "SickLeave", "Emergency", "CustomHours" };

    public ShiftService(SpaDbContext context, ILogger<ShiftService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Employee Shifts

    public async Task<ShiftResponse> CreateShiftAsync(CreateShiftRequest request)
    {
        var employee = await _context.Employees.FindAsync(request.EmployeeId)
            ?? throw new InvalidOperationException($"Employee with ID {request.EmployeeId} not found");

        ValidateShiftTimes(request.StartTime, request.EndTime);
        ValidateDayOfWeek(request.DayOfWeek);

        // Check for overlapping active shift on the same day
        var existingShift = await _context.EmployeeShifts
            .AnyAsync(s => s.EmployeeId == request.EmployeeId
                        && s.DayOfWeek == request.DayOfWeek
                        && s.IsActive
                        && (s.EffectiveTo == null || s.EffectiveTo >= request.EffectiveFrom));

        if (existingShift)
        {
            throw new InvalidOperationException(
                $"Employee already has an active shift on {DayNames[request.DayOfWeek]}. " +
                "Deactivate or delete the existing shift first, or use bulk shift assignment.");
        }

        var shift = new EmployeeShift
        {
            EmployeeId = request.EmployeeId,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            IsRecurring = request.IsRecurring,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.EmployeeShifts.Add(shift);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created shift for employee {EmployeeId} on {Day}: {Start}-{End}",
            request.EmployeeId, DayNames[request.DayOfWeek], request.StartTime, request.EndTime);

        return MapToShiftResponse(shift, employee.FullName);
    }

    public async Task<List<ShiftResponse>> GetEmployeeShiftsAsync(int employeeId)
    {
        var employee = await _context.Employees.FindAsync(employeeId)
            ?? throw new InvalidOperationException($"Employee with ID {employeeId} not found");

        var shifts = await _context.EmployeeShifts
            .AsNoTracking()
            .Where(s => s.EmployeeId == employeeId && s.IsActive)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        return shifts.Select(s => MapToShiftResponse(s, employee.FullName)).ToList();
    }

    public async Task<WeeklyShiftScheduleResponse> GetWeeklyScheduleAsync(int employeeId)
    {
        var employee = await _context.Employees.FindAsync(employeeId)
            ?? throw new InvalidOperationException($"Employee with ID {employeeId} not found");

        var shifts = await _context.EmployeeShifts
            .AsNoTracking()
            .Where(s => s.EmployeeId == employeeId
                     && s.IsActive
                     && (s.EffectiveTo == null || s.EffectiveTo >= DateTime.UtcNow.Date))
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        var shiftResponses = shifts.Select(s => MapToShiftResponse(s, employee.FullName)).ToList();
        var totalHours = shifts.Sum(s => (decimal)(s.EndTime - s.StartTime).TotalHours);

        return new WeeklyShiftScheduleResponse
        {
            EmployeeId = employeeId,
            EmployeeName = employee.FullName,
            Shifts = shiftResponses,
            TotalWeeklyHours = Math.Round(totalHours, 2)
        };
    }

    public async Task<ShiftResponse> UpdateShiftAsync(int shiftId, UpdateShiftRequest request)
    {
        var shift = await _context.EmployeeShifts
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.ShiftId == shiftId)
            ?? throw new InvalidOperationException($"Shift with ID {shiftId} not found");

        ValidateShiftTimes(request.StartTime, request.EndTime);

        shift.StartTime = request.StartTime;
        shift.EndTime = request.EndTime;
        shift.IsRecurring = request.IsRecurring;
        shift.EffectiveTo = request.EffectiveTo;
        shift.IsActive = request.IsActive;
        shift.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated shift {ShiftId} for employee {EmployeeId}", shiftId, shift.EmployeeId);

        return MapToShiftResponse(shift, shift.Employee.FullName);
    }

    public async Task<bool> DeleteShiftAsync(int shiftId)
    {
        var shift = await _context.EmployeeShifts.FindAsync(shiftId);
        if (shift == null) return false;

        _context.EmployeeShifts.Remove(shift);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted shift {ShiftId} for employee {EmployeeId}", shiftId, shift.EmployeeId);
        return true;
    }

    public async Task<List<ShiftResponse>> SetBulkShiftsAsync(BulkShiftRequest request)
    {
        var employee = await _context.Employees.FindAsync(request.EmployeeId)
            ?? throw new InvalidOperationException($"Employee with ID {request.EmployeeId} not found");

        if (request.Shifts.Count == 0)
            throw new InvalidOperationException("At least one shift must be provided");

        // Validate all shifts
        foreach (var dayShift in request.Shifts)
        {
            ValidateDayOfWeek(dayShift.DayOfWeek);
            ValidateShiftTimes(dayShift.StartTime, dayShift.EndTime);
        }

        // Check for duplicate days
        var duplicateDays = request.Shifts.GroupBy(s => s.DayOfWeek).Where(g => g.Count() > 1).Select(g => DayNames[g.Key]);
        if (duplicateDays.Any())
        {
            throw new InvalidOperationException($"Duplicate days in bulk request: {string.Join(", ", duplicateDays)}");
        }

        // Deactivate all existing active shifts for this employee
        var existingShifts = await _context.EmployeeShifts
            .Where(s => s.EmployeeId == request.EmployeeId && s.IsActive)
            .ToListAsync();

        foreach (var existing in existingShifts)
        {
            existing.IsActive = false;
            existing.EffectiveTo = request.EffectiveFrom.AddDays(-1);
            existing.UpdatedAt = DateTime.UtcNow;
        }

        // Create new shifts
        var newShifts = request.Shifts.Select(s => new EmployeeShift
        {
            EmployeeId = request.EmployeeId,
            DayOfWeek = s.DayOfWeek,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            IsRecurring = true,
            EffectiveFrom = request.EffectiveFrom,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        _context.EmployeeShifts.AddRange(newShifts);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Set bulk shifts for employee {EmployeeId}: {Count} days",
            request.EmployeeId, newShifts.Count);

        return newShifts.Select(s => MapToShiftResponse(s, employee.FullName)).ToList();
    }

    #endregion

    #region Shift Exceptions

    public async Task<ShiftExceptionResponse> CreateShiftExceptionAsync(CreateShiftExceptionRequest request)
    {
        var employee = await _context.Employees.FindAsync(request.EmployeeId)
            ?? throw new InvalidOperationException($"Employee with ID {request.EmployeeId} not found");

        ValidateExceptionType(request.ExceptionType);

        // For CustomHours, times are required
        if (request.ExceptionType == "CustomHours" && (request.StartTime == null || request.EndTime == null))
        {
            throw new InvalidOperationException("StartTime and EndTime are required for CustomHours exception type");
        }

        if (request.StartTime.HasValue && request.EndTime.HasValue)
        {
            ValidateShiftTimes(request.StartTime.Value, request.EndTime.Value);
        }

        // Check for existing exception on the same date
        var existingException = await _context.ShiftExceptions
            .AnyAsync(e => e.EmployeeId == request.EmployeeId && e.ExceptionDate.Date == request.ExceptionDate.Date);

        if (existingException)
        {
            throw new InvalidOperationException(
                $"Employee already has a shift exception on {request.ExceptionDate:yyyy-MM-dd}. " +
                "Update or delete the existing exception first.");
        }

        var exception = new ShiftException
        {
            EmployeeId = request.EmployeeId,
            ExceptionDate = request.ExceptionDate.Date,
            ExceptionType = request.ExceptionType,
            StartTime = request.ExceptionType == "CustomHours" ? request.StartTime : null,
            EndTime = request.ExceptionType == "CustomHours" ? request.EndTime : null,
            Reason = request.Reason,
            CreatedAt = DateTime.UtcNow
        };

        _context.ShiftExceptions.Add(exception);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created shift exception for employee {EmployeeId} on {Date}: {Type}",
            request.EmployeeId, request.ExceptionDate.Date, request.ExceptionType);

        return MapToExceptionResponse(exception, employee.FullName);
    }

    public async Task<List<ShiftExceptionResponse>> GetEmployeeExceptionsAsync(int employeeId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var employee = await _context.Employees.FindAsync(employeeId)
            ?? throw new InvalidOperationException($"Employee with ID {employeeId} not found");

        var query = _context.ShiftExceptions
            .AsNoTracking()
            .Where(e => e.EmployeeId == employeeId);

        if (fromDate.HasValue)
            query = query.Where(e => e.ExceptionDate >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(e => e.ExceptionDate <= toDate.Value.Date);

        var exceptions = await query
            .OrderByDescending(e => e.ExceptionDate)
            .ToListAsync();

        return exceptions.Select(e => MapToExceptionResponse(e, employee.FullName)).ToList();
    }

    public async Task<List<ShiftExceptionResponse>> GetExceptionsByDateAsync(DateTime date)
    {
        var exceptions = await _context.ShiftExceptions
            .AsNoTracking()
            .Include(e => e.Employee)
            .Where(e => e.ExceptionDate.Date == date.Date)
            .OrderBy(e => e.Employee.LastName)
            .ThenBy(e => e.Employee.FirstName)
            .ToListAsync();

        return exceptions.Select(e => MapToExceptionResponse(e, e.Employee.FullName)).ToList();
    }

    public async Task<ShiftExceptionResponse> UpdateShiftExceptionAsync(int exceptionId, UpdateShiftExceptionRequest request)
    {
        var exception = await _context.ShiftExceptions
            .Include(e => e.Employee)
            .FirstOrDefaultAsync(e => e.ExceptionId == exceptionId)
            ?? throw new InvalidOperationException($"Shift exception with ID {exceptionId} not found");

        ValidateExceptionType(request.ExceptionType);

        if (request.ExceptionType == "CustomHours" && (request.StartTime == null || request.EndTime == null))
        {
            throw new InvalidOperationException("StartTime and EndTime are required for CustomHours exception type");
        }

        if (request.StartTime.HasValue && request.EndTime.HasValue)
        {
            ValidateShiftTimes(request.StartTime.Value, request.EndTime.Value);
        }

        exception.ExceptionType = request.ExceptionType;
        exception.StartTime = request.ExceptionType == "CustomHours" ? request.StartTime : null;
        exception.EndTime = request.ExceptionType == "CustomHours" ? request.EndTime : null;
        exception.Reason = request.Reason;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated shift exception {ExceptionId} for employee {EmployeeId}",
            exceptionId, exception.EmployeeId);

        return MapToExceptionResponse(exception, exception.Employee.FullName);
    }

    public async Task<bool> DeleteShiftExceptionAsync(int exceptionId)
    {
        var exception = await _context.ShiftExceptions.FindAsync(exceptionId);
        if (exception == null) return false;

        _context.ShiftExceptions.Remove(exception);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted shift exception {ExceptionId} for employee {EmployeeId}",
            exceptionId, exception.EmployeeId);
        return true;
    }

    #endregion

    #region Availability

    public async Task<EmployeeAvailabilityResponse> GetEmployeeAvailabilityAsync(int employeeId, DateTime date)
    {
        var employee = await _context.Employees.FindAsync(employeeId)
            ?? throw new InvalidOperationException($"Employee with ID {employeeId} not found");

        var dayOfWeek = (int)date.DayOfWeek;

        // Check for shift exceptions on this date
        var exception = await _context.ShiftExceptions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.ExceptionDate.Date == date.Date);

        if (exception != null)
        {
            return new EmployeeAvailabilityResponse
            {
                EmployeeId = employeeId,
                EmployeeName = employee.FullName,
                Date = date.Date,
                DayName = DayNames[dayOfWeek],
                IsAvailable = exception.ExceptionType == "CustomHours",
                Status = exception.ExceptionType,
                ShiftStart = exception.StartTime,
                ShiftEnd = exception.EndTime,
                ExceptionReason = exception.Reason
            };
        }

        // Check regular shift schedule
        var shift = await _context.EmployeeShifts
            .AsNoTracking()
            .Where(s => s.EmployeeId == employeeId
                     && s.DayOfWeek == dayOfWeek
                     && s.IsActive
                     && s.EffectiveFrom <= date.Date
                     && (s.EffectiveTo == null || s.EffectiveTo >= date.Date))
            .FirstOrDefaultAsync();

        if (shift != null)
        {
            return new EmployeeAvailabilityResponse
            {
                EmployeeId = employeeId,
                EmployeeName = employee.FullName,
                Date = date.Date,
                DayName = DayNames[dayOfWeek],
                IsAvailable = true,
                Status = "Working",
                ShiftStart = shift.StartTime,
                ShiftEnd = shift.EndTime
            };
        }

        // No shift scheduled = day off
        return new EmployeeAvailabilityResponse
        {
            EmployeeId = employeeId,
            EmployeeName = employee.FullName,
            Date = date.Date,
            DayName = DayNames[dayOfWeek],
            IsAvailable = false,
            Status = "DayOff"
        };
    }

    public async Task<List<EmployeeAvailabilityResponse>> GetAvailableStaffAsync(StaffAvailabilityRequest request)
    {
        var date = request.Date.Date;
        var dayOfWeek = (int)date.DayOfWeek;

        // Get all active employees (optionally only therapists)
        var employeesQuery = _context.Employees
            .AsNoTracking()
            .Where(e => e.IsActive && e.EmploymentStatus == "Active");

        if (request.TherapistsOnly)
            employeesQuery = employeesQuery.Where(e => e.IsTherapist);

        var employees = await employeesQuery.ToListAsync();

        // Get shifts for all these employees on this day
        var employeeIds = employees.Select(e => e.EmployeeId).ToList();

        var shifts = await _context.EmployeeShifts
            .AsNoTracking()
            .Where(s => employeeIds.Contains(s.EmployeeId)
                     && s.DayOfWeek == dayOfWeek
                     && s.IsActive
                     && s.EffectiveFrom <= date
                     && (s.EffectiveTo == null || s.EffectiveTo >= date))
            .ToListAsync();

        // Get exceptions for this date
        var exceptions = await _context.ShiftExceptions
            .AsNoTracking()
            .Where(e => employeeIds.Contains(e.EmployeeId) && e.ExceptionDate.Date == date)
            .ToListAsync();

        // Also check approved time off requests
        var onLeave = await _context.TimeOffRequests
            .AsNoTracking()
            .Where(t => employeeIds.Contains(t.EmployeeId)
                     && t.Status == "Approved"
                     && t.StartDate <= date
                     && t.EndDate >= date)
            .Select(t => t.EmployeeId)
            .ToListAsync();

        var results = new List<EmployeeAvailabilityResponse>();

        foreach (var employee in employees)
        {
            // Skip if on approved leave
            if (onLeave.Contains(employee.EmployeeId))
            {
                results.Add(new EmployeeAvailabilityResponse
                {
                    EmployeeId = employee.EmployeeId,
                    EmployeeName = employee.FullName,
                    Date = date,
                    DayName = DayNames[dayOfWeek],
                    IsAvailable = false,
                    Status = "TimeOff"
                });
                continue;
            }

            // Check for shift exception
            var exception = exceptions.FirstOrDefault(e => e.EmployeeId == employee.EmployeeId);
            if (exception != null)
            {
                var isAvailable = exception.ExceptionType == "CustomHours";

                // If custom hours and time filter is specified, check overlap
                if (isAvailable && request.StartTime.HasValue && request.EndTime.HasValue
                    && exception.StartTime.HasValue && exception.EndTime.HasValue)
                {
                    isAvailable = exception.StartTime.Value <= request.StartTime.Value
                               && exception.EndTime.Value >= request.EndTime.Value;
                }

                results.Add(new EmployeeAvailabilityResponse
                {
                    EmployeeId = employee.EmployeeId,
                    EmployeeName = employee.FullName,
                    Date = date,
                    DayName = DayNames[dayOfWeek],
                    IsAvailable = isAvailable,
                    Status = exception.ExceptionType,
                    ShiftStart = exception.StartTime,
                    ShiftEnd = exception.EndTime,
                    ExceptionReason = exception.Reason
                });
                continue;
            }

            // Check regular shift
            var shift = shifts.FirstOrDefault(s => s.EmployeeId == employee.EmployeeId);
            if (shift != null)
            {
                var isAvailable = true;

                // If time filter specified, check that shift covers the requested time
                if (request.StartTime.HasValue && request.EndTime.HasValue)
                {
                    isAvailable = shift.StartTime <= request.StartTime.Value
                               && shift.EndTime >= request.EndTime.Value;
                }

                results.Add(new EmployeeAvailabilityResponse
                {
                    EmployeeId = employee.EmployeeId,
                    EmployeeName = employee.FullName,
                    Date = date,
                    DayName = DayNames[dayOfWeek],
                    IsAvailable = isAvailable,
                    Status = "Working",
                    ShiftStart = shift.StartTime,
                    ShiftEnd = shift.EndTime
                });
                continue;
            }

            // No shift = day off
            results.Add(new EmployeeAvailabilityResponse
            {
                EmployeeId = employee.EmployeeId,
                EmployeeName = employee.FullName,
                Date = date,
                DayName = DayNames[dayOfWeek],
                IsAvailable = false,
                Status = "DayOff"
            });
        }

        return results.OrderByDescending(r => r.IsAvailable)
                       .ThenBy(r => r.EmployeeName)
                       .ToList();
    }

    #endregion

    #region Private Helpers

    private static ShiftResponse MapToShiftResponse(EmployeeShift shift, string employeeName) => new()
    {
        ShiftId = shift.ShiftId,
        EmployeeId = shift.EmployeeId,
        EmployeeName = employeeName,
        DayOfWeek = shift.DayOfWeek,
        DayName = DayNames[shift.DayOfWeek],
        StartTime = shift.StartTime,
        EndTime = shift.EndTime,
        Duration = $"{shift.Duration.Hours}h {shift.Duration.Minutes}m",
        IsRecurring = shift.IsRecurring,
        EffectiveFrom = shift.EffectiveFrom,
        EffectiveTo = shift.EffectiveTo,
        IsActive = shift.IsActive,
        CreatedAt = shift.CreatedAt,
        UpdatedAt = shift.UpdatedAt
    };

    private static ShiftExceptionResponse MapToExceptionResponse(ShiftException exception, string employeeName) => new()
    {
        ExceptionId = exception.ExceptionId,
        EmployeeId = exception.EmployeeId,
        EmployeeName = employeeName,
        ExceptionDate = exception.ExceptionDate,
        ExceptionType = exception.ExceptionType,
        StartTime = exception.StartTime,
        EndTime = exception.EndTime,
        Reason = exception.Reason,
        IsFullDayOff = exception.IsFullDayOff,
        CreatedAt = exception.CreatedAt
    };

    private static void ValidateShiftTimes(TimeSpan startTime, TimeSpan endTime)
    {
        if (startTime >= endTime)
            throw new InvalidOperationException("Start time must be before end time");

        if (startTime < TimeSpan.Zero || endTime > new TimeSpan(23, 59, 59))
            throw new InvalidOperationException("Shift times must be between 00:00 and 23:59");
    }

    private static void ValidateDayOfWeek(int dayOfWeek)
    {
        if (dayOfWeek < 0 || dayOfWeek > 6)
            throw new InvalidOperationException("DayOfWeek must be between 0 (Sunday) and 6 (Saturday)");
    }

    private static void ValidateExceptionType(string exceptionType)
    {
        if (!ValidExceptionTypes.Contains(exceptionType))
        {
            throw new InvalidOperationException(
                $"Invalid exception type '{exceptionType}'. Valid types: {string.Join(", ", ValidExceptionTypes)}");
        }
    }

    #endregion
}
