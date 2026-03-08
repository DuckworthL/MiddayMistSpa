namespace MiddayMistSpa.API.DTOs.Settings;

// =============================================================================
// General Settings
// =============================================================================

public record GeneralSettingsDto
{
    public string AppName { get; init; } = "MiddayMist Spa";
    public string Currency { get; init; } = "PHP";
    public string DateFormat { get; init; } = "MM/dd/yyyy";
    public string TimeFormat { get; init; } = "12";
    public string Timezone { get; init; } = "Asia/Manila";
    public string Language { get; init; } = "en";
    public int DefaultDuration { get; init; } = 60;
    public int BookingLeadTime { get; init; } = 24;
    public int CancellationWindow { get; init; } = 48;
    public bool AllowOnlineBooking { get; init; } = true;
    public bool RequireDeposit { get; init; }
    public bool SendReminders { get; init; } = true;
}

// =============================================================================
// Business Info Settings
// =============================================================================

public record BusinessInfoDto
{
    public string Name { get; init; } = "MiddayMist Spa";
    public string LegalName { get; init; } = "MiddayMist Spa Inc.";
    public string TaxId { get; init; } = "";
    public string Email { get; init; } = "info@middaymistspa.com";
    public string Phone { get; init; } = "";
    public string Address { get; init; } = "";
    public string City { get; init; } = "";
    public string State { get; init; } = "";
    public string ZipCode { get; init; } = "";
    public string Website { get; init; } = "";
    public List<OperatingHoursDto> OperatingHours { get; init; } = new();
}

public record OperatingHoursDto
{
    public string Day { get; init; } = "";
    public string OpenTime { get; init; } = "09:00";
    public string CloseTime { get; init; } = "18:00";
    public bool IsClosed { get; init; }
}

// =============================================================================
// Notification Settings
// =============================================================================

public record NotificationSettingsDto
{
    public bool Enabled { get; init; } = true;
    public int RefreshIntervalSeconds { get; init; } = 30;
    public int AutoDismissDays { get; init; } = 30;
    public bool ShowBadgeCount { get; init; } = true;
    public bool ShowPopupAlert { get; init; }
    public int MaxInDropdown { get; init; } = 10;
    public List<NotificationTriggerDto> Triggers { get; init; } = new();
}

public record NotificationTriggerDto
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public bool Enabled { get; init; } = true;
    public string Priority { get; init; } = "Normal";
    public string Icon { get; init; } = "bi-bell";
    public string IconColor { get; init; } = "text-muted";
}

// =============================================================================
// User Management
// =============================================================================

public record UserListResponse
{
    public int UserId { get; init; }
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string Username { get; init; } = "";
    public string Email { get; init; } = "";
    public string Role { get; init; } = "";
    public int RoleId { get; init; }
    public string Status { get; init; } = "Active";
    public string? Phone { get; init; }
    public DateTime? LastLogin { get; init; }
    public int? EmployeeId { get; init; }
}

public record CreateUserRequest
{
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string Username { get; init; } = "";
    public string Email { get; init; } = "";
    public string Password { get; init; } = "";
    public string Role { get; init; } = "Therapist";
    public string? Phone { get; init; }
}

public record UpdateUserRequest
{
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string Email { get; init; } = "";
    public string Role { get; init; } = "";
    public string? Phone { get; init; }
}

public record ResetPasswordRequest
{
    public string NewPassword { get; init; } = "";
}

// =============================================================================
// Role Management
// =============================================================================

public record RoleListResponse
{
    public int RoleId { get; init; }
    public string RoleCode { get; init; } = "";
    public string RoleName { get; init; } = "";
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public bool IsSystem { get; init; }
    public int UserCount { get; init; }
    public string Icon { get; init; } = "bi-person";
    public HashSet<string> Permissions { get; init; } = new();
}

public record CreateRoleRequest
{
    public string RoleName { get; init; } = "";
    public string? Description { get; init; }
    public int CopyFromRoleId { get; init; }
}

public record UpdateRolePermissionsRequest
{
    public HashSet<string> Permissions { get; init; } = new();
}

// =============================================================================
// Service Category (extends existing DTOs)
// =============================================================================

public record ServiceCategorySettingsResponse
{
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = "";
    public string? Description { get; init; }
    public int DisplayOrder { get; init; }
    public int ServiceCount { get; init; }
    public bool IsActive { get; init; }
    public string Color { get; init; } = "#8B5A9C";
}

// =============================================================================
// Philippine Holidays
// =============================================================================

public record HolidayResponse
{
    public int HolidayId { get; init; }
    public string HolidayName { get; init; } = "";
    public DateTime HolidayDate { get; init; }
    public string HolidayType { get; init; } = "";
    public int Year { get; init; }
    public bool IsRecurring { get; init; }
}

public record CreateHolidayRequest
{
    public string HolidayName { get; init; } = "";
    public DateTime HolidayDate { get; init; }
    public string HolidayType { get; init; } = "Regular";
    public bool IsRecurring { get; init; } = true;
}

public record UpdateHolidayRequest
{
    public string HolidayName { get; init; } = "";
    public DateTime HolidayDate { get; init; }
    public string HolidayType { get; init; } = "Regular";
    public bool IsRecurring { get; init; } = true;
}
