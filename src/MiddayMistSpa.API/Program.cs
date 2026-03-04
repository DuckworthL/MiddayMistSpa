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
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("SuperAdmin"));
    options.AddPolicy("AdminOrAbove", policy => policy.RequireRole("SuperAdmin", "Admin"));
    options.AddPolicy("HRAccess", policy => policy.RequireRole("SuperAdmin", "Admin", "HR"));
    options.AddPolicy("AccountingAccess", policy => policy.RequireRole("SuperAdmin", "Admin", "Accountant"));
    options.AddPolicy("InventoryAccess", policy => policy.RequireRole("SuperAdmin", "Admin", "Inventory"));
    options.AddPolicy("SalesAccess", policy => policy.RequireRole("SuperAdmin", "Admin", "Sales"));
    options.AddPolicy("ReceptionistAccess", policy => policy.RequireRole("SuperAdmin", "Admin", "Receptionist"));
    options.AddPolicy("TherapistAccess", policy => policy.RequireRole("SuperAdmin", "Admin", "Therapist"));
    options.AddPolicy("AllStaff", policy => policy.RequireRole(
        "SuperAdmin", "Admin", "Receptionist", "Therapist", "Inventory", "Accountant", "HR", "Sales"));
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
