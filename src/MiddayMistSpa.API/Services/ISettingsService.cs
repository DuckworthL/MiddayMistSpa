using MiddayMistSpa.API.DTOs.Settings;

namespace MiddayMistSpa.API.Services;

public interface ISettingsService
{
    // General Settings
    Task<GeneralSettingsDto> GetGeneralSettingsAsync();
    Task<GeneralSettingsDto> SaveGeneralSettingsAsync(GeneralSettingsDto settings, int updatedByUserId);

    // Business Info
    Task<BusinessInfoDto> GetBusinessInfoAsync();
    Task<BusinessInfoDto> SaveBusinessInfoAsync(BusinessInfoDto info, int updatedByUserId);

    // Notification Settings
    Task<NotificationSettingsDto> GetNotificationSettingsAsync();
    Task<NotificationSettingsDto> SaveNotificationSettingsAsync(NotificationSettingsDto settings, int updatedByUserId);

    // User Management
    Task<List<UserListResponse>> GetUsersAsync();
    Task<UserListResponse> CreateUserAsync(CreateUserRequest request);
    Task<UserListResponse> UpdateUserAsync(int userId, UpdateUserRequest request);
    Task<UserListResponse> ToggleUserStatusAsync(int userId);
    Task<bool> ResetUserPasswordAsync(int userId, ResetPasswordRequest request);

    // Role Management
    Task<List<RoleListResponse>> GetRolesAsync();
    Task<RoleListResponse> CreateRoleAsync(CreateRoleRequest request);
    Task<RoleListResponse> UpdateRolePermissionsAsync(int roleId, UpdateRolePermissionsRequest request);
    Task<bool> DeleteRoleAsync(int roleId);
}
