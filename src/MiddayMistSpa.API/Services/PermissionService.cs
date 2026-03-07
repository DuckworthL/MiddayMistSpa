using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MiddayMistSpa.Infrastructure.Data;
using System.Text.Json;

namespace MiddayMistSpa.API.Services;

/// <summary>
/// Provides permission checking for role-based access control.
/// Reads role permissions from the SystemSettings table and caches them in memory.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Check if a given role has a specific permission (e.g., "services.create").
    /// </summary>
    Task<bool> HasPermissionAsync(string roleName, string permission);

    /// <summary>
    /// Check if a given role has ANY of the specified permissions.
    /// </summary>
    Task<bool> HasAnyPermissionAsync(string roleName, params string[] permissions);

    /// <summary>
    /// Get all permissions for a role.
    /// </summary>
    Task<HashSet<string>> GetPermissionsAsync(string roleName);

    /// <summary>
    /// Invalidate cached permissions for a role (call after updating permissions).
    /// </summary>
    void InvalidateCache(int? roleId = null);
}

public class PermissionService : IPermissionService
{
    private readonly SpaDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PermissionService> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "RolePermissions_";

    // Default permissions for system roles (fallback when no DB entry exists)
    private static readonly Dictionary<string, HashSet<string>> DefaultPermissions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SuperAdmin"] = new HashSet<string>
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
        ["Admin"] = new HashSet<string>
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
        ["Receptionist"] = new HashSet<string>
        {
            "dashboard.view",
            "appointments.view", "appointments.create", "appointments.edit",
            "customers.view", "customers.create", "customers.edit",
            "services.view",
            "pos.access",
            "timeattendance.view",
            "notifications.view"
        },
        ["Therapist"] = new HashSet<string>
        {
            "dashboard.view",
            "appointments.view",
            "customers.view",
            "services.view",
            "timeattendance.view",
            "notifications.view"
        },
        ["HR"] = new HashSet<string>
        {
            "dashboard.view",
            "employees.view", "employees.create", "employees.edit", "employees.delete",
            "shifts.view", "shifts.manage",
            "payroll.view", "payroll.manage",
            "timeattendance.view", "timeattendance.manage",
            "reports.view", "reports.export",
            "notifications.view"
        },
        ["Inventory"] = new HashSet<string>
        {
            "dashboard.view",
            "inventory.view", "inventory.manage",
            "reports.view", "reports.export",
            "timeattendance.view",
            "notifications.view"
        },
        ["Accountant"] = new HashSet<string>
        {
            "dashboard.view",
            "accounting.view", "accounting.manage",
            "payroll.view",
            "reports.view", "reports.export",
            "timeattendance.view",
            "notifications.view"
        },
        ["Sales Ledger"] = new HashSet<string>
        {
            "dashboard.view",
            "pos.access",
            "customers.view",
            "services.view",
            "accounting.view",
            "reports.view", "reports.export",
            "timeattendance.view",
            "notifications.view"
        }
    };

    public PermissionService(SpaDbContext context, IMemoryCache cache, ILogger<PermissionService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> HasPermissionAsync(string roleName, string permission)
    {
        // SuperAdmin always has all permissions
        if (string.Equals(roleName, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            return true;

        var permissions = await GetPermissionsAsync(roleName);
        return permissions.Contains(permission);
    }

    public async Task<bool> HasAnyPermissionAsync(string roleName, params string[] permissions)
    {
        if (string.Equals(roleName, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            return true;

        var rolePermissions = await GetPermissionsAsync(roleName);
        return permissions.Any(p => rolePermissions.Contains(p));
    }

    public async Task<HashSet<string>> GetPermissionsAsync(string roleName)
    {
        var cacheKey = CacheKeyPrefix + roleName;

        if (_cache.TryGetValue<HashSet<string>>(cacheKey, out var cached) && cached != null)
            return cached;

        // Look up the role's ID first
        var role = await _context.Roles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.RoleName == roleName);

        if (role == null)
        {
            _logger.LogWarning("Role '{RoleName}' not found in database", roleName);
            return new HashSet<string>();
        }

        // Look up permissions from SystemSettings
        var permSetting = await _context.SystemSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Category == "RolePermissions" && s.SettingKey == $"Role_{role.RoleId}_Permissions");

        HashSet<string> permissions;

        if (permSetting != null && !string.IsNullOrEmpty(permSetting.SettingValue))
        {
            try
            {
                permissions = JsonSerializer.Deserialize<HashSet<string>>(permSetting.SettingValue) ?? new HashSet<string>();
            }
            catch (JsonException)
            {
                _logger.LogWarning("Failed to deserialize permissions for role '{RoleName}', using defaults", roleName);
                permissions = GetDefaultPermissionsForRole(roleName);
            }
        }
        else
        {
            // No DB entry yet — use hardcoded defaults
            permissions = GetDefaultPermissionsForRole(roleName);
        }

        _cache.Set(cacheKey, permissions, CacheDuration);
        return permissions;
    }

    public void InvalidateCache(int? roleId = null)
    {
        if (roleId == null)
        {
            // Invalidate all role caches
            foreach (var roleName in DefaultPermissions.Keys)
            {
                _cache.Remove(CacheKeyPrefix + roleName);
            }
        }
        else
        {
            // We'd need to look up the role name — just invalidate all for simplicity
            foreach (var roleName in DefaultPermissions.Keys)
            {
                _cache.Remove(CacheKeyPrefix + roleName);
            }
        }
    }

    private static HashSet<string> GetDefaultPermissionsForRole(string roleName)
    {
        return DefaultPermissions.TryGetValue(roleName, out var defaults)
            ? new HashSet<string>(defaults)
            : new HashSet<string>();
    }
}
