using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.API.DTOs.TimeAttendance;

namespace MiddayMistSpa.API.Services;

public interface ITimeAttendanceService
{
    // ============================================================================
    // Employee Schedule Management
    // ============================================================================
    Task<ScheduleResponse> CreateScheduleAsync(CreateScheduleRequest request);
    Task<ScheduleResponse?> GetScheduleByIdAsync(int scheduleId);
    Task<List<ScheduleResponse>> GetEmployeeSchedulesAsync(int employeeId, DateTime? effectiveDate = null);
    Task<WeeklyScheduleResponse> GetWeeklyScheduleAsync(int employeeId, DateTime? asOfDate = null);
    Task<PagedResponse<ScheduleResponse>> SearchSchedulesAsync(ScheduleSearchRequest request);
    Task<ScheduleResponse> UpdateScheduleAsync(int scheduleId, UpdateScheduleRequest request);
    Task<bool> DeleteScheduleAsync(int scheduleId);
    Task<List<ScheduleResponse>> SetWeeklyScheduleAsync(int employeeId, List<CreateScheduleRequest> schedules);

    // ============================================================================
    // Time Off Request Management
    // ============================================================================
    Task<TimeOffResponse> CreateTimeOffRequestAsync(CreateTimeOffRequest request);
    Task<TimeOffResponse?> GetTimeOffRequestByIdAsync(int timeOffRequestId);
    Task<PagedResponse<TimeOffResponse>> SearchTimeOffRequestsAsync(TimeOffSearchRequest request);
    Task<TimeOffResponse> UpdateTimeOffRequestAsync(int timeOffRequestId, UpdateTimeOffRequest request);
    Task<TimeOffResponse> ApproveTimeOffRequestAsync(int timeOffRequestId, int approvedByUserId);
    Task<TimeOffResponse> RejectTimeOffRequestAsync(int timeOffRequestId, int rejectedByUserId, RejectTimeOffRequest request);
    Task<bool> CancelTimeOffRequestAsync(int timeOffRequestId, int cancelledByUserId);
    Task<List<TimeOffResponse>> GetPendingTimeOffRequestsAsync();

    // ============================================================================
    // Leave Balance Management
    // ============================================================================
    Task<LeaveBalanceResponse> CreateLeaveBalanceAsync(CreateLeaveBalanceRequest request);
    Task<LeaveBalanceResponse?> GetLeaveBalanceAsync(int employeeId, int year);
    Task<LeaveBalanceSummary> GetEmployeeLeaveBalancesAsync(int employeeId);
    Task<LeaveBalanceResponse> UpdateLeaveBalanceAsync(int leaveBalanceId, UpdateLeaveBalanceRequest request);
    Task<int> InitializeYearlyLeaveBalancesAsync(int year);
    Task<LeaveBalanceResponse> AdjustLeaveBalanceAsync(int employeeId, int year, string leaveType, decimal days, bool isAddition);

    // ============================================================================
    // Employee Advance Management
    // ============================================================================
    Task<AdvanceResponse> CreateAdvanceAsync(CreateAdvanceRequest request, int approvedByUserId);
    Task<AdvanceResponse?> GetAdvanceByIdAsync(int advanceId);
    Task<PagedResponse<AdvanceResponse>> SearchAdvancesAsync(AdvanceSearchRequest request);
    Task<AdvanceResponse> UpdateAdvanceAsync(int advanceId, UpdateAdvanceRequest request);
    Task<AdvanceResponse> RecordAdvancePaymentAsync(int advanceId, RecordPaymentRequest request);
    Task<List<AdvanceResponse>> GetActiveAdvancesForEmployeeAsync(int employeeId);

    // ============================================================================
    // Clock In/Out & Attendance Records
    // ============================================================================
    Task<AttendanceRecordDto> ClockInAsync(int employeeId, int? performedByUserId = null);
    Task<AttendanceRecordDto> ClockOutAsync(int employeeId);
    Task<AttendanceRecordDto> StartBreakAsync(int employeeId);
    Task<AttendanceRecordDto> EndBreakAsync(int employeeId);
    Task<List<AttendanceRecordDto>> GetAttendanceRecordsAsync(int? employeeId = null, DateTime? date = null);
    Task<List<LiveAttendanceStatusDto>> GetLiveStatusAsync();
    Task<AttendanceRecordDto> CreateManualEntryAsync(ManualAttendanceRequest request, int createdByUserId);
    Task<AttendanceRecordDto> ApproveAttendanceRecordAsync(int attendanceId, int approvedByUserId);

    // ============================================================================
    // Attendance Summary & Reports
    // ============================================================================
    Task<AttendanceSummaryResponse> GetAttendanceSummaryAsync(int employeeId, DateTime startDate, DateTime endDate);
    Task<TeamAttendanceResponse> GetTeamAttendanceAsync(DateTime startDate, DateTime endDate);
    Task<List<ScheduleCalendarDayResponse>> GetScheduleCalendarAsync(ScheduleCalendarRequest request);
}
