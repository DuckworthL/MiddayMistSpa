using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiddayMistSpa.API.DTOs.Settings;
using MiddayMistSpa.API.Services;

namespace MiddayMistSpa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Permission:settings.manage")]
[Produces("application/json")]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(ISettingsService settingsService, IPermissionService permissionService, ILogger<SettingsController> logger)
    {
        _settingsService = settingsService;
        _permissionService = permissionService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    // =========================================================================
    // General Settings
    // =========================================================================

    [HttpGet("general")]
    public async Task<ActionResult<GeneralSettingsDto>> GetGeneralSettings()
    {
        try
        {
            var settings = await _settingsService.GetGeneralSettingsAsync();
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting general settings");
            return StatusCode(500, new { message = "Failed to retrieve general settings" });
        }
    }

    [HttpPut("general")]
    public async Task<ActionResult<GeneralSettingsDto>> SaveGeneralSettings([FromBody] GeneralSettingsDto dto)
    {
        try
        {
            var result = await _settingsService.SaveGeneralSettingsAsync(dto, GetCurrentUserId());
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving general settings");
            return StatusCode(500, new { message = "Failed to save general settings" });
        }
    }

    // =========================================================================
    // Business Info
    // =========================================================================

    [HttpGet("business")]
    public async Task<ActionResult<BusinessInfoDto>> GetBusinessInfo()
    {
        try
        {
            var info = await _settingsService.GetBusinessInfoAsync();
            return Ok(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting business info");
            return StatusCode(500, new { message = "Failed to retrieve business info" });
        }
    }

    [HttpPut("business")]
    public async Task<ActionResult<BusinessInfoDto>> SaveBusinessInfo([FromBody] BusinessInfoDto dto)
    {
        try
        {
            var result = await _settingsService.SaveBusinessInfoAsync(dto, GetCurrentUserId());
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving business info");
            return StatusCode(500, new { message = "Failed to save business info" });
        }
    }

    // =========================================================================
    // Notification Settings
    // =========================================================================

    [HttpGet("notifications")]
    public async Task<ActionResult<NotificationSettingsDto>> GetNotificationSettings()
    {
        try
        {
            var settings = await _settingsService.GetNotificationSettingsAsync();
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification settings");
            return StatusCode(500, new { message = "Failed to retrieve notification settings" });
        }
    }

    [HttpPut("notifications")]
    public async Task<ActionResult<NotificationSettingsDto>> SaveNotificationSettings([FromBody] NotificationSettingsDto dto)
    {
        try
        {
            var result = await _settingsService.SaveNotificationSettingsAsync(dto, GetCurrentUserId());
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving notification settings");
            return StatusCode(500, new { message = "Failed to save notification settings" });
        }
    }

    // =========================================================================
    // User Management
    // =========================================================================

    [HttpGet("users")]
    public async Task<ActionResult<List<UserListResponse>>> GetUsers()
    {
        try
        {
            var users = await _settingsService.GetUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return StatusCode(500, new { message = "Failed to retrieve users" });
        }
    }

    [HttpPost("users")]
    public async Task<ActionResult<UserListResponse>> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var user = await _settingsService.CreateUserAsync(request);
            return CreatedAtAction(nameof(GetUsers), new { id = user.UserId }, user);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { message = "Failed to create user" });
        }
    }

    [HttpPut("users/{userId}")]
    public async Task<ActionResult<UserListResponse>> UpdateUser(int userId, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var user = await _settingsService.UpdateUserAsync(userId, request);
            return Ok(user);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            return StatusCode(500, new { message = "Failed to update user" });
        }
    }

    [HttpPut("users/{userId}/toggle-status")]
    public async Task<ActionResult<UserListResponse>> ToggleUserStatus(int userId)
    {
        try
        {
            var user = await _settingsService.ToggleUserStatusAsync(userId);
            return Ok(user);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling user status {UserId}", userId);
            return StatusCode(500, new { message = "Failed to toggle user status" });
        }
    }

    [HttpPost("users/{userId}/reset-password")]
    public async Task<ActionResult> ResetPassword(int userId, [FromBody] ResetPasswordRequest request)
    {
        try
        {
            await _settingsService.ResetUserPasswordAsync(userId, request);
            return Ok(new { message = "Password reset successful" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {UserId}", userId);
            return StatusCode(500, new { message = "Failed to reset password" });
        }
    }

    // =========================================================================
    // Role Management
    // =========================================================================

    [HttpGet("roles")]
    public async Task<ActionResult<List<RoleListResponse>>> GetRoles()
    {
        try
        {
            var roles = await _settingsService.GetRolesAsync();
            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles");
            return StatusCode(500, new { message = "Failed to retrieve roles" });
        }
    }

    [HttpPost("roles")]
    public async Task<ActionResult<RoleListResponse>> CreateRole([FromBody] CreateRoleRequest request)
    {
        try
        {
            var role = await _settingsService.CreateRoleAsync(request);
            return CreatedAtAction(nameof(GetRoles), new { id = role.RoleId }, role);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role");
            return StatusCode(500, new { message = "Failed to create role" });
        }
    }

    [HttpPut("roles/{roleId}/permissions")]
    public async Task<ActionResult<RoleListResponse>> UpdateRolePermissions(int roleId, [FromBody] UpdateRolePermissionsRequest request)
    {
        try
        {
            var role = await _settingsService.UpdateRolePermissionsAsync(roleId, request);
            // Invalidate permission cache so changes take effect immediately
            _permissionService.InvalidateCache(roleId);
            return Ok(role);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role permissions {RoleId}", roleId);
            return StatusCode(500, new { message = "Failed to update role permissions" });
        }
    }

    [HttpDelete("roles/{roleId}")]
    public async Task<ActionResult> DeleteRole(int roleId)
    {
        try
        {
            await _settingsService.DeleteRoleAsync(roleId);
            return Ok(new { message = "Role deleted successfully" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role {RoleId}", roleId);
            return StatusCode(500, new { message = "Failed to delete role" });
        }
    }

    // =========================================================================
    // Philippine Holidays
    // =========================================================================

    [HttpGet("holidays")]
    public async Task<ActionResult<List<HolidayResponse>>> GetHolidays([FromQuery] int? year)
    {
        try
        {
            var holidays = await _settingsService.GetHolidaysAsync(year);
            return Ok(holidays);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting holidays");
            return StatusCode(500, new { message = "Failed to retrieve holidays" });
        }
    }

    [HttpPost("holidays")]
    public async Task<ActionResult<HolidayResponse>> CreateHoliday([FromBody] CreateHolidayRequest request)
    {
        try
        {
            var holiday = await _settingsService.CreateHolidayAsync(request);
            return Ok(holiday);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating holiday");
            return StatusCode(500, new { message = "Failed to create holiday" });
        }
    }

    [HttpPut("holidays/{holidayId}")]
    public async Task<ActionResult<HolidayResponse>> UpdateHoliday(int holidayId, [FromBody] UpdateHolidayRequest request)
    {
        try
        {
            var holiday = await _settingsService.UpdateHolidayAsync(holidayId, request);
            return Ok(holiday);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating holiday {HolidayId}", holidayId);
            return StatusCode(500, new { message = "Failed to update holiday" });
        }
    }

    [HttpDelete("holidays/{holidayId}")]
    public async Task<ActionResult> DeleteHoliday(int holidayId)
    {
        try
        {
            await _settingsService.DeleteHolidayAsync(holidayId);
            return Ok(new { message = "Holiday deleted successfully" });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting holiday {HolidayId}", holidayId);
            return StatusCode(500, new { message = "Failed to delete holiday" });
        }
    }
}
