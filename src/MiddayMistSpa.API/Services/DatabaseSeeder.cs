using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using MiddayMistSpa.Core.Entities.Accounting;
using MiddayMistSpa.Core.Entities.Appointment;
using MiddayMistSpa.Core.Entities.Configuration;
using MiddayMistSpa.Core.Entities.Customer;
using MiddayMistSpa.Core.Entities.Employee;
using MiddayMistSpa.Core.Entities.Identity;
using MiddayMistSpa.Core.Entities.Inventory;
using MiddayMistSpa.Core.Entities.Payroll;
using MiddayMistSpa.Core.Entities.Service;
using MiddayMistSpa.Infrastructure.Data;

namespace MiddayMistSpa.API.Services;

public interface IDatabaseSeeder
{
    Task SeedAsync();
}

public class DatabaseSeeder : IDatabaseSeeder
{
    private readonly SpaDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(SpaDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            // Apply any pending migrations
            await _context.Database.MigrateAsync();

            // Core security data
            await SeedRolesAsync();
            await SeedUsersAsync();
            await SeedSystemSettingsAsync();

            // Philippine payroll compliance tables
            await SeedSSSContributionRatesAsync();
            await SeedPhilHealthContributionRatesAsync();
            await SeedPagIBIGContributionRatesAsync();
            await SeedWithholdingTaxBracketsAsync();
            await SeedPhilippineHolidaysAsync();

            // Sample business data
            await SeedServiceCategoriesAndServicesAsync();
            await SeedProductCategoriesAndProductsAsync();
            await SeedSampleCustomersAsync();
            await SeedSampleEmployeesAsync();
            await LinkUsersToEmployeesAsync();
            await SeedEmployeeShiftsAsync();
            await SeedAttendanceRecordsAsync();
            await SeedRoomsAsync();

            // Accounting
            await SeedChartOfAccountsAsync();
            await EnsureContraRevenueAccountsAsync();

            // Ensure new settings exist (for existing databases)
            await EnsureCaptchaSettingsAsync();

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private async Task SeedRolesAsync()
    {
        if (await _context.Roles.AnyAsync())
        {
            _logger.LogInformation("Roles already seeded, skipping...");
            return;
        }

        var roles = new List<Role>
        {
            new() { RoleCode = "SUPERADMIN", RoleName = "SuperAdmin", Description = "Full system access with all permissions", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { RoleCode = "ADMIN", RoleName = "Admin", Description = "Administrative access to most system features", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { RoleCode = "RECEPTIONIST", RoleName = "Receptionist", Description = "Front desk operations, appointments, and customer management", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { RoleCode = "THERAPIST", RoleName = "Therapist", Description = "View appointments and update service status", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { RoleCode = "INVENTORY", RoleName = "Inventory", Description = "Manage products, stock, and purchase orders", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { RoleCode = "ACCOUNTANT", RoleName = "Accountant", Description = "Financial management, reports, and payroll", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { RoleCode = "HR", RoleName = "HR", Description = "Employee management, schedules, and time-off requests", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { RoleCode = "SALES_LEDGER", RoleName = "Sales Ledger", Description = "View sales reports and customer analytics", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        await _context.Roles.AddRangeAsync(roles);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} roles", roles.Count);
    }

    private async Task SeedUsersAsync()
    {
        var existingUsernames = await _context.Users.Select(u => u.Username).ToListAsync();

        // Get all roles
        var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "SuperAdmin");
        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
        var receptionistRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Receptionist");
        var therapistRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Therapist");
        var inventoryRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Inventory");
        var accountantRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Accountant");
        var hrRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "HR");
        var salesLedgerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Sales Ledger");

        if (superAdminRole == null || adminRole == null)
        {
            _logger.LogError("Required roles not found. Please seed roles first.");
            return;
        }

        var users = new List<User>
        {
            // SuperAdmin account
            new()
            {
                Username = "superadmin",
                Email = "superadmin@middaymistspa.com",
                EmailConfirmed = true,
                PasswordHash = HashPassword("SuperAdmin@2026!"),
                SecurityStamp = Guid.NewGuid().ToString(),
                RoleId = superAdminRole.RoleId,
                FirstName = "System",
                LastName = "Administrator",
                IsActive = true,
                LockoutEnabled = false,
                PasswordExpiryDate = DateTime.UtcNow.AddYears(1),
                MustChangePassword = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            // Admin account
            new()
            {
                Username = "admin",
                Email = "admin@middaymistspa.com",
                EmailConfirmed = true,
                PasswordHash = HashPassword("Admin@2026!"),
                SecurityStamp = Guid.NewGuid().ToString(),
                RoleId = adminRole.RoleId,
                FirstName = "Spa",
                LastName = "Admin",
                IsActive = true,
                LockoutEnabled = true,
                PasswordExpiryDate = DateTime.UtcNow.AddDays(90),
                MustChangePassword = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            // Receptionist account
            new()
            {
                Username = "receptionist",
                Email = "receptionist@middaymistspa.com",
                EmailConfirmed = true,
                PasswordHash = HashPassword("Receptionist@2026!"),
                SecurityStamp = Guid.NewGuid().ToString(),
                RoleId = receptionistRole?.RoleId ?? adminRole.RoleId,
                FirstName = "Maria",
                LastName = "Santos",
                IsActive = true,
                LockoutEnabled = true,
                PasswordExpiryDate = DateTime.UtcNow.AddDays(90),
                MustChangePassword = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            // Therapist account
            new()
            {
                Username = "therapist",
                Email = "therapist@middaymistspa.com",
                EmailConfirmed = true,
                PasswordHash = HashPassword("Therapist@2026!"),
                SecurityStamp = Guid.NewGuid().ToString(),
                RoleId = therapistRole?.RoleId ?? adminRole.RoleId,
                FirstName = "Ana",
                LastName = "Reyes",
                IsActive = true,
                LockoutEnabled = true,
                PasswordExpiryDate = DateTime.UtcNow.AddDays(90),
                MustChangePassword = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            // Inventory account
            new()
            {
                Username = "inventory",
                Email = "inventory@middaymistspa.com",
                EmailConfirmed = true,
                PasswordHash = HashPassword("Inventory@2026!"),
                SecurityStamp = Guid.NewGuid().ToString(),
                RoleId = inventoryRole?.RoleId ?? adminRole.RoleId,
                FirstName = "Jose",
                LastName = "Cruz",
                IsActive = true,
                LockoutEnabled = true,
                PasswordExpiryDate = DateTime.UtcNow.AddDays(90),
                MustChangePassword = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            // Accountant account
            new()
            {
                Username = "accountant",
                Email = "accountant@middaymistspa.com",
                EmailConfirmed = true,
                PasswordHash = HashPassword("Accountant@2026!"),
                SecurityStamp = Guid.NewGuid().ToString(),
                RoleId = accountantRole?.RoleId ?? adminRole.RoleId,
                FirstName = "Patricia",
                LastName = "Garcia",
                IsActive = true,
                LockoutEnabled = true,
                PasswordExpiryDate = DateTime.UtcNow.AddDays(90),
                MustChangePassword = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            // HR account
            new()
            {
                Username = "hr",
                Email = "hr@middaymistspa.com",
                EmailConfirmed = true,
                PasswordHash = HashPassword("HR@2026!"),
                SecurityStamp = Guid.NewGuid().ToString(),
                RoleId = hrRole?.RoleId ?? adminRole.RoleId,
                FirstName = "Carmen",
                LastName = "Dela Cruz",
                IsActive = true,
                LockoutEnabled = true,
                PasswordExpiryDate = DateTime.UtcNow.AddDays(90),
                MustChangePassword = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            // Sales Ledger account
            new()
            {
                Username = "salesledger",
                Email = "salesledger@middaymistspa.com",
                EmailConfirmed = true,
                PasswordHash = HashPassword("SalesLedger@2026!"),
                SecurityStamp = Guid.NewGuid().ToString(),
                RoleId = salesLedgerRole?.RoleId ?? adminRole.RoleId,
                FirstName = "Roberto",
                LastName = "Mendoza",
                IsActive = true,
                LockoutEnabled = true,
                PasswordExpiryDate = DateTime.UtcNow.AddDays(90),
                MustChangePassword = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        var newUsers = users.Where(u => !existingUsernames.Contains(u.Username)).ToList();

        if (newUsers.Any())
        {
            await _context.Users.AddRangeAsync(newUsers);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} new users (skipped {Skipped} existing)", newUsers.Count, users.Count - newUsers.Count);
        }
        else
        {
            _logger.LogInformation("All users already seeded, skipping inserts...");
        }

        // Re-sync passwords for existing demo users to ensure demo credentials always work
        var demoPasswords = users.ToDictionary(u => u.Username, u => u.PasswordHash);
        var existingUsers = await _context.Users
            .Where(u => demoPasswords.Keys.Contains(u.Username))
            .ToListAsync();

        var updatedCount = 0;
        foreach (var eu in existingUsers)
        {
            var expectedHash = demoPasswords[eu.Username];
            if (eu.PasswordHash != expectedHash)
            {
                eu.PasswordHash = expectedHash;
                eu.IsActive = true;
                eu.LockoutEnd = null;
                eu.LockoutEnabled = false;
                eu.AccessFailedCount = 0;
                eu.UpdatedAt = DateTime.UtcNow;
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Re-synced passwords for {Count} demo users", updatedCount);
        }
    }

    private async Task SeedSystemSettingsAsync()
    {
        if (await _context.SystemSettings.AnyAsync())
        {
            _logger.LogInformation("System settings already seeded, skipping...");
            return;
        }

        var settings = new List<SystemSetting>
        {
            // Business Information
            new() { SettingKey = "Business.Name", SettingValue = "MiddayMist Spa", SettingType = "String", Category = "Business", Description = "Business name", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "Business.Address", SettingValue = "123 Wellness Avenue, Manila, Philippines", SettingType = "String", Category = "Business", Description = "Business address", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "Business.Phone", SettingValue = "+63 2 1234 5678", SettingType = "String", Category = "Business", Description = "Business phone number", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "Business.Email", SettingValue = "info@middaymistspa.com", SettingType = "String", Category = "Business", Description = "Business email", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "Business.TIN", SettingValue = "", SettingType = "String", Category = "Business", Description = "Tax Identification Number", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            
            // Operating Hours
            new() { SettingKey = "Hours.OpenTime", SettingValue = "09:00", SettingType = "String", Category = "Hours", Description = "Opening time", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "Hours.CloseTime", SettingValue = "21:00", SettingType = "String", Category = "Hours", Description = "Closing time", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "Hours.DaysOpen", SettingValue = "Monday,Tuesday,Wednesday,Thursday,Friday,Saturday,Sunday", SettingType = "String", Category = "Hours", Description = "Days of operation", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            
            // Appointment Settings
            new() { SettingKey = "Appointment.DefaultDuration", SettingValue = "60", SettingType = "Number", Category = "Appointment", Description = "Default appointment duration in minutes", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "Appointment.BufferTime", SettingValue = "15", SettingType = "Number", Category = "Appointment", Description = "Buffer time between appointments in minutes", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "Appointment.MaxAdvanceBookingDays", SettingValue = "30", SettingType = "Number", Category = "Appointment", Description = "Maximum days in advance for booking", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "Appointment.CancellationHours", SettingValue = "24", SettingType = "Number", Category = "Appointment", Description = "Hours before appointment for free cancellation", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            
            // Financial Settings
            new() { SettingKey = "Finance.Currency", SettingValue = "PHP", SettingType = "String", Category = "Finance", Description = "Default currency", IsEditable = false, UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "Finance.TaxRate", SettingValue = "12", SettingType = "Number", Category = "Finance", Description = "VAT rate percentage", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "Finance.ServiceCharge", SettingValue = "10", SettingType = "Number", Category = "Finance", Description = "Service charge percentage", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            
            // Inventory Settings
            new() { SettingKey = "Inventory.LowStockThreshold", SettingValue = "10", SettingType = "Number", Category = "Inventory", Description = "Low stock warning threshold", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "Inventory.ReorderPoint", SettingValue = "5", SettingType = "Number", Category = "Inventory", Description = "Automatic reorder point", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            
            // Payroll Settings (Philippine compliance)
            new() { SettingKey = "Payroll.PayFrequency", SettingValue = "SemiMonthly", SettingType = "String", Category = "Payroll", Description = "Pay frequency (SemiMonthly, Monthly)", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "Payroll.FirstCutoff", SettingValue = "15", SettingType = "Number", Category = "Payroll", Description = "First cutoff day of month", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "Payroll.SecondCutoff", SettingValue = "30", SettingType = "Number", Category = "Payroll", Description = "Second cutoff day of month", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "Payroll.MinimumWage", SettingValue = "610", SettingType = "Number", Category = "Payroll", Description = "Daily minimum wage (NCR)", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            
            // Integration Settings
            new() { SettingKey = "Integration.MultiCurrencyEnabled", SettingValue = "true", SettingType = "Boolean", Category = "Integration", Description = "Enable multi-currency POS support", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "Integration.CurrencyRefreshMinutes", SettingValue = "30", SettingType = "Number", Category = "Integration", Description = "Currency rate refresh interval in minutes", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            
            // System Settings
            new() { SettingKey = "System.MaintenanceMode", SettingValue = "false", SettingType = "Boolean", Category = "System", Description = "Enable maintenance mode", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "System.SessionTimeout", SettingValue = "30", SettingType = "Number", Category = "System", Description = "Session timeout in minutes", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "System.MaxConcurrentSessions", SettingValue = "2", SettingType = "Number", Category = "System", Description = "Maximum concurrent sessions per user", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            
            // Captcha Settings (off by default, Google reCAPTCHA v2)
            new() { SettingKey = "Captcha.Enabled", SettingValue = "false", SettingType = "Boolean", Category = "Captcha", Description = "Enable reCAPTCHA on login page", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "Captcha.SiteKey", SettingValue = "6LfHZn8sAAAAAKBb6fKq8naNpNe94LfBVIlvpP-g", SettingType = "String", Category = "Captcha", Description = "Google reCAPTCHA v2 site key", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "Captcha.SecretKey", SettingValue = "6LfHZn8sAAAAAL75V5kS1ugi4zENyDRuMXFGUC4j", SettingType = "String", Category = "Captcha", Description = "Google reCAPTCHA v2 secret key", IsEditable = true, UpdatedAt = DateTime.UtcNow }
        };

        await _context.SystemSettings.AddRangeAsync(settings);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} system settings", settings.Count);
    }

    private static string HashPassword(string password)
    {
        var saltBytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            100000,
            HashAlgorithmName.SHA256,
            32);

        // Combine salt and hash for storage
        var combined = new byte[saltBytes.Length + hash.Length];
        Buffer.BlockCopy(saltBytes, 0, combined, 0, saltBytes.Length);
        Buffer.BlockCopy(hash, 0, combined, saltBytes.Length, hash.Length);

        return Convert.ToBase64String(combined);
    }

    #region Philippine Payroll Compliance Seeding

    /// <summary>
    /// 2026 SSS Contribution Table (effective January 2024 schedule)
    /// Based on Republic Act 11199 - Social Security Act of 2018
    /// </summary>
    private async Task SeedSSSContributionRatesAsync()
    {
        if (await _context.SSSContributionRates.AnyAsync())
        {
            _logger.LogInformation("SSS contribution rates already seeded, skipping...");
            return;
        }

        var sssRates = new List<SSSContributionRate>
        {
            // MSC Range: ₱4,000 - ₱30,000 (2024-2025 schedule)
            new() { MinSalary = 0, MaxSalary = 4249.99m, SalaryCredit = 4000, EmployeeShare = 180, EmployerShare = 380, TotalContribution = 560, EffectiveYear = 2026 },
            new() { MinSalary = 4250, MaxSalary = 4749.99m, SalaryCredit = 4500, EmployeeShare = 202.50m, EmployerShare = 427.50m, TotalContribution = 630, EffectiveYear = 2026 },
            new() { MinSalary = 4750, MaxSalary = 5249.99m, SalaryCredit = 5000, EmployeeShare = 225, EmployerShare = 475, TotalContribution = 700, EffectiveYear = 2026 },
            new() { MinSalary = 5250, MaxSalary = 5749.99m, SalaryCredit = 5500, EmployeeShare = 247.50m, EmployerShare = 522.50m, TotalContribution = 770, EffectiveYear = 2026 },
            new() { MinSalary = 5750, MaxSalary = 6249.99m, SalaryCredit = 6000, EmployeeShare = 270, EmployerShare = 570, TotalContribution = 840, EffectiveYear = 2026 },
            new() { MinSalary = 6250, MaxSalary = 6749.99m, SalaryCredit = 6500, EmployeeShare = 292.50m, EmployerShare = 617.50m, TotalContribution = 910, EffectiveYear = 2026 },
            new() { MinSalary = 6750, MaxSalary = 7249.99m, SalaryCredit = 7000, EmployeeShare = 315, EmployerShare = 665, TotalContribution = 980, EffectiveYear = 2026 },
            new() { MinSalary = 7250, MaxSalary = 7749.99m, SalaryCredit = 7500, EmployeeShare = 337.50m, EmployerShare = 712.50m, TotalContribution = 1050, EffectiveYear = 2026 },
            new() { MinSalary = 7750, MaxSalary = 8249.99m, SalaryCredit = 8000, EmployeeShare = 360, EmployerShare = 760, TotalContribution = 1120, EffectiveYear = 2026 },
            new() { MinSalary = 8250, MaxSalary = 8749.99m, SalaryCredit = 8500, EmployeeShare = 382.50m, EmployerShare = 807.50m, TotalContribution = 1190, EffectiveYear = 2026 },
            new() { MinSalary = 8750, MaxSalary = 9249.99m, SalaryCredit = 9000, EmployeeShare = 405, EmployerShare = 855, TotalContribution = 1260, EffectiveYear = 2026 },
            new() { MinSalary = 9250, MaxSalary = 9749.99m, SalaryCredit = 9500, EmployeeShare = 427.50m, EmployerShare = 902.50m, TotalContribution = 1330, EffectiveYear = 2026 },
            new() { MinSalary = 9750, MaxSalary = 10249.99m, SalaryCredit = 10000, EmployeeShare = 450, EmployerShare = 950, TotalContribution = 1400, EffectiveYear = 2026 },
            new() { MinSalary = 10250, MaxSalary = 10749.99m, SalaryCredit = 10500, EmployeeShare = 472.50m, EmployerShare = 997.50m, TotalContribution = 1470, EffectiveYear = 2026 },
            new() { MinSalary = 10750, MaxSalary = 11249.99m, SalaryCredit = 11000, EmployeeShare = 495, EmployerShare = 1045, TotalContribution = 1540, EffectiveYear = 2026 },
            new() { MinSalary = 11250, MaxSalary = 11749.99m, SalaryCredit = 11500, EmployeeShare = 517.50m, EmployerShare = 1092.50m, TotalContribution = 1610, EffectiveYear = 2026 },
            new() { MinSalary = 11750, MaxSalary = 12249.99m, SalaryCredit = 12000, EmployeeShare = 540, EmployerShare = 1140, TotalContribution = 1680, EffectiveYear = 2026 },
            new() { MinSalary = 12250, MaxSalary = 12749.99m, SalaryCredit = 12500, EmployeeShare = 562.50m, EmployerShare = 1187.50m, TotalContribution = 1750, EffectiveYear = 2026 },
            new() { MinSalary = 12750, MaxSalary = 13249.99m, SalaryCredit = 13000, EmployeeShare = 585, EmployerShare = 1235, TotalContribution = 1820, EffectiveYear = 2026 },
            new() { MinSalary = 13250, MaxSalary = 13749.99m, SalaryCredit = 13500, EmployeeShare = 607.50m, EmployerShare = 1282.50m, TotalContribution = 1890, EffectiveYear = 2026 },
            new() { MinSalary = 13750, MaxSalary = 14249.99m, SalaryCredit = 14000, EmployeeShare = 630, EmployerShare = 1330, TotalContribution = 1960, EffectiveYear = 2026 },
            new() { MinSalary = 14250, MaxSalary = 14749.99m, SalaryCredit = 14500, EmployeeShare = 652.50m, EmployerShare = 1377.50m, TotalContribution = 2030, EffectiveYear = 2026 },
            new() { MinSalary = 14750, MaxSalary = 15249.99m, SalaryCredit = 15000, EmployeeShare = 675, EmployerShare = 1425, TotalContribution = 2100, EffectiveYear = 2026 },
            new() { MinSalary = 15250, MaxSalary = 15749.99m, SalaryCredit = 15500, EmployeeShare = 697.50m, EmployerShare = 1472.50m, TotalContribution = 2170, EffectiveYear = 2026 },
            new() { MinSalary = 15750, MaxSalary = 16249.99m, SalaryCredit = 16000, EmployeeShare = 720, EmployerShare = 1520, TotalContribution = 2240, EffectiveYear = 2026 },
            new() { MinSalary = 16250, MaxSalary = 16749.99m, SalaryCredit = 16500, EmployeeShare = 742.50m, EmployerShare = 1567.50m, TotalContribution = 2310, EffectiveYear = 2026 },
            new() { MinSalary = 16750, MaxSalary = 17249.99m, SalaryCredit = 17000, EmployeeShare = 765, EmployerShare = 1615, TotalContribution = 2380, EffectiveYear = 2026 },
            new() { MinSalary = 17250, MaxSalary = 17749.99m, SalaryCredit = 17500, EmployeeShare = 787.50m, EmployerShare = 1662.50m, TotalContribution = 2450, EffectiveYear = 2026 },
            new() { MinSalary = 17750, MaxSalary = 18249.99m, SalaryCredit = 18000, EmployeeShare = 810, EmployerShare = 1710, TotalContribution = 2520, EffectiveYear = 2026 },
            new() { MinSalary = 18250, MaxSalary = 18749.99m, SalaryCredit = 18500, EmployeeShare = 832.50m, EmployerShare = 1757.50m, TotalContribution = 2590, EffectiveYear = 2026 },
            new() { MinSalary = 18750, MaxSalary = 19249.99m, SalaryCredit = 19000, EmployeeShare = 855, EmployerShare = 1805, TotalContribution = 2660, EffectiveYear = 2026 },
            new() { MinSalary = 19250, MaxSalary = 19749.99m, SalaryCredit = 19500, EmployeeShare = 877.50m, EmployerShare = 1852.50m, TotalContribution = 2730, EffectiveYear = 2026 },
            new() { MinSalary = 19750, MaxSalary = 20249.99m, SalaryCredit = 20000, EmployeeShare = 900, EmployerShare = 1900, TotalContribution = 2800, EffectiveYear = 2026 },
            new() { MinSalary = 20250, MaxSalary = 20749.99m, SalaryCredit = 20500, EmployeeShare = 922.50m, EmployerShare = 1947.50m, TotalContribution = 2870, EffectiveYear = 2026 },
            new() { MinSalary = 20750, MaxSalary = 21249.99m, SalaryCredit = 21000, EmployeeShare = 945, EmployerShare = 1995, TotalContribution = 2940, EffectiveYear = 2026 },
            new() { MinSalary = 21250, MaxSalary = 21749.99m, SalaryCredit = 21500, EmployeeShare = 967.50m, EmployerShare = 2042.50m, TotalContribution = 3010, EffectiveYear = 2026 },
            new() { MinSalary = 21750, MaxSalary = 22249.99m, SalaryCredit = 22000, EmployeeShare = 990, EmployerShare = 2090, TotalContribution = 3080, EffectiveYear = 2026 },
            new() { MinSalary = 22250, MaxSalary = 22749.99m, SalaryCredit = 22500, EmployeeShare = 1012.50m, EmployerShare = 2137.50m, TotalContribution = 3150, EffectiveYear = 2026 },
            new() { MinSalary = 22750, MaxSalary = 23249.99m, SalaryCredit = 23000, EmployeeShare = 1035, EmployerShare = 2185, TotalContribution = 3220, EffectiveYear = 2026 },
            new() { MinSalary = 23250, MaxSalary = 23749.99m, SalaryCredit = 23500, EmployeeShare = 1057.50m, EmployerShare = 2232.50m, TotalContribution = 3290, EffectiveYear = 2026 },
            new() { MinSalary = 23750, MaxSalary = 24249.99m, SalaryCredit = 24000, EmployeeShare = 1080, EmployerShare = 2280, TotalContribution = 3360, EffectiveYear = 2026 },
            new() { MinSalary = 24250, MaxSalary = 24749.99m, SalaryCredit = 24500, EmployeeShare = 1102.50m, EmployerShare = 2327.50m, TotalContribution = 3430, EffectiveYear = 2026 },
            new() { MinSalary = 24750, MaxSalary = 25249.99m, SalaryCredit = 25000, EmployeeShare = 1125, EmployerShare = 2375, TotalContribution = 3500, EffectiveYear = 2026 },
            new() { MinSalary = 25250, MaxSalary = 25749.99m, SalaryCredit = 25500, EmployeeShare = 1147.50m, EmployerShare = 2422.50m, TotalContribution = 3570, EffectiveYear = 2026 },
            new() { MinSalary = 25750, MaxSalary = 26249.99m, SalaryCredit = 26000, EmployeeShare = 1170, EmployerShare = 2470, TotalContribution = 3640, EffectiveYear = 2026 },
            new() { MinSalary = 26250, MaxSalary = 26749.99m, SalaryCredit = 26500, EmployeeShare = 1192.50m, EmployerShare = 2517.50m, TotalContribution = 3710, EffectiveYear = 2026 },
            new() { MinSalary = 26750, MaxSalary = 27249.99m, SalaryCredit = 27000, EmployeeShare = 1215, EmployerShare = 2565, TotalContribution = 3780, EffectiveYear = 2026 },
            new() { MinSalary = 27250, MaxSalary = 27749.99m, SalaryCredit = 27500, EmployeeShare = 1237.50m, EmployerShare = 2612.50m, TotalContribution = 3850, EffectiveYear = 2026 },
            new() { MinSalary = 27750, MaxSalary = 28249.99m, SalaryCredit = 28000, EmployeeShare = 1260, EmployerShare = 2660, TotalContribution = 3920, EffectiveYear = 2026 },
            new() { MinSalary = 28250, MaxSalary = 28749.99m, SalaryCredit = 28500, EmployeeShare = 1282.50m, EmployerShare = 2707.50m, TotalContribution = 3990, EffectiveYear = 2026 },
            new() { MinSalary = 28750, MaxSalary = 29249.99m, SalaryCredit = 29000, EmployeeShare = 1305, EmployerShare = 2755, TotalContribution = 4060, EffectiveYear = 2026 },
            new() { MinSalary = 29250, MaxSalary = 29749.99m, SalaryCredit = 29500, EmployeeShare = 1327.50m, EmployerShare = 2802.50m, TotalContribution = 4130, EffectiveYear = 2026 },
            new() { MinSalary = 29750, MaxSalary = 999999999m, SalaryCredit = 30000, EmployeeShare = 1350, EmployerShare = 2850, TotalContribution = 4200, EffectiveYear = 2026 }
        };

        await _context.SSSContributionRates.AddRangeAsync(sssRates);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} SSS contribution rates", sssRates.Count);
    }

    /// <summary>
    /// 2026 PhilHealth Contribution Table
    /// 5% premium rate split 50-50 between employee and employer
    /// Ceiling: ₱100,000 monthly income
    /// </summary>
    private async Task SeedPhilHealthContributionRatesAsync()
    {
        if (await _context.PhilHealthContributionRates.AnyAsync())
        {
            _logger.LogInformation("PhilHealth contribution rates already seeded, skipping...");
            return;
        }

        var philHealthRates = new List<PhilHealthContributionRate>
        {
            // 2024-2026: 5% premium rate
            new() { MinSalary = 0, MaxSalary = 10000m, PremiumRate = 0.05m, EmployeeShare = 0.025m, EmployerShare = 0.025m, EffectiveYear = 2026 },
            new() { MinSalary = 10000.01m, MaxSalary = 100000m, PremiumRate = 0.05m, EmployeeShare = 0.025m, EmployerShare = 0.025m, EffectiveYear = 2026 },
            // For incomes above ceiling, contribution is capped at ₱5,000 (₱2,500 each)
            new() { MinSalary = 100000.01m, MaxSalary = 999999999m, PremiumRate = 0.05m, EmployeeShare = 0.025m, EmployerShare = 0.025m, EffectiveYear = 2026 }
        };

        await _context.PhilHealthContributionRates.AddRangeAsync(philHealthRates);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} PhilHealth contribution rates", philHealthRates.Count);
    }

    /// <summary>
    /// 2026 Pag-IBIG Contribution Table
    /// Employee: 2% (max ₱200), Employer: 2% (max ₱200)
    /// Based on ₱10,000 ceiling
    /// </summary>
    private async Task SeedPagIBIGContributionRatesAsync()
    {
        if (await _context.PagIBIGContributionRates.AnyAsync())
        {
            _logger.LogInformation("Pag-IBIG contribution rates already seeded, skipping...");
            return;
        }

        var pagIbigRates = new List<PagIBIGContributionRate>
        {
            // Below ₱1,500: Employee 1%, Employer 2%
            new() { MinSalary = 0, MaxSalary = 1500m, EmployeeRate = 0.01m, EmployerRate = 0.02m, EmployeeMaxContribution = 200, EmployerMaxContribution = 200, EffectiveYear = 2026 },
            // ₱1,500 and above: Both 2%, capped at ₱10,000 base (₱200 max each)
            new() { MinSalary = 1500.01m, MaxSalary = 999999999m, EmployeeRate = 0.02m, EmployerRate = 0.02m, EmployeeMaxContribution = 200, EmployerMaxContribution = 200, EffectiveYear = 2026 }
        };

        await _context.PagIBIGContributionRates.AddRangeAsync(pagIbigRates);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} Pag-IBIG contribution rates", pagIbigRates.Count);
    }

    /// <summary>
    /// 2026 BIR Withholding Tax Brackets (Monthly)
    /// Based on TRAIN Law (RA 10963) and CREATE MORE Act adjustments
    /// </summary>
    private async Task SeedWithholdingTaxBracketsAsync()
    {
        if (await _context.WithholdingTaxBrackets.AnyAsync())
        {
            _logger.LogInformation("Withholding tax brackets already seeded, skipping...");
            return;
        }

        var taxBrackets = new List<WithholdingTaxBracket>
        {
            // Monthly tax table (2023-2026 under TRAIN Law)
            new() { MinIncome = 0, MaxIncome = 20833, BaseTax = 0, TaxRate = 0, ExcessOver = 0, EffectiveYear = 2026 },
            new() { MinIncome = 20833.01m, MaxIncome = 33332, BaseTax = 0, TaxRate = 0.15m, ExcessOver = 20833, EffectiveYear = 2026 },
            new() { MinIncome = 33332.01m, MaxIncome = 66666, BaseTax = 1875, TaxRate = 0.20m, ExcessOver = 33333, EffectiveYear = 2026 },
            new() { MinIncome = 66666.01m, MaxIncome = 166666, BaseTax = 8541.67m, TaxRate = 0.25m, ExcessOver = 66667, EffectiveYear = 2026 },
            new() { MinIncome = 166666.01m, MaxIncome = 666666, BaseTax = 33541.67m, TaxRate = 0.30m, ExcessOver = 166667, EffectiveYear = 2026 },
            new() { MinIncome = 666666.01m, MaxIncome = null, BaseTax = 183541.67m, TaxRate = 0.35m, ExcessOver = 666667, EffectiveYear = 2026 }
        };

        await _context.WithholdingTaxBrackets.AddRangeAsync(taxBrackets);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} withholding tax brackets", taxBrackets.Count);
    }

    /// <summary>
    /// 2026 Philippine Holidays (Official)
    /// Regular Holidays: 200% pay
    /// Special Non-Working Days: 130% pay (if worked)
    /// </summary>
    private async Task SeedPhilippineHolidaysAsync()
    {
        if (await _context.PhilippineHolidays.AnyAsync(h => h.Year == 2026))
        {
            _logger.LogInformation("Philippine holidays for 2026 already seeded, skipping...");
            return;
        }

        var holidays = new List<PhilippineHoliday>
        {
            // Regular Holidays 2026
            new() { HolidayName = "New Year's Day", HolidayDate = new DateTime(2026, 1, 1), HolidayType = "Regular", Year = 2026, IsRecurring = true },
            new() { HolidayName = "Maundy Thursday", HolidayDate = new DateTime(2026, 4, 2), HolidayType = "Regular", Year = 2026, IsRecurring = false },
            new() { HolidayName = "Good Friday", HolidayDate = new DateTime(2026, 4, 3), HolidayType = "Regular", Year = 2026, IsRecurring = false },
            new() { HolidayName = "Araw ng Kagitingan", HolidayDate = new DateTime(2026, 4, 9), HolidayType = "Regular", Year = 2026, IsRecurring = true },
            new() { HolidayName = "Labor Day", HolidayDate = new DateTime(2026, 5, 1), HolidayType = "Regular", Year = 2026, IsRecurring = true },
            new() { HolidayName = "Independence Day", HolidayDate = new DateTime(2026, 6, 12), HolidayType = "Regular", Year = 2026, IsRecurring = true },
            new() { HolidayName = "National Heroes Day", HolidayDate = new DateTime(2026, 8, 31), HolidayType = "Regular", Year = 2026, IsRecurring = false },
            new() { HolidayName = "Bonifacio Day", HolidayDate = new DateTime(2026, 11, 30), HolidayType = "Regular", Year = 2026, IsRecurring = true },
            new() { HolidayName = "Christmas Day", HolidayDate = new DateTime(2026, 12, 25), HolidayType = "Regular", Year = 2026, IsRecurring = true },
            new() { HolidayName = "Rizal Day", HolidayDate = new DateTime(2026, 12, 30), HolidayType = "Regular", Year = 2026, IsRecurring = true },
            
            // Special Non-Working Days 2026
            new() { HolidayName = "Chinese New Year", HolidayDate = new DateTime(2026, 2, 17), HolidayType = "Special Non-Working", Year = 2026, IsRecurring = false },
            new() { HolidayName = "EDSA People Power Revolution Anniversary", HolidayDate = new DateTime(2026, 2, 25), HolidayType = "Special Non-Working", Year = 2026, IsRecurring = true },
            new() { HolidayName = "Black Saturday", HolidayDate = new DateTime(2026, 4, 4), HolidayType = "Special Non-Working", Year = 2026, IsRecurring = false },
            new() { HolidayName = "Ninoy Aquino Day", HolidayDate = new DateTime(2026, 8, 21), HolidayType = "Special Non-Working", Year = 2026, IsRecurring = true },
            new() { HolidayName = "All Saints' Day", HolidayDate = new DateTime(2026, 11, 1), HolidayType = "Special Non-Working", Year = 2026, IsRecurring = true },
            new() { HolidayName = "All Souls' Day", HolidayDate = new DateTime(2026, 11, 2), HolidayType = "Special Non-Working", Year = 2026, IsRecurring = true },
            new() { HolidayName = "Immaculate Conception", HolidayDate = new DateTime(2026, 12, 8), HolidayType = "Special Non-Working", Year = 2026, IsRecurring = true },
            new() { HolidayName = "Christmas Eve", HolidayDate = new DateTime(2026, 12, 24), HolidayType = "Special Non-Working", Year = 2026, IsRecurring = true },
            new() { HolidayName = "Last Day of the Year", HolidayDate = new DateTime(2026, 12, 31), HolidayType = "Special Non-Working", Year = 2026, IsRecurring = true },
            
            // Eid'l Fitr and Eid'l Adha (dates are approximate, subject to moon sighting)
            new() { HolidayName = "Eid'l Fitr", HolidayDate = new DateTime(2026, 3, 20), HolidayType = "Regular", Year = 2026, IsRecurring = false },
            new() { HolidayName = "Eid'l Adha", HolidayDate = new DateTime(2026, 5, 27), HolidayType = "Regular", Year = 2026, IsRecurring = false }
        };

        await _context.PhilippineHolidays.AddRangeAsync(holidays);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} Philippine holidays for 2026", holidays.Count);
    }

    #endregion

    #region Sample Business Data Seeding

    private async Task SeedServiceCategoriesAndServicesAsync()
    {
        if (await _context.ServiceCategories.AnyAsync())
        {
            _logger.LogInformation("Service categories already seeded, skipping...");
            return;
        }

        // Service Categories
        var categories = new List<ServiceCategory>
        {
            new() { CategoryName = "Massage Therapy", Description = "Full body and targeted massage services", DisplayOrder = 1, IsActive = true },
            new() { CategoryName = "Facial Treatments", Description = "Skin care and facial rejuvenation services", DisplayOrder = 2, IsActive = true },
            new() { CategoryName = "Body Treatments", Description = "Body scrubs, wraps, and detox treatments", DisplayOrder = 3, IsActive = true },
            new() { CategoryName = "Nail Services", Description = "Manicure, pedicure, and nail art", DisplayOrder = 4, IsActive = true },
            new() { CategoryName = "Hair Services", Description = "Hair styling, treatment, and coloring", DisplayOrder = 5, IsActive = true },
            new() { CategoryName = "Spa Packages", Description = "Combination treatments and special packages", DisplayOrder = 6, IsActive = true },
            new() { CategoryName = "Add-On Services", Description = "Extra services to complement main treatments", DisplayOrder = 7, IsActive = true }
        };

        await _context.ServiceCategories.AddRangeAsync(categories);
        await _context.SaveChangesAsync();

        // Get category IDs
        var massageCategory = await _context.ServiceCategories.FirstAsync(c => c.CategoryName == "Massage Therapy");
        var facialCategory = await _context.ServiceCategories.FirstAsync(c => c.CategoryName == "Facial Treatments");
        var bodyCategory = await _context.ServiceCategories.FirstAsync(c => c.CategoryName == "Body Treatments");
        var nailCategory = await _context.ServiceCategories.FirstAsync(c => c.CategoryName == "Nail Services");
        var hairCategory = await _context.ServiceCategories.FirstAsync(c => c.CategoryName == "Hair Services");
        var packageCategory = await _context.ServiceCategories.FirstAsync(c => c.CategoryName == "Spa Packages");
        var addonCategory = await _context.ServiceCategories.FirstAsync(c => c.CategoryName == "Add-On Services");

        // Services
        var services = new List<Service>
        {
            // Massage Therapy
            new() { CategoryId = massageCategory.CategoryId, ServiceCode = "MSG-001", ServiceName = "Swedish Massage", Description = "Classic relaxation massage with long flowing strokes", DurationMinutes = 60, RegularPrice = 800, MemberPrice = 720, TherapistCommissionRate = 0.40m, IsActive = true },
            new() { CategoryId = massageCategory.CategoryId, ServiceCode = "MSG-002", ServiceName = "Deep Tissue Massage", Description = "Firm pressure massage targeting deep muscle layers", DurationMinutes = 60, RegularPrice = 1000, MemberPrice = 900, TherapistCommissionRate = 0.40m, IsActive = true },
            new() { CategoryId = massageCategory.CategoryId, ServiceCode = "MSG-003", ServiceName = "Hot Stone Massage", Description = "Heated basalt stones combined with massage therapy", DurationMinutes = 90, RegularPrice = 1500, MemberPrice = 1350, TherapistCommissionRate = 0.40m, IsActive = true },
            new() { CategoryId = massageCategory.CategoryId, ServiceCode = "MSG-004", ServiceName = "Aromatherapy Massage", Description = "Essential oil massage for mind and body relaxation", DurationMinutes = 60, RegularPrice = 900, MemberPrice = 810, TherapistCommissionRate = 0.40m, IsActive = true },
            new() { CategoryId = massageCategory.CategoryId, ServiceCode = "MSG-005", ServiceName = "Foot Reflexology", Description = "Pressure point therapy on feet for whole body wellness", DurationMinutes = 45, RegularPrice = 600, MemberPrice = 540, TherapistCommissionRate = 0.40m, IsActive = true },
            new() { CategoryId = massageCategory.CategoryId, ServiceCode = "MSG-006", ServiceName = "Thai Massage", Description = "Traditional Thai stretching and pressure massage", DurationMinutes = 90, RegularPrice = 1200, MemberPrice = 1080, TherapistCommissionRate = 0.40m, IsActive = true },
            new() { CategoryId = massageCategory.CategoryId, ServiceCode = "MSG-007", ServiceName = "Prenatal Massage", Description = "Gentle massage designed for expecting mothers", DurationMinutes = 60, RegularPrice = 1000, MemberPrice = 900, TherapistCommissionRate = 0.40m, IsActive = true },
            new() { CategoryId = massageCategory.CategoryId, ServiceCode = "MSG-008", ServiceName = "Sports Massage", Description = "Athletic recovery and performance massage", DurationMinutes = 60, RegularPrice = 1100, MemberPrice = 990, TherapistCommissionRate = 0.40m, IsActive = true },

            // Facial Treatments
            new() { CategoryId = facialCategory.CategoryId, ServiceCode = "FCL-001", ServiceName = "Classic Facial", Description = "Deep cleansing and hydrating facial treatment", DurationMinutes = 60, RegularPrice = 1200, MemberPrice = 1080, TherapistCommissionRate = 0.35m, IsActive = true },
            new() { CategoryId = facialCategory.CategoryId, ServiceCode = "FCL-002", ServiceName = "Anti-Aging Facial", Description = "Collagen-boosting treatment to reduce fine lines", DurationMinutes = 75, RegularPrice = 1800, MemberPrice = 1620, TherapistCommissionRate = 0.35m, IsActive = true },
            new() { CategoryId = facialCategory.CategoryId, ServiceCode = "FCL-003", ServiceName = "Acne Treatment Facial", Description = "Deep pore cleansing with antibacterial treatment", DurationMinutes = 60, RegularPrice = 1500, MemberPrice = 1350, TherapistCommissionRate = 0.35m, IsActive = true },
            new() { CategoryId = facialCategory.CategoryId, ServiceCode = "FCL-004", ServiceName = "Brightening Facial", Description = "Vitamin C treatment for radiant, even skin tone", DurationMinutes = 60, RegularPrice = 1600, MemberPrice = 1440, TherapistCommissionRate = 0.35m, IsActive = true },
            new() { CategoryId = facialCategory.CategoryId, ServiceCode = "FCL-005", ServiceName = "Hydrating Facial", Description = "Intensive moisture treatment for dry skin", DurationMinutes = 60, RegularPrice = 1400, MemberPrice = 1260, TherapistCommissionRate = 0.35m, IsActive = true },

            // Body Treatments
            new() { CategoryId = bodyCategory.CategoryId, ServiceCode = "BDY-001", ServiceName = "Body Scrub", Description = "Exfoliating treatment for smooth, glowing skin", DurationMinutes = 45, RegularPrice = 900, MemberPrice = 810, TherapistCommissionRate = 0.35m, IsActive = true },
            new() { CategoryId = bodyCategory.CategoryId, ServiceCode = "BDY-002", ServiceName = "Body Wrap", Description = "Detoxifying and slimming body wrap treatment", DurationMinutes = 60, RegularPrice = 1200, MemberPrice = 1080, TherapistCommissionRate = 0.35m, IsActive = true },
            new() { CategoryId = bodyCategory.CategoryId, ServiceCode = "BDY-003", ServiceName = "Back Facial", Description = "Deep cleansing treatment for the back area", DurationMinutes = 45, RegularPrice = 1000, MemberPrice = 900, TherapistCommissionRate = 0.35m, IsActive = true },

            // Nail Services
            new() { CategoryId = nailCategory.CategoryId, ServiceCode = "NAL-001", ServiceName = "Classic Manicure", Description = "Nail shaping, cuticle care, and polish", DurationMinutes = 30, RegularPrice = 350, MemberPrice = 315, TherapistCommissionRate = 0.30m, IsActive = true },
            new() { CategoryId = nailCategory.CategoryId, ServiceCode = "NAL-002", ServiceName = "Classic Pedicure", Description = "Foot care with nail shaping and polish", DurationMinutes = 45, RegularPrice = 450, MemberPrice = 405, TherapistCommissionRate = 0.30m, IsActive = true },
            new() { CategoryId = nailCategory.CategoryId, ServiceCode = "NAL-003", ServiceName = "Gel Manicure", Description = "Long-lasting gel polish application", DurationMinutes = 45, RegularPrice = 550, MemberPrice = 495, TherapistCommissionRate = 0.30m, IsActive = true },
            new() { CategoryId = nailCategory.CategoryId, ServiceCode = "NAL-004", ServiceName = "Gel Pedicure", Description = "Foot care with gel polish", DurationMinutes = 60, RegularPrice = 650, MemberPrice = 585, TherapistCommissionRate = 0.30m, IsActive = true },
            new() { CategoryId = nailCategory.CategoryId, ServiceCode = "NAL-005", ServiceName = "Nail Art (per nail)", Description = "Custom nail design and art", DurationMinutes = 10, RegularPrice = 50, MemberPrice = 45, TherapistCommissionRate = 0.30m, IsActive = true },

            // Hair Services
            new() { CategoryId = hairCategory.CategoryId, ServiceCode = "HAR-001", ServiceName = "Hair Treatment", Description = "Deep conditioning and repair treatment", DurationMinutes = 45, RegularPrice = 800, MemberPrice = 720, TherapistCommissionRate = 0.30m, IsActive = true },
            new() { CategoryId = hairCategory.CategoryId, ServiceCode = "HAR-002", ServiceName = "Scalp Massage", Description = "Relaxing scalp treatment with essential oils", DurationMinutes = 30, RegularPrice = 400, MemberPrice = 360, TherapistCommissionRate = 0.35m, IsActive = true },

            // Spa Packages
            new() { CategoryId = packageCategory.CategoryId, ServiceCode = "PKG-001", ServiceName = "MiddayMist Signature", Description = "Full body massage + facial + body scrub", DurationMinutes = 180, RegularPrice = 2800, MemberPrice = 2500, TherapistCommissionRate = 0.40m, IsActive = true },
            new() { CategoryId = packageCategory.CategoryId, ServiceCode = "PKG-002", ServiceName = "Couple's Retreat", Description = "Side-by-side massage experience for two", DurationMinutes = 90, RegularPrice = 2400, MemberPrice = 2160, TherapistCommissionRate = 0.40m, IsActive = true },
            new() { CategoryId = packageCategory.CategoryId, ServiceCode = "PKG-003", ServiceName = "Executive Stress Relief", Description = "Deep tissue massage + hot stone therapy", DurationMinutes = 120, RegularPrice = 2200, MemberPrice = 1980, TherapistCommissionRate = 0.40m, IsActive = true },
            new() { CategoryId = packageCategory.CategoryId, ServiceCode = "PKG-004", ServiceName = "Head to Toe Pampering", Description = "Manicure + pedicure + facial + massage", DurationMinutes = 180, RegularPrice = 3000, MemberPrice = 2700, TherapistCommissionRate = 0.40m, IsActive = true },

            // Add-On Services
            new() { CategoryId = addonCategory.CategoryId, ServiceCode = "ADD-001", ServiceName = "Extended Time (+15 min)", Description = "Add 15 minutes to any massage service", DurationMinutes = 15, RegularPrice = 200, MemberPrice = 180, TherapistCommissionRate = 0.40m, IsActive = true },
            new() { CategoryId = addonCategory.CategoryId, ServiceCode = "ADD-002", ServiceName = "Hot Towel Treatment", Description = "Warm towel application during service", DurationMinutes = 10, RegularPrice = 100, MemberPrice = 90, TherapistCommissionRate = 0.35m, IsActive = true },
            new() { CategoryId = addonCategory.CategoryId, ServiceCode = "ADD-003", ServiceName = "Essential Oil Upgrade", Description = "Premium aromatherapy oil selection", DurationMinutes = 0, RegularPrice = 150, MemberPrice = 135, TherapistCommissionRate = 0.35m, IsActive = true },
            new() { CategoryId = addonCategory.CategoryId, ServiceCode = "ADD-004", ServiceName = "Eye Mask Treatment", Description = "Cooling eye treatment for relaxation", DurationMinutes = 10, RegularPrice = 120, MemberPrice = 108, TherapistCommissionRate = 0.35m, IsActive = true }
        };

        await _context.Services.AddRangeAsync(services);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {CategoryCount} service categories and {ServiceCount} services", categories.Count, services.Count);
    }

    private async Task SeedProductCategoriesAndProductsAsync()
    {
        if (await _context.ProductCategories.AnyAsync())
        {
            _logger.LogInformation("Product categories already seeded, skipping...");
            return;
        }

        // Product Categories
        var categories = new List<ProductCategory>
        {
            new() { CategoryName = "Retail Products", Description = "Products available for customer purchase", IsActive = true },
            new() { CategoryName = "Massage Supplies", Description = "Oils, lotions, and massage accessories", IsActive = true },
            new() { CategoryName = "Facial Supplies", Description = "Skincare products for facial treatments", IsActive = true },
            new() { CategoryName = "Nail Supplies", Description = "Nail polish, tools, and accessories", IsActive = true },
            new() { CategoryName = "Consumables", Description = "Disposable items and daily supplies", IsActive = true },
            new() { CategoryName = "Equipment", Description = "Spa equipment and furniture", IsActive = true }
        };

        await _context.ProductCategories.AddRangeAsync(categories);
        await _context.SaveChangesAsync();

        // Get category IDs
        var retailCategory = await _context.ProductCategories.FirstAsync(c => c.CategoryName == "Retail Products");
        var massageCategory = await _context.ProductCategories.FirstAsync(c => c.CategoryName == "Massage Supplies");
        var facialCategory = await _context.ProductCategories.FirstAsync(c => c.CategoryName == "Facial Supplies");
        var nailCategory = await _context.ProductCategories.FirstAsync(c => c.CategoryName == "Nail Supplies");
        var consumablesCategory = await _context.ProductCategories.FirstAsync(c => c.CategoryName == "Consumables");

        // Products
        var products = new List<Product>
        {
            // Retail Products
            new() { ProductCategoryId = retailCategory.ProductCategoryId, ProductCode = "RTL-001", ProductName = "Lavender Essential Oil", Description = "Pure lavender essential oil 30ml", ProductType = "Retail", CurrentStock = 50, ReorderLevel = 10, UnitOfMeasure = "bottle", CostPrice = 350, SellingPrice = 650, RetailCommissionRate = 0.10m, IsActive = true },
            new() { ProductCategoryId = retailCategory.ProductCategoryId, ProductCode = "RTL-002", ProductName = "Eucalyptus Oil", Description = "Therapeutic eucalyptus oil 30ml", ProductType = "Retail", CurrentStock = 45, ReorderLevel = 10, UnitOfMeasure = "bottle", CostPrice = 300, SellingPrice = 550, RetailCommissionRate = 0.10m, IsActive = true },
            new() { ProductCategoryId = retailCategory.ProductCategoryId, ProductCode = "RTL-003", ProductName = "Facial Moisturizer", Description = "Hydrating daily moisturizer 50ml", ProductType = "Retail", CurrentStock = 30, ReorderLevel = 8, UnitOfMeasure = "tube", CostPrice = 450, SellingPrice = 850, RetailCommissionRate = 0.10m, IsActive = true },
            new() { ProductCategoryId = retailCategory.ProductCategoryId, ProductCode = "RTL-004", ProductName = "Body Lotion", Description = "Luxurious body lotion 250ml", ProductType = "Retail", CurrentStock = 40, ReorderLevel = 10, UnitOfMeasure = "bottle", CostPrice = 280, SellingPrice = 520, RetailCommissionRate = 0.10m, IsActive = true },
            new() { ProductCategoryId = retailCategory.ProductCategoryId, ProductCode = "RTL-005", ProductName = "Scented Candle", Description = "Relaxation aromatherapy candle", ProductType = "Retail", CurrentStock = 25, ReorderLevel = 5, UnitOfMeasure = "piece", CostPrice = 200, SellingPrice = 400, RetailCommissionRate = 0.10m, IsActive = true },

            // Massage Supplies
            new() { ProductCategoryId = massageCategory.ProductCategoryId, ProductCode = "MSG-001", ProductName = "Massage Oil (Bulk)", Description = "Professional massage oil 1L", ProductType = "Supply", CurrentStock = 20, ReorderLevel = 5, UnitOfMeasure = "liter", CostPrice = 400, SellingPrice = null, RetailCommissionRate = 0, IsActive = true },
            new() { ProductCategoryId = massageCategory.ProductCategoryId, ProductCode = "MSG-002", ProductName = "Hot Stone Set", Description = "Basalt stones for hot stone massage", ProductType = "Supply", CurrentStock = 5, ReorderLevel = 2, UnitOfMeasure = "set", CostPrice = 2500, SellingPrice = null, RetailCommissionRate = 0, IsActive = true },
            new() { ProductCategoryId = massageCategory.ProductCategoryId, ProductCode = "MSG-003", ProductName = "Massage Cream", Description = "Professional massage cream 500g", ProductType = "Supply", CurrentStock = 15, ReorderLevel = 5, UnitOfMeasure = "jar", CostPrice = 350, SellingPrice = null, RetailCommissionRate = 0, IsActive = true },

            // Facial Supplies
            new() { ProductCategoryId = facialCategory.ProductCategoryId, ProductCode = "FCL-001", ProductName = "Facial Cleanser (Pro)", Description = "Professional facial cleanser 250ml", ProductType = "Supply", CurrentStock = 12, ReorderLevel = 4, UnitOfMeasure = "bottle", CostPrice = 500, SellingPrice = null, RetailCommissionRate = 0, IsActive = true },
            new() { ProductCategoryId = facialCategory.ProductCategoryId, ProductCode = "FCL-002", ProductName = "Clay Mask", Description = "Deep cleansing clay mask 200g", ProductType = "Supply", CurrentStock = 10, ReorderLevel = 3, UnitOfMeasure = "jar", CostPrice = 600, SellingPrice = null, RetailCommissionRate = 0, IsActive = true },
            new() { ProductCategoryId = facialCategory.ProductCategoryId, ProductCode = "FCL-003", ProductName = "Serum - Vitamin C", Description = "Professional vitamin C serum 100ml", ProductType = "Supply", CurrentStock = 8, ReorderLevel = 3, UnitOfMeasure = "bottle", CostPrice = 800, SellingPrice = null, RetailCommissionRate = 0, IsActive = true },
            new() { ProductCategoryId = facialCategory.ProductCategoryId, ProductCode = "FCL-004", ProductName = "Collagen Sheet Mask", Description = "Single-use collagen mask (10 pcs)", ProductType = "Supply", CurrentStock = 50, ReorderLevel = 20, UnitOfMeasure = "piece", CostPrice = 80, SellingPrice = null, RetailCommissionRate = 0, IsActive = true },

            // Nail Supplies
            new() { ProductCategoryId = nailCategory.ProductCategoryId, ProductCode = "NAL-001", ProductName = "Gel Polish Set", Description = "Assorted gel polish colors (12 pcs)", ProductType = "Supply", CurrentStock = 6, ReorderLevel = 2, UnitOfMeasure = "set", CostPrice = 1800, SellingPrice = null, RetailCommissionRate = 0, IsActive = true },
            new() { ProductCategoryId = nailCategory.ProductCategoryId, ProductCode = "NAL-002", ProductName = "Cuticle Oil", Description = "Nourishing cuticle oil 30ml", ProductType = "Supply", CurrentStock = 20, ReorderLevel = 5, UnitOfMeasure = "bottle", CostPrice = 150, SellingPrice = null, RetailCommissionRate = 0, IsActive = true },
            new() { ProductCategoryId = nailCategory.ProductCategoryId, ProductCode = "NAL-003", ProductName = "Nail File Set", Description = "Professional nail files (100 pcs)", ProductType = "Consumable", CurrentStock = 200, ReorderLevel = 50, UnitOfMeasure = "piece", CostPrice = 10, SellingPrice = null, RetailCommissionRate = 0, IsActive = true },

            // Consumables
            new() { ProductCategoryId = consumablesCategory.ProductCategoryId, ProductCode = "CON-001", ProductName = "Disposable Face Rest Covers", Description = "Hygienic face rest covers (500 pcs)", ProductType = "Consumable", CurrentStock = 1000, ReorderLevel = 200, UnitOfMeasure = "piece", CostPrice = 5, SellingPrice = null, RetailCommissionRate = 0, IsActive = true },
            new() { ProductCategoryId = consumablesCategory.ProductCategoryId, ProductCode = "CON-002", ProductName = "Cotton Pads", Description = "Facial cotton pads (500 pcs)", ProductType = "Consumable", CurrentStock = 800, ReorderLevel = 200, UnitOfMeasure = "piece", CostPrice = 2, SellingPrice = null, RetailCommissionRate = 0, IsActive = true },
            new() { ProductCategoryId = consumablesCategory.ProductCategoryId, ProductCode = "CON-003", ProductName = "Disposable Slippers", Description = "Guest slippers (50 pairs)", ProductType = "Consumable", CurrentStock = 100, ReorderLevel = 30, UnitOfMeasure = "pair", CostPrice = 25, SellingPrice = null, RetailCommissionRate = 0, IsActive = true },
            new() { ProductCategoryId = consumablesCategory.ProductCategoryId, ProductCode = "CON-004", ProductName = "Hand Towels", Description = "Clean hand towels (50 pcs)", ProductType = "Supply", CurrentStock = 150, ReorderLevel = 50, UnitOfMeasure = "piece", CostPrice = 50, SellingPrice = null, RetailCommissionRate = 0, IsActive = true },
            new() { ProductCategoryId = consumablesCategory.ProductCategoryId, ProductCode = "CON-005", ProductName = "Bed Sheets (Disposable)", Description = "Disposable bed sheets (100 pcs)", ProductType = "Consumable", CurrentStock = 300, ReorderLevel = 100, UnitOfMeasure = "piece", CostPrice = 15, SellingPrice = null, RetailCommissionRate = 0, IsActive = true },

            // LOW-STOCK product for testing notifications (CurrentStock 2 <= ReorderLevel 10 → IsLowStock = true)
            new() { ProductCategoryId = retailCategory.ProductCategoryId, ProductCode = "RTL-006", ProductName = "Hyaluronic Acid Serum", Description = "Premium hydrating serum 30ml - CRITICALLY LOW STOCK", ProductType = "Retail", CurrentStock = 2, ReorderLevel = 10, UnitOfMeasure = "bottle", CostPrice = 650, SellingPrice = 1200, RetailCommissionRate = 0.10m, IsActive = true }
        };

        await _context.Products.AddRangeAsync(products);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {CategoryCount} product categories and {ProductCount} products (includes 1 low-stock item)", categories.Count, products.Count);
    }

    private async Task SeedSampleCustomersAsync()
    {
        if (await _context.Customers.AnyAsync())
        {
            _logger.LogInformation("Customers already seeded, skipping...");
            return;
        }

        var now = DateTime.UtcNow;

        // 100 customers designed for DBSCAN RFM segmentation:
        // VIP Platinum (~8): Recent <30d, Freq >2/mo, Monetary >₱10K
        // Loyal Regulars (~20): Recent <90d, Freq 0.5-2/mo, Monetary ₱3K-10K+
        // Promising (~12): Recent <30d, Medium value ₱3K-10K
        // New Customers (~20): Recent <30d, Low freq <0.5/mo, Monetary <₱3K
        // At-Risk (~15): Recency 30-90d, Freq 0.5-2/mo
        // Hibernating (~13): Recency >90d, Freq ≥0.5/mo
        // Lost (~12): Recency >90d, Freq <0.5/mo, Monetary <₱3K

        var firstNames = new[] { "Maria", "Juan", "Anna", "Carlos", "Isabel", "Michael", "Sofia", "David", "Elena", "Roberto",
            "Carmen", "Jose", "Patricia", "Antonio", "Angela", "Rafael", "Luz", "Fernando", "Rosa", "Miguel",
            "Cristina", "Paolo", "Teresa", "Marco", "Diana", "Gabriel", "Victoria", "Andres", "Jasmine", "Ricardo",
            "Donna", "Enrique", "Beatriz", "Ernesto", "Katrina", "Manuel", "Nina", "Ramon", "Gloria", "Francisco",
            "Lorna", "Oscar", "Maricel", "Alejandro", "Precious", "Vincent", "Joanna", "Benedict", "Sheila", "Leonardo",
            "Rowena", "Danilo", "Cynthia", "Ariel", "Mylene", "Joel", "Gemma", "Noel", "Aileen", "Rodrigo",
            "Cherry", "Jessie", "Alma", "Dennis", "Grace", "Allan", "Jane", "Ruel", "Marissa", "Jeffrey",
            "Nerissa", "Bryan", "Imelda", "Gerald", "Honey", "Ronaldo", "Liza", "Jomar", "Anita", "Edgar",
            "Shiela", "Jayson", "Melissa", "Ricky", "Camille", "Emilio", "Yvonne", "Randy", "Lorena", "Dante",
            "Faith", "Philip", "Hazel", "Dominic", "Abigail", "Cesar", "Mae", "Raul", "Irene", "Alfredo" };

        var lastNames = new[] { "Santos", "Dela Cruz", "Reyes", "Garcia", "Lim", "Tan", "Cruz", "Mendoza", "Villanueva", "Fernandez",
            "Gonzales", "Ramos", "Aquino", "Pascual", "Torres", "Bautista", "Rivera", "Lopez", "Esperanza", "Castillo",
            "Navarro", "David", "Salazar", "Rosales", "Hernandez", "Aguilar", "Mercado", "Morales", "Dizon", "Manalo",
            "Flores", "De Leon", "Santiago", "Ocampo", "Del Rosario", "Perez", "Magno", "Dimaculangan", "Pangilinan", "Concepcion",
            "Soriano", "Valdez", "Ignacio", "Lacson", "Miranda", "Tolentino", "Yap", "Enriquez", "Corpuz", "Abad",
            "Mallari", "Cunanan", "Chua", "Galang", "Pineda", "Natividad", "Guevara", "Sicat", "Bondoc", "Pangasinan",
            "Macapagal", "Lugtu", "Alfonso", "Layug", "Quizon", "Fajardo", "Magsaysay", "Zarate", "Villar", "Legaspi",
            "Ponce", "Cabrera", "Padilla", "Tamayo", "Librado", "Sison", "Velasco", "Magat", "Ragasa", "Tugade",
            "Cayetano", "Romualdez", "Lumaban", "Panganiban", "Dalisay", "Zamora", "Ledesma", "Villarama", "Estrella", "Buenaventura",
            "Monsalud", "Palma", "Cordero", "Barrera", "Arevalo", "Magbanua", "Luna", "Macaraig", "Trinidad", "Almonte" };

        var cities = new[] { "Makati City", "Taguig City", "Pasig City", "Quezon City", "Manila", "Mandaluyong City",
            "San Juan City", "Marikina City", "Muntinlupa City", "Las Piñas City", "Parañaque City", "Caloocan City" };

        var addresses = new[] { "123 Ayala Ave", "456 BGC High St", "789 Ortigas Center", "321 Eastwood City", "654 Ermita Blvd",
            "987 Shaw Blvd", "147 EDSA", "258 Katipunan Ave", "369 Alabang-Zapote Rd", "741 Roxas Blvd",
            "852 Pasong Tamo", "963 Congressional Ave" };

        var genders = new[] { "Female", "Male" };
        var referralSources = new[] { "Walk-in", "Facebook", "Google", "Referral", "Instagram", null };
        var pressures = new[] { "Light", "Medium", "Firm", null };
        var temperatures = new[] { "Cool", "Warm", "Hot", null };
        var musicPrefs = new[] { "Classical", "Spa Music", "Nature Sounds", "Silence", null };
        var commChannels = new[] { "Email", "SMS", "Both", "None" };

        // Deterministic seed for reproducibility
        var rng = new Random(2026);

        var customers = new List<Customer>();
        int idx = 0;

        // ── VIP Platinum (8): Recent <30d, >2 visits/mo, >₱10K ──
        var vipData = new (int visits, decimal spent, int daysAgo, string membership)[]
        {
            (60, 200000m, 1, "Platinum"), (55, 180000m, 2, "Platinum"),
            (48, 150000m, 3, "Platinum"), (42, 125000m, 1, "Platinum"),
            (38, 110000m, 5, "Platinum"), (35, 95000m, 2, "Gold"),
            (32, 85000m, 7, "Platinum"), (30, 80000m, 4, "Gold")
        };
        foreach (var v in vipData)
        {
            var i = idx++;
            var firstVisit = now.AddMonths(-rng.Next(12, 36));
            customers.Add(new Customer
            {
                CustomerCode = $"CUST-{i + 1:D3}",
                FirstName = firstNames[i],
                LastName = lastNames[i],
                Email = $"{firstNames[i].ToLower()}.{lastNames[i].ToLower().Replace(" ", "")}@email.com",
                PhoneNumber = $"+63 9{rng.Next(17, 29):D2} {rng.Next(100, 999)} {rng.Next(1000, 9999)}",
                Gender = genders[i % 2],
                DateOfBirth = new DateTime(rng.Next(1970, 1995), rng.Next(1, 13), rng.Next(1, 28)),
                Address = addresses[i % addresses.Length],
                City = cities[i % cities.Length],
                MembershipType = v.membership,
                LoyaltyPoints = v.visits * 50,
                TotalVisits = v.visits,
                TotalSpent = v.spent,
                FirstVisitDate = firstVisit,
                LastVisitDate = now.AddDays(-v.daysAgo),
                PressurePreference = pressures[rng.Next(3)],
                TemperaturePreference = temperatures[rng.Next(3)],
                MusicPreference = musicPrefs[rng.Next(4)],
                ReferralSource = "Referral",
                PreferredCommunicationChannel = "Both",
                MarketingConsent = true,
                SmsConsent = true,
                IsActive = true,
                CreatedAt = firstVisit,
                UpdatedAt = now
            });
        }

        // ── Loyal Regulars (20): Recent <90d, 0.5-2 visits/mo, ₱3K-₱80K ──
        var loyalData = new (int visits, decimal spent, int daysAgo, string membership)[]
        {
            (25, 65000m, 5, "Gold"), (22, 55000m, 8, "Gold"), (20, 48000m, 10, "Gold"),
            (18, 42000m, 7, "Gold"), (16, 38000m, 12, "Silver"), (15, 35000m, 15, "Silver"),
            (14, 32000m, 18, "Silver"), (13, 28000m, 20, "Silver"), (12, 25000m, 14, "Silver"),
            (11, 22000m, 22, "Silver"), (10, 20000m, 25, "Silver"), (10, 18000m, 28, "Silver"),
            (9, 16000m, 30, "Regular"), (8, 14000m, 35, "Regular"), (8, 12000m, 40, "Regular"),
            (7, 10000m, 45, "Regular"), (7, 8000m, 50, "Regular"), (6, 7500m, 55, "Regular"),
            (6, 6500m, 60, "Regular"), (5, 5000m, 70, "Regular")
        };
        foreach (var v in loyalData)
        {
            var i = idx++;
            var firstVisit = now.AddMonths(-rng.Next(6, 24));
            customers.Add(new Customer
            {
                CustomerCode = $"CUST-{i + 1:D3}",
                FirstName = firstNames[i],
                LastName = lastNames[i],
                Email = $"{firstNames[i].ToLower()}.{lastNames[i].ToLower().Replace(" ", "")}@email.com",
                PhoneNumber = $"+63 9{rng.Next(17, 29):D2} {rng.Next(100, 999)} {rng.Next(1000, 9999)}",
                Gender = genders[i % 2],
                DateOfBirth = new DateTime(rng.Next(1975, 2000), rng.Next(1, 13), rng.Next(1, 28)),
                Address = addresses[i % addresses.Length],
                City = cities[i % cities.Length],
                MembershipType = v.membership,
                LoyaltyPoints = v.visits * 30,
                TotalVisits = v.visits,
                TotalSpent = v.spent,
                FirstVisitDate = firstVisit,
                LastVisitDate = now.AddDays(-v.daysAgo),
                PressurePreference = pressures[rng.Next(pressures.Length)],
                TemperaturePreference = temperatures[rng.Next(temperatures.Length)],
                MusicPreference = musicPrefs[rng.Next(musicPrefs.Length)],
                ReferralSource = referralSources[rng.Next(referralSources.Length)],
                PreferredCommunicationChannel = commChannels[rng.Next(commChannels.Length)],
                MarketingConsent = rng.Next(2) == 1,
                SmsConsent = rng.Next(2) == 1,
                IsActive = true,
                CreatedAt = firstVisit,
                UpdatedAt = now
            });
        }

        // ── Promising (12): Recent <30d, medium value ₱3K-₱10K ──
        var promData = new (int visits, decimal spent, int daysAgo, string membership)[]
        {
            (5, 9500m, 3, "Silver"), (4, 8000m, 5, "Regular"), (4, 7500m, 7, "Regular"),
            (3, 7000m, 8, "Regular"), (3, 6500m, 10, "Regular"), (3, 5800m, 12, "Regular"),
            (3, 5200m, 14, "Regular"), (2, 4800m, 5, "Regular"), (2, 4200m, 8, "Regular"),
            (2, 3800m, 10, "Regular"), (2, 3500m, 15, "Regular"), (2, 3200m, 18, "Regular")
        };
        foreach (var v in promData)
        {
            var i = idx++;
            var firstVisit = now.AddMonths(-rng.Next(2, 8));
            customers.Add(new Customer
            {
                CustomerCode = $"CUST-{i + 1:D3}",
                FirstName = firstNames[i],
                LastName = lastNames[i],
                Email = $"{firstNames[i].ToLower()}.{lastNames[i].ToLower().Replace(" ", "")}@email.com",
                PhoneNumber = $"+63 9{rng.Next(17, 29):D2} {rng.Next(100, 999)} {rng.Next(1000, 9999)}",
                Gender = genders[i % 2],
                DateOfBirth = new DateTime(rng.Next(1985, 2003), rng.Next(1, 13), rng.Next(1, 28)),
                Address = addresses[i % addresses.Length],
                City = cities[i % cities.Length],
                MembershipType = v.membership,
                LoyaltyPoints = v.visits * 20,
                TotalVisits = v.visits,
                TotalSpent = v.spent,
                FirstVisitDate = firstVisit,
                LastVisitDate = now.AddDays(-v.daysAgo),
                PressurePreference = pressures[rng.Next(pressures.Length)],
                TemperaturePreference = temperatures[rng.Next(temperatures.Length)],
                MusicPreference = musicPrefs[rng.Next(musicPrefs.Length)],
                ReferralSource = referralSources[rng.Next(referralSources.Length)],
                PreferredCommunicationChannel = commChannels[rng.Next(commChannels.Length)],
                MarketingConsent = true,
                SmsConsent = rng.Next(2) == 1,
                IsActive = true,
                CreatedAt = firstVisit,
                UpdatedAt = now
            });
        }

        // ── New Customers (20): Recent <30d, <0.5 visits/mo, <₱3K ──
        var newData = new (int visits, decimal spent, int daysAgo)[]
        {
            (1, 800m, 1), (1, 900m, 2), (1, 1000m, 3), (1, 1100m, 4), (1, 1200m, 5),
            (1, 1300m, 6), (1, 1500m, 7), (1, 1600m, 8), (1, 1800m, 10), (1, 2000m, 12),
            (2, 2200m, 3), (2, 2400m, 5), (2, 2500m, 7), (2, 2600m, 8), (2, 2800m, 10),
            (1, 850m, 14), (1, 950m, 16), (1, 1050m, 18), (1, 1150m, 20), (1, 1250m, 22)
        };
        foreach (var v in newData)
        {
            var i = idx++;
            var firstVisit = now.AddDays(-(v.daysAgo + rng.Next(0, 10)));
            customers.Add(new Customer
            {
                CustomerCode = $"CUST-{i + 1:D3}",
                FirstName = firstNames[i],
                LastName = lastNames[i],
                Email = $"{firstNames[i].ToLower()}.{lastNames[i].ToLower().Replace(" ", "")}@email.com",
                PhoneNumber = $"+63 9{rng.Next(17, 29):D2} {rng.Next(100, 999)} {rng.Next(1000, 9999)}",
                Gender = genders[i % 2],
                DateOfBirth = new DateTime(rng.Next(1990, 2005), rng.Next(1, 13), rng.Next(1, 28)),
                Address = addresses[i % addresses.Length],
                City = cities[i % cities.Length],
                MembershipType = "Regular",
                LoyaltyPoints = v.visits * 10,
                TotalVisits = v.visits,
                TotalSpent = v.spent,
                FirstVisitDate = firstVisit,
                LastVisitDate = now.AddDays(-v.daysAgo),
                ReferralSource = referralSources[rng.Next(referralSources.Length)],
                PreferredCommunicationChannel = "Email",
                MarketingConsent = rng.Next(2) == 1,
                SmsConsent = false,
                IsActive = true,
                CreatedAt = firstVisit,
                UpdatedAt = now
            });
        }

        // ── At-Risk (15): Recency 30-90d, freq 0.5-2/mo, varied value ──
        var atRiskData = new (int visits, decimal spent, int daysAgo, string membership)[]
        {
            (18, 40000m, 35, "Gold"), (16, 35000m, 40, "Gold"), (14, 30000m, 42, "Silver"),
            (12, 25000m, 45, "Silver"), (11, 22000m, 50, "Silver"), (10, 18000m, 55, "Silver"),
            (9, 15000m, 58, "Regular"), (8, 12000m, 60, "Regular"), (8, 10000m, 65, "Regular"),
            (7, 9000m, 68, "Regular"), (7, 8000m, 70, "Regular"), (6, 7000m, 72, "Regular"),
            (6, 6000m, 75, "Regular"), (5, 5500m, 80, "Regular"), (5, 4500m, 85, "Regular")
        };
        foreach (var v in atRiskData)
        {
            var i = idx++;
            var firstVisit = now.AddMonths(-rng.Next(8, 20));
            customers.Add(new Customer
            {
                CustomerCode = $"CUST-{i + 1:D3}",
                FirstName = firstNames[i],
                LastName = lastNames[i],
                Email = $"{firstNames[i].ToLower()}.{lastNames[i].ToLower().Replace(" ", "")}@email.com",
                PhoneNumber = $"+63 9{rng.Next(17, 29):D2} {rng.Next(100, 999)} {rng.Next(1000, 9999)}",
                Gender = genders[i % 2],
                DateOfBirth = new DateTime(rng.Next(1975, 2000), rng.Next(1, 13), rng.Next(1, 28)),
                Address = addresses[i % addresses.Length],
                City = cities[i % cities.Length],
                MembershipType = v.membership,
                LoyaltyPoints = v.visits * 25,
                TotalVisits = v.visits,
                TotalSpent = v.spent,
                FirstVisitDate = firstVisit,
                LastVisitDate = now.AddDays(-v.daysAgo),
                PressurePreference = pressures[rng.Next(pressures.Length)],
                TemperaturePreference = temperatures[rng.Next(temperatures.Length)],
                MusicPreference = musicPrefs[rng.Next(musicPrefs.Length)],
                ReferralSource = referralSources[rng.Next(referralSources.Length)],
                PreferredCommunicationChannel = commChannels[rng.Next(commChannels.Length)],
                MarketingConsent = true,
                SmsConsent = rng.Next(2) == 1,
                IsActive = true,
                CreatedAt = firstVisit,
                UpdatedAt = now
            });
        }

        // ── Hibernating (13): Recency >90d, freq ≥0.5/mo, varied value ──
        var hibData = new (int visits, decimal spent, int daysAgo, string membership)[]
        {
            (15, 25000m, 100, "Silver"), (12, 20000m, 110, "Silver"), (10, 18000m, 120, "Silver"),
            (9, 15000m, 130, "Regular"), (8, 12000m, 140, "Regular"), (8, 10000m, 150, "Regular"),
            (7, 9000m, 160, "Regular"), (7, 8000m, 165, "Regular"), (6, 7000m, 170, "Regular"),
            (6, 6500m, 175, "Regular"), (5, 5500m, 180, "Regular"), (5, 5000m, 190, "Regular"),
            (5, 4500m, 200, "Regular")
        };
        foreach (var v in hibData)
        {
            var i = idx++;
            var firstVisit = now.AddMonths(-rng.Next(12, 30));
            customers.Add(new Customer
            {
                CustomerCode = $"CUST-{i + 1:D3}",
                FirstName = firstNames[i],
                LastName = lastNames[i],
                Email = $"{firstNames[i].ToLower()}.{lastNames[i].ToLower().Replace(" ", "")}@email.com",
                PhoneNumber = $"+63 9{rng.Next(17, 29):D2} {rng.Next(100, 999)} {rng.Next(1000, 9999)}",
                Gender = genders[i % 2],
                DateOfBirth = new DateTime(rng.Next(1975, 2000), rng.Next(1, 13), rng.Next(1, 28)),
                Address = addresses[i % addresses.Length],
                City = cities[i % cities.Length],
                MembershipType = v.membership,
                LoyaltyPoints = v.visits * 15,
                TotalVisits = v.visits,
                TotalSpent = v.spent,
                FirstVisitDate = firstVisit,
                LastVisitDate = now.AddDays(-v.daysAgo),
                PressurePreference = pressures[rng.Next(pressures.Length)],
                ReferralSource = referralSources[rng.Next(referralSources.Length)],
                PreferredCommunicationChannel = "Email",
                MarketingConsent = false,
                SmsConsent = false,
                IsActive = true,
                CreatedAt = firstVisit,
                UpdatedAt = now
            });
        }

        // ── Lost (12): Recency >90d, freq <0.5/mo, monetary <₱3K ──
        var lostData = new (int visits, decimal spent, int daysAgo)[]
        {
            (3, 2800m, 150), (2, 2500m, 180), (2, 2200m, 200), (2, 2000m, 220),
            (1, 1800m, 250), (1, 1500m, 270), (1, 1200m, 300), (1, 1000m, 320),
            (1, 900m, 330), (1, 850m, 340), (1, 800m, 350), (1, 800m, 365)
        };
        foreach (var v in lostData)
        {
            var i = idx++;
            var firstVisit = now.AddDays(-(v.daysAgo + rng.Next(0, 30)));
            customers.Add(new Customer
            {
                CustomerCode = $"CUST-{i + 1:D3}",
                FirstName = firstNames[i],
                LastName = lastNames[i],
                Email = $"{firstNames[i].ToLower()}.{lastNames[i].ToLower().Replace(" ", "")}@email.com",
                PhoneNumber = $"+63 9{rng.Next(17, 29):D2} {rng.Next(100, 999)} {rng.Next(1000, 9999)}",
                Gender = genders[i % 2],
                DateOfBirth = new DateTime(rng.Next(1980, 2002), rng.Next(1, 13), rng.Next(1, 28)),
                Address = addresses[i % addresses.Length],
                City = cities[i % cities.Length],
                MembershipType = "Regular",
                LoyaltyPoints = 0,
                TotalVisits = v.visits,
                TotalSpent = v.spent,
                FirstVisitDate = firstVisit,
                LastVisitDate = now.AddDays(-v.daysAgo),
                ReferralSource = referralSources[rng.Next(referralSources.Length)],
                PreferredCommunicationChannel = "None",
                MarketingConsent = false,
                SmsConsent = false,
                IsActive = true,
                CreatedAt = firstVisit,
                UpdatedAt = now
            });
        }

        await _context.Customers.AddRangeAsync(customers);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} sample customers (DBSCAN-optimized)", customers.Count);
    }

    private async Task SeedSampleEmployeesAsync()
    {
        if (await _context.Employees.AnyAsync())
        {
            _logger.LogInformation("Employees already seeded, skipping...");
            return;
        }

        var employees = new List<Employee>
        {
            // Therapists
            new() { EmployeeCode = "EMP-001", FirstName = "Rosa", LastName = "Mercado", DateOfBirth = new DateTime(1990, 5, 15), Gender = "Female", PhoneNumber = "+63 917 111 2222", Email = "rosa.m@middaymistspa.com", Position = "Senior Therapist", Department = "Operations", EmploymentType = "Regular", HireDate = new DateTime(2020, 3, 15), DailyRate = 962, MonthlyBasicSalary = 25000, SSSNumber = "34-1234567-8", PhilHealthNumber = "12-345678901-2", PagIBIGNumber = "1234-5678-9012", TINNumber = "123-456-789-000", IsTherapist = true, Specialization = "Swedish Massage, Deep Tissue", IsActive = true },
            new() { EmployeeCode = "EMP-002", FirstName = "Jenny", LastName = "Aquino", DateOfBirth = new DateTime(1992, 8, 20), Gender = "Female", PhoneNumber = "+63 918 222 3333", Email = "jenny.a@middaymistspa.com", Position = "Therapist", Department = "Operations", EmploymentType = "Regular", HireDate = new DateTime(2021, 6, 1), DailyRate = 769, MonthlyBasicSalary = 20000, SSSNumber = "34-2345678-9", PhilHealthNumber = "12-456789012-3", PagIBIGNumber = "2345-6789-0123", TINNumber = "234-567-890-000", IsTherapist = true, Specialization = "Aromatherapy, Hot Stone", IsActive = true },
            new() { EmployeeCode = "EMP-003", FirstName = "Mark", LastName = "Villanueva", DateOfBirth = new DateTime(1988, 3, 10), Gender = "Male", PhoneNumber = "+63 919 333 4444", Email = "mark.v@middaymistspa.com", Position = "Therapist", Department = "Operations", EmploymentType = "Regular", HireDate = new DateTime(2022, 1, 10), DailyRate = 692, MonthlyBasicSalary = 18000, SSSNumber = "34-3456789-0", PhilHealthNumber = "12-567890123-4", PagIBIGNumber = "3456-7890-1234", TINNumber = "345-678-901-000", IsTherapist = true, Specialization = "Sports Massage, Thai Massage", IsActive = true },
            new() { EmployeeCode = "EMP-004", FirstName = "Ana", LastName = "Gonzales", DateOfBirth = new DateTime(1995, 11, 25), Gender = "Female", PhoneNumber = "+63 920 444 5555", Email = "ana.g@middaymistspa.com", Position = "Therapist", Department = "Operations", EmploymentType = "Part-Time", HireDate = new DateTime(2023, 4, 1), DailyRate = 462, MonthlyBasicSalary = 12000, SSSNumber = "34-4567890-1", PhilHealthNumber = "12-678901234-5", PagIBIGNumber = "4567-8901-2345", TINNumber = "456-789-012-000", IsTherapist = true, Specialization = "Foot Reflexology", IsActive = true },
            new() { EmployeeCode = "EMP-005", FirstName = "Paolo", LastName = "Ramos", DateOfBirth = new DateTime(1985, 7, 8), Gender = "Male", PhoneNumber = "+63 921 555 6666", Email = "paolo.r@middaymistspa.com", Position = "Senior Therapist", Department = "Operations", EmploymentType = "Regular", HireDate = new DateTime(2019, 8, 20), DailyRate = 1077, MonthlyBasicSalary = 28000, SSSNumber = "34-5678901-2", PhilHealthNumber = "12-789012345-6", PagIBIGNumber = "5678-9012-3456", TINNumber = "567-890-123-000", IsTherapist = true, Specialization = "Deep Tissue, Sports Massage", IsActive = true },

            // Front Desk
            new() { EmployeeCode = "EMP-006", FirstName = "Carla", LastName = "Mendoza", DateOfBirth = new DateTime(1993, 2, 14), Gender = "Female", PhoneNumber = "+63 922 666 7777", Email = "carla.m@middaymistspa.com", Position = "Receptionist", Department = "Front Desk", EmploymentType = "Regular", HireDate = new DateTime(2021, 2, 1), DailyRate = 692, MonthlyBasicSalary = 18000, SSSNumber = "34-6789012-3", PhilHealthNumber = "12-890123456-7", PagIBIGNumber = "6789-0123-4567", TINNumber = "678-901-234-000", IsTherapist = false, IsActive = true },
            new() { EmployeeCode = "EMP-007", FirstName = "Michelle", LastName = "Santos", DateOfBirth = new DateTime(1991, 9, 30), Gender = "Female", PhoneNumber = "+63 923 777 8888", Email = "michelle.s@middaymistspa.com", Position = "Senior Receptionist", Department = "Front Desk", EmploymentType = "Regular", HireDate = new DateTime(2019, 11, 15), DailyRate = 846, MonthlyBasicSalary = 22000, SSSNumber = "34-7890123-4", PhilHealthNumber = "12-901234567-8", PagIBIGNumber = "7890-1234-5678", TINNumber = "789-012-345-000", IsTherapist = false, IsActive = true },

            // Management
            new() { EmployeeCode = "EMP-008", FirstName = "Ricardo", LastName = "Lim", DateOfBirth = new DateTime(1980, 4, 5), Gender = "Male", PhoneNumber = "+63 924 888 9999", Email = "ricardo.l@middaymistspa.com", Position = "Operations Manager", Department = "Management", EmploymentType = "Regular", HireDate = new DateTime(2018, 5, 1), DailyRate = 1731, MonthlyBasicSalary = 45000, SSSNumber = "34-8901234-5", PhilHealthNumber = "12-012345678-9", PagIBIGNumber = "8901-2345-6789", TINNumber = "890-123-456-000", IsTherapist = false, IsActive = true },
            new() { EmployeeCode = "EMP-009", FirstName = "Teresa", LastName = "Reyes", DateOfBirth = new DateTime(1978, 6, 20), Gender = "Female", PhoneNumber = "+63 925 999 0000", Email = "teresa.r@middaymistspa.com", Position = "HR Manager", Department = "Human Resources", EmploymentType = "Regular", HireDate = new DateTime(2017, 9, 1), DailyRate = 1538, MonthlyBasicSalary = 40000, SSSNumber = "34-9012345-6", PhilHealthNumber = "12-123456789-0", PagIBIGNumber = "9012-3456-7890", TINNumber = "901-234-567-000", IsTherapist = false, IsActive = true },
            new() { EmployeeCode = "EMP-010", FirstName = "Antonio", LastName = "Cruz", DateOfBirth = new DateTime(1982, 12, 12), Gender = "Male", PhoneNumber = "+63 926 000 1111", Email = "antonio.c@middaymistspa.com", Position = "Accountant", Department = "Finance", EmploymentType = "Regular", HireDate = new DateTime(2020, 1, 15), DailyRate = 1346, MonthlyBasicSalary = 35000, SSSNumber = "34-0123456-7", PhilHealthNumber = "12-234567890-1", PagIBIGNumber = "0123-4567-8901", TINNumber = "012-345-678-000", IsTherapist = false, IsActive = true }
        };

        await _context.Employees.AddRangeAsync(employees);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} sample employees", employees.Count);
    }

    /// <summary>
    /// Links User accounts to Employee records so they can clock in/out themselves.
    /// Creates Employee records for Users that don't have one yet.
    /// </summary>
    private async Task LinkUsersToEmployeesAsync()
    {
        // Roles whose users need an Employee record to clock in/out
        var staffRoles = new[] { "Receptionist", "Therapist", "Inventory", "Accountant", "HR", "Sales Ledger" };

        var usersWithRoles = await _context.Users
            .Include(u => u.Role)
            .Where(u => u.Role != null && staffRoles.Contains(u.Role.RoleName))
            .ToListAsync();

        // Get existing employee-user links
        var linkedUserIds = await _context.Employees
            .Where(e => e.UserId != null)
            .Select(e => e.UserId!.Value)
            .ToListAsync();

        // Get max employee code number for new records
        var maxCode = await _context.Employees
            .Where(e => e.EmployeeCode.StartsWith("EMP-"))
            .Select(e => e.EmployeeCode)
            .ToListAsync();
        var nextNum = maxCode
            .Select(c => int.TryParse(c.Replace("EMP-", ""), out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var created = 0;
        foreach (var user in usersWithRoles)
        {
            if (linkedUserIds.Contains(user.UserId))
                continue;

            // Determine position and department based on role
            var (position, department, isTherapist) = user.Role!.RoleName switch
            {
                "Therapist" => ("Therapist", "Operations", true),
                "Receptionist" => ("Receptionist", "Front Desk", false),
                "Inventory" => ("Inventory Staff", "Inventory", false),
                "Accountant" => ("Accountant", "Finance", false),
                "HR" => ("HR Staff", "Human Resources", false),
                "Sales Ledger" => ("Sales Ledger", "Finance", false),
                _ => ("Staff", "General", false)
            };

            var emp = new Employee
            {
                UserId = user.UserId,
                EmployeeCode = $"EMP-{nextNum:D3}",
                FirstName = user.FirstName,
                LastName = user.LastName,
                DateOfBirth = new DateTime(1990, 1, 1),
                Gender = "Other",
                PhoneNumber = user.Email ?? "",
                Email = user.Email,
                Position = position,
                Department = department,
                EmploymentType = "Regular",
                HireDate = DateTime.UtcNow.AddYears(-1),
                DailyRate = 700,
                MonthlyBasicSalary = 18200,
                IsTherapist = isTherapist,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Employees.AddAsync(emp);
            nextNum++;
            created++;
        }

        if (created > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Linked {Count} user accounts to new employee records", created);
        }
        else
        {
            _logger.LogInformation("All staff users already linked to employees, skipping...");
        }
    }

    #endregion

    #region Employee Shifts & Attendance

    private async Task SeedEmployeeShiftsAsync()
    {
        if (await _context.EmployeeShifts.AnyAsync())
        {
            _logger.LogInformation("Employee shifts already seeded, skipping...");
            return;
        }

        var employeeMap = await _context.Employees
            .ToDictionaryAsync(e => e.EmployeeCode, e => e.EmployeeId);

        if (!employeeMap.Any())
        {
            _logger.LogWarning("No employees found, skipping shift seeding...");
            return;
        }

        var shiftStart = new TimeSpan(9, 0, 0);  // 9:00 AM
        var shiftEnd = new TimeSpan(18, 0, 0);   // 6:00 PM
        var effectiveFrom = new DateTime(2026, 1, 1);

        // Regular employees: Mon–Sat (DayOfWeek 1–6)
        var regularCodes = new[] { "EMP-001", "EMP-002", "EMP-003", "EMP-005", "EMP-006", "EMP-007", "EMP-008", "EMP-009", "EMP-010" };
        var regularDays = new[] { 1, 2, 3, 4, 5, 6 };

        // Part-time: Tue, Thu, Sat (DayOfWeek 2, 4, 6)
        var partTimeCodes = new[] { "EMP-004" };
        var partTimeDays = new[] { 2, 4, 6 };

        var shifts = new List<EmployeeShift>();

        foreach (var code in regularCodes)
        {
            if (!employeeMap.TryGetValue(code, out var empId)) continue;
            foreach (var day in regularDays)
                shifts.Add(new EmployeeShift { EmployeeId = empId, DayOfWeek = day, StartTime = shiftStart, EndTime = shiftEnd, IsRecurring = true, EffectiveFrom = effectiveFrom, EffectiveTo = null, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        }

        foreach (var code in partTimeCodes)
        {
            if (!employeeMap.TryGetValue(code, out var empId)) continue;
            foreach (var day in partTimeDays)
                shifts.Add(new EmployeeShift { EmployeeId = empId, DayOfWeek = day, StartTime = shiftStart, EndTime = shiftEnd, IsRecurring = true, EffectiveFrom = effectiveFrom, EffectiveTo = null, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        }

        await _context.EmployeeShifts.AddRangeAsync(shifts);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} employee shifts", shifts.Count);
    }

    private async Task SeedAttendanceRecordsAsync()
    {
        if (await _context.AttendanceRecords.AnyAsync())
        {
            _logger.LogInformation("Attendance records already seeded, skipping...");
            return;
        }

        var employeeMap = await _context.Employees
            .ToDictionaryAsync(e => e.EmployeeCode, e => e.EmployeeId);

        if (!employeeMap.Any())
        {
            _logger.LogWarning("No employees found, skipping attendance seeding...");
            return;
        }

        // Regular (Mon–Sat) vs Part-time (Tue, Thu, Sat)
        var regularCodes = new[] { "EMP-001", "EMP-002", "EMP-003", "EMP-005", "EMP-006", "EMP-007", "EMP-008", "EMP-009", "EMP-010" };
        var partTimeCodes = new[] { "EMP-004" };

        bool WorksThisDay(string code, DateTime date)
        {
            var dow = (int)date.DayOfWeek; // 0=Sun … 6=Sat
            if (regularCodes.Contains(code)) return dow >= 1 && dow <= 6;
            if (partTimeCodes.Contains(code)) return dow == 2 || dow == 4 || dow == 6;
            return false;
        }

        // Payroll period Feb 15–23, 2026 (skip Sundays Feb 15 & 22)
        var workDays = new[]
        {
            new DateTime(2026, 2, 16), // Monday
            new DateTime(2026, 2, 17), // Tuesday
            new DateTime(2026, 2, 18), // Wednesday
            new DateTime(2026, 2, 19), // Thursday
            new DateTime(2026, 2, 20), // Friday
            new DateTime(2026, 2, 21), // Saturday
            new DateTime(2026, 2, 23)  // Monday
        };

        // OT days: clockout at 7 PM instead of 6 PM
        var otDays = new Dictionary<(string, DateTime), TimeSpan>
        {
            { ("EMP-005", new DateTime(2026, 2, 19)), new TimeSpan(19, 0, 0) }, // Paolo OT Thursday
            { ("EMP-001", new DateTime(2026, 2, 20)), new TimeSpan(19, 0, 0) }  // Rosa OT Friday
        };

        var allCodes = regularCodes.Concat(partTimeCodes).ToArray();
        var records = new List<AttendanceRecord>();

        foreach (var day in workDays)
        {
            foreach (var code in allCodes)
            {
                if (!WorksThisDay(code, day)) continue;
                if (!employeeMap.TryGetValue(code, out var empId)) continue;

                var clockIn = day.AddHours(9);  // 9:00 AM
                var breakStart = day.AddHours(12); // 12:00 PM
                var breakEnd = day.AddHours(13);   // 1:00 PM
                var clockOutTime = otDays.TryGetValue((code, day), out var otOut)
                    ? otOut
                    : new TimeSpan(18, 0, 0); // 6:00 PM standard
                var clockOut = day.Add(clockOutTime);
                var totalHours = (decimal)(clockOut - clockIn - TimeSpan.FromHours(1)).TotalHours;

                records.Add(new AttendanceRecord
                {
                    EmployeeId = empId,
                    Date = day,
                    ClockIn = clockIn,
                    ClockOut = clockOut,
                    BreakStart = breakStart,
                    BreakEnd = breakEnd,
                    TotalHours = totalHours,
                    BreakMinutes = 60,
                    Status = "ClockedOut",
                    IsApproved = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.AttendanceRecords.AddRangeAsync(records);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} attendance records for payroll period Feb 15–23 2026", records.Count);
    }

    #endregion

    #region Rooms

    private async Task SeedRoomsAsync()
    {
        if (await _context.Rooms.AnyAsync())
        {
            _logger.LogInformation("Rooms already seeded, skipping...");
            return;
        }

        var rooms = new List<Room>
        {
            new() { RoomName = "Serenity Suite", RoomCode = "RM-001", Description = "Premium private treatment room with ambient lighting and sound system", RoomType = "Massage", Capacity = 1, IsActive = true },
            new() { RoomName = "Tranquil Room", RoomCode = "RM-002", Description = "Standard massage room with heated table", RoomType = "Massage", Capacity = 1, IsActive = true },
            new() { RoomName = "Harmony Room", RoomCode = "RM-003", Description = "Spacious room for Swedish and deep tissue treatments", RoomType = "Massage", Capacity = 1, IsActive = true },
            new() { RoomName = "Glow Studio", RoomCode = "RM-004", Description = "Facial and skin care treatment room with magnifying lamp", RoomType = "Facial", Capacity = 1, IsActive = true },
            new() { RoomName = "Radiance Room", RoomCode = "RM-005", Description = "Facial and anti-aging treatment room", RoomType = "Facial", Capacity = 1, IsActive = true },
            new() { RoomName = "Zen Den", RoomCode = "RM-006", Description = "Multi-purpose room for body scrubs, wraps, and aromatherapy", RoomType = "Multi-Purpose", Capacity = 1, IsActive = true },
            new() { RoomName = "Nail Bar 1", RoomCode = "RM-007", Description = "Manicure and pedicure station", RoomType = "Nail", Capacity = 2, IsActive = true },
            new() { RoomName = "Nail Bar 2", RoomCode = "RM-008", Description = "Manicure and pedicure station", RoomType = "Nail", Capacity = 2, IsActive = true },
        };

        await _context.Rooms.AddRangeAsync(rooms);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} rooms", rooms.Count);
    }

    #endregion

    #region Chart of Accounts

    private async Task SeedChartOfAccountsAsync()
    {
        if (await _context.ChartOfAccounts.AnyAsync())
        {
            _logger.LogInformation("Chart of accounts already seeded, skipping...");
            return;
        }

        var accounts = new List<ChartOfAccount>
        {
            // ── Assets (Debit-normal) ──
            new() { AccountCode = "1000", AccountName = "Assets", AccountType = "Asset", NormalBalance = "Debit", AccountCategory = "Assets" },
            new() { AccountCode = "1010", AccountName = "Cash on Hand", AccountType = "Asset", NormalBalance = "Debit", AccountCategory = "Cash" },
            new() { AccountCode = "1020", AccountName = "Cash in Bank", AccountType = "Asset", NormalBalance = "Debit", AccountCategory = "Bank" },
            new() { AccountCode = "1100", AccountName = "Accounts Receivable", AccountType = "Asset", NormalBalance = "Debit", AccountCategory = "Accounts Receivable" },
            new() { AccountCode = "1200", AccountName = "Inventory - Products", AccountType = "Asset", NormalBalance = "Debit", AccountCategory = "Inventory" },
            new() { AccountCode = "1300", AccountName = "Prepaid Expenses", AccountType = "Asset", NormalBalance = "Debit", AccountCategory = "Prepaid" },
            new() { AccountCode = "1500", AccountName = "Equipment", AccountType = "Asset", NormalBalance = "Debit", AccountCategory = "Fixed Asset" },
            new() { AccountCode = "1510", AccountName = "Furniture & Fixtures", AccountType = "Asset", NormalBalance = "Debit", AccountCategory = "Fixed Asset" },
            new() { AccountCode = "1590", AccountName = "Accumulated Depreciation", AccountType = "Asset", NormalBalance = "Credit", AccountCategory = "Contra Asset" },

            // ── Liabilities (Credit-normal) ──
            new() { AccountCode = "2000", AccountName = "Liabilities", AccountType = "Liability", NormalBalance = "Credit", AccountCategory = "Liabilities" },
            new() { AccountCode = "2010", AccountName = "Accounts Payable", AccountType = "Liability", NormalBalance = "Credit", AccountCategory = "Accounts Payable" },
            new() { AccountCode = "2100", AccountName = "SSS Payable", AccountType = "Liability", NormalBalance = "Credit", AccountCategory = "SSS Payable" },
            new() { AccountCode = "2110", AccountName = "PhilHealth Payable", AccountType = "Liability", NormalBalance = "Credit", AccountCategory = "PhilHealth Payable" },
            new() { AccountCode = "2120", AccountName = "Pag-IBIG Payable", AccountType = "Liability", NormalBalance = "Credit", AccountCategory = "Pag-IBIG Payable" },
            new() { AccountCode = "2130", AccountName = "Withholding Tax Payable", AccountType = "Liability", NormalBalance = "Credit", AccountCategory = "Withholding Tax Payable" },
            new() { AccountCode = "2200", AccountName = "Accrued Expenses", AccountType = "Liability", NormalBalance = "Credit", AccountCategory = "Accrued" },
            new() { AccountCode = "2300", AccountName = "Unearned Revenue", AccountType = "Liability", NormalBalance = "Credit", AccountCategory = "Deferred Revenue" },

            // ── Equity (Credit-normal) ──
            new() { AccountCode = "3000", AccountName = "Owner's Equity", AccountType = "Equity", NormalBalance = "Credit", AccountCategory = "Owner's Equity" },
            new() { AccountCode = "3100", AccountName = "Owner's Capital", AccountType = "Equity", NormalBalance = "Credit", AccountCategory = "Capital" },
            new() { AccountCode = "3200", AccountName = "Retained Earnings", AccountType = "Equity", NormalBalance = "Credit", AccountCategory = "Retained Earnings" },
            new() { AccountCode = "3300", AccountName = "Owner's Drawings", AccountType = "Equity", NormalBalance = "Debit", AccountCategory = "Drawings" },

            // ── Revenue (Credit-normal) ──
            new() { AccountCode = "4000", AccountName = "Revenue", AccountType = "Revenue", NormalBalance = "Credit", AccountCategory = "Revenue" },
            new() { AccountCode = "4010", AccountName = "Service Revenue", AccountType = "Revenue", NormalBalance = "Credit", AccountCategory = "Service Revenue" },
            new() { AccountCode = "4020", AccountName = "Product Sales Revenue", AccountType = "Revenue", NormalBalance = "Credit", AccountCategory = "Product Revenue" },
            new() { AccountCode = "4030", AccountName = "Tips Revenue", AccountType = "Revenue", NormalBalance = "Credit", AccountCategory = "Tips" },
            new() { AccountCode = "4050", AccountName = "Service Returns & Allowances", AccountType = "Revenue", NormalBalance = "Debit", AccountCategory = "Service Returns" },
            new() { AccountCode = "4060", AccountName = "Product Returns & Allowances", AccountType = "Revenue", NormalBalance = "Debit", AccountCategory = "Product Returns" },
            new() { AccountCode = "4900", AccountName = "Other Income", AccountType = "Revenue", NormalBalance = "Credit", AccountCategory = "Other Income" },

            // ── Expenses (Debit-normal) ──
            new() { AccountCode = "5000", AccountName = "Expenses", AccountType = "Expense", NormalBalance = "Debit", AccountCategory = "Expenses" },
            new() { AccountCode = "5010", AccountName = "Salary Expense", AccountType = "Expense", NormalBalance = "Debit", AccountCategory = "Salary Expense" },
            new() { AccountCode = "5020", AccountName = "Employer SSS Contribution", AccountType = "Expense", NormalBalance = "Debit", AccountCategory = "Employer Benefits" },
            new() { AccountCode = "5030", AccountName = "Employer PhilHealth Contribution", AccountType = "Expense", NormalBalance = "Debit", AccountCategory = "Employer Benefits" },
            new() { AccountCode = "5040", AccountName = "Employer Pag-IBIG Contribution", AccountType = "Expense", NormalBalance = "Debit", AccountCategory = "Employer Benefits" },
            new() { AccountCode = "5100", AccountName = "Rent Expense", AccountType = "Expense", NormalBalance = "Debit", AccountCategory = "Rent" },
            new() { AccountCode = "5200", AccountName = "Utilities Expense", AccountType = "Expense", NormalBalance = "Debit", AccountCategory = "Utilities" },
            new() { AccountCode = "5300", AccountName = "Supplies Expense", AccountType = "Expense", NormalBalance = "Debit", AccountCategory = "Supplies" },
            new() { AccountCode = "5400", AccountName = "Depreciation Expense", AccountType = "Expense", NormalBalance = "Debit", AccountCategory = "Depreciation" },
            new() { AccountCode = "5500", AccountName = "Marketing Expense", AccountType = "Expense", NormalBalance = "Debit", AccountCategory = "Marketing" },
            new() { AccountCode = "5600", AccountName = "Insurance Expense", AccountType = "Expense", NormalBalance = "Debit", AccountCategory = "Insurance" },
            new() { AccountCode = "5700", AccountName = "Cost of Goods Sold", AccountType = "Expense", NormalBalance = "Debit", AccountCategory = "COGS" },
            new() { AccountCode = "5900", AccountName = "Miscellaneous Expense", AccountType = "Expense", NormalBalance = "Debit", AccountCategory = "Miscellaneous" }
        };

        // Set parent account IDs (header accounts)
        var assetChildren = accounts.Where(a => a.AccountType == "Asset" && a.AccountCode != "1000").ToList();
        var liabChildren = accounts.Where(a => a.AccountType == "Liability" && a.AccountCode != "2000").ToList();
        var equityChildren = accounts.Where(a => a.AccountType == "Equity" && a.AccountCode != "3000").ToList();
        var revenueChildren = accounts.Where(a => a.AccountType == "Revenue" && a.AccountCode != "4000").ToList();
        var expenseChildren = accounts.Where(a => a.AccountType == "Expense" && a.AccountCode != "5000").ToList();

        // Add all accounts first to get IDs
        await _context.ChartOfAccounts.AddRangeAsync(accounts);
        await _context.SaveChangesAsync();

        // Now set parent IDs
        var parentAsset = accounts.First(a => a.AccountCode == "1000");
        var parentLiab = accounts.First(a => a.AccountCode == "2000");
        var parentEquity = accounts.First(a => a.AccountCode == "3000");
        var parentRevenue = accounts.First(a => a.AccountCode == "4000");
        var parentExpense = accounts.First(a => a.AccountCode == "5000");

        foreach (var a in assetChildren) a.ParentAccountId = parentAsset.AccountId;
        foreach (var a in liabChildren) a.ParentAccountId = parentLiab.AccountId;
        foreach (var a in equityChildren) a.ParentAccountId = parentEquity.AccountId;
        foreach (var a in revenueChildren) a.ParentAccountId = parentRevenue.AccountId;
        foreach (var a in expenseChildren) a.ParentAccountId = parentExpense.AccountId;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} chart of accounts", accounts.Count);
    }

    /// <summary>
    /// Ensure contra-revenue accounts exist for existing databases.
    /// These are needed for proper refund accounting (Sales Returns & Allowances).
    /// </summary>
    private async Task EnsureContraRevenueAccountsAsync()
    {
        var revenueParent = await _context.ChartOfAccounts.FirstOrDefaultAsync(a => a.AccountCode == "4000");
        if (revenueParent == null) return;

        var contraAccounts = new List<(string Code, string Name, string Category)>
        {
            ("4050", "Service Returns & Allowances", "Service Returns"),
            ("4060", "Product Returns & Allowances", "Product Returns")
        };

        var added = 0;
        foreach (var (code, name, category) in contraAccounts)
        {
            if (!await _context.ChartOfAccounts.AnyAsync(a => a.AccountCode == code))
            {
                _context.ChartOfAccounts.Add(new ChartOfAccount
                {
                    AccountCode = code,
                    AccountName = name,
                    AccountType = "Revenue",
                    NormalBalance = "Debit",
                    AccountCategory = category,
                    ParentAccountId = revenueParent.AccountId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
                added++;
            }
        }

        if (added > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Added {Count} contra-revenue accounts", added);
        }
    }

    /// <summary>
    /// Ensures captcha settings exist in the database (for databases seeded before captcha feature was added)
    /// </summary>
    private async Task EnsureCaptchaSettingsAsync()
    {
        var hasCaptchaEnabled = await _context.SystemSettings.AnyAsync(s => s.SettingKey == "Captcha.Enabled");
        if (hasCaptchaEnabled) return;

        var captchaSettings = new List<SystemSetting>
        {
            new() { SettingKey = "Captcha.Enabled", SettingValue = "false", SettingType = "Boolean", Category = "Captcha", Description = "Enable reCAPTCHA on login page", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "Captcha.SiteKey", SettingValue = "6LfHZn8sAAAAAKBb6fKq8naNpNe94LfBVIlvpP-g", SettingType = "String", Category = "Captcha", Description = "Google reCAPTCHA v2 site key", IsEditable = true, UpdatedAt = DateTime.UtcNow },
            new() { SettingKey = "Captcha.SecretKey", SettingValue = "6LfHZn8sAAAAAL75V5kS1ugi4zENyDRuMXFGUC4j", SettingType = "String", Category = "Captcha", Description = "Google reCAPTCHA v2 secret key", IsEditable = true, UpdatedAt = DateTime.UtcNow }
        };

        await _context.SystemSettings.AddRangeAsync(captchaSettings);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded captcha settings (disabled by default, using Google test keys)");
    }

    #endregion
}
