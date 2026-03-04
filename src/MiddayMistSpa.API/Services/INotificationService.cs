using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.API.DTOs.Notification;

namespace MiddayMistSpa.API.Services;

public interface INotificationService
{
    // ============================================================================
    // Notification CRUD
    // ============================================================================
    Task<NotificationResponse> CreateNotificationAsync(CreateNotificationRequest request);
    Task<NotificationResponse?> GetNotificationByIdAsync(int notificationId);
    Task<PagedResponse<NotificationResponse>> SearchNotificationsAsync(NotificationSearchRequest request);
    Task<List<NotificationResponse>> GetUserNotificationsAsync(int userId, bool unreadOnly = false);
    Task<NotificationResponse> MarkAsReadAsync(int notificationId);
    Task<int> MarkAllAsReadAsync(int userId);
    Task<bool> DeleteNotificationAsync(int notificationId);
    Task<int> GetUnreadCountAsync(int userId);

    // ============================================================================
    // Email Operations
    // ============================================================================
    Task<SendResult> SendEmailAsync(SendEmailRequest request);
    Task<SendResult> SendTemplatedEmailAsync(string templateCode, string to, Dictionary<string, string> data);

    // ============================================================================
    // SMS Operations
    // ============================================================================
    Task<SendResult> SendSmsAsync(SendSmsRequest request);
    Task<SendResult> SendTemplatedSmsAsync(string templateCode, string phoneNumber, Dictionary<string, string> data);

    // ============================================================================
    // Appointment Reminders
    // ============================================================================
    Task<ReminderResult> SendAppointmentReminderAsync(AppointmentReminderRequest request);
    Task<ReminderResult> SendBulkAppointmentRemindersAsync(BulkReminderRequest request);
    Task<ReminderResult> SendAppointmentConfirmationAsync(int appointmentId);
    Task<ReminderResult> SendAppointmentCancellationAsync(int appointmentId, string? reason);

    // ============================================================================
    // Employee Notifications
    // ============================================================================
    Task<SendResult> SendPayslipNotificationAsync(int employeeId, int payrollRecordId);
    Task<SendResult> SendScheduleUpdateNotificationAsync(int employeeId, DateTime effectiveDate);
    Task<SendResult> SendTimeOffApprovalNotificationAsync(int timeOffRequestId, bool approved, string? reason);
    Task<SendResult> SendLowLeaveBalanceNotificationAsync(int employeeId);

    // ============================================================================
    // Inventory Notifications
    // ============================================================================
    Task<SendResult> SendLowStockAlertAsync(int productId);
    Task<SendResult> SendExpiryAlertAsync(int productId, DateTime expiryDate);
    Task<int> SendBulkInventoryAlertsAsync();

    // ============================================================================
    // Marketing/Broadcast
    // ============================================================================
    Task<BroadcastResponse> CreateBroadcastAsync(BroadcastRequest request);
    Task<BroadcastResponse?> GetBroadcastByIdAsync(int broadcastId);
    Task<List<BroadcastResponse>> GetBroadcastsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<BroadcastResponse> ProcessBroadcastAsync(int broadcastId);
    Task<bool> CancelBroadcastAsync(int broadcastId);

    // ============================================================================
    // Notification Preferences
    // ============================================================================
    Task<NotificationPreferencesResponse> GetPreferencesAsync(int? userId = null, int? customerId = null);
    Task<NotificationPreferencesResponse> UpdatePreferencesAsync(int preferenceId, UpdateNotificationPreferencesRequest request);
    Task<NotificationPreferencesResponse> CreateDefaultPreferencesAsync(int? userId = null, int? customerId = null);

    // ============================================================================
    // Email Templates
    // ============================================================================
    Task<EmailTemplateResponse> CreateEmailTemplateAsync(CreateEmailTemplateRequest request);
    Task<EmailTemplateResponse?> GetEmailTemplateByIdAsync(int templateId);
    Task<EmailTemplateResponse?> GetEmailTemplateByCodeAsync(string templateCode);
    Task<List<EmailTemplateResponse>> GetEmailTemplatesAsync(string? category = null);
    Task<EmailTemplateResponse> UpdateEmailTemplateAsync(int templateId, UpdateEmailTemplateRequest request);
    Task<bool> DeleteEmailTemplateAsync(int templateId);

    // ============================================================================
    // SMS Templates
    // ============================================================================
    Task<SmsTemplateResponse> CreateSmsTemplateAsync(CreateSmsTemplateRequest request);
    Task<SmsTemplateResponse?> GetSmsTemplateByIdAsync(int templateId);
    Task<SmsTemplateResponse?> GetSmsTemplateByCodeAsync(string templateCode);
    Task<List<SmsTemplateResponse>> GetSmsTemplatesAsync(string? category = null);
    Task<SmsTemplateResponse> UpdateSmsTemplateAsync(int templateId, UpdateSmsTemplateRequest request);
    Task<bool> DeleteSmsTemplateAsync(int templateId);

    // ============================================================================
    // Statistics
    // ============================================================================
    Task<NotificationStatsResponse> GetNotificationStatsAsync(NotificationStatsRequest request);
}
