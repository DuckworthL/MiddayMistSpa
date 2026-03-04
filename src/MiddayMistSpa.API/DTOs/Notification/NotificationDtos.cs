using System.ComponentModel.DataAnnotations;

namespace MiddayMistSpa.API.DTOs.Notification;

// ============================================================================
// Notification DTOs
// ============================================================================

public class NotificationResponse
{
    public int NotificationId { get; set; }
    public int? UserId { get; set; }
    public int? EmployeeId { get; set; }
    public int? CustomerId { get; set; }
    public string Type { get; set; } = string.Empty; // Email, SMS, InApp, Push
    public string Category { get; set; } = string.Empty; // Appointment, Payroll, Inventory, System, Marketing
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Pending, Sent, Delivered, Failed, Read
    public string? Recipient { get; set; } // Email address or phone number
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateNotificationRequest
{
    public int? UserId { get; set; }
    public int? EmployeeId { get; set; }
    public int? CustomerId { get; set; }

    [Required, StringLength(20)]
    public string Type { get; set; } = "InApp"; // Email, SMS, InApp, Push

    [Required, StringLength(50)]
    public string Category { get; set; } = "System";

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public string? Recipient { get; set; } // Override recipient if different from user's default
    public bool SendImmediately { get; set; } = true;
    public DateTime? ScheduledAt { get; set; }
}

public class NotificationSearchRequest
{
    public int? UserId { get; set; }
    public int? EmployeeId { get; set; }
    public int? CustomerId { get; set; }
    public string? Type { get; set; }
    public string? Category { get; set; }
    public string? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool UnreadOnly { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// ============================================================================
// Appointment Reminder DTOs
// ============================================================================

public class AppointmentReminderRequest
{
    [Required]
    public int AppointmentId { get; set; }

    public bool SendEmail { get; set; } = true;
    public bool SendSms { get; set; } = true;
    public bool SendInApp { get; set; } = true;
}

public class BulkReminderRequest
{
    [Required]
    public DateTime ReminderDate { get; set; }

    public int HoursBeforeAppointment { get; set; } = 24;
    public bool SendEmail { get; set; } = true;
    public bool SendSms { get; set; } = true;
}

public class ReminderResult
{
    public int TotalAppointments { get; set; }
    public int EmailsSent { get; set; }
    public int SmsSent { get; set; }
    public int InAppSent { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = new();
}

// ============================================================================
// Email Template DTOs
// ============================================================================

public class EmailTemplateResponse
{
    public int TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateEmailTemplateRequest
{
    [Required, StringLength(100)]
    public string TemplateName { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string TemplateCode { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Category { get; set; } = string.Empty;
}

public class UpdateEmailTemplateRequest
{
    [StringLength(100)]
    public string? TemplateName { get; set; }

    [StringLength(200)]
    public string? Subject { get; set; }

    public string? Body { get; set; }
    public bool? IsActive { get; set; }
}

// ============================================================================
// SMS Template DTOs
// ============================================================================

public class SmsTemplateResponse
{
    public int TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSmsTemplateRequest
{
    [Required, StringLength(100)]
    public string TemplateName { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string TemplateCode { get; set; } = string.Empty;

    [Required, StringLength(160)]
    public string Message { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Category { get; set; } = string.Empty;
}

public class UpdateSmsTemplateRequest
{
    [StringLength(100)]
    public string? TemplateName { get; set; }

    [StringLength(160)]
    public string? Message { get; set; }

    public bool? IsActive { get; set; }
}

// ============================================================================
// Direct Message DTOs
// ============================================================================

public class SendEmailRequest
{
    [Required, EmailAddress]
    public string To { get; set; } = string.Empty;

    public string? Cc { get; set; }
    public string? Bcc { get; set; }

    [Required, StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    public bool IsHtml { get; set; } = true;
    public string? TemplateCode { get; set; }
    public Dictionary<string, string>? TemplateData { get; set; }
}

public class SendSmsRequest
{
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, StringLength(160)]
    public string Message { get; set; } = string.Empty;

    public string? TemplateCode { get; set; }
    public Dictionary<string, string>? TemplateData { get; set; }
}

public class SendResult
{
    public bool Success { get; set; }
    public string? MessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
}

// ============================================================================
// Notification Preferences DTOs
// ============================================================================

public class NotificationPreferencesResponse
{
    public int PreferenceId { get; set; }
    public int? UserId { get; set; }
    public int? CustomerId { get; set; }

    // Appointment Notifications
    public bool AppointmentEmailEnabled { get; set; } = true;
    public bool AppointmentSmsEnabled { get; set; } = true;
    public bool AppointmentInAppEnabled { get; set; } = true;
    public int AppointmentReminderHours { get; set; } = 24;

    // Marketing Notifications
    public bool MarketingEmailEnabled { get; set; } = true;
    public bool MarketingSmsEnabled { get; set; } = false;

    // System Notifications
    public bool SystemEmailEnabled { get; set; } = true;
    public bool SystemInAppEnabled { get; set; } = true;

    // Payroll Notifications (for employees)
    public bool PayrollEmailEnabled { get; set; } = true;
    public bool PayrollInAppEnabled { get; set; } = true;

    public DateTime UpdatedAt { get; set; }
}

public class UpdateNotificationPreferencesRequest
{
    public bool? AppointmentEmailEnabled { get; set; }
    public bool? AppointmentSmsEnabled { get; set; }
    public bool? AppointmentInAppEnabled { get; set; }
    public int? AppointmentReminderHours { get; set; }
    public bool? MarketingEmailEnabled { get; set; }
    public bool? MarketingSmsEnabled { get; set; }
    public bool? SystemEmailEnabled { get; set; }
    public bool? SystemInAppEnabled { get; set; }
    public bool? PayrollEmailEnabled { get; set; }
    public bool? PayrollInAppEnabled { get; set; }
}

// ============================================================================
// Broadcast/Marketing DTOs
// ============================================================================

public class BroadcastRequest
{
    [Required, StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public string TargetAudience { get; set; } = "AllCustomers"; // AllCustomers, ActiveCustomers, NewCustomers, BySegment
    public int? SegmentId { get; set; }
    public bool SendEmail { get; set; } = true;
    public bool SendSms { get; set; } = false;
    public DateTime? ScheduledAt { get; set; }
}

public class BroadcastResponse
{
    public int BroadcastId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = string.Empty;
    public int TotalRecipients { get; set; }
    public int EmailsSent { get; set; }
    public int SmsSent { get; set; }
    public int Failed { get; set; }
    public string Status { get; set; } = string.Empty; // Pending, Sending, Completed, Failed
    public DateTime? ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ============================================================================
// Notification Statistics DTOs
// ============================================================================

public class NotificationStatsRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class NotificationStatsResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Totals
    public int TotalSent { get; set; }
    public int TotalDelivered { get; set; }
    public int TotalFailed { get; set; }
    public int TotalRead { get; set; }

    // By Type
    public NotificationTypeStats EmailStats { get; set; } = new();
    public NotificationTypeStats SmsStats { get; set; } = new();
    public NotificationTypeStats InAppStats { get; set; } = new();

    // By Category
    public List<NotificationCategoryStats> ByCategory { get; set; } = new();

    // Daily Trend
    public List<DailyNotificationStats> DailyTrend { get; set; } = new();
}

public class NotificationTypeStats
{
    public int Sent { get; set; }
    public int Delivered { get; set; }
    public int Failed { get; set; }
    public int Read { get; set; }
    public decimal DeliveryRate { get; set; }
    public decimal ReadRate { get; set; }
}

public class NotificationCategoryStats
{
    public string Category { get; set; } = string.Empty;
    public int Sent { get; set; }
    public int Delivered { get; set; }
    public int Failed { get; set; }
}

public class DailyNotificationStats
{
    public DateTime Date { get; set; }
    public int Sent { get; set; }
    public int Delivered { get; set; }
    public int Failed { get; set; }
}
