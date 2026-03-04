using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.API.DTOs.Notification;
using MiddayMistSpa.API.Services;

namespace MiddayMistSpa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    // ============================================================================
    // Notification CRUD
    // ============================================================================

    /// <summary>
    /// Create a new notification
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,Manager,Receptionist")]
    public async Task<ActionResult<NotificationResponse>> CreateNotification([FromBody] CreateNotificationRequest request)
    {
        try
        {
            var result = await _notificationService.CreateNotificationAsync(request);
            return CreatedAtAction(nameof(GetNotificationById), new { id = result.NotificationId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification");
            return StatusCode(500, "An error occurred while creating the notification");
        }
    }

    /// <summary>
    /// Get notification by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<NotificationResponse>> GetNotificationById(int id)
    {
        var notification = await _notificationService.GetNotificationByIdAsync(id);
        return notification != null ? Ok(notification) : NotFound();
    }

    /// <summary>
    /// Search notifications with filters
    /// </summary>
    [HttpGet("search")]
    [Authorize(Roles = "SuperAdmin,Admin,Manager")]
    public async Task<ActionResult<PagedResponse<NotificationResponse>>> SearchNotifications([FromQuery] NotificationSearchRequest request)
    {
        var result = await _notificationService.SearchNotificationsAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Get current user's notifications
    /// </summary>
    [HttpGet("my-notifications")]
    public async Task<ActionResult<List<NotificationResponse>>> GetMyNotifications([FromQuery] bool unreadOnly = false)
    {
        // In production, get userId from JWT claims
        var userId = GetCurrentUserId();
        var result = await _notificationService.GetUserNotificationsAsync(userId, unreadOnly);
        return Ok(result);
    }

    /// <summary>
    /// Get unread notification count for current user
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(count);
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    [HttpPut("{id}/read")]
    public async Task<ActionResult<NotificationResponse>> MarkAsRead(int id)
    {
        try
        {
            var result = await _notificationService.MarkAsReadAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Mark all notifications as read for current user
    /// </summary>
    [HttpPut("read-all")]
    public async Task<ActionResult<int>> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        var count = await _notificationService.MarkAllAsReadAsync(userId);
        return Ok(new { MarkedAsRead = count });
    }

    /// <summary>
    /// Delete a notification
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteNotification(int id)
    {
        var deleted = await _notificationService.DeleteNotificationAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    // ============================================================================
    // Email Operations
    // ============================================================================

    /// <summary>
    /// Send an email
    /// </summary>
    [HttpPost("email/send")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<SendResult>> SendEmail([FromBody] SendEmailRequest request)
    {
        var result = await _notificationService.SendEmailAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Send a templated email
    /// </summary>
    [HttpPost("email/send-templated")]
    [Authorize(Roles = "Admin,Manager,Receptionist")]
    public async Task<ActionResult<SendResult>> SendTemplatedEmail(
        [FromQuery] string templateCode,
        [FromQuery] string to,
        [FromBody] Dictionary<string, string> data)
    {
        var result = await _notificationService.SendTemplatedEmailAsync(templateCode, to, data);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ============================================================================
    // SMS Operations
    // ============================================================================

    /// <summary>
    /// Send an SMS
    /// </summary>
    [HttpPost("sms/send")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<SendResult>> SendSms([FromBody] SendSmsRequest request)
    {
        var result = await _notificationService.SendSmsAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Send a templated SMS
    /// </summary>
    [HttpPost("sms/send-templated")]
    [Authorize(Roles = "Admin,Manager,Receptionist")]
    public async Task<ActionResult<SendResult>> SendTemplatedSms(
        [FromQuery] string templateCode,
        [FromQuery] string phoneNumber,
        [FromBody] Dictionary<string, string> data)
    {
        var result = await _notificationService.SendTemplatedSmsAsync(templateCode, phoneNumber, data);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ============================================================================
    // Appointment Reminders
    // ============================================================================

    /// <summary>
    /// Send appointment reminder
    /// </summary>
    [HttpPost("appointments/{appointmentId}/reminder")]
    [Authorize(Roles = "Admin,Manager,Receptionist")]
    public async Task<ActionResult<ReminderResult>> SendAppointmentReminder(int appointmentId, [FromBody] AppointmentReminderRequest request)
    {
        try
        {
            request.AppointmentId = appointmentId;
            var result = await _notificationService.SendAppointmentReminderAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending appointment reminder for {AppointmentId}", appointmentId);
            return StatusCode(500, "An error occurred while sending the reminder");
        }
    }

    /// <summary>
    /// Send bulk appointment reminders for a date
    /// </summary>
    [HttpPost("appointments/bulk-reminders")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ReminderResult>> SendBulkReminders([FromBody] BulkReminderRequest request)
    {
        try
        {
            var result = await _notificationService.SendBulkAppointmentRemindersAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk reminders");
            return StatusCode(500, "An error occurred while sending bulk reminders");
        }
    }

    /// <summary>
    /// Send appointment confirmation
    /// </summary>
    [HttpPost("appointments/{appointmentId}/confirmation")]
    [Authorize(Roles = "Admin,Manager,Receptionist")]
    public async Task<ActionResult<ReminderResult>> SendAppointmentConfirmation(int appointmentId)
    {
        try
        {
            var result = await _notificationService.SendAppointmentConfirmationAsync(appointmentId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Send appointment cancellation notification
    /// </summary>
    [HttpPost("appointments/{appointmentId}/cancellation")]
    [Authorize(Roles = "Admin,Manager,Receptionist")]
    public async Task<ActionResult<ReminderResult>> SendAppointmentCancellation(int appointmentId, [FromQuery] string? reason = null)
    {
        try
        {
            var result = await _notificationService.SendAppointmentCancellationAsync(appointmentId, reason);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    // ============================================================================
    // Employee Notifications
    // ============================================================================

    /// <summary>
    /// Send payslip notification
    /// </summary>
    [HttpPost("employees/{employeeId}/payslip/{payrollRecordId}")]
    [Authorize(Roles = "Admin,Manager,HRManager")]
    public async Task<ActionResult<SendResult>> SendPayslipNotification(int employeeId, int payrollRecordId)
    {
        var result = await _notificationService.SendPayslipNotificationAsync(employeeId, payrollRecordId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Send schedule update notification
    /// </summary>
    [HttpPost("employees/{employeeId}/schedule-update")]
    [Authorize(Roles = "Admin,Manager,HRManager")]
    public async Task<ActionResult<SendResult>> SendScheduleUpdateNotification(int employeeId, [FromQuery] DateTime effectiveDate)
    {
        var result = await _notificationService.SendScheduleUpdateNotificationAsync(employeeId, effectiveDate);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Send time off approval notification
    /// </summary>
    [HttpPost("time-off/{timeOffRequestId}/approval")]
    [Authorize(Roles = "Admin,Manager,HRManager")]
    public async Task<ActionResult<SendResult>> SendTimeOffApprovalNotification(
        int timeOffRequestId,
        [FromQuery] bool approved,
        [FromQuery] string? reason = null)
    {
        var result = await _notificationService.SendTimeOffApprovalNotificationAsync(timeOffRequestId, approved, reason);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Send low leave balance notification
    /// </summary>
    [HttpPost("employees/{employeeId}/low-leave-balance")]
    [Authorize(Roles = "Admin,Manager,HRManager")]
    public async Task<ActionResult<SendResult>> SendLowLeaveBalanceNotification(int employeeId)
    {
        var result = await _notificationService.SendLowLeaveBalanceNotificationAsync(employeeId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ============================================================================
    // Inventory Notifications
    // ============================================================================

    /// <summary>
    /// Send low stock alert
    /// </summary>
    [HttpPost("inventory/{productId}/low-stock")]
    [Authorize(Roles = "Admin,Manager,InventoryManager")]
    public async Task<ActionResult<SendResult>> SendLowStockAlert(int productId)
    {
        var result = await _notificationService.SendLowStockAlertAsync(productId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Send product expiry alert
    /// </summary>
    [HttpPost("inventory/{productId}/expiry")]
    [Authorize(Roles = "Admin,Manager,InventoryManager")]
    public async Task<ActionResult<SendResult>> SendExpiryAlert(int productId, [FromQuery] DateTime expiryDate)
    {
        var result = await _notificationService.SendExpiryAlertAsync(productId, expiryDate);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Send all pending inventory alerts
    /// </summary>
    [HttpPost("inventory/bulk-alerts")]
    [Authorize(Roles = "Admin,Manager,InventoryManager")]
    public async Task<ActionResult<int>> SendBulkInventoryAlerts()
    {
        var count = await _notificationService.SendBulkInventoryAlertsAsync();
        return Ok(new { AlertsSent = count });
    }

    // ============================================================================
    // Marketing/Broadcast
    // ============================================================================

    /// <summary>
    /// Create a new broadcast
    /// </summary>
    [HttpPost("broadcasts")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<BroadcastResponse>> CreateBroadcast([FromBody] BroadcastRequest request)
    {
        try
        {
            var result = await _notificationService.CreateBroadcastAsync(request);
            return CreatedAtAction(nameof(GetBroadcastById), new { id = result.BroadcastId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating broadcast");
            return StatusCode(500, "An error occurred while creating the broadcast");
        }
    }

    /// <summary>
    /// Get broadcast by ID
    /// </summary>
    [HttpGet("broadcasts/{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<BroadcastResponse>> GetBroadcastById(int id)
    {
        var broadcast = await _notificationService.GetBroadcastByIdAsync(id);
        return broadcast != null ? Ok(broadcast) : NotFound();
    }

    /// <summary>
    /// Get all broadcasts
    /// </summary>
    [HttpGet("broadcasts")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<List<BroadcastResponse>>> GetBroadcasts(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var result = await _notificationService.GetBroadcastsAsync(startDate, endDate);
        return Ok(result);
    }

    /// <summary>
    /// Process a scheduled broadcast
    /// </summary>
    [HttpPost("broadcasts/{id}/process")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<BroadcastResponse>> ProcessBroadcast(int id)
    {
        try
        {
            var result = await _notificationService.ProcessBroadcastAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Cancel a broadcast
    /// </summary>
    [HttpPost("broadcasts/{id}/cancel")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> CancelBroadcast(int id)
    {
        try
        {
            var cancelled = await _notificationService.CancelBroadcastAsync(id);
            return cancelled ? Ok(new { Message = "Broadcast cancelled" }) : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // ============================================================================
    // Notification Preferences
    // ============================================================================

    /// <summary>
    /// Get notification preferences
    /// </summary>
    [HttpGet("preferences")]
    public async Task<ActionResult<NotificationPreferencesResponse>> GetPreferences(
        [FromQuery] int? userId = null,
        [FromQuery] int? customerId = null)
    {
        var result = await _notificationService.GetPreferencesAsync(userId, customerId);
        return Ok(result);
    }

    /// <summary>
    /// Update notification preferences
    /// </summary>
    [HttpPut("preferences/{id}")]
    public async Task<ActionResult<NotificationPreferencesResponse>> UpdatePreferences(
        int id,
        [FromBody] UpdateNotificationPreferencesRequest request)
    {
        try
        {
            var result = await _notificationService.UpdatePreferencesAsync(id, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Create default notification preferences
    /// </summary>
    [HttpPost("preferences")]
    public async Task<ActionResult<NotificationPreferencesResponse>> CreateDefaultPreferences(
        [FromQuery] int? userId = null,
        [FromQuery] int? customerId = null)
    {
        var result = await _notificationService.CreateDefaultPreferencesAsync(userId, customerId);
        return CreatedAtAction(nameof(GetPreferences), new { userId, customerId }, result);
    }

    // ============================================================================
    // Email Templates
    // ============================================================================

    /// <summary>
    /// Create email template
    /// </summary>
    [HttpPost("templates/email")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EmailTemplateResponse>> CreateEmailTemplate([FromBody] CreateEmailTemplateRequest request)
    {
        try
        {
            var result = await _notificationService.CreateEmailTemplateAsync(request);
            return CreatedAtAction(nameof(GetEmailTemplateById), new { id = result.TemplateId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Get email template by ID
    /// </summary>
    [HttpGet("templates/email/{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<EmailTemplateResponse>> GetEmailTemplateById(int id)
    {
        var template = await _notificationService.GetEmailTemplateByIdAsync(id);
        return template != null ? Ok(template) : NotFound();
    }

    /// <summary>
    /// Get email template by code
    /// </summary>
    [HttpGet("templates/email/by-code/{code}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<EmailTemplateResponse>> GetEmailTemplateByCode(string code)
    {
        var template = await _notificationService.GetEmailTemplateByCodeAsync(code);
        return template != null ? Ok(template) : NotFound();
    }

    /// <summary>
    /// Get all email templates
    /// </summary>
    [HttpGet("templates/email")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<List<EmailTemplateResponse>>> GetEmailTemplates([FromQuery] string? category = null)
    {
        var result = await _notificationService.GetEmailTemplatesAsync(category);
        return Ok(result);
    }

    /// <summary>
    /// Update email template
    /// </summary>
    [HttpPut("templates/email/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EmailTemplateResponse>> UpdateEmailTemplate(int id, [FromBody] UpdateEmailTemplateRequest request)
    {
        try
        {
            var result = await _notificationService.UpdateEmailTemplateAsync(id, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Delete email template
    /// </summary>
    [HttpDelete("templates/email/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteEmailTemplate(int id)
    {
        var deleted = await _notificationService.DeleteEmailTemplateAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    // ============================================================================
    // SMS Templates
    // ============================================================================

    /// <summary>
    /// Create SMS template
    /// </summary>
    [HttpPost("templates/sms")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SmsTemplateResponse>> CreateSmsTemplate([FromBody] CreateSmsTemplateRequest request)
    {
        try
        {
            var result = await _notificationService.CreateSmsTemplateAsync(request);
            return CreatedAtAction(nameof(GetSmsTemplateById), new { id = result.TemplateId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Get SMS template by ID
    /// </summary>
    [HttpGet("templates/sms/{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<SmsTemplateResponse>> GetSmsTemplateById(int id)
    {
        var template = await _notificationService.GetSmsTemplateByIdAsync(id);
        return template != null ? Ok(template) : NotFound();
    }

    /// <summary>
    /// Get SMS template by code
    /// </summary>
    [HttpGet("templates/sms/by-code/{code}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<SmsTemplateResponse>> GetSmsTemplateByCode(string code)
    {
        var template = await _notificationService.GetSmsTemplateByCodeAsync(code);
        return template != null ? Ok(template) : NotFound();
    }

    /// <summary>
    /// Get all SMS templates
    /// </summary>
    [HttpGet("templates/sms")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<List<SmsTemplateResponse>>> GetSmsTemplates([FromQuery] string? category = null)
    {
        var result = await _notificationService.GetSmsTemplatesAsync(category);
        return Ok(result);
    }

    /// <summary>
    /// Update SMS template
    /// </summary>
    [HttpPut("templates/sms/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SmsTemplateResponse>> UpdateSmsTemplate(int id, [FromBody] UpdateSmsTemplateRequest request)
    {
        try
        {
            var result = await _notificationService.UpdateSmsTemplateAsync(id, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Delete SMS template
    /// </summary>
    [HttpDelete("templates/sms/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteSmsTemplate(int id)
    {
        var deleted = await _notificationService.DeleteSmsTemplateAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    // ============================================================================
    // Statistics
    // ============================================================================

    /// <summary>
    /// Get notification statistics
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<NotificationStatsResponse>> GetNotificationStats([FromQuery] NotificationStatsRequest request)
    {
        var result = await _notificationService.GetNotificationStatsAsync(request);
        return Ok(result);
    }

    // ============================================================================
    // Helper Methods
    // ============================================================================

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("User identity not found");
        return userId;
    }
}
