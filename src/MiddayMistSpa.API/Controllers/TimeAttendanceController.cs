using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.API.DTOs.TimeAttendance;
using MiddayMistSpa.API.Services;
using System.Security.Claims;

namespace MiddayMistSpa.API.Controllers;

[ApiController]
[Route("api/time-attendance")]
[Authorize]
public class TimeAttendanceController : ControllerBase
{
    private readonly ITimeAttendanceService _timeAttendanceService;
    private readonly ILogger<TimeAttendanceController> _logger;

    public TimeAttendanceController(ITimeAttendanceService timeAttendanceService, ILogger<TimeAttendanceController> logger)
    {
        _timeAttendanceService = timeAttendanceService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("User identity not found");
        return userId;
    }

    // ============================================================================
    // Schedule Endpoints
    // ============================================================================

    [HttpPost("schedules")]
    [Authorize(Policy = "Permission:timeattendance.manage")]
    public async Task<ActionResult<ScheduleResponse>> CreateSchedule([FromBody] CreateScheduleRequest request)
    {
        try
        {
            var result = await _timeAttendanceService.CreateScheduleAsync(request);
            return CreatedAtAction(nameof(GetSchedule), new { id = result.ScheduleId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating schedule");
            return StatusCode(500, new { error = "An error occurred while creating the schedule" });
        }
    }

    [HttpGet("schedules/{id}")]
    [Authorize(Policy = "Permission:timeattendance.view")]
    public async Task<ActionResult<ScheduleResponse>> GetSchedule(int id)
    {
        var result = await _timeAttendanceService.GetScheduleByIdAsync(id);
        if (result == null)
            return NotFound(new { error = "Schedule not found" });

        return Ok(result);
    }

    [HttpGet("schedules")]
    [Authorize(Policy = "Permission:timeattendance.view")]
    public async Task<ActionResult<PagedResponse<ScheduleResponse>>> SearchSchedules([FromQuery] ScheduleSearchRequest request)
    {
        var result = await _timeAttendanceService.SearchSchedulesAsync(request);
        return Ok(result);
    }

    [HttpGet("employees/{employeeId}/schedules")]
    [Authorize(Policy = "Permission:timeattendance.view")]
    public async Task<ActionResult<List<ScheduleResponse>>> GetEmployeeSchedules(int employeeId, [FromQuery] DateTime? effectiveDate)
    {
        try
        {
            var result = await _timeAttendanceService.GetEmployeeSchedulesAsync(employeeId, effectiveDate);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("employees/{employeeId}/weekly-schedule")]
    [Authorize(Policy = "Permission:timeattendance.view")]
    public async Task<ActionResult<WeeklyScheduleResponse>> GetWeeklySchedule(int employeeId, [FromQuery] DateTime? asOfDate)
    {
        try
        {
            var result = await _timeAttendanceService.GetWeeklyScheduleAsync(employeeId, asOfDate);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPut("schedules/{id}")]
    [Authorize(Policy = "Permission:timeattendance.manage")]
    public async Task<ActionResult<ScheduleResponse>> UpdateSchedule(int id, [FromBody] UpdateScheduleRequest request)
    {
        try
        {
            var result = await _timeAttendanceService.UpdateScheduleAsync(id, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating schedule {ScheduleId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the schedule" });
        }
    }

    [HttpDelete("schedules/{id}")]
    [Authorize(Policy = "Permission:timeattendance.manage")]
    public async Task<ActionResult> DeleteSchedule(int id)
    {
        var result = await _timeAttendanceService.DeleteScheduleAsync(id);
        if (!result)
            return NotFound(new { error = "Schedule not found" });

        return NoContent();
    }

    [HttpPost("employees/{employeeId}/weekly-schedule")]
    [Authorize(Policy = "Permission:timeattendance.manage")]
    public async Task<ActionResult<List<ScheduleResponse>>> SetWeeklySchedule(int employeeId, [FromBody] List<CreateScheduleRequest> schedules)
    {
        try
        {
            var result = await _timeAttendanceService.SetWeeklyScheduleAsync(employeeId, schedules);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting weekly schedule for employee {EmployeeId}", employeeId);
            return StatusCode(500, new { error = "An error occurred while setting the weekly schedule" });
        }
    }

    // ============================================================================
    // Time Off Request Endpoints
    // ============================================================================

    [HttpPost("time-off")]
    public async Task<ActionResult<TimeOffResponse>> CreateTimeOffRequest([FromBody] CreateTimeOffRequest request)
    {
        try
        {
            var result = await _timeAttendanceService.CreateTimeOffRequestAsync(request);
            return CreatedAtAction(nameof(GetTimeOffRequest), new { id = result.TimeOffRequestId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating time off request");
            return StatusCode(500, new { error = "An error occurred while creating the time off request" });
        }
    }

    [HttpGet("time-off/{id}")]
    public async Task<ActionResult<TimeOffResponse>> GetTimeOffRequest(int id)
    {
        var result = await _timeAttendanceService.GetTimeOffRequestByIdAsync(id);
        if (result == null)
            return NotFound(new { error = "Time off request not found" });

        return Ok(result);
    }

    [HttpGet("time-off")]
    [Authorize(Policy = "Permission:timeattendance.manage")]
    public async Task<ActionResult<PagedResponse<TimeOffResponse>>> SearchTimeOffRequests([FromQuery] TimeOffSearchRequest request)
    {
        var result = await _timeAttendanceService.SearchTimeOffRequestsAsync(request);
        return Ok(result);
    }

    [HttpGet("time-off/pending")]
    [Authorize(Policy = "Permission:timeattendance.manage")]
    public async Task<ActionResult<List<TimeOffResponse>>> GetPendingTimeOffRequests()
    {
        var result = await _timeAttendanceService.GetPendingTimeOffRequestsAsync();
        return Ok(result);
    }

    [HttpPut("time-off/{id}")]
    public async Task<ActionResult<TimeOffResponse>> UpdateTimeOffRequest(int id, [FromBody] UpdateTimeOffRequest request)
    {
        try
        {
            var result = await _timeAttendanceService.UpdateTimeOffRequestAsync(id, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating time off request {TimeOffRequestId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the time off request" });
        }
    }

    [HttpPost("time-off/{id}/approve")]
    [Authorize(Policy = "Permission:timeattendance.manage")]
    public async Task<ActionResult<TimeOffResponse>> ApproveTimeOffRequest(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _timeAttendanceService.ApproveTimeOffRequestAsync(id, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving time off request {TimeOffRequestId}", id);
            return StatusCode(500, new { error = "An error occurred while approving the time off request" });
        }
    }

    [HttpPost("time-off/{id}/reject")]
    [Authorize(Policy = "Permission:timeattendance.manage")]
    public async Task<ActionResult<TimeOffResponse>> RejectTimeOffRequest(int id, [FromBody] RejectTimeOffRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _timeAttendanceService.RejectTimeOffRequestAsync(id, userId, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting time off request {TimeOffRequestId}", id);
            return StatusCode(500, new { error = "An error occurred while rejecting the time off request" });
        }
    }

    [HttpDelete("time-off/{id}")]
    public async Task<ActionResult> CancelTimeOffRequest(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _timeAttendanceService.CancelTimeOffRequestAsync(id, userId);
            if (!result)
                return NotFound(new { error = "Time off request not found" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling time off request {TimeOffRequestId}", id);
            return StatusCode(500, new { error = "An error occurred while cancelling the time off request" });
        }
    }

    // ============================================================================
    // Leave Balance Endpoints
    // ============================================================================

    [HttpPost("leave-balances")]
    [Authorize(Policy = "Permission:timeattendance.manage")]
    public async Task<ActionResult<LeaveBalanceResponse>> CreateLeaveBalance([FromBody] CreateLeaveBalanceRequest request)
    {
        try
        {
            var result = await _timeAttendanceService.CreateLeaveBalanceAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating leave balance");
            return StatusCode(500, new { error = "An error occurred while creating the leave balance" });
        }
    }

    [HttpGet("employees/{employeeId}/leave-balance")]
    public async Task<ActionResult<LeaveBalanceResponse>> GetLeaveBalance(int employeeId, [FromQuery] int? year)
    {
        var result = await _timeAttendanceService.GetLeaveBalanceAsync(employeeId, year ?? DateTime.UtcNow.Year);
        if (result == null)
            return NotFound(new { error = "Leave balance not found" });

        return Ok(result);
    }

    [HttpGet("employees/{employeeId}/leave-summary")]
    public async Task<ActionResult<LeaveBalanceSummary>> GetEmployeeLeaveBalances(int employeeId)
    {
        try
        {
            var result = await _timeAttendanceService.GetEmployeeLeaveBalancesAsync(employeeId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPut("leave-balances/{id}")]
    [Authorize(Policy = "Permission:timeattendance.manage")]
    public async Task<ActionResult<LeaveBalanceResponse>> UpdateLeaveBalance(int id, [FromBody] UpdateLeaveBalanceRequest request)
    {
        try
        {
            var result = await _timeAttendanceService.UpdateLeaveBalanceAsync(id, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating leave balance {LeaveBalanceId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the leave balance" });
        }
    }

    [HttpPost("leave-balances/initialize")]
    [Authorize(Policy = "Permission:timeattendance.manage")]
    public async Task<ActionResult> InitializeYearlyLeaveBalances([FromQuery] int? year)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var count = await _timeAttendanceService.InitializeYearlyLeaveBalancesAsync(targetYear);
        return Ok(new { message = $"Initialized leave balances for {count} employees for year {targetYear}" });
    }

    [HttpPost("employees/{employeeId}/leave-balance/adjust")]
    [Authorize(Policy = "Permission:timeattendance.manage")]
    public async Task<ActionResult<LeaveBalanceResponse>> AdjustLeaveBalance(
        int employeeId,
        [FromQuery] int year,
        [FromQuery] string leaveType,
        [FromQuery] decimal days,
        [FromQuery] bool isAddition = true)
    {
        try
        {
            if (year == 0) year = DateTime.UtcNow.Year;
            var result = await _timeAttendanceService.AdjustLeaveBalanceAsync(employeeId, year, leaveType, days, isAddition);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ============================================================================
    // Advance Endpoints
    // ============================================================================

    [HttpPost("advances")]
    [Authorize(Policy = "Permission:accounting.manage")]
    public async Task<ActionResult<AdvanceResponse>> CreateAdvance([FromBody] CreateAdvanceRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _timeAttendanceService.CreateAdvanceAsync(request, userId);
            return CreatedAtAction(nameof(GetAdvance), new { id = result.AdvanceId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating advance");
            return StatusCode(500, new { error = "An error occurred while creating the advance" });
        }
    }

    [HttpGet("advances/{id}")]
    [Authorize(Policy = "Permission:accounting.view")]
    public async Task<ActionResult<AdvanceResponse>> GetAdvance(int id)
    {
        var result = await _timeAttendanceService.GetAdvanceByIdAsync(id);
        if (result == null)
            return NotFound(new { error = "Advance not found" });

        return Ok(result);
    }

    [HttpGet("advances")]
    [Authorize(Policy = "Permission:accounting.view")]
    public async Task<ActionResult<PagedResponse<AdvanceResponse>>> SearchAdvances([FromQuery] AdvanceSearchRequest request)
    {
        var result = await _timeAttendanceService.SearchAdvancesAsync(request);
        return Ok(result);
    }

    [HttpGet("employees/{employeeId}/advances")]
    [Authorize(Policy = "Permission:accounting.view")]
    public async Task<ActionResult<List<AdvanceResponse>>> GetEmployeeAdvances(int employeeId)
    {
        var result = await _timeAttendanceService.GetActiveAdvancesForEmployeeAsync(employeeId);
        return Ok(result);
    }

    [HttpPut("advances/{id}")]
    [Authorize(Policy = "Permission:accounting.manage")]
    public async Task<ActionResult<AdvanceResponse>> UpdateAdvance(int id, [FromBody] UpdateAdvanceRequest request)
    {
        try
        {
            var result = await _timeAttendanceService.UpdateAdvanceAsync(id, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating advance {AdvanceId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the advance" });
        }
    }

    [HttpPost("advances/{id}/payment")]
    [Authorize(Policy = "Permission:accounting.manage")]
    public async Task<ActionResult<AdvanceResponse>> RecordAdvancePayment(int id, [FromBody] RecordPaymentRequest request)
    {
        try
        {
            var result = await _timeAttendanceService.RecordAdvancePaymentAsync(id, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording advance payment {AdvanceId}", id);
            return StatusCode(500, new { error = "An error occurred while recording the advance payment" });
        }
    }

    // ============================================================================
    // Attendance Report Endpoints
    // ============================================================================

    [HttpGet("employees/{employeeId}/attendance-summary")]
    [Authorize(Policy = "Permission:timeattendance.view")]
    public async Task<ActionResult<AttendanceSummaryResponse>> GetAttendanceSummary(
        int employeeId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            var result = await _timeAttendanceService.GetAttendanceSummaryAsync(employeeId, startDate, endDate);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendance summary for employee {EmployeeId}", employeeId);
            return StatusCode(500, new { error = "An error occurred while getting the attendance summary" });
        }
    }

    [HttpGet("team-attendance")]
    [Authorize(Policy = "Permission:timeattendance.manage")]
    public async Task<ActionResult<TeamAttendanceResponse>> GetTeamAttendance(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            var result = await _timeAttendanceService.GetTeamAttendanceAsync(startDate, endDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team attendance");
            return StatusCode(500, new { error = "An error occurred while getting the team attendance" });
        }
    }

    [HttpGet("schedule-calendar")]
    [Authorize(Policy = "Permission:timeattendance.view")]
    public async Task<ActionResult<List<ScheduleCalendarDayResponse>>> GetScheduleCalendar([FromQuery] ScheduleCalendarRequest request)
    {
        try
        {
            var result = await _timeAttendanceService.GetScheduleCalendarAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting schedule calendar");
            return StatusCode(500, new { error = "An error occurred while getting the schedule calendar" });
        }
    }

    // ============================================================================
    // Clock In/Out Endpoints
    // ============================================================================

    [HttpPost("employees/{employeeId}/clock-in")]
    [Authorize(Policy = "Permission:timeattendance.view")]
    public async Task<ActionResult<AttendanceRecordDto>> ClockIn(int employeeId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _timeAttendanceService.ClockInAsync(employeeId, currentUserId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clocking in employee {EmployeeId}", employeeId);
            return StatusCode(500, new { error = "An error occurred while clocking in" });
        }
    }

    [HttpPost("employees/{employeeId}/clock-out")]
    [Authorize(Policy = "Permission:timeattendance.view")]
    public async Task<ActionResult<AttendanceRecordDto>> ClockOut(int employeeId)
    {
        try
        {
            var result = await _timeAttendanceService.ClockOutAsync(employeeId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clocking out employee {EmployeeId}", employeeId);
            return StatusCode(500, new { error = "An error occurred while clocking out" });
        }
    }

    [HttpPost("employees/{employeeId}/start-break")]
    [Authorize(Policy = "Permission:timeattendance.view")]
    public async Task<ActionResult<AttendanceRecordDto>> StartBreak(int employeeId)
    {
        try
        {
            var result = await _timeAttendanceService.StartBreakAsync(employeeId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting break for employee {EmployeeId}", employeeId);
            return StatusCode(500, new { error = "An error occurred while starting break" });
        }
    }

    [HttpPost("employees/{employeeId}/end-break")]
    [Authorize(Policy = "Permission:timeattendance.view")]
    public async Task<ActionResult<AttendanceRecordDto>> EndBreak(int employeeId)
    {
        try
        {
            var result = await _timeAttendanceService.EndBreakAsync(employeeId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending break for employee {EmployeeId}", employeeId);
            return StatusCode(500, new { error = "An error occurred while ending break" });
        }
    }

    [HttpPost("attendance/{id}/approve")]
    [Authorize(Policy = "Permission:timeattendance.manage")]
    public async Task<ActionResult<AttendanceRecordDto>> ApproveAttendanceRecord(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _timeAttendanceService.ApproveAttendanceRecordAsync(id, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving attendance record {AttendanceId}", id);
            return StatusCode(500, new { error = "An error occurred while approving the attendance record" });
        }
    }

    [HttpGet("attendance")]
    [Authorize(Policy = "Permission:timeattendance.view")]
    public async Task<ActionResult<List<AttendanceRecordDto>>> GetAttendanceRecords(
        [FromQuery] int? employeeId = null,
        [FromQuery] DateTime? date = null)
    {
        try
        {
            var result = await _timeAttendanceService.GetAttendanceRecordsAsync(employeeId, date);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendance records");
            return StatusCode(500, new { error = "An error occurred while getting attendance records" });
        }
    }

    [HttpGet("live-status")]
    [Authorize(Policy = "Permission:timeattendance.view")]
    public async Task<ActionResult<List<LiveAttendanceStatusDto>>> GetLiveStatus()
    {
        try
        {
            var result = await _timeAttendanceService.GetLiveStatusAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting live attendance status");
            return StatusCode(500, new { error = "An error occurred while getting live status" });
        }
    }

    [HttpPost("manual-entry")]
    [Authorize(Policy = "Permission:timeattendance.manage")]
    public async Task<ActionResult<AttendanceRecordDto>> CreateManualEntry([FromBody] ManualAttendanceRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _timeAttendanceService.CreateManualEntryAsync(request, currentUserId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating manual attendance entry");
            return StatusCode(500, new { error = "An error occurred while creating manual entry" });
        }
    }
}
