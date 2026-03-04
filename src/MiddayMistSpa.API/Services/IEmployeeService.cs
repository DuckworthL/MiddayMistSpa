using MiddayMistSpa.API.DTOs.Employee;

namespace MiddayMistSpa.API.Services;

public interface IEmployeeService
{
    #region Employee CRUD

    Task<EmployeeResponse> CreateEmployeeAsync(CreateEmployeeRequest request);
    Task<EmployeeResponse?> GetEmployeeByIdAsync(int employeeId);
    Task<EmployeeResponse?> GetEmployeeByCodeAsync(string employeeCode);
    Task<PagedResponse<EmployeeListResponse>> GetEmployeesAsync(PagedRequest request);
    Task<List<EmployeeListResponse>> GetActiveEmployeesAsync();
    Task<List<EmployeeListResponse>> GetTherapistsAsync();
    Task<EmployeeResponse> UpdateEmployeeAsync(int employeeId, UpdateEmployeeRequest request);
    Task<bool> DeactivateEmployeeAsync(int employeeId);
    Task<bool> ReactivateEmployeeAsync(int employeeId);

    #endregion

    #region Schedules

    Task<ScheduleResponse> CreateScheduleAsync(CreateScheduleRequest request);
    Task<List<ScheduleResponse>> GetEmployeeSchedulesAsync(int employeeId);
    Task<ScheduleResponse> UpdateScheduleAsync(int scheduleId, UpdateScheduleRequest request);
    Task<bool> DeleteScheduleAsync(int scheduleId);
    Task<List<ScheduleResponse>> SetBulkScheduleAsync(BulkScheduleRequest request);
    Task<List<EmployeeListResponse>> GetAvailableTherapistsAsync(DateTime date, TimeSpan startTime, TimeSpan endTime);

    #endregion

    #region Time Off Requests

    Task<TimeOffResponse> CreateTimeOffRequestAsync(CreateTimeOffRequest request);
    Task<List<TimeOffResponse>> GetEmployeeTimeOffRequestsAsync(int employeeId);
    Task<PagedResponse<TimeOffResponse>> GetPendingTimeOffRequestsAsync(PagedRequest request);
    Task<TimeOffResponse> ApproveOrRejectTimeOffAsync(int timeOffRequestId, ApproveTimeOffRequest request, int approvedByUserId);
    Task<bool> CancelTimeOffRequestAsync(int timeOffRequestId);

    #endregion

    #region Leave Balances

    Task<LeaveBalanceResponse?> GetEmployeeLeaveBalanceAsync(int employeeId, int year);
    Task<LeaveBalanceResponse> InitializeLeaveBalanceAsync(int employeeId, int year);
    Task<LeaveBalanceResponse> UpdateLeaveBalanceAsync(int employeeId, int year, UpdateLeaveBalanceRequest request);

    #endregion

    #region Advances/Loans

    Task<AdvanceResponse> CreateAdvanceAsync(CreateAdvanceRequest request, int approvedByUserId);
    Task<List<AdvanceResponse>> GetEmployeeAdvancesAsync(int employeeId);
    Task<List<AdvanceResponse>> GetActiveAdvancesAsync();
    Task<AdvanceResponse> RecordAdvancePaymentAsync(int advanceId, decimal amount);

    #endregion
}
