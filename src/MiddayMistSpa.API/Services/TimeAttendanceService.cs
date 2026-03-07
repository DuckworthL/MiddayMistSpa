using Microsoft.EntityFrameworkCore;
using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.API.DTOs.TimeAttendance;
using MiddayMistSpa.Core;
using MiddayMistSpa.Core.Entities.Employee;
using MiddayMistSpa.Infrastructure.Data;

namespace MiddayMistSpa.API.Services;

public class TimeAttendanceService : ITimeAttendanceService
{
    private readonly SpaDbContext _context;
    private readonly ILogger<TimeAttendanceService> _logger;

    private static readonly string[] DayNames = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

    public TimeAttendanceService(SpaDbContext context, ILogger<TimeAttendanceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ============================================================================
    // Employee Schedule Management
    // ============================================================================

    public async Task<ScheduleResponse> CreateScheduleAsync(CreateScheduleRequest request)
    {
        var employee = await _context.Employees.FindAsync(request.EmployeeId);
        if (employee == null)
            throw new InvalidOperationException("Employee not found");

        // Check for existing schedule on same day with overlapping effective dates
        var existingSchedule = await _context.EmployeeSchedules
            .Where(s => s.EmployeeId == request.EmployeeId)
            .Where(s => s.DayOfWeek == request.DayOfWeek)
            .Where(s => s.EffectiveDate <= request.EffectiveDate)
            .Where(s => s.EndDate == null || s.EndDate >= request.EffectiveDate)
            .FirstOrDefaultAsync();

        if (existingSchedule != null)
        {
            existingSchedule.EndDate = request.EffectiveDate.AddDays(-1);
            existingSchedule.UpdatedAt = DateTime.UtcNow;
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

        return (await GetScheduleByIdAsync(schedule.ScheduleId))!;
    }

    public async Task<ScheduleResponse?> GetScheduleByIdAsync(int scheduleId)
    {
        var schedule = await _context.EmployeeSchedules
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);

        if (schedule == null)
            return null;

        return MapToScheduleResponse(schedule);
    }

    public async Task<List<ScheduleResponse>> GetEmployeeSchedulesAsync(int employeeId, DateTime? effectiveDate = null)
    {
        var date = effectiveDate ?? DateTime.UtcNow.Date;

        var schedules = await _context.EmployeeSchedules
            .Include(s => s.Employee)
            .Where(s => s.EmployeeId == employeeId)
            .Where(s => s.EffectiveDate <= date)
            .Where(s => s.EndDate == null || s.EndDate >= date)
            .OrderBy(s => s.DayOfWeek)
            .ToListAsync();

        return schedules.Select(MapToScheduleResponse).ToList();
    }

    public async Task<WeeklyScheduleResponse> GetWeeklyScheduleAsync(int employeeId, DateTime? asOfDate = null)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
            throw new InvalidOperationException("Employee not found");

        var schedules = await GetEmployeeSchedulesAsync(employeeId, asOfDate);

        var scheduleDays = schedules.Select(s => new ScheduleDay
        {
            ScheduleId = s.ScheduleId,
            DayOfWeek = s.DayOfWeek,
            DayOfWeekName = s.DayName,
            ShiftStartTime = s.ShiftStartTime,
            ShiftEndTime = s.ShiftEndTime,
            BreakStartTime = s.BreakStartTime,
            BreakEndTime = s.BreakEndTime,
            IsRestDay = s.IsRestDay,
            WorkingHours = CalculateWorkingHours(s)
        }).ToList();

        return new WeeklyScheduleResponse
        {
            EmployeeId = employeeId,
            EmployeeName = $"{employee.FirstName} {employee.LastName}",
            Schedules = scheduleDays,
            TotalWeeklyHours = scheduleDays.Where(s => !s.IsRestDay).Sum(s => s.WorkingHours),
            WorkDays = scheduleDays.Count(s => !s.IsRestDay),
            RestDays = scheduleDays.Count(s => s.IsRestDay)
        };
    }

    public async Task<PagedResponse<ScheduleResponse>> SearchSchedulesAsync(ScheduleSearchRequest request)
    {
        var query = _context.EmployeeSchedules
            .Include(s => s.Employee)
            .AsQueryable();

        if (request.EmployeeId.HasValue)
            query = query.Where(s => s.EmployeeId == request.EmployeeId);

        if (request.EffectiveDate.HasValue)
        {
            var date = request.EffectiveDate.Value;
            query = query.Where(s => s.EffectiveDate <= date && (s.EndDate == null || s.EndDate >= date));
        }

        if (request.ActiveOnly == true)
        {
            var today = DateTime.UtcNow.Date;
            query = query.Where(s => s.EndDate == null || s.EndDate >= today);
        }

        var totalCount = await query.CountAsync();
        var schedules = await query
            .OrderBy(s => s.Employee.LastName)
            .ThenBy(s => s.DayOfWeek)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResponse<ScheduleResponse>
        {
            Items = schedules.Select(MapToScheduleResponse).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<ScheduleResponse> UpdateScheduleAsync(int scheduleId, UpdateScheduleRequest request)
    {
        var schedule = await _context.EmployeeSchedules.FindAsync(scheduleId);
        if (schedule == null)
            throw new InvalidOperationException("Schedule not found");

        schedule.ShiftStartTime = request.ShiftStartTime;
        schedule.ShiftEndTime = request.ShiftEndTime;
        schedule.BreakStartTime = request.BreakStartTime;
        schedule.BreakEndTime = request.BreakEndTime;
        schedule.IsRestDay = request.IsRestDay;
        schedule.EndDate = request.EndDate;
        schedule.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (await GetScheduleByIdAsync(scheduleId))!;
    }

    public async Task<bool> DeleteScheduleAsync(int scheduleId)
    {
        var schedule = await _context.EmployeeSchedules.FindAsync(scheduleId);
        if (schedule == null)
            return false;

        _context.EmployeeSchedules.Remove(schedule);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<ScheduleResponse>> SetWeeklyScheduleAsync(int employeeId, List<CreateScheduleRequest> schedules)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
            throw new InvalidOperationException("Employee not found");

        var results = new List<ScheduleResponse>();

        foreach (var request in schedules)
        {
            var req = new CreateScheduleRequest
            {
                EmployeeId = employeeId,
                DayOfWeek = request.DayOfWeek,
                ShiftStartTime = request.ShiftStartTime,
                ShiftEndTime = request.ShiftEndTime,
                BreakStartTime = request.BreakStartTime,
                BreakEndTime = request.BreakEndTime,
                IsRestDay = request.IsRestDay,
                EffectiveDate = request.EffectiveDate,
                EndDate = request.EndDate
            };
            var result = await CreateScheduleAsync(req);
            results.Add(result);
        }

        return results;
    }

    // ============================================================================
    // Time Off Request Management
    // ============================================================================

    public async Task<TimeOffResponse> CreateTimeOffRequestAsync(CreateTimeOffRequest request)
    {
        var employee = await _context.Employees.FindAsync(request.EmployeeId);
        if (employee == null)
            throw new InvalidOperationException("Employee not found");

        if (request.EndDate < request.StartDate)
            throw new InvalidOperationException("End date must be on or after start date");

        var totalDays = await CalculateLeaveDaysAsync(request.EmployeeId, request.StartDate, request.EndDate);

        if (request.LeaveType == "SIL")
        {
            var balance = await GetLeaveBalanceAsync(request.EmployeeId, request.StartDate.Year);
            if (balance != null && balance.SILRemaining < totalDays)
                throw new InvalidOperationException($"Insufficient SIL balance. Available: {balance.SILRemaining} days");
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

        return (await GetTimeOffRequestByIdAsync(timeOff.TimeOffRequestId))!;
    }

    public async Task<TimeOffResponse?> GetTimeOffRequestByIdAsync(int timeOffRequestId)
    {
        var request = await _context.TimeOffRequests
            .Include(t => t.Employee)
            .Include(t => t.ApprovedByUser)
            .FirstOrDefaultAsync(t => t.TimeOffRequestId == timeOffRequestId);

        if (request == null)
            return null;

        return MapToTimeOffResponse(request);
    }

    public async Task<PagedResponse<TimeOffResponse>> SearchTimeOffRequestsAsync(TimeOffSearchRequest request)
    {
        var query = _context.TimeOffRequests
            .Include(t => t.Employee)
            .Include(t => t.ApprovedByUser)
            .AsQueryable();

        if (request.EmployeeId.HasValue)
            query = query.Where(t => t.EmployeeId == request.EmployeeId);

        if (!string.IsNullOrWhiteSpace(request.LeaveType))
            query = query.Where(t => t.LeaveType == request.LeaveType);

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(t => t.Status == request.Status);

        if (request.DateFrom.HasValue)
            query = query.Where(t => t.StartDate >= request.DateFrom.Value);

        if (request.DateTo.HasValue)
            query = query.Where(t => t.EndDate <= request.DateTo.Value);

        query = request.SortBy?.ToLower() switch
        {
            "employee" => request.SortDescending
                ? query.OrderByDescending(t => t.Employee.LastName)
                : query.OrderBy(t => t.Employee.LastName),
            "status" => request.SortDescending
                ? query.OrderByDescending(t => t.Status)
                : query.OrderBy(t => t.Status),
            _ => request.SortDescending
                ? query.OrderByDescending(t => t.StartDate)
                : query.OrderBy(t => t.StartDate)
        };

        var totalCount = await query.CountAsync();
        var requests = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResponse<TimeOffResponse>
        {
            Items = requests.Select(MapToTimeOffResponse).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<TimeOffResponse> UpdateTimeOffRequestAsync(int timeOffRequestId, UpdateTimeOffRequest request)
    {
        var timeOff = await _context.TimeOffRequests.FindAsync(timeOffRequestId);
        if (timeOff == null)
            throw new InvalidOperationException("Time off request not found");

        if (timeOff.Status != "Pending")
            throw new InvalidOperationException("Only pending requests can be updated");

        if (!string.IsNullOrWhiteSpace(request.LeaveType))
            timeOff.LeaveType = request.LeaveType;
        if (request.StartDate.HasValue)
            timeOff.StartDate = request.StartDate.Value;
        if (request.EndDate.HasValue)
            timeOff.EndDate = request.EndDate.Value;
        if (request.Reason != null)
            timeOff.Reason = request.Reason;

        timeOff.TotalDays = await CalculateLeaveDaysAsync(timeOff.EmployeeId, timeOff.StartDate, timeOff.EndDate);
        timeOff.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (await GetTimeOffRequestByIdAsync(timeOffRequestId))!;
    }

    public async Task<TimeOffResponse> ApproveTimeOffRequestAsync(int timeOffRequestId, int approvedByUserId)
    {
        var timeOff = await _context.TimeOffRequests
            .Include(t => t.Employee)
            .FirstOrDefaultAsync(t => t.TimeOffRequestId == timeOffRequestId);

        if (timeOff == null)
            throw new InvalidOperationException("Time off request not found");

        if (timeOff.Status != "Pending")
            throw new InvalidOperationException("Only pending requests can be approved");

        timeOff.Status = "Approved";
        timeOff.ApprovedBy = approvedByUserId;
        timeOff.ApprovedAt = DateTime.UtcNow;
        timeOff.UpdatedAt = DateTime.UtcNow;

        await DeductLeaveBalanceAsync(timeOff.EmployeeId, timeOff.StartDate.Year, timeOff.LeaveType, timeOff.TotalDays);

        await _context.SaveChangesAsync();
        return (await GetTimeOffRequestByIdAsync(timeOffRequestId))!;
    }

    public async Task<TimeOffResponse> RejectTimeOffRequestAsync(int timeOffRequestId, int rejectedByUserId, RejectTimeOffRequest request)
    {
        var timeOff = await _context.TimeOffRequests.FindAsync(timeOffRequestId);
        if (timeOff == null)
            throw new InvalidOperationException("Time off request not found");

        if (timeOff.Status != "Pending")
            throw new InvalidOperationException("Only pending requests can be rejected");

        timeOff.Status = "Rejected";
        timeOff.ApprovedBy = rejectedByUserId;
        timeOff.ApprovedAt = DateTime.UtcNow;
        timeOff.RejectionReason = request.RejectionReason;
        timeOff.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (await GetTimeOffRequestByIdAsync(timeOffRequestId))!;
    }

    public async Task<bool> CancelTimeOffRequestAsync(int timeOffRequestId, int cancelledByUserId)
    {
        var timeOff = await _context.TimeOffRequests.FindAsync(timeOffRequestId);
        if (timeOff == null)
            return false;

        if (timeOff.Status == "Approved")
        {
            await RestoreLeaveBalanceAsync(timeOff.EmployeeId, timeOff.StartDate.Year, timeOff.LeaveType, timeOff.TotalDays);
        }

        _context.TimeOffRequests.Remove(timeOff);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<TimeOffResponse>> GetPendingTimeOffRequestsAsync()
    {
        var requests = await _context.TimeOffRequests
            .Include(t => t.Employee)
            .Where(t => t.Status == "Pending")
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        return requests.Select(MapToTimeOffResponse).ToList();
    }

    // ============================================================================
    // Leave Balance Management
    // ============================================================================

    public async Task<LeaveBalanceResponse> CreateLeaveBalanceAsync(CreateLeaveBalanceRequest request)
    {
        var employee = await _context.Employees.FindAsync(request.EmployeeId);
        if (employee == null)
            throw new InvalidOperationException("Employee not found");

        var existing = await _context.EmployeeLeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == request.EmployeeId && b.Year == request.Year);

        if (existing != null)
            throw new InvalidOperationException("Leave balance already exists for this employee and year");

        var balance = new EmployeeLeaveBalance
        {
            EmployeeId = request.EmployeeId,
            Year = request.Year,
            SILDays = request.SILDays,
            SickLeaveDays = request.SickLeaveDays,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.EmployeeLeaveBalances.Add(balance);
        await _context.SaveChangesAsync();

        return (await GetLeaveBalanceAsync(request.EmployeeId, request.Year))!;
    }

    public async Task<LeaveBalanceResponse?> GetLeaveBalanceAsync(int employeeId, int year)
    {
        var balance = await _context.EmployeeLeaveBalances
            .Include(b => b.Employee)
            .FirstOrDefaultAsync(b => b.EmployeeId == employeeId && b.Year == year);

        if (balance == null)
            return null;

        return MapToLeaveBalanceResponse(balance);
    }

    public async Task<LeaveBalanceSummary> GetEmployeeLeaveBalancesAsync(int employeeId)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
            throw new InvalidOperationException("Employee not found");

        var balances = await _context.EmployeeLeaveBalances
            .Include(b => b.Employee)
            .Where(b => b.EmployeeId == employeeId)
            .OrderByDescending(b => b.Year)
            .ToListAsync();

        var currentYear = DateTime.UtcNow.Year;
        var currentYearBalance = balances.FirstOrDefault(b => b.Year == currentYear);

        return new LeaveBalanceSummary
        {
            EmployeeId = employeeId,
            EmployeeName = $"{employee.FirstName} {employee.LastName}",
            Year = currentYear,
            TotalLeaveEntitlement = (currentYearBalance?.SILDays ?? 0) + (currentYearBalance?.SickLeaveDays ?? 0),
            TotalLeaveUsed = (currentYearBalance?.SILUsed ?? 0) + (currentYearBalance?.SickLeaveUsed ?? 0),
            TotalLeaveRemaining = (currentYearBalance?.SILRemaining ?? 0) + (currentYearBalance?.SickLeaveRemaining ?? 0)
        };
    }

    public async Task<LeaveBalanceResponse> UpdateLeaveBalanceAsync(int leaveBalanceId, UpdateLeaveBalanceRequest request)
    {
        var balance = await _context.EmployeeLeaveBalances.FindAsync(leaveBalanceId);
        if (balance == null)
            throw new InvalidOperationException("Leave balance not found");

        balance.SILDays = request.SILDays;
        balance.SickLeaveDays = request.SickLeaveDays;
        balance.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (await GetLeaveBalanceAsync(balance.EmployeeId, balance.Year))!;
    }

    public async Task<int> InitializeYearlyLeaveBalancesAsync(int year)
    {
        var activeEmployees = await _context.Employees
            .Where(e => e.IsActive)
            .ToListAsync();

        var count = 0;
        foreach (var employee in activeEmployees)
        {
            var existing = await _context.EmployeeLeaveBalances
                .FirstOrDefaultAsync(b => b.EmployeeId == employee.EmployeeId && b.Year == year);

            if (existing == null)
            {
                var balance = new EmployeeLeaveBalance
                {
                    EmployeeId = employee.EmployeeId,
                    Year = year,
                    SILDays = 5.0m,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.EmployeeLeaveBalances.Add(balance);
                count++;
            }
        }

        await _context.SaveChangesAsync();
        return count;
    }

    public async Task<LeaveBalanceResponse> AdjustLeaveBalanceAsync(int employeeId, int year, string leaveType, decimal days, bool isAddition)
    {
        var balance = await _context.EmployeeLeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == employeeId && b.Year == year);

        if (balance == null)
        {
            balance = new EmployeeLeaveBalance
            {
                EmployeeId = employeeId,
                Year = year,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.EmployeeLeaveBalances.Add(balance);
        }

        switch (leaveType)
        {
            case "SIL":
                balance.SILDays = isAddition ? balance.SILDays + days : balance.SILDays - days;
                break;
            case "Sick Leave":
                balance.SickLeaveDays = isAddition ? balance.SickLeaveDays + days : balance.SickLeaveDays - days;
                break;
        }

        balance.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (await GetLeaveBalanceAsync(employeeId, year))!;
    }

    // ============================================================================
    // Employee Advance Management
    // ============================================================================

    public async Task<AdvanceResponse> CreateAdvanceAsync(CreateAdvanceRequest request, int approvedByUserId)
    {
        var employee = await _context.Employees.FindAsync(request.EmployeeId);
        if (employee == null)
            throw new InvalidOperationException("Employee not found");

        var advance = new EmployeeAdvance
        {
            EmployeeId = request.EmployeeId,
            AdvanceType = request.AdvanceType,
            Amount = request.Amount,
            Balance = request.Amount,
            MonthlyDeduction = request.MonthlyDeduction,
            StartDate = request.StartDate,
            NumberOfInstallments = request.NumberOfInstallments,
            Status = "Active",
            ApprovedBy = approvedByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<EmployeeAdvance>().Add(advance);
        await _context.SaveChangesAsync();

        return (await GetAdvanceByIdAsync(advance.AdvanceId))!;
    }

    public async Task<AdvanceResponse?> GetAdvanceByIdAsync(int advanceId)
    {
        var advance = await _context.Set<EmployeeAdvance>()
            .Include(a => a.Employee)
            .Include(a => a.ApprovedByUser)
            .FirstOrDefaultAsync(a => a.AdvanceId == advanceId);

        if (advance == null)
            return null;

        return MapToAdvanceResponse(advance);
    }

    public async Task<PagedResponse<AdvanceResponse>> SearchAdvancesAsync(AdvanceSearchRequest request)
    {
        var query = _context.Set<EmployeeAdvance>()
            .Include(a => a.Employee)
            .Include(a => a.ApprovedByUser)
            .AsQueryable();

        if (request.EmployeeId.HasValue)
            query = query.Where(a => a.EmployeeId == request.EmployeeId);

        if (!string.IsNullOrWhiteSpace(request.AdvanceType))
            query = query.Where(a => a.AdvanceType == request.AdvanceType);

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(a => a.Status == request.Status);

        query = request.SortBy?.ToLower() switch
        {
            "amount" => request.SortDescending
                ? query.OrderByDescending(a => a.Amount)
                : query.OrderBy(a => a.Amount),
            "balance" => request.SortDescending
                ? query.OrderByDescending(a => a.Balance)
                : query.OrderBy(a => a.Balance),
            _ => request.SortDescending
                ? query.OrderByDescending(a => a.CreatedAt)
                : query.OrderBy(a => a.CreatedAt)
        };

        var totalCount = await query.CountAsync();
        var advances = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResponse<AdvanceResponse>
        {
            Items = advances.Select(MapToAdvanceResponse).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<AdvanceResponse> UpdateAdvanceAsync(int advanceId, UpdateAdvanceRequest request)
    {
        var advance = await _context.Set<EmployeeAdvance>().FindAsync(advanceId);
        if (advance == null)
            throw new InvalidOperationException("Advance not found");

        if (advance.Status == "Fully Paid")
            throw new InvalidOperationException("Cannot update fully paid advance");

        if (request.MonthlyDeduction.HasValue)
            advance.MonthlyDeduction = request.MonthlyDeduction.Value;
        if (request.NumberOfInstallments.HasValue)
            advance.NumberOfInstallments = request.NumberOfInstallments.Value;

        advance.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (await GetAdvanceByIdAsync(advanceId))!;
    }

    public async Task<AdvanceResponse> RecordAdvancePaymentAsync(int advanceId, RecordPaymentRequest request)
    {
        var advance = await _context.Set<EmployeeAdvance>().FindAsync(advanceId);
        if (advance == null)
            throw new InvalidOperationException("Advance not found");

        if (advance.Status == "Fully Paid")
            throw new InvalidOperationException("Advance is already fully paid");

        advance.Balance -= request.Amount;
        advance.InstallmentsPaid++;

        if (advance.Balance <= 0 || advance.InstallmentsPaid >= advance.NumberOfInstallments)
        {
            advance.Status = "Fully Paid";
            advance.Balance = 0;
        }

        advance.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (await GetAdvanceByIdAsync(advanceId))!;
    }

    public async Task<List<AdvanceResponse>> GetActiveAdvancesForEmployeeAsync(int employeeId)
    {
        var advances = await _context.Set<EmployeeAdvance>()
            .Include(a => a.Employee)
            .Where(a => a.EmployeeId == employeeId && a.Status == "Active")
            .ToListAsync();

        return advances.Select(MapToAdvanceResponse).ToList();
    }

    // ============================================================================
    // Attendance Summary & Reports
    // ============================================================================

    public async Task<AttendanceSummaryResponse> GetAttendanceSummaryAsync(int employeeId, DateTime startDate, DateTime endDate)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
            throw new InvalidOperationException("Employee not found");

        var schedules = await GetEmployeeSchedulesAsync(employeeId, startDate);
        var scheduledWorkDays = 0;
        decimal scheduledHours = 0;

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var dayOfWeek = (int)date.DayOfWeek;
            var schedule = schedules.FirstOrDefault(s => s.DayOfWeek == dayOfWeek);

            if (schedule != null && !schedule.IsRestDay)
            {
                scheduledWorkDays++;
                scheduledHours += CalculateWorkingHours(schedule);
            }
        }

        var leaves = await _context.TimeOffRequests
            .Where(t => t.EmployeeId == employeeId)
            .Where(t => t.Status == "Approved")
            .Where(t => t.StartDate <= endDate && t.EndDate >= startDate)
            .ToListAsync();

        var leaveDays = leaves.Sum(l => l.TotalDays);

        var totalWorkDays = (int)(endDate - startDate).TotalDays + 1;
        var attendanceRate = scheduledWorkDays > 0
            ? ((scheduledWorkDays - (int)leaveDays) / (decimal)scheduledWorkDays) * 100
            : 100;

        return new AttendanceSummaryResponse
        {
            EmployeeId = employeeId,
            EmployeeName = $"{employee.FirstName} {employee.LastName}",
            StartDate = startDate,
            EndDate = endDate,
            TotalWorkDays = totalWorkDays,
            ScheduledWorkDays = scheduledWorkDays,
            TotalScheduledHours = scheduledHours,
            LeaveDays = (int)leaveDays,
            AbsentDays = 0,
            AttendanceRate = attendanceRate
        };
    }

    public async Task<TeamAttendanceResponse> GetTeamAttendanceAsync(DateTime startDate, DateTime endDate)
    {
        var employees = await _context.Employees
            .Where(e => e.IsActive)
            .ToListAsync();

        var summaries = new List<AttendanceSummaryResponse>();
        foreach (var employee in employees)
        {
            var summary = await GetAttendanceSummaryAsync(employee.EmployeeId, startDate, endDate);
            summaries.Add(summary);
        }

        var leaveRequests = await _context.TimeOffRequests
            .Where(t => t.StartDate <= endDate && t.EndDate >= startDate)
            .ToListAsync();

        return new TeamAttendanceResponse
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalEmployees = employees.Count,
            AverageAttendanceRate = summaries.Any() ? summaries.Average(s => s.AttendanceRate) : 0,
            TotalLeaveRequests = leaveRequests.Count,
            ApprovedLeaves = leaveRequests.Count(l => l.Status == "Approved"),
            PendingLeaves = leaveRequests.Count(l => l.Status == "Pending"),
            EmployeeSummaries = summaries
        };
    }

    public async Task<List<ScheduleCalendarDayResponse>> GetScheduleCalendarAsync(ScheduleCalendarRequest request)
    {
        var employees = await _context.Employees
            .Where(e => e.IsActive)
            .ToListAsync();

        var allSchedules = await _context.EmployeeSchedules
            .Include(s => s.Employee)
            .Where(s => s.EffectiveDate <= request.EndDate)
            .Where(s => s.EndDate == null || s.EndDate >= request.StartDate)
            .ToListAsync();

        var leaves = await _context.TimeOffRequests
            .Where(l => l.Status == "Approved")
            .Where(l => l.StartDate <= request.EndDate && l.EndDate >= request.StartDate)
            .ToListAsync();

        var holidays = await _context.Set<Core.Entities.Payroll.PhilippineHoliday>()
            .Where(h => h.HolidayDate >= request.StartDate && h.HolidayDate <= request.EndDate)
            .ToListAsync();

        var result = new List<ScheduleCalendarDayResponse>();

        for (var date = request.StartDate; date <= request.EndDate; date = date.AddDays(1))
        {
            var dayOfWeek = (int)date.DayOfWeek;
            var holiday = holidays.FirstOrDefault(h => h.HolidayDate.Date == date.Date);

            var dailySchedule = new ScheduleCalendarDayResponse
            {
                Date = date,
                DayName = DayNames[dayOfWeek],
                IsHoliday = holiday != null,
                HolidayName = holiday?.HolidayName,
                Schedules = []
            };

            foreach (var employee in employees)
            {
                var schedule = allSchedules
                    .Where(s => s.EmployeeId == employee.EmployeeId)
                    .Where(s => s.DayOfWeek == dayOfWeek)
                    .FirstOrDefault();

                var leave = leaves
                    .Where(l => l.EmployeeId == employee.EmployeeId)
                    .Where(l => l.StartDate <= date && l.EndDate >= date)
                    .FirstOrDefault();

                var employeeDaySchedule = new EmployeeDaySchedule
                {
                    EmployeeId = employee.EmployeeId,
                    EmployeeName = $"{employee.FirstName} {employee.LastName}",
                    ShiftStart = schedule?.ShiftStartTime,
                    ShiftEnd = schedule?.ShiftEndTime,
                    IsRestDay = schedule?.IsRestDay ?? false,
                    IsOnLeave = leave != null,
                    LeaveType = leave?.LeaveType
                };

                employeeDaySchedule.Status = leave != null ? "On Leave"
                    : holiday != null ? "Holiday"
                    : schedule?.IsRestDay == true ? "Rest Day"
                    : "Scheduled";

                dailySchedule.Schedules.Add(employeeDaySchedule);
            }

            result.Add(dailySchedule);
        }

        return result;
    }

    // ============================================================================
    // Helper Methods
    // ============================================================================

    private decimal CalculateWorkingHours(ScheduleResponse schedule)
    {
        if (schedule.IsRestDay) return 0;

        var totalHours = (decimal)(schedule.ShiftEndTime - schedule.ShiftStartTime).TotalHours;
        var breakHours = (schedule.BreakStartTime.HasValue && schedule.BreakEndTime.HasValue)
            ? (decimal)(schedule.BreakEndTime.Value - schedule.BreakStartTime.Value).TotalHours : 0;

        return totalHours - breakHours;
    }

    private async Task<decimal> CalculateLeaveDaysAsync(int employeeId, DateTime startDate, DateTime endDate)
    {
        var schedules = await GetEmployeeSchedulesAsync(employeeId, startDate);
        decimal leaveDays = 0;

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var dayOfWeek = (int)date.DayOfWeek;
            var schedule = schedules.FirstOrDefault(s => s.DayOfWeek == dayOfWeek);

            if (schedule == null || !schedule.IsRestDay)
            {
                leaveDays++;
            }
        }

        return leaveDays;
    }

    private async Task DeductLeaveBalanceAsync(int employeeId, int year, string leaveType, decimal days)
    {
        var balance = await _context.EmployeeLeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == employeeId && b.Year == year);

        if (balance == null)
            return;

        switch (leaveType)
        {
            case "SIL":
                balance.SILUsed += days;
                break;
            case "Sick Leave":
                balance.SickLeaveUsed += days;
                break;
        }

        balance.UpdatedAt = DateTime.UtcNow;
    }

    private async Task RestoreLeaveBalanceAsync(int employeeId, int year, string leaveType, decimal days)
    {
        var balance = await _context.EmployeeLeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == employeeId && b.Year == year);

        if (balance == null)
            return;

        switch (leaveType)
        {
            case "SIL":
                balance.SILUsed = Math.Max(0, balance.SILUsed - days);
                break;
            case "Sick Leave":
                balance.SickLeaveUsed = Math.Max(0, balance.SickLeaveUsed - days);
                break;
        }

        balance.UpdatedAt = DateTime.UtcNow;
    }

    private ScheduleResponse MapToScheduleResponse(EmployeeSchedule schedule)
    {
        return new ScheduleResponse
        {
            ScheduleId = schedule.ScheduleId,
            EmployeeId = schedule.EmployeeId,
            EmployeeName = schedule.Employee != null
                ? $"{schedule.Employee.FirstName} {schedule.Employee.LastName}"
                : "",
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
    }

    private TimeOffResponse MapToTimeOffResponse(TimeOffRequest request)
    {
        return new TimeOffResponse
        {
            TimeOffRequestId = request.TimeOffRequestId,
            EmployeeId = request.EmployeeId,
            EmployeeName = request.Employee != null
                ? $"{request.Employee.FirstName} {request.Employee.LastName}"
                : "",
            LeaveType = request.LeaveType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalDays = request.TotalDays,
            Reason = request.Reason,
            Status = request.Status,
            ApprovedByName = request.ApprovedByUser != null
                ? $"{request.ApprovedByUser.FirstName} {request.ApprovedByUser.LastName}"
                : null,
            ApprovedAt = request.ApprovedAt,
            RejectionReason = request.RejectionReason,
            CreatedAt = request.CreatedAt
        };
    }

    private LeaveBalanceResponse MapToLeaveBalanceResponse(EmployeeLeaveBalance balance)
    {
        return new LeaveBalanceResponse
        {
            LeaveBalanceId = balance.LeaveBalanceId,
            EmployeeId = balance.EmployeeId,
            EmployeeName = balance.Employee != null
                ? $"{balance.Employee.FirstName} {balance.Employee.LastName}"
                : "",
            Year = balance.Year,
            SILDays = balance.SILDays,
            SILUsed = balance.SILUsed,
            SILRemaining = balance.SILRemaining,
            SickLeaveDays = balance.SickLeaveDays,
            SickLeaveUsed = balance.SickLeaveUsed,
            SickLeaveRemaining = balance.SickLeaveRemaining
        };
    }

    private AdvanceResponse MapToAdvanceResponse(EmployeeAdvance advance)
    {
        return new AdvanceResponse
        {
            AdvanceId = advance.AdvanceId,
            EmployeeId = advance.EmployeeId,
            EmployeeName = advance.Employee != null
                ? $"{advance.Employee.FirstName} {advance.Employee.LastName}"
                : "",
            AdvanceType = advance.AdvanceType,
            Amount = advance.Amount,
            Balance = advance.Balance,
            MonthlyDeduction = advance.MonthlyDeduction,
            StartDate = advance.StartDate,
            NumberOfInstallments = advance.NumberOfInstallments,
            InstallmentsPaid = advance.InstallmentsPaid,
            InstallmentsRemaining = advance.InstallmentsRemaining,
            Status = advance.Status,
            ApprovedByName = advance.ApprovedByUser != null
                ? $"{advance.ApprovedByUser.FirstName} {advance.ApprovedByUser.LastName}"
                : null,
            CreatedAt = advance.CreatedAt
        };
    }

    // ============================================================================
    // Clock In/Out & Attendance Records
    // ============================================================================

    public async Task<AttendanceRecordDto> ClockInAsync(int employeeId, int? performedByUserId = null)
    {
        var employee = await _context.Employees.FindAsync(employeeId)
            ?? throw new InvalidOperationException("Employee not found");

        var today = PhilippineTime.Today;
        var now = PhilippineTime.Now;

        // Shift enforcement: employee must have an active shift for today's day of week
        var todayDayOfWeek = (int)today.DayOfWeek; // 0=Sunday, 1=Monday, ..., 6=Saturday
        var hasShift = await _context.EmployeeShifts
            .AnyAsync(s => s.EmployeeId == employeeId
                && s.DayOfWeek == todayDayOfWeek
                && s.IsActive
                && s.EffectiveFrom <= today
                && (s.EffectiveTo == null || s.EffectiveTo >= today));

        if (!hasShift)
            throw new InvalidOperationException(
                $"{employee.FullName} does not have a scheduled shift for today ({today.DayOfWeek}). Please create a shift in Shift Management before clocking in.");

        // Duplicate prevention: check if already clocked in today (has open record)
        var existing = await _context.AttendanceRecords
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId
                && a.Date == today
                && a.ClockOut == null);

        if (existing != null)
            throw new InvalidOperationException("Employee is already clocked in today. Please clock out first.");

        // Also check if already has a completed record today (clocked in and out)
        var completedToday = await _context.AttendanceRecords
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId
                && a.Date == today
                && a.ClockOut != null);

        if (completedToday != null)
            throw new InvalidOperationException("Employee already has a completed attendance record for today.");

        var record = new AttendanceRecord
        {
            EmployeeId = employeeId,
            Date = today,
            ClockIn = now,
            Status = "ClockedIn",
            ClockedInByUserId = performedByUserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _context.AttendanceRecords.AddAsync(record);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Employee {EmployeeId} ({Name}) clocked in at {Time}{ByUser}",
            employeeId, employee.FullName, record.ClockIn,
            performedByUserId.HasValue ? $" by user {performedByUserId}" : " (self)");

        return MapToAttendanceDto(record, employee);
    }

    public async Task<AttendanceRecordDto> ClockOutAsync(int employeeId)
    {
        var employee = await _context.Employees.FindAsync(employeeId)
            ?? throw new InvalidOperationException("Employee not found");

        var today = PhilippineTime.Today;
        var now = PhilippineTime.Now;

        var record = await _context.AttendanceRecords
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId
                && a.Date == today
                && a.ClockOut == null)
            ?? throw new InvalidOperationException("Employee is not clocked in today.");

        // End any active break first
        if (record.Status == "OnBreak" && record.BreakStart.HasValue && !record.BreakEnd.HasValue)
        {
            record.BreakEnd = now;
        }

        record.ClockOut = now;
        record.Status = "ClockedOut";

        // Calculate totals
        if (record.ClockIn.HasValue)
        {
            var totalSpan = record.ClockOut.Value - record.ClockIn.Value;
            decimal breakMins = 0;
            if (record.BreakStart.HasValue && record.BreakEnd.HasValue)
            {
                breakMins = (decimal)(record.BreakEnd.Value - record.BreakStart.Value).TotalMinutes;
            }
            record.BreakMinutes = breakMins;
            record.TotalHours = Math.Round((decimal)totalSpan.TotalHours - (breakMins / 60m), 2);
            if (record.TotalHours < 0) record.TotalHours = 0;
        }

        record.UpdatedAt = now;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Employee {EmployeeId} ({Name}) clocked out at {Time}. Total hours: {Hours}",
            employeeId, employee.FullName, record.ClockOut, record.TotalHours);

        return MapToAttendanceDto(record, employee);
    }

    public async Task<AttendanceRecordDto> StartBreakAsync(int employeeId)
    {
        var employee = await _context.Employees.FindAsync(employeeId)
            ?? throw new InvalidOperationException("Employee not found");

        var today = PhilippineTime.Today;
        var now = PhilippineTime.Now;

        var record = await _context.AttendanceRecords
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId
                && a.Date == today
                && a.ClockOut == null)
            ?? throw new InvalidOperationException("Employee is not clocked in today.");

        if (record.Status == "OnBreak")
            throw new InvalidOperationException("Employee is already on break.");

        record.BreakStart = now;
        record.BreakEnd = null;
        record.Status = "OnBreak";
        record.UpdatedAt = now;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Employee {EmployeeId} started break at {Time}", employeeId, record.BreakStart);

        return MapToAttendanceDto(record, employee);
    }

    public async Task<AttendanceRecordDto> EndBreakAsync(int employeeId)
    {
        var employee = await _context.Employees.FindAsync(employeeId)
            ?? throw new InvalidOperationException("Employee not found");

        var today = PhilippineTime.Today;
        var now = PhilippineTime.Now;

        var record = await _context.AttendanceRecords
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId
                && a.Date == today
                && a.ClockOut == null)
            ?? throw new InvalidOperationException("Employee is not clocked in today.");

        if (record.Status != "OnBreak" || !record.BreakStart.HasValue)
            throw new InvalidOperationException("Employee is not on break.");

        record.BreakEnd = now;
        record.Status = "ClockedIn";
        record.BreakMinutes = (decimal)(record.BreakEnd.Value - record.BreakStart.Value).TotalMinutes;
        record.UpdatedAt = now;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Employee {EmployeeId} ended break at {Time}. Break minutes: {Mins}",
            employeeId, record.BreakEnd, record.BreakMinutes);

        return MapToAttendanceDto(record, employee);
    }

    public async Task<AttendanceRecordDto> ApproveAttendanceRecordAsync(int attendanceId, int approvedByUserId)
    {
        var record = await _context.AttendanceRecords
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.AttendanceId == attendanceId)
            ?? throw new InvalidOperationException("Attendance record not found");

        if (record.IsApproved)
            throw new InvalidOperationException("Attendance record is already approved");

        record.IsApproved = true;
        record.UpdatedAt = PhilippineTime.Now;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Attendance record {AttendanceId} approved by user {UserId}", attendanceId, approvedByUserId);

        return MapToAttendanceDto(record, record.Employee);
    }

    public async Task<List<AttendanceRecordDto>> GetAttendanceRecordsAsync(int? employeeId = null, DateTime? date = null)
    {
        var query = _context.AttendanceRecords
            .Include(a => a.Employee)
            .AsQueryable();

        if (employeeId.HasValue)
            query = query.Where(a => a.EmployeeId == employeeId.Value);

        if (date.HasValue)
            query = query.Where(a => a.Date == date.Value.Date);
        else
            // Default to last 7 days (Philippine time)
            query = query.Where(a => a.Date >= PhilippineTime.Today.AddDays(-7));

        var records = await query
            .OrderByDescending(a => a.Date)
            .ThenBy(a => a.Employee.LastName)
            .ToListAsync();

        return records.Select(r => MapToAttendanceDto(r, r.Employee)).ToList();
    }

    public async Task<List<LiveAttendanceStatusDto>> GetLiveStatusAsync()
    {
        var today = PhilippineTime.Today;

        var employees = await _context.Employees
            .Where(e => e.IsActive && e.EmploymentStatus == "Active")
            .OrderBy(e => e.LastName)
            .ToListAsync();

        var todayRecords = await _context.AttendanceRecords
            .Where(a => a.Date == today)
            .ToListAsync();

        var result = new List<LiveAttendanceStatusDto>();

        foreach (var emp in employees)
        {
            var record = todayRecords.FirstOrDefault(r => r.EmployeeId == emp.EmployeeId);
            var initials = $"{emp.FirstName.FirstOrDefault()}{emp.LastName.FirstOrDefault()}";

            decimal todayHours = 0;
            if (record?.ClockIn != null)
            {
                var now = PhilippineTime.Now;
                var endTime = record.ClockOut ?? now;
                var totalMins = (decimal)(endTime - record.ClockIn.Value).TotalMinutes;
                totalMins -= record.BreakMinutes;
                if (record.Status == "OnBreak" && record.BreakStart.HasValue)
                {
                    totalMins -= (decimal)(now - record.BreakStart.Value).TotalMinutes;
                }
                todayHours = Math.Max(0, Math.Round(totalMins / 60m, 1));
            }

            // Determine the live status string
            string liveStatus;
            if (record == null)
                liveStatus = "NotYetIn";
            else if (record.ClockOut != null)
                liveStatus = "ClockedOut";
            else if (record.Status == "OnBreak")
                liveStatus = "OnBreak";
            else
                liveStatus = "ClockedIn";

            result.Add(new LiveAttendanceStatusDto
            {
                EmployeeId = emp.EmployeeId,
                EmployeeName = emp.FullName,
                Initials = initials.ToUpper(),
                Position = emp.Position,
                IsClockedIn = record != null && record.ClockOut == null,
                ClockIn = record?.ClockIn,
                ClockOut = record?.ClockOut,
                OnBreak = record?.Status == "OnBreak",
                BreakStart = record?.BreakStart,
                TodayHours = todayHours,
                Status = liveStatus
            });
        }

        return result;
    }

    public async Task<AttendanceRecordDto> CreateManualEntryAsync(ManualAttendanceRequest request, int createdByUserId)
    {
        var employee = await _context.Employees.FindAsync(request.EmployeeId)
            ?? throw new InvalidOperationException("Employee not found");

        var date = request.Date.Date;

        // Check for existing record on that date
        var existing = await _context.AttendanceRecords
            .FirstOrDefaultAsync(a => a.EmployeeId == request.EmployeeId && a.Date == date);

        if (existing != null)
            throw new InvalidOperationException($"Attendance record already exists for {employee.FullName} on {date:MMM dd, yyyy}.");

        decimal totalHours = 0;
        decimal breakMins = 0;
        var status = "ClockedIn";

        if (request.BreakStart.HasValue && request.BreakEnd.HasValue)
            breakMins = (decimal)(request.BreakEnd.Value - request.BreakStart.Value).TotalMinutes;
        else if (request.BreakMinutes > 0)
            breakMins = request.BreakMinutes;

        if (request.ClockOut.HasValue)
        {
            var totalSpan = request.ClockOut.Value - request.ClockIn;
            totalHours = Math.Round((decimal)totalSpan.TotalHours - (breakMins / 60m), 2);
            if (totalHours < 0) totalHours = 0;
            status = "ClockedOut";
        }

        var record = new AttendanceRecord
        {
            EmployeeId = request.EmployeeId,
            Date = date,
            ClockIn = request.ClockIn,
            ClockOut = request.ClockOut,
            BreakStart = request.BreakStart,
            BreakEnd = request.BreakEnd,
            TotalHours = totalHours,
            BreakMinutes = breakMins,
            Status = status,
            IsApproved = true, // Manual entries by admin are auto-approved
            ClockedInByUserId = createdByUserId,
            Notes = request.Notes ?? "Manual entry",
            CreatedAt = PhilippineTime.Now,
            UpdatedAt = PhilippineTime.Now
        };

        await _context.AttendanceRecords.AddAsync(record);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Manual attendance entry created for employee {EmployeeId} on {Date} by user {UserId}",
            request.EmployeeId, date, createdByUserId);

        return MapToAttendanceDto(record, employee);
    }

    private static AttendanceRecordDto MapToAttendanceDto(AttendanceRecord record, Employee employee)
    {
        return new AttendanceRecordDto
        {
            AttendanceId = record.AttendanceId,
            EmployeeId = record.EmployeeId,
            EmployeeName = employee.FullName,
            Position = employee.Position,
            Date = record.Date,
            ClockIn = record.ClockIn,
            ClockOut = record.ClockOut,
            BreakStart = record.BreakStart,
            BreakEnd = record.BreakEnd,
            TotalHours = record.TotalHours,
            BreakMinutes = record.BreakMinutes,
            Status = record.Status,
            IsApproved = record.IsApproved,
            ClockedInByUserId = record.ClockedInByUserId,
            Notes = record.Notes
        };
    }
}
