using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MiddayMistSpa.API.Services;
using MiddayMistSpa.API.Settings;
using MiddayMistSpa.Core.Interfaces;
using MiddayMistSpa.Infrastructure.Data;
using MiddayMistSpa.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// CONFIGURATION - Bind settings from appsettings.json
// =============================================================================
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<PasswordPolicySettings>(builder.Configuration.GetSection(PasswordPolicySettings.SectionName));
builder.Services.Configure<LockoutPolicySettings>(builder.Configuration.GetSection(LockoutPolicySettings.SectionName));
builder.Services.Configure<SessionPolicySettings>(builder.Configuration.GetSection(SessionPolicySettings.SectionName));
builder.Services.Configure<CurrencySettings>(builder.Configuration.GetSection(CurrencySettings.SectionName));
builder.Services.Configure<TwoFactorSettings>(builder.Configuration.GetSection(TwoFactorSettings.SectionName));

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;
var currencySettings = builder.Configuration.GetSection(CurrencySettings.SectionName).Get<CurrencySettings>() ?? new CurrencySettings();

// =============================================================================
// DATABASE - Entity Framework Core with SQL Server
// =============================================================================
builder.Services.AddDbContext<SpaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// =============================================================================
// REPOSITORIES - Unit of Work pattern
// =============================================================================
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// =============================================================================
// SERVICES - Business Logic
// =============================================================================
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IShiftService, ShiftService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ISpaServiceService, SpaServiceService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IPayrollService, PayrollService>();
builder.Services.AddScoped<ITimeAttendanceService, TimeAttendanceService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAccountingService, AccountingService>();
builder.Services.AddScoped<IClusteringService, ClusteringService>();
builder.Services.AddScoped<ICurrencyService, CurrencyService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();
builder.Services.AddScoped<ICaptchaService, CaptchaService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, AnyPermissionAuthorizationHandler>();
builder.Services.AddMemoryCache();

// =============================================================================
// HTTP CLIENTS - External API integrations (no API keys required)
// =============================================================================
builder.Services.AddHttpClient<IIpGeoLocationService, IpWhoIsService>(client =>
{
    client.BaseAddress = new Uri(currencySettings.IpWhoIsBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "MiddayMistSpa/1.0");
});

builder.Services.AddHttpClient<IFrankfurterService, FrankfurterService>(client =>
{
    client.BaseAddress = new Uri(currencySettings.FrankfurterBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "MiddayMistSpa/1.0");
});

// =============================================================================
// CAPTCHA - Google reCAPTCHA v2 verification
// =============================================================================
builder.Services.AddHttpClient("Captcha", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

// =============================================================================
// BACKGROUND SERVICES - Periodic tasks
// =============================================================================
builder.Services.AddHostedService<CurrencyRateRefreshService>();

// =============================================================================
// AUTHENTICATION - JWT Bearer
// =============================================================================
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero // No tolerance for token expiration
    };
});

// =============================================================================
// AUTHORIZATION - Role-based policies
// =============================================================================
builder.Services.AddAuthorization(options =>
{
    // Legacy role-based policies (kept for backward compatibility)
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("SuperAdmin"));
    options.AddPolicy("AdminOrAbove", policy => policy.RequireRole("SuperAdmin", "Admin"));
    options.AddPolicy("AllStaff", policy => policy.RequireRole(
        "SuperAdmin", "Admin", "Receptionist", "Therapist", "Inventory", "Accountant", "HR", "Sales"));

    // Permission-based policies — driven by Roles & Permissions settings
    // Appointments
    options.AddPolicy("Permission:appointments.view", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("appointments.view")));
    options.AddPolicy("Permission:appointments.create", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("appointments.create")));
    options.AddPolicy("Permission:appointments.edit", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("appointments.edit")));
    options.AddPolicy("Permission:appointments.delete", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("appointments.delete")));
    // Customers
    options.AddPolicy("Permission:customers.view", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("customers.view")));
    options.AddPolicy("Permission:customers.create", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("customers.create")));
    options.AddPolicy("Permission:customers.edit", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("customers.edit")));
    options.AddPolicy("Permission:customers.delete", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("customers.delete")));
    // Services
    options.AddPolicy("Permission:services.view", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("services.view")));
    options.AddPolicy("Permission:services.create", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("services.create")));
    options.AddPolicy("Permission:services.edit", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("services.edit")));
    options.AddPolicy("Permission:services.delete", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("services.delete")));
    // Employees
    options.AddPolicy("Permission:employees.view", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("employees.view")));
    options.AddPolicy("Permission:employees.create", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("employees.create")));
    options.AddPolicy("Permission:employees.edit", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("employees.edit")));
    options.AddPolicy("Permission:employees.delete", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("employees.delete")));
    // Inventory
    options.AddPolicy("Permission:inventory.view", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("inventory.view")));
    options.AddPolicy("Permission:inventory.manage", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("inventory.manage")));
    // POS
    options.AddPolicy("Permission:pos.access", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("pos.access")));
    options.AddPolicy("Permission:pos.refund", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("pos.refund")));
    options.AddPolicy("Permission:pos.discount", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("pos.discount")));
    // Accounting
    options.AddPolicy("Permission:accounting.view", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("accounting.view")));
    options.AddPolicy("Permission:accounting.manage", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("accounting.manage")));
    // Payroll
    options.AddPolicy("Permission:payroll.view", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("payroll.view")));
    options.AddPolicy("Permission:payroll.manage", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("payroll.manage")));
    // Shifts
    options.AddPolicy("Permission:shifts.view", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("shifts.view")));
    options.AddPolicy("Permission:shifts.manage", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("shifts.manage")));
    // Time & Attendance
    options.AddPolicy("Permission:timeattendance.view", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("timeattendance.view")));
    options.AddPolicy("Permission:timeattendance.manage", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("timeattendance.manage")));
    // Reports
    options.AddPolicy("Permission:reports.view", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("reports.view")));
    options.AddPolicy("Permission:reports.export", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("reports.export")));
    // Notifications
    options.AddPolicy("Permission:notifications.view", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("notifications.view")));
    options.AddPolicy("Permission:notifications.manage", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("notifications.manage")));
    // Settings
    options.AddPolicy("Permission:settings.access", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("settings.access")));
    options.AddPolicy("Permission:settings.manage", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("settings.manage")));
});

// =============================================================================
// CONTROLLERS & Swagger
// =============================================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =============================================================================
// CORS - Allow Web frontend
// =============================================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp", policy =>
    {
        policy.WithOrigins(
                "https://localhost:7001",
                "http://localhost:5001")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// =============================================================================
// HTTP CONTEXT - For accessing user context in services
// =============================================================================
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// =============================================================================
// DATABASE SEEDING - Run on startup
// =============================================================================
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
    await seeder.SeedAsync();
}

// =============================================================================
// MIDDLEWARE PIPELINE
// =============================================================================

// Development tools
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS must be before Authentication/Authorization
app.UseCors("AllowWebApp");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Root endpoint - API info
app.MapGet("/", () => Results.Ok(new
{
    Name = "MiddayMist Spa API",
    Version = "1.0.0",
    Status = "Running",
    Timestamp = DateTime.UtcNow,
    Endpoints = new
    {
        Health = "/health",
        Swagger = "/swagger",
        Api = "/api/*"
    }
}))
   .WithName("Root")
   .WithTags("Info")
   .AllowAnonymous();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck")
   .WithTags("Health")
   .AllowAnonymous();

app.Run();
