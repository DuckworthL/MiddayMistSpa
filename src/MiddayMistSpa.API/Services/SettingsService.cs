using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MiddayMistSpa.API.DTOs.Settings;
using MiddayMistSpa.Core.Entities.Configuration;
using MiddayMistSpa.Core.Entities.Identity;
using MiddayMistSpa.Infrastructure.Data;

namespace MiddayMistSpa.API.Services;

public class SettingsService : ISettingsService
{
    private readonly SpaDbContext _context;
    private readonly ILogger<SettingsService> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    // System role codes that cannot be deleted
    private static readonly HashSet<string> SystemRoleCodes = new()
    {
        "SUPERADMIN", "ADMIN", "RECEPTIONIST", "THERAPIST", "HR", "INVENTORY", "ACCOUNTANT", "SALES_LEDGER"
    };

    // Default role icons
    private static readonly Dictionary<string, string> RoleIcons = new()
    {
        ["SuperAdmin"] = "bi-shield-lock",
        ["Admin"] = "bi-person-gear",
        ["Receptionist"] = "bi-person-workspace",
        ["Therapist"] = "bi-person-heart",
        ["HR"] = "bi-people",
        ["Inventory"] = "bi-box-seam",
        ["Accountant"] = "bi-calculator",
        ["Sales Ledger"] = "bi-journal-text"
    };

    public SettingsService(SpaDbContext context, ILogger<SettingsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // =========================================================================
    // General Settings
    // =========================================================================

    public async Task<GeneralSettingsDto> GetGeneralSettingsAsync()
    {
        var settings = await _context.SystemSettings
            .Where(s => s.Category == "General")
            .ToListAsync();

        if (!settings.Any())
            return new GeneralSettingsDto();

        return new GeneralSettingsDto
        {
            AppName = GetValue(settings, "AppName", "MiddayMist Spa"),
            Currency = GetValue(settings, "Currency", "PHP"),
            DateFormat = GetValue(settings, "DateFormat", "MM/dd/yyyy"),
            TimeFormat = GetValue(settings, "TimeFormat", "12"),
            Timezone = GetValue(settings, "Timezone", "Asia/Manila"),
            Language = GetValue(settings, "Language", "en"),
            DefaultDuration = GetIntValue(settings, "DefaultDuration", 60),
            BookingLeadTime = GetIntValue(settings, "BookingLeadTime", 24),
            CancellationWindow = GetIntValue(settings, "CancellationWindow", 48),
            AllowOnlineBooking = GetBoolValue(settings, "AllowOnlineBooking", true),
            RequireDeposit = GetBoolValue(settings, "RequireDeposit", false),
            SendReminders = GetBoolValue(settings, "SendReminders", true)
        };
    }

    public async Task<GeneralSettingsDto> SaveGeneralSettingsAsync(GeneralSettingsDto dto, int updatedByUserId)
    {
        await UpsertSettingAsync("General", "AppName", dto.AppName, "String", updatedByUserId);
        await UpsertSettingAsync("General", "Currency", dto.Currency, "String", updatedByUserId);
        await UpsertSettingAsync("General", "DateFormat", dto.DateFormat, "String", updatedByUserId);
        await UpsertSettingAsync("General", "TimeFormat", dto.TimeFormat, "String", updatedByUserId);
        await UpsertSettingAsync("General", "Timezone", dto.Timezone, "String", updatedByUserId);
        await UpsertSettingAsync("General", "Language", dto.Language, "String", updatedByUserId);
        await UpsertSettingAsync("General", "DefaultDuration", dto.DefaultDuration.ToString(), "Number", updatedByUserId);
        await UpsertSettingAsync("General", "BookingLeadTime", dto.BookingLeadTime.ToString(), "Number", updatedByUserId);
        await UpsertSettingAsync("General", "CancellationWindow", dto.CancellationWindow.ToString(), "Number", updatedByUserId);
        await UpsertSettingAsync("General", "AllowOnlineBooking", dto.AllowOnlineBooking.ToString(), "Boolean", updatedByUserId);
        await UpsertSettingAsync("General", "RequireDeposit", dto.RequireDeposit.ToString(), "Boolean", updatedByUserId);
        await UpsertSettingAsync("General", "SendReminders", dto.SendReminders.ToString(), "Boolean", updatedByUserId);

        await _context.SaveChangesAsync();
        _logger.LogInformation("General settings saved by user {UserId}", updatedByUserId);
        return dto;
    }

    // =========================================================================
    // Business Info
    // =========================================================================

    public async Task<BusinessInfoDto> GetBusinessInfoAsync()
    {
        var settings = await _context.SystemSettings
            .Where(s => s.Category == "Business")
            .ToListAsync();

        if (!settings.Any())
            return GetDefaultBusinessInfo();

        var operatingHoursJson = GetValue(settings, "OperatingHours", "");
        var operatingHours = string.IsNullOrEmpty(operatingHoursJson)
            ? GetDefaultOperatingHours()
            : JsonSerializer.Deserialize<List<OperatingHoursDto>>(operatingHoursJson, _jsonOptions) ?? GetDefaultOperatingHours();

        return new BusinessInfoDto
        {
            Name = GetValue(settings, "BusinessName", "MiddayMist Spa"),
            LegalName = GetValue(settings, "LegalName", "MiddayMist Spa Inc."),
            TaxId = GetValue(settings, "TaxId", ""),
            Email = GetValue(settings, "Email", "info@middaymistspa.com"),
            Phone = GetValue(settings, "Phone", ""),
            Address = GetValue(settings, "Address", ""),
            City = GetValue(settings, "City", ""),
            State = GetValue(settings, "State", ""),
            ZipCode = GetValue(settings, "ZipCode", ""),
            Website = GetValue(settings, "Website", ""),
            OperatingHours = operatingHours
        };
    }

    public async Task<BusinessInfoDto> SaveBusinessInfoAsync(BusinessInfoDto dto, int updatedByUserId)
    {
        await UpsertSettingAsync("Business", "BusinessName", dto.Name, "String", updatedByUserId);
        await UpsertSettingAsync("Business", "LegalName", dto.LegalName, "String", updatedByUserId);
        await UpsertSettingAsync("Business", "TaxId", dto.TaxId, "String", updatedByUserId);
        await UpsertSettingAsync("Business", "Email", dto.Email, "String", updatedByUserId);
        await UpsertSettingAsync("Business", "Phone", dto.Phone, "String", updatedByUserId);
        await UpsertSettingAsync("Business", "Address", dto.Address, "String", updatedByUserId);
        await UpsertSettingAsync("Business", "City", dto.City, "String", updatedByUserId);
        await UpsertSettingAsync("Business", "State", dto.State, "String", updatedByUserId);
        await UpsertSettingAsync("Business", "ZipCode", dto.ZipCode, "String", updatedByUserId);
        await UpsertSettingAsync("Business", "Website", dto.Website, "String", updatedByUserId);

        var hoursJson = JsonSerializer.Serialize(dto.OperatingHours, _jsonOptions);
        await UpsertSettingAsync("Business", "OperatingHours", hoursJson, "JSON", updatedByUserId);

        await _context.SaveChangesAsync();
        _logger.LogInformation("Business info saved by user {UserId}", updatedByUserId);
        return dto;
    }

    // =========================================================================
    // Notification Settings
    // =========================================================================

    public async Task<NotificationSettingsDto> GetNotificationSettingsAsync()
    {
        var settings = await _context.SystemSettings
            .Where(s => s.Category == "Notification")
            .ToListAsync();

        if (!settings.Any())
            return GetDefaultNotificationSettings();

        var triggersJson = GetValue(settings, "Triggers", "");
        var triggers = string.IsNullOrEmpty(triggersJson)
            ? GetDefaultTriggers()
            : JsonSerializer.Deserialize<List<NotificationTriggerDto>>(triggersJson, _jsonOptions) ?? GetDefaultTriggers();

        return new NotificationSettingsDto
        {
            Enabled = GetBoolValue(settings, "Enabled", true),
            RefreshIntervalSeconds = GetIntValue(settings, "RefreshIntervalSeconds", 30),
            AutoDismissDays = GetIntValue(settings, "AutoDismissDays", 30),
            ShowBadgeCount = GetBoolValue(settings, "ShowBadgeCount", true),
            ShowPopupAlert = GetBoolValue(settings, "ShowPopupAlert", false),
            MaxInDropdown = GetIntValue(settings, "MaxInDropdown", 10),
            Triggers = triggers
        };
    }

    public async Task<NotificationSettingsDto> SaveNotificationSettingsAsync(NotificationSettingsDto dto, int updatedByUserId)
    {
        await UpsertSettingAsync("Notification", "Enabled", dto.Enabled.ToString(), "Boolean", updatedByUserId);
        await UpsertSettingAsync("Notification", "RefreshIntervalSeconds", dto.RefreshIntervalSeconds.ToString(), "Number", updatedByUserId);
        await UpsertSettingAsync("Notification", "AutoDismissDays", dto.AutoDismissDays.ToString(), "Number", updatedByUserId);
        await UpsertSettingAsync("Notification", "ShowBadgeCount", dto.ShowBadgeCount.ToString(), "Boolean", updatedByUserId);
        await UpsertSettingAsync("Notification", "ShowPopupAlert", dto.ShowPopupAlert.ToString(), "Boolean", updatedByUserId);
        await UpsertSettingAsync("Notification", "MaxInDropdown", dto.MaxInDropdown.ToString(), "Number", updatedByUserId);

        var triggersJson = JsonSerializer.Serialize(dto.Triggers, _jsonOptions);
        await UpsertSettingAsync("Notification", "Triggers", triggersJson, "JSON", updatedByUserId);

        await _context.SaveChangesAsync();
        _logger.LogInformation("Notification settings saved by user {UserId}", updatedByUserId);
        return dto;
    }

    // =========================================================================
    // User Management
    // =========================================================================

    public async Task<List<UserListResponse>> GetUsersAsync()
    {
        var users = await _context.Users
            .Include(u => u.Role)
            .OrderBy(u => u.RoleId)
            .ThenBy(u => u.LastName)
            .ToListAsync();

        // Get employee links
        var userIds = users.Select(u => u.UserId).ToList();
        var employees = await _context.Employees
            .Where(e => e.UserId != null && userIds.Contains(e.UserId.Value))
            .Select(e => new { e.UserId, e.EmployeeId })
            .ToListAsync();

        var employeeMap = employees.ToDictionary(e => e.UserId!.Value, e => e.EmployeeId);

        return users.Select(u => new UserListResponse
        {
            UserId = u.UserId,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Username = u.Username,
            Email = u.Email,
            Role = u.Role.RoleName,
            RoleId = u.RoleId,
            Status = u.IsActive ? "Active" : "Inactive",
            Phone = u.PhoneNumber,
            LastLogin = u.LastLoginAt,
            EmployeeId = employeeMap.GetValueOrDefault(u.UserId)
        }).ToList();
    }

    public async Task<UserListResponse> CreateUserAsync(CreateUserRequest request)
    {
        // Find role
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == request.Role)
            ?? throw new ArgumentException($"Role '{request.Role}' not found");

        // Check username uniqueness
        var exists = await _context.Users.AnyAsync(u => u.Username == request.Username);
        if (exists)
            throw new ArgumentException($"Username '{request.Username}' already exists");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            RoleId = role.RoleId,
            PhoneNumber = request.Phone,
            IsActive = true,
            MustChangePassword = true,
            PasswordExpiryDate = DateTime.UtcNow.AddDays(90),
            SecurityStamp = Guid.NewGuid().ToString()
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User '{Username}' created with role '{Role}'", user.Username, request.Role);

        return new UserListResponse
        {
            UserId = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Username = user.Username,
            Email = user.Email,
            Role = role.RoleName,
            RoleId = role.RoleId,
            Status = "Active",
            Phone = user.PhoneNumber
        };
    }

    public async Task<UserListResponse> UpdateUserAsync(int userId, UpdateUserRequest request)
    {
        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new ArgumentException($"User with ID {userId} not found");

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == request.Role)
            ?? throw new ArgumentException($"Role '{request.Role}' not found");

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Email = request.Email;
        user.RoleId = role.RoleId;
        user.PhoneNumber = request.Phone;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User '{Username}' updated", user.Username);

        return new UserListResponse
        {
            UserId = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Username = user.Username,
            Email = user.Email,
            Role = role.RoleName,
            RoleId = role.RoleId,
            Status = user.IsActive ? "Active" : "Inactive",
            Phone = user.PhoneNumber,
            LastLogin = user.LastLoginAt
        };
    }

    public async Task<UserListResponse> ToggleUserStatusAsync(int userId)
    {
        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId)
            ?? throw new ArgumentException($"User with ID {userId} not found");

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User '{Username}' status toggled to {Status}", user.Username, user.IsActive ? "Active" : "Inactive");

        return new UserListResponse
        {
            UserId = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role.RoleName,
            RoleId = user.RoleId,
            Status = user.IsActive ? "Active" : "Inactive",
            Phone = user.PhoneNumber,
            LastLogin = user.LastLoginAt
        };
    }

    public async Task<bool> ResetUserPasswordAsync(int userId, ResetPasswordRequest request)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new ArgumentException($"User with ID {userId} not found");

        user.PasswordHash = HashPassword(request.NewPassword);
        user.MustChangePassword = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Password reset for user '{Username}'", user.Username);
        return true;
    }

    // =========================================================================
    // Role Management
    // =========================================================================

    public async Task<List<RoleListResponse>> GetRolesAsync()
    {
        var roles = await _context.Roles
            .Include(r => r.Users)
            .OrderBy(r => r.RoleId)
            .ToListAsync();

        // Load all role permissions from SystemSettings
        var permSettings = await _context.SystemSettings
            .Where(s => s.Category == "RolePermissions")
            .ToListAsync();

        var result = new List<RoleListResponse>();
        foreach (var role in roles)
        {
            var permSetting = permSettings.FirstOrDefault(s => s.SettingKey == $"Role_{role.RoleId}_Permissions");
            HashSet<string> permissions;

            if (permSetting != null && !string.IsNullOrEmpty(permSetting.SettingValue))
            {
                permissions = JsonSerializer.Deserialize<HashSet<string>>(permSetting.SettingValue, _jsonOptions) ?? new HashSet<string>();
            }
            else
            {
                // Return default permissions for system roles
                permissions = GetDefaultPermissions(role.RoleName);
            }

            result.Add(new RoleListResponse
            {
                RoleId = role.RoleId,
                RoleCode = role.RoleCode,
                RoleName = role.RoleName,
                Description = role.Description,
                IsActive = role.IsActive,
                IsSystem = SystemRoleCodes.Contains(role.RoleCode.ToUpperInvariant()),
                UserCount = role.Users.Count,
                Icon = RoleIcons.GetValueOrDefault(role.RoleName, "bi-person"),
                Permissions = permissions
            });
        }

        return result;
    }

    public async Task<RoleListResponse> CreateRoleAsync(CreateRoleRequest request)
    {
        var roleCode = request.RoleName.Replace(" ", "").ToUpperInvariant();

        var exists = await _context.Roles.AnyAsync(r => r.RoleCode == roleCode);
        if (exists)
            throw new ArgumentException($"Role '{request.RoleName}' already exists");

        var role = new Role
        {
            RoleCode = roleCode,
            RoleName = request.RoleName,
            Description = request.Description,
            IsActive = true
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        // Copy permissions if requested
        HashSet<string> permissions = new();
        if (request.CopyFromRoleId > 0)
        {
            var sourcePermSetting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Category == "RolePermissions" && s.SettingKey == $"Role_{request.CopyFromRoleId}_Permissions");

            if (sourcePermSetting != null && !string.IsNullOrEmpty(sourcePermSetting.SettingValue))
            {
                permissions = JsonSerializer.Deserialize<HashSet<string>>(sourcePermSetting.SettingValue, _jsonOptions) ?? new();
            }
            else
            {
                var sourceRole = await _context.Roles.FindAsync(request.CopyFromRoleId);
                if (sourceRole != null)
                    permissions = GetDefaultPermissions(sourceRole.RoleName);
            }
        }

        // Save permissions
        var permJson = JsonSerializer.Serialize(permissions, _jsonOptions);
        _context.SystemSettings.Add(new SystemSetting
        {
            SettingKey = $"Role_{role.RoleId}_Permissions",
            SettingValue = permJson,
            SettingType = "JSON",
            Category = "RolePermissions",
            Description = $"Permissions for role: {role.RoleName}",
            IsEditable = true,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        _logger.LogInformation("Role '{RoleName}' created", role.RoleName);

        return new RoleListResponse
        {
            RoleId = role.RoleId,
            RoleCode = role.RoleCode,
            RoleName = role.RoleName,
            Description = role.Description,
            IsActive = true,
            IsSystem = false,
            UserCount = 0,
            Icon = "bi-person",
            Permissions = permissions
        };
    }

    public async Task<RoleListResponse> UpdateRolePermissionsAsync(int roleId, UpdateRolePermissionsRequest request)
    {
        var role = await _context.Roles.Include(r => r.Users).FirstOrDefaultAsync(r => r.RoleId == roleId)
            ?? throw new ArgumentException($"Role with ID {roleId} not found");

        // Don't allow modifying SuperAdmin permissions
        if (role.RoleCode == "SUPERADMIN")
            throw new InvalidOperationException("SuperAdmin permissions cannot be modified");

        var permJson = JsonSerializer.Serialize(request.Permissions, _jsonOptions);

        var existing = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.Category == "RolePermissions" && s.SettingKey == $"Role_{roleId}_Permissions");

        if (existing != null)
        {
            existing.SettingValue = permJson;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.SystemSettings.Add(new SystemSetting
            {
                SettingKey = $"Role_{roleId}_Permissions",
                SettingValue = permJson,
                SettingType = "JSON",
                Category = "RolePermissions",
                Description = $"Permissions for role: {role.RoleName}",
                IsEditable = true,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Permissions updated for role '{RoleName}'", role.RoleName);

        return new RoleListResponse
        {
            RoleId = role.RoleId,
            RoleCode = role.RoleCode,
            RoleName = role.RoleName,
            Description = role.Description,
            IsActive = role.IsActive,
            IsSystem = SystemRoleCodes.Contains(role.RoleCode.ToUpperInvariant()),
            UserCount = role.Users.Count,
            Icon = RoleIcons.GetValueOrDefault(role.RoleName, "bi-person"),
            Permissions = request.Permissions
        };
    }

    public async Task<bool> DeleteRoleAsync(int roleId)
    {
        var role = await _context.Roles.Include(r => r.Users).FirstOrDefaultAsync(r => r.RoleId == roleId)
            ?? throw new ArgumentException($"Role with ID {roleId} not found");

        if (SystemRoleCodes.Contains(role.RoleCode.ToUpperInvariant()))
            throw new InvalidOperationException("Cannot delete a system role");

        // Reassign any users with this role to the default "Receptionist" role
        if (role.Users.Any())
        {
            var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleCode == "RECEPTIONIST")
                ?? throw new InvalidOperationException("Default role not found for reassignment");

            foreach (var user in role.Users.ToList())
            {
                user.RoleId = defaultRole.RoleId;
            }
            _logger.LogInformation("Reassigned {Count} users from role '{RoleName}' to '{DefaultRole}'",
                role.Users.Count, role.RoleName, defaultRole.RoleName);
        }

        // Remove permission settings
        var permSetting = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.Category == "RolePermissions" && s.SettingKey == $"Role_{roleId}_Permissions");
        if (permSetting != null)
            _context.SystemSettings.Remove(permSetting);

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Role '{RoleName}' deleted", role.RoleName);
        return true;
    }

    // =========================================================================
    // Helper Methods
    // =========================================================================

    private async Task UpsertSettingAsync(string category, string key, string? value, string type, int updatedByUserId)
    {
        var setting = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.Category == category && s.SettingKey == key);

        if (setting != null)
        {
            setting.SettingValue = value;
            setting.UpdatedAt = DateTime.UtcNow;
            setting.UpdatedBy = updatedByUserId;
        }
        else
        {
            _context.SystemSettings.Add(new SystemSetting
            {
                SettingKey = key,
                SettingValue = value,
                SettingType = type,
                Category = category,
                IsEditable = true,
                UpdatedBy = updatedByUserId,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }

    private static string GetValue(List<SystemSetting> settings, string key, string defaultValue)
        => settings.FirstOrDefault(s => s.SettingKey == key)?.SettingValue ?? defaultValue;

    private static int GetIntValue(List<SystemSetting> settings, string key, int defaultValue)
    {
        var val = settings.FirstOrDefault(s => s.SettingKey == key)?.SettingValue;
        return int.TryParse(val, out var result) ? result : defaultValue;
    }

    private static bool GetBoolValue(List<SystemSetting> settings, string key, bool defaultValue)
    {
        var val = settings.FirstOrDefault(s => s.SettingKey == key)?.SettingValue;
        return bool.TryParse(val, out var result) ? result : defaultValue;
    }

    private static string HashPassword(string password)
    {
        var saltBytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);

        var hash = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, 100000, HashAlgorithmName.SHA256, 32);

        var combined = new byte[saltBytes.Length + hash.Length];
        Buffer.BlockCopy(saltBytes, 0, combined, 0, saltBytes.Length);
        Buffer.BlockCopy(hash, 0, combined, saltBytes.Length, hash.Length);

        return Convert.ToBase64String(combined);
    }

    private static BusinessInfoDto GetDefaultBusinessInfo() => new()
    {
        Name = "MiddayMist Spa",
        LegalName = "MiddayMist Spa Inc.",
        Email = "info@middaymistspa.com",
        OperatingHours = GetDefaultOperatingHours()
    };

    private static List<OperatingHoursDto> GetDefaultOperatingHours() => new()
    {
        new() { Day = "Monday", OpenTime = "09:00", CloseTime = "18:00" },
        new() { Day = "Tuesday", OpenTime = "09:00", CloseTime = "18:00" },
        new() { Day = "Wednesday", OpenTime = "09:00", CloseTime = "18:00" },
        new() { Day = "Thursday", OpenTime = "09:00", CloseTime = "18:00" },
        new() { Day = "Friday", OpenTime = "09:00", CloseTime = "18:00" },
        new() { Day = "Saturday", OpenTime = "10:00", CloseTime = "17:00" },
        new() { Day = "Sunday", IsClosed = true }
    };

    private static NotificationSettingsDto GetDefaultNotificationSettings() => new()
    {
        Triggers = GetDefaultTriggers()
    };

    private static List<NotificationTriggerDto> GetDefaultTriggers() => new()
    {
        new() { Name = "Appointment Booked", Description = "When a new appointment is booked", Enabled = true, Priority = "Normal", Icon = "bi-calendar-plus", IconColor = "text-primary" },
        new() { Name = "Appointment Reminder", Description = "Before a scheduled appointment", Enabled = true, Priority = "High", Icon = "bi-alarm", IconColor = "text-warning" },
        new() { Name = "Appointment Cancelled", Description = "When an appointment is cancelled", Enabled = true, Priority = "Normal", Icon = "bi-calendar-x", IconColor = "text-danger" },
        new() { Name = "Appointment Rescheduled", Description = "When appointment time is changed", Enabled = true, Priority = "Normal", Icon = "bi-calendar-event", IconColor = "text-info" },
        new() { Name = "Payment Received", Description = "After successful payment/transaction", Enabled = true, Priority = "Normal", Icon = "bi-cash-coin", IconColor = "text-success" },
        new() { Name = "Low Stock Alert", Description = "When inventory item falls below threshold", Enabled = true, Priority = "Urgent", Icon = "bi-exclamation-triangle", IconColor = "text-danger" },
        new() { Name = "New Customer", Description = "When a new customer is registered", Enabled = true, Priority = "Low", Icon = "bi-person-plus", IconColor = "text-primary" },
        new() { Name = "Shift Reminder", Description = "Before an employee's shift starts", Enabled = true, Priority = "High", Icon = "bi-clock", IconColor = "text-warning" },
        new() { Name = "Payroll Generated", Description = "When payroll is generated or finalized", Enabled = true, Priority = "Normal", Icon = "bi-wallet2", IconColor = "text-success" },
        new() { Name = "Exchange Rate Update", Description = "When currency rates are refreshed", Enabled = false, Priority = "Low", Icon = "bi-currency-exchange", IconColor = "text-info" }
    };

    private static HashSet<string> GetDefaultPermissions(string roleName) => roleName switch
    {
        "SuperAdmin" or "Admin" => new HashSet<string>
        {
            "dashboard.view",
            "appointments.view", "appointments.create", "appointments.edit", "appointments.delete",
            "customers.view", "customers.create", "customers.edit", "customers.delete",
            "services.view", "services.create", "services.edit", "services.delete",
            "employees.view", "employees.create", "employees.edit", "employees.delete",
            "inventory.view", "inventory.manage",
            "pos.access", "pos.refund", "pos.discount",
            "accounting.view", "accounting.manage",
            "payroll.view", "payroll.manage",
            "shifts.view", "shifts.manage",
            "timeattendance.view", "timeattendance.manage",
            "reports.view", "reports.export",
            "notifications.view", "notifications.manage",
            "settings.access", "settings.manage"
        },
        "Receptionist" => new HashSet<string>
        {
            "dashboard.view",
            "appointments.view", "appointments.create", "appointments.edit",
            "customers.view", "customers.create", "customers.edit",
            "services.view",
            "pos.access",
            "timeattendance.view",
            "notifications.view"
        },
        "Therapist" => new HashSet<string>
        {
            "dashboard.view",
            "appointments.view",
            "customers.view",
            "services.view",
            "timeattendance.view",
            "notifications.view"
        },
        "HR" => new HashSet<string>
        {
            "dashboard.view",
            "employees.view", "employees.create", "employees.edit", "employees.delete",
            "shifts.view", "shifts.manage",
            "payroll.view", "payroll.manage",
            "timeattendance.view", "timeattendance.manage",
            "reports.view", "reports.export",
            "notifications.view"
        },
        "Inventory" => new HashSet<string>
        {
            "dashboard.view",
            "inventory.view", "inventory.manage",
            "reports.view", "reports.export",
            "timeattendance.view",
            "notifications.view"
        },
        "Accountant" => new HashSet<string>
        {
            "dashboard.view",
            "accounting.view", "accounting.manage",
            "payroll.view", "payroll.manage",
            "pos.access",
            "reports.view", "reports.export",
            "timeattendance.view",
            "notifications.view"
        },
        "Sales Ledger" => new HashSet<string>
        {
            "dashboard.view",
            "pos.access",
            "customers.view",
            "services.view",
            "accounting.view",
            "reports.view", "reports.export",
            "timeattendance.view",
            "notifications.view"
        },
        _ => new HashSet<string> { "dashboard.view" }
    };
}
