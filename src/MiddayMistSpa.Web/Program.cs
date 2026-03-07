using MiddayMistSpa.Web.Components;
using MiddayMistSpa.Web.Services;
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
// BLAZOR SERVER COMPONENTS
// =============================================================================
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// =============================================================================
// PRODUCTION: Embedded API (single-site deployment for MonsterASP.NET)
// DEVELOPMENT: Separate API server via HttpClient
// =============================================================================
if (!builder.Environment.IsDevelopment())
{
    // --- PRODUCTION MODE: Embed API directly ---

    // Configuration - Bind settings from appsettings.Production.json
    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
    builder.Services.Configure<PasswordPolicySettings>(builder.Configuration.GetSection(PasswordPolicySettings.SectionName));
    builder.Services.Configure<LockoutPolicySettings>(builder.Configuration.GetSection(LockoutPolicySettings.SectionName));
    builder.Services.Configure<SessionPolicySettings>(builder.Configuration.GetSection(SessionPolicySettings.SectionName));
    builder.Services.Configure<CurrencySettings>(builder.Configuration.GetSection(CurrencySettings.SectionName));

    var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;
    var currencySettings = builder.Configuration.GetSection(CurrencySettings.SectionName).Get<CurrencySettings>() ?? new CurrencySettings();

    // Database - Entity Framework Core with SQL Server
    builder.Services.AddDbContext<SpaDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Repositories - Unit of Work pattern
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

    // Services - Business Logic
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

    // HTTP Clients - External API integrations
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

    builder.Services.AddHttpClient("Captcha", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(10);
    });

    // Background Services
    builder.Services.AddHostedService<CurrencyRateRefreshService>();

    // Authentication - JWT Bearer
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = false; // MonsterASP.NET free plan - no SSL
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

    // Authorization - Role-based and permission-based policies
    builder.Services.AddAuthorization(options =>
    {
        // Legacy role-based policies
        options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("SuperAdmin"));
        options.AddPolicy("AdminOrAbove", policy => policy.RequireRole("SuperAdmin", "Admin"));
        options.AddPolicy("AllStaff", policy => policy.RequireRole(
            "SuperAdmin", "Admin", "Receptionist", "Therapist", "Inventory", "Accountant", "HR", "Sales Ledger"));

        // Permission-based policies
        options.AddPolicy("Permission:appointments.view", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("appointments.view")));
        options.AddPolicy("Permission:appointments.create", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("appointments.create")));
        options.AddPolicy("Permission:appointments.edit", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("appointments.edit")));
        options.AddPolicy("Permission:appointments.delete", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("appointments.delete")));
        options.AddPolicy("Permission:customers.view", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("customers.view")));
        options.AddPolicy("Permission:customers.create", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("customers.create")));
        options.AddPolicy("Permission:customers.edit", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("customers.edit")));
        options.AddPolicy("Permission:customers.delete", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("customers.delete")));
        options.AddPolicy("Permission:services.view", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("services.view")));
        options.AddPolicy("Permission:services.create", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("services.create")));
        options.AddPolicy("Permission:services.edit", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("services.edit")));
        options.AddPolicy("Permission:services.delete", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("services.delete")));
        options.AddPolicy("Permission:employees.view", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("employees.view")));
        options.AddPolicy("Permission:employees.create", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("employees.create")));
        options.AddPolicy("Permission:employees.edit", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("employees.edit")));
        options.AddPolicy("Permission:employees.delete", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("employees.delete")));
        options.AddPolicy("Permission:inventory.view", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("inventory.view")));
        options.AddPolicy("Permission:inventory.manage", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("inventory.manage")));
        options.AddPolicy("Permission:pos.access", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("pos.access")));
        options.AddPolicy("Permission:pos.refund", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("pos.refund")));
        options.AddPolicy("Permission:pos.discount", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("pos.discount")));
        options.AddPolicy("Permission:accounting.view", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("accounting.view")));
        options.AddPolicy("Permission:accounting.manage", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("accounting.manage")));
        options.AddPolicy("Permission:payroll.view", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("payroll.view")));
        options.AddPolicy("Permission:payroll.manage", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("payroll.manage")));
        options.AddPolicy("Permission:shifts.view", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("shifts.view")));
        options.AddPolicy("Permission:shifts.manage", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("shifts.manage")));
        options.AddPolicy("Permission:timeattendance.view", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("timeattendance.view")));
        options.AddPolicy("Permission:timeattendance.manage", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("timeattendance.manage")));
        options.AddPolicy("Permission:reports.view", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("reports.view")));
        options.AddPolicy("Permission:reports.export", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("reports.export")));
        options.AddPolicy("Permission:notifications.view", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("notifications.view")));
        options.AddPolicy("Permission:notifications.manage", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("notifications.manage")));
        options.AddPolicy("Permission:settings.access", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("settings.access")));
        options.AddPolicy("Permission:settings.manage", policy => policy.Requirements.Add(new MiddayMistSpa.API.Services.PermissionRequirement("settings.manage")));
    });

    // Controllers for API endpoints
    builder.Services.AddControllers()
        .AddApplicationPart(typeof(MiddayMistSpa.API.Controllers.AuthController).Assembly);

    builder.Services.AddHttpContextAccessor();
}

// Configure HttpClient for API communication
InProcessHandler? inProcessHandler = null;
if (!builder.Environment.IsDevelopment())
{
    // Production: route API calls through the in-process middleware pipeline
    // to avoid HTTP loopback issues on MonsterASP.NET shared hosting
    inProcessHandler = new InProcessHandler();
    builder.Services.AddHttpClient("SpaApi", client =>
    {
        client.BaseAddress = new Uri("http://localhost");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    }).ConfigurePrimaryHttpMessageHandler(() => inProcessHandler)
      .SetHandlerLifetime(Timeout.InfiniteTimeSpan);
}
else
{
    builder.Services.AddHttpClient("SpaApi", client =>
    {
        var baseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5286";
        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });
}

// Register Blazor API Services (shared between dev/prod)
builder.Services.AddScoped<IApiClient, ApiClient>();
builder.Services.AddScoped<IAuthStateService, AuthStateService>();
builder.Services.AddScoped<IRolePermissionService, RolePermissionService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IEmployeeApiService, EmployeeApiService>();
builder.Services.AddScoped<ICustomerApiService, CustomerApiService>();
builder.Services.AddScoped<IAppointmentApiService, AppointmentApiService>();
builder.Services.AddScoped<IServiceApiService, ServiceApiService>();
builder.Services.AddScoped<IInventoryApiService, InventoryApiService>();
builder.Services.AddScoped<ITransactionApiService, TransactionApiService>();
builder.Services.AddScoped<IPayrollApiService, PayrollApiService>();
builder.Services.AddScoped<ITimeAttendanceApiService, TimeAttendanceApiService>();
builder.Services.AddScoped<IReportsApiService, ReportsApiService>();
builder.Services.AddScoped<INotificationApiService, NotificationApiService>();
builder.Services.AddScoped<IAccountingApiService, AccountingApiService>();
builder.Services.AddScoped<IProfileApiService, ProfileApiService>();
builder.Services.AddScoped<IShiftApiService, ShiftApiService>();
builder.Services.AddScoped<ICurrencyApiService, CurrencyApiService>();
builder.Services.AddScoped<ITwoFactorApiService, TwoFactorApiService>();
builder.Services.AddScoped<ICaptchaApiService, CaptchaApiService>();
builder.Services.AddScoped<ISettingsApiService, SettingsApiService>();

// Customer Segmentation: use direct service calls in production (bypass HTTP loopback)
if (!builder.Environment.IsDevelopment())
    builder.Services.AddScoped<ICustomerSegmentationService, DirectCustomerSegmentationService>();
else
    builder.Services.AddScoped<ICustomerSegmentationService, CustomerSegmentationService>();

var app = builder.Build();

// =============================================================================
// PRODUCTION: Seed database on startup
// =============================================================================
if (!app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Starting database migration and seeding...");

        var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
        await seeder.SeedAsync();

        logger.LogInformation("Database migration and seeding completed successfully!");
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "FATAL: Database migration/seeding failed. The app will continue but login may not work.");
        // Don't rethrow - let the app start so we can see the error in logs
    }
}

// =============================================================================
// MIDDLEWARE PIPELINE
// =============================================================================
// Production: wire InProcessHandler to the middleware pipeline so all
// HttpClient calls are processed in-process (no network loopback)
if (!app.Environment.IsDevelopment() && inProcessHandler != null)
{
    app.Use(next =>
    {
        inProcessHandler.Configure(next, app.Services.GetRequiredService<IServiceScopeFactory>());
        return next;
    });
    // Explicit UseRouting so the captured 'next' pipeline includes endpoint routing.
    // The framework adds implicit UseRouting BEFORE this point, so without this,
    // the in-process pipeline would never match controller endpoints.
    app.UseRouting();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // Note: HSTS and HTTPS redirection disabled for MonsterASP.NET free plan (no SSL)
    // app.UseHsts();
    // app.UseHttpsRedirection();
}

// Only apply status code pages for non-API routes (API routes should return JSON errors, not HTML)
app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/api"), branch =>
    branch.UseStatusCodePagesWithReExecute("/not-found"));
app.UseStaticFiles();

// Production: Add authentication/authorization middleware for API
if (!app.Environment.IsDevelopment())
{
    app.UseAuthentication();
    app.UseAuthorization();

    // Map API controllers
    app.MapControllers();

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
       .AllowAnonymous();
}

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
