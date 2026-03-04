using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.Core.Entities.Employee;
using MiddayMistSpa.Core.Entities.Identity;
using MiddayMistSpa.Infrastructure.Data;

namespace MiddayMistSpa.API.Services;

public class EmployeeService : IEmployeeService
{
    private readonly SpaDbContext _context;
    private readonly ILogger<EmployeeService> _logger;
    private static readonly string[] DayNames = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

    public EmployeeService(SpaDbContext context, ILogger<EmployeeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Employee CRUD

    public async Task<EmployeeResponse> CreateEmployeeAsync(CreateEmployeeRequest request)
    {
        // Generate employee code
        var lastEmployee = await _context.Employees
            .OrderByDescending(e => e.EmployeeId)
            .FirstOrDefaultAsync();
        var nextNumber = (lastEmployee?.EmployeeId ?? 0) + 1;
        var employeeCode = $"EMP-{nextNumber:D5}";

        var employee = new Employee
        {
            EmployeeCode = employeeCode,
            FirstName = request.FirstName,
            LastName = request.LastName,
            MiddleName = request.MiddleName,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            CivilStatus = request.CivilStatus,
            Address = request.Address,
            City = request.City,
            Province = request.Province,
            PostalCode = request.PostalCode,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            EmergencyContactName = request.EmergencyContactName,
            EmergencyContactPhone = request.EmergencyContactPhone,
            SSSNumber = request.SSSNumber,
            PhilHealthNumber = request.PhilHealthNumber,
            PagIBIGNumber = request.PagIBIGNumber,
            TINNumber = request.TINNumber,
            Position = request.Position,
            Department = request.Department,
            HireDate = request.HireDate,
            EmploymentType = request.EmploymentType,
            EmploymentStatus = "Active",
            DailyRate = request.DailyRate,
            MonthlyBasicSalary = request.MonthlyBasicSalary,
            PayrollType = request.PayrollType,
            IsTherapist = request.IsTherapist,
            Specialization = request.Specialization,
            LicenseNumber = request.LicenseNumber,
            LicenseExpiryDate = request.LicenseExpiryDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Create user account if requested
        if (request.CreateUserAccount && !string.IsNullOrEmpty(request.Email) && request.RoleId.HasValue)
        {
            var username = request.Email.Split('@')[0].ToLower();
            var tempPassword = GenerateTemporaryPassword();

            var user = new User
            {
                Username = username,
                Email = request.Email,
                EmailConfirmed = true,
                PasswordHash = HashPassword(tempPassword),
                SecurityStamp = Guid.NewGuid().ToString(),
                RoleId = request.RoleId.Value,
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsActive = true,
                LockoutEnabled = true,
                MustChangePassword = true,
                PasswordExpiryDate = DateTime.UtcNow.AddDays(90),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            employee.UserId = user.UserId;

            _logger.LogInformation("Created user account for employee {EmployeeCode} with temporary password", employeeCode);
        }

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        // Initialize leave balance for current year
        await InitializeLeaveBalanceAsync(employee.EmployeeId, DateTime.UtcNow.Year);

        _logger.LogInformation("Created employee {EmployeeCode}: {FullName}", employee.EmployeeCode, employee.FullName);

        return MapToResponse(employee);
    }

    public async Task<EmployeeResponse?> GetEmployeeByIdAsync(int employeeId)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

        return employee == null ? null : MapToResponse(employee);
    }

    public async Task<EmployeeResponse?> GetEmployeeByCodeAsync(string employeeCode)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode);

        return employee == null ? null : MapToResponse(employee);
    }

    public async Task<PagedResponse<EmployeeListResponse>> GetEmployeesAsync(PagedRequest request)
    {
        var query = _context.Employees.AsNoTracking();

        // Active filter
        if (request.ActiveOnly.HasValue)
        {
            query = query.Where(e => e.IsActive == request.ActiveOnly.Value);
        }

        // Department filter
        if (!string.IsNullOrWhiteSpace(request.Department))
        {
            query = query.Where(e => e.Department == request.Department);
        }

        // Search
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(e =>
                e.FirstName.ToLower().Contains(term) ||
                e.LastName.ToLower().Contains(term) ||
                e.EmployeeCode.ToLower().Contains(term) ||
                e.Position.ToLower().Contains(term) ||
                (e.Department != null && e.Department.ToLower().Contains(term)));
        }

        // Sort
        query = request.SortBy?.ToLower() switch
        {
            "name" => request.SortDescending
                ? query.OrderByDescending(e => e.LastName).ThenByDescending(e => e.FirstName)
                : query.OrderBy(e => e.LastName).ThenBy(e => e.FirstName),
            "position" => request.SortDescending
                ? query.OrderByDescending(e => e.Position)
                : query.OrderBy(e => e.Position),
            "hiredate" => request.SortDescending
                ? query.OrderByDescending(e => e.HireDate)
                : query.OrderBy(e => e.HireDate),
            _ => query.OrderBy(e => e.EmployeeCode)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new EmployeeListResponse
            {
                EmployeeId = e.EmployeeId,
                EmployeeCode = e.EmployeeCode,
                FullName = e.FullName,
                Position = e.Position,
                Department = e.Department,
                PhoneNumber = e.PhoneNumber,
                EmploymentStatus = e.EmploymentStatus,
                IsTherapist = e.IsTherapist,
                Specialization = e.Specialization,
                IsActive = e.IsActive
            })
            .ToListAsync();

        return new PagedResponse<EmployeeListResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<List<EmployeeListResponse>> GetActiveEmployeesAsync()
    {
        return await _context.Employees
            .AsNoTracking()
            .Where(e => e.IsActive && e.EmploymentStatus == "Active")
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Select(e => new EmployeeListResponse
            {
                EmployeeId = e.EmployeeId,
                EmployeeCode = e.EmployeeCode,
                FullName = e.FullName,
                Position = e.Position,
                Department = e.Department,
                PhoneNumber = e.PhoneNumber,
                EmploymentStatus = e.EmploymentStatus,
                IsTherapist = e.IsTherapist,
                Specialization = e.Specialization,
                IsActive = e.IsActive
            })
            .ToListAsync();
    }

    public async Task<List<EmployeeListResponse>> GetTherapistsAsync()
    {
        return await _context.Employees
            .AsNoTracking()
            .Where(e => e.IsActive && e.IsTherapist && e.EmploymentStatus == "Active")
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Select(e => new EmployeeListResponse
            {
                EmployeeId = e.EmployeeId,
                EmployeeCode = e.EmployeeCode,
                FullName = e.FullName,
                Position = e.Position,
                Department = e.Department,
                PhoneNumber = e.PhoneNumber,
                EmploymentStatus = e.EmploymentStatus,
                IsTherapist = e.IsTherapist,
                Specialization = e.Specialization,
                IsActive = e.IsActive
            })
            .ToListAsync();
    }

    public async Task<EmployeeResponse> UpdateEmployeeAsync(int employeeId, UpdateEmployeeRequest request)
    {
        var employee = await _context.Employees.FindAsync(employeeId)
            ?? throw new InvalidOperationException($"Employee with ID {employeeId} not found");

        employee.FirstName = request.FirstName;
        employee.LastName = request.LastName;
        employee.MiddleName = request.MiddleName;
        employee.DateOfBirth = request.DateOfBirth;
        employee.Gender = request.Gender;
        employee.CivilStatus = request.CivilStatus;
        employee.Address = request.Address;
        employee.City = request.City;
        employee.Province = request.Province;
        employee.PostalCode = request.PostalCode;
        employee.PhoneNumber = request.PhoneNumber;
        employee.Email = request.Email;
        employee.EmergencyContactName = request.EmergencyContactName;
        employee.EmergencyContactPhone = request.EmergencyContactPhone;
        employee.SSSNumber = request.SSSNumber;
        employee.PhilHealthNumber = request.PhilHealthNumber;
        employee.PagIBIGNumber = request.PagIBIGNumber;
        employee.TINNumber = request.TINNumber;
        employee.Position = request.Position;
        employee.Department = request.Department;
        employee.EmploymentType = request.EmploymentType;
        employee.EmploymentStatus = request.EmploymentStatus;
        employee.DailyRate = request.DailyRate;
        employee.MonthlyBasicSalary = request.MonthlyBasicSalary;
        employee.PayrollType = request.PayrollType;
        employee.IsTherapist = request.IsTherapist;
        employee.Specialization = request.Specialization;
        employee.LicenseNumber = request.LicenseNumber;
        employee.LicenseExpiryDate = request.LicenseExpiryDate;
        employee.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated employee {EmployeeCode}", employee.EmployeeCode);

        return MapToResponse(employee);
    }

    public async Task<bool> DeactivateEmployeeAsync(int employeeId)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null) return false;

        employee.IsActive = false;
        employee.EmploymentStatus = "Resigned";
        employee.UpdatedAt = DateTime.UtcNow;

        // Deactivate user account if exists
        if (employee.UserId.HasValue)
        {
            var user = await _context.Users.FindAsync(employee.UserId.Value);
            if (user != null)
            {
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Deactivated employee {EmployeeCode}", employee.EmployeeCode);
        return true;
    }

    public async Task<bool> ReactivateEmployeeAsync(int employeeId)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null) return false;

        employee.IsActive = true;
        employee.EmploymentStatus = "Active";
        employee.UpdatedAt = DateTime.UtcNow;

        // Reactivate user account if exists
        if (employee.UserId.HasValue)
        {
            var user = await _context.Users.FindAsync(employee.UserId.Value);
            if (user != null)
            {
                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Reactivated employee {EmployeeCode}", employee.EmployeeCode);
        return true;
    }

    #endregion

    #region Schedules

    public async Task<ScheduleResponse> CreateScheduleAsync(CreateScheduleRequest request)
    {
        var employee = await _context.Employees.FindAsync(request.EmployeeId)
            ?? throw new InvalidOperationException($"Employee with ID {request.EmployeeId} not found");

        // End any existing schedules for this day
        var existingSchedules = await _context.EmployeeSchedules
            .Where(s => s.EmployeeId == request.EmployeeId &&
                        s.DayOfWeek == request.DayOfWeek &&
                        s.EndDate == null)
            .ToListAsync();

        foreach (var existing in existingSchedules)
        {
            existing.EndDate = request.EffectiveDate.AddDays(-1);
            existing.UpdatedAt = DateTime.UtcNow;
        }

        var schedule = new EmployeeSchedule
        {
            EmployeeId = request.EmployeeId,
            DayOfWeek = request.DayOfWeek,
            ShiftStartTime = request.ShiftStartTime,
            ShiftEndTime = request.ShiftEndTime,
            BreakStartTime = request.BreakStartTime,
            BreakEndTime = request.BreakEndTime,
            IsRestDay = request.IsRestDay,
            EffectiveDate = request.EffectiveDate,
            EndDate = request.EndDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.EmployeeSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        return MapToScheduleResponse(schedule, employee.FullName);
    }

    public async Task<List<ScheduleResponse>> GetEmployeeSchedulesAsync(int employeeId)
    {
        var employee = await _context.Employees.FindAsync(employeeId)
            ?? throw new InvalidOperationException($"Employee with ID {employeeId} not found");

        var schedules = await _context.EmployeeSchedules
            .AsNoTracking()
            .Where(s => s.EmployeeId == employeeId && s.EndDate == null)
            .OrderBy(s => s.DayOfWeek)
            .ToListAsync();

        return schedules.Select(s => MapToScheduleResponse(s, employee.FullName)).ToList();
    }

    public async Task<ScheduleResponse> UpdateScheduleAsync(int scheduleId, UpdateScheduleRequest request)
    {
        var schedule = await _context.EmployeeSchedules
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId)
            ?? throw new InvalidOperationException($"Schedule with ID {scheduleId} not found");

        schedule.ShiftStartTime = request.ShiftStartTime;
        schedule.ShiftEndTime = request.ShiftEndTime;
        schedule.BreakStartTime = request.BreakStartTime;
        schedule.BreakEndTime = request.BreakEndTime;
        schedule.IsRestDay = request.IsRestDay;
        schedule.EndDate = request.EndDate;
        schedule.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToScheduleResponse(schedule, schedule.Employee.FullName);
    }

    public async Task<bool> DeleteScheduleAsync(int scheduleId)
    {
        var schedule = await _context.EmployeeSchedules.FindAsync(scheduleId);
        if (schedule == null) return false;

        _context.EmployeeSchedules.Remove(schedule);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ScheduleResponse>> SetBulkScheduleAsync(BulkScheduleRequest request)
    {
        var employee = await _context.Employees.FindAsync(request.EmployeeId)
            ?? throw new InvalidOperationException($"Employee with ID {request.EmployeeId} not found");

        // End all existing schedules
        var existingSchedules = await _context.EmployeeSchedules
            .Where(s => s.EmployeeId == request.EmployeeId && s.EndDate == null)
            .ToListAsync();

        foreach (var existing in existingSchedules)
        {
            existing.EndDate = request.EffectiveDate.AddDays(-1);
            existing.UpdatedAt = DateTime.UtcNow;
        }

        var newSchedules = request.Schedules.Select(s => new EmployeeSchedule
        {
            EmployeeId = request.EmployeeId,
            DayOfWeek = s.DayOfWeek,
            ShiftStartTime = s.ShiftStartTime,
            ShiftEndTime = s.ShiftEndTime,
            BreakStartTime = s.BreakStartTime,
            BreakEndTime = s.BreakEndTime,
            IsRestDay = s.IsRestDay,
            EffectiveDate = request.EffectiveDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        _context.EmployeeSchedules.AddRange(newSchedules);
        await _context.SaveChangesAsync();

        return newSchedules.Select(s => MapToScheduleResponse(s, employee.FullName)).ToList();
    }

    public async Task<List<EmployeeListResponse>> GetAvailableTherapistsAsync(DateTime date, TimeSpan startTime, TimeSpan endTime)
    {
        var dayOfWeek = (int)date.DayOfWeek;

        // Get therapists scheduled to work on this day
        var availableTherapistIds = await _context.EmployeeSchedules
            .AsNoTracking()
            .Where(s =>
                s.Employee.IsTherapist &&
                s.Employee.IsActive &&
                s.Employee.EmploymentStatus == "Active" &&
                s.DayOfWeek == dayOfWeek &&
                !s.IsRestDay &&
                s.EffectiveDate <= date &&
                (s.EndDate == null || s.EndDate >= date) &&
                s.ShiftStartTime <= startTime &&
                s.ShiftEndTime >= endTime)
            .Select(s => s.EmployeeId)
            .Distinct()
            .ToListAsync();

        // Exclude therapists who have approved time off
        var therapistsOnLeave = await _context.TimeOffRequests
            .AsNoTracking()
            .Where(t =>
                availableTherapistIds.Contains(t.EmployeeId) &&
                t.Status == "Approved" &&
                t.StartDate <= date &&
                t.EndDate >= date)
            .Select(t => t.EmployeeId)
            .ToListAsync();

        var finalTherapistIds = availableTherapistIds.Except(therapistsOnLeave).ToList();

        return await _context.Employees
            .AsNoTracking()
            .Where(e => finalTherapistIds.Contains(e.EmployeeId))
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Select(e => new EmployeeListResponse
            {
                EmployeeId = e.EmployeeId,
                EmployeeCode = e.EmployeeCode,
                FullName = e.FullName,
                Position = e.Position,
                Department = e.Department,
                PhoneNumber = e.PhoneNumber,
                EmploymentStatus = e.EmploymentStatus,
                IsTherapist = e.IsTherapist,
                Specialization = e.Specialization,
                IsActive = e.IsActive
            })
            .ToListAsync();
    }

    #endregion

    #region Time Off Requests

    public async Task<TimeOffResponse> CreateTimeOffRequestAsync(CreateTimeOffRequest request)
    {
        var employee = await _context.Employees.FindAsync(request.EmployeeId)
            ?? throw new InvalidOperationException($"Employee with ID {request.EmployeeId} not found");

        // Calculate total days (excluding Sundays for Philippine business rules)
        var totalDays = CalculateWorkingDays(request.StartDate, request.EndDate);

        // Check leave balance if SIL or Sick Leave
        if (request.LeaveType == "SIL" || request.LeaveType == "Sick Leave")
        {
            var balance = await GetOrCreateLeaveBalanceAsync(request.EmployeeId, request.StartDate.Year);
            var available = request.LeaveType == "SIL" ? balance.SILRemaining : balance.SickLeaveRemaining;

            if (totalDays > available)
            {
                throw new InvalidOperationException(
                    $"Insufficient {request.LeaveType} balance. Available: {available} days, Requested: {totalDays} days");
            }
        }

        var timeOff = new TimeOffRequest
        {
            EmployeeId = request.EmployeeId,
            LeaveType = request.LeaveType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalDays = totalDays,
            Reason = request.Reason,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TimeOffRequests.Add(timeOff);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created time off request for employee {EmployeeCode}: {LeaveType} from {Start} to {End}",
            employee.EmployeeCode, request.LeaveType, request.StartDate, request.EndDate);

        return MapToTimeOffResponse(timeOff, employee.FullName, null);
    }

    public async Task<List<TimeOffResponse>> GetEmployeeTimeOffRequestsAsync(int employeeId)
    {
        var employee = await _context.Employees.FindAsync(employeeId)
            ?? throw new InvalidOperationException($"Employee with ID {employeeId} not found");

        var requests = await _context.TimeOffRequests
            .AsNoTracking()
            .Include(t => t.ApprovedByUser)
            .Where(t => t.EmployeeId == employeeId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return requests.Select(t => MapToTimeOffResponse(t, employee.FullName,
            t.ApprovedByUser != null ? $"{t.ApprovedByUser.FirstName} {t.ApprovedByUser.LastName}" : null)).ToList();
    }

    public async Task<PagedResponse<TimeOffResponse>> GetPendingTimeOffRequestsAsync(PagedRequest request)
    {
        var query = _context.TimeOffRequests
            .AsNoTracking()
            .Include(t => t.Employee)
            .Where(t => t.Status == "Pending");

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(t => t.StartDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TimeOffResponse
            {
                TimeOffRequestId = t.TimeOffRequestId,
                EmployeeId = t.EmployeeId,
                EmployeeName = t.Employee.FullName,
                LeaveType = t.LeaveType,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                TotalDays = t.TotalDays,
                Reason = t.Reason,
                Status = t.Status,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return new PagedResponse<TimeOffResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<TimeOffResponse> ApproveOrRejectTimeOffAsync(int timeOffRequestId, ApproveTimeOffRequest request, int approvedByUserId)
    {
        var timeOff = await _context.TimeOffRequests
            .Include(t => t.Employee)
            .FirstOrDefaultAsync(t => t.TimeOffRequestId == timeOffRequestId)
            ?? throw new InvalidOperationException($"Time off request with ID {timeOffRequestId} not found");

        if (timeOff.Status != "Pending")
            throw new InvalidOperationException("Only pending requests can be approved or rejected");

        var approver = await _context.Users.FindAsync(approvedByUserId);

        if (request.Approved)
        {
            timeOff.Status = "Approved";
            timeOff.ApprovedBy = approvedByUserId;
            timeOff.ApprovedAt = DateTime.UtcNow;

            // Deduct from leave balance
            if (timeOff.LeaveType == "SIL" || timeOff.LeaveType == "Sick Leave")
            {
                var balance = await GetOrCreateLeaveBalanceAsync(timeOff.EmployeeId, timeOff.StartDate.Year);
                if (timeOff.LeaveType == "SIL")
                    balance.SILUsed += timeOff.TotalDays;
                else
                    balance.SickLeaveUsed += timeOff.TotalDays;
                balance.UpdatedAt = DateTime.UtcNow;
            }

            _logger.LogInformation("Approved time off request {Id} for employee {EmployeeCode}",
                timeOffRequestId, timeOff.Employee.EmployeeCode);
        }
        else
        {
            timeOff.Status = "Rejected";
            timeOff.ApprovedBy = approvedByUserId;
            timeOff.ApprovedAt = DateTime.UtcNow;
            timeOff.RejectionReason = request.RejectionReason;

            _logger.LogInformation("Rejected time off request {Id} for employee {EmployeeCode}: {Reason}",
                timeOffRequestId, timeOff.Employee.EmployeeCode, request.RejectionReason);
        }

        timeOff.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToTimeOffResponse(timeOff, timeOff.Employee.FullName,
            approver != null ? $"{approver.FirstName} {approver.LastName}" : null);
    }

    public async Task<bool> CancelTimeOffRequestAsync(int timeOffRequestId)
    {
        var timeOff = await _context.TimeOffRequests.FindAsync(timeOffRequestId);
        if (timeOff == null) return false;

        if (timeOff.Status == "Approved")
        {
            // Restore leave balance
            if (timeOff.LeaveType == "SIL" || timeOff.LeaveType == "Sick Leave")
            {
                var balance = await GetOrCreateLeaveBalanceAsync(timeOff.EmployeeId, timeOff.StartDate.Year);
                if (timeOff.LeaveType == "SIL")
                    balance.SILUsed -= timeOff.TotalDays;
                else
                    balance.SickLeaveUsed -= timeOff.TotalDays;
                balance.UpdatedAt = DateTime.UtcNow;
            }
        }

        _context.TimeOffRequests.Remove(timeOff);
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Leave Balances

    public async Task<LeaveBalanceResponse?> GetEmployeeLeaveBalanceAsync(int employeeId, int year)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null) return null;

        var balance = await _context.EmployeeLeaveBalances
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.EmployeeId == employeeId && b.Year == year);

        if (balance == null) return null;

        return MapToLeaveBalanceResponse(balance, employee.FullName);
    }

    public async Task<LeaveBalanceResponse> InitializeLeaveBalanceAsync(int employeeId, int year)
    {
        var employee = await _context.Employees.FindAsync(employeeId)
            ?? throw new InvalidOperationException($"Employee with ID {employeeId} not found");

        var existing = await _context.EmployeeLeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == employeeId && b.Year == year);

        if (existing != null)
            return MapToLeaveBalanceResponse(existing, employee.FullName);

        var balance = new EmployeeLeaveBalance
        {
            EmployeeId = employeeId,
            Year = year,
            SILDays = 5.0m, // Philippine Labor Code mandates 5 days SIL
            SILUsed = 0,
            SickLeaveDays = 0, // Company-specific benefit
            SickLeaveUsed = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.EmployeeLeaveBalances.Add(balance);
        await _context.SaveChangesAsync();

        return MapToLeaveBalanceResponse(balance, employee.FullName);
    }

    public async Task<LeaveBalanceResponse> UpdateLeaveBalanceAsync(int employeeId, int year, UpdateLeaveBalanceRequest request)
    {
        var employee = await _context.Employees.FindAsync(employeeId)
            ?? throw new InvalidOperationException($"Employee with ID {employeeId} not found");

        var balance = await _context.EmployeeLeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == employeeId && b.Year == year)
            ?? throw new InvalidOperationException($"Leave balance not found for employee {employeeId} year {year}");

        balance.SILDays = request.SILDays;
        balance.SickLeaveDays = request.SickLeaveDays;
        balance.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToLeaveBalanceResponse(balance, employee.FullName);
    }

    #endregion

    #region Advances/Loans

    public async Task<AdvanceResponse> CreateAdvanceAsync(CreateAdvanceRequest request, int approvedByUserId)
    {
        var employee = await _context.Employees.FindAsync(request.EmployeeId)
            ?? throw new InvalidOperationException($"Employee with ID {request.EmployeeId} not found");

        var approver = await _context.Users.FindAsync(approvedByUserId);

        var advance = new EmployeeAdvance
        {
            EmployeeId = request.EmployeeId,
            AdvanceType = request.AdvanceType,
            Amount = request.Amount,
            Balance = request.Amount,
            MonthlyDeduction = request.MonthlyDeduction,
            StartDate = request.StartDate,
            NumberOfInstallments = request.NumberOfInstallments,
            InstallmentsPaid = 0,
            Status = "Active",
            ApprovedBy = approvedByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.EmployeeAdvances.Add(advance);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created advance for employee {EmployeeCode}: {Type} {Amount:C}",
            employee.EmployeeCode, request.AdvanceType, request.Amount);

        return MapToAdvanceResponse(advance, employee.FullName,
            approver != null ? $"{approver.FirstName} {approver.LastName}" : null);
    }

    public async Task<List<AdvanceResponse>> GetEmployeeAdvancesAsync(int employeeId)
    {
        var employee = await _context.Employees.FindAsync(employeeId)
            ?? throw new InvalidOperationException($"Employee with ID {employeeId} not found");

        var advances = await _context.EmployeeAdvances
            .AsNoTracking()
            .Include(a => a.ApprovedByUser)
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return advances.Select(a => MapToAdvanceResponse(a, employee.FullName,
            a.ApprovedByUser != null ? $"{a.ApprovedByUser.FirstName} {a.ApprovedByUser.LastName}" : null)).ToList();
    }

    public async Task<List<AdvanceResponse>> GetActiveAdvancesAsync()
    {
        var advances = await _context.EmployeeAdvances
            .AsNoTracking()
            .Include(a => a.Employee)
            .Include(a => a.ApprovedByUser)
            .Where(a => a.Status == "Active")
            .OrderBy(a => a.Employee.LastName)
            .ThenBy(a => a.Employee.FirstName)
            .ToListAsync();

        return advances.Select(a => MapToAdvanceResponse(a, a.Employee.FullName,
            a.ApprovedByUser != null ? $"{a.ApprovedByUser.FirstName} {a.ApprovedByUser.LastName}" : null)).ToList();
    }

    public async Task<AdvanceResponse> RecordAdvancePaymentAsync(int advanceId, decimal amount)
    {
        var advance = await _context.EmployeeAdvances
            .Include(a => a.Employee)
            .Include(a => a.ApprovedByUser)
            .FirstOrDefaultAsync(a => a.AdvanceId == advanceId)
            ?? throw new InvalidOperationException($"Advance with ID {advanceId} not found");

        advance.Balance -= amount;
        advance.InstallmentsPaid++;

        if (advance.Balance <= 0 || advance.InstallmentsPaid >= advance.NumberOfInstallments)
        {
            advance.Balance = 0;
            advance.Status = "Fully Paid";
        }

        advance.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Recorded payment of {Amount:C} for advance {Id}, remaining balance: {Balance:C}",
            amount, advanceId, advance.Balance);

        return MapToAdvanceResponse(advance, advance.Employee.FullName,
            advance.ApprovedByUser != null ? $"{advance.ApprovedByUser.FirstName} {advance.ApprovedByUser.LastName}" : null);
    }

    #endregion

    #region Private Helpers

    private static EmployeeResponse MapToResponse(Employee employee) => new()
    {
        EmployeeId = employee.EmployeeId,
        EmployeeCode = employee.EmployeeCode,
        FullName = employee.FullName,
        FirstName = employee.FirstName,
        LastName = employee.LastName,
        MiddleName = employee.MiddleName,
        DateOfBirth = employee.DateOfBirth,
        Gender = employee.Gender,
        CivilStatus = employee.CivilStatus,
        Address = employee.Address,
        City = employee.City,
        Province = employee.Province,
        PostalCode = employee.PostalCode,
        PhoneNumber = employee.PhoneNumber,
        Email = employee.Email,
        EmergencyContactName = employee.EmergencyContactName,
        EmergencyContactPhone = employee.EmergencyContactPhone,
        SSSNumber = employee.SSSNumber,
        PhilHealthNumber = employee.PhilHealthNumber,
        PagIBIGNumber = employee.PagIBIGNumber,
        TINNumber = employee.TINNumber,
        Position = employee.Position,
        Department = employee.Department,
        HireDate = employee.HireDate,
        EmploymentType = employee.EmploymentType,
        EmploymentStatus = employee.EmploymentStatus,
        DailyRate = employee.DailyRate,
        MonthlyBasicSalary = employee.MonthlyBasicSalary,
        PayrollType = employee.PayrollType,
        IsTherapist = employee.IsTherapist,
        Specialization = employee.Specialization,
        LicenseNumber = employee.LicenseNumber,
        LicenseExpiryDate = employee.LicenseExpiryDate,
        IsActive = employee.IsActive,
        UserId = employee.UserId,
        CreatedAt = employee.CreatedAt,
        UpdatedAt = employee.UpdatedAt
    };

    private static ScheduleResponse MapToScheduleResponse(EmployeeSchedule schedule, string employeeName) => new()
    {
        ScheduleId = schedule.ScheduleId,
        EmployeeId = schedule.EmployeeId,
        EmployeeName = employeeName,
        DayOfWeek = schedule.DayOfWeek,
        DayName = DayNames[schedule.DayOfWeek],
        ShiftStartTime = schedule.ShiftStartTime,
        ShiftEndTime = schedule.ShiftEndTime,
        BreakStartTime = schedule.BreakStartTime,
        BreakEndTime = schedule.BreakEndTime,
        IsRestDay = schedule.IsRestDay,
        EffectiveDate = schedule.EffectiveDate,
        EndDate = schedule.EndDate
    };

    private static TimeOffResponse MapToTimeOffResponse(TimeOffRequest timeOff, string employeeName, string? approvedByName) => new()
    {
        TimeOffRequestId = timeOff.TimeOffRequestId,
        EmployeeId = timeOff.EmployeeId,
        EmployeeName = employeeName,
        LeaveType = timeOff.LeaveType,
        StartDate = timeOff.StartDate,
        EndDate = timeOff.EndDate,
        TotalDays = timeOff.TotalDays,
        Reason = timeOff.Reason,
        Status = timeOff.Status,
        ApprovedByName = approvedByName,
        ApprovedAt = timeOff.ApprovedAt,
        RejectionReason = timeOff.RejectionReason,
        CreatedAt = timeOff.CreatedAt
    };

    private static LeaveBalanceResponse MapToLeaveBalanceResponse(EmployeeLeaveBalance balance, string employeeName) => new()
    {
        LeaveBalanceId = balance.LeaveBalanceId,
        EmployeeId = balance.EmployeeId,
        EmployeeName = employeeName,
        Year = balance.Year,
        SILDays = balance.SILDays,
        SILUsed = balance.SILUsed,
        SILRemaining = balance.SILRemaining,
        SickLeaveDays = balance.SickLeaveDays,
        SickLeaveUsed = balance.SickLeaveUsed,
        SickLeaveRemaining = balance.SickLeaveRemaining
    };

    private static AdvanceResponse MapToAdvanceResponse(EmployeeAdvance advance, string employeeName, string? approvedByName) => new()
    {
        AdvanceId = advance.AdvanceId,
        EmployeeId = advance.EmployeeId,
        EmployeeName = employeeName,
        AdvanceType = advance.AdvanceType,
        Amount = advance.Amount,
        Balance = advance.Balance,
        MonthlyDeduction = advance.MonthlyDeduction,
        StartDate = advance.StartDate,
        NumberOfInstallments = advance.NumberOfInstallments,
        InstallmentsPaid = advance.InstallmentsPaid,
        InstallmentsRemaining = advance.InstallmentsRemaining,
        Status = advance.Status,
        ApprovedByName = approvedByName,
        CreatedAt = advance.CreatedAt
    };

    private async Task<EmployeeLeaveBalance> GetOrCreateLeaveBalanceAsync(int employeeId, int year)
    {
        var balance = await _context.EmployeeLeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == employeeId && b.Year == year);

        if (balance == null)
        {
            balance = new EmployeeLeaveBalance
            {
                EmployeeId = employeeId,
                Year = year,
                SILDays = 5.0m,
                SILUsed = 0,
                SickLeaveDays = 0,
                SickLeaveUsed = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.EmployeeLeaveBalances.Add(balance);
            await _context.SaveChangesAsync();
        }

        return balance;
    }

    private static decimal CalculateWorkingDays(DateTime startDate, DateTime endDate)
    {
        decimal count = 0;
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            // Exclude Sundays (Philippine business norm)
            if (date.DayOfWeek != DayOfWeek.Sunday)
                count++;
        }
        return count;
    }

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
        var random = new byte[12];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(random);
        return new string(random.Select(b => chars[b % chars.Length]).ToArray());
    }

    private static string HashPassword(string password)
    {
        var saltBytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            100000,
            HashAlgorithmName.SHA256,
            32);

        var combined = new byte[saltBytes.Length + hash.Length];
        Buffer.BlockCopy(saltBytes, 0, combined, 0, saltBytes.Length);
        Buffer.BlockCopy(hash, 0, combined, saltBytes.Length, hash.Length);

        return Convert.ToBase64String(combined);
    }

    #endregion
}
