namespace MiddayMistSpa.Web.Services;

/// <summary>
/// Service for managing role-based permissions and navigation access.
/// Fetches permissions from the API (which reads from the database) and caches them.
/// </summary>
public interface IRolePermissionService
{
    bool CanAccessPage(string? userRole, string pagePath);
    List<NavMenuItem> GetNavMenuForRole(string? userRole);

    /// <summary>
    /// Check if the current user has a specific permission key (e.g., "services.create").
    /// </summary>
    bool HasPermission(string permissionKey);

    /// <summary>
    /// Load permissions for the current user from the API. Call after login.
    /// </summary>
    Task LoadPermissionsAsync();

    /// <summary>
    /// Clear cached permissions. Call on logout.
    /// </summary>
    void ClearPermissions();
}

public class RolePermissionService : IRolePermissionService
{
    private readonly IApiClient _apiClient;
    private HashSet<string> _cachedPermissions = new();
    private string? _cachedRole;
    private bool _loaded;

    // Maps permission keys to page paths
    private static readonly Dictionary<string, string[]> PermissionToPages = new()
    {
        ["dashboard.view"] = new[] { "/" },
        ["appointments.view"] = new[] { "/appointments" },
        ["customers.view"] = new[] { "/customers", "/customers/segmentation" },
        ["services.view"] = new[] { "/services" },
        ["employees.view"] = new[] { "/employees" },
        ["inventory.view"] = new[] { "/inventory" },
        ["pos.access"] = new[] { "/pos" },
        ["accounting.view"] = new[] { "/transactions", "/accounting", "/accounting/income", "/accounting/expenses", "/accounting/invoices", "/accounting/journal" },
        ["payroll.view"] = new[] { "/payroll" },
        ["shifts.view"] = new[] { "/shifts" },
        ["timeattendance.view"] = new[] { "/time-attendance" },
        ["reports.view"] = new[] { "/reports" },
        ["notifications.view"] = new[] { "/notifications" },
        ["settings.access"] = new[] { "/settings" }
    };

    // Hardcoded fallback for when API is unavailable
    private static readonly Dictionary<string, HashSet<string>> FallbackRolePermissions = new()
    {
        ["SuperAdmin"] = new HashSet<string>
        {
            "/", "/appointments", "/customers", "/customers/segmentation", "/services", "/pos",
            "/employees", "/shifts", "/inventory", "/transactions", "/accounting", "/accounting/income",
            "/accounting/expenses", "/accounting/invoices", "/accounting/journal",
            "/payroll", "/time-attendance", "/reports", "/settings", "/profile", "/notifications"
        },
        ["Admin"] = new HashSet<string>
        {
            "/", "/appointments", "/customers", "/customers/segmentation", "/services", "/pos",
            "/employees", "/shifts", "/inventory", "/transactions", "/accounting", "/accounting/income",
            "/accounting/expenses", "/accounting/invoices", "/accounting/journal",
            "/payroll", "/time-attendance", "/reports", "/settings", "/profile", "/notifications"
        },
        ["Sales Ledger"] = new HashSet<string>
        {
            "/", "/transactions", "/customers", "/customers/segmentation", "/services",
            "/accounting", "/accounting/income", "/accounting/invoices",
            "/reports", "/time-attendance", "/profile", "/notifications"
        }
    };

    public RolePermissionService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public bool HasPermission(string permissionKey)
    {
        if (string.IsNullOrEmpty(permissionKey))
            return false;

        // SuperAdmin always has all permissions
        if (_cachedRole == "SuperAdmin")
            return true;

        return _cachedPermissions.Contains(permissionKey);
    }

    public async Task LoadPermissionsAsync()
    {
        try
        {
            var permissions = await _apiClient.GetAsync<HashSet<string>>("api/auth/me/permissions");
            if (permissions != null)
            {
                _cachedPermissions = permissions;
                _loaded = true;
            }
        }
        catch
        {
            // If API call fails, keep existing permissions
        }
    }

    public void ClearPermissions()
    {
        _cachedPermissions = new HashSet<string>();
        _cachedRole = null;
        _loaded = false;
    }

    public bool CanAccessPage(string? userRole, string pagePath)
    {
        if (string.IsNullOrEmpty(userRole))
            return false;

        _cachedRole = userRole;

        // Always allow profile
        var normalizedPath = pagePath.ToLowerInvariant().TrimEnd('/');
        if (string.IsNullOrEmpty(normalizedPath))
            normalizedPath = "/";

        if (normalizedPath == "/profile")
            return true;

        // SuperAdmin and Admin have access to everything
        if (userRole == "SuperAdmin" || userRole == "Admin")
            return true;

        // If we have loaded permissions from the API, use them
        if (_loaded && _cachedPermissions.Count > 0)
        {
            // Build allowed pages from permission keys
            var allowedPages = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "/", "/profile", "/notifications" };
            foreach (var perm in _cachedPermissions)
            {
                if (PermissionToPages.TryGetValue(perm, out var pages))
                {
                    foreach (var page in pages)
                        allowedPages.Add(page);
                }
            }

            return allowedPages.Any(p =>
                normalizedPath.Equals(p, StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.StartsWith(p + "/", StringComparison.OrdinalIgnoreCase));
        }

        // Fallback to hardcoded permissions if API hasn't been called yet
        if (FallbackRolePermissions.TryGetValue(userRole, out var fallbackPerms))
        {
            return fallbackPerms.Any(p =>
                normalizedPath.Equals(p, StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.StartsWith(p + "/", StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }

    public List<NavMenuItem> GetNavMenuForRole(string? userRole)
    {
        var allItems = GetAllNavItems();

        if (string.IsNullOrEmpty(userRole))
            return new List<NavMenuItem>();

        _cachedRole = userRole;

        // SuperAdmin and Admin see everything
        if (userRole == "SuperAdmin" || userRole == "Admin")
            return allItems;

        // If we have loaded permissions from the API, use them
        if (_loaded && _cachedPermissions.Count > 0)
        {
            var allowedPages = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "/", "/profile", "/notifications" };
            foreach (var perm in _cachedPermissions)
            {
                if (PermissionToPages.TryGetValue(perm, out var pages))
                {
                    foreach (var page in pages)
                        allowedPages.Add(page);
                }
            }

            return allItems
                .Where(item => allowedPages.Contains(item.Href.ToLowerInvariant()))
                .ToList();
        }

        // Fallback
        if (FallbackRolePermissions.TryGetValue(userRole, out var fallbackPerms))
        {
            return allItems
                .Where(item => fallbackPerms.Contains(item.Href.ToLowerInvariant()))
                .ToList();
        }

        // Unknown role — show dashboard and time attendance only
        return allItems.Where(item => item.Href == "/" || item.Href == "/time-attendance" || item.Href == "/notifications").ToList();
    }

    private static List<NavMenuItem> GetAllNavItems()
    {
        return new List<NavMenuItem>
        {
            // Main Navigation
            new() { Href = "/", Icon = "bi-speedometer2", Label = "Dashboard", Section = null },
            new() { Href = "/appointments", Icon = "bi-calendar-check", Label = "Appointments", Section = null },
            new() { Href = "/customers", Icon = "bi-people", Label = "Customers", Section = null },
            new() { Href = "/customers/segmentation", Icon = "bi-diagram-3", Label = "Segmentation", Section = null },
            new() { Href = "/services", Icon = "bi-gem", Label = "Services", Section = null },
            new() { Href = "/pos", Icon = "bi-cart3", Label = "Point of Sale", Section = null },
            
            // Management Section
            new() { Href = "/employees", Icon = "bi-person-badge", Label = "Employees", Section = "Management" },
            new() { Href = "/inventory", Icon = "bi-box-seam", Label = "Inventory", Section = "Management" },
            new() { Href = "/transactions", Icon = "bi-receipt", Label = "Transactions", Section = "Management" },
            new() { Href = "/accounting", Icon = "bi-calculator", Label = "Accounting", Section = "Management" },
            
            // HR & Payroll Section
            new() { Href = "/shifts", Icon = "bi-calendar3-week", Label = "Shift Management", Section = "HR & Payroll" },
            new() { Href = "/payroll", Icon = "bi-cash-coin", Label = "Payroll", Section = "HR & Payroll" },
            new() { Href = "/time-attendance", Icon = "bi-clock-history", Label = "Time & Attendance", Section = "HR & Payroll" },
            
            // Reports Section
            new() { Href = "/reports", Icon = "bi-bar-chart-line", Label = "Reports", Section = "Reports" },
            
            // Settings Section
            new() { Href = "/settings", Icon = "bi-gear", Label = "Settings", Section = "Settings" }
        };
    }
}

public class NavMenuItem
{
    public string Href { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Label { get; set; } = "";
    public string? Section { get; set; }
}
