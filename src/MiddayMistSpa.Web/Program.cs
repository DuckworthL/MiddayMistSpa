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
    builder.Services.AddScoped<ICaptchaService, CaptchaService>();

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

    // Authorization - Role-based policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("SuperAdmin"));
        options.AddPolicy("AdminOrAbove", policy => policy.RequireRole("SuperAdmin", "Admin"));
        options.AddPolicy("HRAccess", policy => policy.RequireRole("SuperAdmin", "Admin", "HR"));
        options.AddPolicy("AccountingAccess", policy => policy.RequireRole("SuperAdmin", "Admin", "Accountant"));
        options.AddPolicy("InventoryAccess", policy => policy.RequireRole("SuperAdmin", "Admin", "Inventory"));
        options.AddPolicy("ReceptionistAccess", policy => policy.RequireRole("SuperAdmin", "Admin", "Receptionist"));
        options.AddPolicy("TherapistAccess", policy => policy.RequireRole("SuperAdmin", "Admin", "Therapist"));
        options.AddPolicy("AllStaff", policy => policy.RequireRole(
            "SuperAdmin", "Admin", "Receptionist", "Therapist", "Inventory", "Accountant", "HR"));
    });

    // Controllers for API endpoints
    builder.Services.AddControllers()
        .AddApplicationPart(typeof(MiddayMistSpa.API.Controllers.AuthController).Assembly);

    builder.Services.AddHttpContextAccessor();
}

// Configure HttpClient for API communication
builder.Services.AddHttpClient("SpaApi", client =>
{
    var baseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5286";
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

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
builder.Services.AddScoped<ICustomerSegmentationService, CustomerSegmentationService>();
builder.Services.AddScoped<IProfileApiService, ProfileApiService>();
builder.Services.AddScoped<IShiftApiService, ShiftApiService>();
builder.Services.AddScoped<ICurrencyApiService, CurrencyApiService>();
builder.Services.AddScoped<ITwoFactorApiService, TwoFactorApiService>();
builder.Services.AddScoped<ICaptchaApiService, CaptchaApiService>();
builder.Services.AddScoped<ISettingsApiService, SettingsApiService>();

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
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // Note: HSTS and HTTPS redirection disabled for MonsterASP.NET free plan (no SSL)
    // app.UseHsts();
    // app.UseHttpsRedirection();
}

app.UseStatusCodePagesWithReExecute("/not-found");
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
