using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiddayMistSpa.API.DTOs.Common;
using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.API.Services;

namespace MiddayMistSpa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(IEmployeeService employeeService, ILogger<EmployeesController> logger)
    {
        _employeeService = employeeService;
        _logger = logger;
    }

    #region Employee CRUD

    /// <summary>
    /// Create a new employee
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "Permission:employees.create")]
    public async Task<ActionResult<EmployeeResponse>> CreateEmployee([FromBody] CreateEmployeeRequest request)
    {
        try
        {
            var employee = await _employeeService.CreateEmployeeAsync(request);
            return CreatedAtAction(nameof(GetEmployee), new { id = employee.EmployeeId }, employee);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating employee");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get employee by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "AllStaff")]
    public async Task<ActionResult<EmployeeResponse>> GetEmployee(int id)
    {
        var employee = await _employeeService.GetEmployeeByIdAsync(id);
        if (employee == null)
            return NotFound(new { message = $"Employee with ID {id} not found" });

        return Ok(employee);
    }

    /// <summary>
    /// Get employee by code
    /// </summary>
    [HttpGet("code/{code}")]
    [Authorize(Policy = "AllStaff")]
    public async Task<ActionResult<EmployeeResponse>> GetEmployeeByCode(string code)
    {
        var employee = await _employeeService.GetEmployeeByCodeAsync(code);
        if (employee == null)
            return NotFound(new { message = $"Employee with code {code} not found" });

        return Ok(employee);
    }

    /// <summary>
    /// Get paginated list of employees
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AllStaff")]
    public async Task<ActionResult<PagedResponse<EmployeeListResponse>>> GetEmployees([FromQuery] PagedRequest request)
    {
        var result = await _employeeService.GetEmployeesAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Get all active employees
    /// </summary>
    [HttpGet("active")]
    [Authorize(Policy = "AllStaff")]
    public async Task<ActionResult<List<EmployeeListResponse>>> GetActiveEmployees()
    {
        var employees = await _employeeService.GetActiveEmployeesAsync();
        return Ok(employees);
    }

    /// <summary>
    /// Get all therapists
    /// </summary>
    [HttpGet("therapists")]
    [Authorize(Policy = "AllStaff")]
    public async Task<ActionResult<List<LookupItemDto>>> GetTherapists()
    {
        var therapists = await _employeeService.GetTherapistsAsync();
        var lookup = therapists.Select(t => new LookupItemDto
        {
            Id = t.EmployeeId,
            Name = t.FullName,
            Code = t.EmployeeCode
        }).ToList();
        return Ok(lookup);
    }

    /// <summary>
    /// Update employee
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "Permission:employees.edit")]
    public async Task<ActionResult<EmployeeResponse>> UpdateEmployee(int id, [FromBody] UpdateEmployeeRequest request)
    {
        try
        {
            var employee = await _employeeService.UpdateEmployeeAsync(id, request);
            return Ok(employee);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating employee {EmployeeId}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deactivate employee (soft delete)
    /// </summary>
    [HttpPost("{id:int}/deactivate")]
    [Authorize(Policy = "Permission:employees.delete")]
    public async Task<ActionResult> DeactivateEmployee(int id)
    {
        var result = await _employeeService.DeactivateEmployeeAsync(id);
        if (!result)
            return NotFound(new { message = $"Employee with ID {id} not found" });

        return Ok(new { message = "Employee deactivated successfully" });
    }

    /// <summary>
    /// Reactivate employee
    /// </summary>
    [HttpPost("{id:int}/reactivate")]
    [Authorize(Policy = "Permission:employees.edit")]
    public async Task<ActionResult> ReactivateEmployee(int id)
    {
        var result = await _employeeService.ReactivateEmployeeAsync(id);
        if (!result)
            return NotFound(new { message = $"Employee with ID {id} not found" });

        return Ok(new { message = "Employee reactivated successfully" });
    }

    #endregion

    #region Schedules

    /// <summary>
    /// Create a schedule for an employee
    /// </summary>
    [HttpPost("{employeeId:int}/schedules")]
    [Authorize(Policy = "Permission:employees.edit")]
    public async Task<ActionResult<ScheduleResponse>> CreateSchedule(int employeeId, [FromBody] CreateScheduleRequest request)
    {
        try
        {
            if (request.EmployeeId != employeeId)
                return BadRequest(new { message = "Employee ID mismatch" });

            var schedule = await _employeeService.CreateScheduleAsync(request);
            return CreatedAtAction(nameof(GetEmployeeSchedules), new { employeeId }, schedule);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating schedule for employee {EmployeeId}", employeeId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get all active schedules for an employee
    /// </summary>
    [HttpGet("{employeeId:int}/schedules")]
    [Authorize(Policy = "AllStaff")]
    public async Task<ActionResult<List<ScheduleResponse>>> GetEmployeeSchedules(int employeeId)
    {
        try
        {
            var schedules = await _employeeService.GetEmployeeSchedulesAsync(employeeId);
            return Ok(schedules);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update a schedule
    /// </summary>
    [HttpPut("schedules/{scheduleId:int}")]
    [Authorize(Policy = "Permission:employees.edit")]
    public async Task<ActionResult<ScheduleResponse>> UpdateSchedule(int scheduleId, [FromBody] UpdateScheduleRequest request)
    {
        try
        {
            var schedule = await _employeeService.UpdateScheduleAsync(scheduleId, request);
            return Ok(schedule);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a schedule
    /// </summary>
    [HttpDelete("schedules/{scheduleId:int}")]
    [Authorize(Policy = "Permission:employees.delete")]
    public async Task<ActionResult> DeleteSchedule(int scheduleId)
    {
        var result = await _employeeService.DeleteScheduleAsync(scheduleId);
        if (!result)
            return NotFound(new { message = $"Schedule with ID {scheduleId} not found" });

        return Ok(new { message = "Schedule deleted successfully" });
    }

    /// <summary>
    /// Set bulk schedule for an employee (weekly schedule)
    /// </summary>
    [HttpPost("{employeeId:int}/schedules/bulk")]
    [Authorize(Policy = "Permission:employees.edit")]
    public async Task<ActionResult<List<ScheduleResponse>>> SetBulkSchedule(int employeeId, [FromBody] BulkScheduleRequest request)
    {
        try
        {
            if (request.EmployeeId != employeeId)
                return BadRequest(new { message = "Employee ID mismatch" });

            var schedules = await _employeeService.SetBulkScheduleAsync(request);
            return Ok(schedules);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get available therapists for a time slot
    /// </summary>
    [HttpGet("therapists/available")]
    [Authorize(Policy = "Permission:appointments.view")]
    public async Task<ActionResult<List<EmployeeListResponse>>> GetAvailableTherapists(
        [FromQuery] DateTime date,
        [FromQuery] TimeSpan startTime,
        [FromQuery] TimeSpan endTime)
    {
        var therapists = await _employeeService.GetAvailableTherapistsAsync(date, startTime, endTime);
        return Ok(therapists);
    }

    #endregion

    #region Time Off Requests

    /// <summary>
    /// Create a time off request
    /// </summary>
    [HttpPost("{employeeId:int}/time-off")]
    [Authorize(Policy = "AllStaff")]
    public async Task<ActionResult<TimeOffResponse>> CreateTimeOffRequest(int employeeId, [FromBody] CreateTimeOffRequest request)
    {
        try
        {
            if (request.EmployeeId != employeeId)
                return BadRequest(new { message = "Employee ID mismatch" });

            var timeOff = await _employeeService.CreateTimeOffRequestAsync(request);
            return CreatedAtAction(nameof(GetEmployeeTimeOffRequests), new { employeeId }, timeOff);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get time off requests for an employee
    /// </summary>
    [HttpGet("{employeeId:int}/time-off")]
    [Authorize(Policy = "AllStaff")]
    public async Task<ActionResult<List<TimeOffResponse>>> GetEmployeeTimeOffRequests(int employeeId)
    {
        try
        {
            var requests = await _employeeService.GetEmployeeTimeOffRequestsAsync(employeeId);
            return Ok(requests);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get all pending time off requests
    /// </summary>
    [HttpGet("time-off/pending")]
    [Authorize(Policy = "Permission:employees.edit")]
    public async Task<ActionResult<PagedResponse<TimeOffResponse>>> GetPendingTimeOffRequests([FromQuery] PagedRequest request)
    {
        var result = await _employeeService.GetPendingTimeOffRequestsAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Approve or reject a time off request
    /// </summary>
    [HttpPost("time-off/{timeOffId:int}/review")]
    [Authorize(Policy = "Permission:employees.edit")]
    public async Task<ActionResult<TimeOffResponse>> ReviewTimeOffRequest(int timeOffId, [FromBody] ApproveTimeOffRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var timeOff = await _employeeService.ApproveOrRejectTimeOffAsync(timeOffId, request, userId.Value);
            return Ok(timeOff);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cancel a time off request
    /// </summary>
    [HttpDelete("time-off/{timeOffId:int}")]
    [Authorize(Policy = "AllStaff")]
    public async Task<ActionResult> CancelTimeOffRequest(int timeOffId)
    {
        var result = await _employeeService.CancelTimeOffRequestAsync(timeOffId);
        if (!result)
            return NotFound(new { message = $"Time off request with ID {timeOffId} not found" });

        return Ok(new { message = "Time off request cancelled successfully" });
    }

    #endregion

    #region Leave Balances

    /// <summary>
    /// Get leave balance for an employee
    /// </summary>
    [HttpGet("{employeeId:int}/leave-balance/{year:int}")]
    [Authorize(Policy = "AllStaff")]
    public async Task<ActionResult<LeaveBalanceResponse>> GetLeaveBalance(int employeeId, int year)
    {
        var balance = await _employeeService.GetEmployeeLeaveBalanceAsync(employeeId, year);
        if (balance == null)
            return NotFound(new { message = $"Leave balance not found for employee {employeeId} year {year}" });

        return Ok(balance);
    }

    /// <summary>
    /// Initialize leave balance for an employee
    /// </summary>
    [HttpPost("{employeeId:int}/leave-balance/{year:int}")]
    [Authorize(Policy = "Permission:employees.edit")]
    public async Task<ActionResult<LeaveBalanceResponse>> InitializeLeaveBalance(int employeeId, int year)
    {
        try
        {
            var balance = await _employeeService.InitializeLeaveBalanceAsync(employeeId, year);
            return Ok(balance);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update leave balance for an employee
    /// </summary>
    [HttpPut("{employeeId:int}/leave-balance/{year:int}")]
    [Authorize(Policy = "Permission:employees.edit")]
    public async Task<ActionResult<LeaveBalanceResponse>> UpdateLeaveBalance(int employeeId, int year, [FromBody] UpdateLeaveBalanceRequest request)
    {
        try
        {
            var balance = await _employeeService.UpdateLeaveBalanceAsync(employeeId, year, request);
            return Ok(balance);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    #endregion

    #region Advances/Loans

    /// <summary>
    /// Create an advance/loan for an employee
    /// </summary>
    [HttpPost("{employeeId:int}/advances")]
    [Authorize(Policy = "Permission:accounting.manage")]
    public async Task<ActionResult<AdvanceResponse>> CreateAdvance(int employeeId, [FromBody] CreateAdvanceRequest request)
    {
        try
        {
            if (request.EmployeeId != employeeId)
                return BadRequest(new { message = "Employee ID mismatch" });

            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var advance = await _employeeService.CreateAdvanceAsync(request, userId.Value);
            return CreatedAtAction(nameof(GetEmployeeAdvances), new { employeeId }, advance);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get advances for an employee
    /// </summary>
    [HttpGet("{employeeId:int}/advances")]
    [Authorize(Policy = "Permission:accounting.view")]
    public async Task<ActionResult<List<AdvanceResponse>>> GetEmployeeAdvances(int employeeId)
    {
        try
        {
            var advances = await _employeeService.GetEmployeeAdvancesAsync(employeeId);
            return Ok(advances);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get all active advances
    /// </summary>
    [HttpGet("advances/active")]
    [Authorize(Policy = "Permission:accounting.view")]
    public async Task<ActionResult<List<AdvanceResponse>>> GetActiveAdvances()
    {
        var advances = await _employeeService.GetActiveAdvancesAsync();
        return Ok(advances);
    }

    /// <summary>
    /// Record a payment for an advance
    /// </summary>
    [HttpPost("advances/{advanceId:int}/payment")]
    [Authorize(Policy = "Permission:accounting.manage")]
    public async Task<ActionResult<AdvanceResponse>> RecordAdvancePayment(int advanceId, [FromBody] decimal amount)
    {
        try
        {
            var advance = await _employeeService.RecordAdvancePaymentAsync(advanceId, amount);
            return Ok(advance);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    #endregion

    #region Private Helpers

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    #endregion
}
