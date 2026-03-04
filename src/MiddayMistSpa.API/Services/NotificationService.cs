using Microsoft.EntityFrameworkCore;
using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.API.DTOs.Notification;
using MiddayMistSpa.Infrastructure.Data;

namespace MiddayMistSpa.API.Services;

public class NotificationService : INotificationService
{
    private readonly SpaDbContext _context;
    private readonly ILogger<NotificationService> _logger;
    private readonly IConfiguration _configuration;

    // In-memory storage for notifications, templates, preferences (until entities are created)
    private static readonly List<NotificationRecord> _notifications = new();
    private static readonly List<EmailTemplate> _emailTemplates = new();
    private static readonly List<SmsTemplate> _smsTemplates = new();
    private static readonly List<NotificationPreference> _preferences = new();
    private static readonly List<Broadcast> _broadcasts = new();
    private static int _notificationIdCounter = 1;
    private static int _templateIdCounter = 1;
    private static int _preferenceIdCounter = 1;
    private static int _broadcastIdCounter = 1;

    public NotificationService(SpaDbContext context, ILogger<NotificationService> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;

        // Seed default email templates
        if (!_emailTemplates.Any())
        {
            SeedDefaultTemplates();
        }
    }

    private void SeedDefaultTemplates()
    {
        _emailTemplates.AddRange(new[]
        {
            new EmailTemplate
            {
                TemplateId = _templateIdCounter++,
                TemplateName = "Appointment Confirmation",
                TemplateCode = "APPT_CONFIRM",
                Subject = "Your Appointment at MiddayMist Spa is Confirmed",
                Body = @"
                    <h2>Appointment Confirmed!</h2>
                    <p>Dear {{CustomerName}},</p>
                    <p>Your appointment has been confirmed:</p>
                    <ul>
                        <li><strong>Service:</strong> {{ServiceName}}</li>
                        <li><strong>Date:</strong> {{Date}}</li>
                        <li><strong>Time:</strong> {{Time}}</li>
                        <li><strong>Therapist:</strong> {{TherapistName}}</li>
                    </ul>
                    <p>We look forward to seeing you!</p>
                    <p>MiddayMist Spa</p>
                ",
                Category = "Appointment",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new EmailTemplate
            {
                TemplateId = _templateIdCounter++,
                TemplateName = "Appointment Reminder",
                TemplateCode = "APPT_REMINDER",
                Subject = "Reminder: Your Appointment Tomorrow at MiddayMist Spa",
                Body = @"
                    <h2>Appointment Reminder</h2>
                    <p>Dear {{CustomerName}},</p>
                    <p>This is a friendly reminder about your upcoming appointment:</p>
                    <ul>
                        <li><strong>Service:</strong> {{ServiceName}}</li>
                        <li><strong>Date:</strong> {{Date}}</li>
                        <li><strong>Time:</strong> {{Time}}</li>
                        <li><strong>Therapist:</strong> {{TherapistName}}</li>
                    </ul>
                    <p>Please arrive 10 minutes early. See you soon!</p>
                    <p>MiddayMist Spa</p>
                ",
                Category = "Appointment",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new EmailTemplate
            {
                TemplateId = _templateIdCounter++,
                TemplateName = "Payslip Notification",
                TemplateCode = "PAYSLIP_READY",
                Subject = "Your Payslip is Ready - {{PeriodName}}",
                Body = @"
                    <h2>Payslip Available</h2>
                    <p>Dear {{EmployeeName}},</p>
                    <p>Your payslip for {{PeriodName}} is now available.</p>
                    <ul>
                        <li><strong>Period:</strong> {{StartDate}} - {{EndDate}}</li>
                        <li><strong>Net Pay:</strong> ₱{{NetPay}}</li>
                        <li><strong>Payment Date:</strong> {{PaymentDate}}</li>
                    </ul>
                    <p>Please log in to view your complete payslip.</p>
                    <p>MiddayMist Spa HR</p>
                ",
                Category = "Payroll",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        });

        _smsTemplates.AddRange(new[]
        {
            new SmsTemplate
            {
                TemplateId = _templateIdCounter++,
                TemplateName = "Appointment Reminder SMS",
                TemplateCode = "APPT_REMINDER_SMS",
                Message = "Hi {{CustomerName}}! Reminder: {{ServiceName}} at MiddayMist Spa tomorrow, {{Time}}. See you! Reply CANCEL to cancel.",
                Category = "Appointment",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new SmsTemplate
            {
                TemplateId = _templateIdCounter++,
                TemplateName = "Appointment Confirmation SMS",
                TemplateCode = "APPT_CONFIRM_SMS",
                Message = "Confirmed! {{ServiceName}} at MiddayMist Spa on {{Date}} at {{Time}}. We look forward to seeing you!",
                Category = "Appointment",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        });
    }

    // ============================================================================
    // Notification CRUD
    // ============================================================================

    public Task<NotificationResponse> CreateNotificationAsync(CreateNotificationRequest request)
    {
        var notification = new NotificationRecord
        {
            NotificationId = _notificationIdCounter++,
            UserId = request.UserId,
            EmployeeId = request.EmployeeId,
            CustomerId = request.CustomerId,
            Type = request.Type,
            Category = request.Category,
            Title = request.Title,
            Message = request.Message,
            Recipient = request.Recipient,
            Status = request.SendImmediately ? "Pending" : "Scheduled",
            ScheduledAt = request.ScheduledAt,
            CreatedAt = DateTime.UtcNow
        };

        _notifications.Add(notification);

        if (request.SendImmediately)
        {
            // Simulate sending
            notification.Status = "Sent";
            notification.SentAt = DateTime.UtcNow;
            notification.DeliveredAt = DateTime.UtcNow;
        }

        return Task.FromResult(MapToResponse(notification));
    }

    public Task<NotificationResponse?> GetNotificationByIdAsync(int notificationId)
    {
        var notification = _notifications.FirstOrDefault(n => n.NotificationId == notificationId);
        return Task.FromResult(notification != null ? MapToResponse(notification) : null);
    }

    public Task<PagedResponse<NotificationResponse>> SearchNotificationsAsync(NotificationSearchRequest request)
    {
        var query = _notifications.AsQueryable();

        if (request.UserId.HasValue)
            query = query.Where(n => n.UserId == request.UserId.Value);

        if (request.EmployeeId.HasValue)
            query = query.Where(n => n.EmployeeId == request.EmployeeId.Value);

        if (request.CustomerId.HasValue)
            query = query.Where(n => n.CustomerId == request.CustomerId.Value);

        if (!string.IsNullOrEmpty(request.Type))
            query = query.Where(n => n.Type == request.Type);

        if (!string.IsNullOrEmpty(request.Category))
            query = query.Where(n => n.Category == request.Category);

        if (!string.IsNullOrEmpty(request.Status))
            query = query.Where(n => n.Status == request.Status);

        if (request.StartDate.HasValue)
            query = query.Where(n => n.CreatedAt >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            query = query.Where(n => n.CreatedAt <= request.EndDate.Value);

        if (request.UnreadOnly)
            query = query.Where(n => n.ReadAt == null);

        var totalCount = query.Count();
        var items = query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(MapToResponse)
            .ToList();

        var result = new PagedResponse<NotificationResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return Task.FromResult(result);
    }

    public Task<List<NotificationResponse>> GetUserNotificationsAsync(int userId, bool unreadOnly = false)
    {
        // Include user-specific notifications AND system-wide notifications (UserId is null)
        var query = _notifications.Where(n => n.UserId == userId || n.UserId == null);

        if (unreadOnly)
            query = query.Where(n => n.ReadAt == null);

        var result = query
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(MapToResponse)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<NotificationResponse> MarkAsReadAsync(int notificationId)
    {
        var notification = _notifications.FirstOrDefault(n => n.NotificationId == notificationId);
        if (notification == null)
            throw new InvalidOperationException("Notification not found");

        notification.ReadAt = DateTime.UtcNow;
        notification.Status = "Read";

        return Task.FromResult(MapToResponse(notification));
    }

    public Task<int> MarkAllAsReadAsync(int userId)
    {
        var notifications = _notifications.Where(n => (n.UserId == userId || n.UserId == null) && n.ReadAt == null).ToList();
        foreach (var notification in notifications)
        {
            notification.ReadAt = DateTime.UtcNow;
            notification.Status = "Read";
        }
        return Task.FromResult(notifications.Count);
    }

    public Task<bool> DeleteNotificationAsync(int notificationId)
    {
        var notification = _notifications.FirstOrDefault(n => n.NotificationId == notificationId);
        if (notification == null)
            return Task.FromResult(false);

        _notifications.Remove(notification);
        return Task.FromResult(true);
    }

    public Task<int> GetUnreadCountAsync(int userId)
    {
        // Include user-specific AND system-wide (null UserId) unread notifications
        var count = _notifications.Count(n => (n.UserId == userId || n.UserId == null) && n.ReadAt == null);
        return Task.FromResult(count);
    }

    // ============================================================================
    // Email Operations
    // ============================================================================

    public Task<SendResult> SendEmailAsync(SendEmailRequest request)
    {
        _logger.LogInformation("Sending email to {To}: {Subject}", request.To, request.Subject);

        // If template code provided, use templated email
        if (!string.IsNullOrEmpty(request.TemplateCode) && request.TemplateData != null)
        {
            return SendTemplatedEmailAsync(request.TemplateCode, request.To, request.TemplateData);
        }

        // Simulate sending (in production, integrate with SendGrid, AWS SES, etc.)
        // var smtpSettings = _configuration.GetSection("Smtp");
        // Use smtpSettings to send actual email

        return Task.FromResult(new SendResult
        {
            Success = true,
            MessageId = Guid.NewGuid().ToString(),
            SentAt = DateTime.UtcNow
        });
    }

    public Task<SendResult> SendTemplatedEmailAsync(string templateCode, string to, Dictionary<string, string> data)
    {
        var template = _emailTemplates.FirstOrDefault(t => t.TemplateCode == templateCode && t.IsActive);
        if (template == null)
        {
            return Task.FromResult(new SendResult
            {
                Success = false,
                ErrorMessage = $"Email template '{templateCode}' not found or inactive"
            });
        }

        var subject = ReplaceTokens(template.Subject, data);
        var body = ReplaceTokens(template.Body, data);

        _logger.LogInformation("Sending templated email '{TemplateCode}' to {To}", templateCode, to);

        return Task.FromResult(new SendResult
        {
            Success = true,
            MessageId = Guid.NewGuid().ToString(),
            SentAt = DateTime.UtcNow
        });
    }

    // ============================================================================
    // SMS Operations
    // ============================================================================

    public Task<SendResult> SendSmsAsync(SendSmsRequest request)
    {
        _logger.LogInformation("Sending SMS to {PhoneNumber}", request.PhoneNumber);

        // If template code provided, use templated SMS
        if (!string.IsNullOrEmpty(request.TemplateCode) && request.TemplateData != null)
        {
            return SendTemplatedSmsAsync(request.TemplateCode, request.PhoneNumber, request.TemplateData);
        }

        // Simulate sending (in production, integrate with Twilio, Semaphore, etc.)
        // Philippine SMS providers: Semaphore, Engagespark, Globe Labs

        return Task.FromResult(new SendResult
        {
            Success = true,
            MessageId = Guid.NewGuid().ToString(),
            SentAt = DateTime.UtcNow
        });
    }

    public Task<SendResult> SendTemplatedSmsAsync(string templateCode, string phoneNumber, Dictionary<string, string> data)
    {
        var template = _smsTemplates.FirstOrDefault(t => t.TemplateCode == templateCode && t.IsActive);
        if (template == null)
        {
            return Task.FromResult(new SendResult
            {
                Success = false,
                ErrorMessage = $"SMS template '{templateCode}' not found or inactive"
            });
        }

        var message = ReplaceTokens(template.Message, data);

        _logger.LogInformation("Sending templated SMS '{TemplateCode}' to {PhoneNumber}", templateCode, phoneNumber);

        return Task.FromResult(new SendResult
        {
            Success = true,
            MessageId = Guid.NewGuid().ToString(),
            SentAt = DateTime.UtcNow
        });
    }

    // ============================================================================
    // Appointment Reminders
    // ============================================================================

    public async Task<ReminderResult> SendAppointmentReminderAsync(AppointmentReminderRequest request)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Service)
            .Include(a => a.Therapist)
            .FirstOrDefaultAsync(a => a.AppointmentId == request.AppointmentId);

        if (appointment == null)
            throw new InvalidOperationException("Appointment not found");

        var result = new ReminderResult { TotalAppointments = 1 };
        var data = new Dictionary<string, string>
        {
            { "CustomerName", $"{appointment.Customer.FirstName} {appointment.Customer.LastName}" },
            { "ServiceName", appointment.Service.ServiceName },
            { "Date", appointment.AppointmentDate.ToString("MMMM dd, yyyy") },
            { "Time", appointment.StartTime.ToString(@"hh\:mm") },
            { "TherapistName", appointment.Therapist != null ? $"{appointment.Therapist.FirstName} {appointment.Therapist.LastName}" : "TBA" }
        };

        if (request.SendEmail && !string.IsNullOrEmpty(appointment.Customer.Email))
        {
            var emailResult = await SendTemplatedEmailAsync("APPT_REMINDER", appointment.Customer.Email, data);
            if (emailResult.Success)
                result.EmailsSent++;
            else
                result.Errors.Add($"Email failed: {emailResult.ErrorMessage}");
        }

        if (request.SendSms && !string.IsNullOrEmpty(appointment.Customer.PhoneNumber))
        {
            var smsResult = await SendTemplatedSmsAsync("APPT_REMINDER_SMS", appointment.Customer.PhoneNumber, data);
            if (smsResult.Success)
                result.SmsSent++;
            else
                result.Errors.Add($"SMS failed: {smsResult.ErrorMessage}");
        }

        if (request.SendInApp)
        {
            await CreateNotificationAsync(new CreateNotificationRequest
            {
                CustomerId = appointment.CustomerId,
                Type = "InApp",
                Category = "Appointment",
                Title = "Appointment Reminder",
                Message = $"Reminder: Your {appointment.Service.ServiceName} appointment is tomorrow at {appointment.StartTime:hh\\:mm}."
            });
            result.InAppSent++;
        }

        result.Failed = result.Errors.Count;
        return result;
    }

    public async Task<ReminderResult> SendBulkAppointmentRemindersAsync(BulkReminderRequest request)
    {
        var targetDate = request.ReminderDate.AddHours(request.HoursBeforeAppointment);
        var appointments = await _context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Service)
            .Include(a => a.Therapist)
            .Where(a => a.AppointmentDate.Date == targetDate.Date &&
                        (a.Status == "Scheduled" || a.Status == "Confirmed"))
            .ToListAsync();

        var result = new ReminderResult { TotalAppointments = appointments.Count };

        foreach (var appointment in appointments)
        {
            try
            {
                var reminderResult = await SendAppointmentReminderAsync(new AppointmentReminderRequest
                {
                    AppointmentId = appointment.AppointmentId,
                    SendEmail = request.SendEmail,
                    SendSms = request.SendSms,
                    SendInApp = true
                });

                result.EmailsSent += reminderResult.EmailsSent;
                result.SmsSent += reminderResult.SmsSent;
                result.InAppSent += reminderResult.InAppSent;
                result.Failed += reminderResult.Failed;
                result.Errors.AddRange(reminderResult.Errors);
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Errors.Add($"Appointment {appointment.AppointmentId}: {ex.Message}");
            }
        }

        return result;
    }

    public async Task<ReminderResult> SendAppointmentConfirmationAsync(int appointmentId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Service)
            .Include(a => a.Therapist)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

        if (appointment == null)
            throw new InvalidOperationException("Appointment not found");

        var result = new ReminderResult { TotalAppointments = 1 };
        var data = new Dictionary<string, string>
        {
            { "CustomerName", $"{appointment.Customer.FirstName} {appointment.Customer.LastName}" },
            { "ServiceName", appointment.Service.ServiceName },
            { "Date", appointment.AppointmentDate.ToString("MMMM dd, yyyy") },
            { "Time", appointment.StartTime.ToString(@"hh\:mm") },
            { "TherapistName", appointment.Therapist != null ? $"{appointment.Therapist.FirstName} {appointment.Therapist.LastName}" : "TBA" }
        };

        if (!string.IsNullOrEmpty(appointment.Customer.Email))
        {
            var emailResult = await SendTemplatedEmailAsync("APPT_CONFIRM", appointment.Customer.Email, data);
            if (emailResult.Success) result.EmailsSent++;
        }

        if (!string.IsNullOrEmpty(appointment.Customer.PhoneNumber))
        {
            var smsResult = await SendTemplatedSmsAsync("APPT_CONFIRM_SMS", appointment.Customer.PhoneNumber, data);
            if (smsResult.Success) result.SmsSent++;
        }

        return result;
    }

    public async Task<ReminderResult> SendAppointmentCancellationAsync(int appointmentId, string? reason)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

        if (appointment == null)
            throw new InvalidOperationException("Appointment not found");

        var result = new ReminderResult { TotalAppointments = 1 };

        if (!string.IsNullOrEmpty(appointment.Customer.Email))
        {
            await SendEmailAsync(new SendEmailRequest
            {
                To = appointment.Customer.Email,
                Subject = "Your Appointment has been Cancelled",
                Body = $@"
                    <h2>Appointment Cancelled</h2>
                    <p>Dear {appointment.Customer.FirstName},</p>
                    <p>Your appointment for {appointment.Service.ServiceName} on {appointment.AppointmentDate:MMMM dd, yyyy} at {appointment.StartTime:hh\:mm} has been cancelled.</p>
                    {(string.IsNullOrEmpty(reason) ? "" : $"<p><strong>Reason:</strong> {reason}</p>")}
                    <p>Please contact us to reschedule. We apologize for any inconvenience.</p>
                    <p>MiddayMist Spa</p>
                "
            });
            result.EmailsSent++;
        }

        return result;
    }

    // ============================================================================
    // Employee Notifications
    // ============================================================================

    public async Task<SendResult> SendPayslipNotificationAsync(int employeeId, int payrollRecordId)
    {
        var payrollRecord = await _context.PayrollRecords
            .Include(pr => pr.Employee)
            .Include(pr => pr.PayrollPeriod)
            .FirstOrDefaultAsync(pr => pr.PayrollRecordId == payrollRecordId);

        if (payrollRecord == null)
            return new SendResult { Success = false, ErrorMessage = "Payroll record not found" };

        var employee = payrollRecord.Employee;
        if (string.IsNullOrEmpty(employee.Email))
            return new SendResult { Success = false, ErrorMessage = "Employee has no email address" };

        var data = new Dictionary<string, string>
        {
            { "EmployeeName", $"{employee.FirstName} {employee.LastName}" },
            { "PeriodName", payrollRecord.PayrollPeriod.PeriodName },
            { "StartDate", payrollRecord.PayrollPeriod.StartDate.ToString("MMMM dd, yyyy") },
            { "EndDate", payrollRecord.PayrollPeriod.EndDate.ToString("MMMM dd, yyyy") },
            { "NetPay", payrollRecord.NetPay.ToString("N2") },
            { "PaymentDate", payrollRecord.PayrollPeriod.PaymentDate.ToString("MMMM dd, yyyy") }
        };

        return await SendTemplatedEmailAsync("PAYSLIP_READY", employee.Email, data);
    }

    public async Task<SendResult> SendScheduleUpdateNotificationAsync(int employeeId, DateTime effectiveDate)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
        if (employee == null)
            return new SendResult { Success = false, ErrorMessage = "Employee not found" };

        await CreateNotificationAsync(new CreateNotificationRequest
        {
            EmployeeId = employeeId,
            Type = "InApp",
            Category = "Schedule",
            Title = "Schedule Updated",
            Message = $"Your work schedule has been updated, effective {effectiveDate:MMMM dd, yyyy}. Please review your new schedule."
        });

        return new SendResult { Success = true, SentAt = DateTime.UtcNow };
    }

    public async Task<SendResult> SendTimeOffApprovalNotificationAsync(int timeOffRequestId, bool approved, string? reason)
    {
        var request = await _context.TimeOffRequests
            .Include(t => t.Employee)
            .FirstOrDefaultAsync(t => t.TimeOffRequestId == timeOffRequestId);

        if (request == null)
            return new SendResult { Success = false, ErrorMessage = "Time off request not found" };

        var status = approved ? "Approved" : "Rejected";
        var message = $"Your time off request for {request.StartDate:MMM dd} - {request.EndDate:MMM dd, yyyy} has been {status}.";
        if (!approved && !string.IsNullOrEmpty(reason))
            message += $" Reason: {reason}";

        await CreateNotificationAsync(new CreateNotificationRequest
        {
            EmployeeId = request.EmployeeId,
            Type = "InApp",
            Category = "TimeOff",
            Title = $"Time Off Request {status}",
            Message = message
        });

        return new SendResult { Success = true, SentAt = DateTime.UtcNow };
    }

    public async Task<SendResult> SendLowLeaveBalanceNotificationAsync(int employeeId)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
        if (employee == null)
            return new SendResult { Success = false, ErrorMessage = "Employee not found" };

        await CreateNotificationAsync(new CreateNotificationRequest
        {
            EmployeeId = employeeId,
            Type = "InApp",
            Category = "Leave",
            Title = "Low Leave Balance Alert",
            Message = "Your leave balance is running low. Please plan your remaining leave days accordingly."
        });

        return new SendResult { Success = true, SentAt = DateTime.UtcNow };
    }

    // ============================================================================
    // Inventory Notifications
    // ============================================================================

    public async Task<SendResult> SendLowStockAlertAsync(int productId)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.ProductId == productId);

        if (product == null)
            return new SendResult { Success = false, ErrorMessage = "Product not found" };

        var isOutOfStock = product.CurrentStock <= 0;
        var alertTitle = isOutOfStock ? "Out of Stock Alert" : "Low Stock Alert";
        var alertMessage = isOutOfStock
            ? $"Product '{product.ProductName}' is out of stock. Current stock: {product.CurrentStock}. Immediate restocking required."
            : $"Product '{product.ProductName}' is running low. Current stock: {product.CurrentStock} (Reorder level: {product.ReorderLevel})";

        // Send to inventory managers (in production, query users with InventoryManager role)
        await CreateNotificationAsync(new CreateNotificationRequest
        {
            Type = "InApp",
            Category = "Inventory",
            Title = alertTitle,
            Message = alertMessage
        });

        _logger.LogWarning("{AlertType}: {ProductName} - Current: {Current}, Reorder: {Reorder}",
            alertTitle, product.ProductName, product.CurrentStock, product.ReorderLevel);

        return new SendResult { Success = true, SentAt = DateTime.UtcNow };
    }

    public async Task<SendResult> SendExpiryAlertAsync(int productId, DateTime expiryDate)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
        if (product == null)
            return new SendResult { Success = false, ErrorMessage = "Product not found" };

        var daysUntilExpiry = (expiryDate - DateTime.UtcNow).Days;

        await CreateNotificationAsync(new CreateNotificationRequest
        {
            Type = "InApp",
            Category = "Inventory",
            Title = "Product Expiry Alert",
            Message = $"Product '{product.ProductName}' will expire in {daysUntilExpiry} days ({expiryDate:MMM dd, yyyy}). Please take action."
        });

        return new SendResult { Success = true, SentAt = DateTime.UtcNow };
    }

    public async Task<int> SendBulkInventoryAlertsAsync()
    {
        var lowStockProducts = await _context.Products
            .Where(p => p.IsActive && p.CurrentStock <= p.ReorderLevel)
            .ToListAsync();

        var expiringProducts = await _context.Products
            .Where(p => p.IsActive && p.ExpiryDate.HasValue && p.ExpiryDate.Value <= DateTime.UtcNow.AddDays(30))
            .ToListAsync();

        var alertCount = 0;

        foreach (var product in lowStockProducts)
        {
            await SendLowStockAlertAsync(product.ProductId);
            alertCount++;
        }

        foreach (var product in expiringProducts)
        {
            await SendExpiryAlertAsync(product.ProductId, product.ExpiryDate!.Value);
            alertCount++;
        }

        return alertCount;
    }

    // ============================================================================
    // Marketing/Broadcast
    // ============================================================================

    public async Task<BroadcastResponse> CreateBroadcastAsync(BroadcastRequest request)
    {
        var customers = await GetTargetCustomersAsync(request.TargetAudience, request.SegmentId);

        var broadcast = new Broadcast
        {
            BroadcastId = _broadcastIdCounter++,
            Subject = request.Subject,
            Message = request.Message,
            TargetAudience = request.TargetAudience,
            TotalRecipients = customers.Count,
            SendEmail = request.SendEmail,
            SendSms = request.SendSms,
            Status = request.ScheduledAt.HasValue ? "Scheduled" : "Pending",
            ScheduledAt = request.ScheduledAt,
            CreatedAt = DateTime.UtcNow
        };

        _broadcasts.Add(broadcast);

        if (!request.ScheduledAt.HasValue)
        {
            // Process immediately in background (simplified)
            _ = ProcessBroadcastInternalAsync(broadcast, customers);
        }

        return MapToBroadcastResponse(broadcast);
    }

    private async Task<List<MiddayMistSpa.Core.Entities.Customer.Customer>> GetTargetCustomersAsync(string targetAudience, int? segmentId)
    {
        var query = _context.Customers.Where(c => c.IsActive);

        return targetAudience switch
        {
            "ActiveCustomers" => await query.Where(c => c.LastVisitDate >= DateTime.UtcNow.AddDays(-90)).ToListAsync(),
            "NewCustomers" => await query.Where(c => c.CreatedAt >= DateTime.UtcNow.AddDays(-30)).ToListAsync(),
            "BySegment" when segmentId.HasValue => await query.Where(c => c.CustomerSegment == segmentId.Value.ToString()).ToListAsync(),
            _ => await query.ToListAsync()
        };
    }

    private async Task ProcessBroadcastInternalAsync(Broadcast broadcast, List<MiddayMistSpa.Core.Entities.Customer.Customer> customers)
    {
        broadcast.Status = "Sending";
        broadcast.StartedAt = DateTime.UtcNow;

        foreach (var customer in customers)
        {
            try
            {
                if (broadcast.SendEmail && !string.IsNullOrEmpty(customer.Email))
                {
                    await SendEmailAsync(new SendEmailRequest
                    {
                        To = customer.Email,
                        Subject = broadcast.Subject,
                        Body = broadcast.Message.Replace("{{CustomerName}}", $"{customer.FirstName} {customer.LastName}")
                    });
                    broadcast.EmailsSent++;
                }

                if (broadcast.SendSms && !string.IsNullOrEmpty(customer.PhoneNumber))
                {
                    await SendSmsAsync(new SendSmsRequest
                    {
                        PhoneNumber = customer.PhoneNumber,
                        Message = broadcast.Message.Replace("{{CustomerName}}", customer.FirstName)
                    });
                    broadcast.SmsSent++;
                }
            }
            catch
            {
                broadcast.Failed++;
            }
        }

        broadcast.Status = "Completed";
        broadcast.CompletedAt = DateTime.UtcNow;
    }

    public Task<BroadcastResponse?> GetBroadcastByIdAsync(int broadcastId)
    {
        var broadcast = _broadcasts.FirstOrDefault(b => b.BroadcastId == broadcastId);
        return Task.FromResult(broadcast != null ? MapToBroadcastResponse(broadcast) : null);
    }

    public Task<List<BroadcastResponse>> GetBroadcastsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _broadcasts.AsQueryable();

        if (startDate.HasValue)
            query = query.Where(b => b.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(b => b.CreatedAt <= endDate.Value);

        var result = query.OrderByDescending(b => b.CreatedAt).Select(MapToBroadcastResponse).ToList();
        return Task.FromResult(result);
    }

    public async Task<BroadcastResponse> ProcessBroadcastAsync(int broadcastId)
    {
        var broadcast = _broadcasts.FirstOrDefault(b => b.BroadcastId == broadcastId);
        if (broadcast == null)
            throw new InvalidOperationException("Broadcast not found");

        if (broadcast.Status != "Pending" && broadcast.Status != "Scheduled")
            throw new InvalidOperationException("Broadcast cannot be processed - invalid status");

        var customers = await GetTargetCustomersAsync(broadcast.TargetAudience, null);
        await ProcessBroadcastInternalAsync(broadcast, customers);

        return MapToBroadcastResponse(broadcast);
    }

    public Task<bool> CancelBroadcastAsync(int broadcastId)
    {
        var broadcast = _broadcasts.FirstOrDefault(b => b.BroadcastId == broadcastId);
        if (broadcast == null)
            return Task.FromResult(false);

        if (broadcast.Status == "Sending" || broadcast.Status == "Completed")
            throw new InvalidOperationException("Cannot cancel a broadcast that is sending or completed");

        broadcast.Status = "Cancelled";
        return Task.FromResult(true);
    }

    // ============================================================================
    // Notification Preferences
    // ============================================================================

    public Task<NotificationPreferencesResponse> GetPreferencesAsync(int? userId = null, int? customerId = null)
    {
        var pref = _preferences.FirstOrDefault(p =>
            (userId.HasValue && p.UserId == userId) ||
            (customerId.HasValue && p.CustomerId == customerId));

        if (pref == null)
        {
            // Return default preferences
            return Task.FromResult(new NotificationPreferencesResponse
            {
                PreferenceId = 0,
                UserId = userId,
                CustomerId = customerId,
                AppointmentEmailEnabled = true,
                AppointmentSmsEnabled = true,
                AppointmentInAppEnabled = true,
                AppointmentReminderHours = 24,
                MarketingEmailEnabled = true,
                MarketingSmsEnabled = false,
                SystemEmailEnabled = true,
                SystemInAppEnabled = true,
                PayrollEmailEnabled = true,
                PayrollInAppEnabled = true,
                UpdatedAt = DateTime.UtcNow
            });
        }

        return Task.FromResult(MapToPreferencesResponse(pref));
    }

    public Task<NotificationPreferencesResponse> UpdatePreferencesAsync(int preferenceId, UpdateNotificationPreferencesRequest request)
    {
        var pref = _preferences.FirstOrDefault(p => p.PreferenceId == preferenceId);
        if (pref == null)
            throw new InvalidOperationException("Preferences not found");

        if (request.AppointmentEmailEnabled.HasValue)
            pref.AppointmentEmailEnabled = request.AppointmentEmailEnabled.Value;
        if (request.AppointmentSmsEnabled.HasValue)
            pref.AppointmentSmsEnabled = request.AppointmentSmsEnabled.Value;
        if (request.AppointmentInAppEnabled.HasValue)
            pref.AppointmentInAppEnabled = request.AppointmentInAppEnabled.Value;
        if (request.AppointmentReminderHours.HasValue)
            pref.AppointmentReminderHours = request.AppointmentReminderHours.Value;
        if (request.MarketingEmailEnabled.HasValue)
            pref.MarketingEmailEnabled = request.MarketingEmailEnabled.Value;
        if (request.MarketingSmsEnabled.HasValue)
            pref.MarketingSmsEnabled = request.MarketingSmsEnabled.Value;
        if (request.SystemEmailEnabled.HasValue)
            pref.SystemEmailEnabled = request.SystemEmailEnabled.Value;
        if (request.SystemInAppEnabled.HasValue)
            pref.SystemInAppEnabled = request.SystemInAppEnabled.Value;
        if (request.PayrollEmailEnabled.HasValue)
            pref.PayrollEmailEnabled = request.PayrollEmailEnabled.Value;
        if (request.PayrollInAppEnabled.HasValue)
            pref.PayrollInAppEnabled = request.PayrollInAppEnabled.Value;

        pref.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult(MapToPreferencesResponse(pref));
    }

    public Task<NotificationPreferencesResponse> CreateDefaultPreferencesAsync(int? userId = null, int? customerId = null)
    {
        var pref = new NotificationPreference
        {
            PreferenceId = _preferenceIdCounter++,
            UserId = userId,
            CustomerId = customerId,
            AppointmentEmailEnabled = true,
            AppointmentSmsEnabled = true,
            AppointmentInAppEnabled = true,
            AppointmentReminderHours = 24,
            MarketingEmailEnabled = true,
            MarketingSmsEnabled = false,
            SystemEmailEnabled = true,
            SystemInAppEnabled = true,
            PayrollEmailEnabled = true,
            PayrollInAppEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _preferences.Add(pref);
        return Task.FromResult(MapToPreferencesResponse(pref));
    }

    // ============================================================================
    // Email Templates
    // ============================================================================

    public Task<EmailTemplateResponse> CreateEmailTemplateAsync(CreateEmailTemplateRequest request)
    {
        if (_emailTemplates.Any(t => t.TemplateCode == request.TemplateCode))
            throw new InvalidOperationException($"Template code '{request.TemplateCode}' already exists");

        var template = new EmailTemplate
        {
            TemplateId = _templateIdCounter++,
            TemplateName = request.TemplateName,
            TemplateCode = request.TemplateCode,
            Subject = request.Subject,
            Body = request.Body,
            Category = request.Category,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _emailTemplates.Add(template);
        return Task.FromResult(MapToEmailTemplateResponse(template));
    }

    public Task<EmailTemplateResponse?> GetEmailTemplateByIdAsync(int templateId)
    {
        var template = _emailTemplates.FirstOrDefault(t => t.TemplateId == templateId);
        return Task.FromResult(template != null ? MapToEmailTemplateResponse(template) : null);
    }

    public Task<EmailTemplateResponse?> GetEmailTemplateByCodeAsync(string templateCode)
    {
        var template = _emailTemplates.FirstOrDefault(t => t.TemplateCode == templateCode);
        return Task.FromResult(template != null ? MapToEmailTemplateResponse(template) : null);
    }

    public Task<List<EmailTemplateResponse>> GetEmailTemplatesAsync(string? category = null)
    {
        var query = _emailTemplates.AsQueryable();
        if (!string.IsNullOrEmpty(category))
            query = query.Where(t => t.Category == category);

        return Task.FromResult(query.Select(MapToEmailTemplateResponse).ToList());
    }

    public Task<EmailTemplateResponse> UpdateEmailTemplateAsync(int templateId, UpdateEmailTemplateRequest request)
    {
        var template = _emailTemplates.FirstOrDefault(t => t.TemplateId == templateId);
        if (template == null)
            throw new InvalidOperationException("Template not found");

        if (!string.IsNullOrEmpty(request.TemplateName))
            template.TemplateName = request.TemplateName;
        if (!string.IsNullOrEmpty(request.Subject))
            template.Subject = request.Subject;
        if (!string.IsNullOrEmpty(request.Body))
            template.Body = request.Body;
        if (request.IsActive.HasValue)
            template.IsActive = request.IsActive.Value;

        template.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult(MapToEmailTemplateResponse(template));
    }

    public Task<bool> DeleteEmailTemplateAsync(int templateId)
    {
        var template = _emailTemplates.FirstOrDefault(t => t.TemplateId == templateId);
        if (template == null)
            return Task.FromResult(false);

        _emailTemplates.Remove(template);
        return Task.FromResult(true);
    }

    // ============================================================================
    // SMS Templates
    // ============================================================================

    public Task<SmsTemplateResponse> CreateSmsTemplateAsync(CreateSmsTemplateRequest request)
    {
        if (_smsTemplates.Any(t => t.TemplateCode == request.TemplateCode))
            throw new InvalidOperationException($"Template code '{request.TemplateCode}' already exists");

        var template = new SmsTemplate
        {
            TemplateId = _templateIdCounter++,
            TemplateName = request.TemplateName,
            TemplateCode = request.TemplateCode,
            Message = request.Message,
            Category = request.Category,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _smsTemplates.Add(template);
        return Task.FromResult(MapToSmsTemplateResponse(template));
    }

    public Task<SmsTemplateResponse?> GetSmsTemplateByIdAsync(int templateId)
    {
        var template = _smsTemplates.FirstOrDefault(t => t.TemplateId == templateId);
        return Task.FromResult(template != null ? MapToSmsTemplateResponse(template) : null);
    }

    public Task<SmsTemplateResponse?> GetSmsTemplateByCodeAsync(string templateCode)
    {
        var template = _smsTemplates.FirstOrDefault(t => t.TemplateCode == templateCode);
        return Task.FromResult(template != null ? MapToSmsTemplateResponse(template) : null);
    }

    public Task<List<SmsTemplateResponse>> GetSmsTemplatesAsync(string? category = null)
    {
        var query = _smsTemplates.AsQueryable();
        if (!string.IsNullOrEmpty(category))
            query = query.Where(t => t.Category == category);

        return Task.FromResult(query.Select(MapToSmsTemplateResponse).ToList());
    }

    public Task<SmsTemplateResponse> UpdateSmsTemplateAsync(int templateId, UpdateSmsTemplateRequest request)
    {
        var template = _smsTemplates.FirstOrDefault(t => t.TemplateId == templateId);
        if (template == null)
            throw new InvalidOperationException("Template not found");

        if (!string.IsNullOrEmpty(request.TemplateName))
            template.TemplateName = request.TemplateName;
        if (!string.IsNullOrEmpty(request.Message))
            template.Message = request.Message;
        if (request.IsActive.HasValue)
            template.IsActive = request.IsActive.Value;

        template.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult(MapToSmsTemplateResponse(template));
    }

    public Task<bool> DeleteSmsTemplateAsync(int templateId)
    {
        var template = _smsTemplates.FirstOrDefault(t => t.TemplateId == templateId);
        if (template == null)
            return Task.FromResult(false);

        _smsTemplates.Remove(template);
        return Task.FromResult(true);
    }

    // ============================================================================
    // Statistics
    // ============================================================================

    public Task<NotificationStatsResponse> GetNotificationStatsAsync(NotificationStatsRequest request)
    {
        var notifications = _notifications
            .Where(n => n.CreatedAt >= request.StartDate && n.CreatedAt <= request.EndDate)
            .ToList();

        var emailStats = notifications.Where(n => n.Type == "Email").ToList();
        var smsStats = notifications.Where(n => n.Type == "SMS").ToList();
        var inAppStats = notifications.Where(n => n.Type == "InApp").ToList();

        return Task.FromResult(new NotificationStatsResponse
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalSent = notifications.Count(n => n.SentAt.HasValue),
            TotalDelivered = notifications.Count(n => n.DeliveredAt.HasValue),
            TotalFailed = notifications.Count(n => n.Status == "Failed"),
            TotalRead = notifications.Count(n => n.ReadAt.HasValue),
            EmailStats = new NotificationTypeStats
            {
                Sent = emailStats.Count(n => n.SentAt.HasValue),
                Delivered = emailStats.Count(n => n.DeliveredAt.HasValue),
                Failed = emailStats.Count(n => n.Status == "Failed"),
                Read = emailStats.Count(n => n.ReadAt.HasValue),
                DeliveryRate = emailStats.Count > 0 ? (decimal)emailStats.Count(n => n.DeliveredAt.HasValue) / emailStats.Count * 100 : 0,
                ReadRate = emailStats.Count > 0 ? (decimal)emailStats.Count(n => n.ReadAt.HasValue) / emailStats.Count * 100 : 0
            },
            SmsStats = new NotificationTypeStats
            {
                Sent = smsStats.Count(n => n.SentAt.HasValue),
                Delivered = smsStats.Count(n => n.DeliveredAt.HasValue),
                Failed = smsStats.Count(n => n.Status == "Failed"),
                Read = 0,
                DeliveryRate = smsStats.Count > 0 ? (decimal)smsStats.Count(n => n.DeliveredAt.HasValue) / smsStats.Count * 100 : 0,
                ReadRate = 0
            },
            InAppStats = new NotificationTypeStats
            {
                Sent = inAppStats.Count,
                Delivered = inAppStats.Count,
                Failed = 0,
                Read = inAppStats.Count(n => n.ReadAt.HasValue),
                DeliveryRate = 100,
                ReadRate = inAppStats.Count > 0 ? (decimal)inAppStats.Count(n => n.ReadAt.HasValue) / inAppStats.Count * 100 : 0
            },
            ByCategory = notifications
                .GroupBy(n => n.Category)
                .Select(g => new NotificationCategoryStats
                {
                    Category = g.Key,
                    Sent = g.Count(n => n.SentAt.HasValue),
                    Delivered = g.Count(n => n.DeliveredAt.HasValue),
                    Failed = g.Count(n => n.Status == "Failed")
                })
                .ToList(),
            DailyTrend = notifications
                .GroupBy(n => n.CreatedAt.Date)
                .Select(g => new DailyNotificationStats
                {
                    Date = g.Key,
                    Sent = g.Count(n => n.SentAt.HasValue),
                    Delivered = g.Count(n => n.DeliveredAt.HasValue),
                    Failed = g.Count(n => n.Status == "Failed")
                })
                .OrderBy(d => d.Date)
                .ToList()
        });
    }

    // ============================================================================
    // Helper Methods
    // ============================================================================

    private static string ReplaceTokens(string template, Dictionary<string, string> data)
    {
        foreach (var kvp in data)
        {
            template = template.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
        }
        return template;
    }

    private static NotificationResponse MapToResponse(NotificationRecord n) => new()
    {
        NotificationId = n.NotificationId,
        UserId = n.UserId,
        EmployeeId = n.EmployeeId,
        CustomerId = n.CustomerId,
        Type = n.Type,
        Category = n.Category,
        Title = n.Title,
        Message = n.Message,
        Status = n.Status,
        Recipient = n.Recipient,
        SentAt = n.SentAt,
        DeliveredAt = n.DeliveredAt,
        ReadAt = n.ReadAt,
        ErrorMessage = n.ErrorMessage,
        CreatedAt = n.CreatedAt
    };

    private static EmailTemplateResponse MapToEmailTemplateResponse(EmailTemplate t) => new()
    {
        TemplateId = t.TemplateId,
        TemplateName = t.TemplateName,
        TemplateCode = t.TemplateCode,
        Subject = t.Subject,
        Body = t.Body,
        Category = t.Category,
        IsActive = t.IsActive,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };

    private static SmsTemplateResponse MapToSmsTemplateResponse(SmsTemplate t) => new()
    {
        TemplateId = t.TemplateId,
        TemplateName = t.TemplateName,
        TemplateCode = t.TemplateCode,
        Message = t.Message,
        Category = t.Category,
        IsActive = t.IsActive,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };

    private static NotificationPreferencesResponse MapToPreferencesResponse(NotificationPreference p) => new()
    {
        PreferenceId = p.PreferenceId,
        UserId = p.UserId,
        CustomerId = p.CustomerId,
        AppointmentEmailEnabled = p.AppointmentEmailEnabled,
        AppointmentSmsEnabled = p.AppointmentSmsEnabled,
        AppointmentInAppEnabled = p.AppointmentInAppEnabled,
        AppointmentReminderHours = p.AppointmentReminderHours,
        MarketingEmailEnabled = p.MarketingEmailEnabled,
        MarketingSmsEnabled = p.MarketingSmsEnabled,
        SystemEmailEnabled = p.SystemEmailEnabled,
        SystemInAppEnabled = p.SystemInAppEnabled,
        PayrollEmailEnabled = p.PayrollEmailEnabled,
        PayrollInAppEnabled = p.PayrollInAppEnabled,
        UpdatedAt = p.UpdatedAt
    };

    private static BroadcastResponse MapToBroadcastResponse(Broadcast b) => new()
    {
        BroadcastId = b.BroadcastId,
        Subject = b.Subject,
        TargetAudience = b.TargetAudience,
        TotalRecipients = b.TotalRecipients,
        EmailsSent = b.EmailsSent,
        SmsSent = b.SmsSent,
        Failed = b.Failed,
        Status = b.Status,
        ScheduledAt = b.ScheduledAt,
        StartedAt = b.StartedAt,
        CompletedAt = b.CompletedAt,
        CreatedAt = b.CreatedAt
    };

    // ============================================================================
    // Internal Models (until entities are created)
    // ============================================================================

    private class NotificationRecord
    {
        public int NotificationId { get; set; }
        public int? UserId { get; set; }
        public int? EmployeeId { get; set; }
        public int? CustomerId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string? Recipient { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private class EmailTemplate
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public string TemplateCode { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    private class SmsTemplate
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public string TemplateCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    private class NotificationPreference
    {
        public int PreferenceId { get; set; }
        public int? UserId { get; set; }
        public int? CustomerId { get; set; }
        public bool AppointmentEmailEnabled { get; set; } = true;
        public bool AppointmentSmsEnabled { get; set; } = true;
        public bool AppointmentInAppEnabled { get; set; } = true;
        public int AppointmentReminderHours { get; set; } = 24;
        public bool MarketingEmailEnabled { get; set; } = true;
        public bool MarketingSmsEnabled { get; set; } = false;
        public bool SystemEmailEnabled { get; set; } = true;
        public bool SystemInAppEnabled { get; set; } = true;
        public bool PayrollEmailEnabled { get; set; } = true;
        public bool PayrollInAppEnabled { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    private class Broadcast
    {
        public int BroadcastId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string TargetAudience { get; set; } = string.Empty;
        public int TotalRecipients { get; set; }
        public bool SendEmail { get; set; }
        public bool SendSms { get; set; }
        public int EmailsSent { get; set; }
        public int SmsSent { get; set; }
        public int Failed { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime? ScheduledAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
