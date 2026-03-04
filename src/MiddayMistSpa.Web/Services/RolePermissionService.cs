namespace MiddayMistSpa.Web.Services;

/// <summary>
/// Service for managing role-based permissions and navigation access
/// </summary>
public interface IRolePermissionService
{
    bool CanAccessPage(string? userRole, string pagePath);
    List<NavMenuItem> GetNavMenuForRole(string? userRole);
}

public class RolePermissionService : IRolePermissionService
{
    private static readonly Dictionary<string, HashSet<string>> RolePermissions = new()
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
        ["Receptionist"] = new HashSet<string>
        {
            "/", "/appointments", "/customers", "/services", "/pos", "/time-attendance", "/profile", "/notifications"
        },
        ["Therapist"] = new HashSet<string>
        {
            "/", "/appointments", "/time-attendance", "/profile", "/notifications"
        },
        ["Inventory"] = new HashSet<string>
        {
            "/", "/inventory", "/time-attendance", "/reports", "/profile", "/notifications"
        },
        ["Accountant"] = new HashSet<string>
        {
            "/", "/transactions", "/accounting", "/accounting/income", "/accounting/expenses",
            "/accounting/invoices", "/accounting/journal", "/payroll", "/time-attendance", "/reports", "/profile", "/notifications"
        },
        ["HR"] = new HashSet<string>
        {
            "/", "/employees", "/shifts", "/payroll", "/time-attendance", "/reports", "/profile", "/notifications"
        },
        ["Sales"] = new HashSet<string>
        {
            "/", "/customers", "/customers/segmentation", "/transactions", "/time-attendance", "/reports", "/profile", "/notifications"
        }
    };

    public bool CanAccessPage(string? userRole, string pagePath)
    {
        if (string.IsNullOrEmpty(userRole))
            return false;

        // Normalize the path
        var normalizedPath = pagePath.ToLowerInvariant().TrimEnd('/');
        if (string.IsNullOrEmpty(normalizedPath))
            normalizedPath = "/";

        // SuperAdmin and Admin have access to everything
        if (userRole == "SuperAdmin" || userRole == "Admin")
            return true;

        if (RolePermissions.TryGetValue(userRole, out var permissions))
        {
            return permissions.Any(p =>
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

        // SuperAdmin and Admin see everything
        if (userRole == "SuperAdmin" || userRole == "Admin")
            return allItems;

        if (!RolePermissions.TryGetValue(userRole, out var permissions))
            return new List<NavMenuItem>();

        // Filter items based on role permissions
        return allItems
            .Where(item => permissions.Contains(item.Href.ToLowerInvariant()))
            .ToList();
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
