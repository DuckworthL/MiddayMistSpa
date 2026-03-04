using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.API.Services;

namespace MiddayMistSpa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShiftsController : ControllerBase
{
    private readonly IShiftService _shiftService;
    private readonly ILogger<ShiftsController> _logger;

    public ShiftsController(IShiftService shiftService, ILogger<ShiftsController> logger)
    {
        _shiftService = shiftService;
        _logger = logger;
    }

    #region Employee Shifts

    /// <summary>
    /// Create a recurring weekly shift for an employee
    /// </summary>
    [HttpPost("employee/{employeeId:int}")]
    [Authorize(Policy = "HRAccess")]
    public async Task<ActionResult<ShiftResponse>> CreateShift(int employeeId, [FromBody] CreateShiftRequest request)
    {
        try
        {
            if (request.EmployeeId != employeeId)
                return BadRequest(new { message = "Employee ID mismatch" });

            var shift = await _shiftService.CreateShiftAsync(request);
            return CreatedAtAction(nameof(GetEmployeeShifts), new { employeeId }, shift);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating shift for employee {EmployeeId}", employeeId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get all active shifts for an employee
    /// </summary>
    [HttpGet("employee/{employeeId:int}")]
    [Authorize(Policy = "AllStaff")]
    public async Task<ActionResult<List<ShiftResponse>>> GetEmployeeShifts(int employeeId)
    {
        try
        {
            var shifts = await _shiftService.GetEmployeeShiftsAsync(employeeId);
            return Ok(shifts);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get weekly schedule summary for an employee
    /// </summary>
    [HttpGet("employee/{employeeId:int}/weekly")]
    [Authorize(Policy = "AllStaff")]
    public async Task<ActionResult<WeeklyShiftScheduleResponse>> GetWeeklySchedule(int employeeId)
    {
        try
        {
            var schedule = await _shiftService.GetWeeklyScheduleAsync(employeeId);
            return Ok(schedule);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing shift
    /// </summary>
    [HttpPut("{shiftId:int}")]
    [Authorize(Policy = "HRAccess")]
    public async Task<ActionResult<ShiftResponse>> UpdateShift(int shiftId, [FromBody] UpdateShiftRequest request)
    {
        try
        {
            var shift = await _shiftService.UpdateShiftAsync(shiftId, request);
            return Ok(shift);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shift {ShiftId}", shiftId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a shift
    /// </summary>
    [HttpDelete("{shiftId:int}")]
    [Authorize(Policy = "HRAccess")]
    public async Task<ActionResult> DeleteShift(int shiftId)
    {
        var result = await _shiftService.DeleteShiftAsync(shiftId);
        if (!result)
            return NotFound(new { message = $"Shift with ID {shiftId} not found" });

        return Ok(new { message = "Shift deleted successfully" });
    }

    /// <summary>
    /// Set a full weekly shift schedule for an employee (replaces existing active shifts)
    /// </summary>
    [HttpPost("employee/{employeeId:int}/bulk")]
    [Authorize(Policy = "HRAccess")]
    public async Task<ActionResult<List<ShiftResponse>>> SetBulkShifts(int employeeId, [FromBody] BulkShiftRequest request)
    {
        try
        {
            if (request.EmployeeId != employeeId)
                return BadRequest(new { message = "Employee ID mismatch" });

            var shifts = await _shiftService.SetBulkShiftsAsync(request);
            return Ok(shifts);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting bulk shifts for employee {EmployeeId}", employeeId);
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    #region Shift Exceptions

    /// <summary>
    /// Create a shift exception (time off, sick leave, emergency, custom hours)
    /// </summary>
    [HttpPost("exceptions")]
    [Authorize(Policy = "HRAccess")]
    public async Task<ActionResult<ShiftExceptionResponse>> CreateShiftException([FromBody] CreateShiftExceptionRequest request)
    {
        try
        {
            var exception = await _shiftService.CreateShiftExceptionAsync(request);
            return CreatedAtAction(nameof(GetEmployeeExceptions), new { employeeId = request.EmployeeId }, exception);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating shift exception for employee {EmployeeId}", request.EmployeeId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get shift exceptions for an employee (optional date range filter)
    /// </summary>
    [HttpGet("exceptions/employee/{employeeId:int}")]
    [Authorize(Policy = "AllStaff")]
    public async Task<ActionResult<List<ShiftExceptionResponse>>> GetEmployeeExceptions(
        int employeeId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var exceptions = await _shiftService.GetEmployeeExceptionsAsync(employeeId, fromDate, toDate);
            return Ok(exceptions);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get all shift exceptions for a specific date
    /// </summary>
    [HttpGet("exceptions/date/{date:datetime}")]
    [Authorize(Policy = "AllStaff")]
    public async Task<ActionResult<List<ShiftExceptionResponse>>> GetExceptionsByDate(DateTime date)
    {
        var exceptions = await _shiftService.GetExceptionsByDateAsync(date);
        return Ok(exceptions);
    }

    /// <summary>
    /// Update a shift exception
    /// </summary>
    [HttpPut("exceptions/{exceptionId:int}")]
    [Authorize(Policy = "HRAccess")]
    public async Task<ActionResult<ShiftExceptionResponse>> UpdateShiftException(int exceptionId, [FromBody] UpdateShiftExceptionRequest request)
    {
        try
        {
            var exception = await _shiftService.UpdateShiftExceptionAsync(exceptionId, request);
            return Ok(exception);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shift exception {ExceptionId}", exceptionId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a shift exception
    /// </summary>
    [HttpDelete("exceptions/{exceptionId:int}")]
    [Authorize(Policy = "HRAccess")]
    public async Task<ActionResult> DeleteShiftException(int exceptionId)
    {
        var result = await _shiftService.DeleteShiftExceptionAsync(exceptionId);
        if (!result)
            return NotFound(new { message = $"Shift exception with ID {exceptionId} not found" });

        return Ok(new { message = "Shift exception deleted successfully" });
    }

    #endregion

    #region Availability

    /// <summary>
    /// Get an employee's availability for a specific date
    /// </summary>
    [HttpGet("availability/{employeeId:int}/{date:datetime}")]
    [Authorize(Policy = "AllStaff")]
    public async Task<ActionResult<EmployeeAvailabilityResponse>> GetEmployeeAvailability(int employeeId, DateTime date)
    {
        try
        {
            var availability = await _shiftService.GetEmployeeAvailabilityAsync(employeeId, date);
            return Ok(availability);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get all available staff for a specific date and optional time slot
    /// </summary>
    [HttpGet("available-staff")]
    [Authorize(Policy = "ReceptionistAccess")]
    public async Task<ActionResult<List<EmployeeAvailabilityResponse>>> GetAvailableStaff([FromQuery] StaffAvailabilityRequest request)
    {
        var staff = await _shiftService.GetAvailableStaffAsync(request);
        return Ok(staff);
    }

    #endregion
}
