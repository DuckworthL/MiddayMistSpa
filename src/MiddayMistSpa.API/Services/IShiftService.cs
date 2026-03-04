using MiddayMistSpa.API.DTOs.Employee;

namespace MiddayMistSpa.API.Services;

public interface IShiftService
{
    #region Employee Shifts

    /// <summary>
    /// Create a recurring weekly shift for an employee
    /// </summary>
    Task<ShiftResponse> CreateShiftAsync(CreateShiftRequest request);

    /// <summary>
    /// Get all active shifts for an employee
    /// </summary>
    Task<List<ShiftResponse>> GetEmployeeShiftsAsync(int employeeId);

    /// <summary>
    /// Get the weekly schedule view for an employee
    /// </summary>
    Task<WeeklyShiftScheduleResponse> GetWeeklyScheduleAsync(int employeeId);

    /// <summary>
    /// Update an existing shift
    /// </summary>
    Task<ShiftResponse> UpdateShiftAsync(int shiftId, UpdateShiftRequest request);

    /// <summary>
    /// Delete a shift
    /// </summary>
    Task<bool> DeleteShiftAsync(int shiftId);

    /// <summary>
    /// Set a full weekly shift schedule for an employee (replaces existing)
    /// </summary>
    Task<List<ShiftResponse>> SetBulkShiftsAsync(BulkShiftRequest request);

    #endregion

    #region Shift Exceptions

    /// <summary>
    /// Create a shift exception (time off, sick leave, custom hours, emergency)
    /// </summary>
    Task<ShiftExceptionResponse> CreateShiftExceptionAsync(CreateShiftExceptionRequest request);

    /// <summary>
    /// Get shift exceptions for an employee within a date range
    /// </summary>
    Task<List<ShiftExceptionResponse>> GetEmployeeExceptionsAsync(int employeeId, DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Get all shift exceptions for a specific date
    /// </summary>
    Task<List<ShiftExceptionResponse>> GetExceptionsByDateAsync(DateTime date);

    /// <summary>
    /// Update a shift exception
    /// </summary>
    Task<ShiftExceptionResponse> UpdateShiftExceptionAsync(int exceptionId, UpdateShiftExceptionRequest request);

    /// <summary>
    /// Delete a shift exception
    /// </summary>
    Task<bool> DeleteShiftExceptionAsync(int exceptionId);

    #endregion

    #region Availability

    /// <summary>
    /// Get an employee's availability for a specific date (shift + exceptions combined)
    /// </summary>
    Task<EmployeeAvailabilityResponse> GetEmployeeAvailabilityAsync(int employeeId, DateTime date);

    /// <summary>
    /// Get all available staff for a specific date/time slot
    /// </summary>
    Task<List<EmployeeAvailabilityResponse>> GetAvailableStaffAsync(StaffAvailabilityRequest request);

    #endregion
}
