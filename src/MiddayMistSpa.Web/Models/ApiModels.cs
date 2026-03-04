using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MiddayMistSpa.Web.Models;

// =============================================================================
// Common Models
// =============================================================================

public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();
}

// =============================================================================
// Dashboard Models
// =============================================================================

public class DashboardSummary
{
    public decimal TodayRevenue { get; set; }
    public int TodayAppointments { get; set; }
    public int TodayCompletedAppointments { get; set; }
    public int TodayNewCustomers { get; set; }
    public int ActiveEmployees { get; set; }
    public int LowStockItems { get; set; }
    public decimal MonthRevenue { get; set; }
    public int MonthAppointments { get; set; }
    public List<AppointmentSummary> UpcomingAppointments { get; set; } = new();
    public List<RevenueByDay> WeeklyRevenue { get; set; } = new();
    public List<TopService> TopServices { get; set; } = new();
}

public class AppointmentSummary
{
    public int AppointmentId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string TherapistName { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class RevenueByDay
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int TransactionCount { get; set; }
}

// TopService is defined in Reports Models section below

// =============================================================================
// Employee Models
// =============================================================================

public class EmployeeResponse
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    private string? _fullName;
    public string FullName
    {
        get => !string.IsNullOrEmpty(_fullName) ? _fullName : $"{FirstName} {LastName}".Trim();
        set => _fullName = value;
    }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string Position { get; set; } = string.Empty;
    public string? Department { get; set; }
    public DateTime HireDate { get; set; }
    public string EmploymentType { get; set; } = string.Empty;
    public string? EmploymentStatus { get; set; }
    public decimal BaseSalary { get; set; }
    public bool IsActive { get; set; }
    public bool IsTherapist { get; set; }
    public string? Specialization { get; set; }
    public string? ProfilePhotoUrl { get; set; }
}

public class CreateEmployeeRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string Position { get; set; } = string.Empty;
    public string? Department { get; set; }
    public DateTime HireDate { get; set; } = DateTime.Today;
    public string EmploymentType { get; set; } = "Full-time";
    public decimal BaseSalary { get; set; }
}

public class UpdateEmployeeRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Position { get; set; }
    public string? Department { get; set; }
    public string? EmploymentType { get; set; }
    public decimal? BaseSalary { get; set; }
    public bool? IsActive { get; set; }
}

// =============================================================================
// Customer Models
// =============================================================================

public class CustomerResponse
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string MembershipType { get; set; } = "Regular";
    public int LoyaltyPoints { get; set; }
    public DateTime? LastVisitDate { get; set; }
    public int TotalVisits { get; set; }
    public decimal TotalSpent { get; set; }
    public string? Allergies { get; set; }
    public string? SpecialRequests { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string PreferredCommunicationChannel { get; set; } = "Email";
    public bool SmsConsent { get; set; }
    public bool IsActive { get; set; }
}

public class CreateCustomerRequest
{
    [Required(ErrorMessage = "First name is required")]
    [MinLength(2, ErrorMessage = "First name must be at least 2 characters")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [MinLength(2, ErrorMessage = "Last name must be at least 2 characters")]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Phone number is required")]
    [MinLength(7, ErrorMessage = "Please enter a valid phone number")]
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Allergies { get; set; }
    public string? SpecialRequests { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string PreferredCommunicationChannel { get; set; } = "Email";
    public bool SmsConsent { get; set; }
}

public class UpdateCustomerRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Allergies { get; set; }
    public string? SpecialRequests { get; set; }
    public string? MembershipType { get; set; }
    public bool? IsActive { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string? PreferredCommunicationChannel { get; set; }
    public bool? SmsConsent { get; set; }
}

// =============================================================================
// Appointment Models
// =============================================================================

public class AppointmentResponse
{
    public int AppointmentId { get; set; }
    public string AppointmentNumber { get; set; } = string.Empty;

    // Customer Info
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public string MembershipType { get; set; } = "Regular";

    // Service Info
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceCode { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }

    // Therapist Info
    public int? TherapistId { get; set; }
    public string? TherapistName { get; set; }

    // Room Info
    public int? RoomId { get; set; }
    public string? RoomName { get; set; }

    // Scheduling
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    // Status
    public string Status { get; set; } = string.Empty;
    public string BookingSource { get; set; } = string.Empty;

    // Notes
    public string? CustomerNotes { get; set; }
    public string? TherapistNotes { get; set; }

    // Status Timestamps
    public DateTime? CheckedInAt { get; set; }
    public DateTime? ServiceStartedAt { get; set; }
    public DateTime? ServiceCompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    // Multi-service support
    public List<AppointmentServiceItemResponse> ServiceItems { get; set; } = new();
    public decimal TotalPrice => ServiceItems.Any() ? ServiceItems.Sum(s => s.UnitPrice * s.Quantity) : ServicePrice;
    public int TotalDurationMinutes => ServiceItems.Any() ? ServiceItems.Sum(s => s.DurationMinutes * s.Quantity) : DurationMinutes;

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Computed (for UI convenience)
    public bool IsToday => AppointmentDate.Date == DateTime.Today;
    public bool IsUpcoming => AppointmentDate.Date >= DateTime.Today && Status != "Cancelled" && Status != "Completed";
    public bool CanBeRescheduled => Status is "Scheduled" or "Confirmed";
    public bool CanBeCancelled => Status is "Scheduled" or "Confirmed";

    // Archive
    public bool IsArchived { get; set; }

    // Backward compatibility alias
    public string AppointmentCode => AppointmentNumber;
    public decimal ServicePrice { get; set; }
    public string? Notes => CustomerNotes;
}

public class CreateAppointmentRequest
{
    public int CustomerId { get; set; }
    public int ServiceId { get; set; }
    public int? TherapistId { get; set; }
    public int? RoomId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public string? CustomerNotes { get; set; }
    public string BookingSource { get; set; } = "Direct";

    // Backward compatibility alias for UI binding - syncs with CustomerNotes
    public string? Notes
    {
        get => CustomerNotes;
        set => CustomerNotes = value;
    }
}

public class AppointmentServiceItemResponse
{
    public int AppointmentServiceItemId { get; set; }
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceCode { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int DurationMinutes { get; set; }
    public int Quantity { get; set; } = 1;
    public DateTime AddedAt { get; set; }
}

public class AddServiceToAppointmentRequest
{
    public int ServiceId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class UpdateAppointmentRequest
{
    public int? CustomerId { get; set; }
    public int? ServiceId { get; set; }
    public int? TherapistId { get; set; }
    public int? RoomId { get; set; }
    public DateTime? AppointmentDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public string? CustomerNotes { get; set; }
    public string? Status { get; set; }

    // Backward compatibility alias for UI binding - syncs with CustomerNotes
    public string? Notes
    {
        get => CustomerNotes;
        set => CustomerNotes = value;
    }
}

// =============================================================================
// Service Models
// =============================================================================

public class ServiceResponse
{
    public int ServiceId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Name => ServiceName; // Alias for compatibility
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public decimal RegularPrice { get; set; }
    public decimal Price => RegularPrice; // Alias for compatibility
    public decimal? MemberPrice { get; set; }
    public decimal? PromoPrice { get; set; }
    public bool IsActive { get; set; }
}

public class ServiceCategoryResponse
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Name => CategoryName; // Alias for compatibility
    public string? Description { get; set; }
    public int ServiceCount { get; set; }
    public bool IsActive { get; set; }
}

public class CreateServiceRequest
{
    public string Name { get; set; } = string.Empty;
    public string ServiceName => Name;
    public string? ServiceCode { get; set; }
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public decimal RegularPrice => Price;
    public decimal? MemberPrice { get; set; }
    public bool IsActive { get; set; } = true;
}

// =============================================================================
// Inventory/Product Models
// =============================================================================

public class ProductResponse
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string SKU => ProductCode; // Alias
    public string ProductName { get; set; } = string.Empty;
    public string Name => ProductName; // Alias
    public string? Brand { get; set; }
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public int ProductCategoryId { get; set; } // API field
    public string? CategoryName { get; set; }
    public decimal CostPrice { get; set; }
    public decimal UnitCost => CostPrice; // Alias
    public decimal? SellingPrice { get; set; }
    public decimal RetailPrice => SellingPrice ?? CostPrice; // Alias with fallback
    public decimal CurrentStock { get; set; }
    public int QuantityInStock => (int)CurrentStock; // Alias
    public decimal ReorderLevel { get; set; }
    public string? UnitOfMeasure { get; set; }
    public string Unit => UnitOfMeasure ?? "pcs"; // Alias
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsLowStock { get; set; }
    public bool IsExpiringSoon { get; set; }
}

public class ProductCategoryResponse
{
    public int ProductCategoryId { get; set; }
    public int CategoryId => ProductCategoryId; // Alias for compatibility
    public string CategoryName { get; set; } = string.Empty;
    public string Name => CategoryName; // Alias for compatibility
    public string? Description { get; set; }
    public int ProductCount { get; set; }
    public bool IsActive { get; set; }
}

public class CreateProductRequest
{
    // API requires ProductName
    public string Name { get; set; } = string.Empty;
    public string ProductName => Name;

    public string? SKU { get; set; }
    public string? Brand { get; set; }
    public string? Description { get; set; }

    // API requires ProductCategoryId (not CategoryId)
    public int? CategoryId { get; set; }
    public int ProductCategoryId => CategoryId ?? 0;

    // API requires ProductType
    public string ProductType { get; set; } = "Retail";

    // API requires UnitOfMeasure
    public string Unit { get; set; } = "pcs";
    public string UnitOfMeasure => Unit;

    // Pricing
    public decimal UnitCost { get; set; }
    public decimal CostPrice => UnitCost;
    public decimal RetailPrice { get; set; }
    public decimal? SellingPrice => RetailPrice > 0 ? RetailPrice : null;

    // Stock
    public int InitialStock { get; set; }
    public decimal ReorderLevel { get; set; }

    public DateTime? ExpiryDate { get; set; }
    public string? Supplier { get; set; }
    public bool IsActive { get; set; } = true;
}

public class StockAdjustmentRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }

    // API expects negative QuantityChange for stock-reducing adjustment types
    private static readonly HashSet<string> _decreaseTypes = new(StringComparer.OrdinalIgnoreCase)
        { "Decrease", "Damaged", "Expired", "Spoilage", "Shrinkage", "Sold", "Service Usage" };

    public decimal QuantityChange => _decreaseTypes.Contains(AdjustmentType) ? -Quantity : Quantity;

    public string AdjustmentType { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

// =============================================================================
// Transaction Models
// =============================================================================

public class TransactionResponse
{
    public int TransactionId { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string MembershipType { get; set; } = string.Empty;
    public int? AppointmentId { get; set; }
    public DateTime TransactionDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TipAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public int CashierId { get; set; }
    public string? CashierName { get; set; }
    public DateTime? VoidedAt { get; set; }
    public string? VoidedByName { get; set; }
    public string? VoidReason { get; set; }
    public List<TransactionServiceItemResponse> ServiceItems { get; set; } = new();
    public List<TransactionProductItemResponse> ProductItems { get; set; } = new();
    public List<RefundResponse> Refunds { get; set; } = new();
    public int ServiceItemCount { get; set; }
    public int ProductItemCount { get; set; }
    public DateTime CreatedAt { get; set; }

    // Multi-Currency
    public string ClientCurrency { get; set; } = "PHP";
    public string? ClientCountryCode { get; set; }
    public string? ClientIPAddress { get; set; }
    public decimal ExchangeRate { get; set; } = 1.0m;
    public decimal? TotalInClientCurrency { get; set; }
}

public class TransactionServiceItemResponse
{
    public int TransactionServiceItemId { get; set; }
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceCode { get; set; } = string.Empty;
    public int? TherapistId { get; set; }
    public string? TherapistName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
}

public class TransactionProductItemResponse
{
    public int TransactionProductItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
}

public class RefundResponse
{
    public int RefundId { get; set; }
    public int TransactionId { get; set; }
    public decimal RefundAmount { get; set; }
    public string RefundMethod { get; set; } = string.Empty;
    public string RefundType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? ApprovedByName { get; set; }
    public string? ProcessedByName { get; set; }
    public DateTime RefundDate { get; set; }
}

public class TransactionServiceItem
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
}

public class TransactionProductItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal TotalPrice { get; set; }
}

public class CreateTransactionRequest
{
    public int CustomerId { get; set; }
    public int? AppointmentId { get; set; }
    public List<CreateTransactionServiceItemRequest> ServiceItems { get; set; } = new();
    public List<CreateTransactionProductItemRequest> ProductItems { get; set; } = new();
    public decimal DiscountPercentage { get; set; } = 0;
    public decimal DiscountAmount { get; set; } = 0;
    public decimal TipAmount { get; set; } = 0;
    public string PaymentMethod { get; set; } = "Cash";
    public decimal? AmountTendered { get; set; }

    // Multi-Currency
    public string? ClientCurrency { get; set; }
    public string? ClientIPAddress { get; set; }
}

public class CreateTransactionServiceItemRequest
{
    public int ServiceId { get; set; }
    public int? TherapistId { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal? UnitPrice { get; set; }
}

public class CreateTransactionProductItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal? UnitPrice { get; set; }
}

public class FinalizePendingTransactionRequest
{
    public List<CreateTransactionProductItemRequest> ProductItems { get; set; } = new();
    public string PaymentMethod { get; set; } = "Cash";
    public decimal DiscountPercentage { get; set; } = 0;
    public decimal DiscountAmount { get; set; } = 0;
    public decimal TipAmount { get; set; } = 0;
    public decimal? AmountTendered { get; set; }
    public string? ClientCurrency { get; set; }
    public string? ClientIPAddress { get; set; }
}

// =============================================================================
// Auth Models
// =============================================================================

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public TokenResponse? Token { get; set; }
    public UserInfo? User { get; set; }
    public bool RequiresPasswordChange { get; set; }
    public bool RequiresTwoFactor { get; set; }
    public string? TwoFactorToken { get; set; }
    public int? RemainingAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }
}

public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string TokenType { get; set; } = "Bearer";
}

public class UserInfo
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? RoleName { get; set; }
    public string Role => RoleName ?? "User";
    public int? EmployeeId { get; set; }
}

// =============================================================================
// Lookup/Reference Data
// =============================================================================

// =============================================================================
// Two-Factor Auth Models
// =============================================================================

public class TwoFactorSetupResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? QrCodeBase64 { get; set; }
    public string? ManualEntryKey { get; set; }
}

public class TwoFactorVerifySetupRequest
{
    public string Code { get; set; } = string.Empty;
}

public class TwoFactorVerifySetupResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string> RecoveryCodes { get; set; } = new();
}

public class TwoFactorValidateRequest
{
    public string TwoFactorToken { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? RecoveryCode { get; set; }
}

public class TwoFactorDisableRequest
{
    public string Code { get; set; } = string.Empty;
}

public class TwoFactorStatusResponse
{
    public bool IsEnabled { get; set; }
    public DateTime? EnabledAt { get; set; }
}

public class LookupItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public decimal? Price { get; set; }
    public int? Stock { get; set; }
}

// =============================================================================
// Available Slot Models
// =============================================================================

public class AvailableSlotResponse
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public List<AvailableTherapistResponse> AvailableTherapists { get; set; } = new();
    public List<UnavailableTherapistResponse> UnavailableTherapists { get; set; } = new();
}

public class AvailableTherapistResponse
{
    public int TherapistId { get; set; }
    public string TherapistName { get; set; } = string.Empty;
}

public class UnavailableTherapistResponse
{
    public int TherapistId { get; set; }
    public string TherapistName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

// Room Models
// =============================================================================

public class RoomResponse
{
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string RoomCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string RoomType { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public bool IsActive { get; set; }
}

// =============================================================================
// Category Models
// =============================================================================

public class CreateCategoryRequest
{
    [JsonPropertyName("categoryName")]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
}

// =============================================================================
// Extended Service Models
// =============================================================================

public class UpdateServiceRequest
{
    public string? Name { get; set; }
    public string? ServiceCode { get; set; }
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public int? DurationMinutes { get; set; }
    public decimal? Price { get; set; }
    public bool? IsActive { get; set; }
}

// =============================================================================
// Extended Product Models  
// =============================================================================

public class UpdateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string ProductName => Name;
    public string? SKU { get; set; }
    public string? Brand { get; set; }
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public int ProductCategoryId => CategoryId ?? 0;
    public string ProductType { get; set; } = "Retail";
    public string UnitOfMeasure { get; set; } = "pcs";
    public decimal UnitCost { get; set; }
    public decimal CostPrice => UnitCost;
    public decimal RetailPrice { get; set; }
    public decimal? SellingPrice => RetailPrice > 0 ? RetailPrice : null;
    public decimal ReorderLevel { get; set; }
    public string? Supplier { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
}

// =============================================================================
// Transaction Item Models for POS
// =============================================================================

public class TransactionItemRequest
{
    public int? ServiceId { get; set; }
    public int? ProductId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
}

public class TransactionItem
{
    public int? ServiceId { get; set; }
    public int? ProductId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

// =============================================================================
// Payroll Models
// =============================================================================

public class PayrollPeriodResponse
{
    public int PayrollPeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string PayrollType { get; set; } = string.Empty;
    public DateTime CutoffDate { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? FinalizedBy { get; set; }
    public string? FinalizedByName { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public int RecordCount { get; set; }
    public decimal TotalGrossPay { get; set; }
    public decimal TotalNetPay { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatePayrollPeriodRequest
{
    public string PeriodName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string PayrollType { get; set; } = "Semi-Monthly";
    public DateTime CutoffDate { get; set; }
    public DateTime PaymentDate { get; set; }
}

public class PayrollRecordResponse
{
    public int PayrollRecordId { get; set; }
    public int PayrollPeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public decimal DaysWorked { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal Commissions { get; set; }
    public decimal Tips { get; set; }
    public decimal TotalAllowances { get; set; }
    public decimal GrossPay { get; set; }

    // Employee Deductions
    public decimal SSSContribution { get; set; }
    public decimal PhilHealthContribution { get; set; }
    public decimal PagIBIGContribution { get; set; }
    public decimal WithholdingTax { get; set; }
    public decimal TotalDeductions { get; set; }

    // Employer Contributions
    public decimal SSSEmployerContribution { get; set; }
    public decimal PhilHealthEmployerContribution { get; set; }
    public decimal PagIBIGEmployerContribution { get; set; }
    public decimal ECContribution { get; set; }

    public decimal NetPay { get; set; }
}

public class CreatePayrollRecordRequest
{
    public int PayrollPeriodId { get; set; }
    public int EmployeeId { get; set; }
    public decimal DaysWorked { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal OvertimeHours { get; set; }
}

public class UpdatePayrollRecordRequest
{
    public decimal? DaysWorked { get; set; }
    public decimal? HoursWorked { get; set; }
    public decimal? OvertimeHours { get; set; }
    public decimal? BasicSalary { get; set; }
    public decimal? Commissions { get; set; }
    public decimal? Tips { get; set; }
}

public class BonusResponse
{
    public int BonusId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string BonusType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
}

public class CreateBonusRequest
{
    public int EmployeeId { get; set; }
    public string BonusType { get; set; } = "Performance";
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; } = DateTime.Today;
}

// =============================================================================
// Time & Attendance Models
// =============================================================================

public class ScheduleResponse
{
    public int ScheduleId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
    public string DayOfWeekName { get; set; } = string.Empty;
    public TimeSpan ShiftStartTime { get; set; }
    public TimeSpan ShiftEndTime { get; set; }
    public TimeSpan? BreakStartTime { get; set; }
    public TimeSpan? BreakEndTime { get; set; }
    public bool IsRestDay { get; set; }
    public decimal WorkingHours { get; set; }
}

public class WeeklyScheduleResponse
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public List<ScheduleDay> Schedules { get; set; } = new();
    public decimal TotalWeeklyHours { get; set; }
    public int WorkDays { get; set; }
    public int RestDays { get; set; }
}

public class ScheduleDay
{
    public int ScheduleId { get; set; }
    public int DayOfWeek { get; set; }
    public string DayOfWeekName { get; set; } = string.Empty;
    public TimeSpan ShiftStartTime { get; set; }
    public TimeSpan ShiftEndTime { get; set; }
    public bool IsRestDay { get; set; }
    public decimal WorkingHours { get; set; }
}

public class CreateScheduleRequest
{
    public int EmployeeId { get; set; }
    public int DayOfWeek { get; set; }
    public TimeSpan ShiftStartTime { get; set; }
    public TimeSpan ShiftEndTime { get; set; }
    public bool IsRestDay { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.Today;
}

public class AttendanceRecordResponse
{
    public int AttendanceId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public DateTime Date { get; set; }
    public DateTime? ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
    public DateTime? BreakStart { get; set; }
    public DateTime? BreakEnd { get; set; }
    public decimal TotalHours { get; set; }
    public decimal BreakMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
}

public class AttendanceSummaryResponse
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalWorkDays { get; set; }
    public int ScheduledWorkDays { get; set; }
    public decimal TotalHoursWorked { get; set; }
    public int LeaveDays { get; set; }
    public int AbsentDays { get; set; }
    public decimal AttendanceRate { get; set; }
}

public class TimeOffResponse
{
    public int TimeOffId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string LeaveType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? ApprovedById { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

public class CreateTimeOffRequest
{
    public int EmployeeId { get; set; }
    public string LeaveType { get; set; } = "Vacation";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class LiveAttendanceStatus
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public bool IsClockedIn { get; set; }
    public DateTime? ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
    public bool OnBreak { get; set; }
    public DateTime? BreakStart { get; set; }
    public decimal TodayHours { get; set; }
    public string Status { get; set; } = "NotYetIn";
}

public class CreateManualEntryRequest
{
    public int EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public DateTime ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
    public int BreakMinutes { get; set; }
    public string? Notes { get; set; }
}

// =============================================================================
// Reports Models
// =============================================================================

public class DashboardReportResponse
{
    public DateTime GeneratedAt { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DashboardKpi Kpis { get; set; } = new();
    public RevenueBreakdown Revenue { get; set; } = new();
    public AppointmentsSummary Appointments { get; set; } = new();
    public InventoryAlerts Inventory { get; set; } = new();
    public List<TopPerformer> TopTherapists { get; set; } = new();
    public List<TopService> TopServices { get; set; } = new();
    public List<TopCustomer> TopCustomers { get; set; } = new();
}

public class DashboardKpi
{
    public decimal TotalRevenue { get; set; }
    public decimal RevenueChange { get; set; }
    public int TotalAppointments { get; set; }
    public int AppointmentChange { get; set; }
    public int NewCustomers { get; set; }
    public int NewCustomerChange { get; set; }
    public decimal AverageTicket { get; set; }
    public decimal OccupancyRate { get; set; }
    public decimal CustomerRetentionRate { get; set; }
}

public class RevenueBreakdown
{
    public decimal ServiceRevenue { get; set; }
    public decimal ProductRevenue { get; set; }
    public decimal PackageRevenue { get; set; }
    public decimal TipsReceived { get; set; }
    public decimal Discounts { get; set; }
    public decimal RefundsProcessed { get; set; }
    public decimal NetRevenue { get; set; }
    public List<DailyRevenue> DailyTrend { get; set; } = new();
}

public class DailyRevenue
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int AppointmentCount { get; set; }
}

public class AppointmentsSummary
{
    public int Scheduled { get; set; }
    public int Completed { get; set; }
    public int Cancelled { get; set; }
    public int NoShow { get; set; }
    public int InProgress { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal CancellationRate { get; set; }
    public decimal NoShowRate { get; set; }
}

public class InventoryAlerts
{
    public int LowStockItems { get; set; }
    public int OutOfStockItems { get; set; }
    public int ExpiringItems { get; set; }
    public List<LowStockItem> LowStockList { get; set; } = new();
}

public class LowStockItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int ReorderLevel { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class TopPerformer
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int AppointmentsCompleted { get; set; }
    public decimal RevenueGenerated { get; set; }
    public decimal CommissionsEarned { get; set; }
    public decimal AverageRating { get; set; }
}

public class TopService
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class TopCustomer
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int VisitCount { get; set; }
    public decimal TotalSpent { get; set; }
    public int LoyaltyPoints { get; set; }
}

public class QuickStatsResponse
{
    public decimal TodayRevenue { get; set; }
    public int TodayAppointments { get; set; }
    public int PendingAppointments { get; set; }
    public int ClockedInEmployees { get; set; }
    public int LowStockAlerts { get; set; }
}

public class SalesReportResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageTransaction { get; set; }
    public decimal ServiceRevenue { get; set; }
    public decimal ProductRevenue { get; set; }
    public List<SalesByCategory> ByCategory { get; set; } = new();
    public List<DailySales> DailySales { get; set; } = new();
}

public class SalesByCategory
{
    public string Category { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class DailySales
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int TransactionCount { get; set; }
}

public class ServicePerformanceResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<ServicePerformanceItem> Services { get; set; } = new();
}

public class ServicePerformanceItem
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal AverageRating { get; set; }
    public decimal GrowthRate { get; set; }
}

public class EmployeePerformanceResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<EmployeePerformanceItem> Employees { get; set; } = new();
}

public class EmployeePerformanceItem
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public int AppointmentsScheduled { get; set; }
    public int AppointmentsCompleted { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal RevenueGenerated { get; set; }
    public decimal CommissionsEarned { get; set; }
    public decimal AverageServiceValue { get; set; }
    public int DaysWorked { get; set; }
    public decimal UtilizationRate { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
}

public class CustomerReportResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalCustomers { get; set; }
    public int NewCustomers { get; set; }
    public int ReturningCustomers { get; set; }
    public decimal RetentionRate { get; set; }
    public decimal AverageSpend { get; set; }
    public List<CustomerSegment> Segments { get; set; } = new();
}

public class CustomerSegment
{
    public string SegmentName { get; set; } = string.Empty;
    public int CustomerCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageSpend { get; set; }
}

public class InventoryReportResponse
{
    public int TotalProducts { get; set; }
    public decimal TotalValue { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public int ExpiringCount { get; set; }
    public List<InventoryByCategory> ByCategory { get; set; } = new();
}

public class InventoryByCategory
{
    public string Category { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public decimal TotalValue { get; set; }
    public int LowStockCount { get; set; }
}

// =============================================================================
// Notification Models
// =============================================================================

public class NotificationResponse
{
    public int NotificationId { get; set; }
    public int? UserId { get; set; }
    public int? EmployeeId { get; set; }
    public int? CustomerId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Recipient { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }

    // Computed properties for UI
    public bool IsRead => ReadAt.HasValue || Status == "Read";

    public string TimeAgo
    {
        get
        {
            var span = DateTime.UtcNow - CreatedAt;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} minutes ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} hours ago";
            if (span.TotalDays < 2) return "Yesterday";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays} days ago";
            return CreatedAt.ToString("MMM d, yyyy");
        }
    }

    public string ActionUrl => Category switch
    {
        "Appointment" => "/appointments",
        "Inventory" => "/inventory",
        "Payroll" => "/payroll",
        "Customer" => "/customers",
        "Transaction" => "/transactions",
        _ => null!
    };
}

public class CreateNotificationRequest
{
    public int? UserId { get; set; }
    public int? EmployeeId { get; set; }
    public int? CustomerId { get; set; }
    public string Type { get; set; } = "InApp";
    public string Category { get; set; } = "System";
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Recipient { get; set; }
    public bool SendImmediately { get; set; } = true;
    public DateTime? ScheduledAt { get; set; }
}

// =============================================================================
// Accounting Models
// =============================================================================

public class ChartOfAccountResponse
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string? AccountCategory { get; set; }
    public int? ParentAccountId { get; set; }
    public string? ParentAccountName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal Balance { get; set; }
    public List<ChartOfAccountResponse> ChildAccounts { get; set; } = new();
}

public class CreateAccountRequest
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string? AccountCategory { get; set; }
    public int? ParentAccountId { get; set; }
}

public class UpdateAccountRequest
{
    public string? AccountName { get; set; }
    public string? AccountCategory { get; set; }
    public bool? IsActive { get; set; }
}

public class JournalEntryResponse
{
    public int JournalEntryId { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string? Description { get; set; }
    public string? ReferenceType { get; set; }
    public string? ReferenceId { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public bool IsBalanced { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<JournalEntryLineResponse> Lines { get; set; } = new();
}

public class JournalEntryLineResponse
{
    public int JournalLineId { get; set; }
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string? Description { get; set; }
}

public class CreateJournalEntryRequest
{
    public DateTime EntryDate { get; set; }
    public string? Description { get; set; }
    public string? ReferenceType { get; set; }
    public string? ReferenceId { get; set; }
    public List<CreateJournalLineRequest> Lines { get; set; } = new();
}

public class CreateJournalLineRequest
{
    public int AccountId { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string? Description { get; set; }
}

public class TrialBalanceResponse
{
    public DateTime AsOfDate { get; set; }
    public List<TrialBalanceLineItem> Items { get; set; } = new();
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public bool IsBalanced { get; set; }
}

public class TrialBalanceLineItem
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public decimal DebitBalance { get; set; }
    public decimal CreditBalance { get; set; }
}

public class IncomeStatementResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<IncomeStatementSection> Sections { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetIncome { get; set; }
}

public class IncomeStatementSection
{
    public string SectionName { get; set; } = string.Empty;
    public List<IncomeStatementLineItem> Items { get; set; } = new();
    public decimal Total { get; set; }
}

public class IncomeStatementLineItem
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class BalanceSheetResponse
{
    public DateTime AsOfDate { get; set; }
    public BalanceSheetSection Assets { get; set; } = new();
    public BalanceSheetSection Liabilities { get; set; } = new();
    public BalanceSheetSection Equity { get; set; } = new();
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }
    public bool IsBalanced { get; set; }
}

public class BalanceSheetSection
{
    public string SectionName { get; set; } = string.Empty;
    public List<BalanceSheetLineItem> Items { get; set; } = new();
    public decimal Total { get; set; }
}

public class BalanceSheetLineItem
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public decimal Amount { get; set; }
}

public class AccountLedgerResponse
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public List<LedgerEntry> Entries { get; set; } = new();
}

public class LedgerEntry
{
    public DateTime Date { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
}

public class ExpenseResponse
{
    public int JournalEntryId { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? Description { get; set; }
    public string? Vendor { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
}

public class CreateExpenseRequest
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Vendor { get; set; }
    public int ExpenseAccountId { get; set; }
    public int PaymentAccountId { get; set; }
    public decimal Amount { get; set; }
}

public class IncomeRecordResponse
{
    public int JournalEntryId { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? Description { get; set; }
    public string? Customer { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreateIncomeRequest
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public int RevenueAccountId { get; set; }
    public int DepositAccountId { get; set; }
    public decimal Amount { get; set; }
}

public class InvoiceResponse
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<InvoiceLineResponse> Lines { get; set; } = new();
}

public class InvoiceLineResponse
{
    public int LineId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}

public class CreateInvoiceRequest
{
    public int CustomerId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public List<CreateInvoiceLineRequest> Lines { get; set; } = new();
}

public class CreateInvoiceLineRequest
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class AccountingSummaryResponse
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetIncome { get; set; }
    public decimal CashOnHand { get; set; }
    public decimal AccountsReceivable { get; set; }
    public decimal AccountsPayable { get; set; }
    public List<MonthlyTrendItem> MonthlyTrend { get; set; } = new();
    public List<ExpenseCategoryItem> ExpensesByCategory { get; set; } = new();
    public List<RevenueStreamItem> RevenueStreams { get; set; } = new();
}

public class MonthlyTrendItem
{
    public string Month { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
    public decimal NetIncome { get; set; }
}

public class ExpenseCategoryItem
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
}

public class RevenueStreamItem
{
    public string Stream { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
}

// =============================================================================
// Customer Segmentation / DBSCAN Models
// =============================================================================

public class DbscanParametersRequest
{
    public double Epsilon { get; set; } = 0.15;
    public int MinSamples { get; set; } = 2;
    public bool NormalizeData { get; set; } = true;
    public double RecencyWeight { get; set; } = 1.0;
    public double FrequencyWeight { get; set; } = 1.0;
    public double MonetaryWeight { get; set; } = 1.0;
}

public class ClusteringResultResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalCustomersAnalyzed { get; set; }
    public int ClustersFound { get; set; }
    public int NoisePoints { get; set; }
    public DateTime AnalysisDate { get; set; }
    public List<ClusterSummary> Clusters { get; set; } = new();
    public DbscanParametersRequest? ParametersUsed { get; set; }
    public ClusteringPerformanceMetrics? PerformanceMetrics { get; set; }
}

public class ClusteringPerformanceMetrics
{
    public double SilhouetteScore { get; set; }
    public double AvgIntraClusterDistance { get; set; }
    public double AvgInterClusterDistance { get; set; }
    public double DaviesBouldinIndex { get; set; }
    public double CoveragePercent { get; set; }
    public string QualityRating { get; set; } = string.Empty;
    public double OverallScore { get; set; }
}

public class ClusterSummary
{
    public int ClusterId { get; set; }
    public string SegmentName { get; set; } = string.Empty;
    public string SegmentCode { get; set; } = string.Empty;
    public int CustomerCount { get; set; }
    public decimal AverageRecency { get; set; }
    public decimal AverageFrequency { get; set; }
    public decimal AverageMonetaryValue { get; set; }
    public string RecommendedAction { get; set; } = string.Empty;
}

public class ClusteringStatusResponse
{
    public DateTime? LastAnalysisDate { get; set; }
    public int TotalCustomers { get; set; }
    public int SegmentedCustomers { get; set; }
    public int UnassignedCustomers { get; set; }
    public List<SegmentStatusItem> Segments { get; set; } = new();
}

public class SegmentStatusItem
{
    public int SegmentId { get; set; }
    public string SegmentName { get; set; } = string.Empty;
    public int CustomerCount { get; set; }
    public decimal Percentage { get; set; }
}

public class CustomerSegmentResponse
{
    public int SegmentId { get; set; }
    public string SegmentName { get; set; } = string.Empty;
    public string SegmentCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ClusterId { get; set; }
    public decimal? AverageRecency { get; set; }
    public decimal? AverageFrequency { get; set; }
    public decimal? AverageMonetaryValue { get; set; }
    public int CustomerCount { get; set; }
    public string? RecommendedAction { get; set; }
    public DateTime? LastAnalysisDate { get; set; }
}

public class SegmentDetailResponse
{
    public int SegmentId { get; set; }
    public string SegmentName { get; set; } = string.Empty;
    public string SegmentCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ClusterId { get; set; }
    public decimal? AverageRecency { get; set; }
    public decimal? AverageFrequency { get; set; }
    public decimal? AverageMonetaryValue { get; set; }
    public int CustomerCount { get; set; }
    public string? RecommendedAction { get; set; }
    public DateTime? LastAnalysisDate { get; set; }
    public List<CustomerListItem> TopCustomers { get; set; } = new();
    public RfmDistribution? RfmDistribution { get; set; }
}

public class RfmDistribution
{
    public decimal MinRecency { get; set; }
    public decimal MaxRecency { get; set; }
    public decimal MinFrequency { get; set; }
    public decimal MaxFrequency { get; set; }
    public decimal MinMonetary { get; set; }
    public decimal MaxMonetary { get; set; }
}

public class CustomerListItem
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string MembershipType { get; set; } = string.Empty;
    public int LoyaltyPoints { get; set; }
    public DateTime? LastVisitDate { get; set; }
    public int TotalVisits { get; set; }
    public string? CustomerSegment { get; set; }
    public bool HasAllergies { get; set; }
    public bool IsActive { get; set; }
}

public class CustomerRfmMetricsResponse
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CurrentSegment { get; set; }
    public int DaysSinceLastVisit { get; set; }
    public int TotalVisits { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal RecencyScore { get; set; }
    public decimal FrequencyScore { get; set; }
    public decimal MonetaryScore { get; set; }
    public string RfmSegment { get; set; } = string.Empty;
    public DateTime? FirstVisitDate { get; set; }
    public DateTime? LastVisitDate { get; set; }
}

// =============================================================================
// Profile / Auth Models
// =============================================================================

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

// =============================================================================
// Shift Management Models
// =============================================================================

public class ShiftResponse
{
    public int ShiftId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Duration { get; set; } = string.Empty;
    public bool IsRecurring { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class WeeklyShiftScheduleResponse
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public List<ShiftResponse> Shifts { get; set; } = new();
    public decimal TotalWeeklyHours { get; set; }
}

public class CreateShiftRequest
{
    public int EmployeeId { get; set; }
    public int DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsRecurring { get; set; } = true;
    public DateTime EffectiveFrom { get; set; } = DateTime.Today;
    public DateTime? EffectiveTo { get; set; }
}

public class UpdateShiftRequest
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsRecurring { get; set; } = true;
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public class BulkShiftRequest
{
    public int EmployeeId { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.Today;
    public List<DayShiftModel> Shifts { get; set; } = new();
}

public class DayShiftModel
{
    public int DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class ShiftExceptionResponse
{
    public int ExceptionId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime ExceptionDate { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string? Reason { get; set; }
    public bool IsFullDayOff { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateShiftExceptionRequest
{
    public int EmployeeId { get; set; }
    public DateTime ExceptionDate { get; set; } = DateTime.Today;
    public string ExceptionType { get; set; } = "TimeOff";
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string? Reason { get; set; }
}

public class UpdateShiftExceptionRequest
{
    public string ExceptionType { get; set; } = string.Empty;
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string? Reason { get; set; }
}

public class EmployeeAvailabilityResponse
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string DayName { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public string Status { get; set; } = string.Empty;
    public TimeSpan? ShiftStart { get; set; }
    public TimeSpan? ShiftEnd { get; set; }
    public string? ExceptionReason { get; set; }
}

public class ChangePasswordResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string>? ValidationErrors { get; set; }
}

// ============================================================================
// Currency Models
// ============================================================================

public class CurrencyRateResponse
{
    public int RateId { get; set; }
    public string BaseCurrency { get; set; } = "PHP";
    public string TargetCurrency { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public DateTime LastUpdated { get; set; }
    public string Source { get; set; } = string.Empty;
    public bool IsStale { get; set; }
}

public class AllRatesResponse
{
    public string BaseCurrency { get; set; } = "PHP";
    public DateTime LastUpdated { get; set; }
    public List<CurrencyRateResponse> Rates { get; set; } = new();
}

public class ConvertCurrencyRequest
{
    public decimal Amount { get; set; }
    public string FromCurrency { get; set; } = "PHP";
    public string ToCurrency { get; set; } = "USD";
}

public class ConvertCurrencyResponse
{
    public decimal OriginalAmount { get; set; }
    public string FromCurrency { get; set; } = string.Empty;
    public decimal ConvertedAmount { get; set; }
    public string ToCurrency { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public DateTime RateTimestamp { get; set; }
    public bool RateIsStale { get; set; }
}

public class DetectedClientInfoResponse
{
    public string IpAddress { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public string DetectedCurrency { get; set; } = string.Empty;
    public bool IsSupported { get; set; }
    public string SuggestedCurrency { get; set; } = "PHP";
    public decimal? ExchangeRate { get; set; }
}

public class SupportedCurrencyInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal? CurrentRate { get; set; }
}

public class SupportedCurrenciesResponse
{
    public string BaseCurrency { get; set; } = "PHP";
    public List<SupportedCurrencyInfo> Currencies { get; set; } = new();
}